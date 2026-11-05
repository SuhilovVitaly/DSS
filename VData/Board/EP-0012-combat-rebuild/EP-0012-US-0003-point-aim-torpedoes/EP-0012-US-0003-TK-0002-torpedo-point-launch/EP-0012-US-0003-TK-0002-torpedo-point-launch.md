---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0003-point-aim-torpedoes"
ticket: "EP-0012-US-0003-TK-0002-torpedo-point-launch"
title: "torpedo-point-launch"
stage: "implemented"
layer: "engine"
depends_on: ["EP-0012-US-0018-TK-0004-shared-point-forecast"]
files_touched: 8
serves: ["US3-AC2"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0003-TK-0002-torpedo-point-launch

## Why

[Торпеда в точку](../EP-0012-US-0003-point-aim-torpedoes.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: torpedo-point-launch; доказать US3-AC2 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Актуальный allowlist (8 файлов, включая tests). Execution scope расширен до изменений ниже перед кодом: point JSON требует нового BuildCombatState/operator projection; RuntimeMotion должен действительно двигать новый DTO; ранняя public projection обязана маскировать чужой captured endpoint. Это необходимые consumers launch path, без area damage/AI/Save18.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs:9; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationEngine.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.cs:17; public sealed partial class SimulationEngine : IDisposable | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/Rng/RngStreamNames.cs](../../../../../src/DeepSpaceSaga.Engine/Rng/RngStreamNames.cs) | Существует: src/DeepSpaceSaga.Engine/Rng/RngStreamNames.cs:9; internal static class RngStreamNames | Только изменения для scope этой карточки и served AC. |
| `src/DeepSpaceSaga.Engine/Combat/LaunchScatterRng.cs` | **NEW — proposed**, сейчас отсутствует | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0003TK0002Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |
| [src/DeepSpaceSaga.Engine/RuntimeMotion.cs](../../../../../src/DeepSpaceSaga.Engine/RuntimeMotion.cs) | Existing authoritative motion dispatcher | Point flight through shared Motion geometry. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.WeaponOperators.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationEngine.WeaponOperators.cs) | Legacy ratings/reload mandatory | Point AccuracySkill operator and profile/reload projection. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs` | NEW | Bounded player/NPC point executor and public own/observed projection helper. |

## Public API after the change

Proposed internal единый point-launch executor в SimulationEngine.Combat.cs для player/NPC и ProjectileKind; публичная граница — PlayerCommand US1-TK2. LaunchScatterRng использует named stream launch_scatter и persisted state/counter.

Execution refinement: executor вынесен в тематический partial SimulationEngine.PointCombat.cs, общий для player/NPC; сначала torpedo Kind, counter Kind подключается своим тикетом. SplitMix64 + Box-Muller pair, ровно два uniform draws на принятый пуск (также sigma=0), без cached normal/System.Random. State/counter доступны capture для Save18; файловое сохранение подключается и проверяется отдельной US9. Rejected команды не потребляют RNG и не меняют reload/sequence. Legacy dispatcher остаётся только для прежнего каталога до cleanup. Новые события launch не содержат endpoint/deadline/rating/module id. Движение выполняется сейчас; подрыв — следующий тикет.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Заменить `TryStartTorpedoFire` и `TryLaunchTorpedo` (`SimulationEngine.Combat.cs:25-163`). Внутренний исполнитель пуска один для игрока и NPC. Поток ГСЧ `launch_scatter` (`Engine/Rng/RngStreamNames.cs`).
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Point_launch_validates_readiness_operator_profile_range**: 70км принимается, >70 отклоняется до RNG.
- **Launch_is_seeded_idempotent_and_captures_parameters**: одинаковый seed/command даёт один projectile и неизменный captured damage/radius.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0003TK0002Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0003TK0002Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs src/DeepSpaceSaga.Engine/SimulationEngine.cs src/DeepSpaceSaga.Engine/Rng/RngStreamNames.cs src/DeepSpaceSaga.Engine/Combat/LaunchScatterRng.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0003TK0002Tests.cs
git diff --check
```

## Definition of Done

- Served AC US3-AC2 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 15 named / 242 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0003-tk0002-results.json).

Проверен настоящий Engine command path, общий player/NPC executor, два Gaussian draws на accepted launch, seeded sequence/state restore и независимые distribution bounds. Range проверяется до scatter, endpoint не clamped; пауза/дубликат/rejected launch не меняют flight/RNG, reload deadline включителен. Foreign snapshot и новый launch journal не содержат captured endpoint/deadline/ratings/module id. Полный Client: первый run 2128/1 (locale enum defect исправлен отдельно 809bd18e); повтор 2127/2 с p99 FAIL 2.227ms и2.378ms против2ms. Это открытый performance gate, причинная связь с текущим launch change не установлена; full Client PASS не заявляется. Файловое Save18, area detonation, AI и окончательная privacy migration остаются своими тикетами.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
