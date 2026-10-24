---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0005-chance-and-resolution
ticket: EP-0007-US-0005-TK-0001-defense-event-contract
title: "Результат перехвата и записи журнала"
stage: draft
layer: contracts
depends_on: ["EP-0007-US-0004-TK-0003-automatic-defense-scheduler"]
files_touched: 4
serves: ["AC-0001","AC-0002","AC-0003","AC-0004"]
created: 2026-10-04
revision: 1
---

# Результат перехвата и записи журнала

STATUS: DRAFT

## Why

Как игрок, я заранее вижу шанс, рассчитанный по базам аппаратов и навыкам операторов. При встрече выполняется ровно один бросок с этим шансом. Успех уничтожает оба снаряда, а промах оставляет торпеду в полёте. Этот тикет обеспечивает: результат перехвата и записи журнала.

## Decisions

[Согласованные D01–D17](../../Documentation.md) задают поведение; применяются критерии AC-0001, AC-0002, AC-0003, AC-0004 [истории](../EP-0007-US-0005-chance-and-resolution.md). Тикет не вводит новых пользовательских решений.

## Assumptions

Применяются A01–A08 эпика. Имена новых API предлагаемые; семантика и allowlist обязательны. Baseline проверен 2026-10-04; если код сдвинулся, актуализировать grounding до реализации. Дата отдельного ответа пользователя не выдумывается.

## Code context

Полный write allowlist относительно D:/DeepSpaceSaga/DSS. 4 файлов, включая тесты. Прочие файлы read-only.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/CombatJournalEntry.cs` | Новый файл этого тикета | Только описанные API и шаги |
| `src/DeepSpaceSaga.Contracts/CountermeasureSnapshot.cs` | Создаётся зависимостью EP-0007-US-0002-TK-0001-countermeasure-contract | Только описанные API и шаги |
| `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs` | Существует: src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:1 | Только описанные API и шаги |
| `tests/DeepSpaceSaga.Contracts.Tests/CountermeasureEventTests.cs` | Новый файл этого тикета | Названные регрессии; сохранить независимые assertions |

## Dependencies

Шаг 20 [плана](../../Tickets.md).
- [EP-0007-US-0004-TK-0003-automatic-defense-scheduler](../../EP-0007-US-0004-automatic-intercept-flight/EP-0007-US-0004-TK-0003-automatic-defense-scheduler/EP-0007-US-0004-TK-0003-automatic-defense-scheduler.md)

Зависимость должна быть реализована и проверена, а не просто существовать как Markdown. Не подменять отсутствующий production результат mock/stub.

## Public API after the change

CombatJournalEntry(eventId,motionTime,type,actor/target/projectile IDs,position,chanceTenths?,roll?,ratingBreakdown?,damage?,result?); AuthoritativeSnapshot.CombatJournal immutable; Countermeasure holds frozen breakdown and resolution marker.

## Implementation steps

1. Проверить текущие файлы allowlist и реализованность зависимостей; сохранить посторонние изменения.
2. События Launch/Intercept/Miss/Hit/Destroyed/SelfDestruct/TargetLost. Numeric payload separately from localized text. Operator IDs/names/skills/bases/ratings и chance fixed atlaunch, log not lookup mutable crew. Эффекты не сериализуются в journal, но journal факты сохраняются.
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

Matching project: `tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj`. Планируемые проверки:

- [ ] `Journal_entry_roundtrips_chance_roll_and_breakdown`
- [ ] `Old_snapshot_has_empty_journal`
- [ ] `Numeric_chance_is_same_value_for_map_and_roll`

Из корня DSS, после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore
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
