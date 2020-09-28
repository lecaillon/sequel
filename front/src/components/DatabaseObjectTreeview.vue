<template>
  <v-card class="mx-auto" style="overflow: visible" flat tile>
    <v-sheet>
      <v-text-field
        v-model="nodeSearch"
        label="Filter items"
        flat
        dense
        solo
        hide-details
        clearable
      ></v-text-field>
    </v-sheet>
    <v-card-text @contextmenu.prevent="rightClick" style="padding: 10px 0 0 0">
      <v-treeview
        activatable
        dense
        return-object
        :items="nodes"
        :load-children="fetchNodes"
        :search="nodeSearch"
        :open.sync="openedNodes"
        @update:active="nodeSelected"
      >
        <template v-slot:prepend="{ item }">
          <v-icon :color="item.color" small>{{ item.icon }}</v-icon>
        </template>
      </v-treeview>
    </v-card-text>
    <v-menu
      v-model="showMenu"
      :position-x="x"
      :position-y="y"
      absolute
      offset-y
    >
      <v-list dense>
        <v-list-item
          v-for="(item, index) in menu"
          :key="index"
          @click="menuItemSelected(item)"
        >
          <v-list-item-icon class="mr-2">
            <v-icon size="18" v-text="item.icon"></v-icon>
          </v-list-item-icon>
          <v-list-item-content>
            <v-list-item-title style="font-weight: normal">{{
              item.title
            }}</v-list-item-title>
          </v-list-item-content>
        </v-list-item>
      </v-list>
    </v-menu>
  </v-card>
</template>

<script lang="ts">
import Vue from "vue";
import store from "@/store";
import { TreeViewNode } from "@/models/treeViewNode";
import { TreeViewMenuItem } from "@/models/treeViewMenuItem";

export default Vue.extend({
  name: "DatabaseObjectTreeview",
  data: () => ({
    openedNodes: [],
    nodeSearch: null,
    showMenu: false,
    x: 0,
    y: 0
  }),
  methods: {
    nodeSelected(nodes: TreeViewNode[]) {
      store.dispatch("changeActiveNode", nodes.length == 0 ? {} : nodes[0]);
    },
    async fetchNodes(parent: TreeViewNode) {
      await store.dispatch("fetchTreeViewNodes", parent);
    },
    async rightClick(e: MouseEvent) {
      (document.elementFromPoint(e.clientX, e.clientY) as HTMLElement).click();
      if (Object.keys(store.state.activeNode).length === 0) {
        (document.elementFromPoint(
          e.clientX,
          e.clientY
        ) as HTMLElement).click();
      }

      await store.dispatch("fetchActiveNodeMenuItems");
      this.showMenu = false;
      this.x = e.clientX;
      this.y = e.clientY;
      this.$nextTick(() => {
        this.showMenu = true;
      });
    },
    menuItemSelected(item: TreeViewMenuItem) {
      store.dispatch("executeTreeViewMenuItem", item);
    }
  },
  computed: {
    nodes() {
      return store.state.nodes;
    },
    menu() {
      return store.state.activeNodeMenu;
    }
  }
});
</script>

<style>
.v-icon.v-icon::after {
  height: 0%;
}
</style>