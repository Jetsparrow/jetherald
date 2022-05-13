using JetHerald.Authorization;
using JetHerald.Contracts;
using JetHerald.Options;
using JetHerald.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace JetHerald.Controllers.Ui;
[Permission("admintools")]
public class AdminToolsController : Controller
{
    Db Db { get; }
    ILogger Log { get; }
    AuthConfig AuthCfg { get; }
    public AdminToolsController(
        ILogger<AdminToolsController> log,
        Db db,
        IOptionsSnapshot<AuthConfig> authCfg
        )
    {
        Db = db;
        Log = log;
        AuthCfg = authCfg.Value;
    }

    [HttpGet, Route("ui/admintools/")]
    public IActionResult Index() => View();


    [HttpGet, Route("ui/admintools/invites")]
    public async Task<IActionResult> ViewInvites()
    {
        var invites = await Db.GetInvites();
        var plans = await Db.GetPlans();
        var roles = await Db.GetRoles();
        return View(new ViewInvitesModel
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
    [HttpPost, Route("ui/admintools/invites/create")]
    public async Task<IActionResult> CreateInvite(CreateInviteRequest req)
    {
        await Db.CreateUserInvite(req.PlanId, req.RoleId, TokenHelper.GetToken(AuthCfg.InviteCodeLength));
        return RedirectToAction(nameof(ViewInvites));
    }
}

public class ViewInvitesModel
{
    public UserInvite[] Invites { get; set; }
    public Dictionary<uint, Plan> Plans { get; set; }
    public Dictionary<uint, Role> Roles { get; set; }
}