namespace ConnectHub.API.Models;

public class Like
{
    public int Id { get; set; }

    // FK al post likeado
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    // FK al usuario que da el like
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
