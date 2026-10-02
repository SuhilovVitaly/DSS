---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0004-impact-and-wreck
ticket: EP-0006-US-0004-TK-0001-swept-impact-math
title: "Столкновение с центром между тиками"
stage: draft
layer: motion
test_project: tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj
depends_on: ["EP-0006-US-0003-TK-0003-launcher-command-panel"]
files_touched: 2
serves: ["AC-0001","AC-0004"]
priority: P1
created: 2026-10-02T08:22:58Z
revision: 1
validation_status: not-run
---

# EP-0006-US-0004-TK-0001-swept-impact-math

Столкновение с центром между тиками.

## Why

Результат тикета: **Столкновение с центром между тиками**. Он нужен для истории [Три попадания превращают Тетрарх во врек](../EP-0006-US-0004-impact-and-wreck.md) и покрывает указанные `serves` в своей части; полные критерии story завершаются совокупностью её тикетов.

- **AC-0001** — Каждое столкновение с любым чужим объектом в радиусе 5 world units завершает торпеду ровно один раз; носитель и сама торпеда исключены.
- **AC-0004** — Любое ускорение и разбиение времени сохраняют первый фактический контакт, HP и единственный врек.

## Decisions

- **D08:** «гарантированно попадает»; «давай центр»; «нужна погрешность скажем 5 пикселей»; фиксированный масштаб — «хорошо»; «может столкнуться. но не с кораблем игрока»; одинаковый допуск/урон для препятствий — «да». Цель наведения — центр. Контакт при расстоянии <=5 world units (500 м, 5 px при 100 м/px) с любым чужим объектом; self projectile и owner исключены. Первый контакт взрывает торпеду, без случайного броска и без сквозного полёта.

Полный журнал, исходный scope и определения D/A — [Documentation.md](../../Documentation.md). Не переносить ранние предложения о вероятностях/КД/дальности из черновика обратно в MVP.

## Assumptions

Применяются A01–A12 эпика в части этого тикета. Все API ниже **предлагаемые**, кроме прямо названных существующих методов. Naming/overload можно уточнить без смены семантики и allowlist. Если для решения нужен дополнительный production/test файл, сначала уточнить scope; не скрывать его за количеством `files_touched`.

При любом ограничении solver бюджеты не должны молча превращать потенциальное попадание в промах; сначала дробить интервал детерминированно. Без обращения к Client/Skia.

## Code context

