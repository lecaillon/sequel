export interface QueryHistoryQuery {
    dbms?: string,
    terms?: string[],
    showErrors: boolean,
    showFavorites: boolean,
    showNamedQueries: boolean,
}