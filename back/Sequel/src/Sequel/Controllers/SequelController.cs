using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
    }
}
