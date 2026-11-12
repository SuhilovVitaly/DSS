# EP-0024 — исполнение

## Текущее продолжение 2026-11-12

Пользователь явно поручил восстановить манифест по UiSpec и продолжить все
доступные тикеты с готовыми PNG (D-12). Общая preflight-блокировка снята.
Только `EP-0024-US-0002-TK-0003-bake-tool` остаётся BLOCKED по исходникам.
Реализация ведётся в той же опубликованной ветке. Манифест реконструирован:
72 роли с явными вариантами состояний, 24 изображения; это не побайтовая
копия утраченного манифеста с заявленными в планировании 51 UI-ролью.
Новые проверки: `D:/DeepSpaceSaga/artifacts/validation/ep-0024/20261112-implementation-01/`.

## История: preflight до уточнения пользователя

**BLOCKED на preflight. Выполнено 0 из 26 implementation-тикетов.**
Пользователь разрешил исполнение всего эпика, актуализацию Board, commit и push.
Полномочия не требуют повторного согласования; препятствие — неполный комплект
исходных материалов. Отдельного продуктового решения или human approval здесь нет.

Ветка: `codex/ep-0024-battle-field-redesign`; remote:
`origin` (`https://github.com/SuhilovVitaly/DSS`). База получена через `git fetch origin`:
`d7895f73`, указанная в эпике. Создана обычная ветка в основном checkout;
worktree/копии репозитория не создавались. Посторонние изменения `VData` сохранены
и не включаются в коммит. Production, tests и текущие игровые контракты не менялись.

## Восстановление канонического Board

В `DB/Board/EP-0024-battle-field-redesign` отсутствовали 10 файлов, доступных
в переданной копии `VData/Board/EP-0024-battle-field-redesign`:

- `Documentation.md`, `Tickets.md`, `UiSpec.md`;
- story `EP-0024-US-0007-salvo-results`;
- tickets `US-0011-TK-0002-steel-frames`, `US-0012-TK-0001-slim-layout`,
  `US-0013-TK-0001-panel-frame`, `US-0013-TK-0003-panel-fire`;
- `Materials/README.md`, `Materials/Reference/README.md`.

Восстановлены только отсутствовавшие файлы; все существовавшие совпадали
с входной копией побайтно. Далее исправлены текущие notices о комплектности
и противоречие D-08/D-11 в Materials README. Исторический review log сохранён
как история, его результаты не считаются результатами этого запуска.
Состав: 13 историй, 26 тикетов. Последняя US-0010 расширена до раздела 6
EpicExecutionPrompt (полный impact inventory и Graphify); она не выполнена.

## Блокирующие материалы

В канонических Materials и указанной пользователем папке VData отсутствуют:

- `Materials/Assets/skin/skin.json`
- `Materials/Bake/ZzSpriteBake.cs`
- `Materials/Bake/ZzPixelBake.cs`
- `Materials/Bake/ZzTypedBake.cs`
- `Materials/Bake/ZzDetailBake.cs`
- `Materials/Bake/explosions.py`
- `Materials/Prototype/tabletop-prototype.patch`
- `Materials/Prototype/src/SkinShipSprites.cs`
- `Materials/Prototype/src/BattleMapRenderer.Templates.cs`
- `Materials/Prototype/src/BattleMapRenderer.FireTemplates.cs`
- `Materials/Prototype/src/BattleMapRenderer.PaintedClouds.cs`
- `Materials/Prototype/src/hooks.diff`
- `Materials/Prototype/src/ZzPixelSalvoCapture.cs`

Первый тикет `EP-0024-US-0001-TK-0001-pack-files` требует переноса готового
пакета без изменений. Манифест не передан; без него `TacticalBattleSkinCatalog`
пропускает каталог (ветка `FileNotFoundException` в `ReadManifests`), поэтому
наличие PNG не даёт выбираемый скин. Исходные генераторы обязательны для
`EP-0024-US-0002-TK-0003-bake-tool`: перенос существующих моделей и проверка
перезапекания не заменяются копированием готовых PNG. Прототип — справочный,
само его отсутствие не является отдельным запретом реализации по UiSpec.

