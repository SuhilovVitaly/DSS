---
epic: EP-0030-debug-console
story: EP-0030-US-0005-documentation
title: Документация дебаг-консоли и синхронизация Board
stage: implemented
dependencies: [EP-0030-US-0001-global-console, EP-0030-US-0002-exit-tutorial-battle, EP-0030-US-0003-kill-drone, EP-0030-US-0004-console-discovery]
created: 2026-11-16T06:50:00Z
source_request: "Создай новую ветку… выполни EP-0030… после каждого тикета коммит и пуш. Тесты только после окончания юзерстори"
current_review: complete
revision: 1
ticket_map_approval: autonomous-execution
---

# US-0005 — Документация и синхронизация Board

## Входное техническое задание

Завершающая история по документации обязательна для автономного исполнения эпика (`DB/Board/EpicExecutionPrompt.md`, раздел 2). Оператор, 2026-11-16: «Создай новую ветку… выполни EP-0030… после каждого тикета коммит и пуш. Тесты только после окончания юзерстори» и «работай автономно». История добавлена исполнителем в рамках этого разрешения; это не отдельное решение пользователя о содержании документации.

## User story

Как разработчик DSS, я хочу найти в канонической документации описание дебаг-консоли, её команд и дебаг-исключения из tutorial gate, а в Board — фактическое состояние эпика с картой тикетов и коммитов, чтобы не восстанавливать это из истории Git.

## Acceptance criteria

- **AC-01:** `DB/Documentation/04-Engineering/DebugConsole.md` описывает вызов Ctrl+Alt+P, маршрутизацию ввода, реестр команд, команды `help`, `list_units`, `exit_battle`, `kill_drone` и правила занятости/устаревания.
- **AC-02:** `04-Engineering/TacticalBattle.md` описывает `DebugDestroyTacticalUnitRequest` и правила Engine; экранные документы (`02-FirstRelease/Screens/TacticalBattle.md`, `ScreenCatalog.md`) упоминают консоль как глобальный overlay; EP-0027 получает датированную заметку о дебаг-исключении.
- **AC-03:** эпик `Documentation.md` содержит актуальную таблицу историй, решения G-01/G-04 и review log; новый `Tickets.md` — карту тикет → SHA, статусы историй, запись native-валидации (NOT RUN, если меньший монитор не подтверждён) и Graphify NOT RUN.
- **AC-04:** ссылки разрешаются, ID согласованы, `git diff --check` чист.

## Non-goals

Изменение production-кода и тестов; новые требования; переписывание исторических разделов других эпиков (только датированная заметка).

## Dependencies

US-0001..US-0004 реализованы и протестированы: документация описывает фактическое поведение.

## Approved ticket map

Принято автономным workflow по разрешению оператора 2026-11-16.

| Тикет | Layer | Depends on | Criteria |
|---|---|---|---|
| [TK-0001 — canonical-docs](EP-0030-US-0005-TK-0001-canonical-docs/EP-0030-US-0005-TK-0001-canonical-docs.md) | documentation | none; prerequisite US-0001..0004 | AC-01, AC-02, AC-04 |
| [TK-0002 — board-sync](EP-0030-US-0005-TK-0002-board-sync/EP-0030-US-0005-TK-0002-board-sync.md) | documentation | EP-0030-US-0005-TK-0001-canonical-docs | AC-03, AC-04 |

## Gaps and backlog

Layer `documentation` отсутствует в таблице слоёв epic-writer; используется для тикетов без production-кода, `test_project: none`, проверка — ссылки/ID/whitespace.

## Review log

2026-11-16T06:50:00Z — история и два тикета созданы при подготовке Board к автономному исполнению.

2026-11-16 — TK-0001 (`c95ba2a7`) и TK-0002 реализованы, stage → implemented.

## Story evidence 2026-11-16

- **AC-01:** создан [`DebugConsole.md`](../../../Documentation/04-Engineering/DebugConsole.md) — Ctrl+Alt+P, оверлей вне `ScreenStack`, маршрутизация ввода и зеркалирование потери фокуса, реестр/`DebugCommandCatalog`, команды `help`, `list_units`, `exit_battle`, `kill_drone` с синтаксисом и сообщениями, правила занятости/устаревания (TK-0001, `c95ba2a7`).
- **AC-02:** [`TacticalBattle.md`](../../../Documentation/04-Engineering/TacticalBattle.md#дебаг-уничтожение-дрона) — раздел о `DebugDestroyTacticalUnitRequest`; [экран боя](../../../Documentation/02-FirstRelease/Screens/TacticalBattle.md), [каталог экранов](../../../Documentation/02-FirstRelease/Screens/ScreenCatalog.md), [карта](../../../Documentation/02-FirstRelease/Screens/GameSession.md) и [оглавление](../../../Documentation/README.md) упоминают консоль; [EP-0027](../../EP-0027-tutorial-start/Documentation.md) получил датированное дополнение (TK-0001). Инвентаризация влияния — в [TK-0001](EP-0030-US-0005-TK-0001-canonical-docs/EP-0030-US-0005-TK-0001-canonical-docs.md#инвентаризация-влияния); allowlist TK-0001 расширен с 5 до 7 файлов до правки по §6 `EpicExecutionPrompt.md`.
- **AC-03:** эпик [Documentation.md](../Documentation.md) — статус исполнения, таблица 5 историй со статусами, G-01/G-04 и решения 2026-11-16, review log; новый [Tickets.md](../Tickets.md) — 13 тикетов с SHA, статусы историй, валидация: полный build/test, native NOT RUN, Graphify NOT RUN (TK-0002).
- **AC-04:** относительные ссылки и якоря изменённых документов разрешаются (perl-проверка вне репозитория), ID и количества (5 историй, 13 тикетов) согласованы, `git diff --check` чист — в обоих тикетах.
- Production-код и тесты не менялись; полный прогон решения взят с `aff52cb0` (см. [Tickets.md](../Tickets.md#валидация-эпика)).
