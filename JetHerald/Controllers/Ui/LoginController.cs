using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Authentication.Cookies;
using JetHerald.Services;
using JetHerald.Utils;

namespace JetHerald.Controllers.Ui;
public class LoginController : Controller
{
    Db Db { get; }
    ILogger Log { get; }

    PathString PathStringOrDefault(string s, string def = "")
    {
        try { return new PathString(s); }
        catch (ArgumentException) { return new PathString(def); }
    }

    public LoginController(Db db, ILogger<LoginController> log)
    {
        Db = db;
        Log = log;
    }

    [HttpGet, Route("ui/login/")]
    public IActionResult Login([FromQuery] string redirect = "")
    {
        ViewData["RedirectTo"] = PathStringOrDefault(redirect);
        return View();
    }

    public class LoginRequest
    {
        [BindProperty(Name = "username"), BindRequired]
        public string Username { get; set; }

        [BindProperty(Name = "password"), BindRequired]
        public string Password { get; set; }
    }

    [HttpPost, Route("ui/login/")]
    public async Task<IActionResult> Login(
        LoginRequest req,
        [FromQuery] string redirect = "")
    {
        if (!ModelState.IsValid)
            return View();

        ViewData["RedirectTo"] = PathStringOrDefault(redirect);

        var user = await Db.GetUser(req.Username);
        if (user == null)
        {
            ModelState.AddModelError("", "User not found");
            return View();
        }
        byte[] newHash = AuthUtils.GetHashFor(req.Password, user.PasswordSalt, user.HashType);
        if (!Enumerable.SequenceEqual(newHash, user.PasswordHash))
        {
            ModelState.AddModelError("", "Incorrect password");
            return View();
        }
        var userIdentity = AuthUtils.CreateIdentity(user.UserId, user.Login, user.Name, user.Allow);
        var principal = new ClaimsPrincipal(userIdentity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        try
        {
            return Redirect(new PathString(redirect));
        }
        catch (ArgumentException)
        {
            return RedirectToAction("Dashboard", "Dashboard");
        }
    }

    [Route("ui/logout/")]
    public async Task<IActionResult> LogOut([FromQuery] string redirect = "")
    {
        await HttpContext.SignOutAsync();
        try
        {
            return Redirect(new PathString(redirect));
        }
        catch (ArgumentException)
        {
            return RedirectToAction(nameof(Login));
        }
    }
}
