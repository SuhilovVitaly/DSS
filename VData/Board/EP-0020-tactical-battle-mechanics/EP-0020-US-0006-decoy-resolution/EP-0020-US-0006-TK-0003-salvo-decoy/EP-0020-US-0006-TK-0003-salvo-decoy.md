---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0006-decoy-resolution
ticket: EP-0020-US-0006-TK-0003-salvo-decoy
title: Ловушки в разрешении залпа и правило ИИ
stage: draft
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0020-US-0006-TK-0002-decoy-hit-transform]
files_touched: 5
serves: [US6-AC2, US6-AC3, US6-AC4, US6-AC5]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Ловушки в разрешении залпа и правило ИИ

## Why

Залп должен знать пущенные ловушки до первого шага и тратить каждую на первый выстрел по её дрону.

## Decisions

Применяются [D-15–D-23](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-17: «первый выстрел» — первый по очерёдности огня шаг с броском по этому дрону; пропущенный шаг ловушку не тратит. A-18: решение ИИ принимается после фиксации целей и уклонений обеих сторон по зафиксированным планам противника; шанс угрозы считается без ловушки. A-19: ловушку можно пустить без цели и при заклинившем оружии; уклоняющийся дрон ловушку не пускает (D-16).

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs` | `Fire` после US-0002 TK-0003: `TacticalFirePlan`, фиксация планов, шаги | `TacticalFirePlan(string? TargetId, bool Evade, bool Decoy)`; `DefaultPlan` без ловушки; после фиксации — `AiDecoys(...)` для дронов ИИ; множество `Lured`, списание заряда, `WithDecoy` для первого выстрела, `Decoyed`/`ChanceWithoutDecoy` в выстреле. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattle.cs` | `Firing`, `Step/Run` | Ловушки ИИ для обеих сторон в `Run/Step`. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleStatistics.cs` | :13-35 | Счётчики `DecoysLaunched`, `ShotsDecoyed`, `HitsDecoyed`. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs` | :13-15 `Shot`, :42-60 `Targets/Incoming`, :64-66 `Rules` | `Decoyed`, `ChanceWithoutDecoy`; `ChanceIfDecoy` для первого входящего по очерёдности; `DecoysLaunched`; правила. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/DecoyTests.cs` | нет | Ручные случаи с точным счётом вызовов RNG. |

## Public API after the change

`TacticalFirePlan.Decoy`; `TacticalBattleSalvo.AiDecoys(...)`.

## Implementation steps

1. Расширить план и фиксацию: проверить заряд и несовместимость с уклонением (иначе `ArgumentException`, как для недопустимой цели), списать заряд, собрать `Lured`.
2. Правило ИИ D-21 после фиксации; для явного пути и `Run/Step` одинаково.
3. В шаге с броском по дрону из `Lured`: шанс `WithDecoy(chance, Decoy.MissFactor, Rules.Hit.MinChance)`, отметить выстрел, удалить дрон из `Lured`.
4. Статистика и снимки.
5. Тесты `DecoyTests` (именованные проверки ниже).

## Out of scope

Сессия и валидация приказа (TK-0004).

## Invariants

Без ловушек поток RNG, исходы и `RandomCalls` совпадают с прежними для одних и тех же приказов.

## Tests

Проект `tests/DeepSpaceSaga.Engine.Tests`. Именованные проверки:

- `DecoyTests.Decoy_lowers_only_first_shot_in_fire_order`
- `DecoyTests.Skipped_step_does_not_consume_decoy`
- `DecoyTests.Unused_decoy_expires_after_salvo_and_charge_is_spent`
- `DecoyTests.Decoy_launch_draws_no_random`
- `DecoyTests.Ai_launches_decoy_at_threat_threshold`
- `DecoyTests.Ai_does_not_launch_when_evading_or_empty`
- `DecoyTests.Incoming_forecast_marks_only_first_shot`
- `SalvoTests`
- `EvasionTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~DecoyTests|FullyQualifiedName~EvasionTests|FullyQualifiedName~SalvoTests|FullyQualifiedName~InitiativeFireTests|FullyQualifiedName~OutcomeTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us6-tk3" --logger "trx;LogFileName=engine.trx"
dotnet format whitespace src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore --include src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattle.cs src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleStatistics.cs src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US6-AC2, US6-AC3, US6-AC4, US6-AC5 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Правило «первого выстрела», момент списания и правило ИИ заданы.

## Исполнение

Не начато.
