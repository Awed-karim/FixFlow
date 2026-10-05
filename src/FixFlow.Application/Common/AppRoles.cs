namespace FixFlow.Application.Common;

public static class AppRoles
{
    public const string Customer = "Customer";
    public const string Technician = "Technician";
    public const string Admin = "Admin";

    public static readonly string[] All = { Customer, Technician, Admin };
}