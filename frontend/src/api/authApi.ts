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

// Kept same-origin (proxied to Mini-SSO by this app's gateway) rather than
// linking straight at Mini-SSO's own address. The registered OAuth
// redirect_uri (e.g. https://todo.example.com/signin-google) is this app's
// own domain, so the whole round-trip — login kick-off, Google/GitHub
// consent, and the callback back to /signin-google — needs to stay on this
// origin. That also means the "token"/"XSRF-TOKEN" cookies Mini-SSO sets
// during the callback are scoped to this domain, not Mini-SSO's, so the
// session is actually visible once the SPA reloads afterward.
export function externalLoginUrl(provider: 'google' | 'github'): string {
  return `/api/auth/external/${provider}/login`
}
