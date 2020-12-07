﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
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
                DBMS.SQLServer => new SqlConnection(server.ConnectionString),
                _ => throw new NotSupportedException($"Unsupported database {server.Type}.")
            };
        }

        public static async Task Validate(this ServerConnection server)
        {
            using var dbConnection = server.CreateConnection();

            await dbConnection.OpenAsync();
            await dbConnection.CloseAsync();
        }

        public static DatabaseSystem GetDatabaseSystem(this ServerConnection server)
        {
            return server.Type switch
            {
                DBMS.SQLServer => new SqlServer(server),
                DBMS.PostgreSQL => new PostgreSQL(server),
                DBMS.SQLite => new SQLite(server),
                _ => throw new NotSupportedException($"Unsupported database system {server.Type}.")
            };
        }

        public static async Task<IEnumerable<string>> QueryStringList(this ServerConnection server, string? database, string sql)
        {
            return await Execute(server, database, sql, async (dbCommand, ct) =>
            {
                var list = new List<string>();
                using var dataReader = await dbCommand.ExecuteReaderAsync(ct);
                while (await dataReader.ReadAsync(ct))
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

        public static async Task<IEnumerable<string>> QueryStringList(this ServerConnection server, string sql) 
            => await QueryStringList(server, null, sql);

        public static async Task<string?> QueryForString(this ServerConnection server, string? database, string sql)
        {
            return await Execute(server, database, sql, async (dbCommand, ct) =>
            {
                return Convert.ToString(await dbCommand.ExecuteScalarAsync(ct));
            });
        }

        public static async Task<string?> QueryForString(this ServerConnection server, string sql) => await QueryForString(server, null, sql);

        public static async Task<long> QueryForLong(this ServerConnection server, string? database, string sql)
        {
            return await Execute(server, database, sql, async (dbCommand, ct) =>
            {
                return Convert.ToInt64(await dbCommand.ExecuteScalarAsync(ct));
            });
        }

        public static async Task<long> QueryForLong(this ServerConnection server, string sql) => await QueryForLong(server, null, sql);

        public static async Task<bool> QueryForBool(this ServerConnection server, string? database, string sql)
        {
            return await Execute(server, database, sql, async (dbCommand, ct) =>
            {
                return (bool)(await dbCommand.ExecuteScalarAsync(ct) ?? false);
            });
        }

        public static async Task<bool> QueryForBool(this ServerConnection server, string sql) => await QueryForBool(server, null, sql);

        public static async Task<IEnumerable<T>> QueryList<T>(this ServerConnection server, string? database, string sql, Func<IDataReader, T> map)
        {
            return await Execute(server, database, sql, async (dbCommand, ct) =>
            {
                var list = new List<T>();
                using var dataReader = await dbCommand.ExecuteReaderAsync(ct);
                while (await dataReader.ReadAsync(ct))
                {
                    list.Add(map(dataReader));
                }

                return list;
            });
        }

        public static async Task<IEnumerable<T>> QueryList<T>(this ServerConnection server, string sql, Func<IDataReader, T> map) => await QueryList(server, null, sql, map);

        public static async Task<T?> Query<T>(this ServerConnection server, string? database, string sql, Func<IDataReader, T> map)
            => (await QueryList(server, database, sql, map)).FirstOrDefault();

        public static async Task<T?> Query<T>(this ServerConnection server, string sql, Func<IDataReader, T> map)
            => (await QueryList(server, null, sql, map)).FirstOrDefault();

        public static async Task<int> ExecuteNonQuery(this ServerConnection server, string? database, string sql)
        {
            return await Execute(server, database, sql, async (dbCommand, ct) =>
            {
                return await dbCommand.ExecuteNonQueryAsync(ct);
            });
        }

        public static async Task<int> ExecuteNonQuery(this ServerConnection server, string sql) => await ExecuteNonQuery(server, null, sql);

        public static async Task<T> Execute<T>(this ServerConnection server,
                                                     string? database,
                                                     string sql,
                                                     Func<DbCommand, CancellationToken, Task<T>> query,                                                     
                                                     Action<IDbCommand>? setupDbCommand = null,
                                                     CancellationToken ct = default)
        {
            using var cnn = server.CreateConnection();
            await cnn.OpenAsync(ct);
            if (database != null && server.Type != DBMS.SQLite)
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
