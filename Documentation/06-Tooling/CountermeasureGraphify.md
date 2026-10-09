# EP-0007 — полный rebuild Graphify

Выполнен 2026-10-04T17:56:29.165048+00:00. Исходная база: `5fa0595816569b4c8aefc198fdc6a3856dedeabb` + незакоммиченный рабочий набор. Это навигационный граф исходников и документов; он не подтверждает прохождение игровых проверок.

## Корпус и свежесть

Фактический корпус: **619 файлов: 501 code, 118 document, 0 media**. Распределение: Board: 47, Documentation: 91, src: 254, tests: 227.

Включены src, tests, актуальная Documentation и только Board/EP-0007-countermeasure-combat. Исключены Images целиком, все изображения/аудио/видео/шрифты/бинарные файлы, bin, obj, .git, node_modules, caches и generated graph directories. Сам этот run report исключён, чтобы не создавать самоссылочный hash. Detect: skipped_sensitive=0, walk_errors=0. Неподдерживаемые .trx/.log/.tmp не являются входами; список сохранён в corpus.json.

Превышение порога 500 файлов явно зафиксировано; состав уже разрешён пользователем для всего эпика, сужения не было. Все118 документов прочитаны семантическими агентами в6чанках; AST всех code files извлечён заново. Изображения не анализировались.

Совокупный SHA-256 отсортированного mapping path→SHA-256: `47b1096962ae00ae6636222fed75aa5da141f4c1fadf2789334723b4d70e6c44`.

[Полные hashes, HEAD, время и dirty paths](../../src/graphify-out/input-evidence.json); [manifest](../../src/graphify-out/manifest.json); [проверка совпадения входов/выходов](../../src/graphify-out/verification.json). Пути узлов нормализованы относительно DSS; staging paths отсутствуют. После финального snapshot повторно проверены hashes всех входов и HEAD.

## Инструментарий и воспроизведение

Установленный graphifyy **0.9.66**, interpreter `D:/DeepSpaceSaga/.graphify-venv/Scripts/python.exe`; CLI `D:/DeepSpaceSaga/.graphify-venv/Scripts/graphify.exe --help` и Python API signatures проверены перед запуском. Использован полный pipeline, без cluster-only и без внешнего LLM API/ключей.

Фактические скрипты локального запуска сохранены в `D:/DeepSpaceSaga/ep7-validation/`: `graphify-prepare.py`, `graphify-ast-final.py`, `graphify-build.py`, `graphify-export.py`. Основные вызовы:

```python
detect(ROOT, cache_root=STAGE, gitignore=False)
extract(code_files, cache_root=fresh_cache, root=ROOT, parallel=False)
# semantic: extraction-spec.md, 6 chunks, all 118 docs; source locations retained
save_semantic_cache(..., root=ROOT, prompt_file=SPEC, cache_root=STAGE)
build_from_json(extraction, root=ROOT, directed=False)
diagnose_extraction(extraction, directed=False, root=ROOT)
cluster(G); score_all(G, communities)
to_json(G, communities, graph_path, built_at_commit=HEAD, community_labels=labels)
to_html(G, communities, html_path, community_labels=labels, node_limit=5000)
save_manifest(stamped_files, manifest_path, root=ROOT, scan_corpus=full_corpus)
```

Первый multiprocessing вызов на Windows сообщил BrokenProcessPool из-за отсутствующего main guard и выполнил последовательный fallback. Финальный проход выполнен явно с parallel=False и новым пустым cache; никаких результатов не потеряно. Семантический prompt совпадает с актуальным skill extraction-spec, cache attribution сохранён.

**Стоимость токенов:** API агентов не отдаёт usage; фактическое число неизвестно. В cost.json стоят null; нули в универсальном Graphify-шаблоне обозначены как placeholders, а не как измеренная бесплатная обработка. Внешних LLM API вызовов:0.

## Результаты и ограничения

| Показатель | Значение |
|---|---:|
| Старый graph nodes | 3018 |
| Новый graph nodes | 8484 |
| Новый graph edges | 25854 |
| Communities | 260 |
| AST raw nodes / edges | 8037 / 25439 |
| Semantic raw nodes / edges | 424 / 513 |
| Дополнительные документные/навигационные связи | 1322 |
| Повторяющиеся AST node IDs (до merge) | 65 |
| Raw dangling endpoint edges | 530 |
| Raw missing endpoints | 0 |
| Raw exact duplicate edges | 0 |
| Raw self loops | 0 |
| Directed same-endpoint collapse | 1347 |
| Undirected same-endpoint collapse | 1416 |
| Unresolved reference placeholders | 26 |
| AST reference/type stubs без source declaration | 1649 |
| Отсутствующие endpoints в итоговом JSON | 0 |
| Несуществующие непустые source_file | 0 |