Полный write allowlist от корня DSS; 2 файлов, включая тесты. Другие файлы — read-only. Новые файлы из зависимостей должны реально существовать к началу исполнения.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Motion/TorpedoCollisionMath.cs` | Новый файл этого тикета; на baseline отсутствует | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `tests/DeepSpaceSaga.Motion.Tests/TorpedoCollisionTests.cs` | Новый файл этого тикета; на baseline отсутствует | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |

Контекст для чтения: [grounding эпика](../../Documentation.md), [история](../EP-0006-US-0004-impact-and-wreck.md), `Documentation/01-Requirements/EngineRequirements.md` (§2, §52, §55–57, §60), `Documentation/00-Process/CLAUDE.md`. Code context определяет границу записи, ссылки для чтения её не расширяют.

## Dependencies

- [EP-0006-US-0003-TK-0003-launcher-command-panel](../../EP-0006-US-0003-manual-guided-launch/EP-0006-US-0003-TK-0003-launcher-command-panel/EP-0006-US-0003-TK-0003-launcher-command-panel.md)

Нужны реализованные и проверенные результаты зависимостей. При их отсутствии остановить этот implementation ticket с конкретным gap, не имитировать готовность mock-объектами и не дописывать зависимость за его пределами.

## Public API after the change

Предлагается FirstContact(projectilePath,targetPath,fromMotionMs,toMotionMs,radiusWorldUnits)->Contact?; Contact(time,x,y). Радиус 5 world units = 500 м, граница включается. Tie-break выбора объекта принадлежит Engine.

## Implementation steps

1. Проверять относительное движение по всему интервалу, а не только расстояние на концах. Для прямых использовать аналитическое пересечение; для дуг/орбит — детерминированное ограничение ошибки/поиск первого корня с явно зафиксированным tolerance.
2. Учитывать начальное перекрытие и касательное касание. Не привязывать число шагов к кадрам, zoom или GameTimeMultiplier.
3. Документировать допуски и проверить независимым численным oracle на пересечениях/касаниях, больших координатах и длинном ускоренном advance.

## Out of scope

Файлы вне allowlist; production слои вне `motion`; AI пирата, защита/ПР, вероятность/баланс RNG, дальность/боезапас/TTL, реальные операторы, self-destruct, area damage, salvage, звуки, victory UI и общий refactor карты. Не менять существующие Board-статусы/requirements или commit/push в рамках реализации без соответствующего поручения.

## Invariants

- Gameplay HP, projectile lifecycle и collision принадлежат Engine; UI работает через immutable Contracts/session и shared Motion (CLAUDE.md, Architecture).
- Flight time — MotionTimeMs; real-time эффекты — monotonic UI clock (SimulationClock.Update; AuthoritativeSnapshot.MotionTimeMs). Пауза не продвигает мир, но session infrastructure работает (§52).
- `TargetObjectId` фиксируется в явной команде; current selection не заменяет цель полёта (PlayerCommand, строка7).
- Owner/self исключены из контактов; первый contact применяется один раз; chance100% не расходует RNG. Размеры5px/50px UI не являются collider world radius.
- Корабль Тетрарх определяется class identity, не картинкой; неизвестные свойства остаются masked. Legacy defaults не дают скрытой новой боевой способности.
- Сохраняются независимые worktree изменения. При ошибке baseline тестов записать конкретный результат, не объявлять весь suite зелёным и не править чужой fixture вне scope.

## Tests

Имена ниже — планируемые тесты, а не уже существующие результаты:

- [ ] `Swept_contact_catches_crossing_between_tick_endpoints`
- [ ] `Contact_at_radius_boundary_is_inclusive`
- [ ] `Moving_obstacle_and_arc_first_contact_are_detected`
- [ ] `Contact_is_partition_and_zoom_independent`

Из корня `D:/DeepSpaceSaga/DSS` (PowerShell):

```powershell
dotnet test tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --no-restore --filter 'FullyQualifiedName~Swept_contact_catches_crossing_between_tick_endpoints|FullyQualifiedName~Contact_at_radius_boundary_is_inclusive|FullyQualifiedName~Moving_obstacle_and_arc_first_contact_are_detected|FullyQualifiedName~Contact_is_partition_and_zoom_independent'
dotnet test tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Motion/DeepSpaceSaga.Motion.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Motion/TorpedoCollisionMath.cs tests/DeepSpaceSaga.Motion.Tests/TorpedoCollisionTests.cs
git diff --check
```

Изменение общего predictor дополнительно требует Motion/Engine/Client regression suites; не менять их source вне allowlist.

## Definition of Done

- [ ] Все шаги выполнены только в Code context; число файлов/слой совпадают с frontmatter.
- [ ] Наблюдаемый end state и вклад в каждый `serves` подтверждены named tests.
- [ ] Точные команды/результаты test/build/scoped format/diff check записаны при реализации; baseline debt отделён.
- [ ] Согласованные API, units/time, deterministic ordering, legacy defaults и out-of-scope соблюдены.
- [ ] Для визуального результата пройден native smoke, либо тикет явно остаётся без окончательной приёмки.
- [ ] Нет скрытых зависимостей/дополнительных файлов, незаписанных assumptions или фиктивного evidence.

## Self-containment check

Родительские epic/story связаны; решения, scope, proposed API, шаги, tests, layer и dependencies указаны. Grounding различает существующий source и будущие helpers. Чтение контекста допустимо, но новые продуктовые решения и расширение allowlist не требуются без изменения baseline.

**Текущий статус:** draft planning; implementation, automated tests и native validation не выполнялись.

