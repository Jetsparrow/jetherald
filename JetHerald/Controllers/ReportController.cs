using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace JetHerald.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        Db Db { get; }
        JetHeraldBot Herald { get; }

        public ReportController(Db db, JetHeraldBot herald)
        {
            Herald = herald;
            Db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ReportArgs args)
        {
            var t = await Db.GetTopic(args.Topic);
            if (t == null)
                return new NotFoundResult();
            else if (!t.WriteToken.Equals(args.WriteToken, StringComparison.OrdinalIgnoreCase))
                return StatusCode(403);

            await Herald.PublishMessage(t, args.Message);
            return new OkResult();
        }

        [DataContract]
        public class ReportArgs
        {
            [DataMember] public string Topic { get; set; }
            [DataMember] public string Message { get; set; }
            [DataMember] public string WriteToken { get; set; }
        }
    }
}
