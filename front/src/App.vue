<template>
  <v-app>
    <v-app-bar app clipped-left clipped-right>
      <v-app-bar-nav-icon @click.stop="showDbExplorer = !showDbExplorer"></v-app-bar-nav-icon>
      <v-toolbar-title>Sequel</v-toolbar-title>
      <v-spacer></v-spacer>
      <v-tooltip bottom>
        <template v-slot:activator="{ on }">
          <v-btn
            icon
            :disabled="!hasActiveTab || !hasActiveDatabase || hasLoadingActiveTab"
            v-on="on"
            @click.stop="executeQuery()"
          >
            <v-icon color="green">mdi-play</v-icon>
          </v-btn>
        </template>
        <span>Execute</span>
      </v-tooltip>
      <v-tooltip bottom>
        <template v-slot:activator="{ on }">
          <v-btn
            icon
            :disabled="!hasActiveTab || !hasActiveDatabase || !hasLoadingActiveTab"
            v-on="on"
            @click.stop="cancelQuery()"
          >
            <v-icon color="red">mdi-stop</v-icon>
          </v-btn>
        </template>
        <span>Cancel</span>
      </v-tooltip>
      <v-tooltip bottom>
        <template v-slot:activator="{ on }">
          <v-btn icon v-on="on" @click.stop="openNewQueryTab()">
            <v-icon color="primary">mdi-tab-plus</v-icon>
          </v-btn>
        </template>
        <span>Open new tab</span>
      </v-tooltip>

      <v-divider vertical inset />
      <v-tooltip bottom>
        <template v-slot:activator="{ on }">
          <v-btn icon :disabled="!hasActiveNode" v-on="on">
            <v-icon color="grey lighten-2">mdi-database-refresh</v-icon>
          </v-btn>
        </template>
        <span>Todo: Refresh database tree node</span>
      </v-tooltip>
      <v-tooltip bottom>
        <template v-slot:activator="{ on }">
          <v-btn
            icon
            :disabled="!hasActiveNode"
            v-on="on"
            @click.stop="showDbProperty = !showDbProperty"
          >
            <v-icon color="grey lighten-2">mdi-wrench-outline</v-icon>
          </v-btn>
        </template>
        <span>Todo: Show database property panel</span>
      </v-tooltip>
      <v-divider vertical inset />
      <v-tooltip bottom>
        <template v-slot:activator="{ on }">
          <v-btn icon v-on="on" @click.stop="openFormServerConnection(true)">
            <v-icon color="primary">mdi-server-plus</v-icon>
          </v-btn>
        </template>
        <span>Add new server</span>
      </v-tooltip>
      <select-server-connection
        @edit="openFormServerConnection(false)"
        class="me-4"
        style="max-width: 450px"
      ></select-server-connection>
      <select-database style="max-width: 400px"></select-database>
    </v-app-bar>

    <v-navigation-drawer
      app
      clipped
      v-model="showDbExplorer"
      width="375"
      style="overflow: visible;background-color: #1E1E1E"
    >
      <database-object-treeview />
    </v-navigation-drawer>

    <v-navigation-drawer app clipped right v-model="showDbProperty">
      <v-list-item>
        <v-list-item-content>
          <v-list-item-title class="title">table1</v-list-item-title>
          <v-list-item-subtitle>Table</v-list-item-subtitle>
        </v-list-item-content>
      </v-list-item>
      <v-divider></v-divider>
    </v-navigation-drawer>

    <v-content>
      <database-query-manager></database-query-manager>
    </v-content>

    <v-footer app></v-footer>

    <form-server-connection
      :show="showFormServerConnection"
      :server="editServer"
      @close="showFormServerConnection = false"
    ></form-server-connection>

    <app-snackbar
      :show="appSnackbar.show"
      :color="appSnackbar.color"
      :message="appSnackbar.message"
      :details="appSnackbar.details"
      @close="closeAppSnackbar"
    ></app-snackbar>
  </v-app>
</template>

<script lang="ts">
import Vue from "vue";
import Vuetify from "vuetify";
import store from "@/store";
import FormServerConnection from "@/components/FormServerConnection.vue";
import SelectServerConnection from "@/components/SelectServerConnection.vue";
import SelectDatabase from "@/components/SelectDatabase.vue";
import DatabaseObjectTreeview from "@/components/DatabaseObjectTreeview.vue";
import DatabaseQueryManager from "@/components/DatabaseQueryManager.vue";
import AppSnackbar from "@/components/AppSnackbar.vue";
import { ServerConnection } from "./models/serverConnection";
import { QueryTabContent } from "./models/queryTabContent";

export default Vue.extend({
  name: "App",
  vuetify: new Vuetify({
    theme: { dark: true }
  }),
  components: {
    FormServerConnection,
    SelectServerConnection,
    SelectDatabase,
    DatabaseObjectTreeview,
    DatabaseQueryManager,
    AppSnackbar
  },
  data: () => ({
    showDbExplorer: true,
    showDbProperty: false,
    showFormServerConnection: false
  }),
  methods: {
    openFormServerConnection(newForm: boolean) {
      if (newForm) {
        store.dispatch("changeEditServer", {
          name: "",
          type: "",
          connectionString: "",
          environment: ""
        } as ServerConnection);
      }
      this.showFormServerConnection = true;
    },
    closeAppSnackbar: () => store.dispatch("hideAppSnackbar"),
    openNewQueryTab: () => store.dispatch("openNewQueryTab"),
    executeQuery: () =>
      store.dispatch(
        "executeQuery",
        store.getters.activeQueryTab as QueryTabContent
      ),
    cancelQuery: () =>
      store.dispatch(
        "cancelQuery",
        store.getters.activeQueryTab as QueryTabContent
      )
  },
  computed: {
    appSnackbar: () => store.state.appSnackbar,
    editServer: () => store.state.editServer,
    hasActiveTab: () => store.state.activeQueryTabIndex >= 0,
    hasLoadingActiveTab: () =>
      store.state.activeQueryTabIndex >= 0 &&
      (store.getters.activeQueryTab as QueryTabContent).loading,
    hasActiveDatabase: () => store.state.activeDatabase?.length > 0,
    hasActiveNode: () => Object.keys(store.state.activeNode).length > 0
  }
});
</script>
