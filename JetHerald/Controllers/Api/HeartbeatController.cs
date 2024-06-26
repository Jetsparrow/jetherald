﻿using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using JetHerald.Services;

namespace JetHerald.Controllers;

[ApiController]
public class HeartbeatController : ControllerBase
{
    Db Db { get; }
    JetHeraldBot Herald { get; }
    LeakyBucket Timeouts { get; }
    Options.TimeoutConfig Config { get; }

    public HeartbeatController(Db db, JetHeraldBot herald, LeakyBucket timeouts, IOptions<Options.TimeoutConfig> cfgOptions)
    {
        Herald = herald;
        Timeouts = timeouts;
        Db = db;
        Config = cfgOptions.Value;
    }

    // tfw when you need to manually parse body and query
    [Route("api/heartbeat")]
    [HttpPost]
    public async Task<IActionResult> Heartbeat()
    {
        var q = Request.Query;
        if (q.ContainsKey("Topic")
            && q.ContainsKey("ExpiryTimeout")
            && q.ContainsKey("WriteToken")
            && q.ContainsKey("Heart"))
        {
            HeartbeatArgs args = new();
            args.Topic = q["Topic"];
            args.WriteToken = q["WriteToken"];
            args.Heart = q["Heart"];
            if (!int.TryParse(q["ExpiryTimeout"], out var expTimeout))
            {
                return BadRequest();
            }
            args.ExpiryTimeout = expTimeout;
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
    public Task<IActionResult> HeartbeatGet([FromQuery] HeartbeatArgs args) => DoHeartbeat(args);

    private async Task<IActionResult> DoHeartbeat(HeartbeatArgs args)
    {
        var heart = args.Heart ?? "General";

        using var ctx = await Db.GetContext();
        var t = await ctx.GetTopic(args.Topic);
        if (t == null)
            return new NotFoundResult();
        else if (!t.WriteToken.Equals(args.WriteToken, StringComparison.Ordinal))
            return StatusCode(403);

        if (Timeouts.IsTimedOut(t.TopicId))
            return StatusCode(StatusCodes.Status429TooManyRequests);

        var wasBeating = await ctx.ReportHeartbeat(t.TopicId, heart, args.ExpiryTimeout);
        ctx.Commit();
        if (wasBeating == 0)
            await Herald.BroadcastMessageRaw(t.TopicId, $"!{t.Description}!:\nHeart \"{heart}\" has started beating at {DateTime.UtcNow:O}");

        Timeouts.ApplyCost(t.TopicId, Config.HeartbeatCost * t.TimeoutMultiplier);

        return new OkResult();
    }

    public class HeartbeatArgs
    {
        [JsonPropertyName("Topic")] public string Topic { get; set; }
        [JsonPropertyName("Heart")] public string Heart { get; set; }
        [JsonPropertyName("ExpiryTimeout")] public int ExpiryTimeout { get; set; }
        [JsonPropertyName("WriteToken")] public string WriteToken { get; set; }
    }
}
