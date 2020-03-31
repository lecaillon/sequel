<template>
  <v-container
    :name="name"
    :index="index"
    @mouseover="mouseOver"
    @mouseleave="mouseleave"
    class="d-flex pa-0"
  >
    {{ name }}
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
    name: String,
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
