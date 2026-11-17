---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0017-world-integration
title: Единый шаг мира для встреч и существующих систем
stage: approved
approval_kind: workflow
implementation_status: implemented
dependencies: [EP-0033-US-0005-crew-upkeep]
source_request: 'Проверь весь 35 эпик после грумминга: нет ли противоречий, пропущенных требований и проблем в зависимостях тикетов. Найденное исправь, пока без реализации.'
current_review: complete
revision: 1
created: 2026-11-17T13:00:31Z
---

# Единый шаг мира для встреч и существующих систем

## User story / Why
Как игрок, я получаю обнаружение, паузу и перехват в одном авторитетном порядке при любой скорости симуляции, а существующие ремонт, снабжение и содержание экипажа продолжают работать. Это техническая интеграция уже принятых D-35-19/21/33/42/43/45/47, без нового баланса или сюжета.

## Grounding / audit decision
SimulationEngine.StationRepair.cs:12,15 уже реализует partial validation/command hooks; SimulationEngine.CrewUpkeep.cs:46 — единственную реализацию tick hook. Вторые partial implementations не компилируются. SimulationEngine.cs:694,723 и1125,1143 вызывают world advance и campaign tick; SimulationEngine.EconomyTime.cs:13 владеет AdvanceWorldTo, Time.cs:56 также вызывает его. Поэтому одних новых feature partial files недостаточно. A-35-INTEGRATION: отделить существующие владельцы в единый dispatcher, без reflection и без смены transport.

## Acceptance criteria
- AC-01: один dispatcher команд/валидации/календарного tick; существующие EP33 операции/receipts/upkeep сохраняются.
- AC-02: world advancement из snapshot, ускорения и save использует один motion path; swept detection/capture/safe-zone не пропускаются между endpoints.
- AC-03: на одном шаге новые контакты объединяются; earliest capture/tie-break детерминирован; report/pause и committed capture не отменяют друг друга. World не продвигается в battle/pause и save не создаёт повторный report/settlement.
- AC-04: реальный интеграционный путь подтвержден tests, отдельно native acceptance; API scaffold не означает готовность.

## Ticket map / order
- [TK0001 dispatch](EP-0035-US-0017-TK-0001-campaign-dispatch/EP-0035-US-0017-TK-0001-campaign-dispatch.md): ранняя engine база после EP33; feature handlers подключаются к ней.
- [TK0002 world integration](EP-0035-US-0017-TK-0002-world-step/EP-0035-US-0017-TK-0002-world-step.md): после owning gameplay и report tickets; перед recovery acceptance и финальной документацией.

## Scope / non-goals / gaps
Только integration существующих правил; не новые gameplay решения, не переписывание motion/economy, не реализация при grooming. Точные имена helper API — проект. При source drift расширять decomposition до production, не обходить лимит5files. Product gaps не добавлены. Implementation NOT STARTED; tests/build/native/product NOT RUN/OPEN. Story-level зависимости указывают раннюю базу; полные integration edges находятся в ticket graph.
