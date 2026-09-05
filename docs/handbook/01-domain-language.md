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

## Rules ที่ยอมรับร่วมกัน

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

## มุมมอง SA: ค้นหากฎก่อนเขียน model

เริ่มจากให้คนร้านเล่าเหตุการณ์จริงหนึ่งครั้ง ตั้งแต่รับคำสั่งจนส่งงานให้ครัว แล้วถามต่อถึงครั้งที่ทำงานไม่สำเร็จ. บันทึกคำที่แต่ละ role ใช้ก่อนแปลงเป็นชื่อ command; อย่าเริ่มสัมภาษณ์ด้วยคำถามว่า “ต้องการ aggregate อะไร”.

| ผู้ให้ข้อมูลที่ควรคุยด้วย | คำถาม | สิ่งที่ต้องนำมายืนยันร่วมกัน |
|---|---|---|
| Waiter | เปิด order เมื่อไร โต๊ะเดียวมีหลาย order ได้ไหม ส่งผิดแล้วทำอย่างไร | trigger, ขอบเขต order และ exception flow |
| Kitchen | ถือว่าได้รับงานเมื่อใด อะไรทำให้เริ่มทำไม่ได้ | ข้อมูลใน ticket และความหมายของการรับงาน |
| ผู้จัดการร้าน / ผู้รับผิดชอบเมนู | ใครเปลี่ยนราคา ใครอนุมัติการแก้ผิด | เจ้าของ policy และข้อยกเว้น |

### ตัวอย่างผลลัพธ์: question และ decision log

แยกสิ่งที่ตกลงแล้วออกจากสมมติฐานที่ยังต้องตรวจสอบ. ในงานจริงแต่ละรายการควรมีแหล่งข้อมูล ผู้ยืนยัน วันที่ และเหตุผล; ตารางนี้ระบุสถานะตาม handbook เท่านั้น.

| ID | ประเด็น | สถานะและคำตอบ | ผู้ที่ควรยืนยันในงานจริง |
|---|---|---|---|
| BR-01 | ส่ง order ว่างได้ไหม | ข้อตกลง MVP: ไม่ได้ ต้องมีอย่างน้อยหนึ่ง line | Waiter และ Kitchen |
| BR-02 | แก้รายการหลังส่งได้ไหม | ข้อตกลง MVP: เพิ่ม/ลบไม่ได้ | Waiter และ Kitchen |
| BR-03 | เพิ่มเมนูเดิมซ้ำทำอย่างไร | ข้อตกลง MVP: รวม quantity และคง name/price ของ line เดิม; ยังไม่มี modifiers | ผู้รับผิดชอบเมนูและ Waiter |
| Q-01 | โต๊ะเดียวเปิดหลาย order พร้อมกันได้ไหม | รอยืนยัน: handbook ยังไม่กำหนดกฎห้าม อย่าอนุมานจากหมายเลขโต๊ะ | ผู้จัดการร้าน |
| Q-02 | สั่งผิดหลังส่งครัวแก้อย่างไร | ภาคต่อ: ไม่มี command แก้หรือยกเลิกหลังส่ง ต้องตกลงกระบวนการทำงานก่อนเพิ่มระบบ | Waiter, Kitchen และผู้จัดการร้าน |

### จากคำพูดกำกวมสู่ decision table

คำว่า “ส่งได้เมื่อพร้อม” ยังทดสอบไม่ได้. ถามว่า “พร้อมหมายถึงมีรายการหรือครัวยืนยันด้วย” แล้วใช้ข้อตกลง MVP แปลงเป็นตารางต่อไปนี้ โดยสมมติว่าพบ order และ request ผ่าน validation/concurrency แล้ว:

| สถานะ | มี line | ส่งครัวได้หรือไม่ | ผลที่คาดหวัง |
|---|---|---|---|
| Draft | ไม่มี | ไม่ได้ — BR-01 | `order.empty`; ไม่เปลี่ยนสถานะและไม่เกิด event |
| Draft | มี | ได้ | เปลี่ยนเป็น `SentToKitchen`; ตั้งแต่บท 05 มี event |
| SentToKitchen | มี | ไม่ได้ — BR-02 | `order.not-draft`; ไม่สร้าง event เพิ่ม |

นำแต่ละแถวไปเขียน Given/When/Then แล้วให้คนร้านตรวจด้วยตัวอย่าง order จริง. กฎ “เพิ่ม/ลบได้เฉพาะ Draft” เป็น invariant ของ `Order`; ส่วน “ใครมีสิทธิ์แก้” เป็นอีกคำถามหนึ่งตามบท 04.

### คำถามที่ SA ต้องตอบ

- flow ปัจจุบันติดขัดตรงไหน และผลลัพธ์ใดทำให้รู้ว่าการเปลี่ยนระบบช่วยได้
- กฎแต่ละข้อเป็น policy ของร้าน ข้อจำกัดของ lab หรือสมมติฐานที่ยังไม่มีผู้ยืนยัน
- เมื่อเกิดข้อยกเว้น ใครรับงานต่อ และกรณีใดต้องระบุ out of scope อย่างชัดเจน

**แบบฝึกหัด:** หยิบ Q-02 มาเขียน main flow และ exception flow พร้อมผู้รับผิดชอบที่ต้องไปยืนยัน โดยยังไม่เพิ่ม command หรือเปลี่ยน BR-02 เอง.
