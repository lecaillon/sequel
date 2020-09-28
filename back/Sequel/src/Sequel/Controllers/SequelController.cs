using System;
using System.Collections.Generic;
using System.Text.Json;
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
            return Ok(await Store<ServerConnection>.GetListAsync());
        }

        [HttpPost]
        [Route("server-connections")]
        public async Task<IActionResult> AddServerConnection(ServerConnection server)
        {
            await Store<ServerConnection>.AddAsync(server);
            return Ok();
        }

        [HttpDelete]
        [Route("server-connections/{id}")]
        public async Task<IActionResult> DeleteServerConnection(int id)
        {
            await Store<ServerConnection>.DeleteAsync(id);
            return Ok();
        }

        [HttpPost]
        [Route("server-connections/test")]

        public async Task<IActionResult> TestServerConnection(ServerConnection server)
        {
            await server.ValidateAsync();
            return Ok();
        }

        [HttpPost]
        [Route("databases")]
        public async Task<ActionResult<IEnumerable<string>>> GetDatabases(ServerConnection server)
        {
            return Ok(await server.GetDatabaseSystem().LoadDatabasesAsync());
        }

        [HttpPost]
        [Route("nodes")]
        public async Task<ActionResult<IEnumerable<TreeViewNode>>> GetTreeViewNodes(QueryExecutionContext context)
        {
            return Ok(await context.Server.GetDatabaseSystem().LoadTreeViewNodesAsync(context.Database, context.Node));
        }

        [HttpPost]
        [Route("nodes/{id}/menu-items")]
        public async Task<ActionResult<List<TreeViewMenuItem>>> GetTreeViewMenuItems(QueryExecutionContext context)
        {
            if (context.Node is null)
            {
                ModelState.AddModelError(nameof(QueryExecutionContext.Node), $"The {nameof(QueryExecutionContext.Node)} field is required.");
            }
            if (string.IsNullOrWhiteSpace(context.Database) && context.Server.Type != DBMS.SQLite)
            {
                ModelState.AddModelError(nameof(QueryExecutionContext.Sql), $"The {nameof(QueryExecutionContext.Database)} field is required.");
            }
            if (ModelState.ErrorCount != 0)
            {
                return BadRequest(new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                });
            }

            return Ok(await context.Server.GetDatabaseSystem().LoadTreeViewMenuItemsAsync(context.Node!, context.Database, context.Server.Id!.Value));
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
            if (string.IsNullOrWhiteSpace(context.Database) && context.Server.Type != DBMS.SQLite)
            {
                ModelState.AddModelError(nameof(QueryExecutionContext.Sql), $"The {nameof(QueryExecutionContext.Database)} field is required.");
            }
            if (ModelState.ErrorCount != 0)
            {
                return BadRequest(new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                });
            }

            return Ok(await QueryManager.ExecuteQueryAsync(context));
        }

        [HttpPost]
        [Route("cancel-query")]
        public IActionResult CancelQuery(JsonElement queryId)
        {
            if (queryId.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(queryId.ToString()))
            {
                ModelState.AddModelError(nameof(queryId), $"The {nameof(queryId)} field is required.");
                return BadRequest(new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                });
            }

            QueryManager.Cancel(queryId.ToString());
            return Ok();
        }

        [HttpPost]
        [Route("intellisense")]
        public async Task<ActionResult<IEnumerable<CompletionItem>>> Intellisense(QueryExecutionContext context)
        {
            return Ok(await context.Server.GetDatabaseSystem().LoadIntellisenseAsync(context.Database));
        }

        private static readonly List<ColumnDefinition> QueryHistoryColumns = new List<ColumnDefinition>
        {
            new ColumnDefinition("id", "int", "Id") { Editable = false },
            new ColumnDefinition("type", "text", "DBMS") { Editable = false, CellRenderer = "cellRendererDbms" },
            new ColumnDefinition("serverConnection", "text", "Connection") { Editable = false },
            new ColumnDefinition("sql", "text") { Hide = true },
            new ColumnDefinition("hash", "text") { Hide = true },
            new ColumnDefinition("executedOn", "date", "Last execution") { Editable = false, ValueFormatter = "new Date(value).toLocaleDateString() + ' ' + new Date(value).toLocaleTimeString()" },
            new ColumnDefinition("status", "text", "Status") { Editable = false, Filter = false, CellRenderer = "cellRendererQueryStatus" },
            new ColumnDefinition("elapsed", "int", "Elapsed (ms)") { Hide = true },
            new ColumnDefinition("rowCount", "int", "Row count") { Hide = true },
            new ColumnDefinition("recordsAffected", "int", "Records affected") { Hide = true },
            new ColumnDefinition("executionCount", "int", "Execution Count") { Hide = true },
            new ColumnDefinition("star", "bool", "Favorite") { Editable = false, Filter = false, CellRenderer = "cellRendererStar" },
        };

        [HttpGet]
        [Route("history")]
        public async Task<ActionResult<QueryResponseContext>> SearchHistory([FromQuery] QueryHistoryQuery? query)
        {
            var response = new QueryResponseContext("id-history");
            response.Columns.AddRange(QueryHistoryColumns);
            try
            {
                var history = await QueryManager.History.Load(query ?? new QueryHistoryQuery());
                response.Rows.AddRange(history);
                response.Status = QueryResponseStatus.Succeeded;
            }
            catch (Exception ex)
            {
                response.Error = ex.Message;
                response.Status = QueryResponseStatus.Failed;
            }

            return Ok(response);
        }

        [HttpPost]
        [Route("history/favorites/{id}")]
        public async Task<IActionResult> UpdateHistoryFavorite(int id, QueryHistoryQuery query)
        {
            await QueryManager.History.UpdateFavorite(id, query.Star);
            return Ok();
        }
    }
}
