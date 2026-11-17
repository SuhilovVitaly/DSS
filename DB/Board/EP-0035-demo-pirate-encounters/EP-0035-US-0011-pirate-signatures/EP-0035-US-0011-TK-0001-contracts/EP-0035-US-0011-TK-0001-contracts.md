---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0011-pirate-signatures
ticket: EP-0035-US-0011-TK-0001-contracts
title: Контракт наблюдаемой сигнатуры пиратского носителя
stage: draft
approval_kind: workflow
implementation_status: not-started
layer: contracts
matching_test_project: tests/DeepSpaceSaga.Contracts.Tests
depends_on: [EP-0035-US-0001-TK-0001-contracts]
files_touched: 3
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-17T11:37:31Z
revision: 1
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Implementation NOT STARTED.

# Контракт наблюдаемой сигнатуры пиратского носителя

## Why
Выполнить owning часть [истории](../EP-0035-US-0011-pirate-signatures.md), сохраняя наблюдаемую сигнатуру отдельно от приватного состава.

## Decisions
D-35-11…13 и exact count grounding — в истории; не выдавать A-35-06/G-35-05/07/08/09 за решения пользователя.

## Assumptions
Одноразовая materialization при первом реальном player acquisition и запрет reroll — действующий контракт. Продуктовые G закрыты; технические API ниже — проект, prerequisites и allowlists проверяются перед исполнением.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Contracts/PirateSignatureContracts.cs | Новый файл этого тикета. | Только steps ниже. |
| src/DeepSpaceSaga.Contracts/PirateGroupsContracts.cs | Существующий файл или owning prerequisite US-0001; grounding в истории. | Только steps ниже. |
| tests/DeepSpaceSaga.Contracts.Tests/Demo/DemoEp0035Us0011Tk0001Tests.cs | Новый matching test file. | Только steps ниже. |

## Public API after the change
Проект PirateSignatureObservation(groupId,signatureTypeId,displayNameKey,observedAt); report total estimate отдельно, без heavy/light list.

## Implementation steps
1. Добавить два логических signature typeId lair/tancred, не использовать display carrier.id как единственный ключ.
2. Публичная проекция содержит только разрешённую сигнатуру/type; private composition и RNG state не сериализуются в public snapshot.
3. Известный ship type не раскрывает exact remaining inventory: выдавать только один приблизительный total по D-35-28/31, без drone classes/equipment. Данные committed capture и pending battle не раскрывают скрытый состав в world report.

## Out of scope
Изменение initial/tutorial battle, переименование thug без решения, скрытая правка легитимного loadout, случайность от renderer, отображение drone classes, новые NPC фракции, баланс и публикация.

## Invariants
Engine владеет RNG/материализацией и snapshot; public report не содержит private composition. Known signature не означает всеведение. World group identity сохраняется. Sensor skill дальности и точность оценки — разные параметры до решения G-35-05.

## Tests
Будущие tests (NOT RUN):
- DemoEp0035Us0011Tk0001Tests.Signature_RoundtripWithoutComposition
- DemoEp0035Us0011Tk0001Tests.PublicSnapshot_ContainsNoDroneClassList
Создать абсолютный <run> внутри D:/DeepSpaceSaga/artifacts/validation/ep0035-signatures/; process tree cap 300000 ms, timeout остановить.
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj -c Release --filter "FullyQualifiedName~DemoEp0035Us0011Tk0001Tests" --results-directory <run>/tests --logger trx
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj -c Release --artifacts-path <run>/build
dotnet format src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --verify-no-changes --include src/DeepSpaceSaga.Contracts/PirateSignatureContracts.cs src/DeepSpaceSaga.Contracts/PirateGroupsContracts.cs tests/DeepSpaceSaga.Contracts.Tests/Demo/DemoEp0035Us0011Tk0001Tests.cs
JSON — loader tests; native/product отдельно.

## Definition of Done
- Применимые G закрыты в Board до исполнения.
- Steps в allowlist, AC истории покрыты вкладом слоя и integration.
- Counts 6/10 проверены, hidden composition не утекает, initial battle сохранён.
- Named tests/build/format проходят либо concrete baseline failure записан.
- Документы US-0006 обновлены после всех функций.

## Self-containment check
Prerequisites, files, proposed schema, steps и tests заданы; G явно блокируют окончательную детализацию. При drift уточнить Board, не расширять implementation молча.


