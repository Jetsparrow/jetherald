using Microsoft.AspNetCore.Mvc;
namespace JetHerald.Controllers.Ui;
public class MainController : Controller
{
    [Route("/error/")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
    [Route("/403")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error403()
    {
        return View();
    }
    [Route("/404")]
    public IActionResult Error404()
    {
        return View();
    }
    [Route("/400")]
    public IActionResult Error400()
    {
        return View();
    }
}
