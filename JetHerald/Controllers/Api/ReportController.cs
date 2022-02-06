using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using JetHerald.Services;

namespace JetHerald.Controllers;
[ApiController]
public class ReportController : ControllerBase
{
    Db Db { get; }
    JetHeraldBot Herald { get; }
    LeakyBucket Timeouts { get; }
    Options.TimeoutConfig Config { get; }

    public ReportController(Db db, JetHeraldBot herald, LeakyBucket timeouts, IOptions<Options.TimeoutConfig> cfgOptions)
    {
        Herald = herald;
        Timeouts = timeouts;
        Db = db;
        Config = cfgOptions.Value;
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
            var args = await JsonSerializer.DeserializeAsync<ReportArgs>(HttpContext.Request.Body, new JsonSerializerOptions()
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

        if (Timeouts.IsTimedOut(t.TopicId))
            return StatusCode(StatusCodes.Status429TooManyRequests);

        await Herald.BroadcastMessageRaw(t.TopicId, $"|{t.Description}|:\n{args.Message}");

        Timeouts.ApplyCost(t.TopicId, Config.ReportCost);

        return new OkResult();
    }

    public class ReportArgs
    {
        public string Topic { get; set; }
        public string Message { get; set; }
        public string WriteToken { get; set; }
    }
}
