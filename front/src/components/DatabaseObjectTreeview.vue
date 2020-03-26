<template>
  <v-card class="mx-auto" style="overflow: visible" flat tile>
    <v-sheet>
      <v-text-field
        v-model="dbObjectSearch"
        label="Filter items"
        flat
        dense
        solo
        hide-details
        clearable
      ></v-text-field>
    </v-sheet>
    <v-card-text style="padding: 10px 0 0 0">
      <v-treeview
        activatable
        dense
        return-object
        :items="nodes"
        :load-children="fetchNodes"
        :search="dbObjectSearch"
        :open.sync="openedNodes"
        @update:active="selected"
      >
        <template v-slot:prepend="{ item }">
          <v-icon small>{{ item.icon }}</v-icon>
        </template>
      </v-treeview>
    </v-card-text>
  </v-card>
</template>

<script lang="ts">
import Vue from "vue";
import store from "@/store";
import { DatabaseObjectNode } from "@/models/databaseObjectNode";

export default Vue.extend({
  name: "DatabaseObjectTreeview",
  data: () => ({
    openedNodes: [],
    dbObjectSearch: null
  }),
  methods: {
    selected(nodes: DatabaseObjectNode[]) {
      store.dispatch("changeActiveNode", nodes.length == 0 ? {} : nodes[0]);
    },
    async fetchNodes(parent: DatabaseObjectNode) {
      await store.dispatch("fetchDatabaseObjectNodes", parent);
    }
  },
  computed: {
    nodes() {
      return store.state.nodes;
    }
  }
});
</script>
