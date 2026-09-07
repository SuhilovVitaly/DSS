# Trade

Статус: панель действия (правый нижний фрейм) реализована — покупка/продажа cargo-товаров и заправка топлива работают через существующий Engine/Contracts слой торговли.

Код: `src/DeepSpaceSaga.Client/UI/Screens/Trade/` (`TradeScreen.cs`, `TradeLayout.cs`).

## Назначение

Экран торговли открывается со станционного экрана, является modal screen и позволяет покупать, продавать и заправлять двигатель.

## Функциональность первого релиза

- Просмотр рынка станции (`Resources`/`Goods`) и панель действия для покупки/продажи/заправки — см. «Статус реализации (MVP)» ниже.

## Связанные механики

- `StationInventory`.

## UI-решение: панель действия (правый нижний фрейм)

Правый нижний фрейм (`TradeLayout`/`TradeScreen`: `_rightPanelLower`, рядом с гридом `Modules`) — это панель действия, а не ещё одна таблица. Слева игрок уже видит рынок (гриды `Resources`/`Goods`/`Modules`); справа ему нужно быстро понять: что он покупает/продаёт, сколько, сколько это стоит, может ли подтвердить.

Верхняя серая шапка (титлбар `_rightPanelLowerTitleBar`) называется по выбранному товару: `Steel`, `Fuel`, `Energy Cells` и т.д. Если товар не выбран — `Select item`.

Расположение содержимого фрейма сверху вниз:

1. Название товара и категория (Resource/Good).
2. Две строки рыночной информации (цена станции, остаток на складе/в трюме).
3. Переключатель `Buy` / `Sell`.
4. Выбор количества: `-`, поле числа, `+`, `Max`.
5. Итог сделки: цена, изменение кредитов, изменение груза.
6. Внизу — крупная кнопка подтверждения.

Состояния:

- Товар не выбран: титлбар — `Select item` (`Trade.SelectItemTitle`), тело фрейма — `Select an item from STATION INVENTORY` (`Trade.SelectItemPrompt`).
- Нельзя купить: кнопка подтверждения `disabled`, причина рядом мелким текстом — `Not enough credits` (`Trade.ReasonInsufficientPlayerCredits`) / `No cargo space` (`Trade.NoCargoSpace`, только для не-`Fuel`: контейнерный модуль полностью заполнен, `AvailableCapacityKg <= 0`).
- Нельзя продать: `You have none in cargo` (`Trade.NoneInCargo`, когда в трюме 0 единиц товара).
- Прочие отклонения Engine (`CargoCapacityExceeded`, `FuelCapacityExceeded`, `InsufficientStationStock`, `InsufficientCargoQuantity`, `InvalidQuantity`, `InvalidPackageQuantity` и т.д.) клиент не предсказывает проактивно — они всплывают реактивно: `TradeScreen` запоминает `CommandId` последней отправленной команды и сверяет его с `CommandResults` следующего снапшота (см. `GameSessionHandle.SendTradeCommand`), показывая причину тем же способом, пока не будет отправлена новая команда или не сменится выбор/режим.
- Выбранная строка слева (в `Resources`/`Goods`/`Modules`) подсвечивается, чтобы было понятно, к чему относится правый фрейм — уже реализовано через `_selectedResourceItemTypeId`/`_selectedGoodItemTypeId`/`_selectedModuleItemTypeId`.

Маршрутизация команд (из существующего Engine/Contracts слоя, `TradeCommandTypes`): `Buy`/`Sell` товара из `Resources`/`Goods` уходят на модуль `module.container` (`trade.buy`/`trade.sell`); покупка `item.fuel` — это всегда `trade.refuel` на модуль `module.engine`, а не `trade.buy` (заправка не занимает место в трюме) — обратной продажи топлива не существует, поэтому для `Fuel` переключатель `Sell` недоступен (заблокирован в UI).

Шаг количества (`-`/`+`) и размер пакета продажи — по категории товара (`StationInventoryItemSnapshot.Category`): `Resource` = 100, `Good` (включая `Fuel`) = 10. `Max`: Buy не-`Fuel` = `min(floor(Кредиты/ЦенаЗаЕдиницу), ОстатокНаСтанции)`, кнопка не действует, если контейнер полностью заполнен; Buy `Fuel` дополнительно ограничен свободной ёмкостью бака; Sell = `min(КоличествоВТрюме, MaxSellableQuantity)`, округлено вниз до кратного размеру пакета.

## Статус реализации (MVP)

Реализовано:

- Открытие кнопкой `TRADE` на `StationScreen` (`ScreenEvent.OpenTrade`, вложенный modal поверх `StationScreen`), закрытие `×`/`Escape`/кликом по фону вне панели, панель `1400×800`, modal pause через `PushModalAsync`/`PopModalAsync`.
- Гриды `Resources`/`Goods` — реальные данные со станции (`DockedStationTrade.Items`), сортировка по колонкам, скролл, выбор строки (взаимоисключающий между тремя гридами).
- Панель действия (правый нижний фрейм): название/категория выбранного товара, две строки рыночной информации, переключатель `Buy`/`Sell` (заблокирован на `Buy` для `Fuel`), степпер количества (`-`/`+`/`Max`), итог сделки (цена, изменение кредитов, изменение груза), кнопка подтверждения с проактивной/реактивной блокировкой и текстом причины.
- Отправка команд: `TradeScreen` получает `GameSessionHandle` (`SkiaWindow.OpenTradeAsync` передаёт `_session`) и вызывает `GameSessionHandle.SendTradeCommand` — `trade.buy`/`trade.sell` на модуль `module.container`, `trade.refuel` на `module.engine` для `Fuel`.

Не реализовано:

- Грид `Modules` — placeholder без данных: модели «модуль на продажу у станции» в Engine/Contracts пока нет (`ResolveModuleRows`).
- Верхний правый фрейм (`_rightPanelUpper`) — заглушка (белая рамка + серый титлбар без содержимого), этот батч его не трогал.
- Точный предиктивный расчёт превышения массы груза при покупке (`ItemTypeDefinition.UnitMassKg` не выражен в `DeepSpaceSaga.Contracts`) — обрабатывается реактивно через `CommandResults`/`CargoCapacityExceeded`, а не проактивной блокировкой кнопки.
- Свободный ввод количества с клавиатуры в поле — количество меняется только степпером (`-`/`+`/`Max`).
