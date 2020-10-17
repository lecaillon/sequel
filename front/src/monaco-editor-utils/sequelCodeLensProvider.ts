import { BASE_URL } from '@/appsettings';
import { http } from '@/core/http';
import { QueryExecutionContext } from '@/models/queryExecutionContext';
import { QueryTabContent } from '@/models/queryTabContent';
import store from '@/store';
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";

export default class SequelCodeLensProvider implements monaco.languages.CodeLensProvider {
    async provideCodeLenses(_model: monaco.editor.ITextModel, _token: monaco.CancellationToken): Promise<monaco.languages.CodeLensList> {
        let lenses: monaco.languages.CodeLens[] = [];
        if (!store.state.isQueryHistoryManagerOpened && store.getters.canExecuteQuery) {
            lenses = await http.post<monaco.languages.CodeLens[]>(`${BASE_URL}/sequel/codelenses`, {
                server: store.state.activeServer,
                database: store.state.activeDatabase,
                sql: (store.getters.activeQueryTab as QueryTabContent).editor?.getValue(),
            } as QueryExecutionContext);
        }
        return {
            lenses,
            dispose: function () { }
        };
    }
}