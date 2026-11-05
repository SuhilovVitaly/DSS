# EP-0015 — статус исполнения

## Текущее состояние — 2026-11-05

Исполнение продолжается. Все четыре тикета US-0001 выполнены технически и проверены; 22/78 тикетов. Новый бой ещё не подключён, остальные 56 тикетов и завершающая документационная история остаются открытыми.

- Ветка `codex/ep-0015-tactical-carrier-battle`, remote `origin` = `https://github.com/SuhilovVitaly/DSS`.
- Отмена запрета GitHub зафиксирована служебным коммитом `22bb633145d479391b985eb73d148bb7cad7de94`, push и remote SHA проверены.
- Принятые уточнения D-USER-14/15: исходники — `VData/Planning/V4.0` Markdown; JSON создаются по таблицам; Python-симулятор не нужен. Служебный коммит `06c5fc69032de5b7b7fe94d19079b5284998846c`, push и remote SHA проверены. Прежний preflight-блокер отсутствующих исходников снят этими указаниями.
- История US-0017 уже существует, зависит от всех функциональных историй и выполняется последней.
- Linux route работает: GitHub Actions run 42086320971 PASS на Ubuntu 24.04 / CPython 3.12.3 / glibc 2.39. Это bootstrap/export + текущие Motion/content tests; геометрия дополнительно проверена на Windows/Linux в PlatformValidation.md; core battle/AI реализован и проверен 90 эталонами Windows/Linux; сессия/UI ещё не подключены.

## Проверки

[ContentValidation.md](ContentValidation.md): build PASS (0 warnings/errors), 5402/5402 теста в шести проектах, scoped format и diff check PASS. Длительные Engine/Economy corpora проверены полными разделёнными наборами по точным ID, без ослабления assertions. Native/input, полная battle parity и приёмка боя пока NOT RUN.

Продолжение: US-0005 TK-0005. US-0001 backend завершён; Client error presentation из AC-03 остаётся в US-0007.

Reference repair: service fdc52ee5ebd1b16f398e3204a24fe4ee4d119e92 → Linux run 42090979851 PASS → schema-2 JSON imported. Numeric expectations unchanged; rearHits name corrected per Combat §7.1. Final repair SHA 0497bcbbc2fe748c73567690450eacc2aaaaf2b2, remote verified.

