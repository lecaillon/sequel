<template>
  <v-app>
    <v-app-bar app clipped-left clipped-right dense>
      <v-app-bar-nav-icon
        @click.stop="showDbExplorer = !showDbExplorer"
      ></v-app-bar-nav-icon>
      <v-toolbar-title>Sequel</v-toolbar-title>
      <v-spacer></v-spacer>
      <v-divider vertical inset />
      <v-tooltip bottom>
        <template v-slot:activator="{ on }">
          <v-btn
            icon
            :disabled="!canExecuteQuery"
            v-on="on"
            @click.stop="executeQuery()"
          >
            <v-icon small color="green">mdi-play</v-icon>
          </v-btn>
        </template>
        <span>Execute</span>
      </v-tooltip>
      <v-tooltip bottom>
        <template v-slot:activator="{ on }">
          <v-btn
            icon
            :disabled="!hasActiveTabLoading"
            v-on="on"
            @click.stop="cancelQuery()"
          >
            <v-icon small color="red">mdi-stop</v-icon>
          </v-btn>
        </template>
        <span>Cancel</span>
      </v-tooltip>
      <v-tooltip bottom>
        <template v-slot:activator="{ on }">
          <v-btn icon v-on="on" @click.stop="openNewQueryTab()">
            <v-icon small color="primary">mdi-tab-plus</v-icon>
          </v-btn>
        </template>
        <span>Open new tab</span>
      </v-tooltip>

      <v-divider vertical inset />
      <v-tooltip bottom>
        <template v-slot:activator="{ on }">
          <v-btn
            icon
            :disabled="!hasActiveTab"
            v-on="on"
            @click.stop="formatQuery()"
          >
            <v-icon small color="grey lighten-2">mdi-format-align-left</v-icon>
          </v-btn>
        </template>
        <span>Format query</span>
      </v-tooltip>

      <v-divider vertical inset />
      <v-tooltip bottom>
        <template v-slot:activator="{ on }">
          <v-btn icon v-on="on" @click.stop="openQueryHistoryManager()">
            <v-icon small color="grey lighten-2">mdi-history</v-icon>
          </v-btn>
        </template>
        <span>Open history</span>
      </v-tooltip>
      <v-tooltip bottom>
        <template v-slot:activator="{ on }">
          <v-btn
            icon
            :disabled="!canExportData"
            v-on="on"
            @click.stop="exportDataAsCsv()"
          >
            <v-icon small color="grey lighten-2"
              >mdi-file-download-outline</v-icon
            >
          </v-btn>
        </template>
        <span>Export data to CSV</span>
      </v-tooltip>
      <v-divider vertical inset />
      <v-tooltip bottom>
        <template v-slot:activator="{ on }">
          <v-btn icon v-on="on" @click.stop="openFormServerConnection(true)">
            <v-icon small color="primary">mdi-server-plus</v-icon>
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

    <v-main>
      <database-query-manager></database-query-manager>
    </v-main>

    <v-footer app></v-footer>

    <form-server-connection
      :show="showFormServerConnection"
      :server="editServer"
      @close="showFormServerConnection = false"
    />

    <query-history-manager
      :show="isQueryHistoryManagerOpened"
      @close="closeQueryHistoryManager"
    />

    <app-snackbar
      :show="appSnackbar.show"
      :color="appSnackbar.color"
      :message="appSnackbar.message"
      :details="appSnackbar.details"
      @close="closeAppSnackbar"
    />
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
import QueryHistoryManager from "@/components/QueryHistoryManager.vue";
import SequelCodeLensProvider from "@/monaco-editor-utils/sequelCodeLensProvider";
import SequelIntellisenseProvider from "@/monaco-editor-utils/sequelIntellisenseProvider";
import { ServerConnection } from "./models/serverConnection";
import { QueryTabContent } from "./models/queryTabContent";
import { CsvExportParams } from "ag-grid-community";
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
    DatabaseQueryManager,
    QueryHistoryManager,
    AppSnackbar
  },
  data: () => ({
    showDbExplorer: true,
    showFormServerConnection: false,
    intellisenseProvider: {} as monaco.IDisposable,
    codeLensProvider: {} as monaco.IDisposable
  }),
  methods: {
    openQueryHistoryManager: () =>
      store.dispatch("showQueryHistoryManager", true),
    closeQueryHistoryManager: () =>
      store.dispatch("showQueryHistoryManager", false),
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
    formatQuery: () => store.dispatch("formatQuery"),
    executeQuery: () => store.dispatch("executeQuery"),
    cancelQuery: () => store.dispatch("cancelQuery"),
    exportDataAsCsv: () =>
      (store.getters.activeQueryTab as QueryTabContent).grid?.exportDataAsCsv({
        allColumns: true
      } as CsvExportParams)
  },
  computed: {
    appSnackbar: () => store.state.appSnackbar,
    editServer: () => store.state.editServer,
    hasActiveTabLoading: () => store.getters.hasActiveTabLoading,
    hasActiveNode: () => store.getters.hasActiveNode,
    canExecuteQuery: () => store.getters.canExecuteQuery,
    hasActiveTab: () => store.getters.hasActiveTab,
    canExportData: () => store.getters.hasActiveGrid,
    isQueryHistoryManagerOpened: () => store.state.isQueryHistoryManagerOpened
  },
  mounted() {
    this.intellisenseProvider = monaco.languages.registerCompletionItemProvider(
      "sql",
      new SequelIntellisenseProvider()
    );
    this.codeLensProvider = monaco.languages.registerCodeLensProvider(
      "sql",
      new SequelCodeLensProvider()
    );

    // https://github.com/microsoft/monaco-editor/issues/1857#issuecomment-594457013
    const commandRegistry = require("monaco-editor/esm/vs/platform/commands/common/commands")
      .CommandsRegistry;
    commandRegistry.registerCommand(
      "CmdExecuteBlockStmt",
      (_: any, args: any[]) => {
        store.dispatch("executeQuery", args);
      }
    );
  },
  beforeDestroy() {
    this.intellisenseProvider.dispose();
    this.codeLensProvider.dispose();
  }
});
</script>
