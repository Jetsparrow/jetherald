using JetHerald.Authorization;
using JetHerald.Contracts;
using JetHerald.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace JetHerald.Controllers.Ui;

[Permission("admin.users")]
[Route("ui/admin/users")]
public class AdminUsersController : Controller
{
    Db Db { get; }
    public AdminUsersController(Db db)
    {
        Db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        using var ctx = await Db.GetContext();

        var users = await ctx.GetUsers();
        var plans = await ctx.GetPlans();
        var roles = await ctx.GetRoles();

        return View(new AdminUsersModel
        {
            Users = users.ToArray(),
            Plans = plans.ToDictionary(p => p.PlanId),
            Roles = roles.ToDictionary(r => r.RoleId)
        });
    }

    public class SetPermsRequest
    {
        [BindProperty(Name = "planId"), BindRequired]
        public uint PlanId { get; set; }
        [BindProperty(Name = "roleId"), BindRequired]
        public uint RoleId { get; set; }
    }

    [Permission("admin.users.setperms")]
    [HttpPost, Route("setperms/{userId?}")]
    public async Task<IActionResult> SetPerms([FromRoute] uint userId, SetPermsRequest req)
    {
        using var ctx = await Db.GetContext();
        await ctx.UpdatePerms(userId, req.PlanId, req.RoleId);
        ctx.Commit();

        return RedirectToAction(nameof(Index));
    }
}

public class AdminUsersModel
{
    public User[] Users { get; set; }
    public Dictionary<uint, Plan> Plans { get; set; }
    public Dictionary<uint, Role> Roles { get; set; }
}