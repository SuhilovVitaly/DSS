# GameMenu

## Current EP-0012 contract — 2026-10-21

Current writer is SaveFormatVersion18. A real Save17 or earlier is rejected atomically without migration/current-world replacement; NewGame scenario version0 is a separate path. Point flights, reload/captured operators, named scatter/dodge RNG, evasion/AI/encounter and fractional motion remainder survive load. Defeated sessions cannot overwrite a slot; error/cancel remains recoverable. Version17/16/15 text below records earlier stages. Existing modal pause, file IO and menu navigation semantics remain applicable. Headless continuation/rejection passed; native physical Save/Load menu acceptance is NOT RUN. [Point-aim combat contract](../../04-Engineering/PointAimCombat.md).

Статус: экран реализован в стиле Xenon-inspired DSS UI.

Код: `src/DeepSpaceSaga.Client/UI/Screens/GameMenu/`.

## Назначение

Модальное меню поверх игровой сессии. Открывается из `GameSessionScreen` и временно останавливает authoritative simulation по правилу modal pause.

## Функциональность первого релиза

- `Resume` закрывает меню и возвращает игрока в игровую сессию.
- `Save` открывает окно сохранения.
- `Load` открывает экран загрузки сохранения.
- `Settings` отображается, но остаётся отключённым и не создаёт событие.
- `Main Menu` возвращает игрока в главное меню.
- `Escape` эквивалентен `Resume`.

## Оформление и локализация

Внешняя оболочка окна — переиспользуемый `GenericWindowTypeA`: он принимает любой
`SKRect` и рисует только navy-фон, cyan-рамку и скошенные углы. Заголовки, подписи,
разделители, кнопки и другие UI-элементы в Generic Type A не входят и определяются
конкретным экраном. GameMenu добавляет заголовок, статус и footer через
`XenonWindowChrome`, а кнопки и ромбические маркеры — через `GenericButtonTypeA`.
Текст не встроен в PNG и берётся из `Data/Locale/English.json` или
`Data/Locale/Russian.json`. Ассеты загружаются один раз относительно
`AppContext.BaseDirectory`; при их отсутствии применяется безопасный Skia fallback.

## Архитектурные требования

- Пока меню открыто, симуляция не должна продвигать game time, модульные циклы, добычу, торговые операции или расход топлива.
- UI продолжает отрисовываться.
- Возврат из меню должен восстанавливать предыдущую скорость симуляции по существующему modal pause rule.
- Единственный fullscreen dim overlay и modal pause остаются ответственностью `SkiaWindow`;
  `GameMenuScreen` не рисует собственное затемнение и не управляет скоростью симуляции.
