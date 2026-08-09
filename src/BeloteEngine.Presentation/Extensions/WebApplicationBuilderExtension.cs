using System.Text.Json;
using Microsoft.OpenApi.Models;

namespace BeloteEngine.Presentation.Extensions;

public static class WebApplicationBuilderExtension
{
    public static void AddPresentation(this WebApplicationBuilder builder)
    {
        builder.Services
        .AddControllers()
        .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = false)
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.WriteIndented = true;
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "Belote Engine API",
                Version = "v1",
                Description = "API for managing Belote game lobbies and gameplay"
            });
            c.AddSecurityDefinition("JwtAuth", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "Jwt"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "JwtAuth"
                        }
                    },
                    []
                }
            });
        });

        //Error Handling middleware
    }
}
