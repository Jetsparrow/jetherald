using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace JetHerald.Controllers;

[ApiController]
public class HelloController : ControllerBase
{
    [Route("api/hello")]
    [HttpGet]
    public object Hello()
    {
        return new
        {
            status = "OK",
            server_name = "JetHerald",
            server_version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion
        };
    }
}
