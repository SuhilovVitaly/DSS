---
epic: EP-maintenance
story: EP-maintenance-US-0027-demo-starter-cabins
ticket: EP-maintenance-US-0027-TK-0001-two-living-modules
stage: approved
implementation_status: implemented
layer: content-data
depends_on: []
---
# Два жилых модуля

Убрать `MOD-PLAYER-LIVING-QUARTERS-03` из `src/DeepSpaceSaga.Client/Data/Ships/demo-starter-carrier.json`. Оставить стартовый экипаж и оба остальных жилых модуля. Первый пассажирский заказ в новой игре вмещается в одну свободную каюту. Старое сохранение с третьим жилым модулем сохраняет свою конфигурацию.

Проекты проверок — `DeepSpaceSaga.Client.Tests` и `DeepSpaceSaga.Engine.Tests`. Проверяются стартовая вместимость, арест в последнюю каюту, публикация и выполнение заказа на одного пассажира и загрузка сохранения с тремя модулями.

## Проверки

Целевой прогон клиента — 74/74, двигателя — 21/21. При широком прогоне двигателя прошло 98 из 104 проверок. Остались шесть сбоев в проверках справочника, конфигурации наград и повторов квитанций перевозок. Ручной запуск игры не выполнялся.

[Клиент](<D:/DeepSpaceSaga/artifacts/validation/demo-two-living-modules/20261120-144746-988/client.log>) · [Каюты и перевозки](<D:/DeepSpaceSaga/artifacts/validation/demo-two-living-modules/20261120-145012-837/engine.log>) · [Широкий прогон](<D:/DeepSpaceSaga/artifacts/validation/demo-two-living-modules/20261120-144930-073/engine.log>).
