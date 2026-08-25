# ScenarioSelect

Статус: экран реализован.

Код: `src/DeepSpaceSaga.Client/UI/Screens/ScenarioSelect/` (`ScenarioSelectScreen.cs`, `ScenarioSelectLayout.cs`).

## Назначение

Экран выбора стартового сценария между `Main Menu` и `GameSessionScreen`. Открывается по `New Game` из `Main Menu`; до выбора сценария на нём новая игровая сессия не создаётся.

## Функциональность первого релиза

- Показывает список сценариев, найденных рекурсивно под клиентской директорией `Scenarios/` (каждый — файл `scenario.json`), отсортированный по `scenarioId` (`ScenarioRepository.ListScenarios`, `DeepSpaceSaga.Engine.LocalClient`).
- Каждая строка показывает `Name` и `Description` сценария (поле `description` в `scenarioMetadata`, необязательное) и кнопку `PLAY`.
- Список скроллируется колесом мыши, если сценариев больше, чем помещается на экран (`ScenarioSelectLayout.VisibleRows`).
- `PLAY` по строке создаёт новую сессию строго из выбранного файла (`IGameSessionFactory.CreateSessionFromScenario`) и переводит игрока на `GameSessionScreen`.
- `BACK` или `Escape` возвращают в `Main Menu` без побочных эффектов — сессия к этому моменту ещё не создана.
- Невалидный/неразбираемый `scenario.json` пропускается при построении списка, а не приводит к падению экрана.

## Связанные механики

- Стартовый сценарий и стартовый корабль игрока (см. `Docs/FirstRelease/Mechanics/TetrarchClass.md`).
- Инициализация `GameSessionScreen` из выбранного сценария.
- Генерация `masterSeed`, если выбранный сценарий его не содержит (обычный случай для New Game).

## Архитектурные требования

- Полноэкранный top-level экран (как `MainMenuScreen`), не модальное окно поверх сессии — активной сессии на этот момент ещё нет, поэтому modal pause rule к нему не применяется.
- Экран не создаёт доменные объекты самостоятельно и не читает scenario JSON напрямую: список получает через `Func<IReadOnlyList<ScenarioInfo>>`, переданный конструктором (в проде — `IGameSessionFactory.ListScenarios`), а выбор передаёт наверх через `ScreenEvent.ScenarioSelected` + `LastSelectedScenarioPath`, по аналогии с `LoadScreen`/`LoadSlotRequested`.
- Загрузка сессии по-прежнему идёт через `IGameSessionFactory`/session boundary; `ScenarioSelectScreen` сам не создаёт `IGameSessionConnection`.

## Оформление

Внешняя оболочка экрана построена через `GenericWindowTypeA`. Кнопки `BACK` и `PLAY`
используют `GenericButtonTypeA`, включая disabled-состояние `PLAY`, когда сценарий не
выбран. Список сценариев и правая информационная панель остаются самостоятельными
внутренними UI-элементами и не входят в Generic Type A.

## Примечания

`ScenarioSelectScreen` не является игровым экраном и не должен напрямую зависеть от `Engine`.
