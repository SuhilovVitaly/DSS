---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0001-module-owned-weapon-rules
ticket: EP-0008-US-0001-TK-0005-chance-resolution
title: "Новая формула и captured параметры при пуске"
stage: implemented
layer: engine
depends_on: ["EP-0008-US-0001-TK-0004-weapon-catalog"]
files_touched: 21
serves: ["AC-0001","AC-0002","AC-0003"]
created: 2026-10-07
revision: 1
---

# Новая формула и captured параметры при пуске

STATUS: IMPLEMENTED; publication tracked in ImplementationStatus.md

## Why

Игрок видит параметры установок и понимает шанс сбития. Одинаковые модули обеих сторон работают одинаково, а навыки пока не меняют вероятность. Этот тикет обеспечивает: новая формула и captured параметры при пуске.

## Decisions

Решения Q14,Q17,Q20–Q25,Q41,Q46,Q48 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0001-module-owned-weapon-rules.md): AC-0001, AC-0002, AC-0003. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Combat/InterceptionMath.cs` | Существует: src/DeepSpaceSaga.Engine/Combat/InterceptionMath.cs:3 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.WeaponOperators.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.WeaponOperators.cs:6 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs:8 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs:8 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Engine.Tests/InterceptionChanceTests.cs` | Существует: tests/DeepSpaceSaga.Engine.Tests/InterceptionChanceTests.cs:4 (baseline 6907c58) | Только результат и шаги этой карточки |

## Dependencies

### Execution scope — 2026-10-11

Baseline TK-0004 `08abe0a` published and verified. Extend the scope to
`SimulationEngine.cs`, `Content/{ModuleTypeDefinition,EngineContentLoader,GameDataRegistry}.cs`,
`Scenario/CountermeasureSaveData.cs`, both shipped weapon JSON files, and matching
Engine tests (`WeaponOperatorRuntimeTests`, `AutomaticDefenseTests`,
`CountermeasureResolutionTests`, `CountermeasureBootstrapTests`,
`CombatContentLoaderTests`, `CountermeasureContentLoaderTests`). The old module
rating fields must be removed from the content ingress and detection paths;
save validation must accept skill-independent captured operator values. Any
existing tests using skills to force success/failure will instead configure
module accuracy/maneuverability and retain their original outcome assertions.
Additional affected tests will be listed before editing if regression finds
them. No range enforcement or lifetime scheduling is claimed in this ticket.
Full non-corpus Engine regression applies; final map corpuses remain an epic gate.

Regression scope addition before editing: `tests/DeepSpaceSaga.Client.Tests/TetrarchLoadoutTests.cs`
asserts the full launcher DTO and must now expect module range200/maneuverability5
instead of missing fields. Preserve all placement/loadout assertions.
The same full-DTO expectation in `tests/DeepSpaceSaga.Client.Tests/PirateScenarioTests.cs`
is included for the identical projection change; pirate motion remains unchanged here.
Also extend `tests/DeepSpaceSaga.Engine.Tests/CountermeasureSaveSchemaTests.cs`
with mismatched captured-value cases to prove the updated persistence guard
rejects malformed input without replacing the current world.

Шаг **5** [общего порядка](../../Tickets.md).
- [EP-0008-US-0001-TK-0004-weapon-catalog](../EP-0008-US-0001-TK-0004-weapon-catalog/EP-0008-US-0001-TK-0004-weapon-catalog.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Заменить 50+ratingDifference на clamp(accuracy−maneuverability); сохранить precision0.1% и один named draw на контакте включая0% ручного/100%. Торпеда фиксирует maneuverability на Fire, ПРО фиксирует accuracy и target value на своём Fire. Привести все runtime calls к новой формуле, сохранив no-operator rejection и old skills как неигровой metadata.
3. Реализовать перечисленные ниже регрессии/проверки. Изменение прежних ожиданий допустимо только при изменённом согласованном контракте; не ослаблять независимые assertions.
4. Review относительно AC и контракта эпика; исправить подтверждённые findings и повторить затронутые проверки.
5. При исполнении через общий промт: записать evidence, отдельный commit с ID тикета и push; не смешивать другой тикет.

## Invariants и Out of scope

- Architecture и временные домены — [эпик](../../Documentation.md): snapshot immutable, Engine authoritative, physical/calendar/real-time разделены.
- Нельзя выдавать full-suite green за доказательство ненаписанного сценария; native проверяется отдельно.
- Недопустимы неоговорённые fuel/ammo/патрульные механики, user commands от имени NPC, скрытые изменения характеристик и повторные RNG после Load.
- Новая фаза требует current-format save; version0 NewGame не путать со старым save.
- Другие production layers не реализуются в этой карточке; необходимое расширение оформляется до изменения.

## Tests и проверки

Имена ниже **планируемые**, не результат выполненного тестирования:

- [x] `FiftyFive_minus_five_is_fifty`
- [x] `Skills_zero_fifty_hundred_do_not_change_chance`
- [x] `Clamp_and_tenths_are_exact`
- [x] `Frozen_values_survive_catalog_changes`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [x] Все AC-0001, AC-0002, AC-0003 в scope этой карточки покрыты реальным результатом/проверками.
- [x] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [x] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [x] Review проведён, ограничения/native статус описаны честно.
- [x] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [x] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.

## Execution evidence — 2026-10-11

Implemented module-owned ratings and clamp(accuracy - maneuverability), precision
0.1% AwayFromZero. Both operator skills are metadata; assignments remain required.
Launch captures ratings and PR motion parameters; saved captures survive a different
catalog. Current save validation rejects inconsistent captures atomically. Legacy
format handling remains until US-0011; manual zero-percent launch/contact is a US-0005
integration gate, because manual Fire is not implemented in US-0001.

Validation from repository root:
- `dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter 'FullyQualifiedName!~CorrectnessCorpus'`: **1728/1728 PASS** (`ep8-tk5-engine-final.trx`).
- Final strengthened skill/formula tests: **19/19 PASS** (`ep8-tk5-focused-final.trx`), including real PR launches at skill0/50/100.
- `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore`: **1761/1761 PASS** (`ep8-tk5-client-final2.trx`).
- `dotnet build DeepSpaceSaga.sln --no-restore -v quiet`: PASS, 0 warnings/errors.
- Scoped `dotnet format whitespace ... --verify-no-changes --no-restore --include <changed C#>` and `git diff --check`: PASS.

Initial failures exposed obsolete skill-based guaranteed outcomes, missing market
events in a custom-registry Save/Load fixture, a synthetic v10 fixture incorrectly
retaining new captures, and outdated launcher DTO/rating expectations. All corrected
with original outcome/placement assertions retained. Self-review completed; no user
approval or independent review claimed. Native NOT RUN; full map corpus deferred
to final epic gate as recorded above.
