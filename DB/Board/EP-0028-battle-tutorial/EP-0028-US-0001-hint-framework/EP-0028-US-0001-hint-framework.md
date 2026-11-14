---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0001-hint-framework
title: Каркас подсказок обучения
stage: approved
dependencies: []
created: 2026-11-14T15:04:43Z
source_request: "US-0001 Каркас подсказок: data format + loader/validation; TutorialDirector step logic; screen integration (IsTutorial only)."
current_review: complete
revision: 1
---

# Каркас подсказок обучения

[Эпик](../Documentation.md) · [Тикеты](../Tickets.md)

## Входное техническое задание

Из утверждённого плана: формат данных уроков с загрузчиком и валидацией; логика шагов `TutorialDirector`; подключение к экрану боя только в бою обучения. Реализует D-02 (рекомендательность), D-06 (тексты только ключами Locale), D-07 (только клиент) [эпика](../Documentation.md#решения-пользователя-обязательные).

## User story

Как игрок в первом бою новой игры, я получаю подсказки, которые появляются в нужный момент боя и исчезают, как только я сам выполнил подсказанное действие. Подсказки описаны в файле данных, поэтому их порядок и условия меняются без правки кода. Если файл повреждён или отсутствует, бой идёт как обычно, а ошибка попадает в InterfaceLog. В обычном бою по F8 ничего не меняется, и ввод боя никогда не перехватывается обучением.

## Acceptance criteria

- **AC-01.** `Data/Tutorial/battle-tutorial.json` (schemaVersion 1) загружается в неизменяемую модель; некорректный файл даёт понятную ошибку в журнал и пустой сценарий без исключения наружу.
- **AC-02.** Директор переводит шаги Waiting → Shown → Completed/Dismissed/Expired по наблюдениям кадра; одна реплика одновременно; шаг завершается действием игрока; уже выполненный шаг не показывается; ни один шаг не блокирует ввод.
- **AC-03.** Контекстные сигналы (под огнём, урон, низкое сознание, аномалия, потеря дрона, итог) вычисляются только из показанного снимка кадра, без RNG и без будущих FireSteps.
- **AC-04.** Сессия обучения создаётся только для `TacticalBattleFlow.IsTutorial`; бой по F8 без изменений; правки в `UI/Screens/TacticalBattle/` не добавляют находок `ForbiddenCallsTests`.

## Non-goals

Визуальная панель, подсветка, журнал и правила тишины — US-0002. Реальные тексты и уроки — US-0003. Переключатель — US-0004.

## Invariants

- Весь новый код — `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/` ([размещение](../Documentation.md#архитектурное-размещение)).
- Наблюдение только читает `TacticalBattleFrame`; Engine, Contracts и сохранения не меняются (D-07).

## Dependencies

none.

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](EP-0028-US-0001-TK-0001-script-format/EP-0028-US-0001-TK-0001-script-format.md) | Формат и загрузчик сценария обучения | client | — | AC-01 |
| [TK-0002](EP-0028-US-0001-TK-0002-director-core/EP-0028-US-0001-TK-0002-director-core.md) | Директор шагов обучения | client | TK-0001 | AC-02 |
| [TK-0003](EP-0028-US-0001-TK-0003-battle-signals/EP-0028-US-0001-TK-0003-battle-signals.md) | Контекстные сигналы боя | client | TK-0002 | AC-03 |
| [TK-0004](EP-0028-US-0001-TK-0004-screen-integration/EP-0028-US-0001-TK-0004-screen-integration.md) | Подключение обучения к экрану боя | client | TK-0003 | AC-04, AC-02 |

Карта принята автоматическим workflow (без отдельного человеческого согласования тикетов).

## Gaps and backlog

G-03 эпика (базовые падения ForbiddenCalls) учитывается в TK-0004.

## Completion evidence

Тесты `DeepSpaceSaga.Client.Tests.Tutorial.*` из тикетов зелёные; `ForbiddenCallsTests` без новых находок относительно базы; `TutorialFlowTests` без новых падений.

## Журнал

2026-11-14T15:04:43Z — история создана по утверждённому плану.
