import Vue from "vue";
import Vuex from "vuex";
import { http } from "@/core/http";
import { BASE_URL } from "@/appsettings";
import { ServerConnection } from "@/models/serverConnection";
import { DatabaseObjectNode } from "@/models/databaseObjectNode";
import { QueryExecutionContext } from "@/models/queryExecutionContext";
import { AppSnackbar } from "@/models/appSnackbar";

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
    activeNode: {} as DatabaseObjectNode
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
      const servers = await http.get<ServerConnection[]>(`${BASE_URL}/sequel/server-connection`);
      context.commit("setServers", servers);
    },
    addServer: async (context, server) => {
      await http.post<void>(`${BASE_URL}/sequel/server-connection`, server);
      context.dispatch("fetchServers");
      context.dispatch("showAppSnackbar", { message: "New database connection added.", color: "success" } as AppSnackbar);
    },
    deleteServer: async (context, serverId) => {
      await http.delete<void>(`${BASE_URL}/sequel/server-connection/${serverId}`);
      context.dispatch("fetchServers");
      context.dispatch("showAppSnackbar", { message: "Database connection deleted.", color: "success" } as AppSnackbar);
    },
    testServer: async (context, server) => {
      await http.post<void>(`${BASE_URL}/sequel/server-connection/test`, server);
      context.dispatch("showAppSnackbar", { message: "Database connection succeeded.", color: "success" } as AppSnackbar);
    },
    changeActiveServer: (context, server) => {
      context.commit("setActiveServer", server);
      context.dispatch("fetchDatabases", server);
    },
    changeEditServer: (context, server) => {
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
    changeActiveDatabase: (context, database) => {
      context.commit("setActiveDatabase", database);
      context.dispatch("fetchDatabaseObjectNodes");
    },
    fetchDatabaseObjectNodes: async context => {
      if (context.state.activeDatabase === undefined) {
        context.commit("setNodes", []);
      } else {
        const nodes = await http.post<DatabaseObjectNode[]>(`${BASE_URL}/sequel/database-objects`, {
          server: context.state.activeServer,
          database: context.state.activeDatabase
        } as QueryExecutionContext);
        context.commit("setNodes", nodes);
      }
    },
    changeActiveNode: (context, node) => {
      context.commit("setActiveNode", node);
    }
  },
  mutations: {
    setAppSnackbar(state, appSnackbar) {
      state.appSnackbar = appSnackbar;
    },
    setServers(state, servers) {
      state.servers = servers;
    },
    setActiveServer(state, server) {
      state.activeServer = server;
    },
    setEditServer(state, server) {
      state.editServer = server;
    },
    setDatabases(state, databases) {
      state.databases = databases;
    },
    setActiveDatabase(state, database) {
      state.activeDatabase = database;
    },
    setNodes(state, nodes) {
      state.nodes = nodes;
    },
    setActiveNode(state, node) {
      state.activeNode = node;
    }
  },
  modules: {}
});
