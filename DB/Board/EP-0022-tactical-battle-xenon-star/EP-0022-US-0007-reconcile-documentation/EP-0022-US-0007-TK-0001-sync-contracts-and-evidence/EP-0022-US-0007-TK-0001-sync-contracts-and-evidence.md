---
epic: EP-0022-tactical-battle-xenon-star
story: EP-0022-US-0007-reconcile-documentation
ticket: EP-0022-US-0007-TK-0001-sync-contracts-and-evidence
title: Синхронизировать документацию и доказательства
stage: draft
layer: client
depends_on: [EP-0022-US-0001-TK-0001, EP-0022-US-0002-TK-0001, EP-0022-US-0003-TK-0001, EP-0022-US-0004-TK-0001, EP-0022-US-0005-TK-0001, EP-0022-US-0006-TK-0001]
files_touched: 4
serves: [AC-01]
created: 2026-11-10T16:52:26Z
revision: 1
---

# Синхронизировать документацию и доказательства

[История](../EP-0022-US-0007-reconcile-documentation.md) · [Эпик](../../Documentation.md)

## Why

Инвентаризация всего итогового diff, канонические контракты, Board и SHA согласованы. Отдельно отражены проверка сборки, native и продуктовые gates. Проверены ссылки, ID и граф при его наличии.

## Decisions

EP-0022 D-01/D-02 задают визуальное направление и геометрию. Запрос исполнения разрешает отдельный commit и push каждого тикета.

## Assumptions

Локально лицензированные срезы подключаются явным свойством сборки `XenonStarSource`. PNG из набора отсутствуют в Git. Общая поставка этих ресурсов остаётся G-01. Используется прежний загрузчик, сохранение выбора и шрифт.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `DB/Documentation/04-Engineering/TacticalBattleSkins.md` | Действующий манифест v1 | Актуализировать оформление и локальное подключение. |
| `DB/Documentation/02-FirstRelease/Screens/TacticalBattle.md` | Геометрия и ввод | Уточнить визуальный охват. |
| `DB/Documentation/02-FirstRelease/Screens/Settings.md` | Действующий выбор | Уточнить наличие опционального пакета. |
| `src/DeepSpaceSaga.Client/Skins/README.md` | Инструкция автору | Добавить воспроизводимую команду и область лицензии. |

## Public API after the change

Схема сохранения, команды и публичные API прежние.

## Implementation steps

1. Актуализировать оформление и локальное подключение.
2. Уточнить визуальный охват.
3. Уточнить наличие опционального пакета.
4. Добавить воспроизводимую команду и область лицензии.

## Out of scope

Другие экраны, правила боя, сохранение мира, замена шрифтов и лицензирование от имени владельца.

## Invariants

Макет, координаты ввода, расчёты и авторитет Engine остаются общими для всех скинов. Срезы применяются только к отрисовке.

## Tests

content, links, diff check

## Definition of Done

AC-01 подтверждён целевыми проверками. Реальные результаты, ограничения и проверка публикации фиксируются в Board. Отдельный commit и немедленный push.

## Self-containment check

Production seam и проверка перечислены в Code context. Существующие команды и fixtures сохраняются.