**Graph health warning сохраняется.** Raw dangling references включают внешние namespace System.Diagnostics, System.Reflection, System.Text.Json, а также неразрешённый локальный DefenseLaunch. Часть локальных ссылок библиотека разрешает при build; для остальных создаёт endpoint placeholders (например System.* и BasicCombatUiFlowTests.Fixture); они явно помечены `unresolved_reference_placeholder`, не имеют выдуманных source paths и не считаются подтверждёнными code symbols. Для сигнатуры LaunchCountermeasure→DefenseLaunch отдельно добавлена проверенная source-reference связь на настоящий узел, исходное предупреждение не скрыто.

AST также содержит reference/type stubs без проверенной декларации (например Fact, SKPaint, ImmutableArray). Они помечены `unlocated_ast_reference`; отсутствие source_file не означает наличие реализации в репозитории. Graph — обычный недирективный networkx.Graph: несколько references/calls между одной парой узлов схлопываются. Например TacticalMapDepthRenderer→SKPaint содержит25 source locations, CommandsPanel→SKPaint24, TradeLayout→SKRect24. Данные о каждом исходном ребре сохранены в [extraction.json](../../src/graphify-out/extraction.json), реальные диагностики — в [diagnostics.json](../../src/graphify-out/diagnostics.json). Навигационные contains_concept/documents edges означают принадлежность или явное упоминание, а не runtime вызов. AST calls с confidence INFERRED остаются выводом парсера и требуют чтения исходника.

Пустой граф запрещён assert. Новый node count выше старого, shrink guard не обходился; force не использовался. Старый набор сохранён вне git, в `D:/DeepSpaceSaga/ep7-validation/graphify-before-EP7/`.

HTML содержит **260 community nodes**, поскольку исходный граф >5000 узлов. Полный уровень symbols остаётся в graph.json/CLI. Браузерная проверка: Codex in-app browser: финальный reload, canvas отрисован, 260 communities / 1869 aggregate edges; поиск Countermeasure показывает State Data, Snapshot, Bootstrap Tests, Save Schema Tests, Content Tests.. HTML использует pinned vis-network9.1.6 CDN; для повторного отображения нужен доступ к нему. JSON/CLI от CDN не зависят.

Числовые cohesion scores каждого сообщества: [community-cohesion.json](../../src/graphify-out/community-cohesion.json). Минимум 0.017418509280006763, максимум 1.0.

## Пять проверенных трасс

Проверка выполнялась NetworkX по конкретным node IDs и существующим ребрам. Ниже ↔ означает навигационную связь; направление вызова, confidence и точные evidence всех ребер сохранены в [verification-traces.json](../../src/graphify-out/verification-traces.json).

### operator-rating-launch

- `.ResolveWeaponOperator()` ↔ `.NextDefenseLaunch()` — calls, INFERRED; [SimulationEngine.Countermeasures.cs L39](../../src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs).
- `.NextDefenseLaunch()` ↔ `.ChanceTenths()` — calls, EXTRACTED; [SimulationEngine.Countermeasures.cs L48](../../src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs).
- `.NextDefenseLaunch()` ↔ `DefenseLaunch` — calls, EXTRACTED; [SimulationEngine.Countermeasures.cs L64](../../src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs).
- `DefenseLaunch` ↔ `.LaunchCountermeasure()` — references, EXTRACTED; [SimulationEngine.Countermeasures.cs L73](../../src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs).
- Документное подтверждение: [CountermeasureCombat.md L36](../../Documentation/04-Engineering/CountermeasureCombat.md), реальное ребро `documents` к code symbol.

### scheduler-contact-RNG

- `.AdvanceCombatTo()` ↔ `.FindFirstIntercept()` — calls, INFERRED; [SimulationEngine.Combat.cs L130](../../src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs).
- `.FindFirstIntercept()` ↔ `.FirstContact()` — calls, EXTRACTED; [SimulationEngine.Countermeasures.cs L154](../../src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs).
- `.AdvanceCombatTo()` ↔ `.ResolveFirstIntercept()` — calls, INFERRED; [SimulationEngine.Combat.cs L146](../../src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs).
- `.ResolveFirstIntercept()` ↔ `.NextRoll()` — calls, INFERRED; [SimulationEngine.Countermeasures.cs L164](../../src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs).
- Документное подтверждение: [EngineRequirements.md L5398](../../Documentation/01-Requirements/EngineRequirements.md), реальное ребро `documents` к code symbol.
- Документное подтверждение: [CountermeasureCombat.md L40](../../Documentation/04-Engineering/CountermeasureCombat.md), реальное ребро `documents` к code symbol.

