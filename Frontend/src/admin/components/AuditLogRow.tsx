import { useState } from 'react'
import { ChevronDownIcon, ClockIcon } from './icons'
import type { AuditLogEntry } from '../../lib/api'

function fmtDateTime(iso: string) {
  return new Date(iso).toLocaleString('ru-RU', { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function tryPretty(json?: string) {
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
export function AuditLogRow({ entry, actionLabel }: { entry: AuditLogEntry; actionLabel?: string }) {
  const [expanded, setExpanded] = useState(false)
  const before = tryPretty(entry.beforeStateJson)
  const after = tryPretty(entry.afterStateJson)
  const canExpand = !!(before || after || entry.reason)

  return (
    <div className="rounded-xl border border-[color:var(--mod-border)]">
      <button
        onClick={() => canExpand && setExpanded((v) => !v)}
        disabled={!canExpand}
        className="flex w-full items-center gap-3 px-3.5 py-3 text-left disabled:cursor-default"
      >
        <span className="grid h-8 w-8 shrink-0 place-items-center rounded-lg bg-[color:var(--mod-accent-dim)] text-[color:var(--mod-accent2)]">
          <ClockIcon width={14} height={14} />
        </span>
        <div className="min-w-0 flex-1">
          <div className="truncate text-[12.5px] font-semibold text-[color:var(--mod-text)]">{actionLabel ?? entry.action}</div>
          <div className="truncate text-[11px] text-[color:var(--mod-muted)]">
            {entry.entityType} #{entry.entityId}
            {entry.performedByEmail ? ` · ${entry.performedByEmail}` : ''}
            {entry.details ? ` · ${entry.details}` : ''}
          </div>
        </div>
        <span className="shrink-0 font-[JetBrains_Mono,monospace] text-[10.5px] text-[color:var(--mod-faint)]">
          {fmtDateTime(entry.occurredAt)}
        </span>
        {canExpand && (
          <ChevronDownIcon width={13} height={13} className={`shrink-0 text-[color:var(--mod-faint)] transition-transform ${expanded ? 'rotate-180' : ''}`} />
        )}
      </button>
      {expanded && canExpand && (
        <div className="border-t border-[color:var(--mod-border)] px-3.5 py-3">
          {entry.reason && (
            <p className="mb-2 text-[12px] text-[color:var(--mod-text)]">
              <span className="font-semibold text-[color:var(--mod-faint)]">Причина: </span>
              {entry.reason}
            </p>
          )}
          {entry.ipAddress && (
            <p className="mb-2 font-[JetBrains_Mono,monospace] text-[11px] text-[color:var(--mod-faint)]">IP: {entry.ipAddress}</p>
          )}
          {(before || after) && (
            <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
              {before && (
                <div>
                  <div className="mb-1 text-[10.5px] font-bold uppercase tracking-wide text-[color:var(--mod-faint)]">До</div>
                  <pre className="overflow-x-auto rounded-lg bg-[color:var(--mod-panel2)] p-2.5 font-[JetBrains_Mono,monospace] text-[10.5px] leading-relaxed text-[color:var(--mod-muted)]">
                    {before}
                  </pre>
                </div>
              )}
              {after && (
                <div>
                  <div className="mb-1 text-[10.5px] font-bold uppercase tracking-wide text-[color:var(--mod-faint)]">После</div>
                  <pre className="overflow-x-auto rounded-lg bg-[color:var(--mod-panel2)] p-2.5 font-[JetBrains_Mono,monospace] text-[10.5px] leading-relaxed text-[color:var(--mod-muted)]">
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
