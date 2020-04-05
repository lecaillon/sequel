import { QueryResponseContext } from "./queryResponseContext";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";

export interface QueryTabContent {
    id: string,
    num: number,
    title: string,
    editor?: monaco.editor.IStandaloneCodeEditor,
    grid: QueryResponseContext
}