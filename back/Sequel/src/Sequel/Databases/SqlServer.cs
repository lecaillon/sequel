using System.Collections.Generic;
using System.Threading.Tasks;
using Sequel.Core;
using Sequel.Models;

namespace Sequel.Databases
{
    public class SqlServer : DatabaseSystem
    {
        private readonly ServerConnection _server;

        public SqlServer(ServerConnection server)
        {
            _server = Check.NotNull(server, nameof(server));
        }

        public override DBMS Type => DBMS.SQLServer;

        public override async Task<IEnumerable<string>> LoadDatabases()
            => await _server.QueryStringList("SELECT name FROM sys.databases ORDER BY name");

        protected override async Task<string?> GetCurrentSchema(string database)
            => await _server.QueryForString(database, "SELECT SCHEMA_NAME()");

        protected override async Task<IEnumerable<string>> LoadSchemas(string database)
            => await _server.QueryStringList(database,
                "SELECT s.name " +
                "FROM sys.schemas s " +
                "INNER JOIN sys.sysusers u ON u.uid = s.principal_id " +
                "WHERE u.issqluser = 1 " +
                "AND u.name NOT IN ('sys', 'guest', 'INFORMATION_SCHEMA')");

        protected override async Task<IEnumerable<string>> LoadTables(string database, string? schema)
        {
            Check.NotNullOrEmpty(schema, nameof(schema));

            return await _server.QueryStringList(database,
                 "SELECT table_name " +
                 "FROM INFORMATION_SCHEMA.TABLES " +
                 "WHERE table_type='BASE TABLE' " +
                $"AND table_schema = '{schema}' " +
                 "ORDER BY table_name");
        }

        protected override async Task<IEnumerable<string>> LoadViews(string database, string? schema)
        {
            Check.NotNullOrEmpty(schema, nameof(schema));

            return await _server.QueryStringList(database, 
                 "SELECT table_name " +
                 "FROM INFORMATION_SCHEMA.VIEWS " +
                $"WHERE table_schema = '{schema}' " +
                 "ORDER BY table_name");
        }

        protected override async Task<IEnumerable<string>> LoadFunctions(string database, string? schema)
        {
            Check.NotNullOrEmpty(schema, nameof(schema));

            return await _server.QueryStringList(database,
                 "SELECT routine_name " +
                 "FROM INFORMATION_SCHEMA.ROUTINES " +
                $"WHERE routine_schema = '{schema}' " +
                 "AND routine_type = 'FUNCTION' " +
                 "ORDER BY routine_name");
        }

        protected override async Task<IEnumerable<string>> LoadProcedures(string database, string? schema)
        {
            Check.NotNullOrEmpty(schema, nameof(schema));

            return await _server.QueryStringList(database,
                 "SELECT routine_name " +
                 "FROM INFORMATION_SCHEMA.ROUTINES " +
                $"WHERE routine_schema = '{schema}' " +
                 "AND routine_type = 'PROCEDURE' " +
                 "ORDER BY routine_name");
        }

        protected override async Task<IEnumerable<string>> LoadSequences(string database, string? schema)
        {
            Check.NotNull(schema, nameof(schema));

            return await _server.QueryStringList(database,
                "SELECT sequence_name " +
                "FROM INFORMATION_SCHEMA.SEQUENCES " +
               $"WHERE sequence_schema = '{schema}' " +
                "ORDER BY sequence_name");
        }

        protected override async Task<IEnumerable<string>> LoadTableColumns(string database, string? schema, string table)
        {
            Check.NotNull(schema, nameof(schema));

            return await _server.QueryStringList(database,
                "SELECT name " +
                "FROM sys.columns " +
               $"WHERE object_id = OBJECT_ID('{schema}.{table}') " +
                "ORDER BY name");
        }

        protected override async Task<IEnumerable<string>> LoadIndexes(string database, string? schema, string table)
        {
            Check.NotNull(schema, nameof(schema));

            return await _server.QueryStringList(database,
                "SELECT name " +
                "FROM sys.indexes " +
               $"WHERE object_id = OBJECT_ID('{schema}.{table}') " +
                "ORDER BY name");
        }

        protected override async Task<IEnumerable<string>> LoadPrimaryKeys(string database, string? schema, string table)
        {
            Check.NotNull(schema, nameof(schema));

            return await _server.QueryStringList(database,
                "SELECT column_name " +
                "FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC " +
                "INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU " +
                "ON TC.CONSTRAINT_TYPE = 'PRIMARY KEY' " +
                "AND TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME " +
               $"AND TC.CONSTRAINT_SCHEMA = '{schema}' " +
               $"AND KU.table_name = '{table}' " +
                "ORDER BY KU.TABLE_NAME, KU.ORDINAL_POSITION");
        }

        protected override async Task<IEnumerable<string>> LoadForeignKeys(string database, string? schema, string table)
        {
            Check.NotNull(schema, nameof(schema));

            return await _server.QueryStringList(database,
                "SELECT c.name " +
                "FROM sys.foreign_key_columns as fk " +
                "INNER JOIN sys.tables as t on fk.parent_object_id = t.object_id " +
                "INNER JOIN sys.columns as c on fk.parent_object_id = c.object_id " +
               $"AND t.object_id = OBJECT_ID('{schema}.{table}') " +
                "AND fk.parent_column_id = c.column_id " +
                "ORDER BY fk.constraint_column_id");
        }

        protected override async Task<IEnumerable<string>> LoadViewColumns(string database, string? schema, string view)
        {
            Check.NotNull(schema, nameof(schema));

            return await _server.QueryStringList(database,
                "SELECT name " +
                "FROM sys.columns " +
               $"WHERE object_id = OBJECT_ID('{schema}.{view}') " +
                "ORDER BY name");
        }
    }
}
