using Microsoft.AspNetCore.Http;
using JetHerald.Services;
using JetHerald.Utils;
using System.Security.Claims;
using JetHerald.Authorization;

namespace JetHerald.Middlewares;
public class AnonymousUserMassagerMiddleware : IMiddleware
{
    Lazy<Task<string>> AnonymousPermissions { get; }
    public AnonymousUserMassagerMiddleware(Db db)
    {
        AnonymousPermissions = new Lazy<Task<string>>(async () =>
        {
            using var ctx = await db.GetContext();
            var anonymousUser = await ctx.GetUser("Anonymous");
            return anonymousUser.Allow;
        });
    }

    public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
    {
        if (ctx.User.FindFirst(ClaimTypes.PrimarySid) == null)
        {
            var perms = await AnonymousPermissions.Value;
            var ci = new ClaimsIdentity();
            ci.AddClaims(new Claim[] {
                    new Claim(ClaimTypes.PrimarySid, "0"),
                    new Claim(ClaimTypes.NameIdentifier, "anonymous"),
                    new Claim(ClaimTypes.Name, "Anonymous"),
                    new Claim(ClaimTypes.Anonymous, "true"),
                    new Claim(Permissions.ClaimId, perms)
                });
            ctx.User.AddIdentity(ci);
        }
        await next(ctx);
    }
}
