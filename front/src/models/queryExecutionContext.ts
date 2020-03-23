import { ServerConnection } from "./serverConnection";
import { DatabaseObjectNode } from "./databaseObjectNode";

export interface QueryExecutionContext {
    server: ServerConnection;
    database: string;
    schema: string;
    databaseObject: DatabaseObjectNode;
    sql: string;
}