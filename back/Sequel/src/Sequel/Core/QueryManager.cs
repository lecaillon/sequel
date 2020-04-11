using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sequel.Models;

namespace Sequel.Core
{
    public static class QueryManager
    {
        private static readonly Dictionary<string, CancellationTokenSource> TokensByQueryId = new Dictionary<string, CancellationTokenSource>();

        public static async Task<QueryResponseContext> ExecuteQueryAsync(QueryExecutionContext context)
        {
            string queryId = Check.NotNull(context.Id, nameof(context.Id));
            var cancellationToken = CreateToken(queryId);

            try
			{
                return await context.ExecuteQueryAsync(cancellationToken);
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
