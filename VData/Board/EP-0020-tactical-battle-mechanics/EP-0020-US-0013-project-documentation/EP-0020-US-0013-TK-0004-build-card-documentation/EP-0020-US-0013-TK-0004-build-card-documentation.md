---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0013-project-documentation
ticket: EP-0020-US-0013-TK-0004-build-card-documentation
title: Документация слотов и карточки сборки
stage: draft
layer: documentation
test_project: —
depends_on: [EP-0020-US-0013-TK-0003-anomaly-documentation, EP-0020-US-0012-TK-0003-build-card-view-regression]
files_touched: 3
serves: [US13-AC2, US13-AC5]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Документация слотов и карточки сборки

## Why

Канонические документы должны описывать слоты сборки и карточку.

## Decisions

Применяются [D-01–D-42](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

—

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `DB/Documentation/04-Engineering/TacticalBattle.md` | после TK-0003 | `Slots` снимка: порядок, состояния. |
| `DB/Documentation/02-FirstRelease/Screens/TacticalBattle.md` | после TK-0003 | Карточка «Пилот и сборка». |
| `DB/Board/EP-0020-tactical-battle-mechanics/ExecutionStatus.md` | после TK-0003 | Итоги M4. |

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
- Для US13-AC2, US13-AC5 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Все документы перечислены.

## Исполнение

Не начато.
