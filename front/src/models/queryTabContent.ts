import { QueryResponseContext } from "./queryResponseContext";
import { GridApi } from "ag-grid-community";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";

export interface QueryTabContent {
    id: string,
    num: number,
    title: string,
    editor?: monaco.editor.IStandaloneCodeEditor,
    grid?: GridApi,
    response: QueryResponseContext,
    loading: boolean
}