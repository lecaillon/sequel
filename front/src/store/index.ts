import Vue from "vue";
import Vuex from "vuex";
import { http } from '@/core/http';
import { ServerConnection } from '@/models/serverConnection';

Vue.use(Vuex);

export default new Vuex.Store({
  state: {
    servers: [] as ServerConnection[]
  },
  actions: {
    fetchServers: async (context) => {
      const servers = await http.get<ServerConnection[]>("http://localhost:5000/sequel/server-connection");
      context.commit("setServers", servers);
    }
  },
  mutations: {
    setServers(state, servers) {
      state.servers = servers;
    }
  },
  modules: {}
});
