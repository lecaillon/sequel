<template>
      <v-autocomplete
        placeholder="Select a server"
        auto-select-first="false"
        dense
        hide-details
        clearable
        solo
        :items="servers"
        item-text="connectionString"
      ></v-autocomplete>
</template>

<script lang="ts">
import Vue from "vue";
import store from '@/store';
import { ServerConnection } from "@/models/serverConnection";

export default Vue.extend({
  name: "SelectServerConnection",
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