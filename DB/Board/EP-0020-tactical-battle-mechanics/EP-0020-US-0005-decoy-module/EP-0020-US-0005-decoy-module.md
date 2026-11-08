---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0005-decoy-module
title: Модуль ловушек: данные и сборка дрона
stage: draft
dependencies: [EP-0020-US-0002-evasion-resolution]
created: 2026-11-08T11:46:28Z
source_request: "Добавить модуль ловушек: слот `moduleSlots` у рам дронов, модуль `decoy.t1` (2 заряда, промах ×1,5, масса 10 кг) во всех компоновках дронов, порог ИИ 0,4 в `rules.json`; масса модуля входит в массу дрона."
current_review: epic
revision: 1
---

# Модуль ловушек: данные и сборка дрона

## Входное техническое задание

D-15, D-20, D-21, A-13–A-15. Рамы и модули — `src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleDefinitions.cs:16-25,37-38`; загрузчик — `TacticalBattleContentLoader.cs:59-60,236-273`; сборка дрона — `TacticalBattleAssembly.cs:48-58`; масса — `TacticalLimits.Of` (`src/DeepSpaceSaga.Motion/TacticalBattle/TacticalLimits.cs:9-10`); таблицы данных — `VData/Planning/V4.0/Data/README.md`, `VData/Planning/V4.0/Data/Frames/README.md`, `VData/Planning/V4.0/Data/Modules/*/README.md`; экспортёр — `tools/tactical-battle/Export-Content.ps1:70-71,95`; синхронность — `ContentSyncTests.cs:51`.

## User story

Как игрок, я хочу, чтобы у моих дронов был модуль ловушек с ограниченным числом зарядов, чтобы в нужный момент сбить прицел противнику.

## Dependencies

[EP-0020-US-0002-evasion-resolution](../EP-0020-US-0002-evasion-resolution/EP-0020-US-0002-evasion-resolution.md)

## Completion evidence

- Папка `Modules/Decoys/` с `decoy.t1.json` и таблицей README; `moduleSlots` у всех рам; `module` во всех компоновках дронов; блок `decoy` в `rules.json`; экспортёр и `ContentSyncTests` синхронны.
- У каждого дрона в начале боя 2 заряда; масса модуля учтена в пределах движения; снимок передаёт модуль и заряды; ожидания исторических тестов обновлены с записанной причиной «масса модуля».

## Acceptance criteria

- **US5-AC1.** `BattleDecoy(Id, Name, Tier, Charges, MissFactor, MassKg)` читается из `Modules/Decoys/<id>.json`; `BattleFrame.ModuleSlots` (по умолчанию 0); `BattleLayout.Module` (`string?`); `rules.json` получает блок `decoy {aiThreatChance 0.4}`. Загрузчик отвергает неизвестный модуль, модуль при `moduleSlots = 0`, модуль у носителя, `charges < 1`, `missFactor < 1`, отрицательные `massKg`/`tier`, `aiThreatChance` вне [0, 1]. `ContentSyncTests` и `Export-Content.ps1 -Check` проходят.
- **US5-AC2.** Данные: `decoy.t1` — «Ловушки I», tier 1, charges 2, missFactor 1.5, massKg 10; `moduleSlots: 1` у рам `light`, `heavy`, `thug`, `0` у `carrier`; `module: decoy.t1` во всех компоновках дронов обеих сторон.
- **US5-AC3.** `TacticalDrone.Decoy` и `DecoyCharges` (= `Charges` модуля при сборке, 0 без модуля); масса модуля входит в `TacticalLimits.Of` новым параметром `moduleMass`; поток RNG сборки прежний.
- **US5-AC4.** `TacticalBuildSnapshot.DecoyId`, `DecoyName` и `TacticalUnitSnapshot.DecoyCharges` (`int?`, init; `null` у носителей); старые нагрузки без полей читаются.

## Non-goals

Пуск и эффект ловушек (US-0006), выбор модуля на экране подготовки боя, другие типы модулей.

## Invariants

Тактический бой не сохраняется (Save20 не меняется). Масса модуля меняет пределы движения и потому исходы боёв; ожидания исторических тестов обновляются в TK-0002 только с этой записанной причиной.

## Решения и согласование

Ожидает утверждения эпика пользователем (см. [открытые вопросы](../Documentation.md#открытые-вопросы)).

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0020-US-0005-TK-0001-decoy-content](EP-0020-US-0005-TK-0001-decoy-content/EP-0020-US-0005-TK-0001-decoy-content.md) | Модуль ловушек, слот рамы и порог ИИ в данных | content-data | `EP-0020-US-0001-TK-0001-rules-content` | US5-AC1, US5-AC2 |
| [EP-0020-US-0005-TK-0002-decoy-state](EP-0020-US-0005-TK-0002-decoy-state/EP-0020-US-0005-TK-0002-decoy-state.md) | Модуль и заряды в состоянии боя, масса, снимки | engine | `EP-0020-US-0005-TK-0001-decoy-content`, `EP-0020-US-0002-TK-0005-evasion-regression` | US5-AC3, US5-AC4 |
