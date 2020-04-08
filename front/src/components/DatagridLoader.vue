<template>
  <v-progress-circular indeterminate size="80" width="5" color="primary">
    <span class="display-1 font-weight-bold">{{elapsed}}</span>
  </v-progress-circular>
</template>

<script lang="ts">
import Vue from "vue";

export default Vue.extend({
  name: "DatagidLoader",
  data: () => ({
    elapsed: null as number | null,
    timer: 0
  }),
  methods: {
    startCounter() {
      this.timer = setInterval(() => {
        if (this.elapsed === null) {
          this.elapsed = 1;
        } else {
          this.elapsed++;
        }
      }, 1000);
    }
  },
  mounted() {
    this.startCounter();
  },
  beforeDestroy() {
    clearInterval(this.timer);
  }
});
</script>
