using System.Text.Json.Serialization;

namespace Sequel
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DBMS
    {
        MySQL,
        MariaDB,
        Oracle,
        PostgreSQL,
        SQLite,
        SQLServer,
        Cassandra,
        CockroachDB
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Env
    {
        Development,
        Testing,
        Staging,
        UAT,
        Demo,
        Production
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DatabaseObjectType
    {
        GroupLabel,
        Database,
        Schema,
        Table,
        Function,
        Column,
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum QueryResponseStatus
    {
        Succeeded,
        Canceled,
        Failed,
    }
}
