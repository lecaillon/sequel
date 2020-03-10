using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sequel.Models;

namespace Sequel.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SequelController : ControllerBase
    {
        private readonly ILogger _logger;

        public SequelController(ILogger<SequelController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("ping")]
        public string Ping()
        {
            return "pong";
        }

        [HttpPost]
        [Route("server-connection/test")]
        public async Task<ActionResult<bool>> TestServerConnection(ServerConnection cnn)
        {
            await Task.Delay(5000);
            return true;
        }
    }
}
