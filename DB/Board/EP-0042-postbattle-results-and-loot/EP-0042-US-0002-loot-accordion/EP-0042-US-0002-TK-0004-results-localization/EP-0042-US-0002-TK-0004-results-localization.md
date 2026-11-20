---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0002-loot-accordion
ticket: EP-0042-US-0002-TK-0004-results-localization
title: Русские и английские строки результатов
stage: draft
layer: content-data
depends_on: []
files_touched: 3
serves: [AC-03, AC-04]
created: 2026-11-20T10:29:12Z
revision: 1
---

# Русские и английские строки результатов

## Why

Игрок должен видеть согласованные названия кнопок и исходы операций на текущем языке игры.

## Decisions

[D-0042-01, D-0042-04, D-0042-11, D-0042-13, D-0042-15](../../Documentation.md#решения-пользователя).

## Assumptions

Сохраняем два текущих языка и используемые placeholders. Тексты подсказок меняются только для нового маршрута, боевой скрипт и награды не меняются.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | TacticalBattle.ToMap, Tutorial.Line.OverVictory | Добавить согласованные ключи и исправить подпись старого маршрута в репликах об исходах. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | TacticalBattle.ToMap, Tutorial.Line.OverVictory | Синхронизировать набор ключей и placeholders. |
| `tests/DeepSpaceSaga.Client.Tests/PostbattleResults/EP0042Us0002Tk0004Tests.cs` | новый | Проверить наличие и согласованность ключей. |

## Public API after the change

No API change. Новый TacticalBattle.Results используется тактической кнопкой; существующий TacticalBattle.ToMap сохраняется для финального выхода. Согласованный префикс BattleResults.* для заголовков, прогресса, чисел и предупреждений.

## Implementation steps

1. Внести «Результаты боя» / “Battle results”. Слова в подсказках итогового тактического экрана указывают на этот обязательный шаг.
2. Добавить строки цены, шанса, размера, демонтажа, ареста, частичного сбора, успеха, провала, отсутствия места, трюма и кают, блокировки, повторной записи и подтверждения.
3. Согласовать употребление ключей с клиентскими тикетами US-0001–0006; сопоставить плейсхолдеры числа и единицы измерения в обеих локалях.

## Out of scope

Новые расчёты, изменение учебных правил, содержание баланса.

## Invariants

Ключи и placeholders совпадают. Повторное использование TacticalBattle.ToMap сохраняет его текущий смысл, а русская кнопка итогового экрана точно называется «Результаты боя».

## Tests

Будущая проверка, не запускалась. `BattleResultsLocalization_RuEnKeysAndPlaceholders`. Запуск из корня DSS, process-tree cap 300000 ms.

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0042/us0002-tk0004-' + (Get-Date -AsUTC -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $run | Out-Null
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~BattleResultsLocalization_RuEnKeysAndPlaceholders' --results-directory $run
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
```

## Definition of Done

Обе локали покрыты тестом, валидны, одинаковы по ключам и placeholders, сборка упаковывает актуальные строки. Ручная приёмка входит в эпик.

## Self-containment check

Зависимостей на реализацию нет. В перечне только два каталога строк и один тест.

## Исполнение

- Добавлены согласованные строки двух локалей. Итоговые реплики обучения направляют в «Результаты боя».
- Сборка Client Tests, scoped whitespace format и `git diff --check` прошли. Проверка каталогов запланирована на завершение US-0002.
