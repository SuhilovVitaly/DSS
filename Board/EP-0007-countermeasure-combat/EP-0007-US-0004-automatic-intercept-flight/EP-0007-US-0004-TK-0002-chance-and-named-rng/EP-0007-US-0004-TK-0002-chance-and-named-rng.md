---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0004-automatic-intercept-flight
ticket: EP-0007-US-0004-TK-0002-chance-and-named-rng
title: "Формула и воспроизводимый именованный поток бросков"
stage: draft
layer: engine
depends_on: ["EP-0007-US-0004-TK-0001-interceptor-guidance"]
files_touched: 4
serves: ["AC-0001","AC-0004"]
created: 2026-10-04
revision: 1
---

# Формула и воспроизводимый именованный поток бросков

STATUS: DRAFT

## Why

Как атакующий игрок, я вижу ответный запуск защиты при входе торпеды в радиус100км. Противоракета летит с упреждением к моей торпеде. Аппарат занят до окончания полёта, затем перезаряжается10секунд. Этот тикет обеспечивает: формула и воспроизводимый именованный поток бросков.

## Decisions

[Согласованные D01–D17](../../Documentation.md) задают поведение; применяются критерии AC-0001, AC-0004 [истории](../EP-0007-US-0004-automatic-intercept-flight.md). Тикет не вводит новых пользовательских решений.

## Assumptions

Применяются A01–A08 эпика. Имена новых API предлагаемые; семантика и allowlist обязательны. Baseline проверен 2026-10-04; если код сдвинулся, актуализировать grounding до реализации. Дата отдельного ответа пользователя не выдумывается.

## Code context

Полный write allowlist относительно D:/DeepSpaceSaga/DSS. 4 файлов, включая тесты. Прочие файлы read-only.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Combat/InterceptionMath.cs` | Новый файл этого тикета | Только описанные API и шаги |
| `src/DeepSpaceSaga.Engine/Combat/CountermeasureRng.cs` | Новый файл этого тикета | Только описанные API и шаги |
| `src/DeepSpaceSaga.Engine/Rng/RngStreamNames.cs` | Существует: src/DeepSpaceSaga.Engine/Rng/RngStreamNames.cs:1 | Только описанные API и шаги |
| `tests/DeepSpaceSaga.Engine.Tests/InterceptionChanceTests.cs` | Новый файл этого тикета | Названные регрессии; сохранить независимые assertions |

## Dependencies

Шаг 18 [плана](../../Tickets.md).
- [EP-0007-US-0004-TK-0001-interceptor-guidance](../../EP-0007-US-0004-automatic-intercept-flight/EP-0007-US-0004-TK-0001-interceptor-guidance/EP-0007-US-0004-TK-0001-interceptor-guidance.md)

Зависимость должна быть реализована и проверена, а не просто существовать как Markdown. Не подменять отсутствующий production результат mock/stub.

## Public API after the change

ChanceTenths=round(clamp(50+Rpr−Rt,0,100)*10, AwayFromZero), integer0..1000; roll uniform integer1..1000, success roll<=ChanceTenths. Stream combat.countermeasure.intercept.v1 seeded through RngStreamSeedDerivation; persist algorithm version/state/counter.

## Implementation steps

1. Проверить текущие файлы allowlist и реализованность зависимостей; сохранить посторонние изменения.
2. Никакого RNG при launch/preview/eligibility/replan. На встрече ровно один draw, в том числе для100%;0% не запускается и drawнет. Использовать стабильный explicitly specified PRNG/state, не новый Random surrogate для теста; добавить golden vectors для реального helper seeded through named stream. При rejected/busy/lost target/refused early gate counter unchanged. Рейтинг не округлять перед subtraction. Пример base30 skill80 vs50 даёт32.0% дляPR; equal30/50→50.0%; boundaries0/100.
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

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Планируемые проверки:

- [ ] `Equal_ratings_give_500_tenths`
- [ ] `Skill_advantage_uses_difference_not_ratio`
- [ ] `Decimal_half_rounds_away_from_zero`
- [ ] `Named_stream_golden_vectors_and_resume`
- [ ] `No_draw_before_contact_or_after_resolution`

Из корня DSS, после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
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
