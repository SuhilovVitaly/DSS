# EP-0015 — статус исполнения

## Текущее состояние — 2026-11-05

Исполнение продолжается. Все четыре тикета US-0001 выполнены технически и проверены; 4/78 тикетов. Новый бой ещё не подключён, остальные 74 тикетов и завершающая документационная история остаются открытыми.

- Ветка `codex/ep-0015-tactical-carrier-battle`, remote `origin` = `https://github.com/SuhilovVitaly/DSS`.
- Отмена запрета GitHub зафиксирована служебным коммитом `22bb633145d479391b985eb73d148bb7cad7de94`, push и remote SHA проверены.
- Принятые уточнения D-USER-14/15: исходники — `VData/Planning/V4.0` Markdown; JSON создаются по таблицам; Python-симулятор не нужен. Служебный коммит `06c5fc69032de5b7b7fe94d19079b5284998846c`, push и remote SHA проверены. Прежний preflight-блокер отсутствующих исходников снят этими указаниями.
- История US-0017 уже существует, зависит от всех функциональных историй и выполняется последней.
- На локальном Windows нет WSL/Docker/Podman. Для Linux-проверок US-0002 исследован GitHub Actions: репозиторий публичный, чтение workflow runs доступно через подключение GitHub. Linux runner ещё не запущен и PASS не заявляется; инфраструктура добавляется в scope соответствующего тикета, если этот путь доступен.

## Проверки

[ContentValidation.md](ContentValidation.md): build PASS (0 warnings/errors), 5402/5402 теста в шести проектах, scoped format и diff check PASS. Длительные Engine/Economy corpora проверены полными разделёнными наборами по точным ID, без ослабления assertions. Native/input, Linux parity и приёмка боя пока NOT RUN.

| Тикет | Коммит | Push | Remote | Проверки |
|---|---|---|---|---|
| EP-0015-US-0001-TK-0001-content-data | 8015f9852f2534a95f16fc8b8ee668932c7062a4 | PASS: remote SHA verified | origin | 29 JSON; 5402 PASS; build/format/diff PASS |

| EP-0015-US-0001-TK-0002-content-definitions | 8daec47976c5746d6aca54c1db3d66e187c32e24 | PASS: remote SHA verified | origin | 3 new tests PASS; build/format/diff PASS; unchanged baseline reused |


| EP-0015-US-0001-TK-0003-content-loader | 981a5f0921e6c37291400f4491f0e9984996aa9f | PASS: remote SHA verified | origin | 69 targeted PASS; Engine 2397, Client 2324, Economy 2 regression PASS; build/format/diff PASS |

| EP-0015-US-0001-TK-0004-content-sync | SHA проверяется Git после создания коммита | pending | origin | 71 Engine + 2 Client targeted PASS; content export/check/build/format/diff PASS |

Порядок продолжения: US-0002 по зависимостям Tickets.md. US-0001 backend завершён; Client error presentation из AC-03 остаётся в US-0007.
