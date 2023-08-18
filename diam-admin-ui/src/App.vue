<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import LayoutHeader from './components/LayoutHeader.vue';
import SideBar from './components/SideBar.vue';

</script>

<template>
  <LayoutHeader></LayoutHeader>

  <div class="container-fluid d-flex h-100">
    <div class="row">
      <div class="col-1  ps-0" style="min-width:180px;">
        <SideBar />
      </div>
      <div class="col pt-5 h-100">
        <RouterView></RouterView>

      </div>

    </div>
  </div>
</template>


<style>
#app {
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  height: 98vh;
  font-family: Avenir, Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

.content {
  /*  */
}
</style>