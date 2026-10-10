using System.Text;
using FixFlow.API.Realtime;
using FixFlow.Application.Interfaces;
using FixFlow.Infrastructure;
using FixFlow.Infrastructure.Automation;
using FixFlow.Infrastructure.Identity;
using FixFlow.Infrastructure.Storage;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

// ---------- Seed: Roles + Admin ----------
using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

// ---------- Pipeline ----------
if (app.Environment.IsDevelopment())
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

// لوحة Hangfire: متاحة من نفس الجهاز بس (localhost)
app.UseHangfireDashboard("/hangfire");

app.MapControllers();
app.MapHub<UpdatesHub>("/hubs/updates");

// تسجيل المهام الدورية (إعادة المحاولة، التصعيد، التقرير اليومي)
RecurringJobsRegistrar.Register(app.Services);

app.Run();