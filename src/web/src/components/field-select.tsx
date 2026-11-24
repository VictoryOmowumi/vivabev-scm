import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'

export type SelectOption = { value: string; label: string; disabled?: boolean }

/**
 * The dropdown for our forms: a list of options and a string value, like a native select, but drawn with the
 * shadcn Select so it opens as our own styled menu. An empty value shows the placeholder.
 */
export function FieldSelect({
  id, value, onChange, options, placeholder = 'Choose…', disabled, required, size, className, ...aria
}: {
  id?: string
  value: string
  onChange: (value: string) => void
  options: SelectOption[]
  placeholder?: string
  disabled?: boolean
  required?: boolean
  size?: 'sm' | 'default'
  className?: string
  'aria-label'?: string
  'aria-invalid'?: boolean
}) {
  return (
    // An empty string clears the selection and shows the placeholder
    <Select value={value} onValueChange={onChange} disabled={disabled} required={required}>
      <SelectTrigger id={id} size={size} className={className} {...aria}>
        <SelectValue placeholder={placeholder} />
      </SelectTrigger>
      <SelectContent>
        {options.map((o) => (
          <SelectItem key={o.value} value={o.value} disabled={o.disabled}>{o.label}</SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}
