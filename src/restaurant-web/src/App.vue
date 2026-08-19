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
    <header class="topbar">
      <div>
        <p class="eyebrow">SERVICE CONTROL / FLOOR 01</p>
        <h1>{{ appTitle }}</h1>
      </div>
      <div class="live-indicator"><span aria-hidden="true"></span> Live operations</div>
    </header>
    <div class="workspace">
      <section class="panel order-panel" aria-labelledby="waiter-heading" :aria-busy="loading">
        <div class="panel-heading">
          <div>
            <p class="eyebrow accent">WAITER DESK</p>
            <h2 id="waiter-heading">Waiter order</h2>
          </div>
          <span class="step-badge">01 / 03</span>
        </div>
        <p class="instruction">Select a table on the floor plan.</p>
        <div class="floor-plan" aria-label="Restaurant floor plan">
          <div v-for="zone in tableZones" :key="zone.name" class="floor-zone">
            <div class="zone-heading"><h3>{{ zone.name }}</h3><span>{{ zone.tables.length }} tables</span></div>
            <div class="table-grid">
          <button
            v-for="table in zone.tables"
            :key="table"
            type="button"
            class="table-button"
            :class="{ selected: selectedTable === table }"
            :aria-pressed="selectedTable === table"
            @click="selectedTable = table"
          >
            <span class="table-number">{{ table }}</span>
            <span class="table-label">TABLE</span>
          </button>
            </div>
          </div>
        </div>
        <div class="selection-row">
          <p v-if="selectedTable !== null" class="selection-state"><span class="selection-dot"></span> Selected table <strong>{{ selectedTable }}</strong></p>
          <p v-else class="selection-state muted">No table selected</p>
          <button class="button button-primary" :disabled="loading || selectedTable === null" @click="startOrder">{{ loading ? 'Creating…' : 'Create order' }}</button>
        </div>
        <p v-if="error" class="error-message" role="alert"><strong>Action needed</strong>{{ error }}</p>
        <template v-if="order">
          <div class="order-summary">
            <div><span class="eyebrow">ACTIVE ORDER</span><strong>{{ order.orderId.slice(0, 8) }}</strong></div>
            <span class="status-pill" :class="{ sent: order.status !== 'Draft' }">{{ order.status }}</span>
          </div>
          <div class="menu-actions"><button v-for="item in sampleMenu" :key="item.menuItemId" class="button button-secondary" :disabled="loading" @click="addSampleItem(item)">+ {{ item.itemName }} <span>฿{{ item.unitPrice }}</span></button></div>
          <p v-if="order.lines.length === 0" class="empty-inline">No items yet. Add a sample menu item to begin.</p>
          <ul v-else class="order-lines"><li v-for="line in order.lines" :key="line.menuItemId"><span>{{ line.itemName }} <small>× {{ line.quantity }}</small></span><strong>฿{{ line.subtotal }}</strong></li></ul>
          <div class="order-footer"><span>Total <strong>฿{{ order.total }}</strong></span><button class="button button-accent" :disabled="loading || order.lines.length === 0 || order.status !== 'Draft'" @click="sendOrder">{{ loading ? 'Sending…' : 'Send to kitchen' }} <span aria-hidden="true">→</span></button></div>
        </template>
      </section>
      <section class="panel kitchen-panel" aria-labelledby="kitchen-heading" :aria-busy="boardLoading">
        <div class="panel-heading">
          <div><p class="eyebrow teal">KITCHEN FLOW</p><h2 id="kitchen-heading">Kitchen board</h2></div>
          <button class="refresh-button" :disabled="boardLoading" @click="refreshBoard"><span :class="{ spinning: boardLoading }" aria-hidden="true">↻</span> {{ boardLoading ? 'Loading…' : 'Refresh' }}</button>
        </div>
        <p class="instruction">Tickets sent from the floor arrive here.</p>
        <p v-if="boardError" class="error-message" role="alert"><strong>Board offline</strong>{{ boardError }}</p>
        <div v-else-if="!boardLoading && board.length === 0" class="empty-state"><span class="empty-icon" aria-hidden="true">✦</span><strong>No kitchen tickets yet</strong><span>Send an order to see it appear here.</span></div>
        <ul v-else class="ticket-list"><li v-for="ticket in board" :key="ticket.orderId" class="ticket"><div class="ticket-top"><span class="table-chip">T{{ ticket.tableNumber }}</span><span class="ticket-time">NEW TICKET</span></div><strong>Table {{ ticket.tableNumber }}</strong><ul><li v-for="line in ticket.lines" :key="line.menuItemId"><span>{{ line.itemName }}</span><strong>× {{ line.quantity }}</strong></li></ul></li></ul>
      </section>
    </div>
  </main>
</template>

