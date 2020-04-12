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
    editor: {} as monaco.editor.IStandaloneCodeEditor,
    editorActionF5: {} as monaco.IDisposable
  }),
  mounted() {
    this.editor = monaco.editor.create(
      document.getElementById("editor-" + this.editorId)!,
      {
        value: "",
        language: "sql",
        theme: "vs-dark",
        mouseWheelZoom: true,
        scrollBeyondLastLine: false,
        automaticLayout: false,
        find: {
          addExtraSpaceOnTop: false
        }
      }
    );

    this.editorActionF5 = this.editor.addAction({
      id: "sequel-F5",
      label: "Execute Query - Sequel",
      keybindings: [monaco.KeyCode.F5],
      contextMenuGroupId: "1_modification",
      run: () => {
        this.$emit("keyPressedF5", this.editorId);
      }
    });

    this.editor.focus();
    this.$emit("created", this.editorId, this.editor);
  },
  beforeDestroy() {
    this.editorActionF5.dispose();
    this.editor.dispose();
  }
});
</script>
