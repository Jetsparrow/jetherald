using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace JetHerald.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EchoController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get(string msg)
        {
            await Task.Delay(100);
            return Ok(msg);
        }

        [HttpPost]
        public IActionResult GetSync(string msg)
        {
            return Ok(msg);
        }
    }
}