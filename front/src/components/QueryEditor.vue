<template>
  <v-sheet
    tile
    :id="'editor-' + this.editorId"
    v-resize="onResize"
    style="height:100%"
  ></v-sheet>
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
    editorActionF5: {} as monaco.IDisposable,
    editorActionF6: {} as monaco.IDisposable,
    onDidChangeContent: {} as monaco.IDisposable
  }),
  methods: {
    onResize() {
      if (Object.keys(this.editor).length > 0) {
        this.editor.layout();
      }
    }
  },
  mounted() {
    this.editor = monaco.editor.create(
      document.getElementById("editor-" + this.editorId)!,
      {
        value: "",
        fontSize: 13,
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
      label: "Execute Query",
      keybindings: [monaco.KeyCode.F5],
      contextMenuGroupId: "1_modification",
      run: () => {
        this.$emit("keyPressedF5", this.editorId);
      }
    });

    this.editorActionF6 = this.editor.addAction({
      id: "sequel-F6",
      label: "Format Query",
      keybindings: [monaco.KeyCode.F6],
      contextMenuGroupId: "1_modification",
      run: () => {
        this.$emit("keyPressedF6", this.editorId);
      }
    });

    this.onDidChangeContent = this.editor
      .getModel()!
      .onDidChangeContent(() =>
        monaco.editor.setModelMarkers(this.editor.getModel()!, "sql", [])
      );

    this.editor.focus();
    this.$emit("created", this.editorId, this.editor);
  },
  beforeDestroy() {
    this.editorActionF5.dispose();
    this.onDidChangeContent.dispose();
    this.editor.dispose();
  }
});
</script>

<style lang="scss">
.monaco-editor .codelens-decoration {
  font-size: 11px !important;
}
.monaco-editor .codelens-decoration a {
  color: #999999 !important;
}
.monaco-editor .codelens-decoration a[id]:hover {
  color: #0097fb !important;
}
</style>