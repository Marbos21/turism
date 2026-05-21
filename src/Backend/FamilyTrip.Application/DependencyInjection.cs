using System.Reflection;
using FluentValidation;
using FamilyTrip.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyTrip.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IAuthAppService, AuthAppService>();
        services.AddScoped<IFamilyAppService, FamilyAppService>();
        services.AddScoped<IDestinationAppService, DestinationAppService>();
        services.AddScoped<ITripPlanAppService, TripPlanAppService>();
        services.AddScoped<IRecommendationAppService, RecommendationAppService>();

        return services;
    }
}
