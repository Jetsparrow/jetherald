using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace JetHerald.Controllers
{
    [ApiController]
    public class HeartbeatController : ControllerBase
    {
        Db Db { get; }
        JetHeraldBot Herald { get; }

        public HeartbeatController(Db db, JetHeraldBot herald)
        {
            Herald = herald;
            Db = db;
        }

        [Route("api/heartbeat")]
        [HttpPost]
        public async Task<IActionResult> Heartbeat([FromBody] HeartbeatArgs args)
        {
            var t = await Db.GetTopic(args.Topic);
            if (t == null)
                return new NotFoundResult();
            else if (!t.WriteToken.Equals(args.WriteToken, StringComparison.OrdinalIgnoreCase))
                return StatusCode(403);

            if (t.ExpiryMessageSent)
                await Herald.HeartbeatSent(t);

            await Db.AddExpiry(t.Name, args.ExpiryTimeout);

            return new OkResult();
        }

        [DataContract]
        public class HeartbeatArgs
        {
            [DataMember] public string Topic { get; set; }
            [DataMember] public int ExpiryTimeout { get; set; }
            [DataMember] public string WriteToken { get; set; }
        }
    }
}