Поиск имён `tabletop`, `ZzSpriteBake`, `ZzDetailBake`, `explosions` в workspace
не дал исходников. `git log --all` по запекателю, патчу и манифесту tabletop
после fetch не вернул коммитов. В `origin/main` комплект EP-0024 отсутствует.
В обеих папках `Bake` и `Prototype` находятся только README. Сетевой fetch
сначала не прошёл в sandbox (DNS), затем успешно выполнен с разрешённым доступом;
сеть не является оставшимся блокером.

## Проверки этого запуска

- PASS: все 26 ticket dependencies разрешаются, циклов нет.
- PASS: 279 локальных Markdown link targets существуют; `git diff --cached --check`
  чист после удаления двух пустых строк в конце импортированных документов.
- PASS: staged diff ограничен каноническим EP-0024; `src`, `tests`, `tools`
  и посторонние изменения `VData` не включены.
- PASS: 24 PNG пакета (фон, 13 листов, 4 эффекта, 6 UI-градиентов),
  3 410 656 пикселей, 1 133 780 байт. Размеры соответствуют заявленным.
- PASS: сигнатура PNG, CRC всех chunks и распаковка IDAT/DEFLATE всех 24 файлов;
  каждый файл побайтно совпадает с указанным пользователем материалом.
- Ресурсные числа ниже лимитов loader: 32 ресурса, 8 388 608 пикселей,
  32 МиБ на пакет. Это проверка инвентаря, не runtime-загрузка.
- NOT RUN: build, unit/regression, Skia decoding, snapshot, native UI,
  производительность облаков, независимый code review, Graphify rebuild.
  Реализация не начата, полного пакета и итогового кода нет.
- Игра не запускалась; monitor-placement gate не задействован.

Evidence: [MaterialAudit.json](D:/DeepSpaceSaga/artifacts/validation/ep-0024/20261112T160603Z-preflight/MaterialAudit.json).
Аудит выполнен встроенным Python без создания временного скрипта. JSON хранит
SHA-256 всех PNG, геометрию и зависимости. Он не доказывает визуальную приёмку.

## Порядок продолжения

1. Восстановить исходный `Materials/Assets/skin/skin.json` и `Materials/Bake/*`;
   приложить оставшиеся файлы прототипа для полного комплекта передачи.
2. Повторить проверку комплекта; прочитать оставшуюся обязательную документацию,
   все карточки и текущие исходники до production-правок.
3. Выполнять ticket order из Tickets.md: US-0001 → US-0002 → US-0003,
   US-0004, US-0005, US-0006 → US-0007, US-0008, US-0011 → US-0012 → US-0013;
   затем US-0009 и последней US-0010. Проверять реальные зависимости.
4. Каждый реализованный тикет — отдельный commit и немедленный push с проверкой.
   Подготовительный коммит Board не закрывает ни одного тикета.

## Ticket → SHA → push

