using AthleteOS.BuildingBlocks.Api.Middleware;
using AthleteOS.BuildingBlocks.Api.Tenant;
using AthleteOS.BuildingBlocks.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AthleteOS.BuildingBlocks.Api.Extensions;

public static class ApiExtensions
{
    public static IServiceCollection AddBuildingBlocksApi(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTenant, HttpContextCurrentTenant>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }

    public static IApplicationBuilder UseBuildingBlocksMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();
        return app;
    }
}
