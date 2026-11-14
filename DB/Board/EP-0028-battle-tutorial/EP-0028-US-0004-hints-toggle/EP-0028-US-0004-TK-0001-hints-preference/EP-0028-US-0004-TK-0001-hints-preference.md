---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0004-hints-toggle
ticket: EP-0028-US-0004-TK-0001-hints-preference
title: Предпочтение tutorialHints
stage: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: []
implementation_files: 1
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialHintsPreference.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialHintsPreferenceTests.cs
serves: [US-0004/AC-01]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Предпочтение tutorialHints

[История](../EP-0028-US-0004-hints-toggle.md) · [Эпик](../../Documentation.md)

## Цель

Чтение и атомарная запись `gameSettings.tutorialHints` в `Settings.json` с сохранением всех прочих ключей.

## Decisions

D-04 — хранение в `Settings.json` → `gameSettings`; A-02 — отсутствие/ошибка = `true` ([эпик](../../Documentation.md#допущения)).

## Assumptions

- Некорректный тип значения (`"yes"`, число) читается как `true` с предупреждением; запись перезаписывает его булевым.
- Файл может отсутствовать — запись создаёт `{ "gameSettings": { "tutorialHints": … } }`.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialHintsPreference.cs` | Нет файла. Образец полностью — `TacticalBattleSkinPreference` (`UI/Screens/TacticalBattle/TacticalBattleSkinPreference.cs:11-113`): `Read` через `JsonDocument`, `TryWrite` через `JsonNode`-merge, temp-файл `.<name>.<guid>.tmp` + `File.Move(..., overwrite: true)`, `IsSettingsError` | Новый статический класс по образцу (копия логики, не наследование и не правка образца) |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialHintsPreferenceTests.cs` | Нет файла. Образец тестов — `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SkinPreferenceTests.cs` | Новые тесты |

## Public API after the change

```csharp
internal static class TutorialHintsPreference
{
    internal const string Property = "tutorialHints";
    internal static bool Read(string settingsPath, Action<string>? warning = null);            // default true
    internal static bool TryWrite(string settingsPath, bool enabled, Action<string>? warning = null);
}
```

## Implementation steps

1. `Read`: нет файла/группы/свойства → `true`; `True`/`False` → значение; иное → `true` + `warning`.
2. `TryWrite`: merge в существующий объект, не трогая другие ключи и группы; без изменений, если значение то же; атомарная замена; очистка temp-файла; ошибки → `false` + `warning`.

## Acceptance criteria

1. Значение по умолчанию — `true`.
2. Запись сохраняет `language`, `uiScale`, `tacticalBattleSkinId`, `tacticalMap` и посторонние корневые ключи.
3. Повреждённый корень (`[]`), недоступный файл — `false`, прежний файл не испорчен, temp-файлов не остаётся.

## Verification signal

Первым красным — `TutorialHintsPreferenceTests.MissingSettingDefaultsToEnabled`; затем `WritePreservesUnrelatedSettingsAndIsReadBack`, `InvalidValueReadsAsEnabledWithWarning`, `WriteFailureKeepsOriginalFileAndLeavesNoTemp`.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0004-tk0001-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~TutorialHintsPreferenceTests|FullyQualifiedName~SkinPreferenceTests" --results-directory "$RUN" --logger "trx;LogFileName=preference.trx"
```

Временные профили — в `DSS_VALIDATION_ROOT` или `Path.GetTempPath()` с удалением. Process-tree cap 300000 ms.

## Out of scope

UI (TK-0002, TK-0003); правка `TacticalBattleSkinPreference.cs`; поставляемый `Settings.json`.

## Invariants

- Код вне `/TacticalBattle/`.
- Атомарность записи как у образца.

## Known fallout

Нет.

## Definition of Done

- Шаги выполнены; named tests зелёные; сборка Client без новых предупреждений.
- Отдельный коммит и `git push`.

## Self-containment check

Образец указан построчно; API и правила по умолчанию заданы.

## Результат

`src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialHintsPreference.cs` — новый `internal static class TutorialHintsPreference` (копия логики `TacticalBattleSkinPreference`, без наследования и без правки образца): `Property = "tutorialHints"`; `Read` — нет файла/каталога/группы/свойства → `true` без предупреждения; `true`/`false` → значение; иной тип (`"yes"`, число, `null`, массив, объект) или повреждённый файл/корень/группа → `true` + `warning`. `TryWrite` — `JsonNode`-merge в существующий объект (все прочие группы и ключи сохраняются), без перезаписи при том же булевом значении, некорректное значение перезаписывается булевым, отсутствующий файл создаётся как `{"gameSettings":{"tutorialHints":…}}`; атомарная замена через `.Settings.json.<guid>.tmp` + `File.Move(overwrite: true)`, temp удаляется в `finally`; ошибки → `false` + `warning`, прежний файл не меняется.

Тесты `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialHintsPreferenceTests.cs`: `MissingSettingDefaultsToEnabled`, `WritePreservesUnrelatedSettingsAndIsReadBack` (`language`, `uiScale`, `tacticalBattleSkinId`, `tacticalMap`, посторонние корневые ключи; повторная запись не трогает файл; создание файла), `InvalidValueReadsAsEnabledWithWarning`, `WriteFailureKeepsOriginalFileAndLeavesNoTemp` (`{`, `[]`, `gameSettings: null/42`, заблокированный файл — файл не испорчен, temp не остаётся).

Проверки 2026-11-14 (артефакты `D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0004-tk0001-20261114T193630/`, `DSS_VALIDATION_ROOT` = папка прогона):

- Красный старт (класс-заглушка с `NotImplementedException`): 4 failed / 0 passed — `red/red.log`.
- `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~TutorialHintsPreferenceTests|FullyQualifiedName~SkinPreferenceTests"` — **11/11** — `preference.trx`, `preference.log`.
- `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj` — 0 warnings, 0 errors.
- Правок в `/TacticalBattle/` нет; поставляемый `Settings.json` не менялся.

## Журнал

2026-11-14T15:04:43Z — тикет создан.

2026-11-14 — реализовано (TDD), `stage: implemented`. Узкие решения исполнителя: (1) JSON `null` и нечисловые/небулевы значения трактуются как «некорректный тип» (`true` + предупреждение), а не как «отсутствует»; (2) сравнение «то же значение» делается только для булева JSON-значения, поэтому некорректное значение всегда перезаписывается.