| Ticket | Состояние | Implementation SHA | Push |
|---|---|---|---|
| [EP-0024-US-0001-TK-0001-pack-files](EP-0024-US-0001-skin-pack/EP-0024-US-0001-TK-0001-pack-files/EP-0024-US-0001-TK-0001-pack-files.md) | DONE (automated; см. карточку) | `b45631c5799857f80462151452f2ccb407f8d011` | PASS |
| [EP-0024-US-0001-TK-0002-tabletop-mode](EP-0024-US-0001-skin-pack/EP-0024-US-0001-TK-0002-tabletop-mode/EP-0024-US-0001-TK-0002-tabletop-mode.md) | DONE (automated; см. карточку) | `f5bf9d2790e0ea2acc6a9919294045f7d9bc9b97` | PASS |
| [EP-0024-US-0002-TK-0001-sprite-renderer](EP-0024-US-0002-ship-sprites/EP-0024-US-0002-TK-0001-sprite-renderer/EP-0024-US-0002-TK-0001-sprite-renderer.md) | DONE (automated; см. карточку) | `d2a5c8fb2d353816da51b0e32401e94ea7d19084` | PASS |
| [EP-0024-US-0002-TK-0002-sprite-geometry](EP-0024-US-0002-ship-sprites/EP-0024-US-0002-TK-0002-sprite-geometry/EP-0024-US-0002-TK-0002-sprite-geometry.md) | DONE (automated; см. карточку) | `c16e6340a56eee7b959efc5cbd41145a1a53a8b2` | PASS |
| [EP-0024-US-0002-TK-0003-bake-tool](EP-0024-US-0002-ship-sprites/EP-0024-US-0002-TK-0003-bake-tool/EP-0024-US-0002-TK-0003-bake-tool.md) | BLOCKED: source generators | — | — |
| [EP-0024-US-0003-TK-0001-plates](EP-0024-US-0003-counter-plates/EP-0024-US-0003-TK-0001-plates/EP-0024-US-0003-TK-0001-plates.md) | DONE (automated; см. карточку) | `2a7e0cf0b4c108fb6e59b25bdad21d61ac76678b` | PASS |
| [EP-0024-US-0004-TK-0001-clouds-render](EP-0024-US-0004-painted-clouds/EP-0024-US-0004-TK-0001-clouds-render/EP-0024-US-0004-TK-0001-clouds-render.md) | DONE (automated; см. карточку) | `546995b7657c06b8151bbf4da7f328b64d9db540` | PASS |
| [EP-0024-US-0004-TK-0002-clouds-cache](EP-0024-US-0004-painted-clouds/EP-0024-US-0004-TK-0002-clouds-cache/EP-0024-US-0004-TK-0002-clouds-cache.md) | IMPLEMENTED; performance gate OPEN | `dc9630da9401b86191acd1b9dd58a52affc29a6e` | PASS |
| [EP-0024-US-0005-TK-0001-templates](EP-0024-US-0005-move-templates/EP-0024-US-0005-TK-0001-templates/EP-0024-US-0005-TK-0001-templates.md) | DONE (automated; см. карточку) | `715e8d6177121ec5448e54cc8399d7726ca6a35e` | PASS |
| [EP-0024-US-0006-TK-0001-fire-render](EP-0024-US-0006-fire-templates/EP-0024-US-0006-TK-0001-fire-render/EP-0024-US-0006-TK-0001-fire-render.md) | DONE (automated; см. карточку) | `bae46a1a574e99eddbdbec216f74b6a72a8643b2` | PASS |
| [EP-0024-US-0006-TK-0002-badges](EP-0024-US-0006-fire-templates/EP-0024-US-0006-TK-0002-badges/EP-0024-US-0006-TK-0002-badges.md) | DONE (automated; см. карточку) | `5bde1a3a7e8a928bb79399830beab92de34974b6` | PASS |
| [EP-0024-US-0007-TK-0001-salvo](EP-0024-US-0007-salvo-results/EP-0024-US-0007-TK-0001-salvo/EP-0024-US-0007-TK-0001-salvo.md) | DONE (automated; см. карточку) | `0d39439bd2d90f1f5d07cd5588927e1fb2aa408f` | PASS |
| [EP-0024-US-0007-TK-0002-torpedo-fx](EP-0024-US-0007-salvo-results/EP-0024-US-0007-TK-0002-torpedo-fx/EP-0024-US-0007-TK-0002-torpedo-fx.md) | DONE: 58/58 | текущий commit | проверяется после commit |
| [EP-0024-US-0008-TK-0001-camera](EP-0024-US-0008-camera/EP-0024-US-0008-TK-0001-camera/EP-0024-US-0008-TK-0001-camera.md) | DONE (automated; см. карточку) | `e0cbaf3ead14975df9920f9501712cf14c5801ba` | PASS |
| [EP-0024-US-0009-TK-0001-snapshots](EP-0024-US-0009-view-regression/EP-0024-US-0009-TK-0001-snapshots/EP-0024-US-0009-TK-0001-snapshots.md) | NOT STARTED: dependencies | — | — |
| [EP-0024-US-0009-TK-0002-panel-snapshots](EP-0024-US-0009-view-regression/EP-0024-US-0009-TK-0002-panel-snapshots/EP-0024-US-0009-TK-0002-panel-snapshots.md) | NOT STARTED: dependencies | — | — |
| [EP-0024-US-0010-TK-0001-docs](EP-0024-US-0010-project-documentation/EP-0024-US-0010-TK-0001-docs/EP-0024-US-0010-TK-0001-docs.md) | NOT STARTED: dependencies | — | — |
| [EP-0024-US-0011-TK-0001-steel-roles](EP-0024-US-0011-steel-panels/EP-0024-US-0011-TK-0001-steel-roles/EP-0024-US-0011-TK-0001-steel-roles.md) | NOT STARTED: dependencies | — | — |
| [EP-0024-US-0011-TK-0002-steel-frames](EP-0024-US-0011-steel-panels/EP-0024-US-0011-TK-0002-steel-frames/EP-0024-US-0011-TK-0002-steel-frames.md) | NOT STARTED: dependencies | — | — |
| [EP-0024-US-0011-TK-0003-role-colours](EP-0024-US-0011-steel-panels/EP-0024-US-0011-TK-0003-role-colours/EP-0024-US-0011-TK-0003-role-colours.md) | NOT STARTED: dependencies | — | — |
| [EP-0024-US-0012-TK-0001-slim-layout](EP-0024-US-0012-slim-column/EP-0024-US-0012-TK-0001-slim-layout/EP-0024-US-0012-TK-0001-slim-layout.md) | NOT STARTED: dependencies | — | — |
| [EP-0024-US-0012-TK-0002-slim-card](EP-0024-US-0012-slim-column/EP-0024-US-0012-TK-0002-slim-card/EP-0024-US-0012-TK-0002-slim-card.md) | NOT STARTED: dependencies | — | — |
| [EP-0024-US-0013-TK-0001-panel-frame](EP-0024-US-0013-command-panel/EP-0024-US-0013-TK-0001-panel-frame/EP-0024-US-0013-TK-0001-panel-frame.md) | NOT STARTED: dependencies | — | — |
| [EP-0024-US-0013-TK-0002-panel-move](EP-0024-US-0013-command-panel/EP-0024-US-0013-TK-0002-panel-move/EP-0024-US-0013-TK-0002-panel-move.md) | NOT STARTED: dependencies | — | — |
| [EP-0024-US-0013-TK-0003-panel-fire](EP-0024-US-0013-command-panel/EP-0024-US-0013-TK-0003-panel-fire/EP-0024-US-0013-TK-0003-panel-fire.md) | NOT STARTED: dependencies | — | — |
| [EP-0024-US-0013-TK-0004-panel-salvo](EP-0024-US-0013-command-panel/EP-0024-US-0013-TK-0004-panel-salvo/EP-0024-US-0013-TK-0004-panel-salvo.md) | NOT STARTED: dependencies | — | — |

