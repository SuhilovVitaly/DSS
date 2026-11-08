---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0009-anomaly-resolution
ticket: EP-0020-US-0009-TK-0002-salvo-anomalies
title: Поглощение и пыль в залпе
stage: draft
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0020-US-0009-TK-0001-anomaly-contracts]
files_touched: 4
serves: [US9-AC1, US9-AC2, US9-AC3]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Поглощение и пыль в залпе

## Why

Поглощение — отдельный бросок до попадания (D-26); пыль — причина пропуска шага (D-27).

## Decisions

Применяются [D-24–D-33](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-26: бросок поглощения делается только при `Absorption > 0`. A-27: поглощённый выстрел тратит ловушку цели. A-28: «в пыли» — по позиции на момент фиксации планов; позиции в залпе не меняются.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs` | `Candidates`, `DefaultTarget`, `Fire` после US-0006 TK-0003 | `Absorption` в кандидате; выбор по итоговому шансу; `InDust` при фиксации; бросок поглощения; поля выстрела. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleStatistics.cs` | :13-35 | `Absorbed`, `DustSkips`. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs` | :13-15, :42-60 | Поля US9-AC5. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/AnomalySalvoTests.cs` | нет | Ручные случаи с точным счётом RNG. |

## Public API after the change

`TacticalCandidate.Absorption`.

## Implementation steps

1. Кандидаты и выбор цели.
2. Пыль при фиксации; причина пропуска.
3. Бросок поглощения, ловушка, статистика, журнал.
4. Тесты.

## Out of scope

Сессия и ИИ движения.

## Invariants

Без аномалий поток RNG и исходы прежние.

## Tests

Проект `tests/DeepSpaceSaga.Engine.Tests`. Именованные проверки:

- `AnomalySalvoTests.Absorption_roll_precedes_hit_roll`
- `AnomalySalvoTests.No_extra_random_without_crossing`
- `AnomalySalvoTests.Absorbed_shot_has_no_damage_and_consumes_decoy`
- `AnomalySalvoTests.Drone_in_dust_skips_without_random`
- `AnomalySalvoTests.Default_target_uses_effective_chance`
- `AnomalySalvoTests.Shots_at_carriers_are_absorbed_too`
- `DecoyTests`
- `EvasionTests`
- `SalvoTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~AnomalySalvoTests|FullyQualifiedName~DecoyTests|FullyQualifiedName~EvasionTests|FullyQualifiedName~SalvoTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us9-tk2" --logger "trx;LogFileName=engine.trx"
dotnet format whitespace src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore --include src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleStatistics.cs src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US9-AC1, US9-AC2, US9-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Порядок бросков и места правок заданы.

## Исполнение

Не начато.
