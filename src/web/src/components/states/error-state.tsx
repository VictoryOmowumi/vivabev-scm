import { Link } from 'react-router'
import { Alert01Icon, FileNotFoundIcon, RefreshIcon, SquareLock02Icon } from '@hugeicons/core-free-icons'
import { HugeiconsIcon } from '@hugeicons/react'
import { Button } from '@/components/ui/button'
import { useCurrentUser } from '@/features/auth/use-auth'
import { ApiError } from '@/lib/api-client'
import { StateCard } from './state-card'

type ErrorStateProps = {
  error: Error
  /** What failed to load, in plain words: "requisitions", "REQ-2027-00099" */
  what: string
  onRetry?: () => void
  /** Where "back" goes from a 403 or 404 */
  back?: { to: string; label: string }
}

/** Picks the right card for a failed load: no access, not found, or something went wrong. */
export function ErrorState({ error, what, onRetry, back }: ErrorStateProps) {
  const status = error instanceof ApiError ? error.status : 0

  if (status === 403) return <ForbiddenState what={what} back={back} />

  if (status === 404) {
    return (
      <StateCard icon={FileNotFoundIcon} tone="warning" title={`We couldn't find ${what}`}
        actions={back && <Button asChild size="pill"><Link to={back.to}>{back.label}</Link></Button>}>
        It may have been mistyped, or the link is out of date.
      </StateCard>
    )
  }

  const traceId = error instanceof ApiError ? error.problem.traceId : undefined
  return (
    <StateCard icon={Alert01Icon} tone="error" title={`We couldn't load ${what}`}
      actions={onRetry && (
        <Button size="pill" onClick={onRetry}>
          <HugeiconsIcon icon={RefreshIcon} strokeWidth={1.5} /> Try again
        </Button>
      )}
      footer={traceId && (
        <span className="text-xs text-muted-foreground">
          Reference <code className="rounded-md bg-accent px-1.5 py-0.5">{traceId}</code>
        </span>
      )}>
      {status >= 500 || status === 0
        ? "The server didn't answer. This is usually brief. If it keeps happening, send IT the reference below."
        : error.message}
    </StateCard>
  )
}

export function ForbiddenState({ what, back }: { what: string; back?: { to: string; label: string } }) {
  const { data: user } = useCurrentUser()
  return (
    <StateCard icon={SquareLock02Icon} tone="warning" title={`You don't have access to ${what}`}
      actions={<>
        {back && <Button asChild size="pill"><Link to={back.to}>{back.label}</Link></Button>}
        <Button asChild size="pill" variant="outline"><Link to="/">Go to dashboard</Link></Button>
      </>}>
      If you need it, ask your manager or the IT service desk.
      {user && <> You're signed in as {user.displayName} ({user.roles.join(', ')}).</>}
    </StateCard>
  )
}
