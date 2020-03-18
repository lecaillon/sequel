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
        [Route("server-connection")]
        public async Task<ActionResult<List<ServerConnection>>> GetAllServerConnection()
        {
            return Ok(await Store<ServerConnection>.GetCollection());
        }

        [HttpPost]
        [Route("server-connection")]
        public async Task<IActionResult> AddServerConnection(ServerConnection server)
        {
            await Store<ServerConnection>.Add(server);
            return Ok();
        }

        [HttpPost]
        [Route("server-connection/test")]
        public async Task<IActionResult> TestServerConnection(ServerConnection server)
        {
            await server.Type.CreateConnection(server.ConnectionString).Validate();
            return Ok();
        }
    }
}
