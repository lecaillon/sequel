<template>
  <v-container
    :title="title"
    :index="index"
    @mouseover="mouseOver"
    @mouseleave="mouseleave"
    class="d-flex pa-0 subtitle-2"
  >
    {{ title }}
    <v-icon
      v-bind:style="{ visibility: btnClose }"
      @click="$emit('close', index)"
      small
      class="ml-1"
    >mdi-close</v-icon>
  </v-container>
</template>

<script lang="ts">
import Vue from "vue";

export default Vue.extend({
  name: "QueryTab",
  props: {
    title: String,
    index: Number
  },
  data: () => ({
    btnClose: "hidden"
  }),
  methods: {
    mouseOver() {
      this.btnClose = "visible";
    },
    mouseleave() {
      this.btnClose = "hidden";
    }
  }
});
</script>