## Публикация и уборка

Подготовительный коммит содержит только канонический EP-0024, материалы и
честный статус блокировки. Его SHA проверяется через Git после создания;
собственный SHA не записывается заранее. Тикетных implementation-коммитов нет.
Единственный новый diagnostic output — указанный MaterialAudit.json, сохранённый
как evidence этого отчёта. Временные скрипты, probes, caches и task-процессы не
создавались; дополнительных task-каталогов в корнях workspace/repository нет.

### EP-0024-US-0001-TK-0002-tabletop-mode

Режим задаётся метрикой; листы и эффекты читаются из каталога с проверкой геометрии и таймингов, fallback сохранён. 43/43 PASS (us1-tk2); scoped format PASS.

### EP-0024-US-0002-TK-0001-sprite-renderer

Модели по типу/стороне/курсу, пиксельная сетка, тени, обломки и маркеры подключены в production. Classic и его 15 снимков проходят без обновления. 47/47 PASS (us2-tk1); scoped format PASS.

### EP-0024-US-0002-TK-0002-sprite-geometry

Tabletop использует увеличенные области моделей и наведения; ввод проверен вне render scope, Classic сохраняет прежние радиусы. 16/16 PASS (us2-tk2); scoped format PASS.

### EP-0024-US-0003-TK-0001-plates

