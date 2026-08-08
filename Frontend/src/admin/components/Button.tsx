import { type ButtonHTMLAttributes, type ReactNode } from 'react'
import clsx from 'clsx'
import { PlusIcon } from './icons'

/**
 * One button component for every "Добавить X" / submit / destructive / secondary action across
 * the StorePartner, Cashier, and platform Admin cabinets (ADMIN_PROMPT follow-up: "приведи все
 * кнопки к одному компоненту с вариантами"). Every variant pairs its background with the token
 * that was DESIGNED to sit on it (--admin-accent-fg / --admin-success-fg / --admin-danger-fg, all
 * per-theme in index.css) instead of a hardcoded text-white -- that hardcoding is exactly what
 * made every "+ Добавить" button unreadable in dark mode (light fill, white-on-white text). Never
 * set a variant's text color independently of its background outside this file.
 */
const VARIANTS = {
  primary: 'bg-[color:var(--admin-accent)] text-[color:var(--admin-accent-fg)] hover:brightness-110 active:scale-[0.97]',
  danger: 'bg-[color:var(--admin-danger)] text-[color:var(--admin-danger-fg)] hover:brightness-110 active:scale-[0.97]',
  secondary:
    'border border-[color:var(--admin-border)] text-[color:var(--admin-text)] hover:bg-[color:var(--admin-hover)] active:scale-[0.97]',
  ghost:
    'text-[color:var(--admin-text-secondary)] hover:bg-[color:var(--admin-hover)] hover:text-[color:var(--admin-text)] active:scale-[0.97]',
} as const

const SIZES = {
  md: 'h-10 px-4 text-[13px] gap-1.5',
  sm: 'h-9 px-3.5 text-[12.5px] gap-1.5',
} as const

export type ButtonVariant = keyof typeof VARIANTS

interface ButtonProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'children'> {
  variant?: ButtonVariant
  size?: keyof typeof SIZES
  /** Shows a spinner in place of `icon` and disables the button, without changing its label --
   *  the caller still passes the resting label as `children`, same contract as every existing
   *  "Сохраняем…" button had, just centralized. */
  loading?: boolean
  icon?: ReactNode
  children: ReactNode
  className?: string
}

export function Button({ variant = 'primary', size = 'md', loading = false, icon, children, className = '', disabled, ...rest }: ButtonProps) {
  return (
    <button
      {...rest}
      disabled={disabled || loading}
      className={clsx(
        'inline-flex shrink-0 items-center justify-center rounded-xl font-semibold transition-[transform,filter,background-color,color] duration-150 disabled:pointer-events-none disabled:opacity-50',
        VARIANTS[variant],
        SIZES[size],
        className,
      )}
    >
      {loading ? <ButtonSpinner /> : icon}
      {children}
    </button>
  )
}

/** The specific "+ Добавить X" shape reused verbatim across every list page -- so the icon,
 *  gap, and weight can't quietly drift between Suppliers vs Reference vs Staff again. */
export function AddButton({ children, ...rest }: Omit<ButtonProps, 'variant' | 'icon'>) {
  return (
    <Button variant="primary" icon={<PlusIcon width={14} height={14} />} {...rest}>
      {children}
    </Button>
  )
}

/** Square, icon-only (close/menu/refresh/theme-toggle buttons) -- no label, no fixed bg by
 *  default so it sits quietly in a header until hovered, matching every existing icon button's
 *  hover treatment instead of each screen re-deriving its own. */
export function IconButton({
  icon,
  variant = 'ghost',
  size = 'md',
  loading = false,
  disabled,
  className = '',
  ...rest
}: Omit<ButtonProps, 'children' | 'icon'> & { icon: ReactNode }) {
  const dim = size === 'sm' ? 'h-8 w-8' : 'h-9 w-9'
  return (
    <button
      {...rest}
      disabled={disabled || loading}
      className={clsx(
        'grid shrink-0 place-items-center rounded-[10px] transition-[transform,filter,background-color,color] duration-150 disabled:pointer-events-none disabled:opacity-50',
        dim,
        variant === 'ghost'
          ? 'text-[color:var(--admin-text-secondary)] hover:bg-[color:var(--admin-hover)] hover:text-[color:var(--admin-text)]'
          : VARIANTS[variant],
        className,
      )}
    >
      {loading ? <ButtonSpinner /> : icon}
    </button>
  )
}

function ButtonSpinner() {
  return (
    <span
      aria-hidden
      className="h-[14px] w-[14px] shrink-0 animate-spin rounded-full border-2 border-current border-t-transparent opacity-70"
    />
  )
}
