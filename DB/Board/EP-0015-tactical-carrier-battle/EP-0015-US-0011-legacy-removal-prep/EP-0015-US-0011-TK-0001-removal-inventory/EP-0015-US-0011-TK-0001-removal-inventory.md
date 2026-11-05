---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0011-legacy-removal-prep
ticket: EP-0015-US-0011-TK-0001-removal-inventory
title: removal-inventory
stage: draft
layer: documentation
depends_on: ["EP-0015-US-0010-TK-0002-sandbox-flow"]
files_touched: 1
serves: [AC-01]
created: 2026-11-05
revision: 3
---

# removal-inventory

## Why
Доказательная база удаления.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `DB/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `DB/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `DB/Board/EP-0015-tactical-carrier-battle/RemovalInventory.md` | new | Символ/файл → consumers → решение → тикет → тесты |

## Public API after the change
—

## Implementation steps
1. Поиск по символам, тестам (≈ 109 файлов Client.Tests, 100 Engine.Tests, 24 Contracts.Tests, 13 Motion.Tests ссылаются на старый бой), данным, csproj; загрузка всех сценариев, сохранений и данных.
2. Нарезать тикеты US-0012…US-0014 так, чтобы каждый собирался; обновить их Code context и `files_touched`.
3. Решения AC-01 записать с обоснованием.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `DB/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Проверка полноты инвентаризации по поиску символов. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-01 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `DB/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
