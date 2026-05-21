using FamilyTrip.Application.Services;
using FamilyTrip.Domain.Interfaces;
using FamilyTrip.Infrastructure.AI;
using FamilyTrip.Infrastructure.Data;
using FamilyTrip.Infrastructure.Integrations;
using FamilyTrip.Infrastructure.Repositories;
using FamilyTrip.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyTrip.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // EF Core with SQLite
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Domain services
        services.AddSingleton<ITokenService, TokenService>();

        // Application services (infrastructure implementations).
        // Concrete rule-based optimizer is also registered because the hybrid uses it as fallback.
        services.AddScoped<ItineraryOptimizer>();
        services.AddScoped<AI.AiItineraryOptimizer>();
        services.AddScoped<IItineraryOptimizer, AI.HybridItineraryOptimizer>();

        // External integrations: OpenWeatherMap, Google Maps, Amadeus + in-memory cache.
        services.AddExternalIntegrations(configuration);

        // AI: agentic recommendation engine with rule-based fallback.
        services.AddAiRecommendations(configuration);

        return services;
    }
}
