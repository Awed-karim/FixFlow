using System.Text;
using FixFlow.API.Realtime;
using FixFlow.Application.Interfaces;
using FixFlow.Infrastructure;
using FixFlow.Infrastructure.Automation;
using FixFlow.Infrastructure.Identity;
using FixFlow.Infrastructure.Persistence;
using FixFlow.Infrastructure.Storage;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------- Infrastructure (DB + Identity + Services) ----------
builder.Services.AddInfrastructure(builder.Configuration);

// مسار الرفع يبقى جوه فولدر مشروع الـ API (مش بيعتمد على مكان التشغيل)
builder.Services.PostConfigure<FileStorageSettings>(settings =>
{
    settings.RootPath = Path.Combine(builder.Environment.ContentRootPath, settings.RootPath);
});

// ---------- JWT Authentication ----------
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt section is missing in appsettings.json");

// حماية: خارج التطوير ممنوع مفتاح ضعيف أو مفتاح المثال
if (!builder.Environment.IsDevelopment() &&
    (jwtSettings.Key.Length < 32 ||
     jwtSettings.Key.Contains("Dev-Secret") ||
     jwtSettings.Key.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase)))
{
    throw new InvalidOperationException(
        "Jwt:Key is weak or still a sample value. Set a strong random secret (32+ chars) in the Jwt__Key environment variable.");
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "name",
            RoleClaimType = "role"
        };

        // SignalR (WebSocket) مش بيقدر يبعت Header، فبيبعت الـ Token في الـ query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ---------- SignalR (م5) ----------
builder.Services.AddSignalR();
builder.Services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();

// ---------- Hangfire (م5): المهام الخلفية، مخزنة في نفس قاعدة البيانات ----------
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer(options =>
{
    options.SchedulePollingInterval = TimeSpan.FromSeconds(5);
});

// ---------- Controllers + Swagger ----------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "FixFlow API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "الصق الـ Token هنا (من غير كلمة Bearer)"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ---------- Database (م7): تطبيق الـ Migrations + Seed ----------
// في Docker الداتابيز بتبدأ فاضية، فالـ API هو اللي يبنيها.
// الـ retry لأن SQL Server ممكن يكون لسه بيصحى.
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (attempt < 10)
        {
            logger.LogWarning(ex, "Database is not ready (attempt {Attempt}/10). Retrying in 5 seconds...", attempt);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

// ---------- Pipeline ----------
// Swagger: في Development، أو لو الإعداد Swagger:Enabled = true
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ملفات wwwroot (صفحة اختبار SignalR)
app.UseStaticFiles();

// تقديم الصور المرفوعة على الرابط /uploads/...
var storageSettings = app.Services.GetRequiredService<IOptions<FileStorageSettings>>().Value;
Directory.CreateDirectory(storageSettings.RootPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storageSettings.RootPath),
    RequestPath = storageSettings.RequestPath
});

app.UseAuthentication();   // لازم قبل UseAuthorization
app.UseAuthorization();

// لوحة Hangfire: في Development بس (من نفس الجهاز). في Docker/Production مقفولة.
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

app.MapControllers();
app.MapHub<UpdatesHub>("/hubs/updates");

// فحص الصحة: بنستخدمه بعد النشر للتأكد إن الـ API والداتابيز شغالين
app.MapGet("/api/health", async (AppDbContext db) =>
{
    var ok = await db.Database.CanConnectAsync();
    return ok ? Results.Ok(new { status = "healthy" }) : Results.StatusCode(503);
});

// تسجيل المهام الدورية (إعادة المحاولة، التصعيد، التقرير اليومي)
RecurringJobsRegistrar.Register(app.Services);

app.Run();