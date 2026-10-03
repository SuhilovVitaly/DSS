---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0004-impact-and-wreck
ticket: EP-0006-US-0004-TK-0002-impact-event-contract
title: "Данные завершения полёта для интерфейса"
stage: draft
layer: contracts
test_project: tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj
depends_on: ["EP-0006-US-0004-TK-0001-swept-impact-math"]
files_touched: 3
serves: ["AC-0001","AC-0003"]
priority: P1
created: 2026-10-02T08:22:58Z
revision: 1
validation_status: not-run
---

# EP-0006-US-0004-TK-0002-impact-event-contract

Данные завершения полёта для интерфейса.

## Why

Результат тикета: **Данные завершения полёта для интерфейса**. Он нужен для истории [Три попадания превращают Тетрарх во врек](../EP-0006-US-0004-impact-and-wreck.md) и покрывает указанные `serves` в своей части; полные критерии story завершаются совокупностью её тикетов.

- **AC-0001** — Каждое столкновение с любым чужим объектом в радиусе 5 world units завершает торпеду ровно один раз; носитель и сама торпеда исключены.
- **AC-0003** — При нуле HP создаётся новый Wreck с новым стабильным ID в точке корабля на время уничтожения; speed=0, direction=0; следующий выстрел доступен сразу.

## Decisions

- **D08:** «гарантированно попадает»; «давай центр»; «нужна погрешность скажем 5 пикселей»; фиксированный масштаб — «хорошо»; «может столкнуться. но не с кораблем игрока»; одинаковый допуск/урон для препятствий — «да». Цель наведения — центр. Контакт при расстоянии <=5 world units (500 м, 5 px при 100 м/px) с любым чужим объектом; self projectile и owner исключены. Первый контакт взрывает торпеду, без случайного броска и без сквозного полёта.
- **D11:** След «исчезает через 2 секунды», время — «реального»; взрыв — «расходящийся из точки взрыва круг красного цвета»; «2 секунды от 0 пикселей до 50 радиуса плавно». После контакта прогноз/крест убираются, след живёт2000мс реального времени. Красное кольцо плавно расширяется0→50px радиуса и тускнеет до нуля за2000мс, независимо от pause/speed.
- **D12:** Сохранять торпеду, busy и урон — «да»; сохранять весь активный след — «да»; «при сохранении взрыв сохранять не нужно». Сохраняются authoritative flight/target/owner/module/route/history/distance/captured parameters, HP, wreck и ID counters. Взрыв/real-time animation state не сохраняются.

Полный журнал, исходный scope и определения D/A — [Documentation.md](../../Documentation.md). Не переносить ранние предложения о вероятностях/КД/дальности из черновика обратно в MVP.

## Assumptions

Применяются A01–A12 эпика в части этого тикета. Все API ниже **предлагаемые**, кроме прямо названных существующих методов. Naming/overload можно уточнить без смены семантики и allowlist. Если для решения нужен дополнительный production/test файл, сначала уточнить scope; не скрывать его за количеством `files_touched`.

Новых продуктовых решений сверх согласованного объёма этот тикет не вводит. Порядок тестов и внутренние helper names выбираются в пределах заявленного слоя.

## Code context

Полный write allowlist от корня DSS; 3 файлов, включая тесты. Другие файлы — read-only. Новые файлы из зависимостей должны реально существовать к началу исполнения.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/CombatSnapshot.cs` | Создаётся зависимостью `EP-0006-US-0001-TK-0001-combat-contract`; на baseline отсутствует | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs` | Существует: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `tests/DeepSpaceSaga.Contracts.Tests/CombatEventTests.cs` | Новый файл этого тикета; на baseline отсутствует | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |

Контекст для чтения: [grounding эпика](../../Documentation.md), [история](../EP-0006-US-0004-impact-and-wreck.md), `Documentation/01-Requirements/EngineRequirements.md` (§2, §52, §55–57, §60), `Documentation/00-Process/CLAUDE.md`. Code context определяет границу записи, ссылки для чтения её не расширяют.

## Dependencies

- [EP-0006-US-0004-TK-0001-swept-impact-math](../../EP-0006-US-0004-impact-and-wreck/EP-0006-US-0004-TK-0001-swept-impact-math/EP-0006-US-0004-TK-0001-swept-impact-math.md)

Нужны реализованные и проверенные результаты зависимостей. При их отсутствии остановить этот implementation ticket с конкретным gap, не имитировать готовность mock-объектами и не дописывать зависимость за его пределами.

## Public API after the change

CombatImpactSnapshot(EventId,TorpedoObjectId,OwnerObjectId,LauncherModuleId,TargetObjectId,HitObjectId,MotionTimeMs,X,Y,FinalTrail,DamageApplied,DestroyedObjectId?,WreckObjectId?). AuthoritativeSnapshot.CombatImpacts — optional immutable array. EventId уникален в сессии.

## Implementation steps

1. Отделить факт попадания от двухсекундной анимации: Engine не передаёт screen pixels/Stopwatch timestamps.
2. Пакет содержит финальную геометрию следа, чтобы событие оставалось отображаемым, даже когда торпеда исчезла между snapshots.
3. Зафиксировать дедупликацию EventId и session reset; коллекция переносится через существующий session boundary, без отдельного message transport.

## Out of scope

Файлы вне allowlist; production слои вне `contracts`; AI пирата, защита/ПР, вероятность/баланс RNG, дальность/боезапас/TTL, реальные операторы, self-destruct, area damage, salvage, звуки, victory UI и общий refactor карты. Не менять существующие Board-статусы/requirements или commit/push в рамках реализации без соответствующего поручения.

## Invariants

- Gameplay HP, projectile lifecycle и collision принадлежат Engine; UI работает через immutable Contracts/session и shared Motion (CLAUDE.md, Architecture).
- Flight time — MotionTimeMs; real-time эффекты — monotonic UI clock (SimulationClock.Update; AuthoritativeSnapshot.MotionTimeMs). Пауза не продвигает мир, но session infrastructure работает (§52).
- `TargetObjectId` фиксируется в явной команде; current selection не заменяет цель полёта (PlayerCommand, строка7).
- Owner/self исключены из контактов; первый contact применяется один раз; chance100% не расходует RNG. Размеры5px/50px UI не являются collider world radius.
- Корабль Тетрарх определяется class identity, не картинкой; неизвестные свойства остаются masked. Legacy defaults не дают скрытой новой боевой способности.
- Сохраняются независимые worktree изменения. При ошибке baseline тестов записать конкретный результат, не объявлять весь suite зелёным и не править чужой fixture вне scope.

## Tests

Имена ниже — планируемые тесты, а не уже существующие результаты:

- [ ] `Impact_event_roundtrips_final_trail_and_object_ids`
- [ ] `Legacy_snapshot_has_empty_combat_impacts`

Из корня `D:/DeepSpaceSaga/DSS` (PowerShell):

```powershell
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter 'FullyQualifiedName~Impact_event_roundtrips_final_trail_and_object_ids|FullyQualifiedName~Legacy_snapshot_has_empty_combat_impacts'
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Contracts/CombatSnapshot.cs src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs tests/DeepSpaceSaga.Contracts.Tests/CombatEventTests.cs
git diff --check
```

При затрагивании shared DTO/loader выполнить соответствующие consumer regression suites. Не расширять production scope ради зелёного полного прогона.

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

