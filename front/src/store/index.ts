import Vue from "vue";
import Vuex from "vuex";
import { http } from "@/core/http";
import { BASE_URL } from "@/appsettings";
import { ServerConnection } from "@/models/serverConnection";
import { TreeViewNode } from "@/models/treeViewNode";
import { QueryExecutionContext } from "@/models/queryExecutionContext";
import { TreeViewContext } from "@/models/treeViewContext";
import { CompletionContext } from "@/models/completionContext";
import { QueryResponseContext } from "@/models/queryResponseContext";
import { AppSnackbar } from "@/models/appSnackbar";
import { QueryTabContent } from "@/models/queryTabContent";
import { QueryHistoryContent } from "@/models/queryHistoryContent";
import { QueryHistoryQuery } from "@/models/queryHistoryQuery";
import { TreeViewMenuItem } from "@/models/treeViewMenuItem";
import { v4 as uuidv4 } from "uuid";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";
import sqlFormatter from "sql-formatter";

Vue.use(Vuex);

export default new Vuex.Store({
  state: {
    appSnackbar: {} as AppSnackbar,
    servers: [] as ServerConnection[],
    activeServer: {} as ServerConnection,
    editServer: {} as ServerConnection,
    databases: [] as string[],
    activeDatabase: {} as string,
    nodes: [] as TreeViewNode[],
    activeNode: {} as TreeViewNode,
    activeNodeMenu: [] as TreeViewMenuItem[],
    activeQueryTabIndex: {} as number,
    queryTabs: [] as QueryTabContent[],
    history: {} as QueryHistoryContent,
    isQueryHistoryManagerOpened: false as boolean,
    snippets: [] as monaco.languages.CompletionItem[]
  },
  getters: {
    activeQueryTab: state => state.queryTabs[state.activeQueryTabIndex],
    getQueryTabById: state => (id: string) => state.queryTabs.find(x => x.id === id),
    hasActiveTab: state => state.activeQueryTabIndex >= 0,
    hasActiveDatabase: state => state.activeDatabase?.length > 0,
    hasActiveTabLoading: (state, getters) => getters.hasActiveTab && (getters.activeQueryTab as QueryTabContent)?.loading,
    hasActiveGrid: (state, getters) => getters.hasActiveTab && (getters.activeQueryTab as QueryTabContent).response.columns.length > 0,
    hasActiveNode: state => Object.keys(state.activeNode).length > 0,
    canExecuteQuery: (state, getters) => getters.hasActiveTab && getters.hasActiveDatabase && !getters.hasActiveTabLoading,
  },
  actions: {
    showAppSnackbar: (context, appSnackbar: AppSnackbar) => {
      appSnackbar.show = true;
      context.commit("setAppSnackbar", appSnackbar);
    },
    hideAppSnackbar: context => {
      context.commit("setAppSnackbar", { show: false } as AppSnackbar);
    },
    showQueryHistoryManager: (context, show: boolean) => {
      context.commit("setIsQueryHistoryManagerOpened", show);
    },
    fetchServers: async context => {
      const servers = await http.get<ServerConnection[]>(`${BASE_URL}/sequel/server-connections`);
      context.commit("setServers", servers);
    },
    addServer: async (context, server: ServerConnection) => {
      await http.post<void>(`${BASE_URL}/sequel/server-connections`, server);
      context.dispatch("fetchServers");
      context.dispatch("showAppSnackbar", { message: "New database connection added.", color: "success" } as AppSnackbar);
    },
    deleteServer: async (context, serverId: number) => {
      await http.delete<void>(`${BASE_URL}/sequel/server-connections/${serverId}`);
      context.dispatch("fetchServers");
      context.dispatch("showAppSnackbar", { message: "Database connection deleted.", color: "success" } as AppSnackbar);
    },
    testServer: async (context, server: ServerConnection) => {
      await http.post<void>(`${BASE_URL}/sequel/server-connections/test`, server);
      context.dispatch("showAppSnackbar", { message: "Database connection succeeded.", color: "success" } as AppSnackbar);
    },
    changeActiveServer: (context, server: ServerConnection) => {
      context.commit("setActiveServer", server);
      context.dispatch("fetchDatabases", server);
    },
    changeEditServer: (context, server: ServerConnection) => {
      context.commit("setEditServer", server);
    },
    fetchDatabases: async context => {
      if (context.state.activeServer === undefined) {
        context.commit("setDatabases", []);
      } else {
        const databases = await http.post<string[]>(`${BASE_URL}/sequel/databases`, context.state.activeServer);
        context.commit("setDatabases", databases);
      }
      context.dispatch("changeActiveDatabase");
    },
    changeActiveDatabase: (context, database: string) => {
      context.commit("setActiveDatabase", database);
      context.dispatch("fetchTreeViewNodes");
      context.dispatch("fetchSnippets");
      if (database && context.state.queryTabs.length == 0) {
        context.dispatch("openNewQueryTab");
      }
    },
    fetchSnippets: async context => {
      if (context.state.activeDatabase !== undefined) {
        const snippets = await http.post<monaco.languages.CompletionItem[]>(`${BASE_URL}/sequel/completion-items/snippet`, {
          server: context.state.activeServer,
          database: context.state.activeDatabase,
          lineNumber: 1,
          column: 1
        } as CompletionContext);
        context.commit("setSnippets", snippets);
      } else {
        context.commit("setSnippets", new Array<monaco.languages.CompletionItem>());
      }
    },
    fetchHistory: async (context, query: QueryHistoryQuery) => {
      try {
        context.commit("setHistory", { loading: true } as QueryHistoryContent);
        const params = new URLSearchParams(Object.entries(query));
        const response = await http.get<QueryResponseContext>(`${BASE_URL}/sequel/history?` + params.toString());
        context.commit("setHistory", { response: { columns: response.columns, rows: response.rows }, loading: false } as QueryHistoryContent);
      }
      catch (Error) {
        context.commit("setHistory", { response: { columns: new Array<any>(), rows: new Array<any>() }, loading: false } as QueryHistoryContent);
      }
    },
    fetchTreeViewNodes: async (context, parent: TreeViewNode) => {
      if (context.state.activeDatabase === undefined || Object.keys(context.state.activeDatabase).length === 0) {
        context.commit("clearNodes");
      } else {
        const nodes = await http.post<TreeViewNode[]>(`${BASE_URL}/sequel/nodes`, {
          server: context.state.activeServer,
          database: context.state.activeDatabase,
          node: parent === undefined ? null : parent
        } as TreeViewContext);
        context.commit("pushNodes", { parent, nodes });
      }
    },
    fetchActiveNodeMenuItems: async (context) => {
      if (context.state.activeNode === undefined || Object.keys(context.state.activeNode).length === 0) {
        context.commit("setActiveNodeMenuItems", null);
      } else {
        const menuItems = await http.post<TreeViewNode[]>(`${BASE_URL}/sequel/nodes/${context.state.activeNode.id}/menu-items`, {
          server: context.state.activeServer,
          database: context.state.activeDatabase,
          node: context.state.activeNode
        } as TreeViewContext);
        context.commit("setActiveNodeMenuItems", menuItems);
      }
    },
    changeActiveNode: (context, node: TreeViewNode) => {
      context.commit("setActiveNode", node);
    },
    openNewQueryTab: context => {
      const num = Math.max(...context.state.queryTabs.map(x => x.num), 0) + 1;
      const index = context.state.queryTabs.length;
      context.commit("pushQueryTab", { id: uuidv4(), num, title: `query${num}`, response: { columns: new Array<any>(), rows: new Array<any>() }, loading: false } as QueryTabContent);
      context.dispatch("changeActiveQueryTab", index);
    },
    closeQueryTab: (context, index: number) => {
      context.commit("removeQueryTab", index);
    },
    changeActiveQueryTab: (context, index: number) => {
      if (index !== undefined) {
        context.commit("setActiveQueryTabIndex", index);
      }
    },
    updateQueryTabContent: (context, tab: QueryTabContent) => {
      context.commit("mergeQueryTabContent", tab);
    },
    executeQuery: async (context, statementIndex?: number) => {
      if (!context.getters.canExecuteQuery) {
        return;
      }

      const tab = context.getters.activeQueryTab as QueryTabContent;
      context.commit("mergeQueryTabContent", { id: tab.id, loading: true } as QueryTabContent);
      const sql = tab.editor?.getSelection()?.isEmpty() || statementIndex !== undefined
        ? tab.editor?.getValue()
        : tab.editor?.getModel()?.getValueInRange(tab.editor!.getSelection()!);

      try {
        const response = await http.post<QueryResponseContext>(`${BASE_URL}/sequel/execute-query`, {
          server: context.state.activeServer,
          database: context.state.activeDatabase,
          sql: sql,
          id: tab.id,
          statementIndex: statementIndex
        } as QueryExecutionContext);
        context.commit("mergeQueryTabContent", { id: response.id, response: { columns: response.columns, rows: response.rows }, loading: false } as QueryTabContent);
        context.dispatch("showAppSnackbar", { message: response.message, color: response.color } as AppSnackbar);

        const model = tab.editor?.getModel();
        if (response.errorPosition != null && model != null) {
          const pos = model.getPositionAt(response.errorPosition);
          let endColumn = pos.column;
          let char = "";
          while (char != " " && char != "," && endColumn <= model.getLineLastNonWhitespaceColumn(pos.lineNumber)) {
            endColumn++;
            char = model.getValueInRange({ startLineNumber: pos.lineNumber, startColumn: endColumn - 1, endLineNumber: pos.lineNumber, endColumn });
          }
          monaco.editor.setModelMarkers(tab.editor?.getModel()!, "sql", [{
            startLineNumber: pos.lineNumber,
            startColumn: pos.column - 1,
            endLineNumber: pos.lineNumber,
            endColumn: endColumn - 1,
            message: response.error!,
            severity: monaco.MarkerSeverity.Error
          }]);
        }
      }
      catch (Error) {
        context.commit("mergeQueryTabContent", { id: tab.id, response: { columns: new Array<any>(), rows: new Array<any>() }, loading: false } as QueryTabContent);
      }
    },
    cancelQuery: async context => {
      const tab = context.getters.activeQueryTab as QueryTabContent;
      await http.post<QueryResponseContext>(`${BASE_URL}/sequel/cancel-query`, tab.id);
    },
    pasteSqlInActiveTab: (context, sql: string) => {
      (context.getters.activeQueryTab as QueryTabContent)?.editor?.trigger('keyboard', 'type', { text: sql + ' ' });
    },
    updateHistoryFavorite: async (context, queryHistory: { code: string, star: boolean }) => {
      await http.post<void>(`${BASE_URL}/sequel/history/favorites/${queryHistory.code}`, { star: queryHistory.star });
      const msg = queryHistory.star ? "Added to the favorites" : "Removed from the favorites";
      context.dispatch("showAppSnackbar", { message: msg, color: "success" } as AppSnackbar);
    },
    updateHistoryName: async (context, queryHistory: { code: string, name: string }) => {
      await http.post<void>(`${BASE_URL}/sequel/history/names/${queryHistory.code}`, { name: queryHistory.name });
      context.dispatch("showAppSnackbar", { message: "Query name updated", color: "success" } as AppSnackbar);
    },
    updateHistoryKeywords: async (context, queryHistory: { code: string, keywords: string[] }) => {
      await http.post<void>(`${BASE_URL}/sequel/history/keywords/${queryHistory.code}`, { keywords: queryHistory.keywords });
      context.dispatch("showAppSnackbar", { message: "Query topics updated", color: "success" } as AppSnackbar);
    },
    executeTreeViewMenuItem: async (context, item: TreeViewMenuItem) => {
      if (!context.getters.hasActiveTab || (context.getters.activeQueryTab as QueryTabContent)?.editor?.getValue()) {
        await context.dispatch("openNewQueryTab");
      }
      (context.getters.activeQueryTab as QueryTabContent)?.editor?.setValue(item.command);
      await context.dispatch("executeQuery");
    },
    formatQuery: context => {
      const editor = (context.getters.activeQueryTab as QueryTabContent).editor!;
      const sql = editor.getSelection()?.isEmpty()
        ? editor.getValue()
        : editor.getModel()?.getValueInRange(editor.getSelection()!);
      if (!sql) {
        return;
      }
      const range = editor.getSelection()?.isEmpty()
        ? editor.getModel()!.getFullModelRange()
        : editor.getSelection()!;
      const op = {
        range: range,
        text: sqlFormatter.format(sql!),
        forceMoveMarkers: true
      } as monaco.editor.IIdentifiedSingleEditOperation;

      editor.executeEdits("sequel-source", [op]);
      editor.focus();
    }
  },
  mutations: {
    setAppSnackbar(state, appSnackbar: AppSnackbar) {
      state.appSnackbar = appSnackbar;
    },
    setIsQueryHistoryManagerOpened(state, show: boolean) {
      state.isQueryHistoryManagerOpened = show;
    },
    setServers(state, servers: ServerConnection[]) {
      state.servers = servers;
    },
    setActiveServer(state, server: ServerConnection) {
      state.activeServer = server;
    },
    setEditServer(state, server: ServerConnection) {
      state.editServer = server;
    },
    setDatabases(state, databases: string[]) {
      state.databases = databases;
    },
    setActiveDatabase(state, database: string) {
      state.activeDatabase = database;
    },
    setSnippets(state, snippets: monaco.languages.CompletionItem[]) {
      state.snippets = snippets;
    },
    setHistory(state, history: QueryHistoryContent) {
      if (history.loading) {
        state.history.loading = true;
        return;
      }
      state.history = history;
    },
    clearNodes(state) {
      state.nodes = [];
      state.activeNode = {} as TreeViewNode;
    },
    pushNodes(state, { parent, nodes }) {
      if (parent === undefined) {
        state.nodes = nodes;
      } else {
        (parent as TreeViewNode).children.push(...nodes);
      }
    },
    setActiveNodeMenuItems(state, menu: TreeViewMenuItem[]) {
      state.activeNodeMenu = menu;
    },
    setActiveNode(state, node: TreeViewNode) {
      state.activeNode = node;
    },
    pushQueryTab(state, queryTab: QueryTabContent) {
      state.queryTabs.push(queryTab);
    },
    removeQueryTab(state, index: number) {
      state.activeQueryTabIndex = -1; // force refresh of the tab content
      state.queryTabs.splice(index, 1);
    },
    setActiveQueryTabIndex(state, index: number) {
      state.activeQueryTabIndex = index;
    },
    mergeQueryTabContent(state, tab: QueryTabContent) {
      const tabToUpdate = state.queryTabs.find(x => x.id === tab.id);
      if (tabToUpdate) {
        if (tab.editor) {
          tabToUpdate.editor = tab.editor;
        }
        if (tab.response) {
          tabToUpdate.response = tab.response;
        }
        if (tab.loading !== undefined) {
          tabToUpdate.loading = tab.loading;
        }
        if (tab.grid) {
          tabToUpdate.grid = tab.grid;
        }
      }
    }
  }
});
