using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using JetHerald.Services;
using JetHerald.Utils;
using JetHerald.Contracts;
using JetHerald.Authorization;

namespace JetHerald.Controllers.Ui;

[Permission("topic")]
public class TopicController : Controller
{
    Db Db { get; }
    public TopicController(Db db)
    {
        Db = db;
    }

    [HttpGet, Route("ui/topic/create")]
    public IActionResult Create() => View();

    public class CreateTopicRequest
    {
        [BindProperty(Name = "name"), BindRequired]
        public string Name { get; set; }


        [BindProperty(Name = "descr"), BindRequired]
        public string Description { get; set; }
    }

    [HttpPost, Route("ui/topic/create")]
    public async Task<IActionResult> Create(
        CreateTopicRequest req)
    {
        if (!ModelState.IsValid)
            return View();
        var userId = HttpContext.User.GetUserId();
        using var ctx = await Db.GetContext();
        var topic = await ctx.CreateTopic(userId, req.Name, req.Description);
        ctx.Commit();
        if (topic == null)
        {
            ModelState.AddModelError("", "Unknown error");
            return View();
        }

        return RedirectToAction(nameof(ViewTopic), "Topic", new { topicName = topic.Name});
    }

    [HttpGet, Route("ui/topic/{topicName}")]
    public async Task<IActionResult> ViewTopic(string topicName)
    {
        var userId = HttpContext.User.GetUserId();
        using var ctx = await Db.GetContext();
        var topic = await ctx.GetTopic(topicName);
        if (topic == null || topic.CreatorId != userId)
            return NotFound();

        var hearts = await ctx.GetHeartsForTopic(topic.TopicId);
        var vm = new TopicViewModel
        {
            Topic = topic,
            Hearts = hearts.ToArray()
        };
        return View(vm);
    }
}

public class TopicViewModel
{
    public Topic Topic{ get; set; }
    public Heart[] Hearts { get; set; }
}
