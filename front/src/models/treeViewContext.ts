import { ServerConnection } from "./serverConnection";
import { TreeViewNode } from './treeViewNode';

export interface TreeViewContext {
    server: ServerConnection;
    database: string;
    node?: TreeViewNode;
}