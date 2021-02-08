<template>
  <v-dialog
    width="90%"
    v-model="show"
    @click:outside="close"
    @keydown.esc="close"
    content-class="v-dialog-history"
  >
    <textarea
      id="sql-copy"
      readonly
      style="left: -9999px; position: absolute"
    ></textarea>
    <v-card style="height: 100%">
      <v-toolbar dark dense flat>
        <v-toolbar-title>Query history</v-toolbar-title>
        <v-spacer></v-spacer>

        <v-divider vertical inset />

        <v-btn-toggle dense group v-model="showDbms">
          <v-btn>
            <v-avatar size="22" tile>
              <v-img :src="require('../assets/db/PostgreSQL.png')"></v-img>
            </v-avatar>
          </v-btn>
          <v-btn>
            <v-avatar size="22" tile>
              <v-img :src="require('../assets/db/SQLServer.png')"></v-img>
            </v-avatar>
          </v-btn>
          <v-btn>
            <v-avatar size="22" tile>
              <v-img :src="require('../assets/db/SQLite.png')"></v-img>
            </v-avatar>
          </v-btn>
        </v-btn-toggle>

        <v-divider vertical inset />

        <v-btn-toggle dense group multiple>
          <v-tooltip bottom>
            <template v-slot:activator="{ on }">
              <v-btn
                v-on="on"
                @click.stop="
                  showErrors = !showErrors;
                  fetchHistory();
                "
              >
                <v-icon color="grey lighten-4">mdi-playlist-remove</v-icon>
              </v-btn>
            </template>
            <span>Show failed queries only</span>
          </v-tooltip>
          <v-tooltip bottom>
            <template v-slot:activator="{ on }">
              <v-btn
                v-on="on"
                @click.stop="
                  showFavorites = !showFavorites;
                  fetchHistory();
                "
              >
                <v-icon color="grey lighten-4">mdi-playlist-star</v-icon>
              </v-btn>
            </template>
            <span>Show favorites only</span>
          </v-tooltip>
          <v-tooltip bottom>
            <template v-slot:activator="{ on }">
              <v-btn
                v-on="on"
                @click.stop="
                  showNamedQueries = !showNamedQueries;
                  fetchHistory();
                "
              >
                <v-icon color="grey lighten-4">mdi-playlist-check</v-icon>
              </v-btn>
            </template>
            <span>Show named queryies only</span>
          </v-tooltip>
        </v-btn-toggle>

        <v-combobox
          label="Search (sql, terms)"
          v-model="search"
          :return-object="false"
          :items="historyTerms"
          @change="fetchHistory"
          item-text="name"
          multiple
          solo
          flat
          dense
          clearable
          hide-details="auto"
          prepend-inner-icon="mdi-magnify"
          style="max-width: 500px"
          class="ml-1"
        >
          <template v-slot:item="{ item }">
            <v-list-item-avatar v-if="item.icon">
              <v-avatar size="32" color="secondary">
                <v-icon size="18"> {{ item.icon }} </v-icon>
              </v-avatar>
            </v-list-item-avatar>
            <v-list-item-avatar v-else>
              <v-avatar size="32" color="secondary">
                {{ item.name.substring(0, 1).toUpperCase() }}
              </v-avatar>
            </v-list-item-avatar>
            <v-list-item-content>
              <v-list-item-title
                v-html="item.name"
                class="subtitle-2"
              ></v-list-item-title>
              <v-list-item-subtitle
                v-html="item.kind"
                class="font-weight-regular"
              ></v-list-item-subtitle>
            </v-list-item-content>
          </template>
        </v-combobox>
      </v-toolbar>
      <v-card-text class="pa-0 pr-1" style="height: calc(100% - 64px)">
        <v-container fluid fill-height class="pa-0">
          <v-row dense style="height: 100%">
            <v-col class="pa-0" cols="12" md="6">
              <data-grid
                v-if="history.response"
                :columns="history.response.columns"
                :rows="history.response.rows"
                :loading="history.loading"
                style="min-height: 150px"
                @created="onGridCreated"
                @selection-changed="onSelectionChanged"
                @cell-focused="onCellFocused"
                @model-updated="onGridUpdated"
              ></data-grid>
            </v-col>

            <v-col class="pa-0" cols="12" md="6">
              <v-container
                fill-height
                class="py-0"
                style="flex-direction: column; align-items: initial"
              >
                <v-row v-if="queryHistory" dense style="flex: 0 1 auto">
                  <v-col cols="12" md="5" class="pb-3">
                    <v-text-field
                      label="Query name"
                      v-model="queryHistory.name"
                      @change="updateName"
                      hide-details="auto"
                      height="27"
                      class="subtitle-1"
                    ></v-text-field>
                  </v-col>

                  <v-col cols="12" md="7" class="history">
                    <v-combobox
                      label="Topics"
                      v-model="queryHistory.topics"
                      :items="historyTopics"
                      @change="updateTopics"
                      hide-details="auto"
                      single-line
                      clearable
                      multiple
                      height="27"
                    >
                      <template
                        v-slot:selection="{ attrs, item, select, selected }"
                      >
                        <v-chip
                          color="secondary"
                          small
                          v-bind="attrs"
                          :input-value="selected"
                          close
                          @click="select"
                          @click:close="removeTopic(item)"
                        >
                          {{ item }}
                        </v-chip>
                      </template>
                    </v-combobox>
                  </v-col>

                  <v-col cols="12" class="pb-5">
                    <v-window v-resize="onResize" show-arrows>
                      <v-window-item
                        v-for="(stat, i) in queryHistory.stats"
                        :key="i"
                      >
                        <v-row no-gutters>
                          <v-col cols="6" md="4">
                            <v-list-item two-line dense class="pl-0">
                              <v-list-item-avatar size="24">
                                <v-icon color="grey lighten-4" size="24">
                                  {{ getStatusIcon(stat.status) }}
                                </v-icon>
                              </v-list-item-avatar>
                              <v-list-item-content>
                                <v-list-item-title
                                  class="font-weight-regular subtitle-2"
                                  >{{
                                    new Date(
                                      stat.executedOn
                                    ).toLocaleDateString()
                                  }}</v-list-item-title
                                >
                                <v-list-item-subtitle
                                  class="font-weight-regular"
                                  >{{
                                    new Date(
                                      stat.executedOn
                                    ).toLocaleTimeString()
                                  }}</v-list-item-subtitle
                                >
                              </v-list-item-content>
                            </v-list-item>
                          </v-col>

                          <v-col cols="6" md="4">
                            <v-list-item two-line dense class="pl-0">
                              <v-list-item-avatar size="25">
                                <v-icon color="grey lighten-4" size="25">
                                  mdi-database
                                </v-icon>
                              </v-list-item-avatar>
                              <v-list-item-content>
                                <v-list-item-title
                                  class="font-weight-regular subtitle-2"
                                  >{{ stat.database }}</v-list-item-title
                                >
                                <v-list-item-subtitle
                                  class="font-weight-regular"
                                  >{{
                                    stat.serverConnection
                                  }}</v-list-item-subtitle
                                >
                              </v-list-item-content>
                            </v-list-item>
                          </v-col>

                          <v-col cols="6" md="4" align-self="center">
                            <v-chip
                              small
                              outlined
                              :color="getEnvironmentColor(stat.environment)"
                            >
                              {{ stat.environment }}
                            </v-chip>
                          </v-col>

                          <v-col cols="6" md="4">
                            <v-list-item two-line dense class="pl-0">
                              <v-list-item-avatar size="26">
                                <v-icon color="grey lighten-4" size="26">
                                  mdi-timer
                                </v-icon>
                              </v-list-item-avatar>
                              <v-list-item-content>
                                <v-list-item-title
                                  class="font-weight-regular subtitle-2"
                                  >{{ stat.elapsed }} ms</v-list-item-title
                                >
                                <v-list-item-subtitle
                                  class="font-weight-regular"
                                  >Elapsed time</v-list-item-subtitle
                                >
                              </v-list-item-content>
                            </v-list-item>
                          </v-col>

                          <v-col cols="6" md="4">
                            <v-list-item two-line dense class="pl-0">
                              <v-list-item-avatar size="24">
                                <v-icon color="grey lighten-4" size="24">
                                  mdi-format-list-numbered
                                </v-icon>
                              </v-list-item-avatar>
                              <v-list-item-content>
                                <v-list-item-title
                                  class="font-weight-regular subtitle-2"
                                  >{{ stat.rowCount }}</v-list-item-title
                                >
                                <v-list-item-subtitle
                                  class="font-weight-regular"
                                  >Row count</v-list-item-subtitle
                                >
                              </v-list-item-content>
                            </v-list-item>
                          </v-col>

                          <v-col cols="6" md="4">
                            <v-list-item two-line dense class="pl-0">
                              <v-list-item-avatar size="26">
                                <v-icon color="grey lighten-4" size="26">
                                  mdi-content-save-outline
                                </v-icon>
                              </v-list-item-avatar>

                              <v-list-item-content>
                                <v-list-item-title
                                  class="font-weight-regular subtitle-2"
                                  >{{ stat.recordsAffected }}</v-list-item-title
                                >
                                <v-list-item-subtitle
                                  class="font-weight-regular"
                                  >Record(s) affected</v-list-item-subtitle
                                >
                              </v-list-item-content>
                            </v-list-item>
                          </v-col>
                        </v-row>
                      </v-window-item>
                    </v-window>
                  </v-col>
                </v-row>

                <v-row
                  v-show="queryHistory"
                  class="pb-5"
                  style="flex: 1 1 auto"
                >
                  <v-col cols="12" class="pl-0">
                    <v-sheet
                      tile
                      id="editor-history"
                      style="height: 100%"
                    ></v-sheet>
                  </v-col>
                </v-row>

                <v-row
                  v-show="queryHistory"
                  dense
                  class="mr-2 mb-1"
                  style="flex: 0 1 auto"
                >
                  <v-spacer></v-spacer>

                  <v-tooltip bottom>
                    <template v-slot:activator="{ on }">
                      <v-btn
                        elevation="2"
                        v-on="on"
                        @click.stop="updateFavorite()"
                      >
                        <v-icon color="orange" small class="mr-1"
                          >mdi-star</v-icon
                        >
                        <span>Star</span>
                      </v-btn>
                    </template>
                    <span>Add / Remove favorites</span>
                  </v-tooltip>

                  <v-btn elevation="2" class="ml-3" @click.stop="copySql()">
                    <v-icon color="grey lighten-2" small class="mr-1"
                      >mdi-content-copy</v-icon
                    >
                    <span>Copy</span>
                  </v-btn>

                  <v-tooltip bottom>
                    <template v-slot:activator="{ on }">
                      <v-btn
                        elevation="2"
                        class="ml-3"
                        v-on="on"
                        @click.stop="pasteSql()"
                      >
                        <v-icon color="grey lighten-2" small class="mr-1"
                          >mdi-content-paste</v-icon
                        >
                        <span>Paste</span>
                      </v-btn>
                    </template>
                    <span>Paste in new tab</span>
                  </v-tooltip>

                  <v-tooltip bottom>
                    <template v-slot:activator="{ on }">
                      <v-btn
                        elevation="2"
                        class="ml-3"
                        v-on="on"
                        @click.stop="deleteHistory()"
                      >
                        <v-icon color="red" small class="mr-1"
                          >mdi-delete-forever</v-icon
                        >
                        <span>Delete</span>
                      </v-btn>
                    </template>
                    <span>Remove query from history</span>
                  </v-tooltip>
                </v-row>
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
import { QueryHistory } from "@/models/queryHistory";
import { ColorByEnvironment } from "@/appsettings";
import DataGrid from "@/components/DataGrid.vue";
import { GridApi, RefreshCellsParams } from "ag-grid-community";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";

