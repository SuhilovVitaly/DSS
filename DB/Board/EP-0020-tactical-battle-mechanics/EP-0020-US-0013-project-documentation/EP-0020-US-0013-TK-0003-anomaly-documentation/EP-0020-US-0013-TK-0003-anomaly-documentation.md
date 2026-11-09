---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0013-project-documentation
ticket: EP-0020-US-0013-TK-0003-anomaly-documentation
title: Документация аномалий
stage: done
layer: documentation
test_project: —
depends_on: [EP-0020-US-0013-TK-0002-decoy-documentation, EP-0020-US-0009-TK-0005-anomaly-regression, EP-0020-US-0010-TK-0004-anomaly-view-regression]
files_touched: 4
serves: [US13-AC2, US13-AC4]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Документация аномалий

## Why

Канонические документы должны описывать аномалии тактического боя.

## Decisions

Применяются [D-01–D-42](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

—

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `DB/Documentation/01-Requirements/EngineRequirements.md` | раздел «Тактический бой» | Аномалии: генерация, поглощение, пыль, ИИ. |
| `DB/Documentation/04-Engineering/TacticalBattle.md` | после TK-0002 | Раздел «Аномалии»: данные, математика, порядок бросков, снимки. |
| `DB/Documentation/02-FirstRelease/Screens/TacticalBattle.md` | после TK-0002 | Вид аномалий и правила пересечения. |
| `DB/Board/EP-0020-tactical-battle-mechanics/ExecutionStatus.md` | после TK-0002 | Итоги M3 и отчёт об аномалиях. |

## Public API after the change

No API change.

## Implementation steps

1. Обновить документы.
2. Дописать ExecutionStatus.
3. Проверить ссылки.

## Out of scope

Код.

## Invariants

Документы не противоречат данным.

## Tests

Проект `—`. Именованные проверки:

- Проверка содержания и ссылок.

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US13-AC2, US13-AC4 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Все документы перечислены.

## Исполнение

Завершено. US13-AC4: описаны все 15anomalies keys, named RNG, placement/Box–Muller, сумма Gaussian/граница/midpoints, absorption порядок, no-extra-RNG при 0, InDust/AI и визуальные правила. Scope расширен на GameSessionScreenUI/FirstReleaseRequirements по inventory. ОтчётM3 с denominator/ограничениями уже в ExecutionStatus и US9/TK5. Self-review отличает clearance anchor от Gaussian хвостов и альтернативу ИИ от гарантии безопасного хода. Content/link/diffPASS; evidence `D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us13-tk3/content-links.json`. Markdown-only, runtime tests не повторялись.
