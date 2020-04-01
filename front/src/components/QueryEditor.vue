<template>
  <v-sheet tile :id="'editor-' + this.editorId" style="height:100%"></v-sheet>
</template>

<script lang="ts">
import Vue from "vue";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";

export default Vue.extend({
  name: "QueryEditor",
  props: {
    editorId: String
  },
  data: () => ({
    editor: {} as monaco.editor.IStandaloneCodeEditor
  }),
  mounted() {
    this.editor = monaco.editor.create(
      document.getElementById("editor-" + this.editorId)!,
      {
        value: "SELECT * FROM TABLE_" + this.editorId,
        language: "sql",
        theme: "vs-dark"
      }
    );
  },
  beforeDestroy() {
    this.editor.dispose();
  }
});
</script>
