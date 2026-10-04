---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0006-defense-tactical-ui
ticket: EP-0007-US-0006-TK-0003-countermeasure-rendering
title: "Полёт, след и результат перехвата"
stage: draft
layer: client
depends_on: ["EP-0007-US-0006-TK-0002-defense-visual-settings"]
files_touched: 5
serves: ["AC-0001","AC-0004"]
created: 2026-10-04
revision: 1
---

# Полёт, след и результат перехвата

STATUS: DRAFT

## Why

Как игрок, я вижу полёт противоракеты, шанс перехвата и состояние аппаратов; могу отключить автоматическую защиту. Этот тикет обеспечивает: полёт, след и результат перехвата.

## Decisions

[Согласованные D01–D17](../../Documentation.md) задают поведение; применяются критерии AC-0001, AC-0004 [истории](../EP-0007-US-0006-defense-tactical-ui.md). Тикет не вводит новых пользовательских решений.

## Assumptions

Применяются A01–A08 эпика. Имена новых API предлагаемые; семантика и allowlist обязательны. Baseline проверен 2026-10-04; если код сдвинулся, актуализировать grounding до реализации. Дата отдельного ответа пользователя не выдумывается.

## Code context

Полный write allowlist относительно D:/DeepSpaceSaga/DSS. 5 файлов, включая тесты. Прочие файлы read-only.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs` | Новый файл этого тикета | Только описанные API и шаги |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs:1 | Только описанные API и шаги |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:1 | Только описанные API и шаги |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatEffectStore.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatEffectStore.cs:1 | Только описанные API и шаги |
| `tests/DeepSpaceSaga.Client.Tests/CountermeasureRenderingTests.cs` | Новый файл этого тикета | Названные регрессии; сохранить независимые assertions |

## Dependencies

Шаг 24 [плана](../../Tickets.md).
- [EP-0007-US-0006-TK-0002-defense-visual-settings](../../EP-0007-US-0006-defense-tactical-ui/EP-0007-US-0006-TK-0002-defense-visual-settings/EP-0007-US-0006-TK-0002-defense-visual-settings.md)

Зависимость должна быть реализована и проверена, а не просто существовать как Markdown. Не подменять отсутствующий production результат mock/stub.

## Public API after the change

Новый partial renderer использует authoritative PR route/trail; real-time deduplicated effects.

## Implementation steps

1. Проверить текущие файлы allowlist и реализованность зависимостей; сохранить посторонние изменения.
2. Рисовать body5px, solid trail, dashed prediction, encounter marker. Miss убирает прогноз, body идёт по coast; текст успех/промах2real sec. Success один ring0→50px/2sec. Не восстанавливать обычные torpedo travelled/terminal trails; self-destruct exception из US3. Не делать RNG/самостоятельный gameplay в renderer.
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

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Планируемые проверки:

- [ ] `Countermeasure_route_matches_snapshot`
- [ ] `Success_has_one_ring`
- [ ] `Miss_text_expires_in_real_time_while_paused`
- [ ] `Ordinary_torpedo_trails_stay_hidden`

Из корня DSS, после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
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
