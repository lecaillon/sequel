<template>
  <v-icon small :color="getStarIconColor">{{ getStarIcon }}</v-icon>
</template>

<script lang="ts">
import Vue from "vue";

export default Vue.extend({
  name: "CellRendererStar",
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
    getStarIcon() {
      return this.params.value ? "mdi-star" : "mdi-star-outline";
    },
    getStarIconColor() {
      return this.params.value ? "orange" : "grey lighten-2";
    }
  }
});
</script>
