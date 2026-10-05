using FixFlow.Application.Auth;
using FixFlow.Application.Common;
using FixFlow.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace FixFlow.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenGenerator _jwtTokenGenerator;

    public AuthService(UserManager<ApplicationUser> userManager, JwtTokenGenerator jwtTokenGenerator)
    {
        _userManager = userManager;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        if (request.Role != AppRoles.Customer && request.Role != AppRoles.Technician)
            return AuthResult.Failure(new[] { "Role must be Customer or Technician." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return AuthResult.Failure(createResult.Errors.Select(e => e.Description));

        await _userManager.AddToRoleAsync(user, request.Role);

        return AuthResult.Success(BuildResponse(user, request.Role));
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return AuthResult.Failure(new[] { "Invalid email or password." });

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? AppRoles.Customer;

        return AuthResult.Success(BuildResponse(user, role));
    }

    private AuthResponse BuildResponse(ApplicationUser user, string role)
    {
        var (token, expiresAt) = _jwtTokenGenerator.Generate(user, role);

        return new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = role,
            Token = token,
            ExpiresAt = expiresAt
        };
    }
}