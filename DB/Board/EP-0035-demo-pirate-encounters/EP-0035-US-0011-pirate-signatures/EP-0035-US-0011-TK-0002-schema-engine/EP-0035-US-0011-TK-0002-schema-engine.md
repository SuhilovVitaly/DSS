---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0011-pirate-signatures
ticket: EP-0035-US-0011-TK-0002-schema-engine
title: Схема сигнатур и приватного состава
stage: draft
approval_kind: workflow
implementation_status: implemented-tests-pending
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0035-US-0011-TK-0001-contracts]
files_touched: 3
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-17T11:37:31Z
revision: 1
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Original planning baseline; implementation status and published commits: see current execution ledger.

# Схема сигнатур и приватного состава

## Why
Выполнить owning часть [истории](../EP-0035-US-0011-pirate-signatures.md), сохраняя наблюдаемую сигнатуру отдельно от приватного состава.

## Decisions
D-35-11…13 и exact count grounding — в истории; не выдавать A-35-06/G-35-05/07/08/09 за решения пользователя.

## Assumptions
Одноразовая materialization при первом реальном player acquisition и запрет reroll — действующий контракт. Продуктовые G закрыты; технические API ниже — проект, prerequisites и allowlists проверяются перед исполнением.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Content/DemoCampaignDefinitions.cs | Существующий файл или owning prerequisite US-0001; grounding в истории. | Только steps ниже. |
| src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs | Существующий файл или owning prerequisite US-0001; grounding в истории. | Только steps ниже. |
| tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0035Us0011Tk0002Tests.cs | Новый matching test file. | Только steps ниже. |

## Public API after the change
Проект content signature profiles(totalCount,weightedLayoutPool), private generated composition/generation version/identity state в encounter data.

## Implementation steps
1. Добавить signatureTypeId к demo group и profiles lair/tancred, fixed initial total=6/10.
2. Проверять profile refs, positive counts и допустимые weighted pools; неизвестные light definitions reject до публикации мира.
3. Приватный состав хранить отдельно от public signature/report DTO; сохранить generation flag/seed identity/results.
4. Legacy/materialized groups с существующим loadout сохранять без silent replacement; migration policy G-35-09.

## Out of scope
Изменение initial/tutorial battle, переименование thug без решения, скрытая правка легитимного loadout, случайность от renderer, отображение drone classes, новые NPC фракции, баланс и публикация.

## Invariants
Engine владеет RNG/материализацией и snapshot; public report не содержит private composition. Known signature не означает всеведение. World group identity сохраняется. Sensor skill дальности и точность оценки — разные параметры до решения G-35-05.

## Tests
Будущие tests (NOT RUN):
- DemoEp0035Us0011Tk0002Tests.InvalidProfileAndPool_Rejected
- DemoEp0035Us0011Tk0002Tests.SignatureCounts_AreSixAndTen
- DemoEp0035Us0011Tk0002Tests.PrivateComposition_NotInPublicProjection
Создать абсолютный <run> внутри D:/DeepSpaceSaga/artifacts/validation/ep0035-signatures/; process tree cap 300000 ms, timeout остановить.
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter "FullyQualifiedName~DemoEp0035Us0011Tk0002Tests" --results-directory <run>/tests --logger trx
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path <run>/build
dotnet format src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --include src/DeepSpaceSaga.Engine/Content/DemoCampaignDefinitions.cs src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0035Us0011Tk0002Tests.cs
JSON — loader tests; native/product отдельно.

## Definition of Done
- Применимые G закрыты в Board до исполнения.
- Steps в allowlist, AC истории покрыты вкладом слоя и integration.
- Counts 6/10 проверены, hidden composition не утекает, initial battle сохранён.
- Named tests/build/format проходят либо concrete baseline failure записан.
- Документы US-0006 обновлены после всех функций.

## Self-containment check
Prerequisites, files, proposed schema, steps и tests заданы; G явно блокируют окончательную детализацию. При drift уточнить Board, не расширять implementation молча.


## Уточнение сигнатур и скоростей 2026-11-17

D-35-14: фактическое total Логова = текущие 6, Танкред = 6+4 = 10; это фиксированное начальное число, не переменная загрузка/вместимость. Случайны light/heavy классы, не total.
D-35-15: «скорость танкреда должна быть на 20% меньше чем у игрока а у Логова на 10% больше».
D-35-16 (пользователь подтвердил): опорная скорость — operational максимальная скорость фактического стартового носителя игрока, захваченная один раз при Demo New Game; V_tancred=0.80*V_ref, V_lair=1.10*V_ref. Не мгновенная текущая скорость и не адаптивный scaling каждый tick; при торможении/повороте игрока пираты не меняют Vmax. Ускорение и поворот остаются в motion model.
Grounding: текущий Demo scenario.json:6 ссылается на starter-carrier.json, :100–106 устанавливает module.engine.basic; Data/Modules/Engine/modules-engine.json:10 даёт maxSpeedMps=6000. SolarSystemGenerator.OperationalMaxSpeedKmS:13 и SimulationEngine.GetMaxSpeedKmS:3387 вычисляют operational Vmax. На текущем старте пример 6/4.8/6.6 км/с, но не hardcode: после EP-0034 сверить фактический Demo manifest.
Profiles totalCount=6/10, не capacity/maxSlots; weights выбирают только классы. Frozen speed reference и materialized group Vmax должны roundtrip; тип не меняет скрытый total случайным filling.

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

## Execution scope 2026-11-17
Replace the unused encounters draft schema with v2 profiles/weights/geometric limits: no shipped encounters.json or persisted materialized group exists at the execution base. Remove obsolete SightRadius/EscapeRadius/EscapeMotionTimeMs sources; sensors remain module-owned, lost-contact behavior has no cooldown. Add owning DemoEncounterDefinitions.cs and PirateCompositionData.cs to keep validated content/private save records separate. Early dependency only US1/US11 contract tickets, enabling US1 content without a semantic cycle. Optional missing save composition remains empty; no silent reroll/migration of a populated unsupported version.


Implemented validated v2 content with fixed6/10 profiles, speed1.1/0.8, ordered independent class/weapon layout pool, module-owned sensor refs, four-group45/45/10 and capture/safe geometry. Private saved compositions reject malformed counts/duplicate identities/unsupported generation versions. Release Engine.Tests build PASS, zero warnings/errors; format/diff PASS. Evidence D:/DeepSpaceSaga/artifacts/validation/ep-0035/20261117-resume/us11-tk2-build.log. Tests authored, NOT RUN until US11 boundary.
