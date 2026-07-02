using System.Security.Claims;
using ConnectHub.API.Data;
using ConnectHub.API.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

namespace ConnectHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public SearchController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET /api/search?q=...  -> busca en posts (contenido + hashtags) y en usuarios.
    // Si q empieza por '#', busca solo posts por ese hashtag.
    [HttpGet]
    public async Task<ActionResult<SearchResultDto>> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new SearchResultDto());

        var currentUserId = GetCurrentUserIdOrZero();
        var term = q.Trim();
        var isHashtag = term.StartsWith('#');
        var tag = (isHashtag ? term[1..] : term).ToLower();

        // Posts: por hashtag exacto, o por contenido/hashtag si es texto libre.
        var postsQuery = isHashtag
            ? _db.Posts.Where(p => p.PostHashtags.Any(ph => ph.Hashtag.Name == tag))
            : _db.Posts.Where(p => p.Content.Contains(term)
                                   || p.PostHashtags.Any(ph => ph.Hashtag.Name == tag));

        var posts = await postsQuery
            .OrderByDescending(p => p.CreatedAt)
            .Take(30)
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

        // Usuarios: solo si la búsqueda es texto libre (no tiene sentido con #).
        var users = isHashtag
            ? new List<UserSummaryDto>()
            : await _db.Users
                .Where(u => u.Username.Contains(term))
                .Take(20)
                .Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    AvatarUrl = u.AvatarUrl,
                    Bio = u.Bio
                })
                .ToListAsync();

        return Ok(new SearchResultDto { Posts = posts, Users = users });
    }

    private int GetCurrentUserIdOrZero()
    {
        if (User.Identity?.IsAuthenticated != true) return 0;
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.Parse(sub!);
    }
}
