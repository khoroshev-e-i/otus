using Api.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api;

[ApiController]
[Route("dialog")]
public class DialogController(UserService _userService) : ControllerBase
{
    [HttpPost("{user_id}/send")]
    public async Task<IActionResult> Send(string user_Id, [FromBody] MessageDto message)
    {
        var currentUserId = HttpContext.GetUserId();
        if (currentUserId is null) return Unauthorized();

        return Ok(await ProcessResult(async () => await _userService.SendMessage(currentUserId, user_Id, message.text)));
    }


    [HttpGet("{user_id}/list")]
    public async Task<IActionResult> List(string user_id)
    {
        var currentUserId = HttpContext.GetUserId();
        if (currentUserId is null) return Unauthorized();

        return Ok(await ProcessResult(async () => await _userService.ListDialog(currentUserId, user_id)));
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

    public record MessageDto(string text);
}