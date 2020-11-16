import { BASE_URL } from '@/appsettings';
import { http } from '@/core/http';
import { CompletionContext } from '@/models/completionContext';
import { QueryTabContent } from '@/models/queryTabContent';
import store from '@/store';
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";

export default class SequelIntellisenseProvider implements monaco.languages.CompletionItemProvider {
    triggerCharacters?: string[] | undefined = ['.', ' '];
    async provideCompletionItems(model: monaco.editor.ITextModel, position: monaco.Position, context: monaco.languages.CompletionContext, token: monaco.CancellationToken): Promise<monaco.languages.CompletionList> {
        let completionItems: monaco.languages.CompletionItem[] = [];
        if (store.getters.canExecuteQuery) {
            completionItems = await http.post<monaco.languages.CompletionItem[]>(`${BASE_URL}/sequel/completion-items`, {
                server: store.state.activeServer,
                database: store.state.activeDatabase,
                lineNumber: position.lineNumber,
                column: position.column,
                triggerCharacter: context.triggerCharacter,
                sql: (store.getters.activeQueryTab as QueryTabContent).editor?.getValue()
            } as CompletionContext);
        }
        return {
            suggestions: completionItems,
            dispose: function () { }
        };
    }
}