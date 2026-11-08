---
epic: EP-0020-tactical-battle-mechanics
stage: draft
created: 2026-11-08T11:46:28Z
---

# EP-0020 — карта тикетов

[Эпик и решения](Documentation.md) · [UiSpec](UiSpec.md) · 13 историй, 44 тикетов. Механика M1 «Уклонение и сознание» — US-0001…US-0004; M2 «Ловушки» — US-0005…US-0007; M3 «Аномалии» — US-0008…US-0010; M4 «Пилот и сборка» — US-0011…US-0012; US-0013 — документация, всегда последняя. Порядок строк соблюдает зависимости.

**Весь эпик исполняется после полного выполнения и приёмки [EP-0019](../EP-0019-tactical-battle-ui-redesign/Tickets.md).**

## Истории

| ID | Файл | Тикетов | Dependencies |
|---|---|---|---|
| US-0001 | [EP-0020-US-0001-consciousness](EP-0020-US-0001-consciousness/EP-0020-US-0001-consciousness.md) | 2 | EP-0019 |
| US-0002 | [EP-0020-US-0002-evasion-resolution](EP-0020-US-0002-evasion-resolution/EP-0020-US-0002-evasion-resolution.md) | 5 | US-0001 |
| US-0003 | [EP-0020-US-0003-evasion-calibration](EP-0020-US-0003-evasion-calibration/EP-0020-US-0003-evasion-calibration.md) | 1 | US-0002 |
| US-0004 | [EP-0020-US-0004-evasion-ui](EP-0020-US-0004-evasion-ui/EP-0020-US-0004-evasion-ui.md) | 4 | US-0002, EP-0019 |
| US-0005 | [EP-0020-US-0005-decoy-module](EP-0020-US-0005-decoy-module/EP-0020-US-0005-decoy-module.md) | 2 | US-0002 |
| US-0006 | [EP-0020-US-0006-decoy-resolution](EP-0020-US-0006-decoy-resolution/EP-0020-US-0006-decoy-resolution.md) | 5 | US-0005 |
| US-0007 | [EP-0020-US-0007-decoy-ui](EP-0020-US-0007-decoy-ui/EP-0020-US-0007-decoy-ui.md) | 4 | US-0006, US-0004 |
| US-0008 | [EP-0020-US-0008-anomaly-field](EP-0020-US-0008-anomaly-field/EP-0020-US-0008-anomaly-field.md) | 3 | US-0006 |
| US-0009 | [EP-0020-US-0009-anomaly-resolution](EP-0020-US-0009-anomaly-resolution/EP-0020-US-0009-anomaly-resolution.md) | 5 | US-0008 |
| US-0010 | [EP-0020-US-0010-anomaly-ui](EP-0020-US-0010-anomaly-ui/EP-0020-US-0010-anomaly-ui.md) | 4 | US-0009, US-0007 |
| US-0011 | [EP-0020-US-0011-build-slots](EP-0020-US-0011-build-slots/EP-0020-US-0011-build-slots.md) | 2 | US-0009 |
| US-0012 | [EP-0020-US-0012-build-card-ui](EP-0020-US-0012-build-card-ui/EP-0020-US-0012-build-card-ui.md) | 3 | US-0011, US-0010 |
| US-0013 | [EP-0020-US-0013-project-documentation](EP-0020-US-0013-project-documentation/EP-0020-US-0013-project-documentation.md) | 4 | US-0003, US-0004, US-0006, US-0007, US-0009, US-0010, US-0012 |

## Тикеты

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|
|

## Контроль передачи

- **US-0001:** данные и состояние сознания без изменения исходов боёв.
- **US-0002:** механика в движке; ожидания исторических тестов обновляются только с записанной причиной.
- **US-0003:** отчёт калибровки для штрафа −20; данные не меняются.
- **US-0004:** интерфейс по UiSpec; проверки вида и регрессионные снимки EP-0019 зелёные, пробы UiSpec §7 проходят.
- **US-0005:** данные модуля и заряды; ожидания исторических тестов обновлены с причиной «масса модуля».
- **US-0006:** ловушки в движке; отчёт о действии ловушек; данные не меняются.
- **US-0007:** интерфейс по UiSpec §8–§14; проверки вида EP-0019 пересчитаны на новых данных seed 1.
- **US-0008:** правила, математика и генерация аномалий; основной поток RNG и исходы не меняются.
- **US-0009:** поглощение, пыль и ИИ в движке; отчёт о влиянии аномалий.
- **US-0010:** интерфейс по UiSpec §15–§21; проверки вида EP-0019 пересчитаны на бое с аномалиями.
- **US-0011:** слоты сборки в снимке; исходы не меняются.
- **US-0012:** карточка «Пилот и сборка» по UiSpec §22–§26; проверки вида EP-0019 обновлены.
- **US-0013:** документация и ExecutionStatus.

Блокирующие решения пользователя: [открытые вопросы](Documentation.md#открытые-вопросы).
