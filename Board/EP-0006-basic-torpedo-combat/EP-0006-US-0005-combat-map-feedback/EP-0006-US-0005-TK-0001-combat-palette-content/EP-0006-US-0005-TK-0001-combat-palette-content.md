---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0005-combat-map-feedback
ticket: EP-0006-US-0005-TK-0001-combat-palette-content
title: "Отдельный файл цветовой схемы боя"
stage: draft
layer: content-data
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: ["EP-0006-US-0004-TK-0004-wreck-lifecycle"]
files_touched: 2
serves: ["AC-0001","AC-0002","AC-0003","AC-0004"]
priority: P1
created: 2026-10-02T08:22:58Z
revision: 1
validation_status: not-run
---

# EP-0006-US-0005-TK-0001-combat-palette-content

Отдельный файл цветовой схемы боя.

## Why

Результат тикета: **Отдельный файл цветовой схемы боя**. Он нужен для истории [Игрок видит торпеду, прочность, взрыв и врек](../EP-0006-US-0005-combat-map-feedback.md) и покрывает указанные `serves` в своей части; полные критерии story завершаются совокупностью её тикетов.

- **AC-0001** — Жёлтая торпеда диаметром 5 px с blur, серый wreck диаметром 5 px; размеры не масштабируются с zoom.
- **AC-0002** — У всех известных Тетрархов, включая игрока, постоянно есть зелёная полоска под label: full→2/3→1/3; после гибели полоски нет.
- **AC-0003** — Взрыв: красное кольцо 0→50 px радиуса за 2000 мс реального времени с плавным угасанием, независимо от паузы/ускорения.
- **AC-0004** — Торпеда/wreck выбираются; панель торпеды содержит цель, скорость, пройденный путь, ETA и шанс 100%. Цвета читаются из отдельного JSON при запуске.

## Decisions

- **D10:** «предсказательная траектория с местом предполагаемого пересечения с траекторией цели и отрисованная собственная пройденная траектория»; «желтая с размытием диаметр 5 пикселей»; оформление yellow solid/dashed/cross — «пока да»; показывать всегда — «все время». Жёлтая точка диаметром5px с blur; executed path solid yellow, remaining prediction dashed yellow, encounter cross yellow. Прогноз исходной цели до встречи тоже виден независимо от selection. Viewport clipping допустим.
- **D11:** След «исчезает через 2 секунды», время — «реального»; взрыв — «расходящийся из точки взрыва круг красного цвета»; «2 секунды от 0 пикселей до 50 радиуса плавно». После контакта прогноз/крест убираются, след живёт2000мс реального времени. Красное кольцо плавно расширяется0→50px радиуса и тускнеет до нуля за2000мс, независимо от pause/speed.
- **D12:** Сохранять торпеду, busy и урон — «да»; сохранять весь активный след — «да»; «при сохранении взрыв сохранять не нужно». Сохраняются authoritative flight/target/owner/module/route/history/distance/captured parameters, HP, wreck и ID counters. Взрыв/real-time animation state не сохраняются.
- **D14:** «под лейблом ... зеленую полоску хитпоинтов»; у всех Тетрархов/игрока/full HP — «да»; врек — «серый круг диаметром 5 пикселей»; выбирать и обстреливать wreck — «да»; «просто продолжается»; звуки — «пока нет». Полоски HP постоянно под labels известных Тетрархов; wreck5px selectable и invulnerable. Игра продолжается без victory screen, лута или звуков.
- **D15:** «цветовые решения нужно вынести в отдельный файл который можно будет редактировать не компилируя код»; способ применения — «перезапуск». Отдельный combat palette JSON для torpedo/trail/prediction/intercept/preview/HP/explosion/wreck; читать при startup, без hot reload или build.

Полный журнал, исходный scope и определения D/A — [Documentation.md](../../Documentation.md). Не переносить ранние предложения о вероятностях/КД/дальности из черновика обратно в MVP.

## Assumptions

Применяются A01–A12 эпика в части этого тикета. Все API ниже **предлагаемые**, кроме прямо названных существующих методов. Naming/overload можно уточнить без смены семантики и allowlist. Если для решения нужен дополнительный production/test файл, сначала уточнить scope; не скрывать его за количеством `files_touched`.

