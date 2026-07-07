import axios, { type InternalAxiosRequestConfig } from 'axios'

declare module 'axios' {
  export interface InternalAxiosRequestConfig {
    _csrfRetried?: boolean
  }
}

export const http = axios.create({
  baseURL: '/api',
  withCredentials: true,
})

function readCookie(name: string): string | null {
  const match = document.cookie.match(new RegExp(`(?:^|; )${name}=([^;]*)`))
  return match ? decodeURIComponent(match[1]) : null
}

// Mini-SSO uses the double-submit CSRF pattern: attach the XSRF-TOKEN cookie
// value as a header on every state-changing request (auth and todos alike).
http.interceptors.request.use((config) => {
  const method = config.method?.toLowerCase()
  if (method && ['post', 'put', 'patch', 'delete'].includes(method)) {
    const csrfToken = readCookie('XSRF-TOKEN')
    if (csrfToken) {
      config.headers['X-CSRF-TOKEN'] = csrfToken
    }
  }
  return config
})

// The XSRF-TOKEN cookie can go stale (or get cleared) well before the "token"
// session cookie does, since nothing here proactively refreshes it after
// login. When that happens, state-changing requests fail CsrfValidationFilter
// with 403 even though the user is still logged in. Re-fetch the CSRF cookie
// once and retry, instead of leaving the action silently no-op'd.
http.interceptors.response.use(
  (response) => response,
  async (error) => {
    const { config, response } = error
    if (response?.status === 403 && config && !config._csrfRetried) {
      config._csrfRetried = true
      try {
        await http.get('/auth/csrf')
        return http(config)
      } catch {
        // fall through to reject with the original error
      }
    }

    if (response?.status === 401) {
      window.location.href = `/login?redirect=${encodeURIComponent(window.location.pathname)}`
    }

    return Promise.reject(error)
  },
)
