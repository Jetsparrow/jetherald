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

        [Route("api/heartbeat")]
        [HttpGet]
        public Task<IActionResult> HeartbeatGet(HeartbeatArgs args) => DoHeartbeat(args);

        private async Task<IActionResult> DoHeartbeat(HeartbeatArgs args)
        {
            var heart = args.Heart ?? "General";

            var t = await Db.GetTopic(args.Topic);
            if (t == null)
                return new NotFoundResult();
            else if (!t.WriteToken.Equals(args.WriteToken, StringComparison.Ordinal))
                return StatusCode(403);


            var affected = await Db.ReportHeartbeat(t.TopicId, heart, args.ExpiryTimeout);

            if (affected == 1)
                await Herald.HeartbeatSent(t, heart);

            return new OkResult();
        }

        public class HeartbeatArgs
        {
            [JsonPropertyName("Topic")] public string Topic;
            [JsonPropertyName("Heart")] public string Heart;
            [JsonPropertyName("ExpiryTimeout")] public int ExpiryTimeout;
            [JsonPropertyName("WriteToken")] public string WriteToken;
        }
    }
}