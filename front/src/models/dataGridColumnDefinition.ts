export interface DataGridColumnDefinition {
    colId: string,
    headerName: string,
    field: string,
    sqlType: string,
    headerTooltip: string,
    sortable: boolean,
    filter: boolean,
    editable: boolean,
    resizable: boolean,
    width?: number,
    cellClass?: string,
}