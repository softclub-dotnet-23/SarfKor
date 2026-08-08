import type { ReactNode, TdHTMLAttributes, ThHTMLAttributes } from 'react'

// Replaces the 3 hand-rolled <table> shells (Reports/Inventory/Staff). The
// horizontal-scroll wrapper is what makes this mobile-safe by construction —
// individual pages no longer need to remember to handle narrow viewports.
const SCHEMES = {
  admin: {
    head: 'text-[color:var(--admin-text-tertiary)]',
    row: 'border-[color:var(--admin-border)]',
    rowHover: 'hover:bg-[color:var(--admin-hover)]',
  },
} as const

type Scheme = keyof typeof SCHEMES

export function Table({ children, className = '' }: { children: ReactNode; className?: string }) {
  return (
    <div className="-mx-1 overflow-x-auto px-1">
      <table className={`w-full min-w-[560px] border-collapse text-left text-[13px] ${className}`}>{children}</table>
    </div>
  )
}

export function Thead({ children, scheme = 'admin' }: { children: ReactNode; scheme?: Scheme }) {
  return <thead><tr className={`text-[11px] font-semibold uppercase tracking-wide ${SCHEMES[scheme].head}`}>{children}</tr></thead>
}

export function Th({ children, className = '', ...rest }: { children?: ReactNode; className?: string } & ThHTMLAttributes<HTMLTableCellElement>) {
  return (
    <th {...rest} className={`pb-3 pr-4 font-semibold ${className}`}>
      {children}
    </th>
  )
}

export function Tr({
  children,
  scheme = 'admin',
  interactive = true,
  className = '',
}: {
  children: ReactNode
  scheme?: Scheme
  interactive?: boolean
  className?: string
}) {
  const t = SCHEMES[scheme]
  return (
    <tr className={`border-t transition-colors duration-200 ${t.row} ${interactive ? t.rowHover : ''} ${className}`}>{children}</tr>
  )
}

export function Td({ children, className = '', ...rest }: { children?: ReactNode; className?: string } & TdHTMLAttributes<HTMLTableCellElement>) {
  return (
    <td {...rest} className={`py-3 pr-4 ${className}`}>
      {children}
    </td>
  )
}
