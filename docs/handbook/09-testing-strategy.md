# 09 — Testing Strategy

ระบบนี้มี test อยู่แล้วในทุก layer; เป้าหมายของบทนี้ไม่ใช่เพิ่มจำนวน test แต่ให้แต่ละ test พิสูจน์ความเสี่ยงใน layer ที่เหมาะสม.

| Layer | พิสูจน์ | ตัวอย่าง |
|---|---|---|
| Domain | invariant และ state transition | order ว่างส่งครัวไม่ได้ |
| Application | orchestration | save ก่อน enqueue Outbox |
| Integration | HTTP, EF mapping, SQLite และ error contract | stale version ได้ `409` |
| Vue | API error และ UI state | loading, empty และ business error แสดง detail |
| Visual smoke | layout และ flow ที่ผู้ใช้เห็น | เลือกโต๊ะ → ส่งครัว → ticket ปรากฏ |

Domain test ต้องเร็วและไม่ใช้ database. Integration test ใช้ SQLite in-memory เพื่อยืนยัน relational mapping โดยไม่ต้องเปิด service ภายนอก. Visual smoke test มีไว้ตรวจ hierarchy/layout; ไม่แทน assertion ของ business rule.

## Regression matrix

| Risk | Test owner |
|---|---|
| bypass draft rule | Domain |
| API แปลง domain error ผิด | Integration |
| event หายเมื่อ crash window | Integration / Outbox |
| event ซ้ำสร้าง ticket ซ้ำ | Integration / idempotency |
| update ทับกัน | HTTP integration / concurrency |
| Vue ซ่อน error หรือ board ว่าง | Vue |

## Commands

```sh
dotnet test Restaurant.sln
cd src/restaurant-web && npm run build && npm test
```

## Visual smoke แบบทำซ้ำได้

รัน API และ Vue ตาม README แล้วเปิดหน้า Vue จากนั้นเลือกโต๊ะ, สร้าง Draft order, เพิ่ม `Pad Thai`, กดส่งครัว และตรวจว่า ticket ปรากฏบน kitchen board. เก็บภาพผลลัพธ์อ้างอิงไว้ที่ [frontend-smoke-test.png](../frontend-smoke-test.png). ขั้นตอนนี้เป็น manual check ของ layout และ flow; automated tests ยังคงเป็นหลักฐานของ business rule.

## Checkpoint

เพิ่ม test เมื่อเกิด regression risk ใหม่ ไม่ใช่เมื่อ coverage percentage ลดลง. Bug ที่แก้แล้วต้องมี test อยู่ใน layer ต่ำสุดที่ reproduce behavior ได้.

## มุมมอง SA: traceability จากกฎถึงหลักฐาน

ใช้ rule ID จากบท 01 เพื่อให้ติดตามได้ว่าข้อตกลงใดถูกพิสูจน์ตรงไหน. ตารางนี้ชี้ตำแหน่ง test ที่มีอยู่สำหรับตรวจสอบต่อ ไม่ใช่รายงานว่ารันผ่านแล้วหรือผ่าน UAT แล้ว:

