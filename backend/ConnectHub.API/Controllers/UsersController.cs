using System.Security.Claims;
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
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly FileStorageService _files;
    private readonly NotificationService _notifications;

    public UsersController(ApplicationDbContext db, FileStorageService files, NotificationService notifications)
    {
        _db = db;
        _files = files;
        _notifications = notifications;
    }

    // GET /api/users/5 -> perfil público con contadores
    [HttpGet("{id}")]
    public async Task<ActionResult<UserProfileDto>> GetProfile(int id)
    {
        var currentUserId = GetCurrentUserIdOrZero();

        var profile = await _db.Users
            .Where(u => u.Id == id)
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                Username = u.Username,
                Bio = u.Bio,
                AvatarUrl = u.AvatarUrl,
                CreatedAt = u.CreatedAt,
                PostsCount = u.Posts.Count,
                FollowersCount = u.Followers.Count,
                FollowingCount = u.Following.Count,
                // ¿Existe un Follow donde yo (currentUser) sigo a este perfil?
                IsFollowedByCurrentUser = u.Followers.Any(f => f.FollowerId == currentUserId)
            })
            .FirstOrDefaultAsync();

        if (profile is null) return NotFound(new { message = "Usuario no encontrado" });
        return Ok(profile);
    }

    // GET /api/users/5/posts -> posts de ese usuario
    [HttpGet("{id}/posts")]
    public async Task<ActionResult<IEnumerable<PostDto>>> GetUserPosts(int id)
    {
        var currentUserId = GetCurrentUserIdOrZero();

        var posts = await _db.Posts
            .Where(p => p.UserId == id)
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

    // POST /api/users/5/follow -> seguir (requiere JWT)
    [Authorize]
    [HttpPost("{id}/follow")]
    public async Task<IActionResult> Follow(int id)
    {
        var userId = GetUserId();

        if (userId == id)
            return BadRequest(new { message = "No puedes seguirte a ti mismo" });

        var targetExists = await _db.Users.AnyAsync(u => u.Id == id);
        if (!targetExists) return NotFound(new { message = "Usuario no encontrado" });

        var already = await _db.Follows.AnyAsync(f => f.FollowerId == userId && f.FollowedId == id);
        if (already) return BadRequest(new { message = "Ya sigues a este usuario" });

        _db.Follows.Add(new Follow { FollowerId = userId, FollowedId = id });
        await _db.SaveChangesAsync();

        await _notifications.CreateAsync(id, userId, NotificationType.Follow, null);

        var followersCount = await _db.Follows.CountAsync(f => f.FollowedId == id);
        return Ok(new { following = true, followersCount });
    }

    // DELETE /api/users/5/follow -> dejar de seguir (requiere JWT)
    [Authorize]
    [HttpDelete("{id}/follow")]
    public async Task<IActionResult> Unfollow(int id)
    {
        var userId = GetUserId();

        var follow = await _db.Follows.FirstOrDefaultAsync(f => f.FollowerId == userId && f.FollowedId == id);
        if (follow is null) return NotFound(new { message = "No sigues a este usuario" });

        _db.Follows.Remove(follow);
        await _db.SaveChangesAsync();

        var followersCount = await _db.Follows.CountAsync(f => f.FollowedId == id);
        return Ok(new { following = false, followersCount });
    }

    // PUT /api/users/me -> actualizar mi bio (requiere JWT)
    [Authorize]
    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(UpdateProfileDto dto)
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        user.Bio = dto.Bio;
        await _db.SaveChangesAsync();

        return Ok(new { user.Id, user.Username, user.Bio, user.AvatarUrl });
    }

    // POST /api/users/me/avatar -> subir foto de perfil (requiere JWT)
    [Authorize]
    [HttpPost("me/avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        try
        {
            var url = await _files.SaveImageAsync(file, "avatars");
            user.AvatarUrl = url;
            await _db.SaveChangesAsync();
            return Ok(new { avatarUrl = url });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private int GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.Parse(sub!);
    }

    private int GetCurrentUserIdOrZero()
    {
        return User.Identity?.IsAuthenticated == true ? GetUserId() : 0;
    }
}
