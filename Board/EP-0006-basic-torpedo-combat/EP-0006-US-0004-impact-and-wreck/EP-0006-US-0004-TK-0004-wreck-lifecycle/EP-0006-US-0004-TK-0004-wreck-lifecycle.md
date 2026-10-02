---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0004-impact-and-wreck
ticket: EP-0006-US-0004-TK-0004-wreck-lifecycle
title: "Новый неподвижный врек и очистка ссылок на погибший корабль"
stage: draft
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: ["EP-0006-US-0004-TK-0003-authoritative-impacts"]
files_touched: 4
serves: ["AC-0003","AC-0004"]
priority: P1
created: 2026-10-02T08:22:58Z
revision: 1
validation_status: not-run
---

# EP-0006-US-0004-TK-0004-wreck-lifecycle

Новый неподвижный врек и очистка ссылок на погибший корабль.

## Why

Результат тикета: **Новый неподвижный врек и очистка ссылок на погибший корабль**. Он нужен для истории [Три попадания превращают Тетрарх во врек](../EP-0006-US-0004-impact-and-wreck.md) и покрывает указанные `serves` в своей части; полные критерии story завершаются совокупностью её тикетов.

- **AC-0003** — При нуле HP создаётся новый Wreck с новым стабильным ID в точке корабля на время уничтожения; speed=0, direction=0; следующий выстрел доступен сразу.
- **AC-0004** — Любое ускорение и разбиение времени сохраняют первый фактический контакт, HP и единственный врек.

## Decisions

- **D09:** «корабль пиратов был уничтожен с трех попаданий»; «пока только на корабль типа Тетрарх. Все остальные объекты неуязвимые»; «превращается в новый объект врек со скоростью и направлением 0». Только явный ship.tetrarch имеет HP=450 и теряет по150: 450→300→150→0. При нуле живой корабль удаляется, новый stationary Wreck имеет новый ID, координаты погибшего корабля на момент контакта, speed=0, heading=0°.
- **D14:** «под лейблом ... зеленую полоску хитпоинтов»; у всех Тетрархов/игрока/full HP — «да»; врек — «серый круг диаметром 5 пикселей»; выбирать и обстреливать wreck — «да»; «просто продолжается»; звуки — «пока нет». Полоски HP постоянно под labels известных Тетрархов; wreck5px selectable и invulnerable. Игра продолжается без victory screen, лута или звуков.

Полный журнал, исходный scope и определения D/A — [Documentation.md](../../Documentation.md). Не переносить ранние предложения о вероятностях/КД/дальности из черновика обратно в MVP.

## Assumptions

Применяются A01–A12 эпика в части этого тикета. Все API ниже **предлагаемые**, кроме прямо названных существующих методов. Naming/overload можно уточнить без смены семантики и allowlist. Если для решения нужен дополнительный production/test файл, сначала уточнить scope; не скрывать его за количеством `files_touched`.

Новых продуктовых решений сверх согласованного объёма этот тикет не вводит. Порядок тестов и внутренние helper names выбираются в пределах заявленного слоя.

## Code context

Полный write allowlist от корня DSS; 4 файлов, включая тесты. Другие файлы — read-only. Новые файлы из зависимостей должны реально существовать к началу исполнения.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Существует: `src/DeepSpaceSaga.Engine/SimulationEngine.cs:528` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` | Создаётся зависимостью `EP-0006-US-0002-TK-0001-combat-bootstrap`; на baseline отсутствует | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs` | Существует: `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs:12` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `tests/DeepSpaceSaga.Engine.Tests/CombatWreckTests.cs` | Новый файл этого тикета; на baseline отсутствует | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |

Контекст для чтения: [grounding эпика](../../Documentation.md), [история](../EP-0006-US-0004-impact-and-wreck.md), `Documentation/01-Requirements/EngineRequirements.md` (§2, §52, §55–57, §60), `Documentation/00-Process/CLAUDE.md`. Code context определяет границу записи, ссылки для чтения её не расширяют.

## Dependencies

- [EP-0006-US-0004-TK-0003-authoritative-impacts](../../EP-0006-US-0004-impact-and-wreck/EP-0006-US-0004-TK-0003-authoritative-impacts/EP-0006-US-0004-TK-0003-authoritative-impacts.md)

Нужны реализованные и проверенные результаты зависимостей. При их отсутствии остановить этот implementation ticket с конкретным gap, не имитировать готовность mock-объектами и не дописывать зависимость за его пределами.

## Public API after the change

CreateWreck(destroyedObjectId,impactMotionTimeMs): Wreck, persistence=Permanent, MovementType=Stationary, speed=0, direction=0, fresh deterministic ID. Минимальный объект без crew/cargo/modules; дальнейшая утилизация вне эпика.

## Implementation steps

1. На HP=0 удалить живой NPC из active world и создать ровно один wreck в позиции уничтоженного корабля; не превращать исходный ObjectId в новый тип и не хранить active zero-HP ship.
2. Очистить selection/active/navigation ссылки на исчезнувший объект; detached camera следует текущему правилу last coordinates, без скачка к игроку. Новый wreck выбирается обычным способом.
3. Добавить Wreck в whitelist и валидацию stationary state ScenarioLoader, а Missile — только с согласованным combat payload в US-0007; не ослаблять все object validators.
4. Проверить второе попадание по wreck: объект неуязвим, нет ещё одного wreck. Не запускать победу, game-over, лут, salvage или NPC атаки.

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

- [ ] `Third_hit_creates_one_new_stationary_wreck`
- [ ] `Destroyed_target_references_are_cleared`
- [ ] `Wreck_is_invulnerable_to_followup_torpedo`
- [ ] `World_continues_after_pirate_destruction`

Из корня `D:/DeepSpaceSaga/DSS` (PowerShell):

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter 'FullyQualifiedName~Third_hit_creates_one_new_stationary_wreck|FullyQualifiedName~Destroyed_target_references_are_cleared|FullyQualifiedName~Wreck_is_invulnerable_to_followup_torpedo|FullyQualifiedName~World_continues_after_pirate_destruction'
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.cs src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs tests/DeepSpaceSaga.Engine.Tests/CombatWreckTests.cs
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

