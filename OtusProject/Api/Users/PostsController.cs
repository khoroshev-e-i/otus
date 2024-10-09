using Api.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Api;

[ApiController]
[Route("post")]
public class PostsController(UserService _userService) : ControllerBase
{
    private static string sessionKey = "session_id";

    [HttpPut("create")]
    public async Task<IActionResult> Create([FromBody] PostBodyDto body)
    {
        var userId = GetUser();
        if (userId is null)
        {
            return Unauthorized("Пользователь не авторизован");
        }

        return Ok(await ProcessResult(async () => await _userService.AddPost(body.text, userId)));
    }


    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] PostUpdateBodyDto body)
    {
        var userId = GetUser();
        if (userId is null)
        {
            return Unauthorized("Пользователь не авторизован");
        }

        return Ok(await ProcessResult(async () => await _userService.UpdatePost(body.id, body.text, userId)));
    }

    [HttpPut("delete/{id}")]
    public async Task<IActionResult> DeletePost(string id)
    {
        var userId = GetUser();
        if (userId is null)
        {
            return Unauthorized("Пользователь не авторизован");
        }

        return Ok(await ProcessResult(async () => await _userService.DeletePost(id, userId)));
    }
    
    [HttpGet("feed")]
    public async Task<IActionResult> Feed(int limit= 10, int offset = 0)
    {
        var userId = GetUser();
        if (userId is null)
        {
            return Unauthorized("Пользователь не авторизован");
        }

        return Ok(await ProcessResult(async () => await _userService.Feed(userId, limit, offset)));
    }

    private string? GetUser()
    {
        StringValues sessionId;
        string userId;
        if (!HttpContext.Request.Headers.TryGetValue(sessionKey, out sessionId) ||
            !Sessions.Active.TryGetValue(sessionId, out userId))
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
        catch (InvalidDataExceptiond e)
        {
            return BadRequest(e.Message);
        }
        catch (UserNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}

public record PostBodyDto(string text);
public record PostUpdateBodyDto(string id, string text);