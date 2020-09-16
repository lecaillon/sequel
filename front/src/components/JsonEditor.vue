<template>
  <v-jsoneditor v-model="json" :options="options" height="100%"></v-jsoneditor>
</template>

<script lang="ts">
import Vue from "vue";

export default Vue.extend({
  name: "JsonEditor",
  props: {
    json: {} as any,
    options: {} as any
  }
});
</script>

<style lang="scss">
.jsoneditor {
  border: 0 !important;
  font-family: inherit !important;
}
.jsoneditor-popover, .jsoneditor-schema-error, div.jsoneditor-field, div.jsoneditor-value, div.jsoneditor td, div.jsoneditor textarea, div.jsoneditor th, pre.jsoneditor-preview {
	font-family: inherit !important;
}
.jsoneditor-search {
  font-family: inherit !important;
}
.jsoneditor-menu {
  background-color: map-get($material-dark-elevation-colors, "4") !important;
  border-bottom: 1px solid map-get($material-dark-elevation-colors, "4") !important;
}
.jsoneditor-navigation-bar {
  background-color: inherit !important;
  font-family: inherit !important;
}
.jsoneditor-frame {
  background-color: #1e1e1e !important;
}
.jsoneditor-frame input {
  color: white !important;
}
div.jsoneditor td.jsoneditor-tree {
  vertical-align: inherit !important;
}
.jsoneditor-field {
  color: white !important;
}
.jsoneditor-string {
  color: #90A4AE !important;
}
.jsoneditor-number {
  color: #7CB342 !important;
}
.jsoneditor-boolean {
  color: #FF8F00 !important;
}
.jsoneditor-null {
  color: #2196F3 !important;
}
div.jsoneditor-field.jsoneditor-highlight,
div.jsoneditor-field[contenteditable="true"]:focus,
div.jsoneditor-field[contenteditable="true"]:hover,
div.jsoneditor-value.jsoneditor-highlight,
div.jsoneditor-value[contenteditable="true"]:focus,
div.jsoneditor-value[contenteditable="true"]:hover {
  background-color: inherit !important;
  border: 1px solid transparent !important;
}
</style>