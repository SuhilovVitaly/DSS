---
epic: EP-0031-board-and-demo-scope
story: EP-0031-US-0005-reduced-demo-contract
ticket: EP-0031-US-0005-TK-0001-current-requirements
title: Сверка требований с фактическим Save21 и боем EP-0029
stage: approved
approval_kind: workflow
implementation_status: not-started
layer: documentation
test_project: none
depends_on: []
files_touched: 5
serves: [AC-01, AC-02, AC-04]
created: 2026-11-17T00:00:00Z
revision: 1
---

# Сверка требований с фактическим Save21 и боем EP-0029

## Why

EP-0031-US-0005 — общий prerequisite всех демо-историй (EP-0032-US-0007 и далее). Без тикетов он не исполним, а все остальные тикеты ссылаются на него как на место нормативной сверки Save20 → Save21. Тикет 1/2 приводит канонические требования к фактическому коду. [Story](../EP-0031-US-0005-reduced-demo-contract.md).

## Decisions

Тикет добавлен 2026-11-17 при ревью декомпозиции EP-0032…EP-0041 по поручению владельца: «пройди по всем, реши нужно ли что-то изменить и добавить. делай это автономно». Human approval формулировок не заявляется; stage approved — готовность workflow планирования. Исполнение требует отдельного поручения.

## Assumptions

- Источник истины — текущий код. Документ меняется только там, где утверждение расходится с source; поставленные EP-0029 результаты описываются как текущие, а не как план.
- Save22 и демо-механики сюда не входят: их описывают документационные истории EP-0032…EP-0041 после реализации.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `DB/Documentation/01-Requirements/EngineRequirements.md` | `:135` — «Writer — Save20. Save18 и Save19 проходят проверку и миграцию…» | Writer Save21, чтение 18/19/20/21, что добавил Save21; прочие разделы не трогать. |
| `DB/Documentation/01-Requirements/FirstReleaseRequirements.md` | `:11` бой — «отдельная временная сессия»; `:12` — «Save20, чтение и миграция Save18/19» | Строки «Бой» и «Сохранение» по фактическому settlement и Save21; ссылка на сокращённую демо. |
| `DB/Documentation/04-Engineering/SaveFormat.md` | `:3`, `:5`, `:7`, `:19` — текущий writer Save20 | Save21 как текущий writer, список принимаемых версий, persisted carrier surface. |
| `DB/Documentation/04-Engineering/TacticalBattle.md` | `:3` «Бой — временная сессия…», `:101` «Engine и Save20 его не видят» | Уточнить: сессия боя не сохраняется, но исход фиксируется через `CommitSortieOutcome` в постоянный мир; Save20 → Save21. |
| `DB/Documentation/02-FirstRelease/Screens/Ship.md` | `:3` — «Отдельный ShipScreen — placeholder shell» | Описать фактический экран: состояние носителя, постоянное крыло, rearm между боями. |

Разрешены только эти 5 documentation files. Read-only источники: `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:40` (`CurrentSaveFormatVersion = 21`), `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs:235` (принимаются 18/19/20/21), `src/DeepSpaceSaga.Engine/SimulationEngine.SortieSettlement.cs:17`, `src/DeepSpaceSaga.Client/UI/Screens/Ship/ShipScreen.cs:9`, [EP-0029](../../../EP-0029-unified-carrier-and-torpedoes/Documentation.md).

## Public API after the change

No API change.

## Implementation steps

1. Сверить каждое утверждение из таблицы с указанными source lines; записать в evidence пары «было в документе → факт в коде». Если код за это время ушёл дальше Save21, описать фактическую версию и отметить drift в Board.
2. Обновить пять документов: Save21 — текущий writer; чтение Save18/19/20 с миграцией; Save21 хранит поверхность носителя (корпус и постоянное крыло). Тактическая сессия по-прежнему не сериализуется, а её исход фиксируется settlement в мире до записи.
3. Ship.md описывает реальный экран EP-0029 и прямо называет оставшиеся заглушки (Hire, Contracts), чтобы демо-тикеты EP-0033/EP-0034 видели исходное состояние. Исторические отчёты эпиков не переписывать.

## Served acceptance criteria

- **AC-01:** Board и актуальные требования согласованы с сокращённым объёмом: 60–120 минут, три способа заработка, один кластер, готовый носитель и дополнительные пассажирские места.
- **AC-02:** Устранены устаревшие текущие описания Save20, временного боя и Ship shell после сверки источников; история прежних поставок сохранена.
- **AC-04:** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

Вклад тикета — канонические требования. AC-01 по Board закрывает TK-0002.

## Out of scope

Save22, демо-механики, правка кода, переписывание исторических ImplementationStatus/ExecutionStatus.

## Invariants

- Документы описывают текущее поведение; план и непринятое помечаются явно.
- Engine владеет исходами/временем/RNG/деньгами; документ не обещает сохранение внутри боя.

## Tests

Проверки: LinksResolve, StatementsMatchSource, OnlyMarkdownInDocumentation. Build/test не требуется: тикет только документационный. Проверить Markdown links, отсутствие фраз «Writer — Save20» / «текущий writer — Save20» в канонических документах (`grep`), `git diff --check`.

## Definition of Done

- Пять документов совпадают с source; evidence «было → факт» в `D:/DeepSpaceSaga/artifacts/validation/EP-0031-US-0005-TK-0001-current-requirements/<run>/`.
- Нет правок вне allowlist; drift за пределами таблицы записан в Board, а не исправлен молча.

## Self-containment check

Allowlist, source lines и шаги перечислены здесь. Новый продуктовый выбор или расширение файлов требует правки Board.
