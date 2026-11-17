---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0011-pirate-signatures
title: Сигнатуры Логова и Танкреда и скрытый состав дронов
stage: draft
approval_kind: workflow
implementation_status: not-started
priority: P0
dependencies: [EP-0035-US-0001-visible-threats, EP-0035-US-0009-sensor-operator]
created: 2026-11-17T11:37:31Z
revision: 1
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Implementation NOT STARTED.

# Сигнатуры Логова и Танкреда и скрытый состав дронов

## Связь с техническим заданием
Пользователь потребовал оценку общего числа дронов по сигнатуре, два типа носителя и случайный light/heavy состав при обнаружении. Отчёт не должен раскрывать классы дронов. Exact request сохранён в эпике.

## User story
Как игрок, по докладу оператора я различаю тип пиратского носителя и оценку общего числа его дронов. Для демо существуют Логово и более вместительный Танкред. Конкретный лёгкий/тяжёлый состав остаётся неизвестным до разрешённого игрового раскрытия.

## Зависимости
EP-0035-US-0001-visible-threats, EP-0035-US-0009-sensor-operator.

## Completion evidence
- AC-01: Типы Логово/Танкред имеют стартовый total 6/10, ссылки на profile валидны.
- AC-02: light/heavy состав создаётся на настоящем sensor acquisition, не debug visibility; классы выбираются независимо50/50 по D-35-27; equipment pool/frame согласованы D-35-38/39.
- AC-03: A-35-06 фиксирует result и identity, проверяет no-reroll; legacy loadout не заменяется молча.
- AC-04: Public signature/report содержат тип и оценку total, не light/heavy/equipment; бой использует actual persisted composition.

## Decisions and gaps
D-35-11: «Да, назначаем Ирину оператором сенсоров. Добавляй в тикеты». Существующая Irina Volskaya / CHR-PLAYER-DEFENSE теперь прямое решение, не A.
D-35-12: число дронов оценивается по сигнатуре пиратского корабля. Для демо два вида: Логово как сейчас и Танкред на 4 дрона больше.
D-35-13: тяжёлые/средние дроны генерируются случайно при обнаружении игроком пирата. В докладе показывать только общее число (оценку) и тип пиратского корабля, не классы/состав дронов.
Grounding count: Data/TacticalBattle/scenarios.json:69–132 — initial.side B carrier.id «Логово», шесть drones Таг-1…Таг-6. Поэтому стартовый total профиля Логово=6, Танкред=10. «Логово» сейчас id конкретного экземпляра, отдельный signature type пока отсутствует.
A-35-06: создавать состав один раз на первом фактическом sensor acquisition, сохранять и не reroll на reacquisition/Load; это рекомендованная защита от повторов, не дословное дополнительное решение пользователя.
G-35-07 weights CLOSED D-35-27: independent50/50 per slot без caps/forced mix. Equipment/layout pool CLOSED D-35-39; existing light/heavy frames по D-35-38, G-35-08 CLOSED.
G-35-08 CLOSED D-35-38: existing light/heavy frames; новый body class не нужен.
G-35-05 starter bounds CLOSED D-35-28/31; +10% range не accuracy%. Distribution/stability остаются A, future progression отложена.
G-35-09: смысл total при частичных потерях/изменении состава и перехват до первого player detection ещё требует явной политики; не менять легитимный existing loadout молча.

## Grounding
- src/DeepSpaceSaga.Client/Data/TacticalBattle/scenarios.json:69–132 — carrier «Логово» и 6 drones Таг-1…6 в initial side B.
- src/DeepSpaceSaga.Client/Data/TacticalBattle/Frames/ содержит carrier.json, heavy.json, light.json, thug.json; light отсутствует.
- src/DeepSpaceSaga.Client/Data/TacticalBattle/layouts.json:2 — heavy.sword.shield; :52 — thug.scrap, не light.
- src/DeepSpaceSaga.Engine/Content/DemoCampaignDefinitions.cs:23 — DemoPirateGroup.LoadoutId, :106 — refs к battle scenario.
- src/DeepSpaceSaga.Engine/SimulationEngine.TacticalBattle.cs:91–119 — текущий build берёт scenario; US-0003 должен интегрировать persisted group composition.
- EngineRequirements «Команды, события и воспроизводимость»: MasterSeed/named RNG, stable order/state.

## Ограничения и gaps
Ни новый light frame, ни его stats/pool weights пока не утверждены. Signature→accuracy и skill progression ещё обсуждаются. Публичный тип не даёт права публиковать private drone list.

