using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sequel.Core;
using Sequel.Models;

namespace Sequel.Controllers
{
    [ApiController]
    [Route("sequel")]
    public class SequelController : ControllerBase
    {
        [HttpGet]
        [Route("server-connections")]
        public async Task<ActionResult<List<ServerConnection>>> GetAllServerConnection()
        {
            return Ok(await Store<ServerConnection>.GetCollection());
        }

        [HttpPost]
        [Route("server-connections")]
        public async Task<IActionResult> AddServerConnection(ServerConnection server)
        {
            await Store<ServerConnection>.Add(server);
            return Ok();
        }

        [HttpDelete]
        [Route("server-connections/{id}")]
        public async Task<IActionResult> DeleteServerConnection(int id)
        {
            await Store<ServerConnection>.Delete(id);
            return Ok();
        }

        [HttpPost]
        [Route("server-connections/test")]
        public async Task<IActionResult> TestServerConnection(ServerConnection server)
        {
            await server.Validate();
            return Ok();
        }

        [HttpPost]
        [Route("databases")]
        public async Task<ActionResult<IEnumerable<string>>> GetDatabases(ServerConnection server)
        {
            return Ok(await server.GetDatabaseSystem().LoadDatabases());
        }

        [HttpPost]
        [Route("database-objects")]
        public async Task<ActionResult<IEnumerable<DatabaseObjectNode>>> GetDatabaseObjects(QueryExecutionContext context)
        {
            return Ok(await context.Server.GetDatabaseSystem().LoadDatabaseObjects(context.Database, context.DatabaseObject));
        }

        [HttpPost]
        [Route("execute-query")]
        public async Task<ActionResult<QueryResponseContext>> ExecuteQuery(QueryExecutionContext context)
        {
            if (string.IsNullOrEmpty(context.Id))
            {
                ModelState.AddModelError(nameof(QueryExecutionContext.Id), $"The {nameof(QueryExecutionContext.Id)} field is required.");
            }
            if (string.IsNullOrWhiteSpace(context.Sql))
            {
                ModelState.AddModelError(nameof(QueryExecutionContext.Sql), $"The {nameof(QueryExecutionContext.Sql)} field is required.");
            }
            if (ModelState.ErrorCount != 0)
            {
                return BadRequest(new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                });
            }

            return Ok(await context.ExecuteQuery());
        }
    }
}
