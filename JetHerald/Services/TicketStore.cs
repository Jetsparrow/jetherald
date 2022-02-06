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
    public Task RemoveAsync(string key)
        => Db.RemoveSession(key);

    public Task RenewAsync(string key, AuthenticationTicket ticket)
        => Db.UpdateSession(
            key,
            TicketSerializer.Default.Serialize(ticket),
            ticket.Properties.ExpiresUtc.Value.DateTime);

    public async Task<AuthenticationTicket> RetrieveAsync(string key)
    {
        var userSession = await Db.GetSession(key);
        return TicketSerializer.Default.Deserialize(userSession.SessionData);
    }

    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var cfg = Cfg.CurrentValue;
        var bytes = RandomNumberGenerator.GetBytes(cfg.TicketIdLengthBytes);
        var key = Convert.ToBase64String(bytes);
        return Db.CreateSession(
            key,
            TicketSerializer.Default.Serialize(ticket),
            ticket.Properties.ExpiresUtc.Value.DateTime);
    }
}
