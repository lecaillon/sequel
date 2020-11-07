#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Sequel.Databases;
using Sequel.Models;

namespace Sequel.Core
{
    public static class SqlHelper
    {
        private static DbConnection CreateConnection(this ServerConnection server)
        {
            return server.Type switch
            {
                DBMS.PostgreSQL => new NpgsqlConnection(server.ConnectionString),
                DBMS.SQLite => new SQLiteConnection(server.ConnectionString),
                _ => throw new NotSupportedException($"Unsupported database {server.Type}.")
            };
        }

        public static async Task ValidateAsync(this ServerConnection server)
        {
            using var dbConnection = server.CreateConnection();

            await dbConnection.OpenAsync();
            await dbConnection.CloseAsync();
        }

        public static DatabaseSystem GetDatabaseSystem(this ServerConnection server)
        {
            return server.Type switch
            {
                DBMS.PostgreSQL => new PostgreSQL(server),
                DBMS.SQLite => new SQLite(server),
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

        public static async Task<string?> QueryForStringAsync(this ServerConnection server, string? database, string sql)
        {
            return await ExecuteAsync(server, database, sql, async (dbCommand, ct) =>
            {
                return Convert.ToString(await dbCommand.ExecuteScalarAsync());
            });
        }

        public static async Task<string?> QueryForStringAsync(this ServerConnection server, string sql) => await QueryForStringAsync(server, null, sql);

        public static async Task<long> QueryForLongAsync(this ServerConnection server, string? database, string sql)
        {
            return await ExecuteAsync(server, database, sql, async (dbCommand, ct) =>
            {
                return Convert.ToInt64(await dbCommand.ExecuteScalarAsync());
            });
        }

        public static async Task<long> QueryForLongAsync(this ServerConnection server, string sql) => await QueryForLongAsync(server, null, sql);

        public static async Task<bool> QueryForBoolAsync(this ServerConnection server, string? database, string sql)
        {
            return await ExecuteAsync(server, database, sql, async (dbCommand, ct) =>
            {
                return (bool)(await dbCommand.ExecuteScalarAsync() ?? false);
            });
        }

        public static async Task<bool> QueryForBoolAsync(this ServerConnection server, string sql) => await QueryForBoolAsync(server, null, sql);

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

        public static async Task<IEnumerable<T>> QueryListAsync<T>(this ServerConnection server, string sql, Func<IDataReader, T> map) => await QueryListAsync(server, null, sql, map);

        public static async Task<T> QueryAsync<T>(this ServerConnection server, string? database, string sql, Func<IDataReader, T> map)
            => (await QueryListAsync(server, database, sql, map)).FirstOrDefault();

        public static async Task<T> QueryAsync<T>(this ServerConnection server, string sql, Func<IDataReader, T> map)
            => (await QueryListAsync(server, null, sql, map)).FirstOrDefault();

        public static async Task<int> ExecuteNonQueryAsync(this ServerConnection server, string? database, string sql)
        {
            return await ExecuteAsync(server, database, sql, async (dbCommand, ct) =>
            {
                return await dbCommand.ExecuteNonQueryAsync();
            });
        }

        public static async Task<int> ExecuteNonQueryAsync(this ServerConnection server, string sql) => await ExecuteNonQueryAsync(server, null, sql);

        public static async Task<T> ExecuteAsync<T>(this ServerConnection server,
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
