<template>
  <v-dialog width="90%" v-model="show" @click:outside="close" @keydown.esc="close" content-class="v-dialog-history">
    <textarea id="sql-copy" readonly style="left:-9999px; position:absolute"></textarea>
    <v-card style="height:100%">
      <v-toolbar dark dense flat>
        <v-toolbar-title>Query history</v-toolbar-title>
        <v-spacer></v-spacer>
        <v-tooltip bottom>
          <template v-slot:activator="{ on }">
            <v-btn icon :disabled="!rowSelected" v-on="on" @click.stop="copySql()">
              <v-icon small color="grey lighten-2">mdi-content-copy</v-icon>
            </v-btn>
          </template>
          <span>Copy</span>
        </v-tooltip>
        <v-tooltip bottom>
          <template v-slot:activator="{ on }">
            <v-btn icon :disabled="!rowSelected" v-on="on" @click.stop="pasteSql()">
              <v-icon small color="grey lighten-2">mdi-content-paste</v-icon>
            </v-btn>
          </template>
          <span>Paste in active tab</span>
        </v-tooltip>
        <v-divider vertical inset />
        <v-tooltip bottom>
          <template v-slot:activator="{ on }">
            <v-btn icon v-on="on" @click.stop="updateFavorite()">
              <v-icon small color="orange">mdi-star</v-icon>
            </v-btn>
          </template>
          <span>Add / Remove favorites</span>
        </v-tooltip>
        <v-divider vertical inset />
        <v-tooltip bottom>
          <template v-slot:activator="{ on }">
            <v-btn icon v-on="on" @click.stop="showErrors=!showErrors; fetchHistory()">
              <v-icon small :color="getShowErrorsIconColor">mdi-close-circle</v-icon>
            </v-btn>
          </template>
          <span>{{ this.showErrors ? 'Hide failed and canceled queries' : 'Show failed and canceled queries' }}</span>
        </v-tooltip>
        <v-tooltip bottom>
          <template v-slot:activator="{ on }">
            <v-btn icon v-on="on" @click.stop="showFavorites=!showFavorites; fetchHistory()">
              <v-icon small :color="getShowFavoritesIconColor">mdi-playlist-star</v-icon>
            </v-btn>
          </template>
          <span>Show favorites only</span>
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
                style="min-height: 150px"
                @created="gridCreated"
                @selection-changed="onSelectionChanged"
                @cell-focused="onCellFocused"
              ></data-grid>
            </v-col>
            <v-col class="pa-0" cols="12" md="7">
              <v-container class="py-0" style="height:35%">

               <v-row dense>

                <v-col cols="12" md="6">
                  <v-text-field hide-details="auto" height="27" single-line label="Query name" ></v-text-field>
                </v-col>

                <v-col cols="12" md="6">
                  <v-combobox hide-details="auto"    
                    single-line
                    clearable
                    label="Topics"
                    multiple
                    height="27"
                  >
                    <template v-slot:selection="{ attrs, item, select, selected }">
                    <v-chip  small
                      v-bind="attrs"
                      :input-value="selected"
                      close
                      @click="select"
                      @click:close="remove(item)"
                    >
                      {{ item }}
                    </v-chip>
                    </template>
                  </v-combobox>
                </v-col>

              <v-col cols="12">
                <v-window v-if="queryHistory" show-arrows>
                  <v-window-item
                    v-for="(stat,i) in queryHistory.stats"
                    :key="i"
                  >
                    <v-row no-gutters>

                      <v-col cols="6" md="4">
                          <v-list-item two-line dense >
                            <v-list-item-avatar>
                              <v-icon size="26">
                                mdi-calendar-alert
                              </v-icon>
                            </v-list-item-avatar>

                            <v-list-item-content>
                              <v-list-item-title class="font-weight-regular">{{ new Date(stat.executedOn).toLocaleDateString() }}</v-list-item-title>
                              <v-list-item-subtitle class="font-weight-regular">{{ new Date(stat.executedOn).toLocaleTimeString() }}</v-list-item-subtitle>
                            </v-list-item-content>
                          </v-list-item>
                      </v-col>

                      <v-col cols="6" md="4">
                          <v-list-item two-line dense>
                            <v-list-item-avatar>
                              <v-icon size="26">
                                mdi-database
                              </v-icon>
                            </v-list-item-avatar>

                            <v-list-item-content>
                              <v-list-item-title class="font-weight-regular">{{ stat.database }}</v-list-item-title>
                              <v-list-item-subtitle class="font-weight-regular">{{ stat.serverConnection }}</v-list-item-subtitle>
                            </v-list-item-content>
                          </v-list-item>
                      </v-col>

                      <v-col cols="6" md="4" align-self="center" >
                        <v-chip label small color="red" class="ml-6 pa-4">
                          {{ stat.environment }}
                        </v-chip>
                      </v-col>

                      <v-col cols="6" md="4">
                          <v-list-item two-line dense>
                            <v-list-item-avatar>
                              <v-icon size="26">
                                mdi-timer
                              </v-icon>
                            </v-list-item-avatar>

                            <v-list-item-content>
                              <v-list-item-title class="font-weight-regular">{{ stat.elapsed }} ms</v-list-item-title>
                              <v-list-item-subtitle class="font-weight-regular">Elapsed time</v-list-item-subtitle>
                            </v-list-item-content>
                          </v-list-item>
                      </v-col>

                      <v-col cols="6" md="4">
                          <v-list-item two-line dense>
                            <v-list-item-avatar>
                              <v-icon size="26">
                                mdi-format-list-numbered
                              </v-icon>
                            </v-list-item-avatar>

                            <v-list-item-content>
                              <v-list-item-title class="font-weight-regular">{{ stat.rowCount }}</v-list-item-title>
                              <v-list-item-subtitle class="font-weight-regular">Row count</v-list-item-subtitle>
                            </v-list-item-content>
                          </v-list-item>
                      </v-col>

                      <v-col cols="6" md="4">
                          <v-list-item two-line dense>
                            <v-list-item-avatar>
                              <v-icon size="26">
                                mdi-content-save-outline
                              </v-icon>
                            </v-list-item-avatar>

                            <v-list-item-content>
                              <v-list-item-title class="font-weight-regular">{{ stat.recordsAffected }}</v-list-item-title>
                              <v-list-item-subtitle class="font-weight-regular">Record(s) affected</v-list-item-subtitle>
                            </v-list-item-content>
                          </v-list-item>
                      </v-col>

                    </v-row>
                  </v-window-item>
                </v-window>
              </v-col>


          </v-row>
          

              </v-container>
              
              <v-container class="pa-0" style="height:65%">
                <v-sheet tile id="editor-history" style="height:100%"></v-sheet>
              </v-container>
            
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
import { QueryHistoryQuery } from "@/models/queryHistoryQuery";
import DataGrid from "@/components/DataGrid.vue";
import { GridApi, RefreshCellsParams } from "ag-grid-community";
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
    gridApi: {} as GridApi,
    rowSelected: false as boolean,
    sql: "" as string,
    code: "" as string,
    queryHistory: null as any,
    showErrors: false as boolean,
    showFavorites: false as boolean
  }),
  watch: {
    search: function() {
      this.fetchHistory();
    },
    show: function(showForm: boolean) {
      if (showForm) {
        this.rowSelected = false;
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
                lineDecorationsWidth: 0,
                automaticLayout: true,
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
    gridCreated(id: string, grid: GridApi) {
      this.gridApi = grid;
    },
    onSelectionChanged() {
      this.sql = this.gridApi.getSelectedRows()[0].sql;
      this.code = this.gridApi.getSelectedRows()[0].code;
      this.queryHistory = this.gridApi.getSelectedRows()[0];
      this.editor?.getModel()?.setValue(this.sql);
      this.rowSelected = true;
    },
    onCellFocused() {
      this.gridApi
        .getRowNode(this.gridApi.getFocusedCell().rowIndex.toString())
        .setSelected(true, true);
    },
    fetchHistory() {
      store.dispatch("fetchHistory", {
        sql: this.search,
        showErrors: this.showErrors,
        showFavorites: this.showFavorites
      } as QueryHistoryQuery);
    },
    copySql() {
      if (!this.rowSelected) {
        return;
      }
      const el = document.getElementById("sql-copy") as HTMLInputElement;
      el.value = this.sql;
      el.select();
      document.execCommand("copy");
      store.dispatch("showAppSnackbar", {
        message: "SQL statement added to your clipboard",
        color: "success"
      } as AppSnackbar);
      this.close();
    },
    pasteSql() {
      if (!this.rowSelected) {
        return;
      }
      store.dispatch("pasteSqlInActiveTab", this.sql);
      this.close();
    },
    updateFavorite() {
      const star = !this.gridApi.getSelectedRows()[0].star;
      store.dispatch("updateFavorite", { code: this.code, star: star });
      this.gridApi.getSelectedRows()[0].star = star;
      this.gridApi.refreshCells({
        rowNodes: this.gridApi.getSelectedNodes(),
        force: true,
        suppressFlash: true
      } as RefreshCellsParams);
    }
  },
  computed: {
    history: () => store.state.history,
    getShowErrorsIconColor() {
      return this.showErrors ? "red" : "grey lighten-2";
    },
    getShowFavoritesIconColor() {
      return this.showFavorites ? "orange" : "grey lighten-2";
    },
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
.v-window__prev, .v-window__next {
  margin: 0 !important;
}
</style>