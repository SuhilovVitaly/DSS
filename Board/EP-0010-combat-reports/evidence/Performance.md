# EP-0010 — frame-time evidence

Статус: **NOT RUN / OPEN**. Цель проекта80 FPS,12,5 ms на кадр. Не утверждать, что исторические performance gates закрыты.

Протокол US0011-TK0003: CombatMukha, фиксированный seed/команды/скорость/zoom, одинаковая сборка/hardware/viewport/scale/locale; отдельно baseline без новой панели и новая панель с 0/200 карточками. Указать warm-up, длительность и число кадров; измерять p50/p95/p99/max frame interval, UI preparation и отсутствие decode/file I/O в Draw. Отдельно отметить CPU/native present/GPU/scanout ограничения доступного измерения. Плохой baseline не превращает новый результат в PASS80 FPS.

| Run | Build/seed/hardware | Viewport/scale | Duration/frames | p50 | p95 | p99 | Max | Result |
|---|---|---|---|---|---|---|---|---|
| Baseline | NOT RUN | — | — | — | — | — | — | OPEN |
| Reports0 | NOT RUN | — | — | — | — | — | — | OPEN |
| Reports200 | NOT RUN | — | — | — | — | — | — | OPEN |

Приложить raw evidence и причину любого NOT RUN. Человеческая плавность и scanout — отдельные наблюдения, не следствие dotnet test.
