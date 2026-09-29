using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using InvestDashboard.WebAPI.Health;
using System.Threading.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.Threading.Tasks;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using InvestDashboard.Infrastructure.Persistence;
using InvestDashboard.Infrastructure.Persistence.RepositoryImpl;
using InvestDashboard.Domain.Repository;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Infrastructure.Services;
using InvestDashboard.Application.Services;
using InvestDashboard.Infrastructure.Realtime.SignalR;
using InvestDashboard.Infrastructure.BackgroundWorkers;
using InvestDashboard.WebAPI.Services;
using InvestDashboard.WebAPI.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add Database Context
builder.Services.AddDbContext<InvestDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("InvestDashboard.Infrastructure")));

// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Scoped Repositories
builder.Services.AddScoped<IAtivoRepository, AtivoRepository>();
builder.Services.AddScoped<IPrecoHistoricoRepository, PrecoHistoricoRepository>();
builder.Services.AddScoped<ITransacaoRepository, TransacaoRepository>();
builder.Services.AddScoped<ICarteiraRepository, CarteiraRepository>();
builder.Services.AddScoped<ITaxaEconomicaRepository, TaxaEconomicaRepository>();

// Register Application Services
builder.Services.AddScoped<ICarteiraAppService, CarteiraAppService>();
builder.Services.AddScoped<ITransacaoAppService, TransacaoAppService>();
builder.Services.AddScoped<ITaxasAppService, TaxasAppService>();
builder.Services.AddScoped<IAuthProvider, SupabaseAuthProvider>();
builder.Services.AddHttpClient<IMarketDataProvider, BrapiMarketDataClient>(client =>
    client.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddScoped<IAuthenticationAppService, AuthenticationAppService>();
builder.Services.AddScoped<IRefreshTokenSessionRepository, RefreshTokenSessionRepository>();
builder.Services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
builder.Services.AddSingleton<IAuthRoleProvider, ConfigurationAuthRoleProvider>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ??
        [
            "http://localhost:5000",
            "http://localhost:5173",
            "http://localhost:8080",
            "capacitor://localhost",
            "http://localhost"
        ];

        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("authentication", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

// HttpContext and Identity services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtualService, UsuarioAtualService>();

// Storage & HttpClient registration
builder.Services.AddHttpClient();
builder.Services.AddScoped<ISupabaseStorageService, SupabaseStorageService>();

// The API issues its own short-lived access tokens after validating credentials with Supabase.
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secret = jwtSettings["Secret"];
if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
{
    if (!builder.Environment.IsEnvironment("Testing"))
        throw new InvalidOperationException("Jwt:Secret must be configured with at least 32 bytes.");

    secret = "test-only-key-for-integration-tests-00000000000000000000000000000000";
}

if (string.IsNullOrWhiteSpace(jwtSettings["Issuer"]) || string.IsNullOrWhiteSpace(jwtSettings["Audience"]))
    throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        NameClaimType = "name",
        RoleClaimType = "role",
        ClockSkew = TimeSpan.FromSeconds(30)
    };
    options.MapInboundClaims = false;

    // Configure token extraction for SignalR WebSocket connections
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var authorization = context.Request.Headers.Authorization.ToString();
            if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                context.Token = authorization["Bearer ".Length..].Trim();
                return Task.CompletedTask;
            }

            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/market-data"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var sessionClaim = context.Principal?.FindFirst("sid")?.Value;
            if (!Guid.TryParseExact(sessionClaim, "N", out var sessionId))
            {
                context.Fail("Access token is missing a valid session.");
                return;
            }

            var userClaim = context.Principal?.FindFirst("sub")?.Value
                ?? context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userClaim, out var userId))
            {
                context.Fail("Access token is missing a valid user.");
                return;
            }

            var sessionRepository = context.HttpContext.RequestServices.GetRequiredService<IRefreshTokenSessionRepository>();
            if (!await sessionRepository.IsActiveAsync(sessionId, userId, DateTime.UtcNow, context.HttpContext.RequestAborted))
                context.Fail("Access token session is no longer active.");
        }
    };
});

builder.Services.AddAuthorization();

// Add controllers
builder.Services.AddControllers();

// Add SignalR Realtime services
builder.Services.AddSignalR();

// Register hosted workers except in test runs, where deterministic API behavior is required.
if (!builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddHostedService<AtualizadorDadosMercadoWorker>();

// Add OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Global Exception Handling Middleware (RFC 7807)
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseCors("DefaultCors");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Map SignalR Realtime Hubs
app.MapHub<DadosMercadoHub>("/hubs/market-data");

ApplyMigrations(app);

app.Run();

static void ApplyMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();

    // InMemory has no relational migrations; all relational providers apply migrations.
    if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
    {
        db.Database.EnsureCreated();
        return;
    }

    db.Database.Migrate();
}
