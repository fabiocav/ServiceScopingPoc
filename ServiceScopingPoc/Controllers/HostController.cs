using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceScopingPoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HostController : ControllerBase
    {
        private readonly IServiceA _a;
        private readonly IServiceB _b;
        private readonly IScriptHostManager _hostManager;

        public HostController(IServiceA a, IServiceB b, IScriptHostManager hostManager)
        {
            _a = a;
            _b = b;
            _hostManager = hostManager;
        }

        [HttpGet]
        public ActionResult<object> Get()
        {
            return new { ServiceA = _a.Id, ServiceB = _b.Id };
        }

        [HttpGet("restart")]
        public async Task<ActionResult> Restart()
        {
            await _hostManager.RestartHostAsync(CancellationToken.None);

            return Ok();
        }
    }
}
