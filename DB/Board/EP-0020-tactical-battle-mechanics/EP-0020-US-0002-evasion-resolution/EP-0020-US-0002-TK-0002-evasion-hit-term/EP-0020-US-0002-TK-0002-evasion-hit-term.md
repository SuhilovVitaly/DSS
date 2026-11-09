---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0002-evasion-resolution
ticket: EP-0020-US-0002-TK-0002-evasion-hit-term
title: Слагаемое уклонения в шансе попадания
stage: done
layer: motion
test_project: tests/DeepSpaceSaga.Motion.Tests
depends_on: [EP-0020-US-0002-TK-0001-evasion-contracts]
files_touched: 2
serves: [US2-AC2]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Слагаемое уклонения в шансе попадания

## Why

Шанс попадания считается одной функцией для движка и прогноза клиента; уклонение — её новое слагаемое.

## Decisions

Применяются [D-01–D-13](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Слагаемое прибавляется после щита (`sum += evasion`); прибавление 0.0 не меняет значение double, поэтому прежние шансы остаются побитно равными.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Motion/TacticalBattle/TacticalHitChance.cs` | :3 `TacticalHitRules`, :5-7 `TacticalShotInput`, :8-9 `TacticalHitTerms`, :17-39 `Terms` | `double EvasionPenalty = 0` в конец `TacticalHitRules`; `bool TargetEvading = false` в конец `TacticalShotInput`; `double Evasion = 0` в конец `TacticalHitTerms`; слагаемое в сумме. |
| `tests/DeepSpaceSaga.Motion.Tests/TacticalBattle/LimitsAndChanceTests.cs` | :51-80 слагаемые, :105-133 V4 trace | Новые проверки; trace без изменений. |

## Public API after the change

`TacticalHitTerms.Evasion`; `TacticalHitRules.EvasionPenalty`; `TacticalShotInput.TargetEvading`.

## Implementation steps

1. Расширить records параметрами по умолчанию.
2. Добавить слагаемое и тесты: −20 для дрона спереди, сзади (с бонусом кормы) и для стрелка по носителю; ограничение 5/95 после слагаемого; 0 без уклонения.

## Out of scope

Где берётся признак уклонения (TK-0003).

## Invariants

`AllPublishedSeedOneShotChancesMatchAtOriginalSixDecimalPrecision` проходит без изменения допуска.

## Tests

Проект `tests/DeepSpaceSaga.Motion.Tests`. Именованные проверки:

- `LimitsAndChanceTests.Evasion_term_subtracts_penalty_before_clamp`
- `LimitsAndChanceTests.Evasion_applies_to_rear_shots`
- `LimitsAndChanceTests.AllPublishedSeedOneShotChancesMatchAtOriginalSixDecimalPrecision`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Motion/DeepSpaceSaga.Motion.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --no-restore --filter "FullyQualifiedName~LimitsAndChanceTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us2-tk2" --logger "trx;LogFileName=motion.trx"
dotnet format whitespace src/DeepSpaceSaga.Motion/DeepSpaceSaga.Motion.csproj --no-restore --include src/DeepSpaceSaga.Motion/TacticalBattle/TacticalHitChance.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US2-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Формула и место слагаемого заданы.

## Исполнение

2026-11-08: Evasion слагаемое после Shield до clamp, только у цели-дрона; optional defaults сохраняют старых callers. Ручные front/rear/clamp и опубликованный V4 trace: LimitsAndChanceTests 6/6 PASS; Motion build/scoped format/diff-check PASS. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us2-tk2/final/tests.trx. Первый тест использовал неверный вход Accuracy128 для raw110; исправлен на108 без изменения production. Self-review: carrier-target guard, бонус кормы, нижний/верхний clamp, отсутствие RNG; US2-AC2 покрыт общей математикой, подключение движка следует в TK-0003.
