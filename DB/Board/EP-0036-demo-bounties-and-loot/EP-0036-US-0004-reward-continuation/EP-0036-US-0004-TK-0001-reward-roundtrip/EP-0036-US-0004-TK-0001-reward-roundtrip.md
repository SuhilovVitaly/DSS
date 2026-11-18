---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0004-reward-continuation
ticket: EP-0036-US-0004-TK-0001-reward-roundtrip
title: Интеграция сохранения наград и послебоевой добычи
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0036-US-0007-TK-0005-postbattle-continuation, EP-0036-US-0009-TK-0003-capacity-and-rations, EP-0036-US-0002-TK-0002-engine]
files_touched: 4
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-17T19:22:37Z
revision: 3
---

# Интеграция сохранения наград и послебоевой добычи

## Current execution status

Implementation is complete under the explicit owner execution request. Planning-only authorizations and NOT STARTED statements below are dated planning history, superseded by this execution. Automated evidence, independent reviews and remaining native/product gates: [ExecutionStatus](../../ExecutionStatus.md). Native/game execution NOT RUN — owner only.


## Why
[Story](../EP-0036-US-0004-reward-continuation.md): замкнуть реальный postbattle save/load/report путь, не выдавать DTO за продолжение игры.

## Decisions
D-36-14…28 эпика задают целые sale-only trophies, тактическую карту после победы, exit discard и однократные rewards. Точные цитаты в [эпике](../../Documentation.md).

## Assumptions
Новые API ниже — проект. Фактические prerequisite implementations проверяются перед исполнением; исправлять только owning files, без скрытого расширения.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0036Us0004Tk0001Tests.cs` | Новый integration corpus. | Только описанная интеграция. |
| `src/DeepSpaceSaga.Engine/Scenario/BattleLootData.cs` | US3 TK3: frozen geometry/attempts/remaining. | Только описанная интеграция. |
| `src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs` | Validate:69; persisted rewards/prisoners. | Только описанная интеграция. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.BattleLootLifecycle.cs` | US7 TK3 owns phase restoration. | Только описанная интеграция. |

## Public API after the change
No new public API; интеграция данных US3/6/7/9 и bounty entitlement.

## Implementation steps
Проверить сценарий confirmed victory→commit losses→loot map→частичный collect/module attempt/arrest→save→load→остаток→exit confirm→станция заказчика→claim bounty. Проверить frozen poses/ObjectIds/пилот identity/attempt terminal и отсутствие повторного боя/RNG/bounty payment. Bounty хранить как отдельный posting, не PassengerPayout и не новую копию PlayerCredits. Добавить malformed duplicate IDs, negative remaining, captured pilot reused, unknown cargo basis и invalid phase rejection до замены мира. Исправления только allowed owning persistence/lifecycle files; расширение других feature rules требует их tickets.

## Out of scope
Новые gameplay rules, generic durability, установка трофеев, пытки/вербовка и изменения чужого inventory/экономики.

## Invariants
Committed sortie/entitlement/attempt не применяются дважды. Frozen loot не становится prebattle replay. Неоплаченная награда и оценка trophies не cash. Входные malformed данные отклоняются до публикации. Client не производит RNG/деньги.

## Tests
Будущие NOT RUN:
- `DemoEp0036Us0004Tk0001Tests.LootOpenRoundtrip_PreservesSourcesAndAttempts`
- `DemoEp0036Us0004Tk0001Tests.ArrestThenLoad_DoesNotDuplicatePerson`
- `DemoEp0036Us0004Tk0001Tests.BountyPaidOnceAfterLootLoad`
- `DemoEp0036Us0004Tk0001Tests.MalformedLootRejectedBeforePublish`

Из корня D:/DeepSpaceSaga/DSS создать уникальный абсолютный D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0004-TK-0001-reward-roundtrip/<UTC-run>/.
`dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0004Tk0001Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`.
Каждый process tree cap300000ms: Start-Process -WindowStyle Hidden с перенаправленными stdout/stderr в run, WaitForExit(300000), taskkill /PID <pid> /T /F при timeout. Owning build `dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path <absolute-run>/build`; format --verify-no-changes --include только C# allowlist, тот же cap. Полную suite не подменять этой проверкой.

## Definition of Done
- Реальный путь и named tests проверяют заявленные AC в allowed files.
- Пройден owning build/format либо конкретная исходная ошибка честно отмечена.
- UI route проверен через реальные callbacks; native/product acceptance отдельно OPEN до исполнения.
- Evidence/cleanup по AGENTS, без commit/push разрешения.

## Self-containment check
Входы принадлежат указанным prerequisite tickets, owning files/API/negative cases перечислены. При необходимости изменить иной слой сначала исправить Board allowlist.


## Execution
Implemented real command integration through accepted bounty, confirmed victory, partial collection/module/arrest, JSON roundtrip, remainder and issuer payment with duplicate rejection. Malformed phase/negative quantity/duplicate source rejected atomically. Existing US9 provenance and US2 defeat tests remain in story regression. Tests scheduled at complete US4 boundary; native NOT RUN.

Review correction: pre-publication Load gate now requires Open loot to match the finished current encounter, with no pending sensor report/active dialogue. Owning SimulationEngine.cs validation is added to allowlist. Regression covers closed A + valid checkpoint B, rejects reopened A atomically.

## Owner review corrections (2026-11-18)
Scope: findings 1 and 5. Closed loot keeps structural/frozen provenance validation without current loot balance identity; Open remains strict. Missing composition/layout references must reject with ScenarioException. Owning lifecycle and existing integration tests; validation pending at story boundary.

Independent review follow-up: BattleLoot.cs added to scope. Closed session projects no actionable objects; full frozen audit remains persisted. This prevents closed content-independent audit from dereferencing obsolete SKUs. Test unknown closed SKU snapshot versus strict Open rejection. US4 initial tests PASS 5/5 (20261118-corrections/us4).

Final correction supersedes prior no-objects approach: preserve closed report objects/outcomes, resolve optional display metadata safely (unknown historical SKU uses identity and zero unavailable mass/value); no actions enabled. ModuleSalvage.cs included for safe closed display lookup. This retains Contracts reward report history.

Review correction tests: scoped whitespace formatted; behavior unchanged.
