---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0006-metrics-acceptance-docs
ticket: EP-0028-US-0006-TK-0003-project-docs
title: Документация проекта
stage: approved
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

## Журнал

2026-11-14T15:04:43Z — тикет создан.
