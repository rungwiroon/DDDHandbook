export type OrderLine = {
  menuItemId: string
  itemName: string
  unitPrice: number
  quantity: number
  subtotal: number
}

export type OrderDetail = {
  orderId: string
  tableNumber: number
  status: string
  lines: OrderLine[]
  total: number
  version: number
}

export class RestaurantApiError extends Error {
  constructor(message: string, readonly code?: string) {
    super(message)
    this.name = 'RestaurantApiError'
  }
}

export type KitchenTicket = {
  orderId: string
  tableNumber: number
  lines: { menuItemId: string; itemName: string; quantity: number }[]
}

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const response = await fetch(url, options)
  if (!response.ok) {
    let detail = `Request failed (${response.status})`
    let code: string | undefined
    try {
      const problem = await response.json()
      detail = problem.detail ?? problem.code ?? detail
      code = problem.code
    } catch {
      // Keep the HTTP status message when the response is not JSON.
    }
    throw new RestaurantApiError(detail, code)
  }
  return response.status === 204 ? (undefined as T) : response.json()
}

export const restaurantApi = {
  createOrder: (tableNumber: number) =>
    request<{ orderId: string }>(
      '/orders',
      { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ tableNumber }) },
    ),
  addItem: (orderId: string, item: { menuItemId: string; itemName: string; unitPrice: number; quantity: number }, version?: number) =>
    request<void>(`/orders/${orderId}/items`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ...item, version }),
    }),
  sendToKitchen: (orderId: string, version?: number) => request<void>(`/orders/${orderId}/send-to-kitchen`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ version }) }),
  getOrder: (orderId: string) => request<OrderDetail>(`/orders/${orderId}`),
  getKitchenBoard: () => request<KitchenTicket[]>('/kitchen-board'),
}
