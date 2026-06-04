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
        var posts = await _db.Posts
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PostDto
            {
                Id = p.Id,
                Content = p.Content,
                ImageUrl = p.ImageUrl,
                CreatedAt = p.CreatedAt,
                UserId = p.UserId,
                Username = p.User.Username,
                UserAvatarUrl = p.User.AvatarUrl
            })
            .ToListAsync();

        return Ok(posts);
    }

    // GET /api/posts/5
    [HttpGet("{id}")]
    public async Task<ActionResult<PostDto>> GetById(int id)
    {
        var post = await _db.Posts
            .Include(p => p.User)
            .Where(p => p.Id == id)
            .Select(p => new PostDto
            {
                Id = p.Id,
                Content = p.Content,
                ImageUrl = p.ImageUrl,
                CreatedAt = p.CreatedAt,
                UserId = p.UserId,
                Username = p.User.Username,
                UserAvatarUrl = p.User.AvatarUrl
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
            UserAvatarUrl = post.User.AvatarUrl
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

    private int GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.Parse(sub!);
    }
}