| Тикет | Коммит | Push | Remote | Проверки |
|---|---|---|---|---|
| EP-0015-US-0001-TK-0001-content-data | 8015f9852f2534a95f16fc8b8ee668932c7062a4 | PASS: remote SHA verified | origin | 29 JSON; 5402 PASS; build/format/diff PASS |
| EP-0015-US-0001-TK-0002-content-definitions | 8daec47976c5746d6aca54c1db3d66e187c32e24 | PASS: remote SHA verified | origin | 3 new tests PASS; build/format/diff PASS; unchanged baseline reused |
| EP-0015-US-0001-TK-0003-content-loader | 981a5f0921e6c37291400f4491f0e9984996aa9f | PASS: remote SHA verified | origin | 69 targeted PASS; Engine 2397, Client 2324, Economy 2 regression PASS; build/format/diff PASS |
| EP-0015-US-0001-TK-0004-content-sync | a0fb809b25dbec7461a17573cc8ece1e0b1881cf | PASS: remote SHA verified | origin | 71 Engine + 2 Client targeted PASS; content export/check/build/format/diff PASS |
| EP-0015-US-0002-TK-0001-py-math | 1926f73afffd6382c6e12d1286969da18555497a | PASS: remote SHA verified | origin | 280/280 Motion; build/format/diff PASS |
| EP-0015-US-0002-TK-0002-reference-fixtures | 8148aec3a43bf1df7c8fb8a3457af351623cbea8 | PASS: remote SHA verified | origin | Linux 42087024500 PASS; Windows 87 Engine; 90 battles/19 turns/1088 vectors; build/format/diff PASS |
| EP-0015-US-0002-TK-0003-mulberry32 | 53144cdbace23309b2f18889af102caf8fd6b680 | PASS: remote SHA verified | origin | 96 Engine + 280 Motion PASS; build/format/diff PASS |
| EP-0015-US-0002-TK-0004-geometry | 8a43baf53ac0ff3f88f518b769893ef7ec244283 | PASS: remote SHA verified | origin | Windows 1372 Motion + 96 Engine PASS; 1088 exact IEEE vectors; license copy/build/format/diff PASS |
| EP-0015-US-0002-TK-0005-limits-and-chance | fc29503a812f6793779d3564f22884a8cd0eecdb | PASS: remote SHA verified | origin | 1376 Motion PASS; 34 reference shot chances; build/format/diff PASS |
| EP-0015-US-0002-TK-0006-platform-determinism | 61c3c4410f0931d7f71fcfe6254ab99e80114ba7 | PASS: remote SHA verified | origin | Windows/Linux 1376 Motion; 1088 exact vectors; evidence/link/diff PASS |
| EP-0015-US-0003-TK-0001-battle-assembly | cdd2f3d6ba4ff648961fa1e4dcebf71f2abc4b09 | PASS: remote SHA verified | origin | 101 Engine + 1376 Motion PASS; independent assembly trace; build/format/diff PASS |
| EP-0015-US-0003-TK-0002-movement | 692e9f5501f7cb994d7d27c479990de6e07ef782 | PASS: remote SHA verified | origin | 104 Engine + 1376 Motion PASS; all 19 slices; build/format/diff PASS |
| EP-0015-US-0003-TK-0003-salvo | 9a505142e2b9c6f80a448c2174d893c007fada22 | PASS: remote SHA verified | origin | 122 Engine + 1376 Motion PASS; all 19 salvo states/calls; build/format/diff PASS |
| EP-0015-US-0003-TK-0004-turn-and-outcome | 834a6ce5f81d645a3a43dbc0bb4befb9222c9399 | PASS: remote SHA verified | origin | 130 targeted; 6443 fresh regression across 6 projects; build/format/diff PASS |

| EP-0015-US-0004-TK-0001-steering | 5c80365baec6e22ca1465f690c730f367d240194 | PASS: remote SHA verified | origin | 138 Engine + 19 source guard PASS; first trace pursuit; build/format/diff PASS |

| EP-0015-US-0004-TK-0002-striker-fighter-tail | d4a2ffbbc7e467c62508c68522d6c8d57c83ff8b | PASS: remote SHA verified | origin | 144 Engine + 19 source guard PASS; modes/hunt/grid/RNG; build/format/diff PASS |

| EP-0015-US-0004-TK-0003-interceptor-commander | cacd325503acb679244764a25acd28f13ab172f7 | PASS: remote SHA verified | origin | 154 Engine + 19 source guard PASS; all first-turn orders/58 calls; build/format/diff PASS |

| EP-0015-US-0004-TK-0004-golden-battles | 1f92fafffe9a6c121205d0c5a740edc5929b077a (code); evidence 682a9b758724922b6b91c49c2d523e078afd9020, remote verified | PASS: remote SHA verified; Linux 42098486879 PASS | origin | 245 Engine; 90 summaries + continuous trace on Windows/Linux; build/format/diff PASS |

| EP-0015-US-0005-TK-0001-contracts | 7b073f4fab7ee9ef8b8a6c665fa90f084c16d9cc | PASS: remote SHA verified | origin | 283 Contracts + 245 Engine PASS; Client 2324 cases covered across broad/isolated runs (one broad timeout retained); build/format/diff PASS |

| EP-0015-US-0005-TK-0002-engine-session | f36f714bce975686e34084eebdad12f83d1ca67b | PASS: remote SHA verified | origin | 274 Engine PASS; complete seed-1 via session both modes; build/format/diff PASS |

| EP-0015-US-0005-TK-0003-world-guard | ab56c05a1d666de24b414f0d7ff3ae7fc28bece2 | PASS: remote SHA verified | origin | 436 Engine PASS; exact world preservation/save bytes; build/format/diff PASS |

| EP-0015-US-0005-TK-0004-connection-api | SHA проверяется Git после создания коммита | pending | origin | 284 Engine + 283 Contracts PASS; concurrent tick boundary; build/format/diff PASS |
