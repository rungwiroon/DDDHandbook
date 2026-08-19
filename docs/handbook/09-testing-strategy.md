# 09 — Testing Strategy

ระบบนี้มี test อยู่แล้วในทุก layer; เป้าหมายของบทนี้ไม่ใช่เพิ่มจำนวน test แต่ให้แต่ละ test พิสูจน์ความเสี่ยงใน layer ที่เหมาะสม.

| Layer | พิสูจน์ | ตัวอย่าง |
|---|---|---|
| Domain | invariant และ state transition | order ว่างส่งครัวไม่ได้ |
| Application | orchestration | save ก่อน enqueue Outbox |
| Integration | HTTP, EF mapping, SQLite และ error contract | stale version ได้ `409` |
| Vue | API error และ UI state | business error แสดง detail |
| Visual smoke | layout และ flow ที่ผู้ใช้เห็น | เลือกโต๊ะ → ส่งครัว → ticket ปรากฏ |

Domain test ต้องเร็วและไม่ใช้ database. Integration test ใช้ SQLite in-memory เพื่อยืนยัน relational mapping โดยไม่ต้องเปิด service ภายนอก. Visual smoke test มีไว้ตรวจ hierarchy/layout; ไม่แทน assertion ของ business rule.

## Regression matrix

| Risk | Test owner |
|---|---|
| bypass draft rule | Domain |
| API แปลง domain error ผิด | Integration |
| event หายเมื่อ crash window | Integration / Outbox |
| event ซ้ำสร้าง ticket ซ้ำ | Integration / idempotency |
| update ทับกัน | Integration / concurrency |
| Vue ซ่อน error หรือ board ว่าง | Vue |

## Commands

```sh
dotnet test Restaurant.sln
cd src/restaurant-web && npm run build && npm test
```

## Checkpoint

เพิ่ม test เมื่อเกิด regression risk ใหม่ ไม่ใช่เมื่อ coverage percentage ลดลง. Bug ที่แก้แล้วต้องมี test อยู่ใน layer ต่ำสุดที่ reproduce behavior ได้.
