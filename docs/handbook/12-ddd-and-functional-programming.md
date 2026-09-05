# 12 — DDD กับ Functional Programming

DDD ช่วยกำหนดภาษาธุรกิจ กฎ และเจ้าของการตัดสินใจ ส่วน Functional Programming (FP) ช่วยจัดรูปแบบการคำนวณและการเปลี่ยน state ให้ตรวจสอบได้. ทั้งสองใช้ร่วมกันได้: Order ยังคงมี identity และ invariant แม้ implementation จะใช้ immutable data กับ functions.

บทนี้เป็นบทเสริมหลังบท 08–10 ใช้กฎร้านอาหารเดิมเพื่อเปรียบเทียบแนวทางออกแบบ. ตัวอย่างและ lab เป็นแบบฝึกหัดแยกจาก reference implementation; ยังไม่ได้เปลี่ยน `Order`, HTTP contract หรือเพิ่ม FP library ให้โปรเจกต์.

## Scenario และกฎที่ต้องคงไว้

Waiter ส่ง draft order เข้าครัวได้เมื่อมีรายการอาหาร. ใช้ BR-01/BR-02 และ decision table จาก [บท 01](01-domain-language.md):

| Input state | ผลตัดสินใจ | Event |
|---|---|---|
| Draft ไม่มี line | ปฏิเสธ `order.empty` | ไม่มี |
| Draft มี line | ยอมรับ เปลี่ยนเป็น SentToKitchen | `OrderSentToKitchen` หนึ่งรายการ |
| SentToKitchen | ปฏิเสธ `order.not-draft` | ไม่มี |

ตารางนี้สมมติว่าพบ order และ input ผ่าน validation แล้ว. การโหลดข้อมูล, authorization หากเพิ่มในอนาคต และ stale-version conflict อยู่คนละขั้นกับการตัดสินใจตาม state ของ Order.

## จาก encapsulated mutation สู่ pure decision

ใน [Order ปัจจุบัน](../../src/Restaurant.Domain/Order.cs), `SendToKitchen()` ตรวจ invariant แล้วแก้ `Status` และ enqueue event ภายใน object. วิธีนี้รักษา business ownership ได้ แต่ยังไม่ใช่ pure function เพราะเปลี่ยน state ของ object ที่ caller ถืออยู่ แม้ไม่เรียก database.

แบบ functional ให้คิดเป็นการรับค่าแล้วคืนผลตัดสินใจ:

```text
SendToKitchen(OrderState)
    → Rejected(OrderError)
    | Accepted(NewOrderState, OrderSentToKitchen)
```

Pure function ให้ผลที่เทียบเท่ากันเมื่อรับ input เดียวกัน และไม่แก้ input หรือทำ observable side effect. หาก decision ต้องใช้เวลา/ID ที่สร้างใหม่ ให้ shell จัดหาค่านั้นแล้วส่งเป็น input; อย่าอ่านนาฬิกาหรือสุ่ม ID ภายใน pure core.

### ตัวอย่าง C#: state ใหม่และผลลัพธ์ที่ระบุชัด

ตัวอย่างนี้แสดงเฉพาะการตัดสินใจส่งครัว ใช้ Value Objects, `OrderStatus` และ event เดิมร่วมกับ BCL. เป็นโค้ดประกอบการออกแบบ ไม่ใช่ replacement ของ aggregate ทั้งตัว:

```csharp
using System.Collections.Immutable;
using Restaurant.Domain;

public sealed record LineState(
    MenuItemId MenuItemId, string ItemName,
    Money UnitPrice, Quantity Quantity);

public sealed record OrderState(
    OrderId Id, TableNumber TableNumber, OrderStatus Status,
    ImmutableArray<LineState> Lines);

public abstract record SendDecision
{
    private SendDecision() { }

    public sealed record Rejected(string Code) : SendDecision;
    public sealed record Accepted(
        OrderState State, OrderSentToKitchen Event) : SendDecision;
}

public static class OrderDecisions
{
    // Precondition: state มาจากการสร้าง/rehydrate ที่ตรวจข้อมูลแล้ว
    // Lines ต้องไม่เป็น default และสมาชิกทุกตัวต้อง valid
    public static SendDecision SendToKitchen(OrderState state)
    {
        if (state.Status != OrderStatus.Draft)
            return new SendDecision.Rejected("order.not-draft");

        if (state.Lines.IsEmpty)
            return new SendDecision.Rejected("order.empty");

        var sent = state with { Status = OrderStatus.SentToKitchen };
        var domainEvent = new OrderSentToKitchen(
            sent.Id, sent.TableNumber,
            [.. sent.Lines.Select(line => new OrderLineSnapshot(
                line.MenuItemId, line.ItemName, line.Quantity))]);

        return new SendDecision.Accepted(sent, domainEvent);
    }
}
```

