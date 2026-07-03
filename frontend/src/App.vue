<script setup lang="ts">
import NavBar from './components/NavBar.vue'
</script>

<template>
  <NavBar />
  <main class="page">
    <router-view />
  </main>
</template>

<style scoped>
.page {
  flex: 1;
  width: 100%;
  max-width: 720px;
  margin: 0 auto;
  padding: var(--spacing-6) 20px;
}
</style>
