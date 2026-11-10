---
epic: EP-maintenance
story: EP-maintenance-US-0002-module-structure-display
ticket: EP-maintenance-US-0002-TK-0001-align-structure-bar
title: Выровнять высоту пустой части шкалы Structure
stage: approved
implementation_status: implemented
layer: client
depends_on: []
files_touched: 2
serves: [AC1, AC2]
created: "2026-11-10T07:13:07Z"
revision: 1
---

# Выровнять высоту пустой части шкалы Structure

[История и критерии приёмки](../EP-maintenance-US-0002-module-structure-display.md) · [Связанный перенос деталей модулей в левую панель](../../../../EP-0012-combat-rebuild/EP-0012-US-0020-bugs/EP-0012-US-0020-TK-0002-player-info-and-left-modules/EP-0012-US-0020-TK-0002-player-info-and-left-modules.md)

## Why

В раскрытой панели команд штриховка незаполненной части шкалы `Structure` поднимается выше заливки и заходит на название модуля. На скриншоте это видно на строках Bridge Navigation Computer и Living quarters MK I. Полные шкалы Container и Generator дают визуальный ориентир по высоте.

## Decisions

- Пользователь попросил зарегистрировать в эпике исправлений дефект слишком высокой пустой части шкалы. Исходное сообщение и описание скриншота приведены в истории.

## Assumptions

- За высоту и вертикальное положение шкалы принимаются границы заливки полностью заполненной шкалы при том же масштабе. У всех шести делений общие верхняя и нижняя границы.
- Заполненные и пустые деления визуально различимы. Способ отрисовки выбирается в рамках текущего рендера панели команд.

## Code context

| Файл | Текущее состояние | Допустимое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs` | Строки 39–69 готовят шрифт и формируют строку прочности. `HealthBar` собирает шесть делений из локализованных глифов. Строки 604–607 оборачивают строки по ширине, а 910–917 рисуют их на холсте. | Формирование и отрисовка шкалы `Structure` в деталях модулей. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0020TK0002Tests.cs` | Строки 21–44 проверяют наличие деталей модулей и рендер панели через `SKBitmap`. | Добавить проверку визуальных границ и заполнения шкалы. |

Шкала топлива использует те же ключи `World.Panel.BarFull` и `World.Panel.BarEmpty` в `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.EnginePanel.cs:15–16`. Правка применяется локально к шкале прочности.

## Public API after the change

Публичные API и схемы данных не меняются.

## Implementation steps

1. Ограничить отрисовку обеих частей шкалы `Structure` общей вертикальной областью по высоте и положению полной заливки. Сохранить различимость частей и размещение подписи и числа.
2. Сохранить разбиение на шесть делений и текущее округление доли прочности. Проверить пустую, частичную и полную шкалы в штатном рендере панели.
3. Добавить проверку, которая обнаруживает выступ незаполненной части за границы шкалы, даже если текст и числовые данные строки корректны.

## Out of scope

Шкала топлива, общие ключи локализации, правила урона, значения прочности и изменение макета панели.

## Invariants

- `CommandsPanel.cs:57–68` выводит `tile.Structure` и использует `tile.StructureMax` для заполнения. При положительном максимуме доля ограничена диапазоном от 0 до 1 и округляется до числа делений. При неположительном максимуме заполнено 0 делений.
- `CommandsPanel.cs:56–58` выводит имя модуля и отдельную подпись для уничтоженного модуля.

## Tests

- Будущий `Structure_bar_segments_share_vertical_bounds` в `PointCombatUS0020TK0002Tests`. Отрисовать шкалы 0/150, 60/150, 80/150 и 150/150 через панель на `SKCanvas`. Проверить число заполненных делений, совпадение вертикальных границ и отсутствие пикселей шкалы в соседних строках. Сопоставить общую ширину шкал при одном масштабе.
- Визуально сверить подпись, число и границы частичной шкалы в русском и английском интерфейсе.
- Из корня DSS запустить команды ниже. Для тестового процесса установить лимит 300000 мс на дерево процессов. Перед запуском подготовить каталог результатов и подставить уникальный ID запуска вместо `RUN-ID`.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter FullyQualifiedName~PointCombatUS0020TK0002Tests --results-directory D:/DeepSpaceSaga/artifacts/validation/maintenance-structure-bar/RUN-ID
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0020TK0002Tests.cs
```

## Definition of Done

- AC1 и AC2 выполнены в штатном рендере панели. Именованный тест, сборка и проверка форматирования пройдены либо исходные сбои записаны отдельно.
- Изменения ограничены файлами из `Code context`. Инварианты соблюдены.
- Тикет содержит статус реализации и фактические результаты проверок.

## Self-containment check

Точка формирования шкалы, место её отрисовки, эталон высоты, контрольные значения и тестовая поверхность указаны. Зависимостей от других тикетов нет.

## Статус

Реализовано 2026-11-10. Штриховка ограничена габаритами полной заливки, ширина каждого деления едина. `dotnet test` с фильтром `PointCombatUS0020TK0002Tests` — 6/6 (растровая проверка English и Russian), `dotnet build` Client и `dotnet format --verify-no-changes` для двух файлов прошли. Нативная визуальная проверка не проводилась.