## Approved ticket map
Workflow draft:
| Ticket | Результат | Prerequisites |
|---|---|---|
| [EP-0035-US-0011-TK-0001-contracts](EP-0035-US-0011-TK-0001-contracts/EP-0035-US-0011-TK-0001-contracts.md) | Контракт наблюдаемой сигнатуры пиратского носителя | EP-0035-US-0001-TK-0001-contracts |
| [EP-0035-US-0011-TK-0002-schema-engine](EP-0035-US-0011-TK-0002-schema-engine/EP-0035-US-0011-TK-0002-schema-engine.md) | Схема сигнатур и приватного состава | EP-0035-US-0011-TK-0001-contracts |
| [EP-0035-US-0011-TK-0003-medium-content](EP-0035-US-0011-TK-0003-medium-content/EP-0035-US-0011-TK-0003-medium-content.md) | Каталог лёгких и тяжёлых дронов для пиратского пула | EP-0035-US-0011-TK-0002-schema-engine |
| [EP-0035-US-0011-TK-0004-signature-content](EP-0035-US-0011-TK-0004-signature-content/EP-0035-US-0011-TK-0004-signature-content.md) | Логово и Танкред как профили демо | EP-0035-US-0011-TK-0003-medium-content |
| [EP-0035-US-0011-TK-0005-generation-engine](EP-0035-US-0011-TK-0005-generation-engine/EP-0035-US-0011-TK-0005-generation-engine.md) | Материализация пиратских дронов при сенсорном контакте | EP-0035-US-0011-TK-0004-signature-content, EP-0035-US-0001-TK-0003-engine, EP-0035-US-0009-TK-0005-sensor-skill |

## Review log
2026-11-17T11:37:31Z — нарезка по прямому поручению; D-35-11…13 отделены от A/G. Ни код, ни content не реализованы. Build/tests/native/product NOT RUN / OPEN. US-0006 последняя.


## Уточнение сигнатур и скоростей 2026-11-17

D-35-14: фактическое total Логова = текущие 6, Танкред = 6+4 = 10; это фиксированное начальное число, не переменная загрузка/вместимость. Случайны light/heavy классы, не total.
D-35-15: «скорость танкреда должна быть на 20% меньше чем у игрока а у Логова на 10% больше».
D-35-16 (пользователь подтвердил): опорная скорость — operational максимальная скорость фактического стартового носителя игрока, захваченная один раз при Demo New Game; V_tancred=0.80*V_ref, V_lair=1.10*V_ref. Не мгновенная текущая скорость и не адаптивный scaling каждый tick; при торможении/повороте игрока пираты не меняют Vmax. Ускорение и поворот остаются в motion model.
Grounding: текущий Demo scenario.json:6 ссылается на starter-carrier.json, :100–106 устанавливает module.engine.basic; Data/Modules/Engine/modules-engine.json:10 даёт maxSpeedMps=6000. SolarSystemGenerator.OperationalMaxSpeedKmS:13 и SimulationEngine.GetMaxSpeedKmS:3387 вычисляют operational Vmax. На текущем старте пример 6/4.8/6.6 км/с, но не hardcode: после EP-0034 сверить фактический Demo manifest.

## D-35-18 — общая физическая дистанция перехвата

D-35-18 (пользователь): «давай возьмем 100 пикселей на самом приближенном масштабе. сколько это будет? Одинаковая дистанция да».
Оба пиратских типа используют одинаковый capture radius в физических world units. Пиксели — запрос калибровки фиксированного радиуса, не runtime условие. Дистанция боя не зависит от текущего zoom, размера окна, DPI/framebuffer scale или типа пиратского носителя.
A-35-10 [SUPERSEDED D-35-19]: первоначально предлагалось 10 км = 100 world units по базовым 100 логическим px. Пользователь позднее выбрал 20 км = 200 world units; 10 км больше не active baseline.
G-35-10 [CLOSED D-35-19]: утверждены одинаковые 20 км = 200 world units. Историческая калибровка 100 px не используется runtime; dynamic max zoom не меняет physical radius.
Grounding: src/DeepSpaceSaga.Client/Settings.json:30–31 — metersPerPixel[0]=100; src/DeepSpaceSaga.Client/UI/MapScalePolicy.cs:14–20 — max(settings.MaximumPpu, shortSide, diagonal); src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:365–378 — framebuffer/window scale и RenderScreenStack с windowSize, то есть логические UI pixels. EngineRequirements: 1 world unit=100 м.
D-35-19: captureRadiusWorldUnits=200 в content; boundary tests проверяют <200, =200, >200 для обоих типов. Число 100 world units из старого A не использовать.

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