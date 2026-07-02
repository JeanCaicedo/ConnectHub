namespace ConnectHub.API.Services;

// Encapsula la validación y el guardado de imágenes en disco.
// Vive en la capa Services para no meter esta lógica en los controllers.
public class FileStorageService
{
    private readonly IWebHostEnvironment _env;

    // Límites y tipos permitidos (no confiar en el cliente: validamos en el servidor).
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };

    public FileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    // Guarda la imagen en wwwroot/uploads/{subfolder} y devuelve la URL relativa
    // (p. ej. /uploads/posts/xxxx.jpg). Lanza ArgumentException si no es válida.
    public async Task<string> SaveImageAsync(IFormFile file, string subfolder)
    {
        if (file is null || file.Length == 0)
            throw new ArgumentException("No se envió ningún archivo.");

        if (file.Length > MaxBytes)
            throw new ArgumentException("La imagen supera el tamaño máximo de 5 MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException("Formato no permitido. Usa jpg, png o webp.");

        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new ArgumentException("Tipo de contenido no permitido.");

        // WebRootPath puede ser null si wwwroot no existe; lo resolvemos y creamos.
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var targetDir = Path.Combine(webRoot, "uploads", subfolder);
        Directory.CreateDirectory(targetDir);

        // Nombre único con GUID para evitar colisiones y adivinación de nombres.
        var fileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(targetDir, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        // URL pública relativa; el frontend le antepone el host del API.
        return $"/uploads/{subfolder}/{fileName}";
    }
}
