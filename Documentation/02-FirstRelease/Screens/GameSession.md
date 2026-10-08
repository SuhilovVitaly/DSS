# GameSession

Статус: экран уже существует в коде.

Код: `src/DeepSpaceSaga.Client/UI/Screens/GameSession/`.

## Назначение

Основной экран первого релиза. На нем игрок видит тактическую карту, управляет кораблем Tetrarch Class, выбирает объекты, открывает игровые панели и выполняет основные действия игрового цикла.

## Уже существующая функциональность

- Тактическая карта с камерой и масштабированием.
- Отрисовка объектов, маркеров, подписей, орбитальных/траекторных элементов и трейлов.
- Панель скоростей: `Speed0`, `Speed1`, `Speed2`, `Speed3`, `Speed4`.
- Панель масштаба карты.
- Панель команд модулей корабля.
- Команды двигателя, включая маневрирование, синхронизацию скорости/направления и orbit.
- Выбор и hover объектов через `ActiveObjectId` и `SelectedObjectId`.
- Информационная панель и панель игрокского корабля.
- Quick Save / Quick Load через клавиши.
- `Escape` открывает `GameMenu` как modal screen.

## Функциональность первого релиза

- Игрок должен уметь долететь до станции, астероида или выбранного celestial object.
- Игрок должен видеть состояние грузового отсека и запас `Fuel` в баках двигателя.
- После успешной команды `navigation.dock` игрок должен получить доступ к экрану станции, а с него - к торговле и диалогам станции.
- При достижении астероида игрок должен получить доступ к добыче льда через Drilling Unit, если условия добычи выполнены.
- Игрок должен иметь доступ к диалогам экипажа с этого экрана или через отдельную панель/экран.
- Из `Session` должны открываться окна `Game Menu`, `Station`, `Ship`, `Loot`, `Character Communication`, `Cargo` и `Dialog` согласно минимальной схеме первого релиза.

## Требующие проектирования элементы UI

- Шесть командных панелей: Navigation, Maneuver, Engine, Space Control, Torpedo Launcher, Countermeasure Launcher (реализованы).
- Команды `navigation.dock` и `navigation.stationsList` навигационного компьютера.
- Команды Scanner: `scanner.generalScan`, `scanner.structuralScan`, `scanner.nearbySignatures`.
- Команды Drilling Unit: `mining.extractIce`, `mining.stopExtraction`.
- Панель грузового отсека.
- Панель `Fuel`.
- Вход в окно `Ship`.
- Вход в окно `Cargo`.
- Вход в окно `Loot` для результатов добычи/подбора.
- Вход в диалоги экипажа.
- Вход в диалоги представителей станции.
- Состояние стыковки/синхронизации с объектом.
- Состояние добычи льда и причины недоступности mining commands.

## Связанные механики

- `TetrarchClass`.
- `CommandPanels`.
- `TacticalMapAndManeuvering`.
- Docking.
- Trading.
- `CargoHold`.
- `CrewDialogues`.
- `StationDialogues`.
- `IceMining`.
- `Fuel`.


## Боевой этап EP-0007

Экран отображает назначенных операторов, самоуничтожение торпеды и автоматическую защиту. ПР выбирается на карте, но недоступна для Fire. Frozen chance/breakdown приходят в snapshot; UI не бросает RNG. Сворачиваемый журнал сохраняет authoritative историю. Круг100км виден у выбранного пирата. [Контракт, Save/Load и проверки](../../04-Engineering/CountermeasureCombat.md).

## Актуализация EP-0004 — 2026-10-08

Карта показывает AI diamond/owner/type, движущиеся defence/patrol circles, Radiation/Dust/Debris и известные POI. Toolbar переключает четыре слоя; выбор перекрытий циклический. Поля показывают отсутствие эффектов, POI — отсутствие исследования. База недоступна для стыковки/торговли и в UI, и в Engine. Панель AI/POI/field компактна и прокручивается на малом viewport; выбор descriptors не отправляет engine command. Native interactions8/8 PASS;80FPS acceptance OPEN.

[Текущий технический контракт и evidence](../../04-Engineering/AiMapEnvironment.md).
