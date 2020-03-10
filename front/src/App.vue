<template>
  <v-app>
    <v-app-bar app clipped-left clipped-right>
      <v-app-bar-nav-icon @click.stop="showDbExplorer = !showDbExplorer"></v-app-bar-nav-icon>
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
      <v-btn icon @click.stop="showFormServerConnection = true">
        <v-icon>mdi-server-plus</v-icon>
      </v-btn>
      <v-autocomplete
        placeholder="Select a server"
        auto-select-first="false"
        dense
        hide-details
        clearable
        solo
        :items="servers"
      ></v-autocomplete>
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
    
    <FormServerConnection :show="showFormServerConnection" @close="showFormServerConnection = false"></FormServerConnection>
  </v-app>
</template>

<script lang="ts">
import Vue from "vue";
import Vuetify from "vuetify";

import FormServerConnection from "@/components/FormServerConnection.vue";

export default Vue.extend({
  name: "App",
  vuetify: new Vuetify({
    theme: { dark: true }
  }),
  components: {
    FormServerConnection
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
    dbObjectSearch: null,
    servers: ["Programming", "Design", "Vue", "Vuetify"]
  })
});
</script>