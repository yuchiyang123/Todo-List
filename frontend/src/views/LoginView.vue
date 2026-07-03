<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { externalLoginUrl } from '../api/authApi'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const userName = ref('')
const password = ref('')
const errorMessage = ref('')
const isSubmitting = ref(false)

async function handleSubmit() {
  errorMessage.value = ''
  isSubmitting.value = true
  try {
    await auth.login(userName.value, password.value)
    const redirect = (route.query.redirect as string) || '/todos'
    router.push(redirect)
  } catch {
    errorMessage.value = '帳號或密碼錯誤，或帳號已被鎖定，請稍後再試'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="card auth-card">
    <h1>登入</h1>
    <form class="auth-form" @submit.prevent="handleSubmit">
      <div class="field">
        <label for="userName">帳號</label>
        <input id="userName" v-model="userName" required autocomplete="username" />
      </div>
      <div class="field">
        <label for="password">密碼</label>
        <input id="password" v-model="password" type="password" required autocomplete="current-password" />
      </div>
      <p v-if="errorMessage" class="error-text">{{ errorMessage }}</p>
      <button class="btn btn-primary" type="submit" :disabled="isSubmitting">登入</button>
    </form>

    <div class="divider"><span>或使用</span></div>

    <div class="oauth-buttons">
      <a class="btn" :href="externalLoginUrl('google')">使用 Google 登入</a>
      <a class="btn" :href="externalLoginUrl('github')">使用 GitHub 登入</a>
    </div>

    <p class="switch-link">
      還沒有帳號？<router-link to="/register">註冊</router-link>
    </p>
  </div>
</template>

<style scoped>
.auth-card {
  max-width: 360px;
  margin: 40px auto 0;
  padding: var(--spacing-6);
}

h1 {
  font-size: 20px;
  margin-bottom: var(--spacing-5);
  text-align: center;
}

.auth-form {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-4);
}

.divider {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
  margin: var(--spacing-5) 0;
  color: var(--text);
  font-size: 13px;
}

.divider::before,
.divider::after {
  content: '';
  flex: 1;
  border-top: 1px solid var(--border);
}

.oauth-buttons {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}

.oauth-buttons a {
  text-decoration: none;
  text-align: center;
}

.switch-link {
  text-align: center;
  margin-top: var(--spacing-5);
  font-size: 14px;
}
</style>
