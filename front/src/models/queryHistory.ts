import { QueryHistoryStat } from "./queryHistoryStat";

export interface QueryHistory {
    code: string,
    status: string,
    type: string,
    sql: string,
    star: boolean,
    executionCount: number,
    lastExecutedOn: Date,
    lastEnvironment: string,
    lastDatabase: string,
    name?: string,
    topics?: string[],
    stats: QueryHistoryStat[]
}