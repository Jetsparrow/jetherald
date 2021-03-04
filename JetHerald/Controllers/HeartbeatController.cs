using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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

        // tfw when you need to manually parse body and query
        [Route("api/heartbeat")]
        [HttpPost]
        public async Task<IActionResult> Heartbeat()
        {
            var q = Request.Query;
            if (q.ContainsKey("Topic")
             && q.ContainsKey("ExpiryTimeout")
             && q.ContainsKey("WriteToken"))
            {
                HeartbeatArgs args = new();
                args.Topic = q["Topic"];
                args.WriteToken = q["WriteToken"];
                if (!int.TryParse(q["ExpiryTimeout"], out args.ExpiryTimeout))
                {
                    return BadRequest();
                }
                return await DoHeartbeat(args);
            }

            try
            {
                var args = await JsonSerializer.DeserializeAsync<HeartbeatArgs>(HttpContext.Request.Body, new JsonSerializerOptions()
                {
                    IncludeFields = true
                });
                return await DoHeartbeat(args);
            }
            catch (JsonException)
            {
                return BadRequest();
            }
        }

        private async Task<IActionResult> DoHeartbeat(HeartbeatArgs args)
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

        public class HeartbeatArgs
        {
            [JsonPropertyName("Topic")] public string Topic;
            [JsonPropertyName("ExpiryTimeout")] public int ExpiryTimeout;
            [JsonPropertyName("WriteToken")] public string WriteToken;
        }
    }
}