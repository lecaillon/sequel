export interface DataGridColumnDefinition {
    colId: string,
    headerName: string,
    field: string,
    sqlType: string,
    headerTooltip: string,
    sortable: boolean,
    filter: any,
    editable: boolean,
    resizable: boolean,
    width?: number,
    cellClass?: string,
}