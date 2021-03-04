using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace JetHerald.Controllers
{
    [ApiController]
    public class ReportController : ControllerBase
    {
        Db Db { get; }
        JetHeraldBot Herald { get; }

        public ReportController(Db db, JetHeraldBot herald)
        {
            Herald = herald;
            Db = db;
        }

        [Route("api/report")]
        [HttpPost]
        public async Task<IActionResult> Report()
        {
            var q = Request.Query;
            if (q.ContainsKey("Topic")
             && q.ContainsKey("Message")
             && q.ContainsKey("WriteToken"))
            {
                ReportArgs args = new();
                args.Topic = q["Topic"];
                args.Message = q["Message"];
                args.WriteToken = q["WriteToken"];
                return await DoReport(args);
            }

            try
            {
                var args = await JsonSerializer.DeserializeAsync<ReportArgs>(HttpContext.Request.Body, new()
                {
                    IncludeFields = true
                });
                return await DoReport(args);
            }
            catch (JsonException)
            {
                return BadRequest();
            }
        }

        private async Task<IActionResult> DoReport(ReportArgs args)
        {
            var t = await Db.GetTopic(args.Topic);
            if (t == null)
                return new NotFoundResult();
            else if (!t.WriteToken.Equals(args.WriteToken, StringComparison.OrdinalIgnoreCase))
                return StatusCode(403);

            await Herald.PublishMessage(t, args.Message);
            return new OkResult();
        }

        public class ReportArgs
        {
            public string Topic { get; set; }
            public string Message { get; set; }
            public string WriteToken { get; set; }
        }
    }
}
