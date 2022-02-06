using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using JetHerald.Contracts;
using JetHerald.Options;
using JetHerald.Services;
using JetHerald.Utils;

namespace JetHerald.Controllers.Ui;
public class RegistrationController : Controller
{
    Db Db { get; }
    ILogger Log { get; }
    AuthConfig Cfg { get; }

    PathString PathStringOrDefault(string s, string def = "")
    {
        try { return new PathString(s); }
        catch (ArgumentException) { return new PathString(def); }
    }

    public RegistrationController(
        ILogger<LoginController> log,
        Db db,
        IOptionsSnapshot<AuthConfig> authConfig)
    {
        Db = db;
        Log = log;
        Cfg = authConfig.Value;
    }

    [HttpGet, Route("ui/register/")]
    public IActionResult Register([FromQuery] string redirect = "")
    {
        ViewData["RedirectTo"] = PathStringOrDefault(redirect);
        return View();
    }

    public class RegisterRequest
    {
        [BindProperty(Name = "invitecode"), BindRequired]
        public string InviteCode { get; set; }

        [BindProperty(Name = "name"), BindRequired]
        [StringLength(maximumLength: 100, MinimumLength = 3)]
        public string Name { get; set; }

        [BindProperty(Name = "login"), BindRequired]
        [StringLength(maximumLength: 64, MinimumLength = 6)]
        public string Login { get; set; }

        [BindProperty(Name = "password"), BindRequired]
        [StringLength(maximumLength: 1024, MinimumLength = 6)]
        public string Password { get; set; }
    }

    [HttpPost, Route("ui/register/")]
    public async Task<IActionResult> Register(RegisterRequest req, [FromQuery] string redirect = "")
    {
        if (!ModelState.IsValid)
            return View();

        ViewData["RedirectTo"] = PathStringOrDefault(redirect);

        var oldUser = await Db.GetUser(req.Login);
        if (oldUser != null)
        {
            ModelState.AddModelError("", "User already exists");
            return View();
        }
        var invite = await Db.GetInviteByCode(req.InviteCode);
        if (invite == null || invite.RedeemedBy != default)
        {
            ModelState.AddModelError("", "No unredeemed invite with this code found");
            return View();
        }
        var user = new User()
        {
            PlanId = invite.PlanId,
            Login = req.Login,
            Name = req.Name,
            PasswordSalt = RandomNumberGenerator.GetBytes(64)
        };
        user.PasswordHash = AuthUtils.GetHashFor(req.Password, user.PasswordSalt, Cfg.HashType);
        var newUser = await Db.RegisterUserFromInvite(user, invite.UserInviteId);
        var userIdentity = AuthUtils.CreateIdentity(newUser.UserId, newUser.Login, newUser.Name, newUser.Allow);
        var principal = new ClaimsPrincipal(userIdentity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        try
        {
            return Redirect(new PathString(redirect));
        }
        catch (ArgumentException)
        {
            return RedirectToAction("News", "Issue");
        }
    }
}
