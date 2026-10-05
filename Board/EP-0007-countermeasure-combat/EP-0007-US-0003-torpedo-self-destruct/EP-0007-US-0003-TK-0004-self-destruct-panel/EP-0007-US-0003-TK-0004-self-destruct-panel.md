---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0003-torpedo-self-destruct
ticket: EP-0007-US-0003-TK-0004-self-destruct-panel
title: "Кнопка Самоуничтожение и визуальный результат"
stage: draft
layer: client
depends_on: ["EP-0007-US-0009-TK-0004-catalog-regression-compatibility"]
files_touched: 5
serves: ["AC-0001","AC-0002"]
created: 2026-10-04
revision: 1
---

# Кнопка Самоуничтожение и визуальный результат

STATUS: DRAFT

## Why

Как игрок, я прекращаю затянувшуюся атаку кнопкой в панели аппарата. Торпеда взрывается без урона и освобождает оператора, даже на паузе. Преследовавшая её противоракета теряет цель. Этот тикет обеспечивает: кнопка самоуничтожение и визуальный результат.

## Decisions

[Согласованные D01–D17](../../Documentation.md) задают поведение; применяются критерии AC-0001, AC-0002 [истории](../EP-0007-US-0003-torpedo-self-destruct.md). Тикет не вводит новых пользовательских решений.

## Assumptions

Применяются A01–A08 эпика. Имена новых API предлагаемые; семантика и allowlist обязательны. Baseline проверен 2026-10-04; если код сдвинулся, актуализировать grounding до реализации. Дата отдельного ответа пользователя не выдумывается.

## Code context

Полный write allowlist относительно D:/DeepSpaceSaga/DSS. 5 файлов, включая тесты. Прочие файлы read-only.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs:1 | Только описанные API и шаги |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs:1 | Только описанные API и шаги |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:1 | Только описанные API и шаги |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatEffectStore.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatEffectStore.cs:1 | Только описанные API и шаги |
| `tests/DeepSpaceSaga.Client.Tests/TorpedoSelfDestructUiTests.cs` | Новый файл этого тикета | Названные регрессии; сохранить независимые assertions |

## Dependencies

Шаг 16 [плана](../../Tickets.md).
- [EP-0007-US-0009-TK-0004-catalog-regression-compatibility](../../EP-0007-US-0009-countermeasure-acceptance/EP-0007-US-0009-TK-0004-catalog-regression-compatibility/EP-0007-US-0009-TK-0004-catalog-regression-compatibility.md)

Зависимость должна быть реализована и проверена, а не просто существовать как Markdown. Не подменять отсутствующий production результат mock/stub.

## Public API after the change

Панель launcher: Fire/Самоуничтожение; command includes captured active projectile ID. Effect store can retain final trail only for explicitly required terminal kinds.

## Implementation steps

1. Проверить текущие файлы allowlist и реализованность зависимостей; сохранить посторонние изменения.
2. Не требуется выделять торпеду. Busy/pending проверять на UI и вEngine. Обычный красный ring0→50px за2real sec, restore final trail solely for self-destruct new requirement; текущие hidden обычные torpedo trails не возвращать глобально.
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

- [ ] `Self_destruct_button_uses_launcher_active_id`
- [ ] `Pause_does_not_block_self_destruct`
- [ ] `Self_destruct_terminal_trail_lives_two_real_seconds`

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
