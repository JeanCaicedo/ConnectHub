using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace ConnectHub.API.Helpers;

// Centraliza la lectura del userId desde los claims del JWT.
// Antes cada controller tenía su propia copia con int.Parse(sub!), que lanzaba
// excepción (500) si el claim faltaba o no era numérico. Con TryParse el caller
// decide qué responder (típicamente 401 Unauthorized).
public static class ClaimsPrincipalExtensions
{
    // Devuelve el userId del claim 'sub' (o NameIdentifier, su equivalente
    // mapeado por ASP.NET), o null si no hay claim válido.
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(sub, out var id) ? id : null;
    }

    // Para endpoints públicos que personalizan la respuesta si hay sesión:
    // devuelve 0 si no hay usuario autenticado, valor que nunca coincide
    // con un Id real (las identities de SQL Server empiezan en 1).
    public static int GetUserIdOrZero(this ClaimsPrincipal user)
        => user.Identity?.IsAuthenticated == true ? user.GetUserId() ?? 0 : 0;
}
