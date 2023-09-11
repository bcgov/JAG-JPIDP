<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import LayoutHeader from './components/LayoutHeader.vue';
import { onBeforeMount } from 'vue';
import { defineStore } from 'pinia'

onBeforeMount(() => {
  debugger;
    // get the hostname we're running on
  const hostname = window.location.hostname;
  
  const mode = import.meta.env.MODE;
  
});


</script>

<template>

  <div class="container-fluid">
      <div class="row">
    <LayoutHeader></LayoutHeader>
    </div>
    <div class="row mt-5 ml-5">
      <RouterView></RouterView>
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