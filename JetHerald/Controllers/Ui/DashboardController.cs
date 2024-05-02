using Microsoft.AspNetCore.Mvc;

using JetHerald.Services;
using JetHerald.Utils;
using JetHerald.Contracts;
using JetHerald.Authorization;

namespace JetHerald.Controllers.Ui;

[Permission("dashboard")]
public class DashboardController : Controller
{
    Db Db { get; }
    LeakyBucket LeakyBucket { get; }
    public DashboardController(Db db, LeakyBucket leakyBucket)
    {
        Db = db;
        LeakyBucket = leakyBucket;
    }

    [HttpGet, Route("ui/dashboard/")]
    public async Task<IActionResult> Index()
    {
        var login = HttpContext.User.GetUserLogin();
        using var ctx = await Db.GetContext();
        var user = await ctx.GetUser(login);
        var topics = await ctx.GetTopicsForUser(user.UserId);
        var hearts = await ctx.GetHeartsForUser(user.UserId);
        var vm = new DashboardViewModel();
        foreach (var t in topics.OrderBy(x => x.TopicId))
        {
            vm.Topics.Add(new TopicInfo()
            {
                Topic = t,
                Hearts = hearts.Where(h => h.TopicId == t.TopicId).ToList(),
                Utilization =  (int)Math.Round(LeakyBucket.GetUtilization(t.TopicId) * 100)
            });
        }
        vm.User = user;
        return View(vm);
    }
}


public static class DateTimeExt
{
    public static string GetReadableDate(DateTime dt, DateTime now)
    {
        dt = dt.Date;
        now = now.Date;
        var daysDiff = (int)Math.Round((dt - now).TotalDays);

        if (daysDiff == 0)
            return "today";
        if (daysDiff == 1)
            return "tomorrow";
        if (daysDiff == -1)
            return "yesterday";
        
        if (Math.Abs(daysDiff) < 62)
            return dt.ToString("MMM dd");
        return dt.ToString("yyyy MMMM dd");
    }
}

public class DashboardViewModel
{ 
    public User User { get; set; }
    public List<TopicInfo> Topics { get; set; } = new();
}

public class TopicInfo
{
    public Topic Topic { get; set; }
    public ICollection<Heart> Hearts { get; set; }
    public int Utilization { get; set; }
}