import Vue from "vue";
import Vuex from "vuex";
import { http } from "@/core/http";
import { BASE_URL } from "@/appsettings"
import { ServerConnection } from "@/models/serverConnection";
import { AppSnackbar } from '@/models/appSnackbar';

Vue.use(Vuex);

export default new Vuex.Store({
  state: {
    servers: [] as ServerConnection[],
    activeServer: {} as ServerConnection,
    appSnackbar: {} as AppSnackbar
  },
  actions: {
    fetchServers: async context => {
      const servers = await http.get<ServerConnection[]>(`${BASE_URL}/sequel/server-connection`);
      context.commit("setServers", servers);
    },
    addServer: async (context, server) => {
      await http.post<void>(`${BASE_URL}/sequel/server-connection`, server);
      context.dispatch("fetchServers");
    },
    testServer: async (context, server) => {
      await http.post<void>(`${BASE_URL}/sequel/server-connection/test`, server);
    },
    changeActiveServer: (context, server) => {
      context.commit("setActiveServer", server);
    },
    displayAppSnackbar: (context, appSnackbar) => {
      context.commit("setAppSnackbar", appSnackbar);
    }
  },
  mutations: {
    setServers(state, servers) {
      state.servers = servers;
    },
    setActiveServer(state, server) {
      state.activeServer = server;
    },
    setAppSnackbar(state, appSnackbar) {
      state.appSnackbar = appSnackbar;
    }
  },
  modules: {}
});
