#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
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

        public static async Task Validate(this ServerConnection server)
        {
            using var cnn = server.CreateConnection();

            await cnn.OpenAsync();
            await cnn.CloseAsync();
        }

        public static IDatabaseSystem GetDatabaseSystem(this ServerConnection server)
        {
            return server.Type switch
            {
                DBMS.PostgreSQL => new PostgreSQL(server),
                _ => throw new NotSupportedException($"Unsupported database system {server.Type}.")
            };
        }

        public static async Task<IEnumerable<string>> QueryForListOfString(this ServerConnection server, string? database, string sql)
        {
            return await Execute(server, database, sql, cmd =>
            {
                var list = new List<string>();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string? item = reader[0].ToString();
                    if (item != null)
                    {
                        list.Add(item);
                    }
                }

                return list;
            });
        }

        public static async Task<IEnumerable<string>> QueryForListOfString(this ServerConnection server, string sql) 
            => await QueryForListOfString(server, null, sql);

        public static async Task<long> QueryForLong(this ServerConnection server, string? database, string sql)
        {
            return await Execute(server, database, sql, cmd =>
            {
                return Convert.ToInt64(cmd.ExecuteScalar());
            });
        }

        public static async Task<long> QueryForLong(this ServerConnection server, string sql) 
            => await QueryForLong(server, null, sql);

        public static async Task<IEnumerable<T>> QueryForList<T>(this ServerConnection server, string? database, string sql, Func<IDataReader, T> map)
        {
            return await Execute(server, database, sql, cmd =>
            {
                var list = new List<T>();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(map(reader));
                }

                return list;
            });
        }

        public static async Task<IEnumerable<T>> QueryForList<T>(this ServerConnection server, string sql, Func<IDataReader, T> map)
            => await QueryForList(server, null, sql, map);

        public static async Task<QueryResponseContext> ExecuteQuery(this QueryExecutionContext context)
        {
            return await Execute(context.Server, context.Database, context.Sql!, cmd =>
            {
                var response = new QueryResponseContext(context.Id!);

                try
                {
                    using var reader = cmd.ExecuteReader();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        response.Columns.Add(new ColumnDefinition(reader.GetName(i), reader.GetDataTypeName(i)));
                    }

                    while (reader.Read())
                    {
                        var dataRow = new ExpandoObject() as IDictionary<string, object?>;
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var value = reader[i];
                            dataRow.Add(reader.GetName(i), value is DBNull ? null : value);
                        }
                        response.Rows.Add(dataRow);
                    }
                }
                catch (Exception ex)
                {
                    response.Success = false;
                    response.Error = ex.Message;
                }

                return response;
            });
        }

        private static async Task<T> Execute<T>(this ServerConnection server, string? database, string sql, Func<IDbCommand, T> query, Action<IDbCommand>? setupDbCommand = null)
        {
            using var cnn = server.CreateConnection();
            await cnn.OpenAsync();
            if (database != null)
            {
                await cnn.ChangeDatabaseAsync(database);
            }

            using var cmd = cnn.CreateCommand();
            cmd.CommandText = sql;
            setupDbCommand?.Invoke(cmd);

            return query(cmd);
        }
    }
}
