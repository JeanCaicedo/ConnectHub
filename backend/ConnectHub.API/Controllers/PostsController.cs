using System.Security.Claims;
using ConnectHub.API.Data;
using ConnectHub.API.DTOs;
using ConnectHub.API.Models;
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

    public PostsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET /api/posts -> público, todos los posts
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PostDto>>> GetAll()
    {
        // Si hay alguien logueado, lo usamos para marcar IsLikedByCurrentUser.
        // Si no (endpoint público), queda en 0 y nunca coincide con un UserId real.
        var currentUserId = GetCurrentUserIdOrZero();

        var posts = await _db.Posts
            .OrderByDescending(p => p.CreatedAt)
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

        return Ok(posts);
    }

    // GET /api/posts/feed -> solo posts de los usuarios que sigo (requiere JWT).
    // Se define ANTES que "{id:int}" y con segmento literal "feed" para que el router
    // no lo confunda con un id.
    [Authorize]
    [HttpGet("feed")]
    public async Task<ActionResult<IEnumerable<PostDto>>> GetFeed()
    {
        var userId = GetUserId();

        // IDs de los usuarios que sigo. Lo dejamos como IQueryable (subconsulta)
        // para que el filtro se traduzca a un solo SQL con un IN (...).
        var followedIds = _db.Follows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowedId);

        var posts = await _db.Posts
            .Where(p => followedIds.Contains(p.UserId))
            .OrderByDescending(p => p.CreatedAt)
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
                IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == userId),
                CommentsCount = p.Comments.Count
            })
            .ToListAsync();

        return Ok(posts);
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

        var postExists = await _db.Posts.AnyAsync(p => p.Id == id);
        if (!postExists) return NotFound(new { message = "Post no encontrado" });

        var alreadyLiked = await _db.Likes.AnyAsync(l => l.PostId == id && l.UserId == userId);
        if (alreadyLiked) return BadRequest(new { message = "Ya diste like a este post" });

        _db.Likes.Add(new Like { PostId = id, UserId = userId });
        await _db.SaveChangesAsync();

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

        var postExists = await _db.Posts.AnyAsync(p => p.Id == id);
        if (!postExists) return NotFound(new { message = "Post no encontrado" });

        int? parentId = dto.ParentCommentId;
        if (parentId is not null)
        {
            // El padre debe existir y pertenecer a ESTE post (no vale responder
            // a un comentario de otra publicación).
            var parent = await _db.Comments
                .FirstOrDefaultAsync(c => c.Id == parentId && c.PostId == id);
            if (parent is null)
                return BadRequest(new { message = "El comentario al que respondes no existe en este post" });

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
