---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0006-torpedo-trajectories
ticket: EP-0006-US-0006-TK-0002-live-flight-trajectories
title: "Постоянные линии полёта и точка встречи"
stage: draft
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: ["EP-0006-US-0006-TK-0001-authoritative-flight-history"]
files_touched: 4
serves: ["AC-0001","AC-0003","AC-0004"]
priority: P1
created: 2026-10-02T08:22:58Z
revision: 1
validation_status: not-run
---

# EP-0006-US-0006-TK-0002-live-flight-trajectories

Постоянные линии полёта и точка встречи.

## Why

Результат тикета: **Постоянные линии полёта и точка встречи**. Он нужен для истории [Пройденный путь, прогноз и предварительное наведение](../EP-0006-US-0006-torpedo-trajectories.md) и покрывает указанные `serves` в своей части; полные критерии story завершаются совокупностью её тикетов.

- **AC-0001** — Во время полёта видны полный пройденный путь жёлтым сплошным, прогноз жёлтым пунктиром и жёлтый крест встречи, а также траектория исходной цели до встречи.
- **AC-0003** — После взрыва прогноз/крест/линия цели исчезают, пройденный след остаётся 2000мс реального времени и затем удаляется.
- **AC-0004** — Смена selection, pause/speed, zoom/camera и загрузка не меняют зафиксированную цель или геометрию полёта.

## Decisions

- **D10:** «предсказательная траектория с местом предполагаемого пересечения с траекторией цели и отрисованная собственная пройденная траектория»; «желтая с размытием диаметр 5 пикселей»; оформление yellow solid/dashed/cross — «пока да»; показывать всегда — «все время». Жёлтая точка диаметром5px с blur; executed path solid yellow, remaining prediction dashed yellow, encounter cross yellow. Прогноз исходной цели до встречи тоже виден независимо от selection. Viewport clipping допустим.
- **D11:** След «исчезает через 2 секунды», время — «реального»; взрыв — «расходящийся из точки взрыва круг красного цвета»; «2 секунды от 0 пикселей до 50 радиуса плавно». После контакта прогноз/крест убираются, след живёт2000мс реального времени. Красное кольцо плавно расширяется0→50px радиуса и тускнеет до нуля за2000мс, независимо от pause/speed.
- **D13:** Показывать до выстрела при hover Пуск — «да но отличающимся цветом. например серым». При hover доступного Пуск показывается gray preview с точкой встречи и нужной траекторией цели. Чистая client prediction, без launch side effects.
- **D15:** «цветовые решения нужно вынести в отдельный файл который можно будет редактировать не компилируя код»; способ применения — «перезапуск». Отдельный combat palette JSON для torpedo/trail/prediction/intercept/preview/HP/explosion/wreck; читать при startup, без hot reload или build.

Полный журнал, исходный scope и определения D/A — [Documentation.md](../../Documentation.md). Не переносить ранние предложения о вероятностях/КД/дальности из черновика обратно в MVP.

## Assumptions

Применяются A01–A12 эпика в части этого тикета. Все API ниже **предлагаемые**, кроме прямо названных существующих методов. Naming/overload можно уточнить без смены семантики и allowlist. Если для решения нужен дополнительный production/test файл, сначала уточнить scope; не скрывать его за количеством `files_touched`.

Новых продуктовых решений сверх согласованного объёма этот тикет не вводит. Порядок тестов и внутренние helper names выбираются в пределах заявленного слоя.

## Code context

Полный write allowlist от корня DSS; 4 файлов, включая тесты. Другие файлы — read-only. Новые файлы из зависимостей должны реально существовать к началу исполнения.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatTrajectoryProjector.cs` | Новый файл этого тикета; на baseline отсутствует | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs` | Создаётся зависимостью `EP-0006-US-0003-TK-0003-launcher-command-panel`; на baseline отсутствует | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Существует: `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:706` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `tests/DeepSpaceSaga.Client.Tests/CombatTrajectoryTests.cs` | Новый файл этого тикета; на baseline отсутствует | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |

Контекст для чтения: [grounding эпика](../../Documentation.md), [история](../EP-0006-US-0006-torpedo-trajectories.md), `Documentation/01-Requirements/EngineRequirements.md` (§2, §52, §55–57, §60), `Documentation/00-Process/CLAUDE.md`. Code context определяет границу записи, ссылки для чтения её не расширяют.

