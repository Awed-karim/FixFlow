using FixFlow.Application.Interfaces;
using FixFlow.Infrastructure.Identity;
using FixFlow.Infrastructure.Persistence;
using FixFlow.Infrastructure.Services;
using FixFlow.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // قاعدة البيانات
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Identity (المستخدمين والأدوار)
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
        })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        // الإعدادات
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<FileStorageSettings>(configuration.GetSection("FileStorage"));

        // JWT
        services.AddScoped<JwtTokenGenerator>();

        // الخدمات
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITechnicianService, TechnicianService>();
        services.AddScoped<IServiceRequestService, ServiceRequestService>();

        // المرحلة 4
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<IRequestWorkflowService, RequestWorkflowService>();
        services.AddScoped<IFileStorage, LocalFileStorage>();

        return services;
    }
}