namespace FixFlow.Application.Auth;

public class AuthResult
{
    public bool Succeeded { get; private set; }
    public AuthResponse? Data { get; private set; }
    public List<string> Errors { get; private set; } = new();

    public static AuthResult Success(AuthResponse data) =>
        new() { Succeeded = true, Data = data };

    public static AuthResult Failure(IEnumerable<string> errors) =>
        new() { Succeeded = false, Errors = errors.ToList() };
}