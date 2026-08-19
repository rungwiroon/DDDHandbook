import { afterEach, describe, expect, test, vi } from 'vitest'
import { restaurantApi } from './api'

afterEach(() => vi.restoreAllMocks())

describe('restaurantApi', () => {
  test('creates an order using the waiter contract', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ orderId: 'order-1' }), { status: 201 })))

    await expect(restaurantApi.createOrder(4)).resolves.toEqual({ orderId: 'order-1' })
    expect(fetch).toHaveBeenCalledWith('/orders', expect.objectContaining({ method: 'POST', body: '{"tableNumber":4}' }))
  })

  test('surfaces business error codes', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ detail: 'An empty order cannot be sent.' }), { status: 409 })))

    await expect(restaurantApi.sendToKitchen('order-1')).rejects.toThrow('An empty order cannot be sent.')
  })

  test('loads the kitchen board read model', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('[{"orderId":"order-1","tableNumber":4,"lines":[]}]', { status: 200 })))

    await expect(restaurantApi.getKitchenBoard()).resolves.toHaveLength(1)
  })
})
