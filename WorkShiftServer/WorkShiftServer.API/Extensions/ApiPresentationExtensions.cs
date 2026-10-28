using Microsoft.OpenApi.Models;
using WorkShiftServer.API.Services.Realtime;

namespace WorkShiftServer.API.Extensions;

public static class ApiPresentationExtensions
{
    public static IServiceCollection AddApiPresentation(this IServiceCollection services)
    {
        services.AddScoped<AdminDataChangedFilter>();
        services.AddControllers(options => options.Filters.AddService<AdminDataChangedFilter>());
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter the access token returned by /api/auth/login."
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
        services.AddSignalR();
        services.AddCors(options =>
        {
            options.AddPolicy("SignalRPolicy", policy =>
            {
                policy.SetIsOriginAllowed(_ => true)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }
}
