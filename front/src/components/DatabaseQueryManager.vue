<template>
  <v-container fluid class="pa-0" fill-height>
    <v-tabs v-model="activeTab">
      <v-tab>Item One</v-tab>
      <v-tab>Item Two</v-tab>
    </v-tabs>
    <v-container fluid class="pa-0" style="height:100%">
      <v-tabs-items class="pt-2" v-model="activeTab" style="height: 100%">
        <v-tab-item style="height: 100%">
          <v-container fluid class="pa-0" style="height:60%">
            <v-sheet tile id="monaco-1" style="height:100%"></v-sheet>
          </v-container>
          <v-container fluid class="pa-0" style="height:40%">
            <v-sheet tile style="height:100%;background-color:grey"></v-sheet>
          </v-container>
        </v-tab-item>
      </v-tabs-items>
    </v-container>
  </v-container>
</template>

<script lang="ts">
import Vue from "vue";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";

export default Vue.extend({
  name: "DatabaseQueryManager",
  props: {},
  data: () => ({
    editor: {} as monaco.editor.IStandaloneCodeEditor,
    activeTab: null,
  }),
  methods: {
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
  }
});
</script>
