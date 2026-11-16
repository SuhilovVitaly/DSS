# Тикеты EP-0030-debug-console

[Эпик](Documentation.md). 5 историй, 13 тикетов. Ветка `feature/ep-0030-debug-console`, база `43a6fecf` (последний коммит планирования до кода). Порядок исполнения: US-0001 → US-0002 → US-0003 → US-0004 → US-0005, внутри истории — по номеру тикета. Approval тикетов — autonomous-execution по поручению оператора 2026-11-16, не отдельный human review технических деталей.

## Статус историй

| История | Stage | Тикеты | Story evidence |
|---|---|---|---|
| [US-0001 — Глобальная консоль](EP-0030-US-0001-global-console/EP-0030-US-0001-global-console.md) | implemented | 3 | `688c0ba5` |
| [US-0002 — Выход из обучения](EP-0030-US-0002-exit-tutorial-battle/EP-0030-US-0002-exit-tutorial-battle.md) | implemented | 1 | в `cdc190ad` (единственный тикет) |
| [US-0003 — Уничтожение по имени](EP-0030-US-0003-kill-drone/EP-0030-US-0003-kill-drone.md) | implemented | 5 | `6ff78b13` |
| [US-0004 — Справочные команды](EP-0030-US-0004-console-discovery/EP-0030-US-0004-console-discovery.md) | implemented | 2 | `aff52cb0` |
| [US-0005 — Документация](EP-0030-US-0005-documentation/EP-0030-US-0005-documentation.md) | implemented | 2 | в коммите TK-0002 |

## Тикеты и опубликованные коммиты

| Тикет | Layer | Depends on | implementation_status | Коммит(ы) |
|---|---|---|---|---|
| [EP-0030-US-0001-TK-0001-hotkey-edge](EP-0030-US-0001-global-console/EP-0030-US-0001-TK-0001-hotkey-edge/EP-0030-US-0001-TK-0001-hotkey-edge.md) | client | none | implemented | `dcd0b565` |
| [EP-0030-US-0001-TK-0002-console-core](EP-0030-US-0001-global-console/EP-0030-US-0001-TK-0002-console-core/EP-0030-US-0001-TK-0002-console-core.md) | client | US-0001-TK-0001 | implemented | `ecee1df5` |
| [EP-0030-US-0001-TK-0003-overlay-integration](EP-0030-US-0001-global-console/EP-0030-US-0001-TK-0003-overlay-integration/EP-0030-US-0001-TK-0003-overlay-integration.md) | client | US-0001-TK-0002 | implemented | `583f75a0` |
| [EP-0030-US-0002-TK-0001-exit-battle](EP-0030-US-0002-exit-tutorial-battle/EP-0030-US-0002-TK-0001-exit-battle/EP-0030-US-0002-TK-0001-exit-battle.md) | client | US-0001 | implemented | `cdc190ad` |
| [EP-0030-US-0003-TK-0001-destroy-contract](EP-0030-US-0003-kill-drone/EP-0030-US-0003-TK-0001-destroy-contract/EP-0030-US-0003-TK-0001-destroy-contract.md) | contracts | US-0001 | implemented | `5715fc65` |
| [EP-0030-US-0003-TK-0002-engine-force-destroy](EP-0030-US-0003-kill-drone/EP-0030-US-0003-TK-0002-engine-force-destroy/EP-0030-US-0003-TK-0002-engine-force-destroy.md) | engine | US-0003-TK-0001 | implemented | `7c036c53`, исправление теста `c4dae7da` |
| [EP-0030-US-0003-TK-0003-local-passthrough](EP-0030-US-0003-kill-drone/EP-0030-US-0003-TK-0003-local-passthrough/EP-0030-US-0003-TK-0003-local-passthrough.md) | local-client | US-0003-TK-0002 | implemented | `f5a3a3cf` |
| [EP-0030-US-0003-TK-0004-handle-flow](EP-0030-US-0003-kill-drone/EP-0030-US-0003-TK-0004-handle-flow/EP-0030-US-0003-TK-0004-handle-flow.md) | client | US-0003-TK-0003 | implemented | `d8a1aae3` |
| [EP-0030-US-0003-TK-0005-kill-command](EP-0030-US-0003-kill-drone/EP-0030-US-0003-TK-0005-kill-command/EP-0030-US-0003-TK-0005-kill-command.md) | client | US-0003-TK-0004 | implemented | `fded9644`, исправление теста `3269d98a` |
| [EP-0030-US-0004-TK-0001-help](EP-0030-US-0004-console-discovery/EP-0030-US-0004-TK-0001-help/EP-0030-US-0004-TK-0001-help.md) | client | US-0001 | implemented | `6a1afff5` |
| [EP-0030-US-0004-TK-0002-list-units](EP-0030-US-0004-console-discovery/EP-0030-US-0004-TK-0002-list-units/EP-0030-US-0004-TK-0002-list-units.md) | client | US-0004-TK-0001 | implemented | `d37a7162` |
| [EP-0030-US-0005-TK-0001-canonical-docs](EP-0030-US-0005-documentation/EP-0030-US-0005-TK-0001-canonical-docs/EP-0030-US-0005-TK-0001-canonical-docs.md) | documentation | US-0001..US-0004 | implemented | `c95ba2a7` |
| [EP-0030-US-0005-TK-0002-board-sync](EP-0030-US-0005-documentation/EP-0030-US-0005-TK-0002-board-sync/EP-0030-US-0005-TK-0002-board-sync.md) | documentation | US-0005-TK-0001 | implemented | коммит `EP-0030 US-0005 TK-0002: synchronize board and record epic evidence`; собственный SHA не записывается заранее — проверяется через `git log` после публикации |

