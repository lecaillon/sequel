using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Sequel.Core;
using Sequel.Models;

namespace Sequel.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SequelController : ControllerBase
    {
        [HttpGet]
        [Route("ping")]
        public string Ping()
        {
            return "pong";
        }

        [HttpGet]
        [Route("server-connection")]
        public async Task<ActionResult<List<ServerConnection>>> GetAllServerConnection()
        {
            return Ok(await Store<ServerConnection>.GetCollection());
        }

        [HttpPost]
        [Route("server-connection/test")]
        public async Task<IActionResult> TestServerConnection(ServerConnection cnn)
        {
            await Store<ServerConnection>.Add(cnn, unique: true);
            return Ok();
        }
    }
}
