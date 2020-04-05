<template>
  <ag-grid-vue
    style="height:100%"
    class="ag-theme-alpine-dark"
    :columnDefs="columns"
    :rowData="rows"
    enableCellTextSelection
    rowSelection="multiple"
    multiSortKey="ctrl"
    stopEditingWhenGridLosesFocus
    preventDefaultOnContextMenu
  ></ag-grid-vue>
</template>

<script lang="ts">
import Vue from "vue";
import { AgGridVue } from "ag-grid-vue";

export default Vue.extend({
  name: "DataGrid",
  props: {
    columns: Array,
    rows: Array
  },
  components: {
    AgGridVue
  }
});
</script>

<style lang="scss">
@import "../../node_modules/ag-grid-community/dist/styles/ag-grid.css";
@import "../../node_modules/ag-grid-community/dist/styles/ag-theme-alpine-dark.css";

.ag-root-wrapper {
  border-width: 1px 0 0 0 !important;
  border-radius: 0px !important;
}
</style>
