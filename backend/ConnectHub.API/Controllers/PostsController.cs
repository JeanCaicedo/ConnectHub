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
                IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId)
            })
            .ToListAsync();

        return Ok(posts);
    }

    // GET /api/posts/5
    [HttpGet("{id}")]
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
                IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId)
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
