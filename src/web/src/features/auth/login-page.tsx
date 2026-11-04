import { useEffect, useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router'
import { HugeiconsIcon } from '@hugeicons/react'
import { Alert01Icon, DropletIcon, ViewIcon, ViewOffSlashIcon } from '@hugeicons/core-free-icons'
import { FullPageSpinner } from '@/components/layout/page-states'
import { ThemeToggle } from '@/components/layout/theme-toggle'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ApiError } from '@/lib/api-client'
import { cn } from '@/lib/utils'
import { LoginArtwork } from './login-artwork'
import { isSessionExpired } from './session'
import { useCurrentUser, useLogin } from './use-auth'

const slides = [
  { title: 'Every bottle starts with a request.', body: 'Raise what your plant or team needs, and see the budget impact before you submit.' },
  { title: 'Approvals in the right order, every time.', body: 'Routed by amount, department and budget, with a full trail of who decided what.' },
  { title: 'Ask Copilot, not a spreadsheet.', body: 'Why is it late? Why is it over budget? Answers from the data you are allowed to see.' },
]

// Shown only by `npm run dev`; a production build leaves them out
const demoAccounts = [
  { email: 'manager@vivabev.example', name: 'Funmi', role: 'Head of IT' },
  { email: 'requester@vivabev.example', name: 'Chinedu', role: 'Requester' },
  { email: 'finance@vivabev.example', name: 'Amaka', role: 'Finance' },
  { email: 'md@vivabev.example', name: 'Babatunde', role: 'Managing Director' },
]

