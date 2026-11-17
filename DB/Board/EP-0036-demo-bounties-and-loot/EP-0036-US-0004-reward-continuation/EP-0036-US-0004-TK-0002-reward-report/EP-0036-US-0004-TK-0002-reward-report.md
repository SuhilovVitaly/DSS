---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0004-reward-continuation
ticket: EP-0036-US-0004-TK-0002-reward-report
title: Раздельный отчёт о награде, трофеях и потерях
stage: draft
approval_kind: workflow
implementation_status: not-started
layer: client
matching_test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0036-US-0004-TK-0001-reward-roundtrip, EP-0036-US-0006-TK-0004-client]
files_touched: 3
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-17T19:22:37Z
revision: 3
---

# Раздельный отчёт о награде, трофеях и потерях

## Why
[Story](../EP-0036-US-0004-reward-continuation.md): замкнуть реальный postbattle save/load/report путь, не выдавать DTO за продолжение игры.

## Decisions
D-36-14…28 эпика задают целые sale-only trophies, тактическую карту после победы, exit discard и однократные rewards. Точные цитаты в [эпике](../../Documentation.md).

## Assumptions
Новые API ниже — проект. Фактические prerequisite implementations проверяются перед исполнением; исправлять только owning files, без скрытого расширения.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Demo/RewardReportPresentation.cs` | Новый read-only presentation. | Только описанная интеграция. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleResults.cs` | Существующие battle result presentation types. | Только описанная интеграция. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0036Us0004Tk0002Tests.cs` | Новый report tests. | Только описанная интеграция. |

## Public API after the change
Проект read-only RewardReportPresentation(confirmed campaign rewards, battle result), no gameplay API.

## Implementation steps
Показать отдельно bounty due/paid (по entitlement и posting), taken modules/consumables/scrap, failed attempts и оставленную добычу, арестованных пилотов, actual player losses. Стоимость trophy — estimated, не cash и не гарантированная прибыль. Не суммировать неизвестный cargo basis как ноль. Повторное открытие и Load отображают прежний результат без команд/переигрывания; US7 window/US4 routing используют компонент. Locale keys отчёта принадлежат US3 locale ticket, здесь не менять content.

## Out of scope
Новые gameplay rules, generic durability, установка трофеев, пытки/вербовка и изменения чужого inventory/экономики.

## Invariants
Committed sortie/entitlement/attempt не применяются дважды. Frozen loot не становится prebattle replay. Неоплаченная награда и оценка trophies не cash. Входные malformed данные отклоняются до публикации. Client не производит RNG/деньги.

## Tests
Будущие NOT RUN:
- `DemoEp0036Us0004Tk0002Tests.DueNotCashAndTrophyNotRevenue`
- `DemoEp0036Us0004Tk0002Tests.ReopenDoesNotSubmitCommands`
- `DemoEp0036Us0004Tk0002Tests.UnknownBasisRemainsUnknown`
- `DemoEp0036Us0004Tk0002Tests.ReportSeparatesLootAndLoss`

Из корня D:/DeepSpaceSaga/DSS создать уникальный абсолютный D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0004-TK-0002-reward-report/<UTC-run>/.
`dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0004Tk0002Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`.
Каждый process tree cap300000ms: Start-Process -WindowStyle Hidden с перенаправленными stdout/stderr в run, WaitForExit(300000), taskkill /PID <pid> /T /F при timeout. Owning build `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path <absolute-run>/build`; format --verify-no-changes --include только C# allowlist, тот же cap. Полную suite не подменять этой проверкой.

## Definition of Done
- Реальный путь и named tests проверяют заявленные AC в allowed files.
- Пройден owning build/format либо конкретная исходная ошибка честно отмечена.
- UI route проверен через реальные callbacks; native/product acceptance отдельно OPEN до исполнения.
- Evidence/cleanup по AGENTS, без commit/push разрешения.

## Self-containment check
Входы принадлежат указанным prerequisite tickets, owning files/API/negative cases перечислены. При необходимости изменить иной слой сначала исправить Board allowlist.
