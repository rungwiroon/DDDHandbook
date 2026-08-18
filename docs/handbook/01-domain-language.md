# 01 — จาก requirement สู่ domain language

## Scenario

พนักงานเปิด order สำหรับโต๊ะหนึ่ง เพิ่มอาหารได้หลายรายการ แก้ไขได้ตราบใดที่ยังเป็น draft และส่งเข้าครัวได้เมื่อมีรายการอาหารอย่างน้อยหนึ่งรายการ. เมื่อส่งแล้ว waiter แก้รายการเดิมไม่ได้.

## Ubiquitous language

| คำ | ความหมาย | ไม่ใช่ |
|---|---|---|
| Order | ความตั้งใจสั่งอาหารของโต๊ะหนึ่งในช่วงเวลาเดียว | แถวข้อมูล order อย่างเดียว |
| Draft | order ที่ waiter แก้ได้ | order ที่ครัวเริ่มทำแล้ว |
| Order line | snapshot ของรายการอาหาร ราคา และจำนวน ณ ตอนสั่ง | การอ้าง object `MenuItem` ข้าม aggregate |
| Send to kitchen | การเปลี่ยนสถานะ business ที่มีผลต่อครัว | ปุ่ม UI หรือ update column ทั่วไป |
| Kitchen ticket | งานที่ครัวต้องทำซึ่งเกิดจาก order ที่ส่งแล้ว | child object ของ Order |

## Rules ที่ยอมรับร่วมกันใน M1

1. Table number และ quantity ต้องมากกว่า 0
2. ชื่อรายการต้องไม่ว่าง และราคาต่อหน่วยต้องไม่ติดลบ
3. เพิ่มหรือลบรายการได้เฉพาะ Draft order
4. เพิ่ม menu item เดิมจะรวม quantity ใน line เดิม เพราะ MVP ยังไม่มี modifiers
5. ลบ menu item ที่ไม่มีอยู่ไม่ได้
6. ส่งครัวได้เฉพาะ Draft order ที่มีอย่างน้อยหนึ่ง line

## Acceptance criteria

```gherkin
Given a draft order with no lines
When the waiter sends it to kitchen
Then the operation is rejected

Given a draft order with one menu item
When the waiter adds the same menu item again
Then the order has one line with the summed quantity

Given a sent order
When the waiter tries to add a line
Then the operation is rejected

Given a sent order
When the waiter tries to remove a line
Then the operation is rejected

Given a draft order with one menu item
When the waiter removes a menu item that is not in the order
Then the operation is rejected
```

Vue อาจแสดง validation เพื่อช่วยผู้ใช้ แต่ API และ Domain ต้องบังคับ rules เดียวกันเสมอ.
