<template>
  <v-autocomplete
    placeholder="Select a database"
    prepend-icon="mdi-database"
    dense
    hide-details
    clearable
    solo
    :value="displayedItem"
    :items="databases"
    @input="selected"
  ></v-autocomplete>
</template>

<script lang="ts">
import Vue from "vue";
import store from "@/store";

export default Vue.extend({
  name: "SelectDatabase",
  methods: {
    selected(database: string) {
      store.dispatch("changeActiveDatabase", database);
    }
  },
  computed: {
    databases() {
      return store.state.databases;
    },
    displayedItem() {
      return store.state.activeDatabase;
    }
  }
});
</script>
