using System.ComponentModel.DataAnnotations;

namespace ConnectHub.API.Models;

public class Post
{
    public int Id { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Key
    public int UserId { get; set; }

    // Navegación: cada post pertenece a un usuario
    public User User { get; set; } = null!;

    // Navegación: un post puede recibir muchos likes
    public ICollection<Like> Likes { get; set; } = new List<Like>();
}
