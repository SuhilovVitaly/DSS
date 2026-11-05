---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0005-session-contracts
ticket: EP-0015-US-0005-TK-0001-contracts
title: contracts
stage: draft
layer: contracts
depends_on: ["EP-0015-US-0004-TK-0004-golden-battles"]
files_touched: 5
serves: [AC-01]
created: 2026-11-05
revision: 3
---

# contracts

## Why
Неизменяемые DTO боя.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `DB/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `DB/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs` | new | Снимок и вложенные DTO, типизированные причины и события |
| `src/DeepSpaceSaga.Contracts/TacticalBattleOptions.cs` | new | Options DTO, доступность |
| `src/DeepSpaceSaga.Contracts/TacticalBattleRequests.cs` | new | Start/Move/Fire/Exit, ответ, коды отказа |
| `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs` | снимок мира | `IsTacticalBattleActive` |
| `tests/DeepSpaceSaga.Contracts.Tests/TacticalBattleContractTests.cs` | new | Roundtrip, неизменяемость, полнота полей |

## Public API after the change
Records в Contracts.

## Implementation steps
1. Поля AC-01 полностью; роли, тактики, рамы — id, не русские строки.
2. Журнал — enum события + параметры + ход + фаза, без готовых строк.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `DB/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Contracts.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-01 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `DB/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
