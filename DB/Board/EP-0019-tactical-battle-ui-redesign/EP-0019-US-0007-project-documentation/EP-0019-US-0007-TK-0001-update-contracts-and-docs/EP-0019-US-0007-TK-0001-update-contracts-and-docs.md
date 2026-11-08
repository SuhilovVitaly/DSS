---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0007-project-documentation
ticket: EP-0019-US-0007-TK-0001-update-contracts-and-docs
title: Итоговая документация
stage: draft
layer: documentation
test_project: —
depends_on: [EP-0019-US-0006-TK-0002-view-acceptance]
files_touched: 4
serves: [US7-AC1, US7-AC2, US7-AC3]
created: 2026-11-08T09:31:05Z
revision: 1
---

# Итоговая документация

## Why

Завершающая история обязательна по EpicExecutionPrompt.

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Скриншоты и рендеры остаются только в artifacts; в Board и Documentation ссылок на изображения нет (D-09).

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `DB/Documentation/02-FirstRelease/Screens/TacticalBattle.md` | описание старой компоновки :3-13 | Описать новый экран и управление (DesignSpec §4–9). |
| `DB/Documentation/04-Engineering/TacticalBattle.md` | контракт боя | Добавить раздел о презентации: масштаб, токены, шрифт, портреты, проверки вида и регрессионные снимки. |
| `DB/Documentation/04-Engineering/TacticalBattleSkins.md` | :46-57 роли | Добавить роли `ui.*`, отметить удалённые роли старой панели. |
| `DB/Board/EP-0019-tactical-battle-ui-redesign/ExecutionStatus.md` | нет | Покрытие критериев, проверки, SHA. |

## Public API after the change

No API change.

## Implementation steps

1. Обновить три документа.
2. Проверить ссылки и `git diff --check`.
3. Заполнить ExecutionStatus.

## Out of scope

Изменение кода.

## Invariants

Только Markdown.

## Tests

Проект `—`. Именованные проверки:

- Проверка содержания и ссылок.

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US7-AC1, US7-AC2, US7-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Документы перечислены.

## Исполнение

Не начато.

## Уточнение scope до реализации

Завершающий тикет дополнительно включает полную инвентаризацию итогового diff по всему репозиторию, обновление всех найденных релевантных README/руководств/Board и значимых комментариев, проверку текущих контрактов и исторической точности, таблицу criterion-to-evidence и ticket-to-SHA, ссылки/ID/количества/dependencies, offline Graphify rebuild после итогового кода и текстов с честной фиксацией ограничений. Scope расширен по §6 EpicExecutionPrompt: исходные три документа не ограничивают обязательную актуализацию проекта. Выполняется последним, после всех функциональных историй.
