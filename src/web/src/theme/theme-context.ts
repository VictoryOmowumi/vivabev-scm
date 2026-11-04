import { createContext, useContext } from 'react'

export type Theme = 'light' | 'dark' | 'system'

export type ThemeContextValue = {
  theme: Theme // what the user chose
  resolvedTheme: 'light' | 'dark' // what is on screen
  setTheme: (theme: Theme) => void
}

export const ThemeContext = createContext<ThemeContextValue | null>(null)

export function useTheme(): ThemeContextValue {
  const value = useContext(ThemeContext)
  if (!value) throw new Error('useTheme must be used inside <ThemeProvider>')
  return value
}
