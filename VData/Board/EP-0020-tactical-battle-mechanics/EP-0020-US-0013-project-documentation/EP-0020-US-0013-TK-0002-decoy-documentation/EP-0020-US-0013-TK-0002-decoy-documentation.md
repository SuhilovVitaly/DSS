---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0013-project-documentation
ticket: EP-0020-US-0013-TK-0002-decoy-documentation
title: Документация ловушек
stage: draft
layer: documentation
test_project: —
depends_on: [EP-0020-US-0013-TK-0001-mechanics-documentation, EP-0020-US-0006-TK-0005-decoy-regression, EP-0020-US-0007-TK-0004-decoy-view-regression]
files_touched: 4
serves: [US13-AC2, US13-AC3]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Документация ловушек

## Why

Канонические документы должны описывать модуль ловушек и его правила.

## Decisions

Применяются [D-01–D-42](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

—

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `DB/Documentation/01-Requirements/EngineRequirements.md` | раздел «Тактический бой» | Модуль ловушек, пуск, эффект, ИИ, видимость. |
| `DB/Documentation/04-Engineering/TacticalBattle.md` | после TK-0001 | Раздел «Ловушки»: данные, фиксация в залпе, первый выстрел, снимки. |
| `DB/Documentation/02-FirstRelease/Screens/TacticalBattle.md` | после TK-0001 | Пуск ловушки, клавиша D, раскрытие. |
| `DB/Board/EP-0020-tactical-battle-mechanics/ExecutionStatus.md` | после TK-0001 | Итоги M2 и отчёт о ловушках. |

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
- Для US13-AC2, US13-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Все документы перечислены.

## Исполнение

Не начато.
