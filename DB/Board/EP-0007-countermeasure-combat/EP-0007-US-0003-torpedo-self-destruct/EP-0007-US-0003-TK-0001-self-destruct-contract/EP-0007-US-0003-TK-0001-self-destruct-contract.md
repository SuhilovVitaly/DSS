---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0003-torpedo-self-destruct
ticket: EP-0007-US-0003-TK-0001-self-destruct-contract
title: "Команда и причина завершения торпеды"
stage: draft
layer: contracts
depends_on: ["EP-0007-US-0002-TK-0005-defense-bootstrap"]
files_touched: 2
serves: ["AC-0001","AC-0002"]
created: 2026-10-04
revision: 1
---

# Команда и причина завершения торпеды

STATUS: DRAFT

## Why

Как игрок, я прекращаю затянувшуюся атаку кнопкой в панели аппарата. Торпеда взрывается без урона и освобождает оператора, даже на паузе. Преследовавшая её противоракета теряет цель. Этот тикет обеспечивает: команда и причина завершения торпеды.

## Decisions

[Согласованные D01–D17](../../Documentation.md) задают поведение; применяются критерии AC-0001, AC-0002 [истории](../EP-0007-US-0003-torpedo-self-destruct.md). Тикет не вводит новых пользовательских решений.

## Assumptions

Применяются A01–A08 эпика. Имена новых API предлагаемые; семантика и allowlist обязательны. Baseline проверен 2026-10-04; если код сдвинулся, актуализировать grounding до реализации. Дата отдельного ответа пользователя не выдумывается.

## Code context

Полный write allowlist относительно D:/DeepSpaceSaga/DSS. 2 файлов, включая тесты. Прочие файлы read-only.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/CombatSnapshot.cs` | Существует: src/DeepSpaceSaga.Contracts/CombatSnapshot.cs:1 | Только описанные API и шаги |
| `tests/DeepSpaceSaga.Contracts.Tests/TorpedoTerminationTests.cs` | Новый файл этого тикета | Названные регрессии; сохранить независимые assertions |

## Dependencies

Шаг 12 [плана](../../Tickets.md).
- [EP-0007-US-0002-TK-0005-defense-bootstrap](../../EP-0007-US-0002-countermeasure-equipped-tetrarch/EP-0007-US-0002-TK-0005-defense-bootstrap/EP-0007-US-0002-TK-0005-defense-bootstrap.md)

Зависимость должна быть реализована и проверена, а не просто существовать как Markdown. Не подменять отсутствующий production результат mock/stub.

## Public API after the change

CombatCommandTypes.SelfDestruct=torpedo.selfDestruct; CombatImpactSnapshot optional TerminationKind (Impact/SelfDestruct/Intercept), preserving old defaults.

## Implementation steps

1. Проверить текущие файлы allowlist и реализованность зависимостей; сохранить посторонние изменения.
2. Команда адресует owner/module и фиксирует ID активной торпеды в TargetObjectId, чтобы запоздалый повтор не уничтожил следующий пуск. Для SelfDestruct не фабриковать HitObjectId реального объекта/урон. UI effect получает финальный trail независимо от скрытого travelling trail.
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

- [ ] `Termination_kind_roundtrips`
- [ ] `Legacy_impact_remains_impact`

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
