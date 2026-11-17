---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0004-reward-continuation
ticket: EP-0036-US-0004-TK-0003-restored-loot-route
title: Открытие сохранённой карты лута при продолжении кампании
stage: draft
approval_kind: workflow
implementation_status: not-started
layer: client
matching_test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0036-US-0007-TK-0004-postbattle-map, EP-0036-US-0007-TK-0005-postbattle-continuation]
files_touched: 4
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-17T19:22:37Z
revision: 3
---

# Открытие сохранённой карты лута при продолжении кампании

## Why
[Story](../EP-0036-US-0004-reward-continuation.md): замкнуть реальный postbattle save/load/report путь, не выдавать DTO за продолжение игры.

## Decisions
D-36-14…28 эпика задают целые sale-only trophies, тактическую карту после победы, exit discard и однократные rewards. Точные цитаты в [эпике](../../Documentation.md).

## Assumptions
Новые API ниже — проект. Фактические prerequisite implementations проверяются перед исполнением; исправлять только owning files, без скрытого расширения.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | Recovered battle route:2180 требует PendingBattle, иначе frozen loot не откроется. | Только описанная интеграция. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs` | Существующий flow создания/восстановления tactical screen. | Только описанная интеграция. |
| `src/DeepSpaceSaga.Client/GameSessionHandle.DemoCampaign.cs` | DemoCampaign:13, ExecuteDemoCampaignAsync:22, ObserveDemoCampaign:78 — owning transport/projection partial. | Только описанная интеграция. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0036Us0004Tk0003Tests.cs` | Новый routing tests. | Только описанная интеграция. |

## Public API after the change
Проект AdoptRecoveredLoot(LootSessionSnapshot) и CreatePostBattleLootView(frozen battlefield,session callbacks); не создавать активный TacticalBattle session.

## Implementation steps
В recovery branch после Load обнаружить Rewards.Loot.Phase=LootOpen независимо от PendingBattle. Передать frozen geometry/state в US7 screen и callbacks той же GameSessionHandle; не вызывать BeginEncounterBattle и не replay. Пробросить Rewards updates/receipts в открытое окно. Обычные PendingBattle recovery и world map paths сохранить. Closed loot возвращает world map без окна; exit из recovered loot выполняет тот же confirm flow. Проверить путь через настоящий SkiaWindow/Flow wiring, а не один helper.

## Out of scope
Новые gameplay rules, generic durability, установка трофеев, пытки/вербовка и изменения чужого inventory/экономики.

## Invariants
Committed sortie/entitlement/attempt не применяются дважды. Frozen loot не становится prebattle replay. Неоплаченная награда и оценка trophies не cash. Входные malformed данные отклоняются до публикации. Client не производит RNG/деньги.

## Tests
Будущие NOT RUN:
- `DemoEp0036Us0004Tk0003Tests.LoadLootOpenCreatesPostbattleView`
- `DemoEp0036Us0004Tk0003Tests.RecoveredLootDoesNotBeginCombat`
- `DemoEp0036Us0004Tk0003Tests.ClosedLootReturnsWorldMap`
- `DemoEp0036Us0004Tk0003Tests.PendingBattleRecoveryUnchanged`

Из корня D:/DeepSpaceSaga/DSS создать уникальный абсолютный D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0004-TK-0003-restored-loot-route/<UTC-run>/.
`dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0004Tk0003Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`.
Каждый process tree cap300000ms: Start-Process -WindowStyle Hidden с перенаправленными stdout/stderr в run, WaitForExit(300000), taskkill /PID <pid> /T /F при timeout. Owning build `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path <absolute-run>/build`; format --verify-no-changes --include только C# allowlist, тот же cap. Полную suite не подменять этой проверкой.

## Definition of Done
- Реальный путь и named tests проверяют заявленные AC в allowed files.
- Пройден owning build/format либо конкретная исходная ошибка честно отмечена.
- UI route проверен через реальные callbacks; native/product acceptance отдельно OPEN до исполнения.
- Evidence/cleanup по AGENTS, без commit/push разрешения.

## Self-containment check
Входы принадлежат указанным prerequisite tickets, owning files/API/negative cases перечислены. При необходимости изменить иной слой сначала исправить Board allowlist.
