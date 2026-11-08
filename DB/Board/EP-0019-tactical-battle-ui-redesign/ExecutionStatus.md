# EP-0019 — итог исполнения 2026-11-08

Реализованы все 7 историй и 21 тикет. Карта на весь экран, компактные карточки пилотов, панель пяти состояний, наведение, движение/назначение целей и воспроизведение залпа работают на настоящем session пути. Engine/Contracts/Motion, правила, RNG и Save20 не менялись.

Ветка: codex/ep-0019-tactical-battle-ui-redesign. Origin: https://github.com/SuhilovVitaly/DSS. База f9fcbd45; исходное дерево чистое. После каждого тикета отдельный commit/push и проверка remote SHA. Force push и переписывание опубликованной истории не применялись. Финальная документационная история выполнялась последней.

## Публикация

| Тикет | SHA | Push |
|---|---|---|
| EP-0019-US-0001-TK-0001 | 33ad15adacf7408d111476f7a6551301b91d9bbf | проверен в origin |
| EP-0019-US-0001-TK-0002 | 476bc097388d65b3e9b08f77b471e06b054fb02d | проверен в origin |
| EP-0019-US-0001-TK-0003 | 4161db5b3aa09efda03fcada03f0ee8aa6ec9e5f | проверен в origin |
| EP-0019-US-0002-TK-0001 | ed5cd0d898ff5d75bbe80b88d97b3f7d6b43980c | проверен в origin |
| EP-0019-US-0002-TK-0002 | 3154e8407f12dbdb617fe34fbb0a204b2e065047 | проверен в origin |
| EP-0019-US-0002-TK-0003 | c5d2e25c1bc2dd6f062d1aba16caa387d3a9f37c | проверен в origin |
| EP-0019-US-0002-TK-0004 | 7e994cd54495e92ea3ad3503fe3b4d66c264914d | проверен в origin |
| EP-0019-US-0003-TK-0001 | de0dff28d92a5bc66601795bab841f29d08c195f | проверен в origin |
| EP-0019-US-0003-TK-0002 | 49f367c599ff93ea29b8fc8a689723987cf00ea0 | проверен в origin |
| EP-0019-US-0003-TK-0003 | af987d41c64029fef71719eab69471196f9292d3 | проверен в origin |
| EP-0019-US-0004-TK-0001 | 021523b5921b52c0cd5c9ecd9c58fa8468a543c6 | проверен в origin |
| EP-0019-US-0004-TK-0002 | 5ef990e76f8a6b862eff3a8d1d7fad3cdf251bd9 | проверен в origin |
| EP-0019-US-0004-TK-0003 | 5e010aa54de610be5998604e4848efcd47fd7823 | проверен в origin |
| EP-0019-US-0004-TK-0004 | 6daf9bd77dc11376e369fda74b8ad7962632cf0f | проверен в origin |
| EP-0019-US-0004-TK-0005 | e957620e1a6906397143ddb75a50ab38e71213b2 | проверен в origin |
| EP-0019-US-0005-TK-0001 | 3daacce24136395391a6d17b79d9b16049c48310 | проверен в origin |
| EP-0019-US-0005-TK-0002 | 45294f90261efd7706aedc8ba51390e2a4b7c248 | проверен в origin |
| EP-0019-US-0005-TK-0003 | 1c037f5af94e64878de3828b41db1e0d05437ab3 | проверен в origin |
| EP-0019-US-0006-TK-0001 | 44c0102b472ec618077bf8de96e4e555f2a9e86a | проверен в origin |
| EP-0019-US-0006-TK-0002 | 86d7567317f6188a4257233a064c8ddc8e44f663 | проверен в origin |
| EP-0019-US-0007-TK-0001 | 1b65b7db8ddee1f3693604700021c696ad9ba81b | проверен в origin |

Документационный SHA 1b65b7db8ddee1f3693604700021c696ad9ba81b разрешён через Git после создания коммита и подтверждён в origin. Таблица всех21 тикетов полностью заполнена; эта отдельная запись публикации не переписывает опубликованную историю. Начальные незавершённые результаты — [ExecutionHistory](ExecutionHistory-20261108-start.md).

## Критерии и evidence

Общий корень: D:/DeepSpaceSaga/artifacts/validation/ep-0019/20261108-resume/. Перетикетные каталоги us1-tk3, us2-tk1…tk4, us3-tk1…tk3, us4-tk1…tk5, us5-tk1…tk3, us6-tk1 содержат tests.log/TRX и форматирование; первые два тикета — предыдущий 20261108-setup, как записано в карточках.

| AC | Подтверждение |
|---|---|
| US1-AC1 | Явные SKColor[] в двух градиентах; solution build на установленном SDK10.0.400 PASS. SDK8 NOT RUN (не установлен). |
| US1-AC2 | BattleTypographyTests 7/7: оба SHA, глифы RU/EN, нейтральные флаги и точные метрики. Весь новый активный UI использует BattleDesign/TextPaint. |
| US1-AC3–AC5 | Реальные seven-scene session routes, recovered reference, сначала Skip вида, затем все проверки включены. |
| US2-AC1–AC5 | OverlayShell, PhaseCamera, BattleDesign, OverlayLocalization: масштаб/ввод, формулы камеры, токены и parity ключей. |
| US3-AC1–AC5 | PilotPortraitCatalog, PilotCardModel, PilotCardColumn: портреты/fallback, значения всех фаз, Mind100, выбор без команды и растровые полосы. |
| US4-AC1–AC5 | PhasePanel/MoveInput/FireInput/Salvo/Outcome: геометрия, sliders, допустимые цели, timing/replay без повторного Fire, исход/возврат/New battle. |
| US5-AC1–AC5 | MapDesign/Movement/Fire + сцены: маркеры/подписи/edges, актуальные Paths, сектор/стрелки, снаряд62%, выноски, отсутствующие старые слои. |
| US6-AC1–AC3 | HoverCardTests; Ep0019ViewTests 29/29 (data + 7 layout + 7 text + 7 probes + 7 regression), без Skip. |
| US7-AC1–AC3 | Актуальные канонические/внешние документы, [инвентаризация](DocumentationImpact.md), проверка ссылок/статусов/публикации и offline Graphify. |

