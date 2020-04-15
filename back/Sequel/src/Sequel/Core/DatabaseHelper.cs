#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Sequel.Databases;
using Sequel.Models;

namespace Sequel.Core
{
    public static class DatabaseHelper
    {
        private static DbConnection CreateConnection(this ServerConnection server)
        {
            return server.Type switch
            {
                DBMS.PostgreSQL => new NpgsqlConnection(server.ConnectionString),
                _ => throw new NotSupportedException($"Unsupported database {server.Type}.")
            };
        }

        public static async Task ValidateAsync(this ServerConnection server)
        {
            using var dbConnection = server.CreateConnection();

            await dbConnection.OpenAsync();
            await dbConnection.CloseAsync();
        }

        public static IDatabaseSystem GetDatabaseSystem(this ServerConnection server)
        {
            return server.Type switch
            {
                DBMS.PostgreSQL => new PostgreSQL(server),
                _ => throw new NotSupportedException($"Unsupported database system {server.Type}.")
            };
        }

        public static async Task<IEnumerable<string>> QueryStringListAsync(this ServerConnection server, string? database, string sql)
        {
            return await ExecuteAsync(server, database, sql, async (dbCommand, ct) =>
            {
                var list = new List<string>();
                using var dataReader = await dbCommand.ExecuteReaderAsync();
                while (await dataReader.ReadAsync())
                {
                    string? item = dataReader[0].ToString();
                    if (item != null)
                    {
                        list.Add(item);
                    }
                }

                return list;
            });
        }

        public static async Task<IEnumerable<string>> QueryStringListAsync(this ServerConnection server, string sql)
            => await QueryStringListAsync(server, null, sql);

        public static async Task<long> QueryForLongAsync(this ServerConnection server, string? database, string sql)
        {
            return await ExecuteAsync(server, database, sql, async (dbCommand, ct) =>
            {
                return Convert.ToInt64(await dbCommand.ExecuteScalarAsync());
            });
        }

        public static async Task<long> QueryForLongAsync(this ServerConnection server, string sql)
            => await QueryForLongAsync(server, null, sql);

        public static async Task<IEnumerable<T>> QueryListAsync<T>(this ServerConnection server, string? database, string sql, Func<IDataReader, T> map)
        {
            return await ExecuteAsync(server, database, sql, async (dbCommand, ct) =>
            {
                var list = new List<T>();
                using var dataReader = await dbCommand.ExecuteReaderAsync();
                while (await dataReader.ReadAsync())
                {
                    list.Add(map(dataReader));
                }

                return list;
            });
        }

        public static async Task<IEnumerable<T>> QueryListAsync<T>(this ServerConnection server, string sql, Func<IDataReader, T> map)
            => await QueryListAsync(server, null, sql, map);

        public static async Task<QueryResponseContext> ExecuteQueryAsync(this QueryExecutionContext context, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(context.Server, context.Database, context.Sql!, async (dbCommand, ct) =>
            {
                var response = new QueryResponseContext(context.Id!);
                var sw = new Stopwatch();

                try
                {
                    sw.Start();
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
                catch (TaskCanceledException)
                {
                    response.Columns.Clear();
                    response.Rows.Clear();
                    response.Status = QueryResponseStatus.Canceled;
                }
                catch (Exception ex)
                {
                    response.Status = QueryResponseStatus.Failed;
                    response.Error = ex.Message;
                }
                finally
                {
                    response.Elapsed = sw.ElapsedMilliseconds;
                }

                return response;
            }, dbCommand => dbCommand.CommandTimeout = 0, cancellationToken);
        }

        private static async Task<T> ExecuteAsync<T>(this ServerConnection server,
                                                     string? database,
                                                     string sql,
                                                     Func<DbCommand, CancellationToken, Task<T>> query,                                                     
                                                     Action<IDbCommand>? setupDbCommand = null,
                                                     CancellationToken ct = default)
        {
            using var cnn = server.CreateConnection();
            await cnn.OpenAsync(ct);
            if (database != null)
            {
                await cnn.ChangeDatabaseAsync(database, ct);
            }

            using var dbCommand = cnn.CreateCommand();
            dbCommand.CommandText = sql;
            setupDbCommand?.Invoke(dbCommand);

            return await query(dbCommand, ct);
        }
    }
}
