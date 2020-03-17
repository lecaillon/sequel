using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Npgsql;

namespace Sequel.Core
{
    public static class DatabaseHelper
    {
        public static DbConnection CreateConnection(this DBMS dbmsType, string cnnStr)
        {
            return dbmsType switch
            {
                DBMS.PostgreSQL => new NpgsqlConnection(cnnStr),
                _ => throw new NotSupportedException($"Unsupported database {dbmsType}.")
            };
        }

        public static async Task Validate(this DbConnection cnn)
        {
            if (cnn.State == ConnectionState.Open)
            {
                return;
            }

            await cnn.OpenAsync();
            await cnn.CloseAsync();
        }
    }
}
