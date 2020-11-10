import { ServerConnection } from "./serverConnection";

export interface QueryExecutionContext {
    server: ServerConnection;
    database: string;
    sql?: string;
    statementIndex?: number;
    id: string;
}