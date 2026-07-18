using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using ATS.Infrastructure.Persistence;
using ATS.Application.Common.Interfaces;
using ATS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ATS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AtsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AtsDbContext>());
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AtsDbContext>());

        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<Microsoft.AspNetCore.Identity.IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AtsDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddScoped<IIdentityService, IdentityService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Secret"] ?? "SuperSecretKeyReplaceThisInProductionWithLongerKey123!"))
                };
            });

        services.AddSingleton<IAuthorizationPolicyProvider, ATS.Infrastructure.Authorization.PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, ATS.Infrastructure.Authorization.PermissionAuthorizationHandler>();

        services.AddSingleton<ITimeProvider, ATS.Infrastructure.Services.SystemTimeProvider>();

        return services;
    }
}
