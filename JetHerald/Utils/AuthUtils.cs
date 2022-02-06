using System.Security.Claims;

using JetHerald.Authorization;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;

namespace JetHerald.Utils;
public static class AuthUtils
{
    public static byte[] GetHashFor(string password, byte[] salt, int hashType = 1) => hashType switch
    {
        1 => KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA512, 100000, 64),
        _ => throw new ArgumentException($"Unexpected hash type {hashType}")
    };

    public static ClaimsIdentity CreateIdentity(uint userId, string login, string name, string perms)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaims(new Claim[] {
            new Claim(ClaimTypes.PrimarySid, userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, login),
            new Claim(ClaimTypes.Name, name),
            new Claim(Permissions.ClaimId, perms),
        });
        return identity;
    }

    public static uint GetUserId(this ClaimsPrincipal principal)
        => uint.Parse(principal.FindFirstValue(ClaimTypes.PrimarySid));

    public static string GetUserLogin(this ClaimsPrincipal principal)
    => principal.FindFirstValue(ClaimTypes.NameIdentifier);

    public static bool IsAnonymous(this ClaimsPrincipal principal)
        => principal.HasClaim(x => x.Type == ClaimTypes.Anonymous);

    public static bool UserCan(this HttpContext ctx, string permission)
        => PermissionParser.ProvePermission(ctx.User.FindFirstValue(Permissions.ClaimId), permission);
}
