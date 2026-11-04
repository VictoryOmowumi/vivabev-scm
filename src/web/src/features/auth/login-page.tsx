import { useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router'
import { HugeiconsIcon } from '@hugeicons/react'
import { DropletIcon } from '@hugeicons/core-free-icons'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { FullPageSpinner } from '@/components/layout/page-states'
import { ApiError } from '@/lib/api-client'
import { useCurrentUser, useLogin } from './use-auth'

export function LoginPage() {
  const { data: user, isPending } = useCurrentUser()
  const login = useLogin()
  const navigate = useNavigate()
  const location = useLocation()
  const from = (location.state as { from?: string } | null)?.from ?? '/'

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  if (isPending) return <FullPageSpinner />
  if (user) return <Navigate to={from} replace />

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    try {
      await login.mutateAsync({ email, password })
      navigate(from, { replace: true })
    } catch {
      // login.error holds it; the form shows it below
    }
  }

  const errorMessage =
    login.error instanceof ApiError ? (login.error.problem.title ?? login.error.message) : login.error?.message

  return (
    <main className="grid min-h-svh place-items-center p-4">
      <div className="flex w-full max-w-sm flex-col gap-6">
        <div className="flex items-center justify-center gap-2 font-heading text-2xl">
          <HugeiconsIcon icon={DropletIcon} size={22} className="text-brand" />
          VivaBev
        </div>

        <Card className="rounded-2xl bg-card backdrop-blur-sm">
          <CardHeader>
            <CardTitle className="font-heading text-2xl font-light">Sign in</CardTitle>
            <CardDescription>Use your VivaBev work email.</CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={onSubmit} className="flex flex-col gap-4" noValidate>
              <div className="flex flex-col gap-2">
                <Label htmlFor="email">Email</Label>
                <Input id="email" type="email" autoComplete="username" required
                  value={email} onChange={(e) => setEmail(e.target.value)} />
              </div>
              <div className="flex flex-col gap-2">
                <Label htmlFor="password">Password</Label>
                <Input id="password" type="password" autoComplete="current-password" required
                  value={password} onChange={(e) => setPassword(e.target.value)} />
              </div>

              {errorMessage && (
                <p role="alert" className="text-sm text-destructive">{errorMessage}</p>
              )}

              <Button type="submit" className="rounded-full" disabled={login.isPending}>
                {login.isPending ? 'Signing in…' : 'Sign in'}
              </Button>
            </form>
          </CardContent>
        </Card>

        {import.meta.env.DEV && (
          <p className="text-center text-xs text-muted-foreground">
            Demo: manager@vivabev.example or requester@vivabev.example, password Demo@2026!
          </p>
        )}
      </div>
    </main>
  )
}
