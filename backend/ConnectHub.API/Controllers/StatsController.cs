using ConnectHub.API.Data;
using ConnectHub.API.DTOs;
using ConnectHub.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConnectHub.API.Controllers;

[Authorize]
[ApiController]
[Route("api/me/stats")]
public class StatsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private const int Days = 30;

    public StatsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET /api/me/stats/posts-per-day -> mis posts por día (últimos 30 días, con ceros)
    [HttpGet("posts-per-day")]
    public async Task<ActionResult<IEnumerable<DailyCountDto>>> PostsPerDay()
    {
        if (User.GetUserId() is not { } userId) return Unauthorized();
        var since = DateTime.UtcNow.Date.AddDays(-(Days - 1));

        // GroupBy por fecha: EF lo traduce a CAST(CreatedAt AS date) + GROUP BY.
        var raw = await _db.Posts
            .Where(p => p.UserId == userId && p.CreatedAt >= since)
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(FillDays(raw.ToDictionary(x => x.Date, x => x.Count), since));
    }

    // GET /api/me/stats/likes-received -> likes recibidos por día en mis posts
    [HttpGet("likes-received")]
    public async Task<ActionResult<IEnumerable<DailyCountDto>>> LikesReceived()
    {
        if (User.GetUserId() is not { } userId) return Unauthorized();
        var since = DateTime.UtcNow.Date.AddDays(-(Days - 1));

        var raw = await _db.Likes
            .Where(l => l.Post.UserId == userId && l.CreatedAt >= since)
            .GroupBy(l => l.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(FillDays(raw.ToDictionary(x => x.Date, x => x.Count), since));
    }

    // GET /api/me/stats/followers-growth -> seguidores acumulados por día
    [HttpGet("followers-growth")]
    public async Task<ActionResult<IEnumerable<DailyCountDto>>> FollowersGrowth()
    {
        if (User.GetUserId() is not { } userId) return Unauthorized();
        var since = DateTime.UtcNow.Date.AddDays(-(Days - 1));

        // Seguidores que ya tenía antes de la ventana (línea base).
        var baseline = await _db.Follows.CountAsync(f => f.FollowedId == userId && f.CreatedAt < since);

        var perDay = await _db.Follows
            .Where(f => f.FollowedId == userId && f.CreatedAt >= since)
            .GroupBy(f => f.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var map = perDay.ToDictionary(x => x.Date, x => x.Count);

        // Acumulado: cada día suma los nuevos seguidores al total previo.
        var result = new List<DailyCountDto>();
        var running = baseline;
        for (var i = 0; i < Days; i++)
        {
            var day = since.AddDays(i);
            running += map.GetValueOrDefault(day, 0);
            result.Add(new DailyCountDto { Date = day, Count = running });
        }

        return Ok(result);
    }

    // GET /api/me/stats/top-posts -> mis 5 posts con más interacción
    [HttpGet("top-posts")]
    public async Task<ActionResult<IEnumerable<TopPostDto>>> TopPosts()
    {
        if (User.GetUserId() is not { } userId) return Unauthorized();

        var top = await _db.Posts
            .Where(p => p.UserId == userId)
            .Select(p => new TopPostDto
            {
                Id = p.Id,
                Content = p.Content.Length > 80 ? p.Content.Substring(0, 80) + "..." : p.Content,
                LikesCount = p.Likes.Count,
                CommentsCount = p.Comments.Count
            })
            .OrderByDescending(t => t.LikesCount + t.CommentsCount)
            .Take(5)
            .ToListAsync();

        return Ok(top);
    }

    // GET /api/me/stats/engagement-rate -> resumen de interacción
    [HttpGet("engagement-rate")]
    public async Task<ActionResult<EngagementDto>> EngagementRate()
    {
        if (User.GetUserId() is not { } userId) return Unauthorized();

        var postsCount = await _db.Posts.CountAsync(p => p.UserId == userId);
        var likesReceived = await _db.Likes.CountAsync(l => l.Post.UserId == userId);
        var commentsReceived = await _db.Comments.CountAsync(c => c.Post.UserId == userId);
        var followersCount = await _db.Follows.CountAsync(f => f.FollowedId == userId);

        var rate = postsCount == 0
            ? 0
            : Math.Round((double)(likesReceived + commentsReceived) / postsCount, 2);

        return Ok(new EngagementDto
        {
            PostsCount = postsCount,
            LikesReceived = likesReceived,
            CommentsReceived = commentsReceived,
            FollowersCount = followersCount,
            EngagementRate = rate
        });
    }

    // Rellena los días sin datos con 0, para que el gráfico tenga 30 puntos continuos.
    private static List<DailyCountDto> FillDays(Dictionary<DateTime, int> map, DateTime since)
    {
        var result = new List<DailyCountDto>();
        for (var i = 0; i < Days; i++)
        {
            var day = since.AddDays(i);
            result.Add(new DailyCountDto { Date = day, Count = map.GetValueOrDefault(day, 0) });
        }
        return result;
    }
}
