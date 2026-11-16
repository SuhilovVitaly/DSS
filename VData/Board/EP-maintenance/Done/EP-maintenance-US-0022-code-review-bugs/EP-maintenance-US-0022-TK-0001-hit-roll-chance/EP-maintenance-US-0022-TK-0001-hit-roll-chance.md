---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0001-hit-roll-chance
title: Бросок попадания показан рядом с другим шансом
stage: approved
approval_kind: workflow
implementation_status: implemented
severity: major
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
serves: [AC1]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Бросок попадания показан рядом с другим шансом

[История](../EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

Подпись итога выстрела показывает тот шанс, с которым движок сравнил бросок попадания: при неудачном броске он не меньше шанса, при попадании меньше шанса.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-maintenance-US-0016-TK-0004/TK-0005 (вошло в merge #113). Серьёзность: major. Уверенность: CONFIRMED.

**Дефект.** Движок сравнивает `HitRoll` с условным шансом (`hit = hitRoll < chance`), а в снимок выстрела кладёт `effectiveChance = chance * (1 - Absorption)`. Клиент печатает `Map.ResultChance` по `shot.Chance` (эффективный шанс) рядом с `Map.HitRoll`.

**Сценарий.** Цель в тепловой аномалии, Absorption 0,2, условный шанс 0,50, бросок поглощения не сработал, бросок попадания 0,45. Движок засчитывает попадание, подпись: «Шанс попадания 40% · Бросок 45%», то есть попадание при броске выше шанса. Расхождение возникает при Absorption > 0 и не поглощённом выстреле.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs:172-189` | Условный `chance`, `hit = hitRoll < chance`, в снимок идёт `effectiveChance`. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.FireTemplates.cs:178-185` | Подпись `Map.ResultChance` по `shot.Chance` плюс `Map.HitRoll`. |
| `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs` | Добавлено при исполнении (allowlist расширен с согласия пользователя): `TacticalShotSnapshot.ConditionalChance`. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs` | Добавлено при исполнении: перенос `ConditionalChance` в снимок. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleResults.cs` | Добавлено при исполнении: для выстрела, отклонённого чужой ловушкой, `ConditionalChance` скрывается, чтобы подпись не раскрывала ловушку. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/battle-data-seed1.txt` | Добавлено при исполнении: эталон дополнен полем `ConditionalChance` у каждого выстрела. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. Публиковать в снимке выстрела условный шанс (например, `ConditionalChance`) и печатать рядом с `HitRoll` именно его. Эффективный шанс оставить там, где он объясняет общий исход с учётом поглощения. Детерминизм и порядок бросков не менять.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

Engine.Tests: снимок выстрела с Absorption > 0 содержит условный шанс, `Hit == HitRoll < ConditionalChance`. Client.Tests (`TabletopSalvoTests`): выстрел с `effective < HitRoll < conditional` показывает попадание и шанс выше броска.

## Definition of Done

- AC1 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Реализован 2026-11-16 на ветке `maintenance/US-0022-code-review-bugs` (от main `d67d538e`). Дефект на `d67d538e` подтверждён.

- Engine: `TacticalShot.ConditionalChance` — шанс с учётом ловушки и без поглощения, с которым сравнивается `HitRoll`; порядок бросков не изменён.
- Contracts: `TacticalShotSnapshot.ConditionalChance` (`double?`, null в старых снимках).
- Client: подпись итога печатает `ConditionalChance` рядом с броском; у поглощённого выстрела остаётся эффективный шанс. Для выстрела, отклонённого чужой ловушкой, поле скрывается вместе с `ChanceWithoutDecoy`.
- Эталон `battle-data-seed1.txt` дополнен полем вручную; при расхождении перегенерировать через `DSS_EP0019_CAPTURE_REFERENCE=1`.

Тесты: `AnomalySalvoTests.Hit_roll_is_compared_with_published_conditional_chance`, `TabletopSalvoTests.Hit_roll_is_printed_next_to_the_conditional_chance_it_was_compared_with`.

Проверки: `dotnet build` для Engine.Tests, Client.Tests, Contracts.Tests — 0 предупреждений, 0 ошибок. Тесты по решению пользователя запускаются один раз после завершения истории; результат записан в документе истории.
