# CLAUDE.md

Behavioral guidelines to reduce common LLM coding mistakes. Merge with project-specific instructions as needed.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

## 5. Time Handling

**Luôn dùng giờ server. Luôn parse/format bằng InvariantCulture.**

- Không dùng `DateTime.Now` / `DateTime.UtcNow` / `DateTime.Today` trong game code. Nguồn giờ duy nhất là `TimeManager` (nó tự fallback về clock device khi chưa sync được).
  - Lưu / so sánh mốc hết hạn → `GameManager.Get<TimeManager>().UtcNow`, lưu xuống save dạng **UTC**.
  - Hiển thị cho user, logic theo ngày lịch (reset 0h, daily) → `GameManager.Get<TimeManager>().Now`.
- Mọi chỗ parse hoặc format `DateTime`/`TimeSpan` **phải** truyền `CultureInfo.InvariantCulture`. Không dùng culture của máy, không hardcode `new CultureInfo("en-US")`.
  - Parse: `DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var t)`.
  - Format khi lưu: `t.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)`.
  - Chuỗi nội suy có thời gian: `FormattableString.Invariant($"...{t:O}")`.
- Chi tiết cơ chế sync server time: xem [GUIDE/TimeService.md](GUIDE/TimeService.md).

## 6. Tween & Animation

**Dùng LitMotion, không dùng DOTween. Motion nào cũng phải ngắt được.**

- Làm transition/tween → luôn chọn **LitMotion** (`LMotion.Create(...)`). Không thêm DOTween vào project; nếu bắt gặp code DOTween thì báo, đừng tự port.
- Mọi motion phải có đường ngắt, không để nó chạy tiếp khi state đã đổi hoặc object đã destroy:
  - Giữ `MotionHandle`, huỷ motion cũ trước khi chạy motion mới:
    ```csharp
    private MotionHandle handle;

    private void CancelHandle()
    {
        if (handle.IsPlaying()) handle.Cancel();
    }

    public void Play()
    {
        CancelHandle();
        handle = LMotion.Create(from, to, duration).Bind(Apply).AddTo(gameObject);
    }
    ```
  - `.AddTo(gameObject)` / `.AddTo(transform)` để motion tự huỷ khi object bị destroy.
  - `await` motion trong async method → truyền `CancellationToken` (`gameObject.GetCancellationTokenOnDestroy()`) để không callback vào object đã chết.

## 7. Manager System

**Hai scope: GameInstance (`GameManager` + `BaseManager`) và World (`WorldSubsystem` + `BaseWorldManager`). Phụ thuộc chỉ một chiều World → GameInstance.**

- Manager mới: kế thừa đúng base, đặt làm con của scope. Không sửa `GameManager.cs` / `WorldSubsystem.cs`.
- Phụ thuộc giữa manager khai báo trong `Initialize()` bằng `managers.InitializeDependency<T>()`, không gọi static.
- Chi tiết: xem [GUIDE/ManagerSystem.md](GUIDE/ManagerSystem.md).

## 8. Duyệt List / Array

**Editor dùng LINQ / `foreach` cho gọn. Runtime dùng `for` để tránh alloc GC.**

- Code chỉ chạy trên Editor (thư mục `Editor/`, block `#if UNITY_EDITOR`, custom inspector, menu tool, `OnValidate`) → được dùng LINQ, `foreach`.
- Code chạy runtime (gameplay, UI, manager, `Update`...) → dùng `for (int i = 0; i < list.Count; i++)`. Không dùng LINQ, không dùng `foreach`.

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.
