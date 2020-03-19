<template>
  <v-dialog :value="show" @click:outside="close" max-width="600px">
    <v-card>
      <v-card-title>
        <span class="headline">Add new database connection</span>
      </v-card-title>
      <v-card-text>
        <v-container>
          <v-row>
            <v-col cols="12" sm="6">
              <v-text-field label="Name*" v-model="name"></v-text-field>
            </v-col>
            <v-col cols="12" sm="6">
              <v-select
                label="Type*"
                v-model="type"
                :items="['PostgreSQL', 'SQLServer']"
                clearable
              ></v-select>
            </v-col>
            <v-col cols="12">
              <v-text-field label="Connection string*" v-model="connectionString"></v-text-field>
            </v-col>
            <v-col cols="12" sm="6">
              <v-select
                label="Environment*"
                v-model="environment"
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
        <v-btn text @click.stop="test" :loading="testing">Test</v-btn>
        <v-btn text @click.stop="del">Delete</v-btn>
        <v-btn text @click.stop="add">Save</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script lang="ts">
import Vue from "vue";
import store from "@/store";
import { ServerConnection } from "@/models/serverConnection";

export default Vue.extend({
  name: "FormServerConnection",
  props: {
    show: Boolean,
    id: Number,
    name: String,
    type: String,
    connectionString: String,
    environment: String
  },
  data: () => ({
    testing: false
  }),
  methods: {
    close() {
      this.$emit("close");
    },
    del() {
      store.dispatch("deleteServer", this.id).finally(() => this.close());
    },
    add() {
      store
        .dispatch("addServer", {
          id: this.id,
          name: this.name,
          type: this.type,
          connectionString: this.connectionString,
          environment: this.environment
        } as ServerConnection)
        .finally(() => this.close());
    },
    test() {
      this.testing = true;
      const server = {
        id: this.id,
        name: this.name,
        type: this.type,
        connectionString: this.connectionString,
        environment: this.environment
      } as ServerConnection;
      store.dispatch("changeEditServer", server);
      store
        .dispatch("testServer", server)
        .finally(() => (this.testing = false));
    }
  }
});
</script>

<style>
</style>