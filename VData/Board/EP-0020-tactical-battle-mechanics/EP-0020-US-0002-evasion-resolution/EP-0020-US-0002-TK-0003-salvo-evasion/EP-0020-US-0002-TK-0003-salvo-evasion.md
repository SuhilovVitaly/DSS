---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0002-evasion-resolution
ticket: EP-0020-US-0002-TK-0003-salvo-evasion
title: Уклонение в разрешении залпа и правило ИИ
stage: done
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0020-US-0002-TK-0002-evasion-hit-term]
files_touched: 6
serves: [US2-AC2, US2-AC3, US2-AC4]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Уклонение в разрешении залпа и правило ИИ

## Why

Залп должен знать множество уклоняющихся до первого шага, потому что оно меняет шанс всех выстрелов по ним.

## Decisions

Применяются [D-01–D-13](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-01: сознание списывается при фиксации планов, даже если дрон выбудет раньше своего шага. A-02: уклонение действует один залп. A-05: правило ИИ применяется к каждому дрону, решение для которого принимает ИИ (обе стороны в `Run/Step`, противник в сессии). A-09: строгое `Hull < 0.5 × MaxHull`. Порядок причин пропуска: BattleEnded, ShooterDestroyed, ShooterDisabled, **Evading**, WeaponJammed, NoTarget/Withheld.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs` | :24-48 `Candidates`/`DefaultTarget`, :50-154 `Fire` | `TacticalFirePlan(string? TargetId, bool Evade)`; колбэк возвращает план; `DefaultPlan(battle, drone, candidates)` = правило ИИ, иначе `DefaultTarget`; множество `Evading` при фиксации; списание сознания; `SkipReason.Evading`; `TargetEvading` во входе шанса; `EvasionPenalty` из правил. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattle.cs` | :33-39 `Firing`, :63-85 `Step/Run` | Сигнатура колбэка; `Run/Step` используют `DefaultPlan`. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleStatistics.cs` | :13-35 | Счётчики `Evasions`, `ShotsAtEvading`, `HitsOnEvading`. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs` | :13-15 `Shot`, :42-60 `Targets/Incoming`, :64-66 `Rules` | `TargetEvading`; `ChanceIfEvading`; правила уклонения. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/EvasionTests.cs` | нет | Ручные случаи. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SalvoTests.cs` | :73, :187, :229 | Обновить сигнатуру колбэка; ожидания прежние. |

## Public API after the change

`internal sealed record TacticalFirePlan(string? TargetId, bool Evade)`; `TacticalBattleSalvo.DefaultPlan(...)`; `TacticalBattle.Firing(Func<TacticalDrone, ImmutableArray<TacticalCandidate>, TacticalFirePlan>? plansFor)`.

## Implementation steps

1. Ввести `TacticalFirePlan` и перевести колбэк; `null` колбэк = `DefaultPlan` для всех.
2. При фиксации планов (`:53-60`) собрать `Evading`, проверить сознание (иначе `ArgumentException`, как для недопустимой цели) и списать `cost`.
3. В шаге: причина `Evading` по порядку из Assumptions; бросков нет.
4. В расчёте кандидатов во время залпа передавать `TargetEvading = Evading.Contains(target.Id)`; вне залпа — `false`.
5. Статистика и снимки.
6. Тесты `EvasionTests` (именованные проверки ниже), каждый со своей ручной фикстурой и точным счётом вызовов RNG.

## Out of scope

Сессия, валидация приказа, открытие фазы (TK-0004).

## Invariants

Без уклонения поток RNG, исходы и `RandomCalls` совпадают с прежними для одних и тех же приказов.

## Tests

Проект `tests/DeepSpaceSaga.Engine.Tests`. Именованные проверки:

- `EvasionTests.Evading_drone_does_not_fire_and_draws_no_random`
- `EvasionTests.Every_shot_at_evading_drone_gets_penalty_including_rear_and_carrier`
- `EvasionTests.Cost_is_charged_at_plan_freeze_even_if_destroyed_before_step`
- `EvasionTests.Ai_evades_strictly_below_half_hull_with_enough_consciousness`
- `EvasionTests.Ai_does_not_evade_below_min_consciousness`
- `EvasionTests.Evading_skip_reason_priority`
- `SalvoTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~EvasionTests|FullyQualifiedName~SalvoTests|FullyQualifiedName~InitiativeFireTests|FullyQualifiedName~OutcomeTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us2-tk3" --logger "trx;LogFileName=engine.trx"
dotnet format whitespace src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore --include src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattle.cs src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleStatistics.cs src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US2-AC2, US2-AC3, US2-AC4 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Порядок причин, момент списания и правило ИИ заданы.

## Исполнение

2026-11-08: планы фиксируются и валидируются целиком до списания; Evading действует весь залп; сознание списано даже при раннем уничтожении; порядок skip и статистика добавлены. Incoming.ChanceIfEvading рассчитан тем же Motion слагаемым, правила и shot metadata передаются в снимок. Evasion/Salvo/InitiativeFire/Outcome 44/44 PASS, build/scoped format/diff-check PASS. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us2-tk3/tests.trx. Self-review: atomic freeze, AI strict hull threshold, jam допускает evade, RNG только по фактическим выстрелам; US2-AC2/3/4 покрыты. Носители существующего Engine не имеют собственного шага стрельбы (стреляют дроны); у цели-носителя штрафа нет. Сессия пока оборачивает прежние цели без изменения поведения, TK-0004 подключает флаг.

## Scope перед реализацией

Смена callback с string на TacticalFirePlan требует компилирующей миграции callers в TacticalBattleSession.cs и тестах ConsciousnessTests, OutcomeTests, InitiativeFireTests, GoldenBattleTests помимо SalvoTests. В этом тикете callers оборачивают прежнюю цель в plan без уклонения; изменение session/explicit AI поведения остаётся в TK-0004/0005.
