using Microsoft.AspNetCore.Mvc;

using JetHerald.Authorization;
using JetHerald.Services;
using JetHerald.Utils;

namespace JetHerald.Controllers.Ui;

[Permission("profile")]
public class ProfileController : Controller
{
    Db Db { get; }
    public ProfileController(Db db)
    {
        Db = db;
    }

    [HttpGet, Route("ui/profile/")]
    public async Task<IActionResult> Index()
    {
        var login = HttpContext.User.GetUserLogin();
        using var ctx = await Db.GetContext();
        var user = await ctx.GetUser(login);

        var vm = new ProfileViewModel
        {
            OrganizationName = user.Name,
            JoinedOn = user.CreateTs,
        };
        return View(vm);
    }
}

public class ProfileViewModel
{
    public string OrganizationName { get; set; }
    public DateTime JoinedOn { get; set; }
}