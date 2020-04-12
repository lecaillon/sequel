<template>
  <v-container fluid class="pa-0" fill-height>
    <v-tabs :value="activeTab" @change="selected">
      <v-tab v-for="(tab, index) in queryTabs" :key="tab.id">
        <query-tab :title="tab.title" :index="index" @close="closeTab"></query-tab>
      </v-tab>
    </v-tabs>
    <v-container fluid class="pa-0" style="height:100%">
      <v-tabs-items class="pt-3" v-model="activeTab" style="height: 100%">
        <v-tab-item v-for="tab in queryTabs" :key="tab.id" style="height: 100%">
          <v-container fluid class="pa-0" style="height:45%">
            <query-editor
              :editorId="tab.id"
              @created="editorCreated"
              @keyPressedF5="editorKeyPressedF5"
            ></query-editor>
          </v-container>
          <v-container fluid class="pa-0" style="height:calc(55% - 48px)">
            <data-grid :columns="tab.grid.columns" :rows="tab.grid.rows" :loading="tab.loading"></data-grid>
          </v-container>
        </v-tab-item>
      </v-tabs-items>
    </v-container>
  </v-container>
</template>

<script lang="ts">
import Vue from "vue";
import store from "@/store";
import QueryTab from "@/components/QueryTab.vue";
import QueryEditor from "@/components/QueryEditor.vue";
import DataGrid from "@/components/DataGrid.vue";
import { QueryTabContent } from "@/models/queryTabContent";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";

export default Vue.extend({
  name: "DatabaseQueryManager",
  components: {
    QueryTab,
    QueryEditor,
    DataGrid
  },
  methods: {
    selected(activeTab: number) {
      store.dispatch("changeActiveQueryTab", activeTab);
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
    editorKeyPressedF5(id: string) {
      if (!store.getters.canExecuteQuery) {
        return;
      }
      store.dispatch(
        "executeQuery",
        store.getters.getQueryTabById(id) as QueryTabContent
      );
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
