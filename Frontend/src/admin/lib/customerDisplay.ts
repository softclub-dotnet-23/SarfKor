// Same-machine, same-browser live sync between the cashier's POS tab and a second,
// customer-facing tab/window (the classic dual-monitor checkout counter setup) — the
// cart never touches the backend until checkout, so there is no server-side state to
// poll; BroadcastChannel is the simplest way to mirror it across tabs in real time.
const CHANNEL_NAME = 'sarfkor-customer-display'

export interface CustomerDisplayLine {
  key: string
  name: string
  unitPrice: number
  quantity: number
}

export interface CustomerDisplayState {
  storeId: number | null
  lines: CustomerDisplayLine[]
  total: number
  currency: string
  completedTotal?: { amount: number; currency: string }
}

export function publishCustomerDisplayState(state: CustomerDisplayState) {
  if (!('BroadcastChannel' in window)) return
  const channel = new BroadcastChannel(CHANNEL_NAME)
  channel.postMessage(state)
  channel.close()
}

export function subscribeToCustomerDisplay(onState: (state: CustomerDisplayState) => void): () => void {
  if (!('BroadcastChannel' in window)) return () => {}
  const channel = new BroadcastChannel(CHANNEL_NAME)
  channel.onmessage = (event: MessageEvent<CustomerDisplayState>) => onState(event.data)
  return () => channel.close()
}
