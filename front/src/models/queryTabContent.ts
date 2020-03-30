import * as monaco from "monaco-editor/esm/vs/editor/editor.api";

export interface QueryTabContent {
    id: number,
    name: string,
    editor: monaco.editor.IStandaloneCodeEditor
}