using ConnectHub.API.Data;
using ConnectHub.API.DTOs;
using ConnectHub.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConnectHub.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public NotificationsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET /api/notifications -> mis notificaciones, más recientes primero
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetMine()
    {
        if (User.GetUserId() is not { } userId) return Unauthorized();

        var notifications = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type.ToString(),
                FromUserId = n.FromUserId,
                FromUsername = n.FromUser.Username,
                FromUserAvatarUrl = n.FromUser.AvatarUrl,
                PostId = n.PostId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return Ok(notifications);
    }

    // GET /api/notifications/unread-count -> cuántas sin leer (para el badge)
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> UnreadCount()
    {
        if (User.GetUserId() is not { } userId) return Unauthorized();
        var count = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        return Ok(count);
    }

    // POST /api/notifications/5/read -> marcar una como leída
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        if (User.GetUserId() is not { } userId) return Unauthorized();
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notification is null) return NotFound();

        notification.IsRead = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/notifications/read-all -> marcar todas como leídas
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        if (User.GetUserId() is not { } userId) return Unauthorized();
        await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        return NoContent();
    }
}
