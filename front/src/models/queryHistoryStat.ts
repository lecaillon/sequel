export interface QueryHistoryStat {
    status: string,
    executionCount: number,
    environment: string,
    database: string,
    serverConnection: string,
    elapsed: number,
    rowCount: number,
    recordsAffected: number,
}