Новых продуктовых решений сверх согласованного объёма этот тикет не вводит. Порядок тестов и внутренние helper names выбираются в пределах заявленного слоя.

## Code context

Полный write allowlist от корня DSS; 2 файлов, включая тесты. Другие файлы — read-only. Новые файлы из зависимостей должны реально существовать к началу исполнения.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/UI/combat-visuals.json` | Новый файл этого тикета; на baseline отсутствует | Только данные, явно названные в Public API и шагах этого тикета |
| `tests/DeepSpaceSaga.Client.Tests/CombatPaletteContentTests.cs` | Новый файл этого тикета; на baseline отсутствует | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |

Контекст для чтения: [grounding эпика](../../Documentation.md), [история](../EP-0006-US-0005-combat-map-feedback.md), `Documentation/01-Requirements/EngineRequirements.md` (§2, §52, §55–57, §60), `Documentation/00-Process/CLAUDE.md`. Code context определяет границу записи, ссылки для чтения её не расширяют.

## Dependencies

- [EP-0006-US-0004-TK-0004-wreck-lifecycle](../../EP-0006-US-0004-impact-and-wreck/EP-0006-US-0004-TK-0004-wreck-lifecycle/EP-0006-US-0004-TK-0004-wreck-lifecycle.md)

Нужны реализованные и проверенные результаты зависимостей. При их отсутствии остановить этот implementation ticket с конкретным gap, не имитировать готовность mock-объектами и не дописывать зависимость за его пределами.

## Public API after the change

schemaVersion=1; цвета #RRGGBBAA: torpedo #FFFF00FF, trail #FFFF00FF, prediction #FFFF00FF, intercept #FFFF00FF, preview #808080FF, hullHp #00FF00FF, explosion #FF0000FF, wreck #808080FF. Это редактируемые стартовые значения.

## Implementation steps

1. Создать только палитру боевых элементов; не переносить общую тему всех экранов.
2. Проверить необходимые keys, alpha syntax и упаковку через существующее Data/**/*.json правило.
3. Сохранять численные игровые параметры в content оружия/корабля, а не в палитре.

## Out of scope

Файлы вне allowlist; production слои вне `content-data`; AI пирата, защита/ПР, вероятность/баланс RNG, дальность/боезапас/TTL, реальные операторы, self-destruct, area damage, salvage, звуки, victory UI и общий refactor карты. Не менять существующие Board-статусы/requirements или commit/push в рамках реализации без соответствующего поручения.

## Invariants

- Gameplay HP, projectile lifecycle и collision принадлежат Engine; UI работает через immutable Contracts/session и shared Motion (CLAUDE.md, Architecture).
- Flight time — MotionTimeMs; real-time эффекты — monotonic UI clock (SimulationClock.Update; AuthoritativeSnapshot.MotionTimeMs). Пауза не продвигает мир, но session infrastructure работает (§52).
- `TargetObjectId` фиксируется в явной команде; current selection не заменяет цель полёта (PlayerCommand, строка7).
- Owner/self исключены из контактов; первый contact применяется один раз; chance100% не расходует RNG. Размеры5px/50px UI не являются collider world radius.
- Корабль Тетрарх определяется class identity, не картинкой; неизвестные свойства остаются masked. Legacy defaults не дают скрытой новой боевой способности.
- Сохраняются независимые worktree изменения. При ошибке baseline тестов записать конкретный результат, не объявлять весь suite зелёным и не править чужой fixture вне scope.

## Tests

Имена ниже — планируемые тесты, а не уже существующие результаты:

- [ ] `Combat_palette_contains_all_eight_color_roles`
- [ ] `Combat_palette_is_in_output_data_directory`

Из корня `D:/DeepSpaceSaga/DSS` (PowerShell):

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~Combat_palette_contains_all_eight_color_roles|FullyQualifiedName~Combat_palette_is_in_output_data_directory'
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Client.Tests/CombatPaletteContentTests.cs
git diff --check
```

Проверить реальные loader и output копии JSON, не только синтаксис. Tests изменяют временные копии данных, не исходный пользовательский контент.

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