<style scoped>
:global(*) { box-sizing: border-box; }
:global(body) { margin: 0; min-width: 320px; color: #dce4f3; background: #101629; font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; }
:global(button) { font: inherit; }
main { max-width: 1440px; margin: 0 auto; padding: 2rem clamp(1rem, 3vw, 3.5rem) 3.5rem; }
.topbar { display: flex; align-items: flex-end; justify-content: space-between; gap: 1rem; margin-bottom: 2rem; }
h1, h2, h3, p { margin-top: 0; }
h1 { margin-bottom: 0; color: #f6f8ff; font-size: clamp(1.65rem, 3vw, 2.35rem); letter-spacing: -0.04em; }
h2 { margin-bottom: 0; color: #f6f8ff; font-size: 1.35rem; letter-spacing: -0.025em; }
.eyebrow { margin-bottom: 0.35rem; color: #7987a6; font-size: 0.66rem; font-weight: 800; letter-spacing: 0.16em; }
.eyebrow.accent { color: #f0b64c; }
.eyebrow.teal { color: #5dd6bc; }
.live-indicator { display: flex; align-items: center; gap: 0.5rem; padding: 0.5rem 0.75rem; border: 1px solid #263452; border-radius: 999px; color: #9eacc6; font-size: 0.74rem; font-weight: 700; }
.live-indicator span { width: 0.45rem; height: 0.45rem; border-radius: 50%; background: #5dd6bc; box-shadow: 0 0 0 4px #5dd6bc22; }
.workspace { display: grid; grid-template-columns: minmax(0, 1.35fr) minmax(20rem, 0.85fr); gap: 1.25rem; align-items: start; }
.panel { min-width: 0; padding: clamp(1rem, 2.3vw, 1.75rem); border: 1px solid #263452; border-radius: 1rem; background: #171f35; box-shadow: 0 1.5rem 3rem #080c18aa; }
.panel-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; margin-bottom: 0.5rem; }
.step-badge { padding: 0.35rem 0.55rem; border-radius: 0.4rem; color: #aab6cd; background: #202b47; font-size: 0.7rem; font-weight: 800; }
.instruction { margin-bottom: 1.25rem; color: #8b99b4; font-size: 0.9rem; }
.floor-plan { display: grid; grid-template-columns: repeat(3, 1fr); gap: 0.75rem; padding: 0.75rem; border: 1px solid #2c3b5b; border-radius: 0.75rem; background: #111a2f; }
.floor-zone { min-height: 9rem; padding: 0.75rem; border: 1px dashed #344566; border-radius: 0.6rem; background: #18223b; }
.zone-heading { display: flex; align-items: baseline; justify-content: space-between; gap: 0.5rem; margin-bottom: 0.9rem; }
.zone-heading h3 { margin: 0; color: #cbd5e8; font-size: 0.78rem; }
.zone-heading span { color: #71809e; font-size: 0.62rem; }
.table-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 0.55rem; }
.table-button { display: flex; min-height: 3.4rem; flex-direction: column; align-items: center; justify-content: center; border: 1px solid #3a4d71; border-radius: 0.55rem; color: #b8c5dc; background: #202e4c; cursor: pointer; transition: border-color 160ms ease, background 160ms ease, transform 160ms ease; }
.table-button:hover { border-color: #f0b64c; transform: translateY(-2px); }
.table-button.selected { border-color: #f0b64c; color: #171b2d; background: #f0b64c; box-shadow: 0 0.5rem 1.2rem #f0b64c33; }
.table-number { font-size: 1.2rem; font-weight: 850; line-height: 1; }
.table-label { margin-top: 0.25rem; font-size: 0.56rem; font-weight: 800; letter-spacing: 0.13em; opacity: 0.72; }
.selection-row { display: flex; align-items: center; justify-content: space-between; gap: 1rem; margin-top: 1rem; }
.selection-state { display: flex; align-items: center; gap: 0.45rem; margin: 0; color: #d6e0f1; font-size: 0.84rem; }
.selection-state.muted { color: #7786a4; }
.selection-dot { width: 0.45rem; height: 0.45rem; border-radius: 50%; background: #f0b64c; }
.button { border: 1px solid transparent; border-radius: 0.55rem; padding: 0.65rem 0.85rem; font-size: 0.8rem; font-weight: 800; cursor: pointer; transition: background 160ms ease, border-color 160ms ease, transform 160ms ease; }
.button:hover:not(:disabled) { transform: translateY(-1px); }
.button:disabled, .refresh-button:disabled { cursor: not-allowed; opacity: 0.45; }
.button-primary { color: #172039; background: #f0b64c; }
.button-secondary { border-color: #354766; color: #d4def0; background: #202c48; }
.button-secondary span { margin-left: 0.4rem; color: #f0b64c; }
.button-accent { color: #142039; background: #f0b64c; }
.error-message { display: flex; flex-direction: column; gap: 0.15rem; margin: 1rem 0 0; padding: 0.75rem; border: 1px solid #a94e5c; border-radius: 0.55rem; color: #ffc7c9; background: #632d3e44; font-size: 0.8rem; }
.error-message strong { color: #ff9d9f; font-size: 0.68rem; letter-spacing: 0.08em; text-transform: uppercase; }
.order-summary { display: flex; align-items: center; justify-content: space-between; margin-top: 1.4rem; padding-top: 1rem; border-top: 1px solid #2a3855; }
.order-summary strong { display: block; color: #ecf1fc; font-size: 1rem; }
.status-pill { padding: 0.35rem 0.6rem; border-radius: 999px; color: #f0b64c; background: #f0b64c1f; font-size: 0.65rem; font-weight: 850; letter-spacing: 0.08em; text-transform: uppercase; }
.status-pill.sent { color: #5dd6bc; background: #5dd6bc1c; }
.menu-actions { display: flex; flex-wrap: wrap; gap: 0.6rem; margin: 1rem 0; }
.empty-inline { padding: 1rem; border: 1px dashed #344566; border-radius: 0.55rem; color: #8190ad; font-size: 0.83rem; }
.order-lines, .ticket-list { margin: 0; padding: 0; list-style: none; }
.order-lines li { display: flex; justify-content: space-between; gap: 1rem; padding: 0.7rem 0; border-bottom: 1px solid #283754; color: #d3dded; font-size: 0.85rem; }
.order-lines small { color: #7f8da8; }
.order-lines strong { color: #f0b64c; }
.order-footer { display: flex; align-items: center; justify-content: space-between; gap: 1rem; margin-top: 1rem; color: #8b99b4; font-size: 0.82rem; }
.order-footer strong { margin-left: 0.3rem; color: #f2f5fc; font-size: 1rem; }
.refresh-button { border: 1px solid #354766; border-radius: 0.5rem; padding: 0.5rem 0.65rem; color: #afbdd5; background: #202c48; font-size: 0.74rem; font-weight: 750; cursor: pointer; }
.refresh-button span { display: inline-block; margin-right: 0.2rem; color: #5dd6bc; font-size: 1rem; }
.spinning { animation: spin 900ms linear infinite; }
.empty-state { display: flex; min-height: 15rem; flex-direction: column; align-items: center; justify-content: center; gap: 0.45rem; border: 1px dashed #344566; border-radius: 0.7rem; color: #8190ad; font-size: 0.8rem; text-align: center; }
.empty-state strong { color: #c6d1e5; font-size: 0.95rem; }
.empty-icon { display: grid; width: 2.5rem; height: 2.5rem; place-items: center; margin-bottom: 0.35rem; border-radius: 50%; color: #5dd6bc; background: #5dd6bc18; font-size: 1.25rem; }
.ticket-list { display: grid; gap: 0.75rem; }
.ticket { padding: 1rem; border: 1px solid #2e4463; border-left: 3px solid #5dd6bc; border-radius: 0.65rem; background: #1c2944; }
.ticket-top { display: flex; align-items: center; justify-content: space-between; margin-bottom: 0.7rem; }
.table-chip { padding: 0.28rem 0.45rem; border-radius: 0.35rem; color: #142039; background: #5dd6bc; font-size: 0.7rem; font-weight: 900; }
.ticket-time { color: #71809e; font-size: 0.6rem; font-weight: 800; letter-spacing: 0.11em; }
.ticket > strong { color: #eef3fc; font-size: 0.88rem; }
.ticket ul { margin: 0.75rem 0 0; padding: 0.7rem 0 0; border-top: 1px solid #304260; list-style: none; }
.ticket li { display: flex; justify-content: space-between; padding: 0.25rem 0; color: #abb9d0; font-size: 0.8rem; }
.ticket li strong { color: #5dd6bc; }
:focus-visible { outline: 3px solid #f0b64c; outline-offset: 3px; }
@keyframes spin { to { transform: rotate(360deg); } }
@media (max-width: 800px) { .workspace { grid-template-columns: 1fr; } }
@media (max-width: 560px) { main { padding: 1.25rem 0.8rem 2.5rem; } .topbar { align-items: flex-start; flex-direction: column; margin-bottom: 1.25rem; } .floor-plan { grid-template-columns: 1fr; } .floor-zone { min-height: auto; } .selection-row, .order-footer { align-items: stretch; flex-direction: column; } .button-primary, .button-accent { width: 100%; } }
@media (prefers-reduced-motion: reduce) { *, *::before, *::after { scroll-behavior: auto !important; animation-duration: 0.01ms !important; animation-iteration-count: 1 !important; transition-duration: 0.01ms !important; } }
</style>
