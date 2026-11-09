---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0013-project-documentation
ticket: EP-0020-US-0013-TK-0004-build-card-documentation
title: Документация слотов и карточки сборки
stage: done
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

В процессе. Scope расширен по inventory на README/tooling/skins/Graphify corpus и итоговые Board статусы. US13-AC5: порядок Slots, состояния, пустые/armor.none/истощённый модуль, A35 и captured selection,300design units и read-only карточка описаны. Graph rebuild и финальная сверка публикации выполняются до закрытия.

Завершено. US13-AC5 и завершающий §6: current docs/README/skins/tools и Save20 синхронизированы,56AC связаны с 44 тикетами,78dependencies разрешены в canonicalBoard. Исправлен только metadata omission US8-AC4 (реализация/28ContractsPASS уже были в US8/TK3). Все 13 историй/44 тикета done.

Offline Graphify:814code+755Markdown (58EP20),22594nodes/60077edges/760communities.1569source hashes и 4fresh semantic input hashes,18export hashes сверены; missing/invalid/dangling/selfloops0 в derived graph. Семантика выборочная 20nodes/22edges/2hyperedges, source locations приведены к canonicalL# с сохранением ranges; portable IDs согласованы со spec. Parser warning1 в ModulePanelLocalizationTests.cs — partial AST coverage, не ошибка C# компиляции;1216unresolved AST edges/3019unlocated symbols и 816canonical loops явно вынесены в audit sidecars, не выдуманы как факты. Граф не является runtime/independent acceptance.

Evidence `D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us13-tk4/graph-quality.json`, `graph/verification.json`, `content-links.json`, `dependency-save-audit.json`. Source HEAD57a3c680 — pre-export; manifest описывает фактическое рабочее дерево с финальным кодом/текстами. Только исключённые publication ledgers/receipt меняются после rebuild. Link/content/diffPASS; runtime suite не повторялась для Markdown/tooling graph update. Итоговая уборка и remote receipt фиксируются отдельной служебной записью после публикации собственного SHA.
