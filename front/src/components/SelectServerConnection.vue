<template>
  <v-autocomplete
    placeholder="Select a server"
    auto-select-first="false"
    dense
    hide-details
    clearable
    solo
    :items="servers"
    @input="selected"
  >
    <template v-slot:selection="{ attrs, item }">
      <v-chip label small color="primary">{{ item.environment }}</v-chip>
      <span class="ms-3">{{ item.name }}</span>
    </template>
    <template v-slot:item="{ index, item }">
      <v-chip label small color="primary">{{ item.environment }}</v-chip>
      <span class="ms-3">{{ item.name }}</span>
    </template>
  </v-autocomplete>
</template>

<script lang="ts">
import Vue from "vue";
import store from "@/store";
import { ServerConnection } from "@/models/serverConnection";

export default Vue.extend({
  name: "SelectServerConnection",
  methods: {
    selected(server: ServerConnection) {
      store.dispatch("changeActiveServer", server);
    }
  },
  computed: {
    servers() {
      return store.state.servers;
    }
  },
  created() {
    store.dispatch("fetchServers");
  }
});
</script>

<style>
</style>