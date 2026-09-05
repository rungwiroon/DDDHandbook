# 04 — Commands และ HTTP API

HTTP เป็นขอบเขตการแปลง input จากผู้ใช้เป็น command ไม่ใช่ที่อยู่ของ business rule. API แปลง request เป็น Value Object และ command, Application โหลด `Order` แล้วเรียก behavior ของ aggregate; `Order` ยังเป็นผู้ตัดสินว่า operation นั้นทำได้หรือไม่.

## Commands ของ Ordering

| Command | Intent | Aggregate behavior |
|---|---|---|
| `CreateOrder` | เปิด draft order สำหรับโต๊ะ | `Order.Create` |
| `AddItemToOrder` | เพิ่มรายการ หรือรวม quantity ของรายการเดิม | `Order.AddItem` |
| `RemoveOrderItem` | เอารายการออกจาก draft order | `Order.RemoveItem` |
| `SendOrderToKitchen` | ยืนยันว่า order พร้อมส่งครัว | `Order.SendToKitchen` |

Application handler มีหน้าที่เพียง load aggregate, แปลง input ที่ผ่าน transport validation เป็น type ของ domain, เรียก behavior หนึ่งครั้ง และ commit. ห้าม handler ตั้ง `Status` หรือแก้ `Lines` เอง.

## HTTP contract

| HTTP | Command | Success |
|---|---|---|
| `POST /orders` | `CreateOrder` | `201 Created` พร้อม `Location: /orders/{orderId}` |
| `POST /orders/{orderId}/items` | `AddItemToOrder` | `204 No Content` |
| `DELETE /orders/{orderId}/items/{menuItemId}` | `RemoveOrderItem` | `204 No Content` |
| `POST /orders/{orderId}/send-to-kitchen` | `SendOrderToKitchen` | `204 No Content` |

ตัวอย่าง request เพื่อเพิ่มรายการ:

```http
POST /orders/5d71c979-e565-4a6e-a644-73d93d805591/items
Content-Type: application/json

{
  "menuItemId": "7555f1d7-d5c9-4b5f-ae59-c7297f77b8be",
  "itemName": "Pad Thai",
  "unitPrice": 85.00,
  "quantity": 2
}
```

`itemName` และ `unitPrice` เป็น snapshot ตอนรับ order. หากเพิ่ม `menuItemId` เดิมอีกครั้ง aggregate รวมเฉพาะ quantity และเก็บ name/price snapshot ของ line เดิมไว้.

`GET /orders/{orderId}` คืน `version` ของ order. Vue ส่งค่านี้เป็น `version` ใน request ที่แก้ order หรือส่งครัว (สำหรับ `DELETE` ใช้ query `?version={n}`) เพื่อป้องกันการเขียนทับจากหน้าจอที่อ่านข้อมูลเก่า; client เก่าที่ไม่มี `version` ยังใช้ flow เดิมได้ใน lab นี้.

## Business error contract

Request ที่ JSON ไม่ถูกต้อง, path/query parameter parse ไม่ได้ หรือ Value Object validation ไม่ผ่านเป็น `400 Bad Request`. Order ที่ไม่พบเป็น `404 Not Found`. Domain rule violation ต้องตอบ Problem Details ที่ machine-readable โดยไม่เปิด implementation detail:

```json
{
  "type": "https://restaurant.example/problems/order-not-draft",
  "title": "Order cannot be changed",
  "status": 409,
  "detail": "Only draft orders can be changed.",
  "code": "order.not-draft",
  "traceId": "00-..."
}
```

| Domain code | HTTP status | ความหมายต่อ UI |
|---|---|---|
| `order.not-draft` | `409 Conflict` | refresh order; ปิด action แก้ไขหรือส่งซ้ำ |
| `order.empty` | `409 Conflict` | แจ้งให้เพิ่มอย่างน้อยหนึ่งรายการ |
| `order.line-not-found` | `409 Conflict` | refresh order เพราะรายการอาจถูกลบไปแล้ว |
| `order.item-name-required` | `422 Unprocessable Content` | ชี้ validation ที่ชื่อรายการ |
| `order.concurrency` | `409 Conflict` | reload order แล้วให้ผู้ใช้ตัดสินใจจากข้อมูลล่าสุด |

Validation ใน Vue มีไว้เพื่อ feedback ที่เร็วขึ้นเท่านั้น. API ต้องคืน error contract เดียวกันเมื่อถูกเรียกตรง ๆ และ Application ต้องไม่แปลง domain error เป็น `500`.

