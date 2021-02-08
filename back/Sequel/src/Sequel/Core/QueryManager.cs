using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using Sequel.Models;
using static Sequel.Helper;

namespace Sequel.Core
{
    public static class QueryManager
    {
        private static readonly Dictionary<string, CancellationTokenSource> TokensByQueryId = new Dictionary<string, CancellationTokenSource>();

        public static async Task<QueryResponseContext> ExecuteQuery(QueryExecutionContext context)
        {
            string queryId = Check.NotNull(context.Id, nameof(context.Id));
            var cancellationToken = CreateToken(queryId);

            try
            {
                return await context.ExecuteQuery(cancellationToken);
            }
            finally
            {
                ReleaseToken(queryId);
            }
        }

        public static void Cancel(string queryId)
        {
            if (TokensByQueryId.TryGetValue(queryId, out var cts))
            {
                try
                {
                    cts.Cancel();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to cancel query execution: {ex.Message}");
                }
                finally
                {
                    ReleaseToken(queryId);
                }
            }
        }

        private static async Task<QueryResponseContext> ExecuteQuery(this QueryExecutionContext context, CancellationToken cancellationToken)
        {
            return await context.Server.Execute(context.Database, context.GetSqlStatement()!, async (dbCommand, ct) =>
            {
                var response = new QueryResponseContext(context.Id);
                var sw = new Stopwatch();

                try
                {
                    sw.Start();
                    using var ctr = ct.Register(() => dbCommand.Cancel());
                    using var dataReader = await dbCommand.ExecuteReaderAsync(ct);

                    do
                    {
                        var columnNames = new List<string>();
                        response.Columns.Clear();
                        response.Rows.Clear();

                        for (int i = 0; i < dataReader.FieldCount; i++)
                        {
                            columnNames.Add(columnNames.Contains(dataReader.GetName(i))
                                ? dataReader.GetName(i) + (i + 1)
                                : dataReader.GetName(i));

                            response.Columns.Add(new ColumnDefinition(columnNames[i], dataReader.GetDataTypeName(i)));
                        }

                        while (await dataReader.ReadAsync(ct))
                        {
                            var dataRow = new ExpandoObject() as IDictionary<string, object?>;
                            for (int i = 0; i < dataReader.FieldCount; i++)
                            {
                                var value = dataReader[i];
                                dataRow.Add(columnNames[i], value is DBNull ? null : value);
                            }
                            response.Rows.Add(dataRow);
                        }
                    } while (await dataReader.NextResultAsync(ct));

                    response.RecordsAffected = dataReader.RecordsAffected;
                }
                catch (Exception ex)
                {
                    if (ct.IsCancellationRequested)
                    {
                        response.Status = QueryResponseStatus.Canceled;
                        response.Columns.Clear();
                        response.Rows.Clear();
                    }
                    else
                    {
                        response.Status = QueryResponseStatus.Failed;
                        response.Error = ex.Message;
                        if (int.TryParse(ex.Data["Position"]?.ToString(), out int position))
                        {
                            response.ErrorPosition = position;
                        }
                    }
                }
                finally
                {
                    response.Elapsed = sw.ElapsedMilliseconds;
                    await IgnoreErrorsAsync(() => QueryHistoryManager.Save(context, response));
                }

                return response;
            }, dbCommand => dbCommand.CommandTimeout = 0, cancellationToken);
        }

        private static CancellationToken CreateToken(string queryId)
        {
            if (TokensByQueryId.ContainsKey(queryId))
            {
                throw new Exception("A query is already being executed.");
            }

            var cts = new CancellationTokenSource();
            TokensByQueryId[queryId] = cts;
            return cts.Token;
        }

        private static bool ReleaseToken(string queryId)
        {
            if (TokensByQueryId.TryGetValue(queryId, out var cts))
            {
                cts.Dispose();
                return TokensByQueryId.Remove(queryId);
            }

            return false;
        }
    }
}
