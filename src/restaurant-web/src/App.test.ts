import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, describe, expect, test, vi } from 'vitest'
import App from './App.vue'
import { RestaurantApiError, restaurantApi } from './api'

afterEach(() => vi.restoreAllMocks())

describe('App workflow states', () => {
  test('shows an empty kitchen board before any ticket exists', () => {
    const wrapper = mount(App)

    expect(wrapper.text()).toContain('No kitchen tickets yet')
    expect(wrapper.text()).toContain('Select a table on the floor plan.')
  })

  test('shows a loading state while creating an order', async () => {
    let resolveCreate: (value: { orderId: string }) => void = () => undefined
    vi.spyOn(restaurantApi, 'createOrder').mockReturnValue(new Promise(resolve => { resolveCreate = resolve }))
    vi.spyOn(restaurantApi, 'getOrder').mockResolvedValue({ orderId: 'order-1', tableNumber: 1, status: 'Draft', lines: [], total: 0, version: 0 })
    const wrapper = mount(App)

    await wrapper.get('button.table-button').trigger('click')
    await wrapper.get('button.button-primary').trigger('click')

    expect(wrapper.text()).toContain('Creating…')
    expect(wrapper.get('button.button-primary').attributes('disabled')).toBeDefined()

    resolveCreate({ orderId: 'order-1' })
    await flushPromises()
  })

  test('renders a business error from the API', async () => {
    vi.spyOn(restaurantApi, 'createOrder').mockRejectedValue(new RestaurantApiError('Table is unavailable.', 'table.unavailable'))
    const wrapper = mount(App)

    await wrapper.get('button.table-button').trigger('click')
    await wrapper.get('button.button-primary').trigger('click')
    await flushPromises()

    expect(wrapper.get('[role="alert"]').text()).toContain('Table is unavailable.')
  })
})
