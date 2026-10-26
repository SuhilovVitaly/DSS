# Торговля

Покупка, продажа и заправка используют текущую станционную quote, quantity и authoritative результат. Учитываются доступный cargo, бак, inventory и бюджет. Pending/error не равны выполненной сделке. Отправка использует адрес модуля/объекта и текущий визит, не локальное списание Client.

См. [каталог экранов](ScreenCatalog.md) и [основные требования](../../01-Requirements/EngineRequirements.md).

Логическая панель1600×800 целиком равномерно вписывается в доступное окно
без увеличения сверх1×. Toolbar/body/tooltip используют единый transform;
mouse hit-test применяет обратный transform. Геометрия в1920×1080 сохраняется,
в1280×720 и1024×768 панель остаётся в viewport вместе с item/exit input.

Источники: [TradeScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeScreen.cs).
