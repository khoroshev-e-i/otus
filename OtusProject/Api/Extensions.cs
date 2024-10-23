using Microsoft.Extensions.Primitives;

namespace Api;

public static class Extensions
{
    private static string sessionKey = "session_id";

    public static string? GetUserId( this HttpContext context)
    {
        StringValues sessionId;
        string userId;
        if (!context.Request.Headers.TryGetValue(sessionKey, out sessionId) || !Sessions.Active.TryGetValue(sessionId, out userId) )
        {
            return null;
        }

        return userId;
    }

}