<template>
  <v-app>
    <v-app-bar app clipped-left clipped-right>
      <v-app-bar-nav-icon
        @click.stop="showDbExplorer = !showDbExplorer"
      ></v-app-bar-nav-icon>
      <v-toolbar-title>Sequel</v-toolbar-title>
      <v-spacer></v-spacer>
      <v-btn icon>
        <v-icon>mdi-play-circle-outline</v-icon>
      </v-btn>
      <v-btn icon>
        <v-icon>mdi-tab-plus</v-icon>
      </v-btn>
      <v-divider vertical inset />
      <v-btn icon>
        <v-icon>mdi-database-refresh</v-icon>
      </v-btn>
      <v-btn icon @click.stop="showDbProperty = !showDbProperty">
        <v-icon>mdi-wrench-outline</v-icon>
      </v-btn>
      <v-divider vertical inset />
      <v-btn icon @click.stop="openFormServerConnection(true)">
        <v-icon>mdi-server-plus</v-icon>
      </v-btn>
      <SelectServerConnection
        @edit="openFormServerConnection(false)"
        class="me-4"
        style="max-width: 500px"
      />
      <SelectDatabase style="max-width: 350px" />
    </v-app-bar>

    <v-navigation-drawer
      app
      clipped
      v-model="showDbExplorer"
      width="300px"
      style="overflow: visible"
    >
      <v-card class="mx-auto" style="overflow: visible">
        <v-sheet>
          <v-text-field
            v-model="dbObjectSearch"
            label="Filter elements"
            flat
            solo
            hide-details
            clearable
          ></v-text-field>
        </v-sheet>
        <v-card-text style="padding: 10px 0 0 0">
          <v-treeview
            dense
            open-on-click
            :items="nodes"
            :search="dbObjectSearch"
            :filter="dbObjectSearch"
            :open.sync="openedNodes"
          >
            <template v-slot:prepend="{ item }">
              <v-icon small>{{ item.icon }}</v-icon>
            </template>
          </v-treeview>
        </v-card-text>
      </v-card>
    </v-navigation-drawer>

    <v-navigation-drawer app clipped right v-model="showDbProperty">
      <v-list-item>
        <v-list-item-content>
          <v-list-item-title class="title">table1</v-list-item-title>
          <v-list-item-subtitle>Table</v-list-item-subtitle>
        </v-list-item-content>
      </v-list-item>
      <v-divider></v-divider>
    </v-navigation-drawer>

    <v-content>
      <v-container fluid></v-container>
    </v-content>

    <v-footer app></v-footer>

    <FormServerConnection
      :show="showFormServerConnection"
      :server="editServer"
      @close="showFormServerConnection = false"
    ></FormServerConnection>

    <AppSnackbar
      :show="appSnackbar.show"
      :color="appSnackbar.color"
      :message="appSnackbar.message"
      :details="appSnackbar.details"
      @close="closeAppSnackbar"
    ></AppSnackbar>
  </v-app>
</template>

<script lang="ts">
import Vue from "vue";
import Vuetify from "vuetify";
import store from "@/store";
import FormServerConnection from "@/components/FormServerConnection.vue";
import SelectServerConnection from "@/components/SelectServerConnection.vue";
import SelectDatabase from "@/components/SelectDatabase.vue";
import AppSnackbar from "@/components/AppSnackbar.vue";
import { ServerConnection } from "./models/serverConnection";

export default Vue.extend({
  name: "App",
  vuetify: new Vuetify({
    theme: { dark: true }
  }),
  components: {
    FormServerConnection,
    SelectServerConnection,
    SelectDatabase,
    AppSnackbar
  },
  data: () => ({
    showDbExplorer: true,
    showDbProperty: false,
    showFormServerConnection: false,
    nodes: [
      {
        id: 1,
        name: "database1",
        icon: "mdi-database",
        children: [
          {
            id: 11,
            name: "schema1",
            icon: "mdi-hexagon-multiple-outline",
            children: [
              {
                id: 111,
                name: "table1",
                icon: "mdi-table",
                children: [
                  {
                    id: 1111,
                    name: "column1",
                    icon: "mdi-table-column"
                  }
                ]
              }
            ]
          }
        ]
      }
    ],
    openedNodes: [1, 11, 111],
    dbObjectSearch: null
  }),
  methods: {
    closeAppSnackbar() {
      store.dispatch("hideAppSnackbar");
    },
    openFormServerConnection(newForm: boolean) {
      if (newForm) {
        store.dispatch("changeEditServer", {
          name: "",
          type: "",
          connectionString: "",
          environment: ""
        } as ServerConnection);
      }
      this.showFormServerConnection = true;
    }
  },
  computed: {
    appSnackbar() {
      return store.state.appSnackbar;
    },
    editServer() {
      return store.state.editServer;
    }
  }
});
</script>
