# 02 — ERD ต่างจาก DDD อย่างไร

ERD ตอบว่า “ข้อมูลใดสัมพันธ์กัน” ส่วน domain model ตอบว่า “ใครอนุญาตให้เปลี่ยนอะไร ภายใต้กฎใด” ทั้งสองมีประโยชน์ แต่ใช้แทนกันไม่ได้.

| มุมมอง | ตัวอย่าง `orders` และ `order_lines` | คำถามที่ตอบ |
|---|---|---|
| ERD | `order_lines.order_id` เชื่อมกับ `orders.id` | เก็บข้อมูลอย่างไร |
| Domain model | `Order.SendToKitchen()` ปฏิเสธ order ว่าง | เปลี่ยนสถานะได้เมื่อไร |
| Read model | kitchen board แสดงเฉพาะงานที่ต้องทำ | UI ต้องอ่านอะไรเร็ว |

## เมื่อ primitive มีความหมายทางธุรกิจ ให้สร้าง Value Object

Developer มักเริ่มด้วย method หน้าตาแบบนี้:

```csharp
void CreateOrder(Guid id, int tableNumber, decimal price, int quantity)
```

โค้ดต่อไปนี้ compile ได้ เพราะ C# เห็นเพียง `Guid`, `int` และ `decimal`:

```csharp
CreateOrder(menuItemId, -3, -50m, 0);
```

แต่ธุรกิจเห็นปัญหาทันที: `menuItemId` ไม่ใช่ `orderId`, โต๊ะต้องมากกว่า 0, ราคาไม่ติดลบ และจำนวนต้องมากกว่า 0. Primitive ไม่บอกความหมายและไม่มีที่กลางสำหรับกฎเหล่านี้.

Lab ใช้ Vogen สร้าง `OrderId`, `MenuItemId`, `TableNumber`, `Quantity` และ `Money` เพื่อให้ validation และ type safety อยู่ที่ขอบเขตของข้อมูล:

```csharp
[ValueObject<int>]
public readonly partial struct TableNumber;
```

เมื่อใช้แล้ว code จะรับค่าเช่น `TableNumber table`, `Money price` และ `Quantity quantity` แทน primitive เปล่า ๆ. จึงสลับ `OrderId` กับ `MenuItemId` ไม่ได้ตั้งแต่ compile และสร้าง `TableNumber.From(0)` หรือ `Money.From(-10m)` ไม่ได้.

Value Object ไม่ใช่ข้ออ้างให้ห่อทุกชนิด: `retryCount` หรือ `pageSize` ยังเป็น `int` ปกติได้. สร้าง Value Object เมื่อ primitive มีภาษา กฎ หรือความเสี่ยงที่จะสลับกันใน domain.

## Common mistake

อย่าให้ endpoint รับ `OrderDto`, แก้ `Status = "Sent"`, แล้วบันทึก. วิธีนั้นไม่มีที่กลางที่ป้องกัน order ว่างหรือการแก้หลังส่งครัว. ใน lab domain ให้ test เรียก behavior ของ `Order` ตรง ๆ ก่อนมี EF หรือ HTTP.
