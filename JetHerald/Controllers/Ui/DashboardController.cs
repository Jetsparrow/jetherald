﻿
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
        var user = await Db.GetUser(login);
        var topics = await Db.GetTopicsForUser(user.UserId);
        var hearts = await Db.GetHeartsForUser(user.UserId);
        var vm = new DashboardViewModel
        {
            Topics = topics.ToArray(),
            Hearts = hearts.ToLookup(h => h.TopicId)
        };
        return View(vm);
    }
}

public class DashboardViewModel
{ 
    public Topic[] Topics { get; set; }
    public ILookup<uint, Heart> Hearts { get; set; }
    

}