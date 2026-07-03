import { http } from './http'

export interface CurrentUser {
  userId: string
  userName: string
  email: string | null
}

export async function fetchCsrfToken(): Promise<void> {
  await http.get('/auth/csrf')
}

export async function login(userName: string, password: string): Promise<void> {
  await http.post('/auth', { userName, password })
}

export async function register(userName: string, email: string, password: string): Promise<void> {
  await http.post('/auth/create', { userName, email, password })
}

export async function checkUsernameAvailable(userName: string): Promise<boolean> {
  const { data } = await http.get<boolean>('/auth/valid/username', {
    params: { userName },
  })
  return data
}

export async function logout(): Promise<void> {
  await http.post('/auth/logout')
}

export async function fetchCurrentUser(): Promise<CurrentUser> {
  const { data } = await http.get<CurrentUser>('/auth/me')
  return data
}

// OAuth logins are full-page redirects that Google/GitHub send back to a fixed,
// pre-registered URL — they must go straight to Mini-SSO's own public origin,
// not through this app's gateway (proxying would strip the port from the Host
// header along the way, breaking the registered redirect_uri). Cookies are
// host-scoped, not port-scoped, so the session Mini-SSO sets here still works
// once the browser comes back to this app on a different port.
const MINI_SSO_ORIGIN = import.meta.env.VITE_MINI_SSO_ORIGIN ?? 'http://localhost:12080'

export function externalLoginUrl(provider: 'google' | 'github'): string {
  return `${MINI_SSO_ORIGIN}/api/auth/external/${provider}/login`
}
