# 11 — DDD กับ AI-assisted coding

AI ช่วยค้นโค้ด, แตกงาน, เขียน implementation และสร้าง test scaffold ได้เร็วขึ้น แต่ไม่ได้แทนความเข้าใจธุรกิจของทีม. DDD จึงยิ่งสำคัญขึ้น: ทีมต้องมี model ที่ชัดพอจะบอก agent ว่าอะไรถูก อะไรผิด และอะไรอยู่นอก scope.

แนวคิดนี้ต่อยอดจาก [Domain-Driven Design matters more when AI writes your code](https://threedots.tech/post/ddd-and-ai-coding/): domain model ไม่ใช่เอกสารที่ให้ agent สร้างแล้วจบ แต่เป็นความเข้าใจร่วมกันที่ต้องถูกทดสอบผ่านการออกแบบและ implementation อย่างต่อเนื่อง.

## AI ช่วยได้ แต่ไม่ใช่ domain expert

| งาน | เจ้าของการตัดสินใจ | AI ช่วยได้ |
|---|---|---|
| ระบุ actor, business outcome และ out-of-scope | SA และทีม | สรุปคำถามที่ยังไม่มีคำตอบ |
| ตั้งชื่อ `Order`, `Draft`, `Order line`, `Kitchen ticket` | คนที่เข้าใจ domain | ค้นหาชื่อที่ใช้ไม่สอดคล้องใน code และเอกสาร |
| กำหนด invariant ของ aggregate | ทีม | เขียนหรือขยาย domain test ตาม rule ที่ตกลงแล้ว |
| เลือก boundary ระหว่าง Ordering กับ Kitchen | ทีม | trace จุดเชื่อมต่อและตรวจการอ้างอิงข้าม boundary |
| เขียน API, Vue และ persistence | ทีม review ผลลัพธ์ | implement ตาม contract และ acceptance criteria |

อย่ามอบโจทย์ว่า “ทำระบบส่ง order เข้าครัว” แล้วรับ code ที่ agent สร้างมาเป็น model. ถ้ายังตอบไม่ได้ว่า order ที่ส่งแล้วแก้รายการได้หรือไม่ หรือ `KitchenTicket` เป็น child ของ `Order` หรือไม่ ให้กลับไปคุย scenario และ boundary ก่อนเริ่ม implementation.

## เริ่มจาก story ที่ ready

ก่อนให้ agent ลงมือ ใช้ Definition of Ready จากบท 10 และแนบ context ที่จำเป็นให้ครบ. ตัวอย่างคำสั่งที่มีขอบเขตชัด:

```text
Implement SendOrderToKitchen for the Ordering boundary.

- Preserve the Order aggregate rule: only a Draft order with at least one line can be sent.
- Publish OrderSentToKitchen; KitchenTicket is created across the boundary through the existing Outbox flow.
- Preserve the HTTP business-error contract in chapter 04.
- Add focused automated tests for the acceptance criteria.
- Do not add billing, a background worker, or new business rules.
```

คำสั่งนี้ไม่ได้แทนการออกแบบ; มันอ้างอิง decision ที่ทีมตกลงแล้ว. ถ้า agent ต้องเดา aggregate owner, error contract หรือ out-of-scope แปลว่า story ยังไม่ ready.

## Guardrails ที่ต้องรักษา

ให้ agent อ่านบท 01, 03, 04 และ 10 ก่อนแก้ implementation ที่เกี่ยวกับธุรกิจ แล้วตรวจผลลัพธ์ตามกติกาเหล่านี้:

- business rule อยู่ใน `Order` aggregate ไม่ใช่ใน API หรือ Vue
- `Order` และ `KitchenTicket` เป็นคนละ boundary แม้ persistence จะมี foreign key ได้
- event `OrderSentToKitchen` คือ business fact ไม่ใช่คำสั่งให้ aggregate อื่นทำงาน
- error code และ acceptance criteria ที่มีอยู่ต้องไม่เปลี่ยนความหมายโดยไม่อัปเดต handbook
- change ทุกชุดต้องมี test ที่พิสูจน์ behavior หรือระบุชัดว่าทำไม test นั้นไม่เหมาะ

Guardrail ที่เขียนใกล้ code และอ้างอิงชื่อใน ubiquitous language ช่วยให้ทั้งคนและ agent ไม่ตีความ concept เดียวกันคนละแบบ.

## Review ให้ตรวจ model ก่อน syntax

ก่อน merge ให้ถามตามลำดับนี้:

1. behavior นี้ตอบ business outcome ของ story จริงหรือไม่
2. ชื่อใน code, API และ UI ใช้ ubiquitous language เดียวกันหรือไม่
3. invariant ถูกบังคับโดย aggregate owner หรือหลุดไปอยู่ใน transport/UI
4. side effect ข้าม boundary ผ่าน event และ reliability contract เดิมหรือไม่
5. change เกิน scope ที่ตกลงไว้หรือไม่

Code review จึงเป็นการยืนยันว่า implementation ตรงกับ design ไม่ใช่จุดเริ่มต้นของการถกว่าควรออกแบบอย่างไร. AI ทำให้การเขียนเร็วขึ้น แต่ไม่ได้ทำให้ทีมข้ามขั้นคิดและรับผิดชอบต่อ model ได้.

## Checkpoint

เลือก story “ส่ง Order เข้าครัว” แล้วเขียน task brief สำหรับ agent หนึ่งชุด. ให้คนในทีมตรวจโดยไม่เปิด code ว่าระบุ aggregate rule, boundary, event, error contract, acceptance criteria และ out-of-scope ครบหรือไม่. ถ้าต้องพึ่ง agent เดาองค์ประกอบสำคัญเหล่านี้ ให้กลับไปปรับ story ก่อน.

## มุมมอง SA: ส่งต่อ decision และความไม่แน่ใจให้ AI

แนบ rule IDs และสถานะการตัดสินใจจากบท 01 พร้อม traceability จากบท 09. ชื่อ test เป็นจุดให้ตรวจสอบ ไม่ใช่หลักฐานว่ารันผ่าน; ต้องให้ agent รายงานสิ่งที่ตรวจจริงและส่วนที่ยังไม่ได้ตรวจ.

ตัวอย่าง context เพิ่มเติมสำหรับ story เดิม:

```text
Confirmed MVP rules: BR-01 and BR-02 in chapter 01.
Command success: Order and Outbox message are committed (chapter 08).
Delivery outcome: one KitchenTicket is visible after successful processing.
Recovery: processor failure leaves a pending message for operator-triggered retry.
Traceability: map each acceptance scenario to implementation/test evidence.
Open questions outside this task: delivery SLA and production permission policy.
Do not turn open questions into requirements or claim UAT has passed.
If a proposed change affects these rules, report the impact before implementing it.
```

**คำถามที่ SA ต้องตอบ:** brief แยกข้อตกลงกับสมมติฐานแล้วหรือยัง และผลที่ AI เสนออ้างอิง decision ใด? หาก AI เสนอ modifiers หรือ workflow ยกเลิก ให้ใช้ change-impact table ในบท 10 และนำคำถามกลับไปยืนยันกับคนร้านก่อนรวมเป็น scope ใหม่.
