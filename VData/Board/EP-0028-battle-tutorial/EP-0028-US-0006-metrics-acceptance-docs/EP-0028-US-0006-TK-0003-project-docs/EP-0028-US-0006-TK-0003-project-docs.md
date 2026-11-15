---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0006-metrics-acceptance-docs
ticket: EP-0028-US-0006-TK-0003-project-docs
title: Документация проекта
stage: implemented
layer: documentation
test_project: none
depends_on: [EP-0028-US-0006-TK-0002-native-acceptance]
implementation_files: 4
files_touched:
  - DB/Documentation/02-FirstRelease/Screens/TacticalBattle.md
  - DB/Documentation/02-FirstRelease/Screens/Settings.md
  - DB/Documentation/04-Engineering/TacticalBattle.md
  - tools/graphify/rebuild.py
serves: [US-0006/AC-03]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Документация проекта

[История](../EP-0028-US-0006-metrics-acceptance-docs.md) · [Эпик](../../Documentation.md)

## Цель

Последний тикет эпика: привести документацию проекта и Board EP-0028 в соответствие с фактическим результатом ([EpicExecutionPrompt.md](../../../EpicExecutionPrompt.md) §6).

## Decisions

D-05 — «Повторить обучение» описывается как отложенное; D-06 — описать зависимость от языка и паритет.

## Assumptions

- Отдельный документ не создаётся: режим обучения уже описан в `Screens/TacticalBattle.md` (раздел «Режим обучения», `:31`) и `04-Engineering/TacticalBattle.md` (раздел «Запуск обучения», `:85-91`). На заголовок «Режим обучения» ссылается `Screens/MainMenu.md:5` (`TacticalBattle.md#режим-обучения`) — заголовок не переименовывать.
- `tools/graphify/rebuild.py` содержит явный список эпиков (`:22-41`, EP-0027 в нём нет). Добавлять EP-0028 — только если граф перестраивается в рамках эпика; иначе файл не трогать и записать решение в «Журнал».
- Board-артефакты EP-0028 (`Documentation.md`, `Tickets.md`, истории, тикеты) обновляются вне лимита файлов.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `DB/Documentation/02-FirstRelease/Screens/TacticalBattle.md` | «Режим обучения» (`:31-40`): запуск, seed 1, выходы | Панель связи, офицер (имя RU/EN, портрет, тон кратко), подсветка, правила тишины, журнал H, Tabletop принудительно, итоговый seed, вкл/выкл |
| `DB/Documentation/02-FirstRelease/Screens/Settings.md` | Перечень настроек (`:5`), источник (`:17`) | Строка «Подсказки обучения», хранение `gameSettings.tutorialHints` |
| `DB/Documentation/04-Engineering/TacticalBattle.md` | «Запуск обучения» (`:85-91`), seed 1 (`:89`) | Контракт: `Data/Tutorial/battle-tutorial.json` (схема), `TutorialDirector`/сигналы/наблюдение кадра, размещение кода вне `/TacticalBattle/`, клиентская граница (D-07), метрики `*.tutorial.json`, итоговый seed |
| `tools/graphify/rebuild.py` | Список эпиков (`:22-41`) | Только по Assumptions |

## Public API after the change

No API change.

## Implementation steps

1. Инвентаризация: по итоговому `git diff d8ab8f71..HEAD --stat` и ключевым словам (`Tutorial`, `tutorialHints`, `battle-tutorial`, `seed 1`) найти все затронутые документы в репозитории (README, `DB/Documentation`, `src/**/README.md`, `Skins/README.md`); таблица «документ — что изменилось — обновление — проверка» в «Журнал».
2. Обновить три документа; убрать устаревшие утверждения (например «seed 1», если seed изменён в US-0005-TK-0002).
3. Board: `Documentation.md` (статус исполнения, SHA тикетов, покрытие критериев), `Tickets.md`, истории (completion evidence), тикеты (журналы).
4. Проверить относительные ссылки и ID, отсутствие не-`.md` файлов в `DB/Documentation`, `git diff --check`.

## Acceptance criteria

1. Документы описывают фактическое поведение (без планов и отчётов прогонов — [DocumentationSystem.md](../../../../Documentation/00-Process/DocumentationSystem.md)).
2. Board EP-0028 содержит статусы, SHA опубликованных тикетов и покрытие критериев.
3. Ссылки валидны; `git diff --check` чист.

## Verification signal

