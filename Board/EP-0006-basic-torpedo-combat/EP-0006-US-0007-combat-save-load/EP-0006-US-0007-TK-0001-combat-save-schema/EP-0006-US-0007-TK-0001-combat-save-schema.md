---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0007-combat-save-load
ticket: EP-0006-US-0007-TK-0001-combat-save-schema
title: "Версия и проверка схемы сохранения боя"
stage: draft
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: ["EP-0006-US-0006-TK-0003-launch-hover-preview"]
files_touched: 4
serves: ["AC-0001","AC-0002","AC-0003"]
priority: P1
created: 2026-10-02T08:22:58Z
revision: 1
validation_status: not-run
---

# EP-0006-US-0007-TK-0001-combat-save-schema

Версия и проверка схемы сохранения боя.

## Why

Результат тикета: **Версия и проверка схемы сохранения боя**. Он нужен для истории [Продолжение боя после сохранения и загрузки](../EP-0006-US-0007-combat-save-load.md) и покрывает указанные `serves` в своей части; полные критерии story завершаются совокупностью её тикетов.

- **AC-0001** — Реальная файловая загрузка восстанавливает активную торпеду, captured параметры, owner/module/target, route/history/distance, motion time, HP и IDs.
- **AC-0002** — Загрузка после 1/2 попаданий не лечит корабль; после 3-го сохраняет единственный wreck. Повтор команд и advancement не дублируют попадание/врек.
- **AC-0003** — Busy ↔ active torpedo — согласованная связь; malformed saves не создают частично загруженную сессию.

## Decisions

- **D12:** Сохранять торпеду, busy и урон — «да»; сохранять весь активный след — «да»; «при сохранении взрыв сохранять не нужно». Сохраняются authoritative flight/target/owner/module/route/history/distance/captured parameters, HP, wreck и ID counters. Взрыв/real-time animation state не сохраняются.
- **D13:** Показывать до выстрела при hover Пуск — «да но отличающимся цветом. например серым». При hover доступного Пуск показывается gray preview с точкой встречи и нужной траекторией цели. Чистая client prediction, без launch side effects.
- **D17:** «для тетрарха в настройки тетрарха, для торпеды в настройки торпедного аппарата который является модулем тетрарха»; скорость и turn rate в модуле — «да». HP450 принадлежит class config Тетрарха; damage150/speed3/turnRate90 — конфигурации module.torpedo.launcher.basic, а не экземпляру палитры/константам Client.

Полный журнал, исходный scope и определения D/A — [Documentation.md](../../Documentation.md). Не переносить ранние предложения о вероятностях/КД/дальности из черновика обратно в MVP.

## Assumptions

Применяются A01–A12 эпика в части этого тикета. Все API ниже **предлагаемые**, кроме прямо названных существующих методов. Naming/overload можно уточнить без смены семантики и allowlist. Если для решения нужен дополнительный production/test файл, сначала уточнить scope; не скрывать его за количеством `files_touched`.

Новых продуктовых решений сверх согласованного объёма этот тикет не вводит. Порядок тестов и внутренние helper names выбираются в пределах заявленного слоя.

## Code context

