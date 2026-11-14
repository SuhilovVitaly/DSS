---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0002-comms-panel
ticket: EP-0028-US-0002-TK-0001-ui-texts
title: Тексты интерфейса обучения RU/EN
stage: approved
layer: content-data
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: []
implementation_files: 2
files_touched:
  - src/DeepSpaceSaga.Client/Data/Locale/Russian.json
  - src/DeepSpaceSaga.Client/Data/Locale/English.json
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialLocaleParityTests.cs
serves: [US-0002/AC-01]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Тексты интерфейса обучения RU/EN

[История](../EP-0028-US-0002-comms-panel.md) · [Эпик](../../Documentation.md)

## Цель

Добавить все неучебные строки обучения (панель, журнал, строка настроек) в обе локали и тест паритета для всех ключей `Tutorial.*` и `Settings.TutorialHints*`. Последующие content-тикеты US-0003 расширяют те же файлы и автоматически попадают под этот тест.

## Decisions

D-06: все строки — из `Data/Locale/{Russian,English}.json`, полный паритет. D-04 и A-03: подписи кнопок называют действие ([эпик](../../Documentation.md#допущения)).

## Assumptions

- A-09: Locale — слой content-data; ключи UI вынесены сюда, чтобы кодовые тикеты оставались в слое client.
- Префикс `Tutorial.` не пересекается с существующими ключами; существующий паритет покрывает только `TacticalBattle.` (`tests/DeepSpaceSaga.Client.Tests/TacticalBattle/LocalizationCoverageTests.cs:110-143,218-226`), поэтому нужен отдельный тест.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | 801 плоский ключ; группа `Settings.*` на `:423-431` | Добавить ключи из таблицы; `Settings.TutorialHints*` — рядом с `Settings.*` |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | 801 ключ, тот же порядок | То же |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialLocaleParityTests.cs` | Нет файла. Чтение локалей — `Localization.LoadLocaleFile(language)` (`src/DeepSpaceSaga.Client/Localization.cs:99-114`); проверка формата — `CompositeFormat.Parse(...).MinimumArgumentCount` (как `LocalizationCoverageTests.cs:122`) | Новый тест |

Ключи (значения — предложение, допустима правка формулировок в пределах длины):

| Key | RU | EN |
|---|---|---|
| `Tutorial.Panel.Title` | СВЯЗЬ | COMMS |
| `Tutorial.Panel.Acknowledge` | Понял | Got it |
| `Tutorial.Panel.HintsOff` | Подсказки: выкл | Hints: off |
| `Tutorial.Panel.HintsOn` | Подсказки: вкл | Hints: on |
| `Tutorial.Panel.JournalHint` | H — журнал связи | H — comms log |
| `Tutorial.Journal.Title` | ЖУРНАЛ СВЯЗИ | COMMS LOG |
| `Tutorial.Journal.Empty` | Сообщений пока нет. | No messages yet. |
| `Tutorial.Journal.Close` | Закрыть | Close |
| `Settings.TutorialHints` | ПОДСКАЗКИ ОБУЧЕНИЯ | TUTORIAL HINTS |
| `Settings.TutorialHints.On` | Включены | On |
| `Settings.TutorialHints.Off` | Выключены | Off |
| `Settings.TutorialHints.SaveFailed` | Не удалось сохранить настройку подсказок. | Could not save the hints setting. |

## Public API after the change

Новые ключи локализации (таблица). Кода нет.

## Implementation steps

1. Добавить ключи в обе локали, сохранив валидный JSON и UTF-8 без BOM-изменений.
2. Тест `TutorialLocaleParityTests`:
   - `TutorialKeysMatchInBothLocales`: множества ключей с префиксами `Tutorial.` и `Settings.TutorialHints` в RU и EN совпадают и не пусты;
   - `TutorialValuesAreNonBlankLocalizedAndFormatCompatible`: значения не пустые, не равны ключу, `MinimumArgumentCount` RU == EN;
   - `RequiredUiKeysExist`: 12 ключей таблицы присутствуют.

## Acceptance criteria

1. 12 ключей есть в обеих локалях.
2. Паритет и формат проверяются тестом, который ловит любой будущий ключ `Tutorial.*` без пары.

## Verification signal

Первым написать `TutorialLocaleParityTests.RequiredUiKeysExist` — красный, пока ключей нет; затем добавить ключи. Детерминированная проверка содержимого — сам тест паритета.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0002-tk0001-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~TutorialLocaleParityTests|FullyQualifiedName~LocalizationTests|FullyQualifiedName~LocalizationCoverageTests" --results-directory "$RUN" --logger "trx;LogFileName=locale.trx"
```

Process-tree cap 300000 ms.

## Out of scope

Реплики уроков и имя офицера (US-0003); использование ключей в коде (TK-0002, TK-0005, US-0004).

## Invariants

- Существующие ключи и их значения не меняются.
- `LocalizationCoverageTests` и `LocalizationTests` остаются зелёными.

## Known fallout

Нет (существующие тесты локалей проверяют свои префиксы).

## Definition of Done

- Ключи добавлены; named tests зелёные; JSON валиден.
- Отдельный коммит и `git push`.

## Self-containment check

Полный список ключей и значений приведён; тестовые API указаны с `path:line`.

## Журнал

2026-11-14T15:04:43Z — тикет создан.
