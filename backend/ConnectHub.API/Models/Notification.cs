namespace ConnectHub.API.Models;

public enum NotificationType
{
    Like,
    Comment,
    Follow
}

public class Notification
{
    public int Id { get; set; }

    // Destinatario de la notificación (a quién le llega)
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public NotificationType Type { get; set; }

    // Quién originó la acción (quién dio like / comentó / siguió)
    public int FromUserId { get; set; }
    public User FromUser { get; set; } = null!;

    // Post relacionado (null en notificaciones de Follow)
    public int? PostId { get; set; }
    public Post? Post { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
