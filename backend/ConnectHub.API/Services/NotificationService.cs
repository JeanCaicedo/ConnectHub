using ConnectHub.API.Data;
using ConnectHub.API.DTOs;
using ConnectHub.API.Hubs;
using ConnectHub.API.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ConnectHub.API.Services;

// Crea notificaciones en la BD y las empuja en tiempo real al destinatario.
public class NotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationService(ApplicationDbContext db, IHubContext<NotificationHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task CreateAsync(int recipientId, int fromUserId, NotificationType type, int? postId)
    {
        // Nadie se notifica a sí mismo (darte like a tu propio post, etc.).
        if (recipientId == fromUserId) return;

        var notification = new Notification
        {
            UserId = recipientId,
            FromUserId = fromUserId,
            Type = type,
            PostId = postId
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        // Datos del autor para pintar la notificación sin otra petición.
        var fromUser = await _db.Users
            .Where(u => u.Id == fromUserId)
            .Select(u => new { u.Username, u.AvatarUrl })
            .FirstOrDefaultAsync();

        var dto = new NotificationDto
        {
            Id = notification.Id,
            Type = type.ToString(),
            FromUserId = fromUserId,
            FromUsername = fromUser?.Username ?? string.Empty,
            FromUserAvatarUrl = fromUser?.AvatarUrl,
            PostId = postId,
            IsRead = false,
            CreatedAt = notification.CreatedAt
        };

        // Empuje en vivo: solo a las conexiones del destinatario.
        // Si no está conectado, no pasa nada: la verá al recargar (está en BD).
        await _hub.Clients.User(recipientId.ToString())
            .SendAsync("ReceiveNotification", dto);
    }
}
