import { defineStore } from 'pinia'
import { fetchCurrentUser, login as apiLogin, logout as apiLogout, fetchCsrfToken, type CurrentUser } from '../api/authApi'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: null as CurrentUser | null,
    isReady: false,
  }),
  getters: {
    isAuthenticated: (state) => state.user !== null,
  },
  actions: {
    async fetchMe(): Promise<boolean> {
      try {
        this.user = await fetchCurrentUser()
        return true
      } catch {
        this.user = null
        return false
      } finally {
        this.isReady = true
      }
    },

    async login(userName: string, password: string) {
      await fetchCsrfToken()
      await apiLogin(userName, password)
      await this.fetchMe()
    },

    async logout() {
      await fetchCsrfToken()
      await apiLogout()
      this.user = null
    },
  },
})
