import Vue from "vue";
import Vuex from "vuex";
import { http } from "@/core/http";
import { BASE_URL } from "@/appsettings";
import { ServerConnection } from "@/models/serverConnection";
import { DatabaseObjectNode } from "@/models/databaseObjectNode";
import { QueryExecutionContext } from "@/models/queryExecutionContext";
import { QueryResponseContext } from "@/models/queryResponseContext";
import { AppSnackbar } from "@/models/appSnackbar";
import { QueryTabContent } from "@/models/queryTabContent";
import { v4 as uuidv4 } from "uuid";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";

Vue.use(Vuex);

export default new Vuex.Store({
  state: {
    appSnackbar: {} as AppSnackbar,
    servers: [] as ServerConnection[],
    activeServer: {} as ServerConnection,
    editServer: {} as ServerConnection,
    databases: [] as string[],
    activeDatabase: {} as string,
    nodes: [] as DatabaseObjectNode[],
    activeNode: {} as DatabaseObjectNode,
    activeQueryTabIndex: {} as number,
    queryTabs: [] as QueryTabContent[],
    intellisense: [] as monaco.languages.CompletionItem[]
  },
  getters: {
    activeQueryTab: state => state.queryTabs[state.activeQueryTabIndex],
    getQueryTabById: state => (id: string) => state.queryTabs.find(x => x.id === id),
    hasActiveTab: state => state.activeQueryTabIndex >= 0,
    hasActiveDatabase: state => state.activeDatabase?.length > 0,
    hasActiveTabLoading: (state, getters) => getters.hasActiveTab && (getters.activeQueryTab as QueryTabContent)?.loading,
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
      context.dispatch("fetchDatabaseObjectNodes");
      context.dispatch("fetchIntellisense");
    },
    fetchIntellisense: async context => {
      if (context.state.activeDatabase !== undefined) {
        const intellisense = await http.post<monaco.languages.CompletionItem[]>(`${BASE_URL}/sequel/intellisense`, {
          server: context.state.activeServer,
          database: context.state.activeDatabase,
        } as QueryExecutionContext);
        context.commit("setIntellisense", intellisense);
      }
    },
    fetchDatabaseObjectNodes: async (context, parent: DatabaseObjectNode) => {
      if (context.state.activeDatabase === undefined) {
        context.commit("clearNodes");
      } else {
        const nodes = await http.post<DatabaseObjectNode[]>(`${BASE_URL}/sequel/database-objects`, {
          server: context.state.activeServer,
          database: context.state.activeDatabase,
          databaseObject: parent === undefined ? null : parent
        } as QueryExecutionContext);
        context.commit("pushNodes", { parent, nodes });
      }
    },
    changeActiveNode: (context, node: DatabaseObjectNode) => {
      context.commit("setActiveNode", node);
    },
    openNewQueryTab: context => {
      const num = Math.max(...context.state.queryTabs.map(x => x.num), 0) + 1;
      const index = context.state.queryTabs.length;
      context.commit("pushQueryTab", { id: uuidv4(), num, title: `query${num}`, grid: { columns: new Array<any>(), rows: new Array<any>() }, loading: false } as QueryTabContent);
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
    executeQuery: async (context, tab: QueryTabContent) => {
      context.commit("mergeQueryTabContent", { id: tab.id, loading: true } as QueryTabContent);
      const sql = tab.editor?.getSelection()?.isEmpty()
        ? tab.editor?.getValue()
        : tab.editor?.getModel()?.getValueInRange(tab.editor!.getSelection()!);

      try {
        const response = await http.post<QueryResponseContext>(`${BASE_URL}/sequel/execute-query`, {
          server: context.state.activeServer,
          database: context.state.activeDatabase,
          sql: sql,
          id: tab.id
        } as QueryExecutionContext);
        context.commit("mergeQueryTabContent", { id: response.id, grid: { columns: response.columns, rows: response.rows }, loading: false } as QueryTabContent);
        context.dispatch("showAppSnackbar", { message: response.message, color: response.color } as AppSnackbar);
      }
      catch (Error) {
        context.commit("mergeQueryTabContent", { id: tab.id, grid: { columns: new Array<any>(), rows: new Array<any>() }, loading: false } as QueryTabContent);
      }
    },
    cancelQuery: async (context, tab: QueryTabContent) => {
      await http.post<QueryResponseContext>(`${BASE_URL}/sequel/cancel-query`, tab.id);
    }
  },
  mutations: {
    setAppSnackbar(state, appSnackbar: AppSnackbar) {
      state.appSnackbar = appSnackbar;
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
    setIntellisense(state, intellisense: monaco.languages.CompletionItem[]) {
      state.intellisense = intellisense;
    },
    clearNodes(state) {
      state.nodes = [];
      state.activeNode = {} as DatabaseObjectNode;
    },
    pushNodes(state, { parent, nodes }) {
      if (parent === undefined) {
        state.nodes = nodes;
      } else {
        (parent as DatabaseObjectNode).children.push(...nodes);
      }
    },
    setActiveNode(state, node: DatabaseObjectNode) {
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
        if (tab.grid) {
          tabToUpdate.grid = tab.grid;
        }
        if (tab.loading !== undefined) {
          tabToUpdate.loading = tab.loading;
        }
      }
    }
  }
});