Input ยังเป็น Draft เดิม; caller ได้ state ใหม่ที่มี OrderId เดิม. Event มีเฉพาะข้อมูลที่ Kitchen ต้องใช้ และไม่บรรทุกราคาหรือ Order ทั้งก้อนตาม [บท 05](05-domain-events-and-eventual-consistency.md). การสร้างค่า event ใน memory ยังไม่ใช่การส่ง event ให้ consumer.

`record` ไม่ได้ทำให้ object graph immutable โดยอัตโนมัติ: ถ้า field เป็น `List<T>` ที่แก้ได้ การ copy ด้วย `with` ยังแชร์ list เดิม. ตัวอย่างจึงใช้ immutable collection และสมาชิกที่ไม่เปิดให้แก้ค่าเดิม.

อย่างไรก็ตาม public constructors และ `with` ในตัวอย่างยังเปิดให้ caller สร้าง state ข้ามกฎได้. ถ้านำไปเป็น model จริง ต้องออกแบบการสร้าง/rehydrate และการเข้าถึง state ให้เปลี่ยนผ่าน domain operation ที่ยอมรับเท่านั้น; อย่านำ `OrderState` นี้ไปผูก request DTO แล้ว save ตรง ๆ. Aggregate boundary และ encapsulation ยังต้องรักษาเหมือน [บท 03](03-bounded-context-and-aggregate.md).

## Functional core และ imperative shell

แบ่งความรับผิดชอบของ use case ดังนี้:

| ขั้นตอน | เจ้าของ | หน้าที่ |
|---|---|---|
| อ่าน request และโหลด Order/version | API / Application / Infrastructure | รับ input และทำ I/O |
| ตัดสินใจส่งครัว | Domain core | คืน rejection หรือ state ใหม่พร้อม event |
| บันทึกผลที่ยอมรับ | Application / Infrastructure | save state กับ Outbox ใน transaction เดียว และตรวจ concurrency ตอนเขียน |
| ส่งต่อ event | Outbox processor | สร้าง KitchenTicket และรองรับ retry/idempotency |
| แปลงผลเป็น HTTP | API | คง status/code ตามบท 04 |

หาก domain ปฏิเสธ shell ต้องไม่บันทึก state ใหม่หรือ enqueue event. หาก domain ยอมรับ แต่ save พบ version conflict ต้อง rollback และตอบ `order.concurrency`; ผลคำนวณสำเร็จใน memory ยังไม่ใช่ command สำเร็จ.

FP ไม่ได้สร้าง transaction ให้เอง. การต่อ `Bind` หรือเรียง function เป็น pipeline บอกลำดับงาน แต่ไม่รับประกัน atomic commit. Outbox, concurrency token และ idempotent consumer จาก [บท 08](08-reliability-outbox-and-concurrency.md) ยังจำเป็น และ `204` ยังไม่ใช่หลักฐานว่าคนครัวรับงานแล้ว.

## Business errors เป็นข้อมูล

`SendDecision` แสดงแนวคิดเดียวกับ `Result<Success, Error>` หรือ `Either<Error, Success>`: caller เห็นจาก return type ว่ามีทางสำเร็จและทางปฏิเสธ. ตัวอย่างใช้ชนิดเฉพาะ use case เพื่อไม่ต้องเริ่มด้วย generic library; C# ไม่ได้รับประกันว่า caller จะจัดการทุกกรณีให้อัตโนมัติ จึงยังต้อง review และทดสอบการแปลงผล.

