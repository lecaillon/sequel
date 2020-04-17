import { DataGridColumnDefinition } from "./dataGridColumnDefinition";

export interface QueryResponseContext {
    id: string,
    status: boolean,
    color: string,
    error?: string,
    errorPosition?: number,
    message: string,
    elapsed: number,
    columns: Array<DataGridColumnDefinition>,
    rows: Array<any>,
    rowCount: number
}