<template>
  <ag-grid-vue
    style="height:100%"
    class="ag-theme-alpine-dark"
    rowHeight="30"
    :gridOptions="gridOptions"
    :columnDefs="columns"
    :rowData="rows"
    :frameworkComponents="frameworkComponents"
    :loadingOverlayComponent="loadingOverlayComponent"
    enableCellTextSelection
    rowSelection="multiple"
    multiSortKey="ctrl"
    stopEditingWhenGridLosesFocus
    preventDefaultOnContextMenu
    @modelUpdated="onModelUpdated"
  ></ag-grid-vue>
</template>

<script lang="ts">
import Vue from "vue";
import { AgGridVue } from "ag-grid-vue";
import "ag-grid-community/dist/styles/ag-grid.css";
import "ag-grid-community/dist/styles/ag-theme-alpine-dark.css";
import DatagridLoader from "@/components/DatagridLoader.vue";

export default Vue.extend({
  name: "DataGrid",
  props: {
    columns: Array,
    rows: Array,
    loading: Boolean
  },
  components: {
    AgGridVue
  },
  data: () => ({
    gridOptions: {},
    gridApi: {},
    gridColumnApi: {},
    frameworkComponents: {
      customLoadingOverlay: DatagridLoader
    },
    loadingOverlayComponent: "customLoadingOverlay"
  }),
  methods: {
    onModelUpdated() {
      this.gridColumnApi.autoSizeColumns(
        this.columns.filter(x => x.width === null).map(x => x.colId)
      );
    }
  },
  watch: {
    loading: function(val) {
      if (val) {
        this.gridApi.showLoadingOverlay();
      } else {
        this.gridApi.hideOverlay();
      }
    }
  },
  mounted() {
    this.gridApi = this.gridOptions.api;
    this.gridColumnApi = this.gridOptions.columnApi;
  }
});
</script>

<style lang="scss">
.ag-root-wrapper {
  border-width: 1px 0 0 0 !important;
  border-radius: 0px !important;
}
</style>
