<template>
  <v-avatar size="20" tile>
    <v-img :src="require('../assets/db/' + this.params.value + '.png')"></v-img>
  </v-avatar>
</template>

<script lang="ts">
import Vue from "vue";

export default Vue.extend({
  name: "CellRendererDbms",
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
    getDbmsIcon() {
      return this.params.value ? "mdi-star" : "mdi-star-outline";
    }
  }
});
</script>
