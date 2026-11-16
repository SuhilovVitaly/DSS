---
epic: EP-0030-debug-console
story: EP-0030-US-0005-documentation
ticket: EP-0030-US-0005-TK-0001-canonical-docs
title: Каноническая документация дебаг-консоли
stage: approved
approval_kind: autonomous-execution
layer: documentation
test_project: none
depends_on: []
prerequisite_stories: [EP-0030-US-0001-global-console, EP-0030-US-0002-exit-tutorial-battle, EP-0030-US-0003-kill-drone, EP-0030-US-0004-console-discovery]
files_touched: 5
serves: [AC-01, AC-02, AC-04]
created: 2026-11-16T06:50:00Z
revision: 1
---

# TK-0001 — Каноническая документация

## Why

Поведение консоли и новый Engine-запрос должны быть описаны в `DB/Documentation`, иначе следующий исполнитель восстанавливает их из кода. Покрывает AC-01/02/04 [истории](../EP-0030-US-0005-documentation.md).

## Decisions

- Оператор, 2026-11-16: «Создай новую ветку… выполни EP-0030… после каждого тикета коммит и пуш. Тесты только после окончания юзерстори» и «работай автономно».
- Пользователь, 2026-11-16: G-01 «Штатный экран гибели»; G-04 «Id или подпись на карте» — отразить в документации.

## Assumptions

- Документация описывает фактически реализованное поведение после US-0001..0004; при расхождении с тикетами истиной считается код и выполненные тесты, расхождение записывается в Board (TK-0002).

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `DB/Documentation/04-Engineering/DebugConsole.md` | Новый | Ctrl+Alt+P, overlay, маршрутизация ввода, реестр/`DebugCommandCatalog`, команды и сообщения, busy/stale правила |
| `DB/Documentation/04-Engineering/TacticalBattle.md` | Существующий инженерный документ боя | Раздел о `DebugDestroyTacticalUnitRequest`: отказы, мутации, idempotency, settlement |
| `DB/Documentation/02-FirstRelease/Screens/TacticalBattle.md` | Экранный документ боя | Заметка о `exit_battle`/`kill_drone` и `ShowConfirmedState` |
| `DB/Documentation/02-FirstRelease/Screens/ScreenCatalog.md` | Каталог экранов | Глобальный overlay консоли |
| `DB/Board/EP-0027-tutorial-start/Documentation.md` | Запрет раннего выхода из обучения | Датированная заметка 2026-11-16: дебаг-исключение `exit_battle` (EP-0030), обычный выход не меняется |

## Public API after the change

No API change.

## Implementation steps

1. Сверить фактический код US-0001..0004 (`src/DeepSpaceSaga.Client/UI/DebugConsole/`, `SimulationEngine.TacticalBattle.cs`, `TacticalBattleSession.cs`).
2. Написать/дополнить пять документов; ссылки относительные.
3. Проверить ссылки, ID, `git diff --check`.

## Out of scope

Board-файлы EP-0030 (TK-0002); production-код; переписывание исторических разделов.

## Invariants

Требования (`01-Requirements`) не меняются; EP-0027 получает только датированное дополнение.

## Tests

Нет automated-тестов (documentation). Детерминированная проверка: разрешение относительных ссылок в изменённых файлах, согласованность ID, `git diff --check`.

## Definition of Done

- Пять файлов обновлены/созданы; ссылки разрешаются; `git diff --check` чист.
- Execution evidence в тикете, commit + push.

## Self-containment check

Файлы, содержание разделов и проверки заданы.
