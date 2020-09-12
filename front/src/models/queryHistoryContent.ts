import { QueryResponseContext } from "./queryResponseContext";

export interface QueryHistoryContent {
    response: QueryResponseContext,
    loading: boolean
}