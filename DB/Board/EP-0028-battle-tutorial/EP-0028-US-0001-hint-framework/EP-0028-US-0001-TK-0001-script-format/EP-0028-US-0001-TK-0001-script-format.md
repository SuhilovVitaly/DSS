---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0001-hint-framework
ticket: EP-0028-US-0001-TK-0001-script-format
title: Формат и загрузчик сценария обучения
stage: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: []
implementation_files: 2
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialScript.cs
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialScriptLoader.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialScriptLoaderTests.cs
serves: [US-0001/AC-01]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Формат и загрузчик сценария обучения

[История](../EP-0028-US-0001-hint-framework.md) · [Эпик](../../Documentation.md)

## Цель

Описать уроки данными: неизменяемая модель `TutorialScript` и загрузчик `battle-tutorial.json` с полной валидацией. Ошибка формата отключает обучение, а не бой.

## Decisions

D-02, D-06, D-07 [эпика](../../Documentation.md#решения-пользователя-обязательные): тексты — только ключи Locale; файл — клиентские данные.

## Assumptions

- Схема фиксируется целиком сейчас (включая `officer` и `limits`), чтобы последующие тикеты не меняли загрузчик.
- Все шаги одноразовые в пределах боя; поля `once` нет.
- Файл ищется в `<baseDir>/Data/Tutorial/battle-tutorial.json`; отсутствие файла — пустой сценарий и одна запись в журнал.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialScript.cs` | Нет файла | Новые records модели |
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialScriptLoader.cs` | Нет файла. Образец обработки ошибок и JSON — `PilotPortraitCatalog.Load` (`UI/Screens/TacticalBattle/PilotPortraitCatalog.cs:11-37`): версия, проверка путей, `catch` узкого набора исключений, `InterfaceLog.Write` | Новый загрузчик |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialScriptLoaderTests.cs` | Нет файла | Новые тесты |

Данные копируются в выход правилом `Data\**\*.json` (`src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj:296`); правка csproj не нужна. Словарь фаз совпадает с `PhasePanel.Phase` (`UI/Screens/TacticalBattle/PhasePanel.cs:14`): `Move`, `Fire`, `Salvo`, `Results`, `Outcome`.

## Public API after the change

```csharp
namespace DeepSpaceSaga.Client.UI.Screens.Tutorial;
internal sealed record TutorialOfficer(string NameKey, string RoleKey, string Portrait, string PortraitKey);
internal sealed record TutorialLimits(int MinGapMs = 2500, int AutoCollapseMs = 15000, int MaxLinesPerPhase = 2);
internal enum TutorialCompletion { PilotSelected, PathEdited, PhaseLeft, TargetChosen, PilotCycled, EvadeOrDecoy, Acknowledged }
internal enum TutorialSignal { None, UnderFire, OwnDamaged, LowConsciousness, AnomalyPresent, DroneLost, OutcomeVictory, OutcomeDefeat, OutcomeOther }
internal sealed record TutorialCondition(string Phase, int? Turn, int? MinTurn, string? After, TutorialSignal Signal);
internal sealed record TutorialStep(string Id, string LineKey, TutorialCondition When, TutorialCompletion Complete, string? Anchor, int Priority);
internal sealed record TutorialScript(int SchemaVersion, TutorialOfficer? Officer, TutorialLimits Limits, ImmutableArray<TutorialStep> Steps)
{ internal static TutorialScript Empty { get; } }
internal static class TutorialScriptLoader
{
    internal const string RelativePath = "Data/Tutorial/battle-tutorial.json";
    internal static TutorialScript Load(string baseDir, Action<string>? warning = null);
    internal static TutorialScript Parse(string json); // throws FormatException with field path
}
```

JSON (camelCase, неизвестные свойства — ошибка):

```json
{ "schemaVersion": 1,
  "officer": { "nameKey": "Tutorial.Officer.Name", "roleKey": "Tutorial.Officer.Role",
               "portrait": "Images/Persons/W4/Portraits/<head>.png", "portraitKey": "tutorial.officer" },
  "limits": { "minGapMs": 2500, "autoCollapseMs": 15000, "maxLinesPerPhase": 2 },
  "steps": [ { "id": "move.select-pilot", "lineKey": "Tutorial.Line.MoveSelectPilot",
               "when": { "phase": "Move", "turn": 1, "after": "intro" },
               "complete": "pilot-selected", "anchor": "pilot:{own}", "priority": 90 } ] }
```

## Implementation steps

1. Модель в `TutorialScript.cs` (records/enums выше, `ImmutableArray`).
2. `Parse` через `JsonDocument`: `schemaVersion == 1`; `officer` необязателен (если есть — все четыре поля непустые; `portrait` — относительный путь из сегментов `[A-Za-z0-9._-]` с расширением `.png`, без `..`); `limits` необязателен, диапазоны `minGapMs 0…60000`, `autoCollapseMs 1000…120000`, `maxLinesPerPhase 1…5`.
3. Шаги: `id` по `^[a-z0-9]+([.-][a-z0-9]+)*$`, уникальный; `lineKey` начинается с `Tutorial.Line.`; `when.phase` из словаря фаз; `turn`/`minTurn` ≥ 1 и не оба; `after` ссылается на шаг **выше** по списку (ацикличность); `signal` — kebab-case имя `TutorialSignal` (`under-fire`, `own-damaged`, `low-consciousness`, `anomaly-present`, `drone-lost`, `outcome-victory`, `outcome-defeat`, `outcome-other`); `complete` — kebab-case `TutorialCompletion`; `anchor` необязателен и соответствует `^[a-z][a-z-]*(:(\{(own|selected|any|signal)\}|[^\s{}]+))?$`; `priority` 0…100.
4. `Load`: отсутствующий файл → `Empty` + `warning("TUTORIAL: <path> not found")`; `IOException`, `UnauthorizedAccessException`, `JsonException`, `FormatException` → `Empty` + `warning("TUTORIAL: <path>: <сообщение>")`. Исключения наружу не выходят.

## Acceptance criteria

1. Корректный файл даёт модель с шагами в исходном порядке.
2. Каждое нарушение из шагов 2–3 даёт `FormatException` с путём поля (например `steps[1].when.after`).
3. `Load` никогда не бросает и пишет ровно одно предупреждение на ошибку.

## Verification signal

Сначала написать и увидеть красным `TutorialScriptLoaderTests.ParsesValidScriptPreservingStepOrder` (тип/метод отсутствуют — не компилируется), затем Theory `RejectsInvalidScript(string json, string fieldPath)` на каждое правило шага 2–3 и `LoadReturnsEmptyAndWarnsOnMissingOrBrokenFile`.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0001-tk0001-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial.TutorialScriptLoaderTests" --results-directory "$RUN" --logger "trx;LogFileName=loader.trx"
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj > "$RUN/build.log"
```

Process-tree cap 300000 ms на тестовый процесс. Временные JSON тестов — в `Path.GetTempPath()`/`DSS_VALIDATION_ROOT` с удалением.

## Out of scope

Сам файл `battle-tutorial.json` (US-0003), директор, отрисовка, правки `UI/Screens/TacticalBattle/`.

## Invariants

- Код вне `/TacticalBattle/` (ForbiddenCalls не затрагивается).
- Нет зависимостей от Engine; только `System.Text.Json` и `InterfaceLog`.

## Known fallout

Нет.

## Definition of Done

- Все implementation steps выполнены в разрешённых файлах.
- `serves` покрыт named tests; команды выше зелёные; сборка Client без новых предупреждений.
- Публичный API совпадает с разделом выше; нет незаписанных assumptions.
- Отдельный коммит `EP-0028-US-0001-TK-0001: …` и `git push`.

## Self-containment check

Схема, правила валидации, образец обработки ошибок и пути указаны полностью; исполнителю не нужно искать другие файлы.

## Результат

Реализованы модель `TutorialScript` (records/enums по разделу Public API) и `TutorialScriptLoader` (`Parse` через `JsonDocument` с полной валидацией и путём поля в начале сообщения `FormatException`; `Load` без исключений наружу, одно предупреждение на ошибку). Сам `battle-tutorial.json` не создавался (US-0003).

Проверки 2026-11-14 (артефакты `D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0001-tk0001-20261114T172756/`):

- Красный старт: `TutorialScriptLoaderTests` не компилировался (namespace `DeepSpaceSaga.Client.UI.Screens.Tutorial` отсутствовал) — `../us0001-tk0001-red/build-red.log`.
- `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial.TutorialScriptLoaderTests"` (`DSS_VALIDATION_ROOT` = папка прогона) — **58/58**: `ParsesValidScriptPreservingStepOrder`, `OptionalSectionsUseDefaults`, `EveryCompletionAndSignalNameParses`, Theory `RejectsInvalidScript` (52 случая), `LoadReturnsEmptyAndWarnsOnMissingOrBrokenFile`, `EmptyScriptHasNoStepsAndDefaultLimits` — `loader.trx`, `test.log`.
- `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj` — 0 предупреждений, 0 ошибок — `build.log`.

## Журнал

2026-11-14T15:04:43Z — тикет создан.

2026-11-14 — реализовано (TDD), `stage: implemented`. Узкие допущения исполнителя: `priority` необязателен (по умолчанию 0); `when.signal` принимает только восемь имён сигналов (`none` — ошибка, отсутствие поля = `None`); `steps` обязателен (пустой массив допустим); `lineKey` длиннее префикса `Tutorial.Line.`; при `warning == null` предупреждение пишется через `InterfaceLog.Write`; синтаксически некорректный JSON в `Parse` даёт `JsonException` (не оборачивается), его перехватывает `Load`.