Источник SHA: `git log --oneline 43a6fecf..HEAD` ветки на момент записи (HEAD `c95ba2a7`).

## Валидация эпика

| Проверка | Результат | Evidence |
|---|---|---|
| Полный build/test решения на `aff52cb0` (весь код эпика) | Build: 0 ошибок. Test: 810 падений, набор идентичен базе `43a6fecf` (Engine 630, Client 166, Performance 10, EconomyBalance 3, Motion 1); (a) регрессий (падает на HEAD, проходит на базе) — 0; (b) новых падающих тестов — 0; 96 новых тестов эпика — все проходят | `D:/DeepSpaceSaga/artifacts/validation/ep0030-debug-console/full-20261116T075334Z/` (build.txt, test.txt, trx); база `full-base-43a6fecf-20261116T075945Z/`; сравнение `full-base-43a6fecf-20261116T075945Z/diff.txt` |
| Тесты историй | См. «Story test evidence» в файлах US-0001..US-0004 | `D:/DeepSpaceSaga/artifacts/validation/ep0030-debug-console/us000*-*/` |
| Независимая валидация (Phase 4) | Регрессий нет, ключевые утверждения подтверждены. Остаточное: native-проверка не выполнена; открытие консоли над картой сбрасывает выбор и ввод манёвра, как потеря фокуса (задокументировано в [DebugConsole](../../Documentation/04-Engineering/DebugConsole.md#маршрутизация-ввода)) | Отчёт оркестратора 2026-11-16 |
| Native/manual run (матрица экранов G-02, RU/EN раскладки, `exit_battle` → карта, `kill_drone`, `help`/`list_units`) | **NOT RUN** — подтверждённый меньший монитор (`DSS_VALIDATION_SMALL_MONITOR=1`) в автономном прогоне недоступен. Продуктовая приёмка открыта | — |
| Graphify | **NOT RUN** — в репозитории нет отслеживаемого вывода графа: `src/graphify-out/` игнорируется `.gitignore` и содержит только исторический кеш (последнее изменение 2026-11-05, без `graph.json`); корневого `graphify-out/` нет; whitelist `.graphifyignore` не включает `DB/Board/EP-0030-debug-console/`. Граф не перестраивался и не отражает EP-0030 | [Graphify](../../Documentation/06-Tooling/Graphify.md) |
| Документы US-0005 | Ссылки/якоря изменённых документов разрешаются, `git diff --check` чист | Тикеты [TK-0001](EP-0030-US-0005-documentation/EP-0030-US-0005-TK-0001-canonical-docs/EP-0030-US-0005-TK-0001-canonical-docs.md#execution-evidence-2026-11-16), [TK-0002](EP-0030-US-0005-documentation/EP-0030-US-0005-TK-0002-board-sync/EP-0030-US-0005-TK-0002-board-sync.md#execution-evidence-2026-11-16) |

Полный прогон выполнен на `aff52cb0`; коммиты US-0005 меняют только Markdown в `DB/`, поэтому повторный прогон кода не требовался.
