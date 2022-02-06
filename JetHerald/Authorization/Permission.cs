using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace JetHerald.Authorization;

public static class Permissions
{
    public const string PolicyPrefix = "permission://";
    public const string ClaimId = "Permission";
}

public class PermissionAttribute : AuthorizeAttribute
{
    public PermissionAttribute(string permission)
        => Policy = Permissions.PolicyPrefix + permission;
}

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permtext)
        => Permission = permtext[Permissions.PolicyPrefix.Length..];
}

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var permissions = context.User.FindFirstValue(Permissions.ClaimId);
        if (PermissionParser.ProvePermission(permissions, requirement.Permission))
            context.Succeed(requirement);
        else
            context.Fail();
        return Task.CompletedTask;
    }
}

public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    public DefaultAuthorizationPolicyProvider Fallback { get; }

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> opt)
        => Fallback = new DefaultAuthorizationPolicyProvider(opt);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => Fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy> GetFallbackPolicyAsync() => Fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Permissions.PolicyPrefix))
        {
            var policy = new AuthorizationPolicyBuilder();
            policy.AddRequirements(new PermissionRequirement(policyName));
            return Task.FromResult(policy.Build());
        }
        return Fallback.GetPolicyAsync(policyName);
    }
}
