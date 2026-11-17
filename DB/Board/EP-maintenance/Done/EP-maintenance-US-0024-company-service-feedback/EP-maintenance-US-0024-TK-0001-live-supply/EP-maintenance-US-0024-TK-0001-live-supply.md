---
epic: EP-maintenance
story: EP-maintenance-US-0024-company-service-feedback
ticket: EP-maintenance-US-0024-TK-0001-live-supply
title: Свежая проекция снабжения
stage: approved
implementation_status: implemented
layer: client
serves: [AC-01]
---
# Свежая проекция снабжения

## Source and grounding
Связанный исходный тикет: DB/Board/EP-0033-demo-company-services/EP-0033-US-0002-ammunition-supply/EP-0033-US-0002-TK-0004-client/EP-0033-US-0002-TK-0004-client.md.
ShipScreen.DrawSupply использует cached response перед campaign snapshot; DrawShipyard не выводит ReplacementOffer.Items, ShipViewModel.DroneRow не содержит оснащение.

## Code context / allowlist
- `src/DeepSpaceSaga.Client/UI/Screens/Ship/ShipScreen.Supply.cs`
- `src/DeepSpaceSaga.Client/UI/Screens/Ship/ShipScreen.cs`
- `src/DeepSpaceSaga.Client/UI/Screens/Ship/ShipScreen.Replacement.cs`
- `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0033Us0002Tk0004Tests.cs`

Matching test project: tests/DeepSpaceSaga.Client.Tests. Board status/evidence этой истории разрешены отдельно.

## Public API after change
Gameplay API не меняется. Представление читает существующие SupplySnapshot, ReplacementOffer.Items и OwnedDroneSnapshot.

## Implementation steps
Хранить отдельно только DecoySupplyQuote; остатки читать из актуальной campaign projection. Сохранить pending/confirm/reset поведение.

## Invariants / out of scope
Engine владеет деньгами и оснащением. Render не отправляет команды. Не менять цены, каталог, правила ремонта, покупки, перезарядки или сохранения.

## Tests / Definition of Done
Добавить регрессию в именованный test file; focused Client suite и owning Release build, scoped format, diff check. Все тестовые процессы ограничить 300000 ms; outputs: D:/DeepSpaceSaga/artifacts/validation/ep-0033-review/20261117-fixes/. Проверки выполняются в конце истории. Перед push записать результаты и перенести завершённый тикет в Done вместе с историей.

## Проверки
Регрессия ResupplyResponseDoesNotHideLaterAmmunitionSnapshot падает до исправления (red.log), после исправления 3/3 теста DemoEp0033Us0002Tk0004Tests PASS (supply.log); Release build выполнен в test. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0033-review/20261117-fixes/. Native не запускался.
