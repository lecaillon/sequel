<template>
  <v-dialog width="90%" v-model="show" @click:outside="close" content-class="v-dialog-history">
    <textarea id="sql-copy" readonly style="left:-9999px; position:absolute"></textarea>
    <v-card style="height:100%">
      <v-toolbar dark dense flat>
        <v-toolbar-title>Query history</v-toolbar-title>
        <v-spacer></v-spacer>
        <v-tooltip bottom>
          <template v-slot:activator="{ on }">
            <v-btn icon :disabled="!hasActiveSql" v-on="on" @click.stop="copySql()">
              <v-icon color="grey lighten-2">mdi-content-copy</v-icon>
            </v-btn>
          </template>
          <span>Copy</span>
        </v-tooltip>
        <v-tooltip bottom>
          <template v-slot:activator="{ on }">
            <v-btn icon :disabled="!hasActiveSql" v-on="on" @click.stop="pasteSql()">
              <v-icon color="grey lighten-2">mdi-content-paste</v-icon>
            </v-btn>
          </template>
          <span>Paste in active tab</span>
        </v-tooltip>
        <v-text-field
          v-model="search"
          solo
          flat
          dense
          clearable
          hide-details
          label="Filter SQL statements"
          prepend-inner-icon="mdi-magnify"
        ></v-text-field>
      </v-toolbar>
      <v-card-text class="pa-0 pr-1" style="height:calc(100% - 64px)">
        <v-container fluid class="pa-0" fill-height>
          <v-row dense style="height:100%">
            <v-col class="pa-0" cols="12" md="5">
              <data-grid
                v-if="history.response"
                :columns="history.response.columns"
                :rows="history.response.rows"
                :loading="history.loading"
                @selection-changed="onSelectionChanged"
              ></data-grid>
            </v-col>
            <v-col class="pa-0 pt-2" cols="12" md="7">
              <v-sheet tile id="editor-history" style="height:100%"></v-sheet>
            </v-col>
          </v-row>
        </v-container>
      </v-card-text>
    </v-card>
  </v-dialog>
</template>

<script lang="ts">
import Vue from "vue";
import store from "@/store";
import { AppSnackbar } from "@/models/appSnackbar";
import DataGrid from "@/components/DataGrid.vue";
import { GridApi } from "ag-grid-community";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";

export default Vue.extend({
  name: "QueryHistoryManager",
  props: {
    show: Boolean
  },
  components: {
    DataGrid
  },
  data: () => ({
    editor: {} as monaco.editor.IStandaloneCodeEditor,
    debouncedSearch: "" as string,
    timeout: null as number | null,
    hasActiveSql: false as boolean
  }),
  watch: {
    search: function() {
      this.fetchHistory();
    },
    show: function(showForm: boolean) {
      if (showForm) {
        this.hasActiveSql = false;
        this.fetchHistory();
        setTimeout(
          () =>
            (this.editor = monaco.editor.create(
              document.getElementById("editor-history")!,
              {
                value: "",
                readOnly: true,
                fontSize: 13,
                language: "sql",
                theme: "vs-dark",
                lineNumbers: "off",
                minimap: { enabled: false },
                mouseWheelZoom: true,
                scrollBeyondLastLine: false,
                automaticLayout: false,
                find: {
                  addExtraSpaceOnTop: false
                }
              }
            )),
          250
        );
      } else {
        this.editor.dispose();
      }
    }
  },
  methods: {
    close() {
      this.$emit("close");
    },
    onResize() {
      if (Object.keys(this.editor).length > 0) {
        this.editor.layout();
      }
    },
    onSelectionChanged(grid: GridApi) {
      const selectedRows = grid.getSelectedRows();
      this.editor?.getModel()?.setValue(selectedRows[0].sql);
      this.hasActiveSql = true;
    },
    fetchHistory() {
      store.dispatch("fetchHistory", this.search);
    },
    copySql() {
      const sql = this.editor?.getModel()?.getValue();
      if (!sql || sql.length === 0) {
        return;
      }
      const el = document.getElementById("sql-copy") as HTMLInputElement;
      el.value = sql;
      el.select();
      document.execCommand("copy");
      store.dispatch("showAppSnackbar", {
        message: "Copied to clipboard!",
        color: "success"
      } as AppSnackbar);
      this.close();
    },
    pasteSql() {
      const sql = this.editor?.getModel()?.getValue();
      if (!sql || sql.length === 0) {
        return;
      }
      store.dispatch("pasteSqlInActiveTab", sql);
      this.close();
    }
  },
  computed: {
    history: () => store.state.history,
    search: {
      get() {
        return this.debouncedSearch ?? "";
      },
      set(val: string) {
        if (this.timeout) clearTimeout(this.timeout);
        this.timeout = setTimeout(() => {
          this.debouncedSearch = val;
        }, 600);
      }
    }
  }
});
</script>

<style lang="scss">
.v-dialog-history {
  height: 75%;
}
</style>