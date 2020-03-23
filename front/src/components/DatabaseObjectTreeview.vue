<template>
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
        :item-key="path"
        :search="dbObjectSearch"
        :open.sync="openedNodes"
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

export default Vue.extend({
  name: "DatabaseObjectTreeview",
  data: () => ({
    openedNodes: [],
    dbObjectSearch: null
  }),
  methods: {
    selected(database: string) {
      store.dispatch("changeActiveDatabase", database);
    }
  },
  computed: {
    nodes() {
      return store.state.nodes;
    }
  }
});
</script>