## Итоговые проверки

- Client полный текущий набор: client-published-final/focused.trx — 2163/2163 PASS, без Skip, 1m35s. Исправлена нестабильность новых тестов: thread-local locale создаётся после awaited fixture, не переносится между потоками.
- После последнего UI-review: tactical-final/focused.trx — 307/307 PASS. acceptance-final/focused.trx — 34/34 PASS (данные, все семь сцен/снимков, hover, токены). Ограничение дерева каждого тестового процесса — 300000ms.
- Снимки: начальное production capture после независимых проб, решение записано в Documentation.md. Regression thresholds не менялись: ΔRGB40, общий0.5%, block16×16 максимум25%; итоговые семь сравнений PASS. Рендеры, layout.json, probes.json, regression.json: acceptance-final/ep0019/<scene>/.
- Scoped format и diff-check выполнены; общая итоговая сборка и проверка ссылок записываются в final-build.log / links.json. Исходные MSB9008 (архивная отсутствующая ModApi) отделены от ошибок; ошибок компиляции нет.
- Native Windows/OpenGL: native-1920.json PASS (1920×1080, 312 кадров), native-secondary-final.json PASS (1280×720, 318 кадров). Intel Arc140V. Последний прогон — на другом, правом мониторе, фактическая позиция3440,0 и selected-monitor-position=true. Проверены реальный GPU capture, мышь/клавиши через router, replay, New battle после итога, возврат камеры/мира и QuickSave/QuickLoad. Это scripted input, не физическое нажатие человека.
- Self-review выполнен по владению данными, pending/receipt, времени анимации, viewport/input, допуску урона и Save/Load. Независимое review не заявляется. После поздних поправок пройдены затронутые проверки.

## Решения восстановления и миграции тестов

Изначальные font assets и battle-data-seed1.txt отсутствовали. TTF восстановлены из Ubuntu2.37-2build1 с точным совпадением исходных SHA. Reference восстановлен до изменения UI на неизменённых Engine/Contracts/TacticalBattle content относительно8c6f4e2a; известные числа проверены literal assertions DesignSpec. Это recovered baseline, а не найденный оригинал. Допуски и нормативный вид не подгонялись.

Проверки старых вкладок/toolbar/флажков/выпадающих списков, medical-cross, enemy forecast, world-aligned5×5 grid и прежних footprints заменены проверками нового контракта; отсутствующие элементы не сохраняются ради старых тестов. Сохранены независимые timeline/impact/replay, session identity, инициативы и возвращения мира. Marker-only disabled alpha из допущения тикета снята в пользу нормативных §8.2/§12.4: в Outcome действительно есть disabled Вера, но проба требует Cyan. Роли используют корректный kebab-case ui.pilot-card.

Q-01/Q-02/Q-04 исполнены по записанным вариантам по умолчанию; Q-03 — по §6.6 как техническое допущение. Поручение исполнить draft не объявляется художественным одобрением пользователя.

## Ограничения и уборка

macOS/Ubuntu и SDK8 NOT RUN: недоступны. Платформенная нейтральность настроек/метрик проверена на Windows; межплатформенная raster parity не заявляется. Human/art/physical-input acceptance остаётся открытой отдельно от реализации. Сознание100 — placeholder, журнал/прогноз/Стороны и ИИ-всем не возвращены в UI; для их развития нужен отдельный scope. Баланс боя и native80FPS этим эпиком не подтверждаются.

Task-owned временные scripts, packages, caches, slots и промежуточные результаты удаляются после завершения процессов; остаётся только evidence карточек/этого отчёта. cleanup.txt перечисляет итоговую уборку и проверки корней. В репозитории/корне workspace новые временные каталоги не создавались. Дополнительный worktree не создавался. Документы не содержат runtime dumps, PNG, JSON или TRX.

### Итоговые уточнения проверки

Дополнительно опубликован исправляющий test-коммит US-0006-TK-0002: OverlayBoundaryTests 2/2 (восемь карточек до y697, missing portrait initials, широкий viewport). Первый Graphify rebuild: 782 code / 697 Markdown, 21154 nodes, 56949 edges, 700 communities, zero missing sources/dangling edges/self-loops after audit. Один AST parser warning в неизменённом ModulePanelLocalizationTests.cs; не C# ошибка. Свежая семантика — 16 nodes/16edges/1hyperedge из10 источников, hashes проверены, token cost unmeasured. Граф обновляется повторно после этой финальной синхронизации карточек и нового boundary test; точный итог — graph/verification.json.

Финальный граф после синхронизации: 783 code / 697 Markdown, 21181 nodes, 56986 edges, 705 communities; missing sources/out-of-range/dangling/self-loops = 0 после явно описанного audit pruning. Все10 semantic source hashes и полный source-manifest проверяются против итогового дерева. Graph token cost unmeasured; один pre-existing AST warning сохранён. Последняя solution build PASS, 3 baseline MSB9008, 0 errors. Cleanup выполнен; monitor enumeration и размещение подтверждены также Interface.log.
