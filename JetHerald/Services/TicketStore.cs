using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using JetHerald.Options;

namespace JetHerald.Services;
public class JetHeraldTicketStore : ITicketStore
{
    Db Db { get; }
    IOptionsMonitor<AuthConfig> Cfg { get; }
    public JetHeraldTicketStore(Db db, IOptionsMonitor<AuthConfig> cfg )
    {
        Db = db;
        Cfg = cfg;
    }
    public async Task RemoveAsync(string key)
    {
        using var ctx = await Db.GetContext();
        await ctx.RemoveSession(key);
        ctx.Commit();
    }
    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        using var ctx = await Db.GetContext();
        await ctx.UpdateSession(
            key,
            TicketSerializer.Default.Serialize(ticket),
            ticket.Properties.ExpiresUtc.Value.DateTime);
        ctx.Commit();

    }
    public async Task<AuthenticationTicket> RetrieveAsync(string key)
    {
        using var ctx = await Db.GetContext();
        var userSession = await ctx.GetSession(key);
        return TicketSerializer.Default.Deserialize(userSession.SessionData);
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var cfg = Cfg.CurrentValue;
        var bytes = RandomNumberGenerator.GetBytes(cfg.TicketIdLengthBytes);
        var key = Convert.ToBase64String(bytes);
        using var ctx = await Db.GetContext();
        await ctx.CreateSession(
            key,
            TicketSerializer.Default.Serialize(ticket),
            ticket.Properties.ExpiresUtc.Value.DateTime);
        ctx.Commit();
        return key;
    }
}
