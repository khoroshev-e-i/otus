using Api.Exceptions;
using Microsoft.AspNetCore.Mvc;
using InvalidDataException = System.IO.InvalidDataException;

namespace Api;

public class IdentityController(UserService _userService) : ControllerBase
{
    [HttpPost("user/register")]
    public async Task<IActionResult> SignOn([FromBody] RegisterUserRequest request)
        => await ProcessResult(async () => await _userService.RegisterUser(request));
    [HttpPost("login")]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
        => await ProcessResult(async () => await _userService.SignIn(request));
    [HttpGet("user/get/{username}")]
    public async Task<IActionResult> GetUser(string username)
        => await ProcessResult(async () => await _userService.GetUser(username));
    
    [HttpGet("user/search")]
    public async Task<IActionResult> SearchUsers(string first_name, string secondName)
        => await ProcessResult(async () => await _userService.SearchUsers(first_name, secondName));

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

public class RegisterUserRequest
{
    public string username { get; set; }
    public string? first_name { get; set; }
    public string? second_name { get; set; }
    public string? birthdate { get; set; }
    public string? biography { get; set; }
    public string? city { get; set; }
    public string password { get; set; } = string.Empty;
}

public record SignInRequest(string username, string password);