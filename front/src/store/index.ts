import Vue from "vue";
import Vuex from "vuex";
import { http } from "@/core/http";
import { BASE_URL } from "@/appsettings"
import { ServerConnection } from "@/models/serverConnection";
import { AppSnackbar } from '@/models/appSnackbar';

Vue.use(Vuex);

export default new Vuex.Store({
  state: {
    appSnackbar: {} as AppSnackbar,
    servers: [] as ServerConnection[],
    activeServer: {} as ServerConnection,
    editServer: {} as ServerConnection,
  },
  actions: {
    showAppSnackbar: (context, appSnackbar: AppSnackbar) => {
      appSnackbar.show = true;
      context.commit("setAppSnackbar", appSnackbar);
    },
    hideAppSnackbar: (context) => {
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
    testServer: async (context, server) => {
      await http.post<void>(`${BASE_URL}/sequel/server-connection/test`, server);
      context.dispatch("showAppSnackbar", { message: "Database connection succeeded.", color: "success" } as AppSnackbar);
    },
    changeActiveServer: (context, server) => {
      context.commit("setActiveServer", server);
    },
    changeEditServer: (context, server) => {
      context.commit("setEditServer", server);
    },
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
  },
  modules: {}
});
