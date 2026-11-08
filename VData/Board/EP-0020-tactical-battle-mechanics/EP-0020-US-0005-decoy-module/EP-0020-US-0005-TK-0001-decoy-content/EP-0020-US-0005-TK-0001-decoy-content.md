---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0005-decoy-module
ticket: EP-0020-US-0005-TK-0001-decoy-content
title: Модуль ловушек, слот рамы и порог ИИ в данных
stage: draft
layer: content-data
test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0020-US-0001-TK-0001-rules-content]
files_touched: 9
serves: [US5-AC1, US5-AC2]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Модуль ловушек, слот рамы и порог ИИ в данных

## Why

Все числа ловушки живут в таблицах данных V4 и попадают в JSON игры через экспортёр; без синхронной таблицы `ContentSyncTests` и экспортёр упадут.

## Decisions

Применяются [D-15–D-23](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-13: параметры эффекта (`charges`, `missFactor`) принадлежат модулю, чтобы будущие уровни модуля отличались числами; порог ИИ — общее правило боя, поэтому в `rules.json`. A-14: модуль стоит во всех компоновках дронов обеих сторон; у носителя слота нет.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `VData/Planning/V4.0/Data/Modules/Decoys/README.md` | нет | Описание полей и таблица `decoys`: `decoy.t1 | Ловушки I | 1 | 2 | 1.5 | 10`. |
| `VData/Planning/V4.0/Data/README.md` | :9-20 устройство папки, :24-35 слоты, таблица `rules`, таблица компоновок :165-176 | Строка папки `Modules/Decoys/` (ключ `decoys`); строка слота «модуль: 0…`moduleSlots` рамы, `module` (null — нет)»; строка `decoy.aiThreatChance = 0.4` после блока `evasion`; столбец `module` в таблице компоновок. |
| `VData/Planning/V4.0/Data/Frames/README.md` | таблица рам | Столбец `moduleSlots`: 1 у `light`, `heavy`, `thug`; 0 у `carrier`. |
| `tools/tactical-battle/Export-Content.ps1` | :70-71 папки модулей, :95 значения по умолчанию компоновки | Добавить `@('Modules/Decoys', 'decoys')`; `module` по умолчанию `null`; `moduleSlots` по умолчанию 0. |
| `src/DeepSpaceSaga.Client/Data/TacticalBattle/Modules/Decoys/decoy.t1.json, Frames/*.json, layouts.json, rules.json` | генерируются экспортёром | Только через экспортёр; руками не править. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleDefinitions.cs` | :5-7 `TacticalRules`, :16-18 `BattleFrame`, :24 `BattleShield`, :37-38 `BattleLayout` | `BattleDecoy(string Id, string Name, int Tier, int Charges, double MissFactor, double MassKg)`; `int ModuleSlots = 0` в `BattleFrame`; `string? Module = null` в `BattleLayout`; `DecoyRules(double AiThreatChance)` в `TacticalRules` после `Evasion`. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleContentLoader.cs` | :59-60 каталоги, :236 рамы, :247-250 модули, :255-273 компоновки | Каталог `DirectoryCatalog<BattleDecoy>("Modules/Decoys")`; проверки US5-AC1. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ContentLoaderTests.cs` | — | Загрузка поставляемых данных и отказ на каждое нарушение. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ContentSyncTests.cs` | :51 список папок модулей | Добавить `("Modules/Decoys", "decoys")`. |

## Public API after the change

`BattleDecoy`; `BattleFrame.ModuleSlots`; `BattleLayout.Module`; `TacticalRules.Decoy`; `TacticalBattleCatalog.Decoys`.

## Implementation steps

1. Добавить таблицы и строки в Markdown V4 Data (значения D-17, D-20, D-21, A-13).
2. Расширить экспортёр и сгенерировать JSON; убедиться, что прочие файлы не изменились.
3. Добавить records, каталог и проверки загрузчика.
4. Тесты загрузчика и синхронности; `Export-Content.ps1 -Check`.

## Out of scope

Состояние дрона и масса (TK-0002).

## Invariants

Прочие значения данных не меняются; порядок листьев JSON совпадает с порядком строк таблиц.

## Tests

Проект `tests/DeepSpaceSaga.Engine.Tests`. Именованные проверки:

- `ContentLoaderTests.Decoy_module_and_slots_load_from_shipped_data`
- `ContentLoaderTests.Invalid_decoy_module_or_slot_is_rejected`
- `ContentSyncTests.EveryMarkdownValueAndOrderedCatalogMatchesGameJson`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~ContentLoaderTests|FullyQualifiedName~ContentSyncTests|FullyQualifiedName~DefinitionsTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us5-tk1" --logger "trx;LogFileName=engine.trx"
dotnet format whitespace src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore --include src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleDefinitions.cs src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleContentLoader.cs --verify-no-changes
pwsh -NoProfile -File tools/tactical-battle/Export-Content.ps1 -Check
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US5-AC1, US5-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Значения заданы решениями пользователя и A-13; таблицы и JSON меняются вместе.

## Исполнение

Не начато.
