using Api.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using InvalidDataException = System.IO.InvalidDataException;

namespace Api;

[Route("friend")]
public class FriendsController(UserService _userService) : ControllerBase
{
    private static string sessionKey = "session_id";

    [HttpPut("set/{user_id}")]
    public async Task<IActionResult> AddFriend(string user_id)
    {
        var userId = GetUser();
        if (userId is null)
        {
            return Unauthorized("Пользователь не авторизован");
        }

        return await ProcessResult(async () => await _userService.AddFriend(user_id, userId));
    }

    [HttpPut("delete/{friendId}")]
    public async Task<IActionResult> DeleteFriend(string friendId)
    {
        var userId = GetUser();
        if (userId is null)
        {
            return Unauthorized("Пользователь не авторизован");
        }

        return await ProcessResult(async () => await _userService.DeleteFriend(userId, friendId));
    }

    private string? GetUser()
    {
        StringValues sessionId;
        string userId;
        if (!HttpContext.Request.Headers.TryGetValue(sessionKey, out sessionId) || !Sessions.Active.TryGetValue(sessionId, out userId) )
        {
            return null;
        }

        return userId;
    }
    
    private async Task<IActionResult> ProcessResult<TResult>(Func<Task<TResult>> func)
    {
        try
        {
            return Ok(await func());
        }
        catch (InvalidDataException e)
        {
            return BadRequest(e.Message);
        }
        catch (UserNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}