#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Npgsql;
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

        public static async Task Validate(this ServerConnection server)
        {
            using var cnn = server.CreateConnection();

            await cnn.OpenAsync();
            await cnn.CloseAsync();
        }

        public static async Task<IEnumerable<string>> QueryForListOfString(this ServerConnection server, string sql)
        {
            return await Execute(server, sql, cmd =>
            {
                var list = new List<string>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string? item = reader[0].ToString();
                        if (item != null)
                        {
                            list.Add(item);
                        }
                    }
                }

                return list;
            });
        }

        private static async Task<T> Execute<T>(this ServerConnection server, string sql, Func<IDbCommand, T> query, Action<IDbCommand>? setupDbCommand = null)
        {
            using var cnn = server.CreateConnection();
            await cnn.OpenAsync();

            using var cmd = cnn.CreateCommand();
            cmd.CommandText = sql;
            setupDbCommand?.Invoke(cmd);

            return query(cmd);
        }
    }
}
