using Api.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using InvalidDataException = System.IO.InvalidDataException;

namespace Api;

[Route("friend")]
public class FriendsController(UserService _userService) : ControllerBase
{

    [HttpPut("set/{user_id}")]
    public async Task<IActionResult> AddFriend(string user_id)
    {
        var userId = HttpContext.GetUserId();
        if (userId is null)
        {
            return Unauthorized("Пользователь не авторизован");
        }

        return await ProcessResult(async () => await _userService.AddFriend(user_id, userId));
    }

    [HttpPut("delete/{friendId}")]
    public async Task<IActionResult> DeleteFriend(string friendId)
    {
        var userId = HttpContext.GetUserId();
        if (userId is null)
        {
            return Unauthorized("Пользователь не авторизован");
        }

        return await ProcessResult(async () => await _userService.DeleteFriend(userId, friendId));
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