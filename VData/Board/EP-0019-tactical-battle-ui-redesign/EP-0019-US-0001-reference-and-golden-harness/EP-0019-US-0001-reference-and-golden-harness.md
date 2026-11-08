---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0001-reference-and-golden-harness
title: Харнесс сцен и проверки вида
stage: draft
dependencies: []
created: 2026-11-08T09:31:05Z
source_request: "Подготовить сборку, шрифт и автоматическую сборку настоящего экрана боя в семи сценах DesignSpec §12 с проверками данных, геометрии, текстов и растровых проб."
current_review: epic
revision: 1
---

# Харнесс сцен и проверки вида

## Входное техническое задание

Спецификация вида и проверок — [DesignSpec](../DesignSpec.md) (§12). Шрифты и значения снимков движка — [Assets](../Assets/README.md). Headless-фикстура — `BattleNavigationTests.Fixture` (`tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleNavigationTests.cs:271-341`).

## User story

Как владелец продукта, я хочу, чтобы готовность нового интерфейса проверялась автоматически по текстовой спецификации, чтобы любой исполнитель на любой машине получал тот же вид.

## Dependencies

—

## Completion evidence

- Решение и все тестовые проекты собираются на SDK 8.0.4xx и 10.x.
- Шрифт DejaVu Sans поставляется в выходной каталог клиента и загружается из файла на Windows, macOS и Ubuntu, на Windows и macOS без установленного DejaVu. Текст боя рисуется с платформенно-независимыми настройками растеризации ([DesignSpec §2 «Шрифт»](../DesignSpec.md#шрифт)).
- Харнесс доводит реальный бой до семи сцен, проверяет данные снимков и выполняет проверки §12.2–12.4; рендеры сцен сохраняются в `DSS_VALIDATION_ROOT`.

## Acceptance criteria

- **US1-AC1.** `dotnet build DeepSpaceSaga.sln` проходит без ошибок CS0121 на SDK 8.0.4xx; поведение градиентов не меняется.
- **US1-AC2.** `BattleTypography` загружает `UI/Fonts/DejaVuSans.ttf` и `DejaVuSans-Bold.ttf` из `AppContext.BaseDirectory`; SHA-256 файлов совпадает с [Assets/README](../Assets/README.md#sha-256); все строки боя RU/EN имеют глифы; `BattleTypography.TextPaint` задаёт `NoHinting`, `LcdRenderText = false`, `SubpixelText = true`, `IsLinearText = true`, и весь текст боя рисуется и измеряется только через неё.
- **US1-AC3.** `Ep0019Scenes` строит семь сцен DesignSpec §12.1 через реальные Engine, сессию и `SkiaWindow` без окна, с RU и управляемыми часами; данные снимков совпадают с `Assets/battle-data-seed1.txt`.
- **US1-AC4.** Проверки вида: модель раскладки (§12.2, точность 0,01), строки и цвета моделей (§12.3), растровые пробы (§12.4). Каждая сцена пишет рендер, `probes.json` (проба, доля, результат) и `layout.json` в `DSS_VALIDATION_ROOT/ep0019/<scene>/`.
- **US1-AC5.** До готовности соответствующих историй проверки вида помечены `Skip` с причиной и категорией `Ep0019View`; проверки данных выполняются сразу.

## Non-goals

Изменение вида экрана, перенос интерфейса в production.

## Invariants

Харнесс не меняет поведение игры и использует только публичные или `internal` (InternalsVisibleTo) точки входа клиента. Ожидаемые значения берутся из DesignSpec, а не из рендера production.

## Решения и согласование

Ожидает утверждения эпика пользователем (см. [открытые вопросы](../Documentation.md#открытые-вопросы-нужно-решение-до-утверждения)).

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0019-US-0001-TK-0001-fix-sdk-build-ambiguity](EP-0019-US-0001-TK-0001-fix-sdk-build-ambiguity/EP-0019-US-0001-TK-0001-fix-sdk-build-ambiguity.md) | Сборка на SDK 8.0.4xx | client | — | US1-AC1 |
| [EP-0019-US-0001-TK-0002-bundle-battle-font](EP-0019-US-0001-TK-0002-bundle-battle-font/EP-0019-US-0001-TK-0002-bundle-battle-font.md) | Поставка шрифта DejaVu Sans и единая растеризация текста | client | `EP-0019-US-0001-TK-0001-fix-sdk-build-ambiguity` | US1-AC2 |
| [EP-0019-US-0001-TK-0003-scene-harness](EP-0019-US-0001-TK-0003-scene-harness/EP-0019-US-0001-TK-0003-scene-harness.md) | Харнесс сцен и проверки вида | tests | `EP-0019-US-0001-TK-0002-bundle-battle-font` | US1-AC3, US1-AC4, US1-AC5 |
