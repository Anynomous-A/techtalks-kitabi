
namespace Kitabi.Policies
{
    public static class AuthorizationPolicies
    {
        public static void AddAppPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("JWTPolicy", policy =>
                policy.AddAuthenticationSchemes("JWTScheme")
                .RequireAuthenticatedUser()
                );
            });
        }
    }
}