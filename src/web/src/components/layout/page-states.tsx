import { HugeiconsIcon } from '@hugeicons/react'
import { Alert02Icon, Loading03Icon } from '@hugeicons/core-free-icons'

export function FullPageSpinner() {
  return (
    <div className="grid min-h-svh place-items-center text-muted-foreground" role="status">
      <HugeiconsIcon icon={Loading03Icon} size={28} className="animate-spin" />
      <span className="sr-only">Loading</span>
    </div>
  )
}

export function FullPageError({ message }: { message: string }) {
  return (
    <div className="grid min-h-svh place-items-center p-4">
      <div className="flex max-w-md flex-col items-center gap-3 text-center">
        <HugeiconsIcon icon={Alert02Icon} size={28} className="text-destructive" />
        <h1 className="text-2xl font-light">Can't reach VivaBev</h1>
        <p className="text-sm text-muted-foreground">{message}</p>
        <p className="text-sm text-muted-foreground">Is the API running on https://localhost:7120?</p>
      </div>
    </div>
  )
}
