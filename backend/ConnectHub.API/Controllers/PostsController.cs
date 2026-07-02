using System.Security.Claims;
using System.Text.RegularExpressions;
using ConnectHub.API.Data;
using ConnectHub.API.DTOs;
using ConnectHub.API.Models;
using ConnectHub.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

namespace ConnectHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly FileStorageService _files;
    private readonly NotificationService _notifications;

    public PostsController(ApplicationDbContext db, FileStorageService files, NotificationService notifications)
    {
        _db = db;
        _files = files;
        _notifications = notifications;
    }

    // GET /api/posts?page=1&pageSize=10 -> público, paginado
    [HttpGet]
    public async Task<ActionResult<PagedResult<PostDto>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        // Si hay alguien logueado, lo usamos para marcar IsLikedByCurrentUser.
        // Si no (endpoint público), queda en 0 y nunca coincide con un UserId real.
        var currentUserId = GetCurrentUserIdOrZero();

        return Ok(await PagePosts(_db.Posts, page, pageSize, currentUserId));
    }

    // GET /api/posts/feed -> solo posts de los usuarios que sigo (requiere JWT).
    // Se define ANTES que "{id:int}" y con segmento literal "feed" para que el router
    // no lo confunda con un id.
    [Authorize]
    [HttpGet("feed")]
    public async Task<ActionResult<PagedResult<PostDto>>> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();

        // IDs de los usuarios que sigo. Lo dejamos como IQueryable (subconsulta)
        // para que el filtro se traduzca a un solo SQL con un IN (...).
        var followedIds = _db.Follows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowedId);

        var query = _db.Posts.Where(p => followedIds.Contains(p.UserId));
        return Ok(await PagePosts(query, page, pageSize, userId));
    }

    // Aplica orden, paginación y proyección a DTO sobre cualquier consulta de posts.
    private async Task<PagedResult<PostDto>> PagePosts(IQueryable<Post> query, int page, int pageSize, int currentUserId)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var ordered = query.OrderByDescending(p => p.CreatedAt);
        var total = await ordered.CountAsync();

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PostDto
            {
                Id = p.Id,
                Content = p.Content,
                ImageUrl = p.ImageUrl,
                CreatedAt = p.CreatedAt,
                UserId = p.UserId,
                Username = p.User.Username,
                UserAvatarUrl = p.User.AvatarUrl,
                LikesCount = p.Likes.Count,
                IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),
                CommentsCount = p.Comments.Count
            })
            .ToListAsync();

        return new PagedResult<PostDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total,
            HasMore = page * pageSize < total
        };
    }

    // GET /api/posts/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PostDto>> GetById(int id)
    {
        var currentUserId = GetCurrentUserIdOrZero();

        var post = await _db.Posts
            .Where(p => p.Id == id)
            .Select(p => new PostDto
            {
                Id = p.Id,
                Content = p.Content,
                ImageUrl = p.ImageUrl,
                CreatedAt = p.CreatedAt,
                UserId = p.UserId,
                Username = p.User.Username,
                UserAvatarUrl = p.User.AvatarUrl,
                LikesCount = p.Likes.Count,
                IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),
                CommentsCount = p.Comments.Count
            })
            .FirstOrDefaultAsync();

        if (post is null) return NotFound();
        return Ok(post);
    }

    // POST /api/posts -> requiere JWT
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<PostDto>> Create(CreatePostDto dto)
    {
        var userId = GetUserId();

        var post = new Post
        {
            Content = dto.Content,
            ImageUrl = dto.ImageUrl,
            UserId = userId
        };

        _db.Posts.Add(post);
        await _db.SaveChangesAsync();

        // Extraemos los #hashtags del contenido y los vinculamos al post.
        await SyncHashtagsAsync(post);

        // Cargamos el user para la respuesta
        await _db.Entry(post).Reference(p => p.User).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = post.Id }, new PostDto
        {
            Id = post.Id,
            Content = post.Content,
            ImageUrl = post.ImageUrl,
            CreatedAt = post.CreatedAt,
            UserId = post.UserId,
            Username = post.User.Username,
            UserAvatarUrl = post.User.AvatarUrl,
            LikesCount = 0,
            IsLikedByCurrentUser = false
        });
    }

    // DELETE /api/posts/5 -> solo el dueño
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var post = await _db.Posts.FindAsync(id);

        if (post is null) return NotFound();
        if (post.UserId != userId) return Forbid();

        _db.Posts.Remove(post);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/posts/5/like -> dar like (requiere JWT)
    [Authorize]
    [HttpPost("{id}/like")]
    public async Task<IActionResult> Like(int id)
    {
        var userId = GetUserId();

        // Traemos el dueño del post (para notificarle) y de paso validamos que existe.
        var ownerId = await _db.Posts.Where(p => p.Id == id)
            .Select(p => (int?)p.UserId).FirstOrDefaultAsync();
        if (ownerId is null) return NotFound(new { message = "Post no encontrado" });

        var alreadyLiked = await _db.Likes.AnyAsync(l => l.PostId == id && l.UserId == userId);
        if (alreadyLiked) return BadRequest(new { message = "Ya diste like a este post" });

        _db.Likes.Add(new Like { PostId = id, UserId = userId });
        await _db.SaveChangesAsync();

        await _notifications.CreateAsync(ownerId.Value, userId, NotificationType.Like, id);

        var likesCount = await _db.Likes.CountAsync(l => l.PostId == id);
        return Ok(new { liked = true, likesCount });
    }

    // DELETE /api/posts/5/like -> quitar like (requiere JWT)
    [Authorize]
    [HttpDelete("{id}/like")]
    public async Task<IActionResult> Unlike(int id)
    {
        var userId = GetUserId();

        var like = await _db.Likes.FirstOrDefaultAsync(l => l.PostId == id && l.UserId == userId);
        if (like is null) return NotFound(new { message = "No habías dado like a este post" });

        _db.Likes.Remove(like);
        await _db.SaveChangesAsync();

        var likesCount = await _db.Likes.CountAsync(l => l.PostId == id);
        return Ok(new { liked = false, likesCount });
    }

    // POST /api/posts/upload-image -> sube una imagen y devuelve su URL (requiere JWT).
    // El flujo del cliente: primero sube la imagen aquí, luego crea el post con esa URL.
    [Authorize]
    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        try
        {
            var url = await _files.SaveImageAsync(file, "posts");
            return Ok(new { url });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET /api/posts/5/comments -> público. Comentarios raíz con sus respuestas anidadas.
    [HttpGet("{id}/comments")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetComments(int id)
    {
        var postExists = await _db.Posts.AnyAsync(p => p.Id == id);
        if (!postExists) return NotFound(new { message = "Post no encontrado" });

        // Traemos solo los comentarios raíz (ParentCommentId == null) y, dentro de la
        // misma proyección, sus respuestas. EF Core traduce esta proyección anidada a SQL.
        var comments = await _db.Comments
            .Where(c => c.PostId == id && c.ParentCommentId == null)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                UserId = c.UserId,
                Username = c.User.Username,
                UserAvatarUrl = c.User.AvatarUrl,
                ParentCommentId = c.ParentCommentId,
                Replies = c.Replies
                    .OrderBy(r => r.CreatedAt)
                    .Select(r => new CommentDto
                    {
                        Id = r.Id,
                        Content = r.Content,
                        CreatedAt = r.CreatedAt,
                        UserId = r.UserId,
                        Username = r.User.Username,
                        UserAvatarUrl = r.User.AvatarUrl,
                        ParentCommentId = r.ParentCommentId
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(comments);
    }

    // POST /api/posts/5/comments -> requiere JWT. Crea comentario raíz o respuesta.
    [Authorize]
    [HttpPost("{id}/comments")]
    public async Task<ActionResult<CommentDto>> AddComment(int id, CreateCommentDto dto)
    {
        var userId = GetUserId();

        var ownerId = await _db.Posts.Where(p => p.Id == id)
            .Select(p => (int?)p.UserId).FirstOrDefaultAsync();
        if (ownerId is null) return NotFound(new { message = "Post no encontrado" });

        int? parentId = dto.ParentCommentId;
        int? parentAuthorId = null;
        if (parentId is not null)
        {
            // El padre debe existir y pertenecer a ESTE post (no vale responder
            // a un comentario de otra publicación).
            var parent = await _db.Comments
                .FirstOrDefaultAsync(c => c.Id == parentId && c.PostId == id);
            if (parent is null)
                return BadRequest(new { message = "El comentario al que respondes no existe en este post" });

            parentAuthorId = parent.UserId;

            // Regla de 1 nivel: si respondes a una respuesta, aplanamos.
            // La nueva respuesta cuelga del comentario raíz, no de la intermedia.
            parentId = parent.ParentCommentId ?? parent.Id;
        }

        var comment = new Comment
        {
            Content = dto.Content,
            PostId = id,
            UserId = userId,
            ParentCommentId = parentId
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        // Notificamos al dueño del post. Si es una respuesta, también al autor
        // del comentario padre (CreateAsync ignora el caso de notificarse a uno mismo).
        await _notifications.CreateAsync(ownerId.Value, userId, NotificationType.Comment, id);
        if (parentAuthorId is not null && parentAuthorId != ownerId)
            await _notifications.CreateAsync(parentAuthorId.Value, userId, NotificationType.Comment, id);

        // Cargamos el autor para poder devolver Username/Avatar en la respuesta.
        await _db.Entry(comment).Reference(c => c.User).LoadAsync();

        var result = new CommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UserId = comment.UserId,
            Username = comment.User.Username,
            UserAvatarUrl = comment.User.AvatarUrl,
            ParentCommentId = comment.ParentCommentId
        };

        return CreatedAtAction(nameof(GetComments), new { id }, result);
    }

    // Extrae #palabra del contenido y crea los vínculos Post <-> Hashtag,
    // reutilizando hashtags que ya existan (no duplica).
    private static readonly Regex HashtagRegex = new(@"#(\w+)", RegexOptions.Compiled);

    private async Task SyncHashtagsAsync(Post post)
    {
        var names = HashtagRegex.Matches(post.Content)
            .Select(m => m.Groups[1].Value.ToLowerInvariant())
            .Distinct()
            .ToList();

        if (names.Count == 0) return;

        var existing = await _db.Hashtags.Where(h => names.Contains(h.Name)).ToListAsync();
        var existingNames = existing.Select(h => h.Name).ToHashSet();

        var toCreate = names
            .Where(n => !existingNames.Contains(n))
            .Select(n => new Hashtag { Name = n })
            .ToList();

        if (toCreate.Count > 0)
        {
            _db.Hashtags.AddRange(toCreate);
            await _db.SaveChangesAsync();
        }

        foreach (var hashtag in existing.Concat(toCreate))
            _db.PostHashtags.Add(new PostHashtag { PostId = post.Id, HashtagId = hashtag.Id });

        await _db.SaveChangesAsync();
    }

    private int GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.Parse(sub!);
    }

    // Igual que GetUserId pero tolera peticiones anónimas (endpoints públicos):
    // devuelve 0 si nadie está autenticado, valor que nunca coincide con un Id real.
    private int GetCurrentUserIdOrZero()
    {
        return User.Identity?.IsAuthenticated == true ? GetUserId() : 0;
    }
}
