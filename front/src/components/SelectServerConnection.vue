<template>
  <v-autocomplete
    placeholder="Select a server"
    dense
    hide-details
    clearable
    solo
    :items="servers"
    @input="selected"
  >
    <template v-slot:selection="{ attrs, item }">
      <v-chip label small :color="getChipColor(item)">{{ item.environment }}</v-chip>
      <span class="ms-3">{{ item.name }}</span>
    </template>
    <template v-slot:item="{ index, item }">
      <v-chip label small :color="getChipColor(item)">{{ item.environment }}</v-chip>
      <span class="ms-3">{{ item.name }}</span>
      <v-spacer></v-spacer>
      <v-list-item-action @click.stop>
        <v-btn icon @click.stop.prevent="edit(item)">
          <v-icon>mdi-pencil</v-icon>
        </v-btn>
      </v-list-item-action>
    </template>
  </v-autocomplete>
</template>

<script lang="ts">
import Vue from "vue";
import store from "@/store";
import { ServerConnection } from "@/models/serverConnection";
import { ColorByEnvironment } from "@/appsettings";

export default Vue.extend({
  name: "SelectServerConnection",
  methods: {
    selected(server: ServerConnection) {
      store.dispatch("changeActiveServer", server);
    },
    getChipColor(server: ServerConnection) {
      return ColorByEnvironment[server.environment];
    },
    edit(server: ServerConnection) {
      store.dispatch("changeEditServer", server);
      this.$emit("edit");
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