## Интеграция D-35-21…26 с эскадрой
Нормативное текущее уточнение supersedes прежнюю single-carrier-only трактовку: stable pirate group может быть singleton или demo squad ровно2xЛогово, одна карта-фишка и один automatic report за кампанию. Member identities/loadouts независимы,6 drones каждому,12 squad total; скрытые классы не попадают в world report. Aggregate/runtime/schema принадлежат US0012; multi-carrier assembly US0013; rules US0014; UI/settlement/save US0015. Эти продолжения не заменять объединением drone lists под одним carrier.
Первый automatic sensor report ставит pause; повторные acquisitions не ставят pause и не открывают modal. consumed flag сохраняется по campaign/group. Simultaneous batching CLOSED D-35-33; manual report — отложенное A.
При уничтожении ПОСЛЕДНЕГО carrier стороны оставшиеся surviving drones получают existing Disabled/Speed0, Destroyed не меняются; до этого автоматического отключения нет. Disable не значит pilot death. D-35-26 закрывает orphan G-35-14.
Старые tickets дают prerequisite singleton behavior; итоговая acceptance EP35 требует US0012–0015. Capture20км и sensor200/240/264км остаются утверждёнными; aggregate reference/range policy согласована D-35-32: один group point, max operational radius.
## D-35-27: независимые классы дронов 50/50
Пользователь: «Давай пока независимые 50/50 без ограничений. Добавляй в тикеты».
Для КАЖДОГО drone slot отдельный независимый seeded Bernoulli: light=0.5, heavy=0.5. Нет cap, минимумов или гарантии смешанного крыла; полностью light и полностью heavy допустимы. Actual totals не случайны: Логово6,Танкред10,эскадра2xЛогово12 (6 у каждого stable carrierId).
G-35-07 CLOSED в части class weights/ограничений. Equipment/layout pool CLOSED D-35-39 (четыре exact layouts по25%); existing light frame используется по D-35-38; G-35-08 CLOSED. Эти параметры не утверждены вероятностью50/50. Starter bounds закрыты D-35-28; progression/другие уровни — остаток G-35-05.
A-35-06 сохраняется как рекомендация: материализация один раз на реальном player sensor acquisition, сохранение результата/no reroll при recontact/Load. D о50/50 не переименовывает эту A в пользовательское одобрение persistence policy. Использовать именованный seeded composition RNG stream кампании, не UI/global RNG; каждый слот получает draw и сохраняет origin carrier identity.
Content: явно задать0.5/0.5 weights и отсутствие forced-mix/cap; schema validate finite normalized weights. Runtime не использовать общий draw на корабль/эскадру.
Tests NOT RUN: FixedSeed_ReproducesSlotClasses; EachSlot_ConsumesIndependentDraw; AllLight_Allowed; AllHeavy_Allowed; Totals6_10_AndTwoTimes6Preserved; SquadDrones_KeepCarrierOwnership; SaveLoad_NoCompositionReroll (A-35-06). Проверки deterministic seeds/controlled RNG, без flaky статистических частот.
## D-35-28: точность стартового доклада Ирины
Пользователь: «Да, принимаем ±1 дрон на носитель и ±2 для эскадры. Добавляй в тикеты».
Стартовый sensor skill Ирины даёт одно целое приблизительное reported total: ошибка относительно actual total не более1 для одиночного носителя, не более2 для demo squad2xЛогово. В докладе «около N», точный тип носителя / типы обоих Логов; light/heavy классы и equipment скрыты. Не показывать симметричный диапазон вокруг actual total, из которого восстанавливается скрытый true count.
Допустимые значения: одиночное Логово5..7 при actual6; Танкред9..11 при actual10; эскадра10..14 при actual12. Это границы допустимого одного числа, НЕ UI-диапазон и НЕ случайное actual число дронов.
G-35-05 CLOSED для starter Irina accuracy bounds и формата одного приблизительного числа. Открытый остаток: прогрессия навыка/другие уровни/другие операторы; distribution ошибки не утверждён пользователем. A-35-12: равномерная целочисленная ошибка в допустимых границах, независимый named estimate RNG stream; если принята при реализации, явно сохранить как assumption. Стабильная сохранённая оценка/no reroll — рекомендованная A, не новое пользовательское D.
+10% дальности от module base — отдельный эффект навыка; не трактовать как10% точности. Module радиусы200/240/264км и capture20км без изменений. Squad report один на группу, approximate aggregate total±2, не два самостоятельных forced reports.
G-35-03 CLOSED D-35-29: до20км по текущим observed vectors, real duration при1×; историческое обсуждение цели/часов superseded.
Tests NOT RUN: IrinaSingleEstimate_WithinOne_Integer; IrinaSquadEstimate_WithinTwo_Integer; Report_OneApproximateNumberAndExactTypes; Report_NoClassOrHiddenLoadout; AccuracyDoesNotAlterActual6_10_12; RangeBonusIsNotAccuracyPercent; StableEstimateOnLoad (A). Проверять разрешённый observation DTO, не переносить private composition в client.
## D-35-34: все группы на старте, без возрождения
Пользователь: «Да, все группы создаются на старте кампании, без возрождения в демо».
Ordinary pirate world groups материализуются seeded при New Game: stable group/member IDs, типы, position и patrol route существуют и движутся до player observation. Sensor detection не создаёт world group; он отдельно материализует скрытую light/heavy composition по D-35-13/27 и формирует доклад. Не путать эти моменты RNG.
В демо нет respawn. Save/Load/revisit/уничтожение группы не создают новые группы/копии. Initial group draw использует squad chance10% (или допустимый override при старте); уже созданные группы не меняются после console command. G-35-13 initial cadence CLOSED; lifetime/pre-NewGame CLOSED D-35-40: app memory only, fog немедленно и NewGame/Load, chance только следующий NewGame; restart defaults true/10%. Conditional singleton50/50 утверждено D-35-35; default45/45/10.
Region/safe routes исходного US1 — A-baseline. «Два обычных состава» НЕ количество world groups: ordinary group count теперь4 по D-35-36. Проверить safe start station separation/route bypass. Position/route не скрытое знание player или pirate AI; fog off не acquisition.
Deferred EP39 integration note (не blocker EP35): EP39-US0003-TK0003:52 планирует story-gated captain; пользователь вынес сюжет из текущего грумминга. EP39 не изменён; варианты появления не утверждены.
Tests NOT RUN: NewCampaign_AllOrdinaryGroupsExistBeforeObservation; UndetectedGroups_MoveOnPatrol; FirstAcquisitionDoesNotSpawnWorldGroup; FirstAcquisition_MaterializesOnlyHiddenComposition; DestroyedGroup_NoRespawn; LoadRevisit_NoDuplicates; ConsoleChange_DoesNotMutateExistingGroups; SameCampaignSeed_ReproducesInitialGroups.
## D-35-38: только лёгкие и тяжёлые дроны
Пользователь: «оставим только тяжелых и легких».
Текущий pool классов light/heavy; прежнее medium/heavy SUPERSEDED. Сохраняются independent50/50 per slot, отсутствие caps/forced mix, actual totals6/10/12, скрытый composition на first actual sensor acquisition. Оператор не раскрывает class distribution.
G-35-08 CLOSED: новый medium frame не нужен. Использовать existing Frames/light.json и Frames/heavy.json. Grounding layouts.json:3,16 — heavy sword/spear с2 shields; :29,41 — light sword/spear с1 shield. Equipment/layout IDs и weights теперь CLOSED D-35-39: два existing sword/spear варианта каждого класса,50/50 equipment.
Stable ticket EP-0035-US-0011-TK-0003-medium-content сохраняет исторический slug/ID, но теперь владеет существующим light/heavy layout pool; medium frame creation удалена из allowlist/steps.
## D-35-39: четыре существующих layouts, по25%
Пользователь подтвердил предложенный pool: «да берем. дальше».
Каждый drone slot независимо: class light/heavy50/50, затем sword/spear50/50. Ровно четыре разрешённых ID с marginal probability25% каждый: light.sword.shield,light.spear,heavy.sword.shield,heavy.spear.shield. Нет forced mix, caps или квот на класс/оружие; actual totals6/10/12 неизменны.
Оборудование брать из существующего layouts.json: gun.sword.t1/gun.spear.t1,plate.medium,shield.t1,два drive.standard,decoy.t1; light один щит,heavy два. Никаких новых frames/modules/layout вариантов. Grounding Client/Data/TacticalBattle/layouts.json:2–53. Existing light.spear ID сохраняется несмотря на отсутствие слова shield в имени.
G-35-07 equipment pool/weights CLOSED D-35-39; G-35-08 CLOSED D-35-38. Schema/content валидируют exact IDs и ссылки, runtime использует seeded draws class+equipment per slot, сохраняет per-carrier origin. Hidden loadout не попадает в сенсорный доклад; actual materialization timing/persistence прежние D/A.
Tests NOT RUN: ExactFourAllowedLayoutIds; ClassThenWeapon_IndependentDeterministicDraws; EachSlotUsesItsOwnDraws; LightOneShield_HeavyTwoShields; ExistingEquipmentReferencesValid; AllSameLayoutAllowed; Totals6_10_12Unchanged; PublicReport_NoLayoutDisclosure. Проверять deterministic RNG mapping, не случайные observed frequency.
Debug override lifetime CLOSED D-35-40.