## Checkpoint

ก่อนเพิ่ม endpoint ให้เขียน application test ที่ยืนยันว่า handler โหลด `Order`, เรียก behavior, และ commit เมื่อสำเร็จ. Integration test ต้องยืนยัน HTTP status กับ `code` ของ business error อย่างน้อย `order.empty` และ `order.not-draft`. Lab นี้ยังไม่สร้าง `KitchenTicket` หรือ publish event; นั่นเป็น responsibility ของบท 5.

## Acceptance criteria

```gherkin
Given an existing draft order
When the client posts a valid item to /orders/{orderId}/items
Then the API returns 204 and the order contains the item snapshot

Given an existing draft order with no lines
When the client posts to /orders/{orderId}/send-to-kitchen
Then the API returns 409 with code "order.empty"

Given an existing sent order
When the client posts an item to /orders/{orderId}/items
Then the API returns 409 with code "order.not-draft"

Given an unknown order id
When the client sends any order command
Then the API returns 404
```

## มุมมอง SA: สิทธิ์ของ actor และแหล่งข้อมูล

การระบุ actor ใน story ยังไม่ใช่การบังคับ authorization. ตารางนี้เป็น **ประเด็นสำหรับออกแบบสิทธิ์ในภาคต่อ** ไม่ใช่ permission ที่ lab รับประกันแล้ว:

| Action | Role ที่ควรหารือ | คำถามก่อนกำหนดสิทธิ์ |
|---|---|---|
| สร้าง/เพิ่ม/ลบ/ส่ง order | Waiter | ทุกคนแก้ draft เดียวกันได้ หรือจำกัดเจ้าของ/พื้นที่ |
| อ่าน kitchen board | Kitchen และ Waiter | ใครต้องเห็นข้อมูลใดเพื่อทำงาน |
| เปลี่ยนราคาเมนู | ผู้รับผิดชอบเมนู | ใครอนุมัติและราคาเริ่มมีผลเมื่อใด; ยังไม่มี API นี้ |
| เรียก `POST /outbox/process` ในบท 08 | ผู้ดูแลระบบ | ใครรับผิดชอบ recovery และบันทึกการดำเนินการอย่างไร |

เมื่อเพิ่ม authorization ต้องตกลงและทดสอบ contract ของผู้ไม่ผ่านการยืนยันตัวตน/ไม่มีสิทธิ์แยกจาก domain errors. การมีสิทธิ์ส่ง order ไม่ยกเว้น BR-01/BR-02 และไม่ควรทำให้ `Order` ผูกกับ HTTP identity.

### ตัวอย่างผลลัพธ์: snapshot และ data contract

ข้อตกลง MVP ของ BR-03 คือเมื่อสร้าง line ใหม่จะใช้ name/price จาก command; เมื่อเพิ่ม `menuItemId` ที่ยังมี line อยู่จะเพิ่มเฉพาะ quantity. เช่น line แรกเป็น Pad Thai ราคา 85 บาท จำนวน 1 ต่อมา request เพิ่มเมนูเดิมส่งราคา 90 บาท จำนวน 1 ผลยังเป็น line ราคา 85 บาท จำนวน 2 รวม 170 บาท. เหตุผลเชิง model คือคงข้อตกลงของ line เดิมเมื่อรวมจำนวน; SA ต้องยืนยันว่าร้านจริงต้องการ policy นี้ด้วย.

snapshot เริ่มตอนเพิ่ม line ใหม่ ไม่ใช่ตอนเปิด draft หรือส่งครัว. หากลบ line แล้วเพิ่มใหม่ จะใช้ snapshot จาก command ใหม่. ตัวอย่างนี้ยังไม่ใช่ flow เปลี่ยนราคาใน Menu Catalog.

**คำถามที่ SA ต้องตอบ:** ใครเป็นแหล่งข้อมูลที่เชื่อถือได้ของราคา? Lab รับ `itemName`/`unitPrice` จาก client และใช้เมนูตัวอย่างตามบท 06; Value Object ตรวจค่าถูกช่วงได้ แต่ไม่ได้พิสูจน์ว่าเป็นราคาที่ร้านอนุมัติ. ก่อนเชื่อม Menu Catalog จริง ต้องตกลงการตรวจหรือดึงราคาฝั่ง server รวมถึง policy เมื่อราคาเปลี่ยน โดยไม่แก้ snapshot เก่าย้อนหลังโดยปริยาย.
