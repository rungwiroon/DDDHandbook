<script setup lang="ts">
import { appTitle } from './app-title'
import { ref } from 'vue'
import { restaurantApi, type KitchenTicket, type OrderDetail } from './api'

const sampleMenu = [
  { menuItemId: '00000000-0000-0000-0000-000000000001', itemName: 'Pad Thai', unitPrice: 85, quantity: 1 },
  { menuItemId: '00000000-0000-0000-0000-000000000002', itemName: 'Iced Tea', unitPrice: 30, quantity: 1 },
]
const tableZones = [
  { name: 'Window', tables: [1, 2, 3] },
  { name: 'Center', tables: [4, 5, 6, 7] },
  { name: 'Patio', tables: [8, 9] },
]
const selectedTable = ref<number | null>(null)
const order = ref<OrderDetail | null>(null)
const board = ref<KitchenTicket[]>([])
const loading = ref(false)
const boardLoading = ref(false)
const error = ref('')
const boardError = ref('')

async function startOrder() {
  if (selectedTable.value === null) {
    error.value = 'Choose a table before creating an order.'
    return
  }
  loading.value = true
  error.value = ''
  try {
    const created = await restaurantApi.createOrder(selectedTable.value)
    order.value = await restaurantApi.getOrder(created.orderId)
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : 'Could not create order.'
  } finally {
    loading.value = false
  }
}

async function addSampleItem(item: (typeof sampleMenu)[number]) {
  if (!order.value) return
  loading.value = true
  error.value = ''
  try {
    await restaurantApi.addItem(order.value.orderId, item)
    order.value = await restaurantApi.getOrder(order.value.orderId)
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : 'Could not add item.'
  } finally {
    loading.value = false
  }
}

async function sendOrder() {
  if (!order.value) return
  loading.value = true
  error.value = ''
  try {
    await restaurantApi.sendToKitchen(order.value.orderId)
    order.value = await restaurantApi.getOrder(order.value.orderId)
    await refreshBoard()
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : 'Could not send order.'
  } finally {
    loading.value = false
  }
}

async function refreshBoard() {
  boardLoading.value = true
  boardError.value = ''
  try {
    board.value = await restaurantApi.getKitchenBoard()
  } catch (cause) {
    boardError.value = cause instanceof Error ? cause.message : 'Could not load kitchen board.'
  } finally {
    boardLoading.value = false
  }
}
</script>

<template>
  <main>
    <h1>{{ appTitle }}</h1>
    <section aria-labelledby="waiter-heading">
      <h2 id="waiter-heading">Waiter order</h2>
      <p>Select a table on the floor plan.</p>
      <div class="floor-plan" aria-label="Restaurant floor plan">
        <div v-for="zone in tableZones" :key="zone.name" class="floor-zone">
          <h3>{{ zone.name }}</h3>
          <button
            v-for="table in zone.tables"
            :key="table"
            type="button"
            class="table-button"
            :class="{ selected: selectedTable === table }"
            :aria-pressed="selectedTable === table"
            @click="selectedTable = table"
          >
            Table {{ table }}
          </button>
        </div>
      </div>
      <p v-if="selectedTable !== null">Selected table: {{ selectedTable }}</p>
      <button :disabled="loading || selectedTable === null" @click="startOrder">Create order</button>
      <p v-if="error" role="alert">{{ error }}</p>
      <template v-if="order">
        <p>Order {{ order.orderId }} · Table {{ order.tableNumber }} · {{ order.status }}</p>
        <div><button v-for="item in sampleMenu" :key="item.menuItemId" :disabled="loading" @click="addSampleItem(item)">Add {{ item.itemName }}</button></div>
        <p v-if="order.lines.length === 0">No items yet.</p>
        <ul v-else><li v-for="line in order.lines" :key="line.menuItemId">{{ line.itemName }} × {{ line.quantity }}</li></ul>
        <button :disabled="loading || order.lines.length === 0 || order.status !== 'Draft'" @click="sendOrder">Send to kitchen</button>
      </template>
    </section>
    <section aria-labelledby="kitchen-heading">
      <h2 id="kitchen-heading">Kitchen board</h2>
      <button :disabled="boardLoading" @click="refreshBoard">{{ boardLoading ? 'Loading…' : 'Refresh board' }}</button>
      <p v-if="boardError" role="alert">{{ boardError }}</p>
      <p v-else-if="!boardLoading && board.length === 0">No kitchen tickets yet.</p>
      <ul v-else><li v-for="ticket in board" :key="ticket.orderId">Table {{ ticket.tableNumber }}: {{ ticket.lines.map(line => `${line.itemName} × ${line.quantity}`).join(', ') }}</li></ul>
    </section>
  </main>
</template>

<style scoped>
.floor-plan {
  display: grid;
  gap: 0.75rem;
  grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr));
  max-width: 42rem;
  padding: 0.75rem;
  border: 1px solid #ccd5df;
  border-radius: 0.5rem;
  background: #f5f7fa;
}

.floor-zone {
  padding: 0.5rem;
  border: 1px dashed #aeb9c6;
  border-radius: 0.35rem;
}

.floor-zone h3 {
  margin: 0 0 0.5rem;
  font-size: 0.9rem;
}

.table-button {
  margin: 0.2rem;
  padding: 0.45rem 0.6rem;
}

.table-button.selected {
  color: #fff;
  background: #245c93;
  border-color: #16446f;
}
</style>
