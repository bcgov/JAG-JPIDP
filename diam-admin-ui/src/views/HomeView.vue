<script setup lang="ts">
const publicEnvVar = import.meta.env.VITE_MY_ENV_VARIABLE;

</script>

<template>

  <div class="mt-5">
  <h1>Welcome to the BCPS DIAM Admin Tool</h1>
  <h2>Environment {{ publicEnvVar  }}</h2>
</div>

</template>

<style scoped>
.bd-placeholder-img {
  font-size: 1.125rem;
  text-anchor: middle;
  -webkit-user-select: none;
  -moz-user-select: none;
  user-select: none;
}

@media (min-width: 768px) {
  .bd-placeholder-img-lg {
    font-size: 3.5rem;
  }
}
</style>
