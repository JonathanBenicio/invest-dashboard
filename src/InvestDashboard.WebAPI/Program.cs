using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
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

// HttpContext and Identity services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtualService, UsuarioAtualService>();

// Storage & HttpClient registration
builder.Services.AddHttpClient();
builder.Services.AddScoped<ISupabaseStorageService, SupabaseStorageService>();

// Configure JWT Authentication (Supabase Auth Integration)
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secret = jwtSettings["Secret"] ?? "default_very_long_fallback_secret_for_security_compliance";

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
    };

    // Configure token extraction for SignalR WebSocket connections
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            // If the request is for our SignalR Hub...
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/market-data"))
            {
                // Assign token to context so JwtBearer middleware can validate it
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Add controllers
builder.Services.AddControllers();

// Add SignalR Realtime services
builder.Services.AddSignalR();

// Register Hosted Services (Background Workers)
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR Realtime Hubs
app.MapHub<DadosMercadoHub>("/hubs/market-data");

ApplyMigrations(app);

app.Run();

static void ApplyMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
    db.Database.Migrate();
}
