import { useState, type ReactNode } from 'react'
import { ChevronDownIcon, ClockIcon } from './icons'
import type { AuditLogEntry } from '../../lib/api'

function fmtDateTime(iso: string) {
  return new Date(iso).toLocaleString('ru-RU', { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function tryPretty(json?: string | null) {
  if (!json) return null
  try {
    return JSON.stringify(JSON.parse(json), null, 2)
  } catch {
    return json
  }
}

// One expandable row: who / what / which object / when / reason, with an optional before->after
// diff — shared by the Журнал page (all entries) and each store/user card's "История действий" tab
// (pre-filtered to that one entity), so the rendering never drifts between the two.
export function AuditLogRow({ entry, actionLabel, extra }: { entry: AuditLogEntry; actionLabel?: string; extra?: ReactNode }) {
  const [expanded, setExpanded] = useState(false)
  const before = tryPretty(entry.beforeStateJson)
  const after = tryPretty(entry.afterStateJson)
  const canExpand = !!(before || after || entry.reason)

  return (
    <div className="group rounded-xl border border-[color:var(--admin-border)]">
      {/* A real sibling button (`extra`), not an absolutely-positioned overlay on top of this row —
          the old overlay drifted onto the timestamp whenever a row's height differed from the
          pixel offset it was hard-coded against. Also can't nest `extra`'s <button> inside this
          row's own toggle, so the toggle is a div with onClick, not a <button>, here. */}
      <div
        role="button"
        tabIndex={canExpand ? 0 : -1}
        onClick={() => canExpand && setExpanded((v) => !v)}
        onKeyDown={(e) => canExpand && (e.key === 'Enter' || e.key === ' ') && setExpanded((v) => !v)}
        className={`flex w-full items-center gap-3 px-3.5 py-3 text-left ${canExpand ? 'cursor-pointer' : 'cursor-default'}`}
      >
        <span className="grid h-8 w-8 shrink-0 place-items-center rounded-lg bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]">
          <ClockIcon width={14} height={14} />
        </span>
        <div className="min-w-0 flex-1">
          <div className="truncate text-[12.5px] font-semibold text-[color:var(--admin-text)]">{actionLabel ?? entry.action}</div>
          <div className="truncate text-[11px] text-[color:var(--admin-text-secondary)]">
            {entry.entityType} #{entry.entityId}
            {entry.performedByEmail ? ` · ${entry.performedByEmail}` : ''}
            {entry.details ? ` · ${entry.details}` : ''}
          </div>
        </div>
        {extra}
        <span className="shrink-0 font-[JetBrains_Mono,monospace] text-[10.5px] text-[color:var(--admin-text-tertiary)]">
          {fmtDateTime(entry.occurredAt)}
        </span>
        {canExpand && (
          <ChevronDownIcon width={13} height={13} className={`shrink-0 text-[color:var(--admin-text-tertiary)] transition-transform ${expanded ? 'rotate-180' : ''}`} />
        )}
      </div>
      {expanded && canExpand && (
        <div className="border-t border-[color:var(--admin-border)] px-3.5 py-3">
          {entry.reason && (
            <p className="mb-2 text-[12px] text-[color:var(--admin-text)]">
              <span className="font-semibold text-[color:var(--admin-text-tertiary)]">Причина: </span>
              {entry.reason}
            </p>
          )}
          {entry.ipAddress && (
            <p className="mb-2 font-[JetBrains_Mono,monospace] text-[11px] text-[color:var(--admin-text-tertiary)]">IP: {entry.ipAddress}</p>
          )}
          {(before || after) && (
            <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
              {before && (
                <div>
                  <div className="mb-1 text-[10.5px] font-bold uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">До</div>
                  <pre className="overflow-x-auto rounded-lg bg-[color:var(--admin-hover)] p-2.5 font-[JetBrains_Mono,monospace] text-[10.5px] leading-relaxed text-[color:var(--admin-text-secondary)]">
                    {before}
                  </pre>
                </div>
              )}
              {after && (
                <div>
                  <div className="mb-1 text-[10.5px] font-bold uppercase tracking-wide text-[color:var(--admin-text-tertiary)]">После</div>
                  <pre className="overflow-x-auto rounded-lg bg-[color:var(--admin-hover)] p-2.5 font-[JetBrains_Mono,monospace] text-[10.5px] leading-relaxed text-[color:var(--admin-text-secondary)]">
                    {after}
                  </pre>
                </div>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  )
}