| ประเภท | ตัวอย่าง | แนวทาง |
|---|---|---|
| Input validation | ชื่อว่าง จำนวนไม่ถูกต้อง | อาจสะสม errors ของ field ที่ตรวจอย่างอิสระได้ |
| Domain rejection | Order ว่างหรือส่งแล้ว | คืนผลตาม decision table; ไม่ใช่เหตุให้ retry อัตโนมัติ |
| Infrastructure failure | database ใช้งานไม่ได้ | shell จัดการ failure/recovery แยกจากกฎธุรกิจ |

การสะสม validation errors กับการตัดสินใจแบบหยุดเมื่อผิดเงื่อนไขเป็นคนละเรื่อง. อย่าเปลี่ยนลำดับตรวจหรือ HTTP error เดิมโดยไม่ทบทวน contract. Domain error ควรสื่อธุรกิจ เช่น `order.empty`; ให้ API เป็นผู้ map ไป `409` ตามบท 04.

การคืน Result ไม่ทำให้ฟังก์ชัน pure โดยอัตโนมัติ หากภายในยังเขียน database หรือแก้ shared state. และการใช้ Result สำหรับ expected rejection ไม่ได้ทำให้ exception จาก runtime/I/O หายไป.

## State modeling ด้วย types

อีกแนวทางหนึ่งคือแยก `DraftOrder` กับ `SentOrder` แล้วให้ operation รับชนิดที่เหมาะสม เช่น `Send(DraftOrder)`. วิธีนี้ลดโอกาสเรียกส่งกับ SentOrder ในโค้ดที่ผ่าน type boundary แล้ว แต่ยังต้องตรวจ raw input และ state ที่โหลดจาก storage.

| กฎ | Type ช่วยได้อย่างไร | สิ่งที่ยังต้องตรวจ |
|---|---|---|
| จำนวนต้องมากกว่า 0 | ใช้ `Quantity` แทน int ทั่วไป | ค่าจาก request ตอนสร้าง Value Object |
| ส่งได้เฉพาะ Draft | รับ `DraftOrder` ใน operation | สถานะจริงเมื่อโหลด และ conflict ตอน save |
| ส่งได้เมื่อมี line | ใช้ non-empty collection หลัง validation | Draft ว่างยังเป็น state ที่ถูกต้อง จึงต้องตรวจตอนส่งหรือแปลงเป็นชนิดที่พร้อมส่ง |

ไม่จำเป็นต้องใช้ทุกเทคนิคพร้อมกัน. สำหรับ handbook นี้เริ่มจาก Value Objects กับ pure decision หนึ่ง use case แล้วประเมินว่าการแยก state types ลดความผิดพลาดคุ้มกับจำนวน types และ mapping ที่เพิ่มหรือไม่.

## Implementation lab และ acceptance criteria

ทำ lab ในพื้นที่ทดลองแยก โดยยังใช้ reference implementation เป็นฐานเปรียบเทียบ:

1. สร้าง snapshot adapter จาก Order ที่ valid ไปเป็น immutable input สำหรับ decision
2. ทดลอง `SendToKitchen` แบบ pure และใช้กรณีเดิมในตารางต้นบทตรวจผลทั้งสองรูปแบบ
3. ตรวจ state/event/error เชิงธุรกิจ ไม่บังคับให้รูปแบบ return value เหมือน method เดิม
4. เขียนแผนว่า shell จะ save state และ Outbox อย่างไร รวมถึง stale-version และ rollback; หากทดลองเชื่อม persistence ต้องเพิ่ม integration test ที่พิสูจน์ transaction จริง

```gherkin
Given a draft order with no lines
When the pure send decision is evaluated
Then it returns rejection with code "order.empty"
And the input is unchanged and no event is returned

Given a draft order with at least one line
When the pure send decision is evaluated
Then it returns a SentToKitchen state with the same OrderId
And it returns one OrderSentToKitchen event with matching kitchen snapshots
And the original input remains Draft with unchanged lines

Given the accepted SentToKitchen state from a previous decision
When that state is passed to the send decision again
Then it returns rejection with code "order.not-draft"
And no event is returned

Given the same valid immutable input
When the pure decision is evaluated twice
Then both results have equivalent business values
And neither evaluation performs persistence or delivery
```

