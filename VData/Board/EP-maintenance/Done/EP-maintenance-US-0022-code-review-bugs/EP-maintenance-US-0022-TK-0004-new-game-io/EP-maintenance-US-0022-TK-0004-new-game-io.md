---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0004-new-game-io
title: Синхронное чтение сохранений и сценариев при нажатии «Новая игра»
stage: approved
approval_kind: workflow
implementation_status: implemented
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC4]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Синхронное чтение сохранений и сценариев при нажатии «Новая игра»

[История](../../../EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

«Новая игра» не читает содержимое сохранений и чужих сценариев в потоке окна: наличие сохранения проверяется по списку файлов, путь обучения ищется без загрузки остальных сценариев, ошибка ввода-вывода не превращает кнопку в бездействие.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0027-US-0001-TK-0001. Серьёзность: minor. Уверенность: CONFIRMED (путь кода), величина фриза PLAUSIBLE.

**Дефект.** Ради проверки «есть ли сохранения» `ListSaveSlots` читает и парсит JSON каждого сохранения. `FindTutorialScenarioPath` полностью загружает и проверяет каждый `scenario.json` по порядку (Default, Default_500 187 КБ, Docked, …) до Tutorial. Оба вызова идут в потоке окна до `_connectionLoader.CreateAsync`, хотя остальной ввод-вывод сессий вынесен в фон. Исключение `Directory.EnumerateFiles` делает «Новую игру» бездействующей.

**Сценарий.** Игрок с несколькими многомегабайтными сохранениями жмёт «Новая игра»: окно замирает, пока разбираются все сохранения, и только потом открывается выбор сценария.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:869, 1092` | `ListSaveSlots().Length == 0` и `GetTutorialScenarioPath()` в потоке окна. |
| `src/DeepSpaceSaga.Engine.LocalClient/SaveSlotRepository.cs:17-44` | `ListSlots` читает и парсит каждый файл. |
| `src/DeepSpaceSaga.Engine.LocalClient/ScenarioRepository.cs:57-77` | `FindTutorialScenarioPath` загружает все сценарии. |
| `src/DeepSpaceSaga.Client/IGameSessionFactory.cs` | Добавлено при исполнении: член `HasAnySaveSlot()` с реализацией по умолчанию. |
| `src/DeepSpaceSaga.Client/Program.cs` | Добавлено при исполнении: `LocalGameSessionFactory.HasAnySaveSlot()` через `SaveSlotRepository.HasAnySlot`. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. Добавить `HasAnySaveSlot()` через `Directory.EnumerateFiles(...).Any()`. Путь обучения искать по известной папке или внутри `Task.Run` в `CreateAsync`. Ошибку ввода-вывода обрабатывать как «сохранений нет» с записью в лог.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

`TutorialFlowTests` и тест уровня Engine.LocalClient: `HasAnySaveSlot` не читает содержимое файлов (например, нечитаемый JSON не мешает ответу).

## Definition of Done

- AC4 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Реализован 2026-11-16 на ветке `maintenance/US-0022-code-review-bugs`. Дефект на текущем main подтверждён: `ListSaveSlots()` разбирал каждое сохранение в потоке окна без обработки ошибок, `GetTutorialScenarioPath()` загружал сценарии до `CreateAsync`.

- `SaveSlotRepository.HasAnySlot`: наличие сохранения по списку `*.json`, без открытия файлов; `IGameSessionFactory.HasAnySaveSlot()` и реализация в `Program.LocalGameSessionFactory`.
- `SkiaWindow`: «Новая игра» использует `HasAnySaveSlot`; `IOException`/`UnauthorizedAccessException` пишутся в InterfaceLog и считаются отсутствием сохранений. Поиск пути обучения перенесён внутрь фоновой задачи `CreateAsync`; запасной путь TK-0002 сохранён.
- Порядок поиска обучения («первое совпадение по порядку») не изменён.

Тесты: `SaveSlotRepositoryTests.HasAnySlot_is_false_for_a_missing_directory_or_one_without_json_files`, `SaveSlotRepositoryTests.HasAnySlot_answers_from_the_file_list_without_reading_slot_contents`, `TutorialFlowTests.NewGameChecksSavesWithoutParsingAndLooksUpTutorialOffTheWindowThread`, `TutorialFlowTests.NewGameTreatsSaveCheckIoErrorAsNoSavesAndStartsTutorial`.

Проверки: `dotnet build tests/DeepSpaceSaga.Client.Tests` — 0 предупреждений, 0 ошибок. Тесты запускаются один раз после завершения истории; результат записан в документе истории.
