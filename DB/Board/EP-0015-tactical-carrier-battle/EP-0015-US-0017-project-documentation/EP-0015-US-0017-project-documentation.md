---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0017-project-documentation
title: "Документация проекта"
stage: draft
dependencies: [EP-0015-US-0001-content-and-loader, EP-0015-US-0002-deterministic-core, EP-0015-US-0003-battle-rules, EP-0015-US-0004-battle-ai, EP-0015-US-0005-session-contracts, EP-0015-US-0006-client-input-and-controls, EP-0015-US-0007-launch-and-setup, EP-0015-US-0008-battle-map, EP-0015-US-0009-fire-and-results, EP-0015-US-0010-battle-end, EP-0015-US-0011-legacy-removal-prep, EP-0015-US-0012-legacy-client-removal, EP-0015-US-0013-legacy-engine-removal, EP-0015-US-0014-legacy-foundation-removal, EP-0015-US-0015-localization, EP-0015-US-0016-balance-and-native-acceptance]
---

# Документация проекта

## Связь с техническим заданием

Источник правил, данных и эталонов — `VData/Planning/V4.0` (Spec, Data, Reference). Решения и gaps — [Documentation.md](../Documentation.md). Требования ниже — целевой результат, не утверждение о реализации.

## User story

Как участник проекта, я нахожу в документации действующий тактический бой V4 и не встречаю описаний старого боя как действующего контракта.

## Зависимости

[EP-0015-US-0001-content-and-loader](../EP-0015-US-0001-content-and-loader/EP-0015-US-0001-content-and-loader.md), [EP-0015-US-0002-deterministic-core](../EP-0015-US-0002-deterministic-core/EP-0015-US-0002-deterministic-core.md), [EP-0015-US-0003-battle-rules](../EP-0015-US-0003-battle-rules/EP-0015-US-0003-battle-rules.md), [EP-0015-US-0004-battle-ai](../EP-0015-US-0004-battle-ai/EP-0015-US-0004-battle-ai.md), [EP-0015-US-0005-session-contracts](../EP-0015-US-0005-session-contracts/EP-0015-US-0005-session-contracts.md), [EP-0015-US-0006-client-input-and-controls](../EP-0015-US-0006-client-input-and-controls/EP-0015-US-0006-client-input-and-controls.md), [EP-0015-US-0007-launch-and-setup](../EP-0015-US-0007-launch-and-setup/EP-0015-US-0007-launch-and-setup.md), [EP-0015-US-0008-battle-map](../EP-0015-US-0008-battle-map/EP-0015-US-0008-battle-map.md), [EP-0015-US-0009-fire-and-results](../EP-0015-US-0009-fire-and-results/EP-0015-US-0009-fire-and-results.md), [EP-0015-US-0010-battle-end](../EP-0015-US-0010-battle-end/EP-0015-US-0010-battle-end.md), [EP-0015-US-0011-legacy-removal-prep](../EP-0015-US-0011-legacy-removal-prep/EP-0015-US-0011-legacy-removal-prep.md), [EP-0015-US-0012-legacy-client-removal](../EP-0015-US-0012-legacy-client-removal/EP-0015-US-0012-legacy-client-removal.md), [EP-0015-US-0013-legacy-engine-removal](../EP-0015-US-0013-legacy-engine-removal/EP-0015-US-0013-legacy-engine-removal.md), [EP-0015-US-0014-legacy-foundation-removal](../EP-0015-US-0014-legacy-foundation-removal/EP-0015-US-0014-legacy-foundation-removal.md), [EP-0015-US-0015-localization](../EP-0015-US-0015-localization/EP-0015-US-0015-localization.md), [EP-0015-US-0016-balance-and-native-acceptance](../EP-0015-US-0016-balance-and-native-acceptance/EP-0015-US-0016-balance-and-native-acceptance.md)

## Completion evidence

- **AC-01.** `DocumentationImpact.md`: по итоговому diff, API, командам, конфигурации и версиям схем — документ → изменение → обновление → проверка.

- **AC-02.** Новый контракт: `04-Engineering/TacticalBattle.md` (правила, ИИ, данные, детерминизм, правила C#-семантики — по `VData/Planning/V4.0/Spec`, числа ссылками на JSON); экраны подготовки и боя в `02-FirstRelease/Screens` и `ScreenCatalog`; `GameSession.md` и `TacticalMapAndManeuvering.md` (F8, клавиши во время боя); `EngineRequirements` §Бой и §Сохранение; `SaveFormat` (Save20); `06-Tooling` (v4_update, CarrierWingSim, LongRunning).

- **AC-03.** Описания старого боя **удалены или переписаны** по `DocumentationSystem.md` (отменённое убирается, история — в Git), без «пометок замены»: `PointAimCombat.md`, `CombatReports.md`, `DebugTuning.md`, `DebugTuningPanel.md`, `ManualManeuver.md`, `NpcObservedTrajectories.md`, `FirstReleaseRequirements.md`, `CommandPanels.md`, `Save.md`, `Defeat.md`, `TacticalMapSpecification.md`, `06-Tooling/GameSessionScreenUI.md`, `DB/Documentation/README.md`, `00-Process/CLAUDE.md` (архитектура combat DTO и reader).

- **AC-04.** Board: EP-0006…EP-0014 — запись «Superseded by EP-0015» с датой в Documentation.md; EP-0015 — статусы, `AcceptanceCoverage.md`, `ExecutionStatus.md` (тикет → SHA → push → remote). Graphify (`06-Tooling/GraphifyRebuild.md`): `src/graphify-out` в `.gitignore`, граф перестраивается локально на VDI исполнения после финальных изменений, результат записывается в ExecutionStatus (без отдельного коммита графа). Ссылки, ID, количества и stale references проверены.

Это план будущих наблюдений, а не выполненная проверка. Automated/render, native/input и product/human acceptance фиксируются раздельно.

## Ограничения и gaps

Датированные результаты прошлых эпиков не переписываются.

## Тикеты

| Ticket | Layer | Story criteria |
|---|---|---|
| [EP-0015-US-0017-TK-0001-impact-inventory](EP-0015-US-0017-TK-0001-impact-inventory/EP-0015-US-0017-TK-0001-impact-inventory.md) | documentation | AC-01 |
| [EP-0015-US-0017-TK-0002-documentation-new-contract](EP-0015-US-0017-TK-0002-documentation-new-contract/EP-0015-US-0017-TK-0002-documentation-new-contract.md) | documentation | AC-02 |
| [EP-0015-US-0017-TK-0003-documentation-legacy-rewrite](EP-0015-US-0017-TK-0003-documentation-legacy-rewrite/EP-0015-US-0017-TK-0003-documentation-legacy-rewrite.md) | documentation | AC-03 |
| [EP-0015-US-0017-TK-0004-board-sync](EP-0015-US-0017-TK-0004-board-sync/EP-0015-US-0017-TK-0004-board-sync.md) | documentation | AC-04 |

## Historical planning review

Draft; human approval отсутствует. Решения и сообщения согласования записываются в review log эпика.
