export interface QueryResponseContext {
    id: string,
    success: boolean,
    error: string,
    elapsed: number,
    columns: Array<any>,
    rows: Array<any>,
    rowCount: number
}