| Business goal / Rule | Acceptance scenario | หลักฐานใน repository |
|---|---|---|
| ครัวได้รับงานที่มีรายการ — BR-01 | ส่ง draft ว่างถูกปฏิเสธและไม่เกิด ticket | `OrderTests.Send_to_kitchen_rejects_an_empty_order`; `OrderEndpointsTests.Sending_an_empty_order_creates_no_kitchen_ticket` |
| รายการที่ส่งแล้วไม่ถูกแก้เงียบ ๆ — BR-02 | เพิ่ม/ลบ/ส่งซ้ำหลังส่งถูกปฏิเสธ | `OrderTests.Sent_order_rejects_add_remove_and_send_operations` |
| เพิ่มเมนูเดิมแล้วจำนวนและราคาถูกต้อง — BR-03 | รวม quantity และคง name/price ของ line เดิม | `OrderTests.Add_item_with_the_same_menu_item_merges_its_quantity` |
| การส่งที่บันทึกแล้วกู้คืนได้ | processor ล้มแล้ว command ยังสำเร็จ; message pending รองรับ retry | `ReliabilityEndpointTests.Send_returns_success_when_immediate_outbox_processing_fails`; `OrderEndpointsTests.Failed_outbox_delivery_stays_pending_for_retry` |
| ครัวไม่มี ticket ซ้ำจาก event เดิม | ส่ง event เดิมเข้า consumer ซ้ำแล้วได้ ticket เดียว | `OrderEndpointsTests.Duplicate_outbox_delivery_is_idempotent_by_order_id` เป็นจุดเริ่มตรวจ; ปัจจุบันเรียก processor สองรอบ ยังไม่พิสูจน์ว่า consumer ได้รับ event เดิมซ้ำจริง |

ดูไฟล์ [OrderTests](../../tests/Restaurant.Domain.Tests/OrderTests.cs), [OrderEndpointsTests](../../tests/Restaurant.IntegrationTests/OrderEndpointsTests.cs) และ [ReliabilityEndpointTests](../../tests/Restaurant.IntegrationTests/ReliabilityEndpointTests.cs). หาก scenario หนึ่งต้องอาศัยหลาย tests ให้ระบุให้ครบ; อย่าถือว่า test ของแต่ละส่วนพิสูจน์ recovery flow ผ่าน UI ทั้งเส้นแล้ว.

## ตัวอย่างผลลัพธ์: business UAT

UAT ให้ตัวแทนผู้ใช้ยืนยันว่าผลลัพธ์ตอบงานร้านจริง ส่วน automated tests พิสูจน์ behavior ที่ระบุ. ตารางนี้เป็นแผนตรวจรับเมื่อทำถึงบท 08 ไม่ใช่ผลตรวจรับที่เกิดขึ้นแล้ว:

| Scenario และข้อมูลตั้งต้น | ผลที่ผู้ใช้ต้องตรวจ | ผู้ตรวจรับที่เสนอ |
|---|---|---|
| โต๊ะ 4, Draft, Pad Thai 85 บาท × 2 แล้วส่งครัว | Order ส่งแล้ว; board แสดงโต๊ะ 4 และ Pad Thai × 2 ใน ticket ของ order นั้น | ตัวแทน Waiter และ Kitchen |
| Draft ไม่มีรายการ | ส่งไม่ได้ มีข้อความเข้าใจได้ และไม่มีงานครัวเกิดขึ้น | ตัวแทน Waiter |
| สองหน้าจออ่าน version เดียวกัน แล้วคนแรกเพิ่มรายการก่อน | คนที่สองถูกแจ้ง conflict เมื่อส่ง version เก่า และเห็นข้อมูลล่าสุดก่อนตัดสินใจต่อ | ตัวแทน Waiter |
| จำลอง processor ล้มในสภาพแวดล้อมทดสอบ แล้วแก้สาเหตุและ process ใหม่ | การส่งไม่หาย; เมื่อ refresh พบ ticket ใบเดียวและรายการถูกต้อง | ตัวแทน Kitchen และผู้ดูแล โดยทีมทดสอบเตรียม failure |

บันทึก environment/build, OrderId, ขั้นตอน, expected/actual result, หลักฐาน และผู้ตรวจรับ/วันที่ของแต่ละกรณี. ถ้าทำ UAT ไม่ผ่าน ให้แยกว่า implementation ผิดจากกฎ หรือกฎยังไม่ตอบงานจริง แล้วเชื่อมกลับไปที่ story/decision ก่อนแก้.

**คำถามที่ SA ต้องตอบ:** ทุกกฎมี scenario ที่ตรวจได้หรือยัง, ใครมีอำนาจตรวจรับผลลัพธ์ และกรณีภาคต่อใดยังไม่ใช่เงื่อนไขผ่านของ MVP?
