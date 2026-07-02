using System.ComponentModel.DataAnnotations;

namespace ConnectHub.API.Models;

public class Hashtag
{
    public int Id { get; set; }

    // Nombre sin la almohadilla y en minúsculas (p. ej. "dotnet"). Único.
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // Navegación a la tabla pivote (N-N con Post).
    public ICollection<PostHashtag> PostHashtags { get; set; } = new List<PostHashtag>();
}

// Tabla pivote de la relación N-N Post <-> Hashtag. Clave compuesta (PostId, HashtagId).
public class PostHashtag
{
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public int HashtagId { get; set; }
    public Hashtag Hashtag { get; set; } = null!;
}
