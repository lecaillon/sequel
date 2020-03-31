<template>
  <v-container fluid class="pa-0" fill-height>
    <v-tabs :value="activeTab" @change="selected">
      <v-tab v-for="(tab, index) in queryTabs" :key="tab.name">
        {{ tab.name }}
        <v-icon show="false" @click="closeTab(index)" small class="ml-1">mdi-close</v-icon>
      </v-tab>
    </v-tabs>
    <v-container fluid class="pa-0" style="height:100%">
      <v-tabs-items class="pt-2" v-model="activeTab" style="height: 100%">
        <v-tab-item v-for="tab in queryTabs" :key="tab.name" style="height: 100%">
          <v-container fluid class="pa-0" style="height:60%">
            <query-editor :editorId="tab.id"></query-editor>
          </v-container>
          <v-container fluid class="pa-0" style="height:40%">
            <v-sheet tile style="height:100%"></v-sheet>
          </v-container>
        </v-tab-item>
      </v-tabs-items>
    </v-container>
  </v-container>
</template>

<script lang="ts">
import Vue from "vue";
import store from "@/store";
import QueryEditor from "@/components/QueryEditor.vue";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";

export default Vue.extend({
  name: "DatabaseQueryManager",
  components: {
    QueryEditor
  },
  methods: {
    selected(activeTab: number) {
      store.dispatch("changeActiveQueryTab", activeTab);
    },
    closeTab(activeTab: number) {
      store.dispatch("closeQueryTab", activeTab);
    }
  },
  computed: {
    queryTabs() {
      return store.state.queryTabs;
    },
    activeTab() {
      return store.state.activeQueryTab;
    }
  }
});
</script>