export default Vue.extend({
  name: "QueryHistoryManager",
  props: {
    show: Boolean,
  },
  components: {
    DataGrid,
  },
  data: () => ({
    editor: null as monaco.editor.IStandaloneCodeEditor | null,
    gridApi: {} as GridApi,
    sql: "" as string,
    code: "" as string,
    queryHistory: null as QueryHistory | null,
    showDbms: undefined as number | undefined,
    showErrors: false as boolean,
    showFavorites: false as boolean,
    showNamedQueries: false as boolean,
    search: [] as string[],
  }),
  watch: {
    show: function (showForm: boolean) {
      this.fetchHistory();
    },
    showDbms: function () {
      this.fetchHistory();
    },
  },
  methods: {
    onResize() {
      this.editor?.layout();
    },
    close() {
      this.$emit("close");
    },
    onGridCreated(id: string, grid: GridApi) {
      this.gridApi = grid;
    },
    onSelectionChanged() {
      this.sql = this.gridApi.getSelectedRows()[0].sql;
      this.code = this.gridApi.getSelectedRows()[0].code;
      this.queryHistory = this.gridApi.getSelectedRows()[0];

      if (this.editor == null) {
        setTimeout(() => {
          this.editor = monaco.editor.create(
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
              automaticLayout: false,
              find: {
                addExtraSpaceOnTop: false,
              },
            }
          );
          this.editor.getModel()?.setValue(this.sql);
        }, 100);
      } else {
        this.editor.getModel()?.setValue(this.sql);
      }
    },
    onCellFocused() {
      this.gridApi
        .getRowNode(this.gridApi.getFocusedCell().rowIndex.toString())
        .setSelected(true, true);
    },
    onGridUpdated() {
      if (this.history.response.rowCount == 0) {
        this.queryHistory = null;
      }
    },
    async fetchHistory() {
      let dbms: string;
      switch (this.showDbms) {
        case 0:
          dbms = "PostgreSQL";
          break;
        case 1:
          dbms = "SQLServer";
          break;
        case 2:
          dbms = "SQLite";
          break;
        default:
          dbms = "";
          break;
      }

      await store.dispatch("fetchHistory", {
        dbms: dbms,
        terms: this.search,
        showErrors: this.showErrors,
        showFavorites: this.showFavorites,
        showNamedQueries: this.showNamedQueries,
      } as QueryHistoryQuery);

      this.gridApi.selectIndex(0, false, false);
    },
    copySql() {
      const el = document.getElementById("sql-copy") as HTMLInputElement;
      el.value = this.sql;
      el.select();
      document.execCommand("copy");
      store.dispatch("showAppSnackbar", {
        message: "SQL statement added to your clipboard",
        color: "success",
      } as AppSnackbar);
      this.close();
    },
    pasteSql() {
      store.dispatch("pasteSqlInActiveTab", this.sql);
      this.close();
    },
    async deleteHistory() {
      await store.dispatch("deleteHistory", this.code);
      this.fetchHistory();
    },
    updateFavorite() {
      this.queryHistory!.star = !this.queryHistory!.star;
      store.dispatch("updateHistoryFavorite", {
        code: this.code,
        star: this.queryHistory!.star,
      });
      this.refreshDatagrid();
    },
    updateName() {
      store.dispatch("updateHistoryName", {
        code: this.code,
        name: this.queryHistory?.name ?? "",
      });
      this.refreshDatagrid();
    },
    updateTopics() {
      store.dispatch("updateHistoryTopics", {
        code: this.code,
        topics: this.queryHistory?.topics ?? "",
      });
    },
    removeTopic(item: any) {
      if (this.queryHistory?.topics != null) {
        this.queryHistory.topics.splice(
          this.queryHistory.topics.indexOf(item),
          1
        );
        this.queryHistory.topics = [...this.queryHistory.topics];
        this.updateTopics();
      }
    },
    refreshDatagrid() {
      this.gridApi.refreshCells({
        rowNodes: this.gridApi.getSelectedNodes(),
        force: true,
        suppressFlash: true,
      } as RefreshCellsParams);
    },
    getEnvironmentColor(environment: string) {
      return ColorByEnvironment[environment];
    },
    getStatusIcon(status: string) {
      console.log("psg");

      if (status == "Succeeded") {
        return "mdi-calendar-check";
      } else if (status == "Failed") {
        return "mdi-calendar-remove";
      } else {
        return "mdi-calendar-alert";
      }
    },
  },
  computed: {
    history: () => store.state.history,
    historyTopics: () => store.state.historyTopics,
    historyTerms: () => store.state.historyTerms,
  },
  created() {
    store.dispatch("fetchHistoryTopics");
    store.dispatch("fetchHistoryTerms");
  },
});
</script>

<style lang="scss">
.v-dialog-history {
  height: 75%;
}
.v-window__prev,
.v-window__next {
  margin: 0 !important;
}
.theme--dark.v-chip::before {
  opacity: 0.08;
}
.v-autocomplete__content .v-list-item__title {
  font-size: 13px;
}
</style>