Полный write allowlist от корня DSS; 4 файлов, включая тесты. Другие файлы — read-only. Новые файлы из зависимостей должны реально существовать к началу исполнения.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Scenario/CombatSaveData.cs` | Новый файл этого тикета; на baseline отсутствует | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs` | Существует: `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:102` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs` | Существует: `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs:12` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `tests/DeepSpaceSaga.Engine.Tests/CombatSaveSchemaTests.cs` | Новый файл этого тикета; на baseline отсутствует | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |

Контекст для чтения: [grounding эпика](../../Documentation.md), [история](../EP-0006-US-0007-combat-save-load.md), `Documentation/01-Requirements/EngineRequirements.md` (§2, §52, §55–57, §60), `Documentation/00-Process/CLAUDE.md`. Code context определяет границу записи, ссылки для чтения её не расширяют.

## Dependencies

- [EP-0006-US-0006-TK-0003-launch-hover-preview](../../EP-0006-US-0006-torpedo-trajectories/EP-0006-US-0006-TK-0003-launch-hover-preview/EP-0006-US-0006-TK-0003-launch-hover-preview.md)

Нужны реализованные и проверенные результаты зависимостей. При их отсутствии остановить этот implementation ticket с конкретным gap, не имитировать готовность mock-объектами и не дописывать зависимость за его пределами.

## Public API after the change

Новая versioned CombatStateData: active torpedoes with route/history/captured parameters, lastProcessedMotionTimeMs, nextProjectileId, nextImpactEventId, processed launch IDs as needed by journal. Instance shipClassId/currentHp/maxHp frozen for continuing save; wreck row ordinary stationary object. Increment current SaveFormat (baseline=9, proposed=10; recheck at implementation).

## Implementation steps

1. Добавить optional combat state для legacy и strict required consistency для новой версии; Missile допустим только с полноценным combat payload, Wreck — stationary/no modules.
2. Валидировать unique IDs, one projectile per launcher, существование owner/module, конечные координаты, valid route version, nonnegative distance/time и 0<=HP<=max. В new saves current HP обязателен для classified ship, нельзя молча лечить отсутствующее поле.
3. Отсутствующая target допускается лишь как явно сохранённый target-lost режим, а не повреждённая незаметно ссылка. HP/max class параметры snapshot фиксируются в save, не пересчитываются по отредактированному JSON.
4. Legacy без class/combat грузится без оружия/уязвимости; не мигрировать классы по sprite. Unknown future versions fail clearly, malformed input must not mutate existing session.

## Out of scope

Файлы вне allowlist; production слои вне `engine`; AI пирата, защита/ПР, вероятность/баланс RNG, дальность/боезапас/TTL, реальные операторы, self-destruct, area damage, salvage, звуки, victory UI и общий refactor карты. Не менять существующие Board-статусы/requirements или commit/push в рамках реализации без соответствующего поручения.

## Invariants

- Gameplay HP, projectile lifecycle и collision принадлежат Engine; UI работает через immutable Contracts/session и shared Motion (CLAUDE.md, Architecture).
- Flight time — MotionTimeMs; real-time эффекты — monotonic UI clock (SimulationClock.Update; AuthoritativeSnapshot.MotionTimeMs). Пауза не продвигает мир, но session infrastructure работает (§52).
- `TargetObjectId` фиксируется в явной команде; current selection не заменяет цель полёта (PlayerCommand, строка7).
- Owner/self исключены из контактов; первый contact применяется один раз; chance100% не расходует RNG. Размеры5px/50px UI не являются collider world radius.
- Корабль Тетрарх определяется class identity, не картинкой; неизвестные свойства остаются masked. Legacy defaults не дают скрытой новой боевой способности.
- Сохраняются независимые worktree изменения. При ошибке baseline тестов записать конкретный результат, не объявлять весь suite зелёным и не править чужой fixture вне scope.

## Tests

Имена ниже — планируемые тесты, а не уже существующие результаты:

- [ ] `Combat_schema_rejects_orphan_duplicate_and_nonfinite_state`
- [ ] `Current_save_requires_complete_hp_and_flight_state`
- [ ] `Legacy_v9_without_combat_remains_readable`
- [ ] `Future_combat_route_version_is_rejected`

Из корня `D:/DeepSpaceSaga/DSS` (PowerShell):

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter 'FullyQualifiedName~Combat_schema_rejects_orphan_duplicate_and_nonfinite_state|FullyQualifiedName~Current_save_requires_complete_hp_and_flight_state|FullyQualifiedName~Legacy_v9_without_combat_remains_readable|FullyQualifiedName~Future_combat_route_version_is_rejected'
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/Scenario/CombatSaveData.cs src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs tests/DeepSpaceSaga.Engine.Tests/CombatSaveSchemaTests.cs
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