Детерминированные проверки: скрипт проверки относительных ссылок по изменённым `.md` (временный, в `$RUN`, удаляется), `git diff --check`, `git grep -n "seed 1"` по документам согласован с `SkiaWindow.TutorialBattleSeed`.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0006-tk0003-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
git diff --check
git grep -n "tutorialHints\|battle-tutorial\|Режим обучения" -- DB/Documentation
```

## Out of scope

Код; новые документы вне перечня без необходимости (при необходимости — записать причину в «Журнал»).

## Invariants

- Исторические датированные записи других эпиков не переписываются.
- В `DB/Documentation` только `.md`.

## Known fallout

`Screens/MainMenu.md:5` ссылается на якорь `#режим-обучения` — сохранить заголовок.

## Definition of Done

- Документы и Board обновлены; проверки ссылок и `git diff --check` чисты.
- Отдельный коммит и `git push`; итоговая сверка отсутствия непубликованных изменений эпика.

## Self-containment check

Перечень документов, разделы с `path:line`, правило для graphify и проверки заданы.

## Результат

Инвентаризация (`git diff d8ab8f71..HEAD --stat` — 69 файлов: `src/DeepSpaceSaga.Client` 20, `tests/DeepSpaceSaga.Client.Tests` 17, остальное — Board; поиск `Tutorial`, `tutorialHints`, `battle-tutorial`, `seed 1` по `DB/Documentation`, README, `src/**/README.md`, `Skins/README.md`, `tools`):

| Документ | Что изменилось в коде | Обновление | Проверка |
|---|---|---|---|
| `02-FirstRelease/Screens/TacticalBattle.md` | Панель связи, офицер, подсветка, правила тишины, журнал H, E/D, принудительный Tabletop, `TutorialBattleSeed`, вкл/выкл | «Режим обучения» дополнен (заголовок сохранён), новый подраздел «Подсказки офицера», источники | ссылки/якоря |
| `02-FirstRelease/Screens/Settings.md` | Строка «Подсказки обучения», `gameSettings.tutorialHints`, шаг строк 4 px, 640×480 | Перечень настроек, абзац о строке, источник `TutorialHintsPreference.cs` | ссылки/якоря |
| `04-Engineering/TacticalBattle.md` | `TutorialBattleSeed`, выбор скина обучения, namespace `Tutorial`, формат `battle-tutorial.json`, директор/сигналы/тишина, предпочтение, метрики | «Запуск обучения» уточнён, новый раздел «Подсказки обучения» | ссылки/якоря, `git grep seed` |
| `02-FirstRelease/Screens/MainMenu.md` | Нет (запуск обучения прежний) | Не меняется — ссылка `TacticalBattle.md#режим-обучения` валидна | якорь |
| `02-FirstRelease/Screens/TacticalBattleSetup.md` | Нет (seed 1 прежний) | Не меняется — «seed 1» верен (`TutorialBattleSeed = 1`) | `git grep seed` |
| `04-Engineering/TacticalBattleSkins.md`, `Skins/README.md`, `src/**/README.md`, `tools/DeepSpaceSaga.Performance/README.md` | Нет | Не меняются (упоминаний обучения нет или они вне эпика) | `git grep` |
| `tools/graphify/rebuild.py` | — | Не меняется: граф в рамках эпика не перестраивался (Assumptions тикета) | — |

Board: эпик `Documentation.md` → `stage: implemented`, статусы G-01…G-04, таблица коммитов 22 тикетов, покрытие критериев, отклонения от каталога уроков, отложенное «Повторить обучение»; `Tickets.md` — строка статуса; истории US-0001…US-0006 → `implemented` с коммитами и evidence.

Проверки 2026-11-14 (`D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0006-tk0003-20261114T2045/`):

- временный скрипт проверки относительных ссылок и якорей по изменённым и связанным `.md` (документы, эпик, Tickets, истории, тикеты US-0006) — 0 битых ссылок, 0 отсутствующих якорей (`links.txt`); скрипт удалён;
- в `DB/Documentation` только `.md`; `git diff --check` чист; `git grep "seed 1"` согласован с `SkiaWindow.TutorialBattleSeed = 1` (`checks.txt`).

## Журнал

2026-11-14T15:04:43Z — тикет создан.

2026-11-14 — документация и Board синхронизированы, `stage: implemented`. Эпик — `implemented`, не `done`: приёмку выполняет пользователь (чек-лист US-0006-TK-0002). Узкие решения исполнителя: (1) `tools/graphify/rebuild.py` и `Screens/MainMenu.md` не изменены (граф не перестраивался; меню описано верно); (2) отдельный документ об обучении не создавался — разделы в существующих документах; (3) исторические записи других эпиков не правились.