export function LoginPage() {
  const { data: user, isPending } = useCurrentUser()
  const login = useLogin()
  const navigate = useNavigate()
  const location = useLocation()
  const from = (location.state as { from?: string } | null)?.from ?? '/'

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [rememberMe, setRememberMe] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  const [slide, setSlide] = useState(0)

  // Move to the next highlight every 7 seconds, unless the OS asks for less motion
  useEffect(() => {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return
    const timer = window.setInterval(() => setSlide((s) => (s + 1) % slides.length), 7000)
    return () => window.clearInterval(timer)
  }, [])

  if (isPending) return <FullPageSpinner />
  if (user) return <Navigate to={from} replace />

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    try {
      await login.mutateAsync({ email, password, rememberMe })
      navigate(from, { replace: true })
    } catch {
      // login.error holds it; the banner shows it
    }
  }

  const problem = login.error instanceof ApiError ? login.error.problem : undefined
  const banner = login.error
    ? problem?.code === 'locked_out'
      ? { tone: 'warn', title: 'Too many failed attempts.', body: 'Try again in 5 minutes, or ask the IT service desk to unlock you.' }
      : login.error instanceof ApiError && login.error.status === 401
        ? { tone: 'error', title: 'Invalid email or password.', body: 'Check both and try again.' }
        : { tone: 'error', title: "Can't reach VivaBev.", body: problem?.title ?? login.error.message }
    : isSessionExpired()
      ? { tone: 'info', title: 'Your session ended.', body: 'Sign in again to carry on where you left off.' }
      : null
  const wrongPassword = login.error instanceof ApiError && login.error.status === 401 && problem?.code !== 'locked_out'

  return (
    <div className="flex min-h-svh flex-wrap bg-surface">
      {/* Left: the picture */}
      <section aria-label="About VivaBev SCM"
        className="relative hidden min-h-svh flex-[1_1_560px] overflow-hidden bg-[#0f2a1c] text-white lg:block">
        <LoginArtwork />
        <div className="relative flex h-full min-h-svh flex-col justify-between px-10 pt-8 pb-11">
          <div className="flex items-center gap-2.5">
            <span className="flex size-10 items-center justify-center rounded-full bg-white text-[#2f7a35]">
              <HugeiconsIcon icon={DropletIcon} size={20} strokeWidth={1.6} />
            </span>
            <span className="font-heading text-xl font-medium">VivaBev</span>
          </div>
          <div className="flex max-w-[460px] flex-col gap-3" aria-live="polite">
            <h1 className="text-[40px] leading-tight font-medium">{slides[slide]!.title}</h1>
            <p className="text-[15px] leading-relaxed text-white/80">{slides[slide]!.body}</p>
            <div role="group" aria-label="Highlights" className="mt-2.5 flex gap-1.5">
              {slides.map((s, i) => (
                <button key={s.title} aria-label={`Show highlight ${i + 1} of ${slides.length}`}
                  aria-pressed={i === slide} onClick={() => setSlide(i)}
                  className={cn('h-1.5 rounded-full transition-all', i === slide ? 'w-7 bg-white' : 'w-1.5 bg-white/45')} />
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* Right: the form */}
      <section aria-labelledby="signin-title" className="flex min-h-svh flex-[1_1_560px] flex-col px-6 pt-7 pb-8 sm:px-10">
        <div className="flex justify-end"><ThemeToggle /></div>

        <div className="flex flex-1 items-center justify-center py-8">
          <div className="flex w-full max-w-[400px] flex-col gap-5">
            <div>
              <h2 id="signin-title" className="text-[32px] leading-tight font-semibold">Welcome back to VivaBev</h2>
              <p className="mt-1.5 text-sm text-muted-foreground">Sign in with your work account</p>
            </div>

            {banner && (
              <div role={banner.tone === 'info' ? 'status' : 'alert'}
                className={cn('flex items-start gap-2.5 rounded-xl px-3.5 py-3 text-sm leading-snug',
                  banner.tone === 'error' && 'bg-destructive/10 text-destructive',
                  banner.tone === 'warn' && 'bg-pending text-pending-foreground',
                  banner.tone === 'info' && 'bg-accent text-foreground')}>
                <HugeiconsIcon icon={Alert01Icon} size={18} strokeWidth={1.5} className="mt-px flex-none" />
                <span><strong>{banner.title}</strong> {banner.body}</span>
              </div>
            )}

            <form onSubmit={onSubmit} className="flex flex-col gap-4" noValidate>
              <div className="flex flex-col gap-2">
                <Label htmlFor="email" className="text-[13px] font-semibold">Work email</Label>
                <Input id="email" type="email" autoComplete="username" required autoFocus
                  value={email} onChange={(e) => setEmail(e.target.value)}
                  className="h-12 rounded-xl bg-surface px-3.5 text-[15px] md:text-[15px]" />
              </div>
              <div className="flex flex-col gap-2">
                <Label htmlFor="password" className="text-[13px] font-semibold">Password</Label>
                <div className="relative">
                  <Input id="password" type={showPassword ? 'text' : 'password'} autoComplete="current-password" required
                    value={password} onChange={(e) => setPassword(e.target.value)} aria-invalid={wrongPassword}
                    className="h-12 rounded-xl bg-surface px-3.5 pr-12 text-[15px] md:text-[15px]" />
                  <button type="button" onClick={() => setShowPassword((v) => !v)}
                    aria-label={showPassword ? 'Hide password' : 'Show password'}
                    className="absolute top-1.5 right-1.5 flex size-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-accent">
                    <HugeiconsIcon icon={showPassword ? ViewOffSlashIcon : ViewIcon} size={18} strokeWidth={1.5} />
                  </button>
                </div>
              </div>
              <div className="flex items-center justify-between text-[13px]">
                <label className="flex cursor-pointer items-center gap-2">
                  <input type="checkbox" checked={rememberMe} onChange={(e) => setRememberMe(e.target.checked)}
                    className="size-4 accent-primary" />
                  Keep me signed in
                </label>
                <span className="text-muted-foreground" title="Passwords are reset by the IT service desk">
                  Forgot it? Ask IT.
                </span>
              </div>
              <Button type="submit" size="pill" className="h-12 rounded-xl text-[15px]" disabled={login.isPending}>
                {login.isPending ? 'Signing in…' : 'Sign in'}
              </Button>
            </form>

            {import.meta.env.DEV && (
              <>
                <div className="flex items-center gap-3 text-xs text-muted-foreground">
                  <span className="h-px flex-1 bg-border" /> Demo accounts · development only <span className="h-px flex-1 bg-border" />
                </div>
                <div className="grid grid-cols-2 gap-2.5">
                  {demoAccounts.map((a) => (
                    <button key={a.email} type="button" onClick={() => { setEmail(a.email); setPassword('Demo@2026!') }}
                      className="flex items-center gap-2.5 rounded-xl border bg-background/40 px-3 py-2.5 text-left text-[13px] hover:bg-accent">
                      <span className="flex size-[30px] flex-none items-center justify-center rounded-full bg-primary text-[11px] font-bold text-primary-foreground">
                        {a.name[0]}
                      </span>
                      <span><strong className="block">{a.name}</strong><span className="text-muted-foreground">{a.role}</span></span>
                    </button>
                  ))}
                </div>
              </>
            )}
          </div>
        </div>

        <p className="text-center text-[13px] text-muted-foreground">No account yet? Accounts are created by IT.</p>
      </section>
    </div>
  )
}
