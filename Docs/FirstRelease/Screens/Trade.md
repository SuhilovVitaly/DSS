# Trade

Статус: реализована заглушка (открытие/закрытие/пауза, без данных механик) — по тому же паттерну, что `Station`/`Hire`/`Finance`/`Contracts`/`Ship`. Предыдущий MVP покупки/продажи/заправки снесён перед полным редизайном экрана.

Код: `src/DeepSpaceSaga.Client/UI/Screens/Trade/` (`TradeScreen.cs`, `TradeLayout.cs`).

## Назначение

Экран торговли открывается со станционного экрана, является modal screen и позволяет покупать, продавать и заправлять двигатель.

## Функциональность первого релиза

- Ожидает редизайна — окно является заглушкой без функциональности (см. «Решения первого релиза» в `Docs/FirstReleaseRequirements.md`).

## Связанные механики

- `StationInventory`.

## Статус реализации (MVP)

Реализовано: открытие кнопкой `TRADE` на `StationScreen` (нажатие возвращает `ScreenEvent.OpenTrade`, `SkiaWindow` пушит `TradeScreen` поверх `StationScreen` — вложенный modal, как `GameMenu → Save/Load`), закрытие `×`/`Escape`/кликом по фону вне панели (возврат на `Station`), панель `1400×800`, modal pause через существующий `PushModalAsync`/`PopModalAsync`.

Не реализовано: сама торговля — экран показывает одну placeholder-строку и полностью ожидает редизайна.

## За рамками первого релиза (до редизайна)

- Покупка/продажа cargo-товаров и заправка `Fuel` — предыдущая реализация снесена; Engine/Contracts слой торговли (`StationTradeSnapshot`, `TradeCommandTypes`, `trade.buy`/`trade.sell`/`trade.refuel`) остаётся нетронутым и будет переиспользован новым UI.
