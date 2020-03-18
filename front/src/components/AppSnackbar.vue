<template>
  <v-snackbar v-model="show" :color="color" :timeout="6000" vertical bottom right>
    {{ message }}
    <ul v-if="details">
      <li v-for="(detail, index) in details" :key="index">{{ detail }}</li>
    </ul>
    <v-btn text @click="show = false">Close</v-btn>
  </v-snackbar>
</template>

<script lang="ts">
import Vue from "vue";

export default Vue.extend({
  name: "AppSnackbar",
  props: {
    show: Boolean,
    color: String,
    message: String,
    details: Array
  }
});
</script>