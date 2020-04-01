<template>
  <v-app>
    <v-app-bar app clipped-left clipped-right>
      <v-app-bar-nav-icon @click.stop="showDbExplorer = !showDbExplorer"></v-app-bar-nav-icon>
      <v-toolbar-title>Sequel</v-toolbar-title>
      <v-spacer></v-spacer>
      <v-btn icon @click.stop="runQuery()">
        <v-icon>mdi-play-circle-outline</v-icon>
      </v-btn>
      <v-btn icon @click.stop="openNewQueryTab()">
        <v-icon>mdi-tab-plus</v-icon>
      </v-btn>
      <v-divider vertical inset />
      <v-btn icon>
        <v-icon>mdi-database-refresh</v-icon>
      </v-btn>
      <v-btn icon @click.stop="showDbProperty = !showDbProperty">
        <v-icon>mdi-wrench-outline</v-icon>
      </v-btn>
      <v-divider vertical inset />
      <v-btn icon @click.stop="openFormServerConnection(true)">
        <v-icon>mdi-server-plus</v-icon>
      </v-btn>
      <select-server-connection
        @edit="openFormServerConnection(false)"
        class="me-4"
        style="max-width: 500px"
      ></select-server-connection>
      <select-database style="max-width: 350px"></select-database>
    </v-app-bar>

    <v-navigation-drawer app clipped v-model="showDbExplorer" width="300" style="overflow: visible">
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
    closeAppSnackbar() {
      store.dispatch("hideAppSnackbar");
    },
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
    openNewQueryTab() {
      store.dispatch("openNewQueryTab");
    },
    runQuery() {
     console.log(store.getters.activeEditor.getValue())
    }
  },
  computed: {
    appSnackbar() {
      return store.state.appSnackbar;
    },
    editServer() {
      return store.state.editServer;
    }
  }
});
</script>
