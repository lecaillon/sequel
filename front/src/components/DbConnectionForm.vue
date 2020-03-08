<template>
  <v-dialog :value="show" persistent max-width="600px">
    <v-card>
      <v-card-title>
        <span class="headline">Add new database connection</span>
      </v-card-title>
      <v-card-text>
        <v-container>
          <v-row>
            <v-col cols="12" sm="6">
              <v-text-field label="Name*" required></v-text-field>
            </v-col>
            <v-col cols="12" sm="6">
              <v-select clearable
                :items="['Development', 'Testing', 'Staging', 'UAT', 'Demo', 'Production']"
                label="Environment"
              ></v-select>
            </v-col>
            <v-col cols="12">
              <v-text-field label="Connection string*" required></v-text-field>
            </v-col>
          </v-row>
        </v-container>
        <small>*indicates required field</small>
      </v-card-text>
      <v-card-actions>
        <v-spacer></v-spacer>
        <v-btn text @click.stop="close">Cancel</v-btn>
        <v-btn text @click.stop="close">Test</v-btn>
        <v-btn text @click.stop="close">Save</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script lang="ts">
import Vue from "vue";

export default Vue.extend({
  name: "DbConnectionForm",
  props: {
    show: Boolean
  },
  methods: {
    close() {
      this.$emit("close");
    }
  }
});
</script>

<style>
</style>