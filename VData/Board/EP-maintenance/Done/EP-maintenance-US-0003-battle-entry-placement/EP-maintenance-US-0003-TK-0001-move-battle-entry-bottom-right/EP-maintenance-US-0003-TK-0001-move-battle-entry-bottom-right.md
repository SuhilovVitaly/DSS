---
epic: EP-maintenance
story: EP-maintenance-US-0003-battle-entry-placement
ticket: EP-maintenance-US-0003-TK-0001-move-battle-entry-bottom-right
title: Переместить кнопку входа в пошаговый бой в правый нижний угол
stage: approved
implementation_status: implemented
layer: client
depends_on: []
files_touched: 3
serves: [AC1, AC2]
created: "2026-11-10T07:15:02Z"
revision: 1
---

# Переместить кнопку входа в пошаговый бой в правый нижний угол

## Why

Игрок должен находить кнопку входа в бой в правом нижнем углу экрана карты. Сейчас она занимает отдельную строку над панелью карты. Исходный вход в бой описан в [EP-0015-US-0007-TK-0002](../../../../EP-0015-tactical-carrier-battle/EP-0015-US-0007-launch-and-setup/EP-0015-US-0007-TK-0002-battle-key-and-hint/EP-0015-US-0007-TK-0002-battle-key-and-hint.md).

## Decisions

- **D-01.** Перенести кнопку в правый нижний угол экрана по [запросу пользователя](../EP-maintenance-US-0003-battle-entry-placement.md#входное-техническое-задание).

## Assumptions

- **A-01.** Под экраном имеется в виду карта текущей игровой сессии. Кнопка привязана к правому и нижнему краям видимой области HUD с существующим отступом `PanelMargin` (8 логических единиц).

## Code context

| Файл | Текущее состояние | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs` | Строка и границы кнопки рассчитываются внутри `DrawMapToolbar` (строки 484–511). В той же части класса собираются прямоугольники HUD, учитываемые при размещении подписей карты (строка 303). | Выделить кнопке собственный прямоугольник в правом нижнем углу, учитывать его при размещении подписей и убрать резерв прежней строки. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Размеры HUD переводятся в логические координаты при рендере (строки 1333–1334). Высота старой строки вычитается из доступного места для правой панели (строка 1363). Нижняя панель команд Finance/Ship рисуется в `DrawMechanicsPanel`. | Подключить новую отрисовку и снять резерв старой строки. При тесной компоновке учесть кнопку в размещении соседних элементов HUD и при отсечении взаимодействий с картой. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleKeyTests.cs` | Уже проверяются локализованная подпись, переход по F8, переназначение клавиши и щелчок по кнопке при масштабе 1,2. | Дополнить покрытие положением, изменением размера, наведением и отсутствием активации в прежней области. |

## Public API after the change

Публичный API сохраняется.

## Implementation steps

1. Вынести размещение и отрисовку `_tacticalBattleEntry` из `DrawMapToolbar`. Определять правый и нижний края кнопки по логическим размерам HUD за вычетом `PanelMargin`. Пересчитывать границы при каждом рендере. Снять резерв строки в панели карты и в расчёте высоты правой панели.
2. Согласовать границы кнопки и соседних элементов в тесной компоновке. Использовать один прямоугольник кнопки для отрисовки, наведения и щелчка. При недоступности входа очищать границы кнопки.
3. Проверить сценарии AC1 и AC2 в существующем классе `BattleKeyTests`.

## Out of scope

Правила боя, переходы внутри боя, экран подготовки, локализованные строки и настройки клавиш.

## Invariants

- `CanOpenTacticalBattle` задаёт доступность входа (`GameSessionScreen.MapView.cs:17`). Наведение использует тот же элемент, что и щелчок (`GameSessionScreen.cs:432–435,614`).
- Щелчок и настроенная клавиша возвращают `ScreenEvent.OpenTacticalBattle` (`GameSessionScreen.cs:432–435,645–646`). Вход по щелчку доступен при конфликте клавиш, что покрывает `ConflictFallbackStillOffersClickableLaunchInScaledMapWithoutFalseF8Hint`.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Добавить в `BattleKeyTests` сценарий `BattleEntryStaysInBottomRightAfterResize` с проверкой границ при двух размерах окна и масштабах 1,0 и 1,2. Расширить `ConflictFallbackStillOffersClickableLaunchInScaledMapWithoutFalseF8Hint` проверкой прежней позиции. Сохранить проверки `DefaultMapHintAndRealWindowF8RouteOpenPausedPreparation` и `RemappedKeyOpensOnlyOnConfiguredPhysicalEdge`.

Из корня DSS, после создания каталога результатов, выполнить команды с ограничением 300000 мс на дерево каждого процесса.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter 'FullyQualifiedName~DeepSpaceSaga.Client.Tests.TacticalBattle.BattleKeyTests' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/battle-entry-bottom-right/<run>'
git diff --check
```

Проверить на экране карты, что панель карты и нижние кнопки HUD доступны при обычной и тесной компоновке, а подсказка целиком видна. Для нативной проверки соблюдать правила выбора меньшего монитора из `AGENTS.md`.

## Definition of Done

- AC1 и AC2 выполнены. Сборка Client и именованные тесты прошли. Команды и результаты проверок записаны в тикете.
- Изменения реализации укладываются в три разрешённых файла. Инварианты входа по щелчку и клавише соблюдены.

## Self-containment check

Указаны место расчёта прежней строки, точка рендера HUD, текущий маршрут ввода и тестовый класс. Зависимостей на другие тикеты нет.

## Статус

Реализовано 2026-11-10. Кнопка привязана к правому нижнему краю HUD и учтена при раскладке карты и соседних элементов. `dotnet test` с фильтром `TacticalBattle.BattleKeyTests` — 7/7, `dotnet build` Client и `git diff --check` прошли. Проверки выполнены без нативного окна.
