---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0004-automatic-intercept-flight
ticket: EP-0007-US-0004-TK-0001-interceptor-guidance
title: "Общая геометрия перехвата движущейся торпеды"
stage: draft
layer: motion
depends_on: ["EP-0007-US-0003-TK-0004-self-destruct-panel"]
files_touched: 3
serves: ["AC-0001","AC-0003"]
created: 2026-10-04
revision: 1
---

# Общая геометрия перехвата движущейся торпеды

STATUS: DRAFT

## Why

Как атакующий игрок, я вижу ответный запуск защиты при входе торпеды в радиус100км. Противоракета летит с упреждением к моей торпеде. Аппарат занят до окончания полёта, затем перезаряжается10секунд. Этот тикет обеспечивает: общая геометрия перехвата движущейся торпеды.

## Decisions

[Согласованные D01–D17](../../Documentation.md) задают поведение; применяются критерии AC-0001, AC-0003 [истории](../EP-0007-US-0004-automatic-intercept-flight.md). Тикет не вводит новых пользовательских решений.

## Assumptions

Применяются A01–A08 эпика. Имена новых API предлагаемые; семантика и allowlist обязательны. Baseline проверен 2026-10-04; если код сдвинулся, актуализировать grounding до реализации. Дата отдельного ответа пользователя не выдумывается.

## Code context

Полный write allowlist относительно D:/DeepSpaceSaga/DSS. 3 файлов, включая тесты. Прочие файлы read-only.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Motion/CountermeasureGuidanceMath.cs` | Новый файл этого тикета | Только описанные API и шаги |
| `src/DeepSpaceSaga.Motion/LinearMotionPredictor.cs` | Существует: src/DeepSpaceSaga.Motion/LinearMotionPredictor.cs:1 | Только описанные API и шаги |
| `tests/DeepSpaceSaga.Motion.Tests/CountermeasureGuidanceTests.cs` | Новый файл этого тикета | Названные регрессии; сохранить независимые assertions |

## Dependencies

Шаг 17 [плана](../../Tickets.md).
- [EP-0007-US-0003-TK-0004-self-destruct-panel](../../EP-0007-US-0003-torpedo-self-destruct/EP-0007-US-0003-TK-0004-self-destruct-panel/EP-0007-US-0003-TK-0004-self-destruct-panel.md)

Зависимость должна быть реализована и проверена, а не просто существовать как Markdown. Не подменять отсутствующий production результат mock/stub.

## Public API after the change

Plan(defenderPose,targetTorpedoRoute,now,speed12,turn90)->route/nullable encounter; no ship aft offset. Target trajectory может иметь дугу, не только постоянную скорость по прямой.

## Implementation steps

1. Проверить текущие файлы allowlist и реализованность зависимостей; сохранить посторонние изменения.
2. Переиспользовать TorpedoGuidanceMath/PredictSegment для shared geometry, не приравнивать projectile target к неподвижной точке. Проверять физическую встречу и arrival before torpedo's first predicted hull contact (500m), не центр позднее. ВремяPR/цели согласовано; bounded turn. Нет реального решения — null. Engine+UI используют same route.
3. Добавить перечисленные ниже проверки реального поведения. Зафиксировать фактический результат, не объявлять планируемые тесты пройденными.

## Out of scope

Другие production layers, файлы внеallowlist, наступательный AI и новые игровые механики вне эпика. Commit/push не входят. Если требуется шестой файл, сначала отдельное согласованное расширениеscope или новый тикет.

## Invariants

- Числа/формулы/границы берутся из эпика; UI не решает исход боя.
- Physical gameplay time и monotonic effect time не смешиваются (EngineRequirements §2 и временной контракт эпика).
- Snapshot immutable; Client не обращается напрямую кEngine (CLAUDE Architecture).
- Непринятые команды и потеря цели до встречи не потребляют draw. Состояние/attempt/frozenchance переживают SaveLoad.
- Совместимость ID следует loader OrdinalIgnoreCase. Existing ordinary torpedo trails остаются скрытыми.

## Tests

Matching project: `tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj`. Планируемые проверки:

- [ ] `Intercept_respects_initial_heading_and_turn_rate`
- [ ] `Curved_torpedo_path_has_consistent_intercept`
- [ ] `Meeting_after_hull_contact_is_not_eligible`
- [ ] `Prediction_is_partition_invariant`

Из корня DSS, после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Motion/DeepSpaceSaga.Motion.csproj --no-restore
git diff --check
```

При scoped lint использовать dotnet format --verify-no-changes --include только изменённых C# файлов. Baseline failure документировать с точной командой; не расширять allowlist молча. Native проверка требуется там, где AC касается реального изображения/input; headless результат её не заменяет.

## Definition of Done

- [ ] Реализованы API/шаги вallowlist, covered критерии наблюдаемы.
- [ ] Named tests и сборка слоя пройдены; baseline ограничения записаны отдельно.
- [ ] Dependencies действительно готовы; нет временных заглушек или скрытой работы.
- [ ] Проверены pause/time domains/idempotency/persistence в применимой части.
- [ ] Diff reviewed; runtime/native статус описан честно.

## Self-containment check

Контракт, ограничения, файлы, зависимости и проверки перечислены здесь; формулы/таблица времени централизованы в связанном эпике. Не требуется восстанавливать смысл коротких ответов из чата. Новое обнаруженное несоответствие оформляется как gap, а не произвольное расширение работ.