### phase-reload

- `CountermeasurePhase` ↔ `.ResolveFirstIntercept()` — references, EXTRACTED; [SimulationEngine.Countermeasures.cs L192](../../src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs).
- `.ResolveFirstIntercept()` ↔ `.RemoveCountermeasure()` — calls, EXTRACTED; [SimulationEngine.Countermeasures.cs L171](../../src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs).
- `.RemoveCountermeasure()` ↔ `DefenseState` — references, EXTRACTED; [SimulationEngine.Countermeasures.cs L264](../../src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs).
- `DefenseState` ↔ `.CompleteDefenseReloads()` — references, EXTRACTED; [SimulationEngine.Countermeasures.cs L114](../../src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs).
- Документное подтверждение: [CountermeasureCombat.md L38](../../Documentation/04-Engineering/CountermeasureCombat.md), реальное ребро `documents` к code symbol.

### save-restore

- `CountermeasureStateData` ↔ `.StageDefenseRestore()` — references, EXTRACTED; [SimulationEngine.CombatPersistence.cs L19](../../src/DeepSpaceSaga.Engine/SimulationEngine.CombatPersistence.cs).
- `.StageDefenseRestore()` ↔ `.LoadScenario()` — calls, INFERRED; [SimulationEngine.cs L382](../../src/DeepSpaceSaga.Engine/SimulationEngine.cs).
- `.LoadScenario()` ↔ `.RestoreDefenseState()` — calls, INFERRED; [SimulationEngine.cs L431](../../src/DeepSpaceSaga.Engine/SimulationEngine.cs).
- Документное подтверждение: [CountermeasureCombat.md L60](../../Documentation/04-Engineering/CountermeasureCombat.md), реальное ребро `documents` к code symbol.

### snapshot-UI-journal

- `.BuildSnapshot()` ↔ `CombatJournalEntry` — references, EXTRACTED; [SimulationEngine.cs L693](../../src/DeepSpaceSaga.Engine/SimulationEngine.cs).
- `CombatJournalEntry` ↔ `.ReceiveJournal()` — references, EXTRACTED; [CombatEffectStore.cs L26](../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatEffectStore.cs).
- `.ReceiveJournal()` ↔ `.DrawCountermeasureResults()` — calls, EXTRACTED; [GameSessionScreen.Countermeasures.cs L113](../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs).
- `CombatJournalEntry` ↔ `CombatJournalPanel` — references, EXTRACTED; [CombatJournalPanel.cs L10](../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatJournalPanel.cs).
- Документное подтверждение: [CountermeasureCombat.md L54](../../Documentation/04-Engineering/CountermeasureCombat.md), реальное ребро `documents` к code symbol.

## Артефакты

- [graph.json](../../src/graphify-out/graph.json) — полный граф.
- [graph.html](../../src/graphify-out/graph.html) — интерактивная карта сообществ.
- [GRAPH_REPORT.md](../../src/graphify-out/GRAPH_REPORT.md) — hubs, cohesion, bridges, suggested queries.
- [corpus.json](../../src/graphify-out/corpus.json), [input-evidence.json](../../src/graphify-out/input-evidence.json), [verification.json](../../src/graphify-out/verification.json) — воспроизводимое происхождение.

Обновлены только Graphify configuration/generated artifacts и этот отчёт. Commit/push/облачная публикация не выполнялись. Нативная приёмка игры этим тикетом не подменяется.

Benchmark Graphify: эвристическая оценка 29.5x меньше токенов на типовой запрос (565600 naive / 19150 average query). Это модель стоимости из структуры графа, не измеренное использование токенов агентами; полный вывод сохранён в benchmark.txt.

## Последующая навигация

Этот документ и `src/graphify-out` описывают исторический EP-0007 corpus. EP-0005 выполняет новый rebuild в корневой `graphify-out`, включая текущие src/tests/Documentation и Board/EP-0005. Свежесть и ограничения нового запуска фиксируются в [EP-0005 graph ticket](../../Board/EP-0005-optimization/EP-0005-US-0003-documentation-and-graph/EP-0005-US-0003-TK-0002-graph-rebuild/EP-0005-US-0003-TK-0002-graph-rebuild.md); старые hashes/counts не являются evidence текущего checkout.

Ссылки на исторические `src/graphify-out` артефакты требуют исходного локального checkout: эта ignored папка не входит в изолированный EP-0005 worktree. Они сохранены как происхождение старого отчёта и отмечены в documentation-validation.json, не перенаправлены на другой corpus.
