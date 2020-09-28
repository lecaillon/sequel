import { ServerConnection } from "./serverConnection";
import { TreeViewNode } from "./treeViewNode";

export interface QueryExecutionContext {
    server: ServerConnection;
    database: string;
    node?: TreeViewNode;
    sql?: string;
    id?: string;
}