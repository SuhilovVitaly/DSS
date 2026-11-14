---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0004-hints-toggle
title: Отключение подсказок
stage: approved
dependencies: [EP-0028-US-0001-hint-framework, EP-0028-US-0002-comms-panel]
created: 2026-11-14T15:04:43Z
source_request: "US-0004 Отключение: gameSettings.tutorialHints preference; Settings row; in-battle toggle."
current_review: complete
revision: 1
---

# Отключение подсказок

[Эпик](../Documentation.md) · [Тикеты](../Tickets.md)

## Входное техническое задание

D-04 [эпика](../Documentation.md#решения-пользователя-обязательные): простой переключатель вкл/выкл без уровней — в Настройках главного меню и кнопкой на панели связи в бою; значение хранится в `Settings.json` → `gameSettings`.

## User story

Как опытный игрок, я выключаю подсказки обучения в Настройках главного меню, и в бою обучения офицер молчит. Если я выключил их прямо в бою кнопкой на панели связи, выбор сохраняется и действует в следующий раз, а вернуть подсказки можно тем же значком в бою или в Настройках. Остальные настройки при этом не меняются.

## Acceptance criteria

- **AC-01.** `gameSettings.tutorialHints` читается (по умолчанию `true`) и атомарно записывается с сохранением всех прочих ключей файла; ошибка записи сообщается, прежнее значение остаётся.
- **AC-02.** В Настройках главного меню есть строка «Подсказки обучения» с двумя состояниями; изменение сохраняется сразу; геометрия остальных строк и кнопки выхода не ломается.
- **AC-03.** В бою обучения начальное состояние берётся из настройки; «Подсказки: выкл»/«Подсказки: вкл» на панели меняет состояние и сохраняет его в `Settings.json`.

## Non-goals

Уровни подробности; настройки внутри игрового меню мира; «Повторить обучение» (D-05).

## Invariants

- Образец записи — `TacticalBattleSkinPreference` (`UI/Screens/TacticalBattle/TacticalBattleSkinPreference.cs:40-96`); новый класс — в `UI/Screens/Tutorial/` (вне ForbiddenCalls).
- Поставляемый `src/DeepSpaceSaga.Client/Settings.json` не меняется (A-02).

## Dependencies

US-0001 (сессия создаётся в `SkiaWindow`), US-0002 (кнопка на панели, ключи `Settings.TutorialHints*`).

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](EP-0028-US-0004-TK-0001-hints-preference/EP-0028-US-0004-TK-0001-hints-preference.md) | Предпочтение tutorialHints | client | — | AC-01 |
| [TK-0002](EP-0028-US-0004-TK-0002-settings-row/EP-0028-US-0004-TK-0002-settings-row.md) | Строка подсказок в Настройках | client | TK-0001, US-0002-TK-0001 | AC-02 |
| [TK-0003](EP-0028-US-0004-TK-0003-battle-toggle/EP-0028-US-0004-TK-0003-battle-toggle.md) | Переключатель подсказок в бою | client | TK-0001, US-0002-TK-0002 | AC-03 |

Карта принята автоматическим workflow.

## Gaps and backlog

Нет.

## Completion evidence

Тесты предпочтения, Настроек и переключателя в бою зелёные; `SettingsScreenTests` и `SkinBattleIntegrationTests` без регрессий; профиль `Settings.json` после переключений сохраняет посторонние ключи.

## Журнал

2026-11-14T15:04:43Z — история создана по утверждённому плану.
