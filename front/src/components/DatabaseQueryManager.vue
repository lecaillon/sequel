<template>
  <v-container fluid class="pa-0" fill-height>
    <v-tabs :value="activeTab" @change="selected">
      <v-tab v-for="(tab, index) in queryTabs" :key="tab.id">
        <query-tab
          :title="tab.title"
          :index="index"
          @close="closeTab"
        ></query-tab>
      </v-tab>
    </v-tabs>
    <v-container fluid class="pa-0" style="height:100%">
      <v-tabs-items class="pt-3" :value="activeTab" style="height: 100%">
        <v-tab-item v-for="tab in queryTabs" :key="tab.id" style="height: 100%">
          <v-container fluid class="pa-0" style="height:45%">
            <query-editor
              :editorId="tab.id"
              @created="editorCreated"
              @keyPressedF5="editorKeyPressedF5"
              @keyPressedF6="editorKeyPressedF6"
            ></query-editor>
          </v-container>
          <v-container fluid class="pa-0" style="height:calc(55% - 48px)">
            <data-grid
              :gridId="tab.id"
              :columns="tab.response.columns"
              :rows="tab.response.rows"
              :loading="tab.loading"
              @created="gridCreated"
              @cell-focused="gridCellfocused"
            ></data-grid>
          </v-container>
        </v-tab-item>
      </v-tabs-items>
    </v-container>
    <v-dialog
      id="jsoneditor-modal"
      :value="showJsonEditor"
      @click:outside="closeJsonEditor"
      @keydown.esc="closeJsonEditor"
      content-class="v-dialog-jsoneditor"
      max-width="600px"
    >
      <v-card style="height:100%">
        <v-card-text class="pa-0" style="height:100%">
          <json-editor :json="jsonb" :options="jsonEditorOptions"></json-editor>
        </v-card-text>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<script lang="ts">
import Vue from "vue";
import store from "@/store";
import QueryTab from "@/components/QueryTab.vue";
import QueryEditor from "@/components/QueryEditor.vue";
import DataGrid from "@/components/DataGrid.vue";
import JsonEditor from "@/components/JsonEditor.vue";
import { QueryTabContent } from "@/models/queryTabContent";
import { DataGridColumnDefinition } from "@/models/dataGridColumnDefinition";
import { GridApi } from "ag-grid-community";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";

export default Vue.extend({
  name: "DatabaseQueryManager",
  components: {
    QueryTab,
    QueryEditor,
    DataGrid,
    JsonEditor
  },
  data: () => ({
    showJsonEditor: false,
    jsonb: {} as any,
    jsonEditorOptions: {
      mode: "tree",
      search: true,
      caseSensitive: false,
      mainMenuBar: true,
      navigationBar: true,
      statusBar: false,
      modalAnchor: document.getElementById("jsoneditor-modal")
    }
  }),
  methods: {
    selected(activeTab: number) {
      store.dispatch("changeActiveQueryTab", activeTab);
      const editor = (store.getters.activeQueryTab as QueryTabContent).editor;
      setTimeout(() => {
        editor?.layout();
        editor?.focus();
      }, 100);
    },
    closeTab(activeTab: number) {
      store.dispatch("closeQueryTab", activeTab);
    },
    editorCreated(id: string, editor: monaco.editor.IStandaloneCodeEditor) {
      store.dispatch("updateQueryTabContent", {
        id,
        editor
      } as QueryTabContent);
    },
    gridCreated(id: string, grid: GridApi) {
      store.dispatch("updateQueryTabContent", {
        id,
        grid
      } as QueryTabContent);
    },
    gridCellfocused(grid: GridApi) {
      const cell = grid.getFocusedCell();
      if (
        (cell.column.getColDef() as DataGridColumnDefinition).sqlType == "jsonb"
      ) {
        const row = grid.getDisplayedRowAtIndex(cell.rowIndex);
        this.jsonb = JSON.parse(grid.getValue(cell.column.getColId(), row));
        this.showJsonEditor = true;
      }
    },
    editorKeyPressedF5(id: string) {
      if (!store.getters.canExecuteQuery) {
        return;
      }
      store.dispatch(
        "executeQuery",
        store.getters.getQueryTabById(id) as QueryTabContent
      );
    },
    editorKeyPressedF6(id: string) {
      store.dispatch(
        "formatQuery",
        store.getters.getQueryTabById(id) as QueryTabContent
      );
    },
    closeJsonEditor() {
      this.showJsonEditor = false;
    }
  },
  computed: {
    queryTabs() {
      return store.state.queryTabs;
    },
    activeTab() {
      return store.state.activeQueryTabIndex;
    }
  }
});
</script>

<style lang="scss">
.v-dialog-jsoneditor {
  height: 60%;
}
</style>