## Dependencies

- [EP-0006-US-0006-TK-0001-authoritative-flight-history](../../EP-0006-US-0006-torpedo-trajectories/EP-0006-US-0006-TK-0001-authoritative-flight-history/EP-0006-US-0006-TK-0001-authoritative-flight-history.md)

Нужны реализованные и проверенные результаты зависимостей. При их отсутствии остановить этот implementation ticket с конкретным gap, не имитировать готовность mock-объектами и не дописывать зависимость за его пределами.

## Public API after the change

CombatTrajectoryProjector consumes immutable route/history and camera to produce clipped path geometry; source target is Torpedo.TargetObjectId, never current selection. Terminal trail deadline owned by CombatEffectStore.

## Implementation steps

1. Рисовать full travelled path, remaining prediction, intercept cross и projected target path до predicted encounter; исключить generic trail/prediction дубли для Missile.
2. Использовать shared guidance data и позы одного presentation timestamp, не отдельный approximate intercept solver в Client.
3. Показывать линии независимо от selection и общего showTrajectoryPrediction toggle для активного combat overlay; viewport clipping допустим, hidden-source knowledge остаётся masked.
4. После impact немедленно убрать прогноз и target segment; сохранить FinalTrail на 2000ms real-time deadline, даже если ни один snapshot активной торпеды не попал на экран.
5. Не строить бесконечную polyline для недостижимой цели: bounded viewport forecast без фиктивного креста/ETA. Сэмплирование для показа не меняет simulation geometry.

## Out of scope

Файлы вне allowlist; production слои вне `client`; AI пирата, защита/ПР, вероятность/баланс RNG, дальность/боезапас/TTL, реальные операторы, self-destruct, area damage, salvage, звуки, victory UI и общий refactor карты. Не менять существующие Board-статусы/requirements или commit/push в рамках реализации без соответствующего поручения.

## Invariants

- Gameplay HP, projectile lifecycle и collision принадлежат Engine; UI работает через immutable Contracts/session и shared Motion (CLAUDE.md, Architecture).
- Flight time — MotionTimeMs; real-time эффекты — monotonic UI clock (SimulationClock.Update; AuthoritativeSnapshot.MotionTimeMs). Пауза не продвигает мир, но session infrastructure работает (§52).
- `TargetObjectId` фиксируется в явной команде; current selection не заменяет цель полёта (PlayerCommand, строка7).
- Owner/self исключены из контактов; первый contact применяется один раз; chance100% не расходует RNG. Размеры5px/50px UI не являются collider world radius.
- Корабль Тетрарх определяется class identity, не картинкой; неизвестные свойства остаются masked. Legacy defaults не дают скрытой новой боевой способности.
- Сохраняются независимые worktree изменения. При ошибке baseline тестов записать конкретный результат, не объявлять весь suite зелёным и не править чужой fixture вне scope.

## Tests

Имена ниже — планируемые тесты, а не уже существующие результаты:

- [ ] `Flight_lines_survive_selection_change_and_pause`
- [ ] `Target_line_ends_at_same_predicted_encounter`
- [ ] `Terminal_trail_lives_exactly_two_real_seconds`
- [ ] `No_intercept_renders_no_false_cross`
- [ ] `Zoom_changes_screen_geometry_not_flight_state`

Из корня `D:/DeepSpaceSaga/DSS` (PowerShell):

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~Flight_lines_survive_selection_change_and_pause|FullyQualifiedName~Target_line_ends_at_same_predicted_encounter|FullyQualifiedName~Terminal_trail_lives_exactly_two_real_seconds|FullyQualifiedName~No_intercept_renders_no_false_cross|FullyQualifiedName~Zoom_changes_screen_geometry_not_flight_state'
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatTrajectoryProjector.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs tests/DeepSpaceSaga.Client.Tests/CombatTrajectoryTests.cs
git diff --check
```

Визуальные пункты требуют native/manual smoke и записанного evidence; headless tests не заменяют этот gate. Сроки эффектов проверять fake monotonic clock, без sleep/flaky wall-time assertions.

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

