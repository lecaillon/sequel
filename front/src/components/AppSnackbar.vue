<template>
  <v-snackbar
    :value="show"
    :color="color"
    :timeout="6000"
    @input="$emit('close')"
    vertical
    bottom
    right
  >
    {{ message }}
    <ul v-if="details">
      <li v-for="(detail, index) in details" :key="index">{{ detail }}</li>
    </ul>
    <v-btn text @click="$emit('close')">Close</v-btn>
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
