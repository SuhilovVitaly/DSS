# DSS Graphify — актуальная перестройка EP-0010

## Current EP-0012 contract — 2026-10-21

Current offline rebuild is EP12 at src/graphify-out/rebuild.py, over fresh .cs/.py AST, Documentation/related Board Markdown structure and explicitly selective current EP12 host-agent semantics. Retired code paths in the dated graph reports below are historical references, not current nodes. See final PointCombatDocumentation inventory and current GRAPH_REPORT/counts/verification. Input hashes, missing paths/line locations, edge endpoints and token-usage limits are checked; no game or external API is invoked. Graph navigation is not acceptance. [Point-aim combat contract](../04-Engineering/PointAimCombat.md).

## Historical record (superseded combat rules)

All combat formulas, saves, commands, conclusions and permissions below describe their original dated stage. They are not the current writer/runtime or fresh acceptance. Non-combat invariants remain applicable.

## Текущая перестройка EP-0011 — 2026-10-18

Ниже сохранены датированные предыдущие перестройки. Текущий граф: 13620 узлов, 47142 связи, 419 сообществ; корпус 735 code / 143 Markdown. Добавлены новые NPC observations и Board EP-0011, обновлены реальные source locations. HTML агрегирован (более 5000 узлов); полный JSON доступен. [Контракт](../04-Engineering/NpcObservedTrajectories.md) · [текущий execution/evidence](../../Board/EP-0011-short-fixes/ImplementationStatus.md) · [проверка хешей и целостности](../../Board/EP-0011-short-fixes/evidence/graph-check.json).

Перестройка src/graphify-out/rebuild.py выполнена offline без --force; shrink guard сохранён. Structural AST/Markdown coverage обновлено, selective curated semantics остаются EP-0010; новая NPC документация не объявляется полной semantic extraction. 1027 unresolved AST endpoint edges явно исключены, 2606 reference-only symbols не имеют локальной declaration location; diagnostics и extraction.json сохраняют ограничения. Missing source files, invalid locations, dangling edges и self-loops: 0. Все 878 input SHA256 и artifact hashes проверены. Token usage unmeasured; нулевая стоимость не заявляется.

Pre-export HEAD: 246987bb7c904f4dc779512e8f5ff42b0ff51061. Source hashes описывают итоговое рабочее дерево документационного тикета. Generated execution report/evidence и self-referential tooling index исключены из source hashes; это не доказательство игрового поведения/native/FPS. Native/human OPEN.

2026-10-16. Текущий output: `src/graphify-out`. [Исполнение/evidence](../../Board/EP-0010-combat-reports/ImplementationStatus.md), [контракт докладов](../04-Engineering/CombatReports.md). Graphify — навигация, не доказательство runtime, детерминизма, баланса, human/native/FPS acceptance. Самостоятельные игровые/native/performance запуски запрещены пользователем. Builder выполняет только offline Python/AST/Markdown, не запускает игру и не обращается к внешним API.

| Область | Проверенный результат |
|---|---|
| Входы SHA256 |844:713 .cs/.py +131 Markdown |
| Markdown scope |Documentation и Board/EP-0010; headings/relative links +selective EP10 mechanisms |
| Граф |13087 nodes /45407 edges /416 communities |
| Полная extraction |13800 nodes /47706 edges; направления/исходные relations сохранены |
| HTML |Агрегированный обзор416 communities /3543cross-community edges; полный JSON содержит symbols |
| Hash/location integrity |844 hashes verified;0missing source files,0invalid line locations,0dangling edges,0self-loops |
| Отдельный audit |992 unresolved AST edges pruned;2494external/reference nodes без declaration location |
| Undirected export |1261relations с совпавшими endpoints объединены; extraction.json сохраняет исходные relations |

Свежая AST extraction выполнена последовательно (`parallel=False`) из-за Windows process pool.14 вручную подтверждённых EP10 mechanisms и архитектурные INFERRED edges имеют source evidence; это выборочное семантическое покрытие, не полная обработка всех документов или изображений. Непроверенная историческая семантика EP1–4 намеренно заменена; уменьшение относительно старого14935-node графа явно разрешено `--force`, не скрыто shrink guard. Текущий AST включает весь выбранный современный код, включая EP8/9. Host-agent token usage не измерялся (`null`), отсутствие внешнего API не означает нулевую стоимость.

`source-manifest.json` / `input-evidence.json` содержат реальные working-tree SHA256. Записанный commit a1a4dd1f — pre-export HEAD, не утверждение о committed состоянии final docs. `verification.json` проверяет итоговые artifacts/hashes. Native Graphify manifest отмечает только AST; selective Markdown не штампуется как full semantic extraction. Исключены generated output/cache/evidence и self-referential Documentation/README.md, этот tooling document, исторический AiMapGraphify.md, EP10/ImplementationStatus.md; список/reasons хранится в manifests.

Воспроизведение без запуска игры:

