using System.ComponentModel.DataAnnotations;

namespace ConnectHub.API.Models;

public class Comment
{
    public int Id { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // FK al post comentado
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    // FK al autor del comentario
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Auto-referencia: si es null -> comentario raíz.
    // Si tiene valor -> es una respuesta a otro comentario (el "padre").
    public int? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }

    // Navegación inversa: las respuestas que cuelgan de este comentario.
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}
