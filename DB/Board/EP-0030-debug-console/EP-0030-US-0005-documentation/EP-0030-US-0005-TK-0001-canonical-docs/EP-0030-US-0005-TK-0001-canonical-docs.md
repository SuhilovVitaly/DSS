---
epic: EP-0030-debug-console
story: EP-0030-US-0005-documentation
ticket: EP-0030-US-0005-TK-0001-canonical-docs
title: Каноническая документация дебаг-консоли
stage: approved
approval_kind: autonomous-execution
implementation_status: implemented
layer: documentation
test_project: none
depends_on: []
prerequisite_stories: [EP-0030-US-0001-global-console, EP-0030-US-0002-exit-tutorial-battle, EP-0030-US-0003-kill-drone, EP-0030-US-0004-console-discovery]
files_touched: 7
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
| `DB/Documentation/README.md` | Оглавление документации | Добавлено исполнителем 2026-11-16 до правки по инвентаризации (`DB/Board/EpicExecutionPrompt.md` §6: лимит файлов не основание пропустить нужное обновление; поручение оркестратора обновить найденные документы): ссылка на новый `DebugConsole.md` |
| `DB/Documentation/02-FirstRelease/Screens/GameSession.md` | Горячие клавиши карты (F8, F12) | Добавлено исполнителем 2026-11-16 до правки по той же причине: Ctrl+Alt+P и сброс выбора/манёвра карты при открытии консоли |

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

- Семь файлов обновлены/созданы (два добавлены до правки по инвентаризации, см. Code context); ссылки разрешаются; `git diff --check` чист.
- Execution evidence в тикете, commit + push.

## Self-containment check

Файлы, содержание разделов и проверки заданы.

## Execution evidence 2026-11-16

Источник фактов — итоговый diff эпика `git diff 43a6fecf..aff52cb0` (46 файлов: `src/DeepSpaceSaga.Client/UI/DebugConsole/*`, `SkiaWindow.cs`, `KeyboardEdgeTracker.cs`, `TacticalBattleFlow.cs`, `TacticalBattleScreen.cs`, `GameSessionHandle.TacticalBattle.cs`, `IGameSessionConnection.cs`, `TacticalBattleRequests.cs`, `LocalGameSessionConnection.cs`, `SimulationEngine.TacticalBattle.cs`, `TacticalBattleSession.cs`, тесты и Board). Расхождений кода с тикетами US-0001..0004 не найдено.

### Инвентаризация влияния

Поиск по `*.md` всего репозитория: `ExitTacticalBattleRequest`, `IGameSessionConnection`, `TacticalBattleRejectionCode`, `InvalidTarget`, `Start/Move/Fire`, `KeyboardEdgeTracker`, `Ctrl+Alt`, `ConfirmExitToMenu`, `exit_battle`, `kill_drone`, «дебаг», «отладк», «консол», горячие клавиши F8/F10/F12.

| Документ | Что изменилось в коде | Требуемое обновление | Результат |
|---|---|---|---|
| `DB/Documentation/04-Engineering/DebugConsole.md` | Новая подсистема `UI/DebugConsole`, маршрутизация ввода в `SkiaWindow` | Новый документ: вызов, оверлей, ввод, занятость/устаревание, 4 команды с текстами, G-01/G-04, расширение через `DebugCommandCatalog`, тесты | Создан |
| `DB/Documentation/04-Engineering/TacticalBattle.md` | `DebugDestroyTacticalUnitRequest`, `SimulationEngine.DebugDestroyTacticalUnit`, `TacticalBattleSession.DebugDestroy` | Строка Contracts в таблице слоёв; раздел «Дебаг-уничтожение дрона» (путь, порядок проверок, InvalidTarget, без RNG/боезапаса/хода, idempotency, CheckVictory, settlement tombstone); исключение `exit_battle` в «Запуск обучения» | Обновлён |
| `DB/Documentation/02-FirstRelease/Screens/TacticalBattle.md` | Консоль поверх боя, `ShowConfirmedState`, `exit_battle` | Абзац о консоли в бою; недатированное «единственный выход» уточнено как «единственный штатный» с отладочным исключением | Обновлён |
| `DB/Documentation/02-FirstRelease/Screens/ScreenCatalog.md` | Глобальный оверлей вне `ScreenStack` | Консоль в перечне дополнительных поверхностей | Обновлён |
| `DB/Documentation/02-FirstRelease/Screens/GameSession.md` | `GameSessionScreen.OnDeactivated` при открытии консоли | Ctrl+Alt+P рядом с F8/F12; сброс выбора и манёвра | Обновлён (allowlist расширен до правки) |
| `DB/Documentation/README.md` | Новый инженерный документ | Ссылка в оглавлении 04-Engineering | Обновлён (allowlist расширен до правки) |
| `DB/Board/EP-0027-tutorial-start/Documentation.md` | Дебаг-исключение из D-03/D-04 EP-0027 | Датированное дополнение 2026-11-16, история не переписана | Обновлён |
| `DB/Documentation/01-Requirements/EngineRequirements.md`, `FirstReleaseRequirements.md` | Отладочный запрос проходит через сессию и Engine, требования авторитета не нарушены | Нет (инвариант тикета: `01-Requirements` не меняются) | Проверено, без изменений |
| `DB/Documentation/00-Process/CLAUDE.md` | Архитектурная сводка боя (Engine владеет исходом, клиент через сессию) | Нет: дебаг-запрос следует той же границе | Проверено, без изменений |
| `DB/Documentation/04-Engineering/SaveFormat.md` | Бой не сохраняется; settlement — существующий путь EP-0029 | Нет | Проверено, без изменений |
| `DB/Documentation/04-Engineering/DebugTuningPanel.md` | F12 не изменена; при открытии консоли над картой панель освобождается вместе с `OnDeactivated` | Нет: описано в `DebugConsole.md` | Проверено, без изменений |
| `VData/Documentation/*` | Архивная копия документации (те же совпадения) | Нет: не канонический источник | Проверено, без изменений |
| `DB/Board/EP-0028-battle-tutorial`, `EP-0029-unified-carrier-and-torpedoes` | Связанные контракты подсказок и settlement не изменены | Нет | Проверено, без изменений |
| Board EP-0030 (эпик, `Tickets.md`, истории) | — | TK-0002 | Вне этого тикета |
| `README`/руководства вне `DB`, документы рядом с `src`/`tests`/`tools` | Совпадений по терминам нет | Нет | Проверено |

### Проверки

- Относительные ссылки и якоря во всех 7 изменённых/новых документах: perl-скрипт проверки (вне репозитория, файл существует, якорь заголовка существует) — `checked 7 files, problems: 0`; скрипт проверен на заведомо битых ссылке и якоре.
- `git diff --check` — exit 0.
- Production-код и тесты не менялись; automated-тесты для documentation не применимы.
