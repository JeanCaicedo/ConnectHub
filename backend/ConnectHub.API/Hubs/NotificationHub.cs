using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ConnectHub.API.Hubs;

// Hub de notificaciones. [Authorize] exige un JWT válido para conectarse.
// No necesita métodos invocables desde el cliente: el servidor empuja mensajes
// con Clients.User(...), y SignalR agrupa las conexiones por el NameIdentifier
// (el claim 'sub' = UserId) del usuario autenticado.
[Authorize]
public class NotificationHub : Hub
{
}
