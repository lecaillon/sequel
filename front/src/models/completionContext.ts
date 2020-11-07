import { ServerConnection } from "./serverConnection";

export interface CompletionContext {
    server: ServerConnection;
    database: string;
    lineNumber: number;
    column: number;
    triggerCharacter?: string;
    sql?: string;
}