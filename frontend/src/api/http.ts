import axios from 'axios'

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
