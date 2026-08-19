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
    try {
      const problem = await response.json()
      detail = problem.detail ?? problem.code ?? detail
    } catch {
      // Keep the HTTP status message when the response is not JSON.
    }
    throw new Error(detail)
  }
  return response.status === 204 ? (undefined as T) : response.json()
}

export const restaurantApi = {
  createOrder: (tableNumber: number) =>
    request<{ orderId: string }>(
      '/orders',
      { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ tableNumber }) },
    ),
  addItem: (orderId: string, item: { menuItemId: string; itemName: string; unitPrice: number; quantity: number }) =>
    request<void>(`/orders/${orderId}/items`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(item),
    }),
  sendToKitchen: (orderId: string) => request<void>(`/orders/${orderId}/send-to-kitchen`, { method: 'POST' }),
  getOrder: (orderId: string) => request<OrderDetail>(`/orders/${orderId}`),
  getKitchenBoard: () => request<KitchenTicket[]>('/kitchen-board'),
}
