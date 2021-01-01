<template>
  <v-icon small :color="getQueryStatusColor">{{ getQueryStatusIcon }}</v-icon>
</template>

<script lang="ts">
import Vue from "vue";

export default Vue.extend({
  name: "CellRendererQueryStatus",
  data: () => ({
    params: {} as any
  }),
  methods: {
    refresh(params: any) {
      if (params.value !== this.params.value) {
        this.params = params;
      }
      return true;
    }
  },
  computed: {
    getQueryStatusIcon() {
      if (this.params.value === "Succeeded") {
        return "mdi-check";
      } else if (this.params.value === "Canceled") {
        return "mdi-pause-circle";
      } else if (this.params.value === "Failed") {
        return "mdi-close-circle";
      } else {
        return "";
      }
    },
    getQueryStatusColor() {
      if (this.params.value === "Succeeded") {
        return "green";
      } else if (this.params.value === "Canceled") {
        return "grey lighten-2";
      } else if (this.params.value === "Failed") {
        return "red";
      } else {
        return "";
      }
    }
  }
});
</script>
