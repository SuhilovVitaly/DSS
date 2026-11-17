---
epic: EP-maintenance
story: EP-maintenance-US-0024-company-service-feedback
ticket: EP-maintenance-US-0024-TK-0002-visible-equipment
title: Состав предложения и установленное оснащение
stage: approved
implementation_status: implemented
layer: client
serves: [AC-02]
---
# Состав предложения и установленное оснащение

## Source and grounding
Связанный исходный тикет: DB/Board/EP-0033-demo-company-services/EP-0033-US-0003-drone-replacement/EP-0033-US-0003-TK-0004-client/EP-0033-US-0003-TK-0004-client.md.
ShipScreen.DrawSupply использует cached response перед campaign snapshot; DrawShipyard не выводит ReplacementOffer.Items, ShipViewModel.DroneRow не содержит оснащение.

## Code context / allowlist
- `src/DeepSpaceSaga.Client/UI/Screens/Ship/ShipScreen.Replacement.cs`
- `src/DeepSpaceSaga.Client/UI/Screens/Ship/ShipViewModel.cs`
- `src/DeepSpaceSaga.Client/Data/Locale/English.json`
- `src/DeepSpaceSaga.Client/Data/Locale/Russian.json`
- `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0033Us0003Tk0004Tests.cs`

Matching test project: tests/DeepSpaceSaga.Client.Tests. Board status/evidence этой истории разрешены отдельно.

## Public API after change
Gameplay API не меняется. Представление читает существующие SupplySnapshot, ReplacementOffer.Items и OwnedDroneSnapshot.

## Implementation steps
Передать реальные module IDs в DroneRow. Показать локализованный состав предложения и установленное оснащение выбранного дрона, без пересечения кнопок; обновлять через существующий live carrier callback.

## Invariants / out of scope
Engine владеет деньгами и оснащением. Render не отправляет команды. Не менять цены, каталог, правила ремонта, покупки, перезарядки или сохранения.

## Tests / Definition of Done
Добавить регрессию в именованный test file; focused Client suite и owning Release build, scoped format, diff check. Все тестовые процессы ограничить 300000 ms; outputs: D:/DeepSpaceSaga/artifacts/validation/ep-0033-review/20261117-fixes/. Проверки выполняются в конце истории. Перед push записать результаты и перенести завершённый тикет в Done вместе с историей.

## Результат и проверки
Показаны локализованные Items каждого предложения и фактические frame/weapon/armor/decoy/shield/engine выбранного дрона; повторяющиеся модули сгруппированы с количеством. Неизвестные IDs не теряются. Carrier revision обновляет оснащение после подтверждения. Кнопки перенесены вместе с hit testing.

58/58 focused Client tests PASS, Release build PASS: D:/DeepSpaceSaga/artifacts/validation/ep-0033-review/20261117-fixes/final2.log. Filter: FullyQualifiedName~DemoEp0033|FullyQualifiedName~Ep0029Us0009Tk000. Scoped dotnet format whitespace PASS (format.log/format.err); diff check PASS. OfferContentsAndInstalledEquipmentFollowConfirmedCarrier проверяет RU/EN, полный состав, переход оснащения и клик четвёртого предложения. Headless screenshots screens/shipyard-Russian.png и shipyard-English.png просмотрены; переносы не закрывают кнопки. Native UI в этом исправлении не запускался; прежние external/product gates остаются открытыми.
