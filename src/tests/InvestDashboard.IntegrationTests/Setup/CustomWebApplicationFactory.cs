using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using InvestDashboard.IntegrationTests.Fakes;
using System.Collections.Generic;

namespace InvestDashboard.IntegrationTests.Setup;

/// <summary>
/// Custom WebApplicationFactory that replaces Npgsql with EF Core InMemory
/// and SupabaseAuthProvider with FakeAuthProvider for isolated testing.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"InvestTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = FakeAuthProvider.TestSecret,
                ["Jwt:Issuer"] = FakeAuthProvider.TestIssuer,
                ["Jwt:Audience"] = FakeAuthProvider.TestAudience
            }));

        builder.ConfigureServices(services =>
        {
            // Remove ALL EF Core related registrations to avoid dual-provider conflict
            var efDescriptors = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<InvestDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true ||
                    d.ImplementationType?.FullName?.Contains("Npgsql") == true)
                .ToList();

            foreach (var descriptor in efDescriptors)
                services.Remove(descriptor);

            services.RemoveAll(typeof(InvestDbContext));

            // Add InMemory database
            services.AddDbContext<InvestDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            // Replace IAuthProvider with FakeAuthProvider
            services.RemoveAll(typeof(IAuthProvider));
            services.AddScoped<IAuthProvider, FakeAuthProvider>();

            services.RemoveAll(typeof(IRefreshTokenSessionRepository));
            services.AddSingleton<IRefreshTokenSessionRepository, FakeRefreshTokenSessionRepository>();

            // Reconfigure JWT to use the fake secret/issuer/audience
            services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
                Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = FakeAuthProvider.TestIssuer,
                        ValidateAudience = true,
                        ValidAudience = FakeAuthProvider.TestAudience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(FakeAuthProvider.TestSecret)),
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };
                    options.MapInboundClaims = false;
                });

            // Remove the hosted background worker to avoid external API calls in tests
            var hostedServiceDescriptor = services
                .Where(d => d.ServiceType == typeof(IHostedService) &&
                             d.ImplementationType?.Name == "AtualizadorDadosMercadoWorker")
                .ToList();
            foreach (var descriptor in hostedServiceDescriptor)
                services.Remove(descriptor);
        });
    }
}
