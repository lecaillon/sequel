import { DataGridColumnDefinition } from "./dataGridColumnDefinition";

export interface QueryResponseContext {
    id: string,
    success: boolean,
    error?: string,
    message: string,
    elapsed: number,
    columns: Array<DataGridColumnDefinition>,
    rows: Array<any>,
    rowCount: number
}