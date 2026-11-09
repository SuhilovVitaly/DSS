---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0011-build-slots
title: Слоты сборки дрона в снимке
stage: done
dependencies: [EP-0020-US-0009-anomaly-resolution]
created: 2026-11-08T11:46:28Z
source_request: "Передавать клиенту полный список слотов рамы каждого дрона — пушка, плиты, щиты, двигатели, модуль — с установленным модулем и состоянием (исправен, заклинило, повреждён ×N), чтобы интерфейс рисовал структуру рамы без знания правил V4."
current_review: epic
revision: 1
---

# Слоты сборки дрона в снимке

## Входное техническое задание

D-41, A-36. Снимок — `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs:30-41` (`TacticalEngineSnapshot`, `TacticalBuildSnapshot`, `TacticalUnitSnapshot` с `Jammed`, `Engines`, `Limits`); построение — `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs:20-33`; рама — `src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleDefinitions.cs:16-18` (`EngineSlots`, `ShieldSlots`, после M2 — `ModuleSlots`).

## User story

Как игрок, я хочу видеть, из чего собран мой дрон и что в нём повреждено, — для этого клиент должен получать структуру рамы целиком.

## Dependencies

[EP-0020-US-0009-anomaly-resolution](../EP-0020-US-0009-anomaly-resolution/EP-0020-US-0009-anomaly-resolution.md)

## Completion evidence

- `TacticalUnitSnapshot.Slots` для каждого дрона; носители — пусто.
- `TacticalBuildSnapshot.EngineSlots`, `ModuleSlots`; старые нагрузки читаются.

## Acceptance criteria

- **US11-AC1.** `TacticalSlotSnapshot(TacticalSlotKind Kind, int Index, string? ModuleId, string? ModuleName, TacticalSlotState State, double Factor = 1)`; `TacticalSlotKind { Weapon, Armor, Shield, Engine, Module }`, `TacticalSlotState { Ok, Jammed, Damaged }`; `TacticalUnitSnapshot.Slots` (init, по умолчанию пусто); `TacticalBuildSnapshot.EngineSlots`, `ModuleSlots` (init).
- **US11-AC2.** Порядок списка: пушка (1), плиты (1), щиты `0…ShieldSlots−1`, двигатели `0…EngineSlots−1`, модули `0…ModuleSlots−1`. Слот без модуля — `ModuleId = null`. Пушка с `Jammed` — `Jammed`; двигатель с множителем < 1 — `Damaged` и `Factor`; остальное — `Ok`. Носитель — пустой список.
- **US11-AC3.** Значения совпадают с прежними полями снимка (`Build`, `Engines`, `Jammed`, `DecoyCharges`) на всех дронах боёв seed 1…20 во всех фазах; исходы боёв и RNG не меняются.

## Non-goals

Изменение правил слотов пушки и плит (остаются ровно по одному), отрисовка (US-0012).

## Invariants

Снимок только добавляет данные; движок и исходы не меняются.

## Решения и согласование

Исполнение всего эпика разрешено текущим поручением пользователя.

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0020-US-0011-TK-0001-slot-contracts](EP-0020-US-0011-TK-0001-slot-contracts/EP-0020-US-0011-TK-0001-slot-contracts.md) | Контракт слотов | contracts | `EP-0020-US-0009-TK-0005-anomaly-regression` | US11-AC1 |
| [EP-0020-US-0011-TK-0002-slot-snapshot](EP-0020-US-0011-TK-0002-slot-snapshot/EP-0020-US-0011-TK-0002-slot-snapshot.md) | Заполнение слотов в снимке | engine | `EP-0020-US-0011-TK-0001-slot-contracts` | US11-AC2, US11-AC3 |

## Исполнение

US11-AC1: Contracts32PASS; US11-AC2/3: SlotSnapshot24PASS,20seeds все фазы,5000полных direct/explicit подписей и5000нормализованных byte-exact к US9. UI34PASS/12растров неизменны. Save20 не менялся; Slots являются эфемерным DTO. Самопроверка выполнена; независимая приёмка не заявляется. Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us11-tk2/`.
