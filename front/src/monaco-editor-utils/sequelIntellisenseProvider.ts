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
        const word = model.getWordUntilPosition(position);
        const previousWord = model.getValueInRange({ startColumn: word.startColumn - 1, endColumn: word.startColumn, startLineNumber: position.lineNumber, endLineNumber: position.lineNumber })

        if (store.getters.canExecuteQuery) {
            if (context.triggerKind === monaco.languages.CompletionTriggerKind.TriggerCharacter || previousWord === '.') {
                completionItems = await http.post<monaco.languages.CompletionItem[]>(`${BASE_URL}/sequel/completion-items/intellisense`, {
                    server: store.state.activeServer,
                    database: store.state.activeDatabase,
                    lineNumber: position.lineNumber,
                    column: position.column,
                    triggerCharacter: context.triggerCharacter,
                    sql: (store.getters.activeQueryTab as QueryTabContent).editor?.getValue()
                } as CompletionContext);
            } else {
                const range = {
                    startLineNumber: position.lineNumber,
                    endLineNumber: position.lineNumber,
                    startColumn: word.startColumn,
                    endColumn: word.endColumn
                };

                completionItems = store.state.snippets;
                completionItems.forEach(x => (x.range = range));
            }
        }
        return {
            suggestions: completionItems,
            dispose: function () { }
        };
    }
}