---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0005-chance-and-resolution
ticket: EP-0007-US-0005-TK-0002-resolve-interception
title: "Один контакт, успех или двухсекундный промах"
stage: draft
layer: engine
depends_on: ["EP-0007-US-0005-TK-0001-defense-event-contract"]
files_touched: 4
serves: ["AC-0001","AC-0002","AC-0003","AC-0004"]
created: 2026-10-04
revision: 1
---

# Один контакт, успех или двухсекундный промах

STATUS: DRAFT

## Why

Как игрок, я заранее вижу шанс, рассчитанный по базам аппаратов и навыкам операторов. При встрече выполняется ровно один бросок с этим шансом. Успех уничтожает оба снаряда, а промах оставляет торпеду в полёте. Этот тикет обеспечивает: один контакт, успех или двухсекундный промах.

## Decisions

[Согласованные D01–D17](../../Documentation.md) задают поведение; применяются критерии AC-0001, AC-0002, AC-0003, AC-0004 [истории](../EP-0007-US-0005-chance-and-resolution.md). Тикет не вводит новых пользовательских решений.

## Assumptions

Применяются A01–A08 эпика. Имена новых API предлагаемые; семантика и allowlist обязательны. Baseline проверен 2026-10-04; если код сдвинулся, актуализировать grounding до реализации. Дата отдельного ответа пользователя не выдумывается.

## Code context

Полный write allowlist относительно D:/DeepSpaceSaga/DSS. 4 файлов, включая тесты. Прочие файлы read-only.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs` | Создаётся зависимостью EP-0007-US-0002-TK-0005-defense-bootstrap | Только описанные API и шаги |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs:1 | Только описанные API и шаги |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.cs:1 | Только описанные API и шаги |
| `tests/DeepSpaceSaga.Engine.Tests/CountermeasureResolutionTests.cs` | Новый файл этого тикета | Названные регрессии; сохранить независимые assertions |

## Dependencies

Шаг 21 [плана](../../Tickets.md).
- [EP-0007-US-0005-TK-0001-defense-event-contract](../../EP-0007-US-0005-chance-and-resolution/EP-0007-US-0005-TK-0001-defense-event-contract/EP-0007-US-0005-TK-0001-defense-event-contract.md)

Зависимость должна быть реализована и проверена, а не просто существовать как Markdown. Не подменять отсутствующий production результат mock/stub.

## Public API after the change

ResolveFirstIntercept(contact) records terminal attempt exactly once; successful impact event is single shared explosion; missed state has heading frozen + coastExpiresAt=contact+2000.

## Implementation steps

1. Проверить текущие файлы allowlist и реализованность зависимостей; сохранить посторонние изменения.
2. Подключить named math/RNG helper и frozen chance. Использовать swept target-only contact на интервале; один бросок с stored chance. Success удаляетtorpedo+PR, освобождает torpedo operator, defense reload now+10000; HP не меняется. Miss сохраняетtorpedo, PR прямойкурс ещё2000ms, operator busy until expiry thenreload10000. If target disappears during missed coast, phase ужеразрешена: coast stays2s, не превращается в target-loss abort. Same-time порядок подробно задан в эпике, не зависит от перечисления коллекций.
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

- [ ] `Successful_intercept_removes_both_once_and_no_hull_damage`
- [ ] `Miss_coasts_straight_then_reload_starts`
- [ ] `Failed_pr_never_retries_or_hits_torpedo_generically`
- [ ] `Lost_target_skips_rng_and_starts_reload`
- [ ] `Frozen_chance_is_used_at_contact`

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
