using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using JetHerald.Authorization;
using JetHerald.Options;
using JetHerald.Services;
using JetHerald.Contracts;

namespace JetHerald.Controllers.Ui;

[Permission("admin.invites")]
[Route("ui/admin/invites")]
public class AdminInvitesController : Controller
{
    Db Db { get; }
    AuthConfig AuthCfg { get; }
    public AdminInvitesController(
        Db db,
        IOptionsSnapshot<AuthConfig> authCfg
        )
    {
        Db = db;
        AuthCfg = authCfg.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        using var ctx = await Db.GetContext();
        var invites = await ctx.GetInvites();
        var plans = await ctx.GetPlans();
        var roles = await ctx.GetRoles();
        return View(new AdminInvitesModel
        {
            Invites = invites.ToArray(),
            Plans = plans.ToDictionary(p => p.PlanId),
            Roles = roles.ToDictionary(r => r.RoleId)
        });
    }

    public class CreateInviteRequest
    {
        [BindProperty(Name = "planId"), BindRequired]
        public uint PlanId { get; set; }
        [BindProperty(Name = "roleId"), BindRequired]
        public uint RoleId { get; set; }
    }

    [Permission("admin.invites.create")]
    [HttpPost, Route("create")]
    public async Task<IActionResult> CreateInvite(CreateInviteRequest req)
    {
        using var ctx = await Db.GetContext();
        await ctx.CreateUserInvite(req.PlanId, req.RoleId, TokenHelper.GetToken(AuthCfg.InviteCodeLength));
        ctx.Commit();
        return RedirectToAction(nameof(Index));
    }

    public class DeleteInviteRequest
    {
        [BindProperty(Name = "inviteId"), BindRequired]
        public uint UserInviteId { get; set; }
    }

    [Permission("admin.invites.delete")]
    [HttpPost, Route("delete")]
    public async Task<IActionResult> DeleteInvite(DeleteInviteRequest req)
    {
        using var ctx = await Db.GetContext();
        await ctx.DeleteUserInvite(req.UserInviteId);
        ctx.Commit();
        return RedirectToAction(nameof(Index));
    }
}

public class AdminInvitesModel
{
    public UserInvite[] Invites { get; set; }
    public Dictionary<uint, Plan> Plans { get; set; }
    public Dictionary<uint, Role> Roles { get; set; }
}