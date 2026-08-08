import { forwardRef, type InputHTMLAttributes, type TextareaHTMLAttributes } from 'react'

// Same scheme split as Select.tsx: 'admin' for the StorePartner cabinet,
// 'mod' for the platform-Admin moderation shell — two independent token
// namespaces (see src/index.css), one shared component.
const SCHEMES = {
  admin: {
    field:
      'border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] text-[color:var(--admin-text)] placeholder:text-[color:var(--admin-text-tertiary)] focus-visible:border-[color:var(--admin-accent)] focus-visible:shadow-[0_0_0_3px_var(--admin-accent-soft)]',
    invalid: 'border-[color:var(--admin-danger)] focus-visible:shadow-[0_0_0_3px_var(--admin-danger-dim)]',
  },
} as const

const SIZES = {
  md: 'rounded-xl px-3.5 py-2.5 text-[13px]',
  sm: 'rounded-lg px-2.5 py-1.5 text-[12.5px]',
} as const

type Scheme = keyof typeof SCHEMES
type UiSize = keyof typeof SIZES

interface Shared {
  scheme?: Scheme
  // Named uiSize, not size: HTMLInputElement's native `size` attribute is a
  // number (visible character width) and would collide with this variant enum.
  uiSize?: UiSize
  invalid?: boolean
}

function fieldClass(t: (typeof SCHEMES)[Scheme], uiSize: UiSize, invalid: boolean | undefined, extra: string) {
  return `w-full border outline-none transition-[border-color,box-shadow] duration-200 disabled:cursor-not-allowed disabled:opacity-50 ${SIZES[uiSize]} ${invalid ? t.invalid : t.field} ${extra}`
}

export const Input = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement> & Shared>(
  ({ scheme = 'admin', uiSize = 'md', invalid, className = '', ...rest }, ref) => (
    <input ref={ref} {...rest} className={fieldClass(SCHEMES[scheme], uiSize, invalid, className)} />
  ),
)
Input.displayName = 'Input'

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaHTMLAttributes<HTMLTextAreaElement> & Shared>(
  ({ scheme = 'admin', uiSize = 'md', invalid, className = '', ...rest }, ref) => (
    <textarea ref={ref} {...rest} className={`resize-none ${fieldClass(SCHEMES[scheme], uiSize, invalid, className)}`} />
  ),
)
Textarea.displayName = 'Textarea'
