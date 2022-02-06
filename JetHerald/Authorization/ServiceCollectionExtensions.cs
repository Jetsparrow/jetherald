using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace JetHerald.Authorization;

public static  class ServiceCollectionExtensions
{
    public static IServiceCollection AddPermissions(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        return services;
    }
}
