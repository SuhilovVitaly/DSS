---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0001-content-and-loader
ticket: EP-0015-US-0001-TK-0001-content-data
title: content-data
stage: draft
layer: content-data
depends_on: []
files_touched: 15
serves: [AC-01, AC-02]
created: 2026-11-05
revision: 3
---

# content-data

## Why
Перенести данные V4 в контент игры тем же деревом, чтобы модули монтировались на рамы дронов по одному файлу.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/TacticalBattle/{rules,ai,pilots,layouts,scenarios}.json` | new | Копия корневых файлов V4.0/Data |
| `src/DeepSpaceSaga.Client/Data/TacticalBattle/Frames/{carrier,heavy,light,thug}.json` | new | Рамы |
| `src/DeepSpaceSaga.Client/Data/TacticalBattle/Modules/{Weapons,Engines,Shields,Armor}/*.json` | new | 20 модулей по файлу (имена — как в V4.0/Data) |
| `src/DeepSpaceSaga.Client/Settings.json` | typeData без боя V4 | `typeData.tacticalBattle` |
| `src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj` | копирует Data/** | Проверить копирование `Data/TacticalBattle/**` в output |

## Scope correction before implementation — 2026-11-05

D-USER-14/15: источник — Markdown `VData/Planning/V4.0`; JSON восстановить из таблиц, справочная реализация не требуется. Добавлены в Code context: `VData/Planning/V4.0/**/*.md` (существующие 14 источников — включение в Git), `tools/tactical-battle/Export-Content.ps1` (новый воспроизводимый экспорт; причина — отсутствие исходных JSON и Python), `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ContentDataTests.cs` (новая проверка settings/output и состава данных). Существующее рекурсивное копирование Data JSON в Client.csproj сохраняется, если проверка подтвердит его достаточность.

## Public API after the change
Новых API нет; контракт — структура и поля `VData/Planning/V4.0/Data/README.md`.

## Implementation steps
1. Первым шагом проверить наличие `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07).
2. Скопировать дерево `VData/Planning/V4.0/Data` без md (29 JSON — исключение из лимита файлов R-03: это данные, не код).
3. Добавить ключ в Settings.json.
4. Значения, имена полей и порядок ключей не менять.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests — файлы в output, ключ Settings. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-01, AC-02 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
