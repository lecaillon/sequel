<template>
  <v-app>
    <v-app-bar app clipped-left clipped-right>
      <v-app-bar-nav-icon @click.stop="showDbExplorer = !showDbExplorer"></v-app-bar-nav-icon>
      <v-toolbar-title>Sequel</v-toolbar-title>
      <v-spacer></v-spacer>
      <v-btn icon>
        <v-icon>mdi-play-circle-outline</v-icon>
      </v-btn>
      <v-btn icon @click.stop="showMonaco('1')">
        <v-icon>mdi-tab-plus</v-icon>
      </v-btn>
      <v-divider vertical inset />
      <v-btn icon @click.stop="resize()">
        <v-icon>mdi-database-refresh</v-icon>
      </v-btn>
      <v-btn icon @click.stop="showDbProperty = !showDbProperty">
        <v-icon>mdi-wrench-outline</v-icon>
      </v-btn>
      <v-divider vertical inset />
      <v-btn icon @click.stop="openFormServerConnection(true)">
        <v-icon>mdi-server-plus</v-icon>
      </v-btn>
      <SelectServerConnection
        @edit="openFormServerConnection(false)"
        class="me-4"
        style="max-width: 500px"
      ></SelectServerConnection>
      <SelectDatabase style="max-width: 350px"></SelectDatabase>
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
      <v-container fluid class="pa-0" fill-height>
        <v-tabs v-model="activeTab">
          <v-tab>Item One</v-tab>
          <v-tab>Item Two</v-tab>
        </v-tabs>

        <v-container fluid class="pa-0" style="height:60%">
          <v-tabs-items v-model="activeTab" class="pt-2" style="height: 100%">
            <v-tab-item style="height: 100%">
              <v-sheet tile id="monaco-1" style="height:100%"></v-sheet>
            </v-tab-item>
            <v-tab-item style="height: 100%">
              <v-sheet tile id="monaco-2" style="height:100%"></v-sheet>
            </v-tab-item>
          </v-tabs-items>
        </v-container>

        <v-container fluid class="pa-0" style="height:40%">
          <v-sheet tile style="height:100%;background-color:grey"></v-sheet>
        </v-container>
      </v-container>
    </v-content>

    <v-footer app></v-footer>

    <FormServerConnection
      :show="showFormServerConnection"
      :server="editServer"
      @close="showFormServerConnection = false"
    ></FormServerConnection>

    <AppSnackbar
      :show="appSnackbar.show"
      :color="appSnackbar.color"
      :message="appSnackbar.message"
      :details="appSnackbar.details"
      @close="closeAppSnackbar"
    ></AppSnackbar>
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
import AppSnackbar from "@/components/AppSnackbar.vue";
import { ServerConnection } from "./models/serverConnection";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";

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
    AppSnackbar
  },
  data: () => ({
    editor: {} as monaco.editor.IStandaloneCodeEditor,
    activeTab: null,
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
    showMonaco(id: string) {
      this.editor = monaco.editor.create(
        document.getElementById("monaco-" + id)!,
        {
          value: "SELECT * FROM TABLE1",
          language: "sql",
          theme: "vs-dark"
        }
      );
    },
    resize() {
      this.editor.layout();
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
