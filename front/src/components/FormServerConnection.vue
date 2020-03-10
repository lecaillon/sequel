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
              <v-text-field label="Name*" v-model="cnn.name" required></v-text-field>
            </v-col>
            <v-col cols="12" sm="6">
              <v-select
                label="Type*"
                v-model="cnn.type"
                :items="['PostgreSQL', 'SQL Server']"
                required
                clearable
              ></v-select>
            </v-col>
            <v-col cols="12">
              <v-text-field label="Connection string*" v-model="cnn.connectionString" required></v-text-field>
            </v-col>
            <v-col cols="12" sm="6">
              <v-select
                label="Environment"
                v-model="cnn.environment"
                :items="['Development', 'Testing', 'Staging', 'UAT', 'Demo', 'Production']"
                clearable
              ></v-select>
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
import { ServerConnection } from "../models/serverConnection";

export default Vue.extend({
  name: "FormServerConnection",
  props: {
    show: Boolean
  },
  data: () => ({
    cnn: {} as ServerConnection
  }),
  methods: {
    close() {
      this.$emit("close");
    }
  }
});
</script>

<style>
</style>