
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
    public DashboardController(Db db)
    {
        Db = db;
    }

    [HttpGet, Route("ui/dashboard/")]
    public async Task<IActionResult> Index()
    {
        var login = HttpContext.User.GetUserLogin();
        using var ctx = await Db.GetContext();
        var user = await ctx.GetUser(login);
        var topics = await ctx.GetTopicsForUser(user.UserId);
        var hearts = await ctx.GetHeartsForUser(user.UserId);
        var vm = new DashboardViewModel
        {
            Topics = topics.ToArray(),
            Hearts = hearts.Where(h => h.ExpiryTs + TimeSpan.FromDays(90) >= DateTime.UtcNow ).ToLookup(h => h.TopicId)
        };
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
    public Topic[] Topics { get; set; }
    public ILookup<uint, Heart> Hearts { get; set; }
    

}