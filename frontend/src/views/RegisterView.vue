<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { register, checkUsernameAvailable, fetchCsrfToken } from '../api/authApi'

const router = useRouter()

const userName = ref('')
const email = ref('')
const password = ref('')
const errorMessage = ref('')
const usernameStatus = ref<'idle' | 'checking' | 'available' | 'taken'>('idle')
const isSubmitting = ref(false)

let checkTimer: ReturnType<typeof setTimeout> | undefined

function onUsernameInput() {
  usernameStatus.value = 'idle'
  clearTimeout(checkTimer)
  if (!userName.value) return

  checkTimer = setTimeout(async () => {
    usernameStatus.value = 'checking'
    try {
      const available = await checkUsernameAvailable(userName.value)
      usernameStatus.value = available ? 'available' : 'taken'
    } catch {
      usernameStatus.value = 'idle'
    }
  }, 400)
}

async function handleSubmit() {
  errorMessage.value = ''
  isSubmitting.value = true
  try {
    await fetchCsrfToken()
    await register(userName.value, email.value, password.value)
    router.push({ name: 'login' })
  } catch {
    errorMessage.value = '註冊失敗，請確認帳號是否已被使用或密碼是否符合規則'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="card auth-card">
    <h1>註冊</h1>
    <form class="auth-form" @submit.prevent="handleSubmit">
      <div class="field">
        <label for="userName">帳號</label>
        <input id="userName" v-model="userName" required autocomplete="username" @input="onUsernameInput" />
        <p v-if="usernameStatus === 'checking'" class="hint">檢查中...</p>
        <p v-else-if="usernameStatus === 'available'" class="hint hint-ok">此帳號可以使用</p>
        <p v-else-if="usernameStatus === 'taken'" class="error-text">此帳號已被使用</p>
      </div>
      <div class="field">
        <label for="email">電子郵件</label>
        <input id="email" v-model="email" type="email" required autocomplete="email" />
      </div>
      <div class="field">
        <label for="password">密碼</label>
        <input id="password" v-model="password" type="password" required autocomplete="new-password" />
      </div>
      <p v-if="errorMessage" class="error-text">{{ errorMessage }}</p>
      <button class="btn btn-primary" type="submit" :disabled="isSubmitting">建立帳號</button>
    </form>

    <p class="switch-link">
      已經有帳號？<router-link to="/login">登入</router-link>
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

.hint {
  font-size: 13px;
  color: var(--text);
}

.hint-ok {
  color: var(--success);
}

.switch-link {
  text-align: center;
  margin-top: var(--spacing-5);
  font-size: 14px;
}
</style>
