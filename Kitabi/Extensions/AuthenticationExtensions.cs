using Microsoft.IdentityModel.Tokens;
using System.Text;
namespace Kitabi.Extensions
{

    public static class AuthenticationExtensions
    {

        public static void AddAppAuthentication(this IServiceCollection services , IConfiguration configuration)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "JWTScheme";
                options.DefaultChallengeScheme = "JWTScheme";
            })
            .AddJwtBearer("JWTScheme", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "MyClient",
                    ValidAudience = "MyServer",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
                };
            });
        }
    }
}