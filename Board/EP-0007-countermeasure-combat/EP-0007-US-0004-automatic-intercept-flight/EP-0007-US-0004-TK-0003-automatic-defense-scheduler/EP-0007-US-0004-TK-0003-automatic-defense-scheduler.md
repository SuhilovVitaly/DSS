---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0004-automatic-intercept-flight
ticket: EP-0007-US-0004-TK-0003-automatic-defense-scheduler
title: "Авторитетная автозащита и перезарядка"
stage: draft
layer: engine
depends_on: ["EP-0007-US-0004-TK-0002-chance-and-named-rng"]
files_touched: 5
serves: ["AC-0001","AC-0002","AC-0003","AC-0004"]
created: 2026-10-04
revision: 1
---

# Авторитетная автозащита и перезарядка

STATUS: DRAFT

## Why

Как атакующий игрок, я вижу ответный запуск защиты при входе торпеды в радиус100км. Противоракета летит с упреждением к моей торпеде. Аппарат занят до окончания полёта, затем перезаряжается10секунд. Этот тикет обеспечивает: авторитетная автозащита и перезарядка.

## Decisions

[Согласованные D01–D17](../../Documentation.md) задают поведение; применяются критерии AC-0001, AC-0002, AC-0003, AC-0004 [истории](../EP-0007-US-0004-automatic-intercept-flight.md). Тикет не вводит новых пользовательских решений.

## Assumptions

Применяются A01–A08 эпика. Имена новых API предлагаемые; семантика и allowlist обязательны. Baseline проверен 2026-10-04; если код сдвинулся, актуализировать grounding до реализации. Дата отдельного ответа пользователя не выдумывается.

## Code context

Полный write allowlist относительно D:/DeepSpaceSaga/DSS. 5 файлов, включая тесты. Прочие файлы read-only.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs` | Создаётся зависимостью EP-0007-US-0002-TK-0005-defense-bootstrap | Только описанные API и шаги |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs:1 | Только описанные API и шаги |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.cs:1 | Только описанные API и шаги |
| `src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs:1 | Только описанные API и шаги |
| `tests/DeepSpaceSaga.Engine.Tests/AutomaticDefenseTests.cs` | Новый файл этого тикета | Названные регрессии; сохранить независимые assertions |

## Dependencies

Шаг 19 [плана](../../Tickets.md).
- [EP-0007-US-0004-TK-0002-chance-and-named-rng](../../EP-0007-US-0004-automatic-intercept-flight/EP-0007-US-0004-TK-0002-chance-and-named-rng/EP-0007-US-0004-TK-0002-chance-and-named-rng.md)

Зависимость должна быть реализована и проверена, а не просто существовать как Markdown. Не подменять отсутствующий production результат mock/stub.

## Public API after the change

AdvanceDefenseTo / NextDefenseBoundary; Ready→Guiding→Reloading→Ready. Explicit enable/disable commands. Использовать InterceptionMath/RNG из TK-0002; не создавать временный дубликат формулы.

## Implementation steps

1. Проверить текущие файлы allowlist и реализованность зависимостей; сохранить посторонние изменения.
2. Обрабатывать вход радиуса и конец reload внутри advancing interval, чтобы большой шаг не пропускал окно; при speed0 нет auto launches. Проверка достижимости/arrival before hit и frozen chance перед side effects. Zero/no-solution не расходуют attempt. Резервировать попытку до spawn, stable target order earliest predicted hit then ID; cooldown начинается при реальном окончании PR. Автоdisable влияет только на будущие пуски, сохранённое true по умолчанию. PR исключена из generic торпедных collisions тоже: иначе провальный interception уничтожит торпеду обычным collision path. IDs проверять OrdinalIgnoreCase как ScenarioLoader. Прямой torpedo.fire в Engine отклоняет Countermeasure target до side effects, независимо от UI.
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

- [ ] `Defense_fires_on_range_entry_only_after_time_advances`
- [ ] `Reload_end_fires_at_pending_inrange_torpedo`
- [ ] `Disable_preserves_inflight_countermeasure`
- [ ] `Zero_chance_and_no_intercept_do_not_consume_attempt`
- [ ] `Large_step_does_not_skip_launch_window`
- [ ] `Countermeasure_is_excluded_from_generic_collisions`

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
