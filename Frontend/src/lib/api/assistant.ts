import { apiFetch } from './client'

export interface AssistantChatMessage {
  role: 'user' | 'assistant'
  content: string
}

export interface ProposedAction {
  pendingActionId: number
  actionType: string
  summary: string
  expiresAt: string
}

export interface AssistantChatResult {
  outcome: 'Answered' | 'StoreNotFound' | 'Forbidden'
  replyText?: string
  proposedAction?: ProposedAction
}

// storeId/UserId-adjacent identity is re-verified server-side against real store
// ownership/employment (see AskAssistantCommandHandler) -- this is just what tells the backend
// which store's own data the caller is currently working in, same as every other partner endpoint.
export function chat(storeId: number | null, history: AssistantChatMessage[], message: string) {
  return apiFetch<AssistantChatResult>('/api/assistant/chat', {
    method: 'POST',
    body: { storeId, history, message },
  })
}

export interface ConfirmActionResult {
  outcome: 'Confirmed' | 'AlreadyConfirmed' | 'NotFound' | 'Forbidden' | 'Expired' | 'FeatureDisabled' | 'ExecutionFailed'
  summary?: string
}

// Separate, explicitly user-triggered request -- never fired automatically just because the
// assistant proposed something in chat (see CLAUDE.md Mode C requirement).
export function confirmAction(pendingActionId: number) {
  return apiFetch<ConfirmActionResult>(`/api/assistant/actions/${pendingActionId}/confirm`, {
    method: 'POST',
  })
}
