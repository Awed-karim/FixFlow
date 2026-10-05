using System.ComponentModel.DataAnnotations;
using FixFlow.Application.Common;

namespace FixFlow.Application.Auth;

public class RegisterRequest
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    // مسموح Customer أو Technician فقط. الـ Admin بيتعمل من النظام.
    [Required]
    public string Role { get; set; } = AppRoles.Customer;
}