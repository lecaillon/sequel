<template>
  <v-app>
    <v-app-bar app clipped-left clipped-right>
      <v-app-bar-nav-icon
        @click.stop="showDbExplorer = !showDbExplorer"
      ></v-app-bar-nav-icon>
      <v-toolbar-title>Sequel</v-toolbar-title>
      <v-spacer></v-spacer>
      <v-tooltip bottom>
        <template v-slot:activator="{ on }">
          <v-btn
            icon
            :disabled="!canExecuteQuery"
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
            :disabled="!hasActiveTabLoading"
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
          <v-btn
            icon
            :disabled="!hasActiveTab"
            v-on="on"
            @click.stop="formatQuery()"
          >
            <v-icon color="grey lighten-2">mdi-format-align-left</v-icon>
          </v-btn>
        </template>
        <span>Format query</span>
      </v-tooltip>

      <v-divider vertical inset />
      <v-tooltip bottom>
        <template v-slot:activator="{ on }">
          <v-btn icon v-on="on" @click.stop="openQueryHistoryManager()">
            <v-icon color="grey lighten-2">mdi-history</v-icon>
          </v-btn>
        </template>
        <span>Open history</span>
      </v-tooltip>
      <!-- <v-tooltip bottom>
        <template v-slot:activator="{ on }">
          <v-btn icon :disabled="!hasActiveNode" v-on="on">
            <v-icon color="grey lighten-2">mdi-database-refresh</v-icon>
          </v-btn>
        </template>
        <span>Todo: Refresh database tree node</span>
      </v-tooltip>-->
      <!-- <v-tooltip bottom>
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
      </v-tooltip>-->
      <v-tooltip bottom>
        <template v-slot:activator="{ on }">
          <v-btn
            icon
            :disabled="!canExportData"
            v-on="on"
            @click.stop="exportDataAsCsv()"
          >
            <v-icon color="grey lighten-2">mdi-file-download-outline</v-icon>
          </v-btn>
        </template>
        <span>Export data to CSV</span>
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
      :show="showQueryHistoryManager"
      @close="showQueryHistoryManager = false"
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
    showDbProperty: false,
    showFormServerConnection: false,
    showQueryHistoryManager: false,
    snippetProvider: {} as monaco.IDisposable,
    intellisenseProvider: {} as monaco.IDisposable
  }),
  methods: {
    openQueryHistoryManager() {
      this.showQueryHistoryManager = true;
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
    closeAppSnackbar: () => store.dispatch("hideAppSnackbar"),
    openNewQueryTab: () => store.dispatch("openNewQueryTab"),
    formatQuery: () =>
      store.dispatch(
        "formatQuery",
        store.getters.activeQueryTab as QueryTabContent
      ),
    executeQuery: () =>
      store.dispatch(
        "executeQuery",
        store.getters.activeQueryTab as QueryTabContent
      ),
    cancelQuery: () =>
      store.dispatch(
        "cancelQuery",
        store.getters.activeQueryTab as QueryTabContent
      ),
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
    canExportData: () => store.getters.hasActiveGrid
  },
  mounted() {
    this.intellisenseProvider = monaco.languages.registerCompletionItemProvider(
      "sql",
      {
        provideCompletionItems: (model, position) => {
          const word = model.getWordUntilPosition(position);
          const range = {
            startLineNumber: position.lineNumber,
            endLineNumber: position.lineNumber,
            startColumn: word.startColumn,
            endColumn: word.endColumn
          };
          const intellisense = Array.from(store.state.intellisense);
          intellisense.forEach(x => (x.range = range));
          return {
            suggestions: intellisense
          };
        }
      }
    );
    this.snippetProvider = monaco.languages.registerCompletionItemProvider(
      "sql",
      {
        provideCompletionItems: (model, position) => {
          const word = model.getWordUntilPosition(position);
          const range = {
            startLineNumber: position.lineNumber,
            endLineNumber: position.lineNumber,
            startColumn: word.startColumn,
            endColumn: word.endColumn
          };
          return {
            suggestions: [
              {
                label: "gb",
                kind: monaco.languages.CompletionItemKind.Snippet,
                insertText: "GROUP BY ",
                detail: "GROUP BY",
                range: range
              },
              {
                label: "hc*",
                kind: monaco.languages.CompletionItemKind.Snippet,
                insertText: "HAVING COUNT(*) ",
                detail: "HAVING COUNT(*)",
                range: range
              },
              {
                label: "ij",
                kind: monaco.languages.CompletionItemKind.Snippet,
                insertText: "INNER JOIN ",
                detail: "INNER JOIN",
                range: range
              },
              {
                label: "in",
                kind: monaco.languages.CompletionItemKind.Snippet,
                insertText: "IS NULL ",
                detail: "IS NULL",
                range: range
              },
              {
                label: "inn",
                kind: monaco.languages.CompletionItemKind.Snippet,
                insertText: "IS NOT NULL ",
                detail: "IS NOT NULL",
                range: range
              },
              {
                label: "ob",
                kind: monaco.languages.CompletionItemKind.Snippet,
                insertText: "ORDER BY ",
                detail: "ORDER BY",
                range: range
              },
              {
                label: "oj",
                kind: monaco.languages.CompletionItemKind.Snippet,
                insertText: "OUTER JOIN ",
                detail: "OUTER JOIN",
                range: range
              },
              {
                label: "s*",
                kind: monaco.languages.CompletionItemKind.Snippet,
                insertText: "SELECT * FROM ",
                detail: "SELECT * FROM",
                range: range
              },
              {
                label: "sc*",
                kind: monaco.languages.CompletionItemKind.Snippet,
                insertText: "SELECT COUNT(*) FROM ",
                detail: "SELECT COUNT(*) FROM",
                range: range
              },
              {
                label: "w",
                kind: monaco.languages.CompletionItemKind.Snippet,
                insertText: "WHERE ",
                detail: "WHERE",
                range: range
              }
            ] as monaco.languages.CompletionItem[]
          };
        }
      }
    );
  },
  beforeDestroy() {
    this.snippetProvider.dispose();
    this.intellisenseProvider.dispose();
  }
});
</script>
