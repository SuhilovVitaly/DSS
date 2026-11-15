---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0008-anomaly-field
ticket: EP-0020-US-0008-TK-0002-anomaly-math
title: Математика поля аномалий
stage: done
layer: motion
test_project: tests/DeepSpaceSaga.Motion.Tests
depends_on: [EP-0020-US-0008-TK-0001-anomaly-rules-content]
files_touched: 2
serves: [US8-AC2]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Математика поля аномалий

## Why

Одна функция поля и доли пути для движка, ИИ, прогноза и отрисовки, иначе правило «внутри» разойдётся с картинкой.

## Decisions

Применяются [D-24–D-33](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-24: доля пути считается по серединам `absorbSamples` равных подотрезков (а не аналитически), чтобы результат был одинаков на всех платформах и легко объясним; порог — `≥ zoneEdge`.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Motion/TacticalBattle/TacticalAnomalyMath.cs` | нет | `TacticalAnomaly`, `TacticalClump`, `TacticalAnomalyKind`; `Field`, `FieldWithGradient` (для отрисовки), `Inside`, `Absorption`, `Segments`. |
| `tests/DeepSpaceSaga.Motion.Tests/TacticalBattle/AnomalyMathTests.cs` | нет | Ручные значения. |

## Public API after the change

`public static class TacticalAnomalyMath` с методами US8-AC2 и `FieldWithGradient(kind, x, y, out gx, out gy)`.

## Implementation steps

1. Реализовать формулы US8-AC2; градиент аналитический.
2. Тесты: одно пятно — значение в центре и на 1σ; поворот на 90°; порог; отрезок целиком внутри = 1, снаружи = 0, наполовину = 0,5 ± 1/200; слияние интервалов; градиент против конечной разности.

## Out of scope

Генерация (TK-0003).

## Invariants

Функции чистые, без состояния и RNG.

## Tests

Проект `tests/DeepSpaceSaga.Motion.Tests`. Именованные проверки:

- `AnomalyMathTests.Field_of_single_clump`
- `AnomalyMathTests.Inside_uses_zone_edge`
- `AnomalyMathTests.Absorption_is_share_of_samples_inside_heat`
- `AnomalyMathTests.Segments_merge_adjacent_samples`
- `AnomalyMathTests.Gradient_matches_finite_difference`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Motion/DeepSpaceSaga.Motion.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --no-restore --filter "FullyQualifiedName~AnomalyMathTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us8-tk2" --logger "trx;LogFileName=motion.trx"
dotnet format whitespace src/DeepSpaceSaga.Motion/DeepSpaceSaga.Motion.csproj --no-restore --include src/DeepSpaceSaga.Motion/TacticalBattle/TacticalAnomalyMath.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US8-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Формулы заданы US8-AC2.

## Исполнение

В процессе. API чистых static функций получает ImmutableArray аномалий и captured background/zoneEdge/samples явно; скрытого глобального поля или чисел100/170/200 в math нет. Rotation — радианы по A-25 U[0,π). Segments возвращает TacticalAnomalySegment(T0,T1); Absorption считает целое число midpoint hits/samples, не суммирует округлённые длины интервалов. Нулевая длина использует ту же midpoint принадлежность.

Завершено: Motion20/20PASS включая8AnomalyMath и12Limits/Chance. Hand values центр/1sigma/90°, суммирование/разделениеkind, ≥граница, целиком/вне/половина/reverse/zero-length, слияние/дваинтервала, градиент hand+finite difference, empty иinvalid inputs проверены. Самопроверка устранила underflow/overflow промежуточного sigma²: нормированные координаты до возведения в квадрат; тестыsigma1e−200/1e200PASS. Pure functions без mutable state/RNG. Build/scoped-format/diff-checkPASS. US8-AC2 покрыт. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us8-tk2/final/tests.trx,format-motion.log,format-tests.log.
