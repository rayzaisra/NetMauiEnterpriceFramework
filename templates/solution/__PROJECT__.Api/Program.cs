using System.Text;

using AspNetCoreRateLimit;

using Hangfire;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using QuestPDF.Infrastructure;

using Serilog;

using __PROJECT__.Api.Hubs;
using __PROJECT__.Api.Services;
using __PROJECT__.Application.Interfaces;
using __PROJECT__.Application.Interfaces.Queries;
using __PROJECT__.Application.Interfaces.Repositories;
using __PROJECT__.Application.Services;
using __PROJECT__.Infrastructure;
using __PROJECT__.Infrastructure.Data;
using __PROJECT__.Infrastructure.Identity;
using __PROJECT__.Infrastructure.Options;
using __PROJECT__.Infrastructure.Queries;
using __PROJECT__.Infrastructure.Repositories.Auth;
using __PROJECT__.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);
var seedOnly = args.Contains("--seed-only", StringComparer.OrdinalIgnoreCase);
QuestPDF.Settings.License = LicenseType.Community;

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "__PROJECT__ API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter JWT with Bearer prefix",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
    });
});

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
var auditConnection = builder.Configuration.GetConnectionString("AuditLogConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:AuditLogConnection is not configured.");

builder.Services.AddDbContext<__PROJECT__DbContext>(options => options.UseSqlServer(defaultConnection));
builder.Services.AddDbContext<AuditLogDbContext>(options => options.UseSqlServer(auditConnection));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUmUserAppRepository, UmUserAppRepository>();
builder.Services.AddScoped<IUmUserGroupRepository, UmUserGroupRepository>();
builder.Services.AddScoped<IUmUserAppFlagRepository, UmUserAppFlagRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IMenuManagementService, MenuManagementService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IGroupManagementService, GroupManagementService>();
builder.Services.AddScoped<IRoleManagementService, RoleManagementService>();
builder.Services.AddScoped<IGroupQueryService, GroupQueryService>();
builder.Services.AddScoped<IRoleQueryService, RoleQueryService>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();
builder.Services.AddScoped<IMenuQueryService, MenuQueryService>();
builder.Services.AddScoped<IScreenMenuQueryService, ScreenMenuQueryService>();
builder.Services.AddScoped<IApprovalQueryService, ApprovalQueryService>();
builder.Services.AddScoped<INotificationQueryService, NotificationQueryService>();
builder.Services.AddScoped<IEmailProviderQueryService, EmailProviderQueryService>();
builder.Services.AddScoped<IParameterStagingService, ParameterStagingService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<IParameterEngineService, ParameterEngineService>();
builder.Services.AddScoped<IDynamicEngineService, DynamicEngineService>();
builder.Services.AddScoped<IScreenMenuEditorService, ScreenMenuEditorService>();
builder.Services.AddScoped<IUserSessionNotifier, UserSessionNotifier>();
builder.Services.AddScoped<IErrorLogService, ErrorLogService>();
builder.Services.Configure<ApiActivityLogOptions>(
    builder.Configuration.GetSection(ApiActivityLogOptions.SectionName));
builder.Services.AddSingleton<ApiActivityLogQueue>();
builder.Services.AddSingleton<IApiActivityLogQueue>(
    services => services.GetRequiredService<ApiActivityLogQueue>());
builder.Services.AddHostedService<ApiActivityLogWriter>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IEmailProviderService, EmailProviderService>();
builder.Services.AddScoped<EmailOutboxProcessor>();
builder.Services.AddScoped<INotificationPusher, SignalRNotificationPusher>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ExcelExportJob>();
builder.Services.AddScoped<PdfExportJob>();
builder.Services.AddScoped<ManagementExportJob>();
builder.Services.AddScoped<IDropDownService, DropDownQueryService>();
builder.Services.AddScoped<IJWTService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasherService, PasswordHasherService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
var jwtKey = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

builder.Services.AddHangfire(config => config.UseSqlServerStorage(defaultConnection));
builder.Services.AddHangfireServer();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AppClient", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy
                .SetIsOriginAllowed(origin =>
                {
                    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                    {
                        return false;
                    }

                    return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                        || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                        || uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase);
                })
                .AllowCredentials()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowCredentials().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || seedOnly)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<__PROJECT__DbContext>();
    var auditDb = services.GetRequiredService<AuditLogDbContext>();

    await db.Database.MigrateAsync();
    await auditDb.Database.MigrateAsync();
    await SynonymSeeder.EnsureAsync(db, auditConnection);

    var seedEnabled = bool.TryParse(builder.Configuration["Seed:Enabled"], out var enabled) && enabled;
    if (seedEnabled)
    {
        var passwordHasher = services.GetRequiredService<IPasswordHasherService>();
        await DbInitializer.SeedAsync(
            db,
            passwordHasher,
            builder.Configuration["Seed:AdminUser"] ?? "admin",
            builder.Configuration["Seed:AdminPassword"] ?? "ChangeMe#12345",
            builder.Configuration["Seed:AdminName"] ?? "Administrator",
            builder.Configuration["Seed:AdminEmail"] ?? "admin@example.com");
    }

    if (seedOnly)
    {
        return;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AppClient");
app.UseAuthentication();
app.UseMiddleware<ApiActivityMiddleware>();
app.UseIpRateLimiting();
app.UseMiddleware<ActiveSessionMiddleware>();
app.UseAuthorization();
app.UseWebSockets();

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapHub<UserHub>("/userHub");
app.MapControllers();
var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobs.AddOrUpdate<EmailOutboxProcessor>(
    "email-outbox",
    job => job.ProcessPendingAsync(),
    "*/1 * * * *");
app.Run();

public partial class Program
{
}
