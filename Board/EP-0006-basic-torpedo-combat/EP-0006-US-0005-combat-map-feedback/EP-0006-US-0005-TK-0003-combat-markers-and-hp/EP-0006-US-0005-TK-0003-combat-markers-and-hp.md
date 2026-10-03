---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0005-combat-map-feedback
ticket: EP-0006-US-0005-TK-0003-combat-markers-and-hp
title: "Торпеда, врек и зелёная полоска HP"
stage: draft
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: ["EP-0006-US-0005-TK-0002-combat-palette-loading"]
files_touched: 5
serves: ["AC-0001","AC-0002"]
priority: P1
created: 2026-10-02T08:22:58Z
revision: 1
validation_status: not-run
---

# EP-0006-US-0005-TK-0003-combat-markers-and-hp

Торпеда, врек и зелёная полоска HP.

## Why

Результат тикета: **Торпеда, врек и зелёная полоска HP**. Он нужен для истории [Игрок видит торпеду, прочность, взрыв и врек](../EP-0006-US-0005-combat-map-feedback.md) и покрывает указанные `serves` в своей части; полные критерии story завершаются совокупностью её тикетов.

- **AC-0001** — Жёлтая торпеда диаметром 5 px с blur, серый wreck диаметром 5 px; размеры не масштабируются с zoom.
- **AC-0002** — У всех известных Тетрархов, включая игрока, постоянно есть зелёная полоска под label: full→2/3→1/3; после гибели полоски нет.

## Decisions

- **D09:** «корабль пиратов был уничтожен с трех попаданий»; «пока только на корабль типа Тетрарх. Все остальные объекты неуязвимые»; «превращается в новый объект врек со скоростью и направлением 0». Только явный ship.tetrarch имеет HP=450 и теряет по150: 450→300→150→0. При нуле живой корабль удаляется, новый stationary Wreck имеет новый ID, координаты погибшего корабля на момент контакта, speed=0, heading=0°.
- **D10:** «предсказательная траектория с местом предполагаемого пересечения с траекторией цели и отрисованная собственная пройденная траектория»; «желтая с размытием диаметр 5 пикселей»; оформление yellow solid/dashed/cross — «пока да»; показывать всегда — «все время». Жёлтая точка диаметром5px с blur; executed path solid yellow, remaining prediction dashed yellow, encounter cross yellow. Прогноз исходной цели до встречи тоже виден независимо от selection. Viewport clipping допустим.
- **D14:** «под лейблом ... зеленую полоску хитпоинтов»; у всех Тетрархов/игрока/full HP — «да»; врек — «серый круг диаметром 5 пикселей»; выбирать и обстреливать wreck — «да»; «просто продолжается»; звуки — «пока нет». Полоски HP постоянно под labels известных Тетрархов; wreck5px selectable и invulnerable. Игра продолжается без victory screen, лута или звуков.
- **D15:** «цветовые решения нужно вынести в отдельный файл который можно будет редактировать не компилируя код»; способ применения — «перезапуск». Отдельный combat palette JSON для torpedo/trail/prediction/intercept/preview/HP/explosion/wreck; читать при startup, без hot reload или build.

Полный журнал, исходный scope и определения D/A — [Documentation.md](../../Documentation.md). Не переносить ранние предложения о вероятностях/КД/дальности из черновика обратно в MVP.

## Assumptions

Применяются A01–A12 эпика в части этого тикета. Все API ниже **предлагаемые**, кроме прямо названных существующих методов. Naming/overload можно уточнить без смены семантики и allowlist. Если для решения нужен дополнительный production/test файл, сначала уточнить scope; не скрывать его за количеством `files_touched`.

Новых продуктовых решений сверх согласованного объёма этот тикет не вводит. Порядок тестов и внутренние helper names выбираются в пределах заявленного слоя.

## Code context

Полный write allowlist от корня DSS; 5 файлов, включая тесты. Другие файлы — read-only. Новые файлы из зависимостей должны реально существовать к началу исполнения.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs` | Создаётся зависимостью `EP-0006-US-0003-TK-0003-launcher-command-panel`; на baseline отсутствует | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Существует: `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:706` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs` | Существует: `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs:98` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelLayout.cs` | Существует: `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelLayout.cs:1` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `tests/DeepSpaceSaga.Client.Tests/CombatMapVisualTests.cs` | Новый файл этого тикета; на baseline отсутствует | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |

Контекст для чтения: [grounding эпика](../../Documentation.md), [история](../EP-0006-US-0005-combat-map-feedback.md), `Documentation/01-Requirements/EngineRequirements.md` (§2, §52, §55–57, §60), `Documentation/00-Process/CLAUDE.md`. Code context определяет границу записи, ссылки для чтения её не расширяют.

## Dependencies

- [EP-0006-US-0005-TK-0002-combat-palette-loading](../../EP-0006-US-0005-combat-map-feedback/EP-0006-US-0005-TK-0002-combat-palette-loading/EP-0006-US-0005-TK-0002-combat-palette-loading.md)

Нужны реализованные и проверенные результаты зависимостей. При их отсутствии остановить этот implementation ticket с конкретным gap, не имитировать готовность mock-объектами и не дописывать зависимость за его пределами.

## Public API after the change

No domain API changes. Dedicated combat drawing helper consumes authoritative HullCombat/Torpedo and palette. Screen pixels are physical canvas pixels, independent of camera zoom and UI scale.

## Implementation steps

1. Рисовать projectile yellow core диаметром5 + blur halo; wreck gray circle диаметром5; исключить двойное generic sprite/marker рисование этих объектов.
2. Добавить зелёную HP полосу под label известных Тетрархов, в том числе игрока и full HP; ratio clamp по authoritative current/max. Label geometry/hit bounds включают полосу.
3. Не скрывать active torpedo или боевую цель cluster/label budget правилами, когда они в viewport; вне viewport обычный clipping допустим. Unknown object не раскрывает класс/HP до разрешённой knowledge projection.
4. Все цвета брать из CombatVisualSettings; освобождать SKPaint/filters по текущему lifetime pattern, не создавать blur resources на каждом кадре.

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

- [ ] `Torpedo_and_wreck_remain_five_pixels_across_zoom`
- [ ] `All_known_tetrarchs_show_authoritative_hp_below_labels`
- [ ] `Hp_visual_progress_is_full_two_thirds_one_third`
- [ ] `Combat_markers_are_not_duplicated_or_cluster_hidden`

Из корня `D:/DeepSpaceSaga/DSS` (PowerShell):

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~Torpedo_and_wreck_remain_five_pixels_across_zoom|FullyQualifiedName~All_known_tetrarchs_show_authoritative_hp_below_labels|FullyQualifiedName~Hp_visual_progress_is_full_two_thirds_one_third|FullyQualifiedName~Combat_markers_are_not_duplicated_or_cluster_hidden'
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelLayout.cs tests/DeepSpaceSaga.Client.Tests/CombatMapVisualTests.cs
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