การเรียกด้วย Draft เดิมสองครั้งย่อมได้ผล accepted ที่เทียบเท่ากันสองครั้ง; pure function ไม่ได้จำว่าเคยประมวลผลแล้ว. shell จึงต้องใช้ state/version ล่าสุดและ delivery safeguards เดิม. ใน tests ให้เทียบค่าของ state และสมาชิก event ทีละส่วน ไม่อาศัย reference equality หรือสมมติว่า record ที่ถือ immutable collection จะเทียบสมาชิกแบบ structural ให้เสมอ.

## มุมมอง SA: เงื่อนไข → ผลตัดสินใจ → ผลข้างเคียง

SA ใช้ decision table เดิมตรวจว่า functional model ยังตรงกับธุรกิจ โดยไม่ต้องเริ่มจากคำว่า Monad:

| สิ่งที่ SA ระบุ | สิ่งที่ทีมใช้ในแบบ functional |
|---|---|
| ข้อมูลก่อนทำรายการ | input state และ preconditions |
| เงื่อนไขที่ยอมรับ/ปฏิเสธ | domain decision และ business error |
| ข้อมูลที่ต้องเปลี่ยนเมื่อยอมรับ | new state ที่คง identity และ invariant |
| งานที่ boundary อื่นต้องได้รับ | event และ delivery/recovery contract |

**คำถามที่ SA ต้องตอบ:** เปลี่ยนเพียงรูปแบบ implementation หรือเปลี่ยนกฎด้วย? การปฏิเสธต่างจากการส่งต่อล้มเหลวอย่างไร? ผลใดเกิดใน memory ผลใด commit แล้ว และผู้ใช้ตรวจผลจริงได้ตรงไหน?

**ตัวอย่างผลลัพธ์การวิเคราะห์:** trace BR-01/BR-02 → decision rows → pure tests และ trace recovery contract → integration tests/UAT ตามบท 09. การทดสอบ pure core ผ่านยังไม่ใช่หลักฐานว่า end-to-end delivery ผ่าน.

## Common mistakes และ checkpoint

- ย้าย invariant ไปไว้ใน API pipeline แล้วเหลือ domain เป็น data bag
- ใช้ mutable collection ภายใน record แล้วเรียกว่า immutable model
- คิดว่า Result, effect type หรือ pipeline ทำให้เกิด transaction/exactly-once delivery
- ยอมให้ caller save state ที่สร้างข้าม domain decision
- เปลี่ยน HTTP errors หรือเพิ่ม library ทั้งระบบเพียงเพื่อทำตัวอย่าง FP

**Checkpoint:** อธิบายได้ว่าแบบเดิมกับแบบ functional รักษากฎเดียวกันอย่างไร, พิสูจน์ input ไม่ถูกแก้ และชี้จุด save/Outbox/concurrency ได้ครบ. บันทึกข้อดีและต้นทุนของแนวทางที่ทดลอง ก่อนเสนอเปลี่ยน reference implementation จริง.

## อ่านต่อใน FP-Concepts

บทนี้เน้นการประยุกต์ FP กับ domain ร้านอาหาร ส่วนรายละเอียดพื้นฐานและไลบรารีอ่านต่อในหนังสืออีกเล่ม:

- [บท 02 — แนวคิดพื้นฐาน FP](https://github.com/rungwiroon/FP-Concepts/blob/main/book/chapter-02.md): pure functions, immutability, composition และ functional core / imperative shell
- [บท 06 — Validation และ Error Handling](https://github.com/rungwiroon/FP-Concepts/blob/main/book/chapter-06.md): Option, Either, Validation และ smart constructors
- [บท 04 — Has Pattern และ Capabilities](https://github.com/rungwiroon/FP-Concepts/blob/main/book/chapter-04.md): อ่านเมื่อจะศึกษาการจัด dependency และ effects ด้วย language-ext

ชื่อ `Result`/`Either` ในบทนี้อธิบายแนวคิด ไม่ใช่ API ของ package ที่ติดตั้งแล้ว. การนำ language-ext หรือ Effect-TS เข้ามาเป็น decision แยก ซึ่งต้องทบทวน dependency constraints, เวอร์ชันและตัวอย่างที่ใช้จริง รวมถึงต้นทุนการเรียนรู้ของทีม.
