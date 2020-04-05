export interface QueryResponseContext {
    id: string,
    success: boolean,
    error: string,
    elapsed: string,
    columns: Array<any>,
    rows: Array<any>,
    rowCount: number
}