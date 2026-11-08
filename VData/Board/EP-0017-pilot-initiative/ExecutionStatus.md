# EP-0017 execution — 2026-11-07

Техническая реализация: 4 истории / 16 тикетов. Все 15 функциональных тикетов опубликованы отдельными коммитами. Последняя US4 синхронизирует текущие контракты всего проекта, Board и свежий Graphify. Полная человеческая приёмка эпика остаётся открытой: self-review и scripted native проверки не являются independent review, physical-input/product/balance approval. Решения пользователя и исходное планирование сохранены с датами.

Результат: инициатива материализуется один раз отдельным RNG, общий seeded rank разрешает обе стороны последовательно. Ранний урон/disable/jam лишает позднего выстрела; потерянная цель не заменяется. Immutable FireSteps передают хронологию; все Client поверхности допускают одинаковый шаг. Прогноз условный, Skip/replay не вызывают симуляцию. Export сохраняет initiative и строго валидирует source до записи. Save20 и исторические V4 source/reference не изменены.

## Publication

Remote: https://github.com/SuhilovVitaly/DSS; branch/upstream: codex/ep-0016-tactical-battle-skins / origin/codex/ep-0016-tactical-battle-skins. Existing confirmed upstream retained. No force push or history rewrite.

| Ticket | Full SHA | Push |
|---|---|---|
| EP-0017-US-0001-TK-0001 | `2b6ad70eeb403cab3bd0ee2c138d0da545755197` | verified ancestor of remote branch |
| EP-0017-US-0001-TK-0002 | `2fc7ea40720f5203bf9b6a7713e964a8b500654b` | verified ancestor of remote branch |
| EP-0017-US-0001-TK-0003 | `a82237365d0fbda579ddd93188f6aaa955b73342` | verified ancestor of remote branch |
| EP-0017-US-0001-TK-0004 | `5efd30f4e3fc6fa7cfd65827b6c6ab6e0e731428` | verified ancestor of remote branch |
| EP-0017-US-0001-TK-0005 | `02711dee0a4ee6ac749ed0945f8243a7bfcf3a69` | verified ancestor of remote branch |
| EP-0017-US-0002-TK-0001 | `ebb18cf4cfb88bfdb72da6a72acb4d477933119b` | verified ancestor of remote branch |
| EP-0017-US-0002-TK-0002 | `78290b5295ec95be5c3394385b48860f63aad73f` | verified ancestor of remote branch |
| EP-0017-US-0002-TK-0003 | `10307272e2b618da8549421b8442a65d844d1d96` | verified ancestor of remote branch |
| EP-0017-US-0002-TK-0004 | `bdd084321bce5635801b34c7723d8d1276a88f8a` | verified ancestor of remote branch |
| EP-0017-US-0002-TK-0005 | `ef450c3ec8908aef2d90ad96632f636844339a68` | verified ancestor of remote branch |
| EP-0017-US-0003-TK-0001 | `1c373bc52c0c9c0a7c89a3168c5b8d523459fda2` | verified ancestor of remote branch |
| EP-0017-US-0003-TK-0002 | `4766cab74d98abe877212e6e7ab210bcb080cbff` | verified ancestor of remote branch |
| EP-0017-US-0003-TK-0003 | `1d5c98203fe320d6cc8af5c496097836111b087a` | verified ancestor of remote branch |
| EP-0017-US-0003-TK-0004 | `290d8b9f71c8f9757440741ae7a4f049555631fe` | verified ancestor of remote branch |
| EP-0017-US-0003-TK-0005 | `adae2608d152c75273d313c9f8000f1253cc2599` | verified ancestor of remote branch |
| EP-0017-US-0004-TK-0001 | resolved after commit | [post-commit receipt](D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/20261107-docs/publication.json) |

Подготовительные Board commits: 35f57c0bb797bbf10be4a1e2d62c97d04ca33cd2 и 3c15f55b3592c3ab0b2c16db59d606d0bbca9fb2. Собственный SHA документационного коммита заранее не записывается: receipt после commit/push содержит HEAD, upstream и ls-remote SHA, список тикетов и проверки ancestry. Конечная публикация проверяется по нему.

## Automated validation

Evidence root: [D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative](D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative). Каждый процесс ограничен 300000 ms с завершением дерева. Логи команд stdout/stderr и TRX сохранены в соответствующих run-каталогах, а не в Documentation.

