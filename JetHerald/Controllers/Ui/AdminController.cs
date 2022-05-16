using Microsoft.AspNetCore.Mvc;
using JetHerald.Authorization;

namespace JetHerald.Controllers.Ui;
[Permission("admin")]
[Route("ui/admin")]
public class AdminController : Controller
{
    public AdminController() { }

    [HttpGet]
    public IActionResult Index() => View();
}