Числовые плашки, порядок кандидатов и fallback, привязки, рамки потерь брони/корпуса реализованы; заменённые выноски отключены только в Tabletop. Проверены три фазы, Classic snapshots и положение плашки относительно модели. Отдельная Client build: 0 предупреждений/ошибок. 41/41 PASS (us3-tk1); scoped format PASS.

### EP-0024-US-0004-TK-0001-clouds-render

Реализованы слои пыли/жара по UiSpec с Perlin-масками и зерном. Проверены оттенки, прозрачность, повторяемость и сцены absorption/move-dust без legacy refinement. Сборка: 0 ошибок/предупреждений. Raster evidence: cloud-2-anomaly-absorption.png и cloud-1-anomaly-move-dust.png в run. Кэш и performance gate следуют отдельным тикетом. 12/12 PASS (us4-tk1); scoped format PASS.

### EP-0024-US-0004-TK-0002-clouds-cache

Кэш по view/scale/clumps, GPU-context path, освобождение экраном проверены. 12 ms gate НЕ закрыт: CPU raster 1627.97/1519.05/1558.23 ms; native GPU NOT RUN. Evidence: cloud-raster-timing.json. Проба центра уточнена на максимальный видимый сгусток: глобальный максимум вне кадра; цвет ближе к тону жара, чем фону. 7/7 PASS (us4-tk2); scoped format PASS.

### EP-0024-US-0005-TK-0001-templates

Ленты, зона досягаемости, скорость, призраки, скобки, RU/EN-подсказка и выноска пыли реализованы. Проверены две сцены, геометрия handles, отсутствие зависимости рендера движения от оружейного сектора и реальное редактирование приказа перетаскиванием. Значки Edge сохранены по A-11. Build: 0 ошибок/предупреждений. 15/15 PASS (us5-tk1); scoped format PASS.

### EP-0024-US-0006-TK-0001-fire-render

Сектор с затемнением, свои ленты, угрозы Incoming, основные/запасные прицелы и сохранённые отрезки поглощения реализованы. Проверены четыре сцены, геометрия целей, цвета и прежние FirePlanning/MapFire проверки. Build: 0 ошибок/предупреждений. 20/20 PASS (us6-tk1); scoped format PASS.

### EP-0024-US-0008-TK-0001-camera

Кадрирование своей группы/боевых участников с полями 200/110, исключением истории Paths, ключом Phase/Turn и удержанием полёта/результатов. Ручной вид сохраняется до нового боя. Проверены пределы кадра, цели/угрозы/последние выстрелы, вызов CaptureFrame вне skin scope и Classic interaction. Build: 0 ошибок/предупреждений. 4/4 PASS (us8-tk1); scoped format PASS.

### EP-0024-US-0006-TK-0002-badges

Значки угроз/своих/запасных целей рисуются после лент; реализованы кандидаты размещения и отдельные записи раскладки. После интеграции камеры Tabletop исходные assertions непересечения chance/плашек и значков проходят. Клик по прицелу выбирает штатную цель и показывает её реальный шанс. Build: 0 ошибок/предупреждений. 18/18 PASS (us6-tk2); scoped format PASS.

### EP-0024-US-0007-TK-0001-salvo

Ленты результатов, значки исходов и маркеры целей реализованы с таймингом ImpactAgeMs/PlaybackSkipped; выноски ловушки/поглощения заменены, отлетающие цифры сохранены. Проверены 0/300/500/1500 ms и три сцены результатов. Build: 0 ошибок/предупреждений. Следующий тикет заменяет полёт/взрыв и сдвигает маркеры на конец соответствующего эффекта. 32/32 PASS (us7-tk1); scoped format PASS.

### EP-0024-US-0007-TK-0002-torpedo-fx

Пиксельные торпеды, trail и эффекты удара/уничтожения/puff подключены; взрывы рисуются поверх моделей и плашек, маркеры ждут конца эффекта, skip не запускает FX. Проверены кадры 0/100/450/880/1000 ms, опциональность скина и Classic snapshots. Build: 0 ошибок/предупреждений. Сверены kill-100.png и kill-450.png с Reference/salvo/kill-sequence.png; kill-*.png — синтетический killed-shot для тайминга, не outcome реального боя. 58/58 PASS (us7-tk2); scoped format PASS.
