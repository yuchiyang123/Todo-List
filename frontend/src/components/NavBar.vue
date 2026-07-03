<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()

async function handleLogout() {
  await auth.logout()
  router.push({ name: 'login' })
}
</script>

<template>
  <header class="navbar">
    <div class="navbar-inner">
      <span class="brand">Todo List</span>
      <div v-if="auth.isAuthenticated" class="navbar-right">
        <span class="username">{{ auth.user?.userName }}</span>
        <button class="btn btn-ghost" @click="handleLogout">登出</button>
      </div>
    </div>
  </header>
</template>

<style scoped>
.navbar {
  border-bottom: 1px solid var(--border);
  background: var(--surface);
}

.navbar-inner {
  max-width: 720px;
  margin: 0 auto;
  padding: 14px 20px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.brand {
  font-weight: 600;
  color: var(--text-h);
}

.navbar-right {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
}

.username {
  font-size: 14px;
  color: var(--text);
}
</style>
