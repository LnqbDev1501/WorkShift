using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using WorkShiftServer.API.Services;

namespace WorkShiftServer.API.Extensions;

public static class ApiAuthenticationExtensions
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var supabaseUrl = configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Missing Supabase:Url configuration.");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"{supabaseUrl.TrimEnd('/')}/auth/v1";
                options.Audience = "authenticated";
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // SignalR browser transports send the bearer token as a query parameter.
                        if (context.Request.Path.StartsWithSegments("/hubs/payroll-notifications")
                            && context.Request.Query.TryGetValue("access_token", out var token))
                            context.Token = token;
                        return Task.CompletedTask;
                    }
                };
            });
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            options.AddPolicy("Admin", policy => policy.RequireRole("ADMIN"));
            options.AddPolicy("Accountant", policy => policy.RequireRole("ADMIN", "ACCOUNTANT"));
            options.AddPolicy("Staff", policy => policy.RequireRole("ADMIN", "STAFF"));
            options.AddPolicy("Manager", policy => policy.RequireRole("ADMIN", "MANAGER"));
        });
        services.AddScoped<Microsoft.AspNetCore.Authentication.IClaimsTransformation, SupabaseRoleClaimsTransformation>();

        return services;
    }
}
