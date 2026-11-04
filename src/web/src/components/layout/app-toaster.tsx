import { Toaster } from 'sonner'
import { useTheme } from '@/theme/theme-context'

/** Toasts after an action ("Approved REQ-..."). Dark pill, bottom right, gone after 5 seconds. */
export function AppToaster() {
  const { resolvedTheme } = useTheme()
  return (
    <Toaster
      theme={resolvedTheme}
      position="bottom-right"
      duration={5000}
      toastOptions={{
        classNames: {
          toast: '!rounded-2xl !border-0 !bg-inverse !text-inverse-foreground !font-sans !shadow-xl',
          description: '!text-inverse-muted',
          error: '!bg-destructive !text-white',
        },
      }}
    />
  )
}
