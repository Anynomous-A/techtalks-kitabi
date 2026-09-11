using Microsoft.IdentityModel.Tokens;
using System.Text;
namespace Kitabi.Extensions
{

    public static class AuthenticationExtensions
    {

        public static void AddAppAuthentication(this IServiceCollection services)
        {
            const string secretKey = "THIS_IS_A_VERY_LONG_SECRET_KEY_CHANGE_ME_1234567890";

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
                    ValidIssuer = "MyServer",
                    ValidAudience = "MyClient",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                };
            });
        }
    }
}