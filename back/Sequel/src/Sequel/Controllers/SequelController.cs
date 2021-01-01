using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Sequel.Core;
using Sequel.Models;

namespace Sequel.Controllers
{
    [ApiController]
    [Route("sequel")]
    public class SequelController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        private static readonly MemoryCacheEntryOptions CodeLensCacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(1));

        public SequelController(IMemoryCache cache)
        {
            _cache = Check.NotNull(cache, nameof(cache));
        }

        [HttpGet]
        [Route("server-connections")]
        public async Task<ActionResult<List<ServerConnection>>> GetAllServerConnection()
        {
            return Ok(await Store<ServerConnection>.GetList());
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
        [Route("nodes")]
        public async Task<ActionResult<IEnumerable<TreeViewNode>>> GetTreeViewNodes(TreeViewContext context)
        {
            return Ok(await context.Server.GetDatabaseSystem().LoadTreeViewNodes(context.Database, context.Node));
        }

        [HttpPost]
        [Route("nodes/{id}/menu-items")]
        public async Task<ActionResult<List<TreeViewMenuItem>>> GetTreeViewMenuItems(TreeViewContext context)
        {
            if (context.Node is null)
            {
                ModelState.AddModelError(nameof(TreeViewContext.Node), $"The {nameof(TreeViewContext.Node)} field is required.");
            }
            if (ModelState.ErrorCount != 0)
            {
                return BadRequest(new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                });
            }

            return Ok(await context.Server.GetDatabaseSystem().LoadTreeViewMenuItems(context.Node!, context.Database, context.Server.Id!.Value));
        }

        [HttpPost]
        [Route("execute-query")]
        public async Task<ActionResult<QueryResponseContext>> ExecuteQuery(QueryExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(context.GetSqlStatement()))
            {
                ModelState.AddModelError("Sql", $"The Sql field is required.");
            }
            if (ModelState.ErrorCount != 0)
            {
                return BadRequest(new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                });
            }

            return Ok(await QueryManager.ExecuteQuery(context));
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

            QueryManager.Cancel(queryId.ToString()!);
            return Ok();
        }

        [HttpPost]
        [Route("completion-items/intellisense")]
        public async Task<ActionResult<IEnumerable<CompletionItem>>> GetIntellisense(CompletionContext context)
        {
            return Ok(await context.Server.GetDatabaseSystem().LoadCompletionItems(
                context.LineNumber,
                context.Column,
                context.TriggerCharacter,
                context.Sql,
                context.Database));
        }

        [HttpPost]
        [Route("completion-items/snippet")]
        public async Task<ActionResult<IEnumerable<CompletionItem>>> GeSnippet(CompletionContext context)
        {
            return Ok((await Store<Snippet>.GetList())
                .Where(x => (x.Dbms.IsNullOrEmpty() || x.Dbms.Contains(context.Server.Type))
                         && (x.ConnectionIds.IsNullOrEmpty() || x.ConnectionIds.Contains(context.Server.Id!.Value))
                         && (x.Databases.IsNullOrEmpty() || context.Database is null || x.Databases.Contains(context.Database))
                         &&  x.Kind is not null)
                .Select(x => new CompletionItem(x.Label, x.Kind!.Value, x.InsertText, x.Detail))
                .OrderBy(x => x.Label));
        }

        [HttpPost]
        [Route("codelenses")]
        public ActionResult<IEnumerable<CodeLens>> GetCodeLenses(QueryExecutionContext context)
        {
            string key = $"CodeLens-{context.Id}";
            if (!_cache.TryGetValue<IEnumerable<CodeLens>>(key, out var lenses))
            {
                lenses = context.Server.GetDatabaseSystem().LoadCodeLens(context.GetSqlStatement());
                _cache.Set(key, lenses, CodeLensCacheEntryOptions);
            }
            
            return Ok(lenses);
        }

        private static readonly List<ColumnDefinition> QueryHistoryColumns = new List<ColumnDefinition>
        {
            new ColumnDefinition("code", "text", "Code") { Hide = true },
            new ColumnDefinition("star", "bool", "Favorite") { Editable = false, Filter = false, CellRenderer = "cellRendererStar" },
            new ColumnDefinition("type", "text", "DBMS") { Editable = false, Filter = false, CellRenderer = "cellRendererDbms" },
            new ColumnDefinition("status", "text", "Status") { Editable = false, Filter = false, CellRenderer = "cellRendererQueryStatus" },
            new ColumnDefinition("executionCount", "int", "Execution Count") { Editable = false, Filter = false },
            new ColumnDefinition("lastExecutedOn", "date", "Last execution") { Editable = false, ValueFormatter = "new Date(value).toLocaleDateString() + ' ' + new Date(value).toLocaleTimeString()" },
            new ColumnDefinition("name", "text", "Name") { Editable = false, Filter = false },
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
        [Route("history/favorites/{code}")]
        public async Task<IActionResult> UpdateHistoryFavorite(string code, QueryHistoryQuery query)
        {
            await QueryManager.History.UpdateFavorite(code, query.Star);
            return Ok();
        }
    }
}
