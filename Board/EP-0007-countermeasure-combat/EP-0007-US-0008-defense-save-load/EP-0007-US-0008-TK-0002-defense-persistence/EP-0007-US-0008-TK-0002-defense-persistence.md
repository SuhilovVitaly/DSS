---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0008-defense-save-load
ticket: EP-0007-US-0008-TK-0002-defense-persistence
title: "Сохранение и восстановление всех фаз"
stage: draft
layer: engine
depends_on: ["EP-0007-US-0008-TK-0001-defense-save-schema"]
files_touched: 5
serves: ["AC-0001","AC-0002","AC-0003"]
created: 2026-10-04
revision: 1
---

# Сохранение и восстановление всех фаз

STATUS: DRAFT

## Why

Как игрок, я сохраняю бой в любой фазе и после загрузки получаю то же продолжение без нового броска и повторных эффектов. Этот тикет обеспечивает: сохранение и восстановление всех фаз.

## Decisions

[Согласованные D01–D17](../../Documentation.md) задают поведение; применяются критерии AC-0001, AC-0002, AC-0003 [истории](../EP-0007-US-0008-defense-save-load.md). Тикет не вводит новых пользовательских решений.

## Assumptions

Применяются A01–A08 эпика. Имена новых API предлагаемые; семантика и allowlist обязательны. Baseline проверен 2026-10-04; если код сдвинулся, актуализировать grounding до реализации. Дата отдельного ответа пользователя не выдумывается.

## Code context

Полный write allowlist относительно D:/DeepSpaceSaga/DSS. 5 файлов, включая тесты. Прочие файлы read-only.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatPersistence.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.CombatPersistence.cs:1 | Только описанные API и шаги |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs` | Создаётся зависимостью EP-0007-US-0002-TK-0005-defense-bootstrap | Только описанные API и шаги |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs` | Создаётся зависимостью EP-0007-US-0007-TK-0001-authoritative-combat-journal | Только описанные API и шаги |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.cs:1 | Только описанные API и шаги |
| `tests/DeepSpaceSaga.Engine.Tests/CountermeasureSaveLoadTests.cs` | Новый файл этого тикета | Названные регрессии; сохранить независимые assertions |

## Dependencies

Шаг 30 [плана](../../Tickets.md).
- [EP-0007-US-0008-TK-0001-defense-save-schema](../../EP-0007-US-0008-defense-save-load/EP-0007-US-0008-TK-0001-defense-save-schema/EP-0007-US-0008-TK-0001-defense-save-schema.md)

Зависимость должна быть реализована и проверена, а не просто существовать как Markdown. Не подменять отсутствующий production результат mock/stub.

## Public API after the change

BuildSave/Restore combat captures all PR, journal and actual named RNG state.

## Implementation steps

1. Проверить текущие файлы allowlist и реализованность зависимостей; сохранить посторонние изменения.
2. Roundtrip через реальный serializer/loader/session. Не генерировать новыеIDs/назначения/броски при restore; deadlines в physical domain. Проверить Guiding/MissedCoast/Reloading/disabled/NoOperator/targetlost, до и после contact. После старого промаха attempt остаётся consumed.
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

- [ ] `Guidance_save_preserves_future_roll`
- [ ] `Missed_coast_save_preserves_expiry_and_attempt`
- [ ] `Reload_and_auto_disable_roundtrip`
- [ ] `Journal_and_rng_resume_exactly`
- [ ] `Invalid_load_does_not_replace_running_world`

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