| Gate | Actual result | Evidence run |
|---|---|---|
| Runtime content/localization | 4/4 PASS | 20261107-tk0101-elevated |
| Contracts additive compatibility | 186/186 PASS | 20261107-tk0201 |
| Strict loader/world availability | 86/86 PASS | 20261107-tk0103 |
| Materialization/content sync | 11/11 PASS | 20261107-tk0104 |
| Pilot cards, rank, chronological session | focused 6/6, 12/12, 38/38 PASS | tk0105, tk0202, tk0204 |
| New initiative regression | focused 185/185; tactical regression 421/421 PASS | 20261107-tk0205 |
| Client full regression | 2070/2070 PASS | 20261107-tk0304/client-regression.trx |
| Export strict negative/read-only/base-field harness | PASS, Check 29 JSON PASS | 20261107-tk0305 |
| Sequential presentation/queue/surfaces/log | focused 8/8,25/25,33/33,25/25 PASS | tk0301…tk0304 |
| Series seeds1…1000 × five tactics | 5000/5000 exact direct/explicit parity PASS | [series summary](D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/20261107-tk0205/series-summary.md) |
| Release build/scoped format | PASS per functional ticket | respective run build/format logs |

Series signatures include seed, outcome, turn, losses, main RNG calls and SHA256 of full final state/RNG. Results are observations, not balance approval. Commander contains one simulated turn-cap Timeout, not a test-process timeout. Old simultaneous expectations were replaced with explicit manual expected cases, a limited independent oracle and new deterministic parity; oracle does not call production Salvo/CheckVictory and intentionally does not independently implement all combat math. The old mutual-shot defect was reproduced before the runtime fix (tk0203/before-runtime.trx), then new focused tests passed.

Typical reproduction from DSS (absolute results directory must exist):

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests -c Release --no-build --filter 'FullyQualifiedName~TacticalBattle&Category!=LongRunning' --results-directory <absolute-run> --logger trx
dotnet test tests/DeepSpaceSaga.Engine.Tests -c Release --no-build --filter 'FullyQualifiedName=DeepSpaceSaga.Engine.Tests.TacticalBattle.SeriesParityTests.Dogfight' --results-directory <absolute-run> --logger trx
dotnet test tests/DeepSpaceSaga.Client.Tests -c Release --no-build --results-directory <absolute-run> --logger trx
```

Run each series exact FQN separately (Dogfight, Raid, Defence, Balanced, Commander). Set DSS_VALIDATION_ROOT to the same absolute diagnostic run for signatures/fixtures. Current final docs checks/build and graph commands/results: [D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/20261107-docs](D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/20261107-docs), [documentation inventory](DocumentationImpact.md).

## Native validation

[13-report native matrix](D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/20261107-native/native-summary.md): Classic/contrast × scale1/1.5 × 1024×768/1280×720/1920×1080 (12 PASS), plus contrast-monitor2 PASS after user steering. Each report has 37 true checks and actual OpenGL framebuffer captures on Intel Arc140V. Captures referenced by JSON are retained. Scripted keyboard router/screen mouse callbacks cover setup, move, fire, playback/replay, outcome, return/world restoration, F5/F9 save/load after exit and second battle. These do not establish physical input, hardware audio, scanout 80FPS or human/product review. The controlled intermediate-damage semantics additionally have engine/client automated coverage. Small fire panels use existing scrolling.

After the user requested another monitor, isolated profiles selected monitor index1 (right, 1920×1080). Regular game settings were not changed. Native test windows exited; no further game launch is needed for documentation work.

## Review, coverage and residual acceptance

AC01 pilot schema/materialization/cards: US1; AC02…05 shared stable queue/immediate effects/skips/end: US2; AC06 all surfaces and chronological log: US3; AC07 determinism/identity/replay/world Save20: US2+US3; AC08 exporter/current docs: US3TK5+US4. Detailed per-story AC mappings remain in Tickets.md and cards. US4 AC01…04: inventory, current docs, publication/evidence, graph and cleanup.

Self-review checks ownership (Engine rules/RNG, Contracts data, Motion math, Client admitted projection), atomic invalid requests, fixed targets, stable tie order, no capture draws, immutable UnitsAfter, no duplicate action on replay/receipts, all surfaces at inclusive impact, zero-shot completion, live-state rejection recovery and world save boundary. No unresolved confirmed implementation defect was found in final self-review. This is not independent review.

NOT RUN: physical-input/hardware-audio acceptance (scripted native substitutes only its stated scope), human visual/product/balance acceptance and independent reviewer approval. They require their respective human judgment/equipment and remain open; technical done must not be read as full product acceptance. No new Linux native check or 80FPS claim. No runtime balancing added outside this epic.

Order correction: exporter US3TK5 executed after US1TK3 and before US1TK4 because ContentSync needs it; its obsolete UI dependency removed. All normative document work was performed last in US4. Scope expansions are recorded before edits in ticket cards. No worktrees created. Historical V4/standalone and unrelated simulator rules remain unchanged.

Graph/cleanup/link/hash verification results are recorded in DocumentationImpact; final receipt establishes publication and clean working tree.

Final docs gate: Release build 0 warnings/errors; scoped format/diff/local links/anchors PASS; Graphify 17699 nodes/51933 edges, 1249 source hashes and 11 semantic hashes verified; cleanup PASS. Details in DocumentationImpact and validation.json.