```powershell
Set-Location D:/DeepSpaceSaga/DSS
& D:/DeepSpaceSaga/.graphify-venv/Scripts/python.exe src/graphify-out/rebuild.py --force
& D:/DeepSpaceSaga/.graphify-venv/Scripts/python.exe src/graphify-out/rebuild.py --dry-run
& D:/DeepSpaceSaga/.graphify-venv/Scripts/python.exe -m graphify query "CombatReportInbox" --graph src/graphify-out/graph.json --budget 1200
```

`--force` нужен только для осознанной замены состава семантики; перед повторной заменой прочитать semantic-coverage.json. `--prepare-ast` прогревает cache. Изменение source evidence token без обновления curated mechanism блокирует builder. При новом коде/документах нужна свежая extraction, а не повторная выдача старых hashes. Изображения и binary evidence не индексируются. Если граф устарел, runtime код и каноническая документация имеют приоритет.

## Историческая перестройка2026-10-10 — superseded current graph выше

# Graphify: торговля и Солнечная система — 2026-10-10

Граф основной копии: [graph.json](../../src/graphify-out/graph.json), [HTML](../../src/graphify-out/graph.html), [отчёт](../../src/graphify-out/GRAPH_REPORT.md), [проверка](../../src/graphify-out/verification.json). Исходная ветка `base-fight / 02c89d4` плюс исправления F01–F05 и локальная интеграция EP-0004. Это навигация по исходникам и документам, не свидетельство прохождения AC.

AST обновлён для всех C# файлов src/tests/tools (без bin/obj/cache). Документы и Board EP-0001–0004/EP-0007 имеют структурные узлы и ссылки; семантика неизменённых документов переиспользуется только при совпадении прежнего SHA256. Сорок три ключевых документа обработаны тремя Graphify-агентами; выборочное покрытие больших requirements/исторических приложений явно указано в semantic-coverage.json. Остальные новые/изменённые документы имеют структурное покрытие, без выдуманной полной семантической обработки. Непроверенные старые semantic edges удалены.

Корпус: 907 файлов (584 C#, 323 Markdown): tests313, Board246, src247, Documentation77, tools24. Граф: **14935 узлов, 37184 связей, 517 сообществ**. Превышены пороги 500 файлов / 5000 узлов; полный JSON сохранён, HTML автоматически показывает агрегированный обзор 517 сообществ, а не все отдельные символы. Полные input hashes и фактические счётчики в input-evidence.json / counts.json. Бинарные медиа, TestResults, raw JSON evidence, игровые изображения и сторонние рабочие копии не индексируются. Этот файл исключён из собственного hash-набора.

EP-0004 интегрирован в основную рабочую копию и включён в AST/документальный граф вместе с исправлениями. Полный исторический граф этой поставки остаётся в `D:/DeepSpaceSaga/DSS-EP-0004/graphify-out`; это отдельный граф с собственной базой и приёмкой.

Граф строится graphifyy через `D:/DeepSpaceSaga/.graphify-venv/Scripts/python.exe`, последовательно для AST из-за Windows process-pool ограничений. `rebuild.py` в output проверяет manifest hashes и повторяет clustering/export по сохранённой extraction; при изменении исходников нужна новая extraction. Confidence/locations сохраняются. Внешние AST reference nodes без declaration location и объединение нескольких отношений в undirected graph отмечены в diagnostics; исходные направления остаются в extraction.json. Token usage host-агентов не измерен; отсутствие внешнего API не означает нулевую стоимость.

```powershell
Set-Location D:/DeepSpaceSaga/DSS
& D:/DeepSpaceSaga/.graphify-venv/Scripts/python.exe -m graphify query "TradingAndSolarSystem" --graph src/graphify-out/graph.json --budget 1200
```

Граф проверен: нет dangling/self-loop edges, отсутствующих source files или выходящих за границы locations. 777 unresolved edges вынесены в pruned-unresolved-edges.json; 2118 reference nodes не имеют declaration location и не считаются доказанными символами.

Прежние output artifacts сохранены вне репозитория в `D:/DeepSpaceSaga/review-ep1-4-work/previous-graphify-out`, чтобы старые counts/traces не выдавались за результат новой сборки.


### Final graph integrity — 2026-10-21

Fresh offline export:792code +311Markdown =1103source SHA256 verified;16038nodes/50778edges/514communities. All21artifact hashes verified;0missing sources/invalid line locations/dangling edges/self-loops. Source canonicalization produced794self-loops, explicitly audited/pruned from derived graph;1112unresolved AST edges and2886reference-only symbols remain explicit limitations. Selective EP12 semantics:21inputs/3host-agent chunks,52nodes/58edges, token usage null/unmeasured. Actual CLI OwnProjectileSnapshot query succeeds (131BFS nodes, explicit700token truncation). Canonical/current Board/tool docs:1324local links and65anchors verified. JSON has full graph; HTML is an aggregated view. Generated audit/status/navigation documents are excluded from graph input hashes to avoid circular results; graph navigation does not establish runtime or human acceptance.
