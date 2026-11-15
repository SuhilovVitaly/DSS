---
epic: EP-0029-unified-carrier-and-torpedoes
title: Единый модульный носитель, постоянное крыло и конечный запас торпед
stage: draft
created: 2026-11-14T19:42:24Z
source_request: "Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами."
current_review: complete
revision: 1
concept_approval: "в остальном одобряю"
approval_scope: "Концепция и уточнения пользователя; детализация и численные assumptions подготовлены агентом, отдельного human approval каждой story не было."
implementation_status: not-started
---

# EP-0029 — Единый модульный носитель и боезапас

Корабль игрока в солнечной системе и носитель в тактическом бою — один объект мира. Его модули обеспечивают хранение дронов и жильё, экипаж управляет постоянным крылом, фактические потери и расход торпед сохраняются. Учебный бой является настоящим первым боем.

**Объём:** 13 историй, 47 тикетов. [Полная карта тикетов и очередность](Tickets.md). Документы подготовлены, реализация не начата; автоматические, native и owner acceptance ворота открыты.

## Исходное техническое задание и решения

Исходная идея: единый модульный корабль; ангары на 1/2 дрона; увеличить жильё; четыре женщины-пилота с английскими именами и подобранными портретами. Предложенная концепция принята с уточнениями ниже.

Сообщение пользователя (дословно):

> два модуля жилых каждый на 4 места. 2 места будут для пленных пилотов пиратов в остальном одобряю. создай эпик с детальным описанием задачи по сторисам и тикетам. Учебный бой настоящий - если игрок потеряет корабль или дроны то это так и останется. заодно давай введем запас торпед на дрон. Легкий 6 торпед тяжелый 12 торпед и на корабле в грузовом трюме 42 торпеды для перезарядки между боями. Если есть вопросы задай их, если нет сделай детальный эпик

Последующее уточнение (дословно):

> Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.

Время записи этих сообщений в Board: **2026-11-14T19:42:24Z**. Это время фиксации, не доказанное время отправки пользователем.

- **D-01:** один мировой корабль и его боевое представление, два двухместных ангара в стартовой комплектации; тип одноместного ангара тоже входит в каталог.
- **D-02:** два жилых модуля по четыре места, шесть членов экипажа, два зарезервированных места для будущих пленных.
- **D-03:** захват/пленные сейчас не реализуются; резерв не создаёт персонажей.
- **D-04:** четыре женщины Evelyn Ward, Audrey Bennett, Claire Morgan, Rachel Hayes; выбранные ниже портреты и прежние навыки. Ирина Вольская занимает прежнюю позицию безымянного Defense operator.
- **D-05:** учебный бой настоящий. Потеря дронов сохраняется; гибель носителя не отменяется возвращением на карту.
- **D-06:** light magazine6, heavy12, cargo42; один фактический выстрел — одна торпеда; пополнение только из трюма между боями.
- **D-07:** создать весь подробный Board epic со stories/tickets в текущем поручении. Реализация/commit/push не запрошены.

Инструкции исполнения берутся из канонического AGENTS и навыка epic-writer. Цитаты — продуктовые требования и поручение на планирование; исторические указания о push в других эпиках не дают разрешения публиковать этот.

## Grounding — текущая реализация

- **G-01** `src/DeepSpaceSaga.Client/Scenarios/Tutorial/scenario.json:11` — SPC-0001 / ship.tetrarch, 6 установленных модулей и 2 члена экипажа; боевого крыла нет.
- **G-02** `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:1081` — StartTutorialAsync создаёт скрытый Tutorial; StartTacticalBattleRequest использует seed 1, без identity корабля.
- **G-03** `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleAssembly.cs:24` — Create собирает носитель и дронов из сценария и каталога; мировой корабль не передан.
- **G-04** `src/DeepSpaceSaga.Engine/SimulationEngine.TacticalBattle.cs:81` — ExitTacticalBattle закрывает сессию без settlement мира.
- **G-05** `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs:181` — Фактический выстрел возникает после повторной проверки доступности цели, до бросков absorption/hit; учёта торпед нет.
- **G-06** `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleState.cs:66` — Limits вычисляется из масс frame/armor/weapon/shields/engines/decoy; боезапас не учтён.
- **G-07** `src/DeepSpaceSaga.Client/Data/Modules/LivingQuarters/modules-livingquarters.json:24` — Существует living.quarters.mk1.four-cabins с cabines=4, массой 2000 кг.
- **G-08** `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:344` — ShipCrewMemberData хранит CrewId, DisplayName, AccuracySkill, ManeuverabilitySkill; нет общей identity портрета/пола/инициативы.
- **G-09** `src/DeepSpaceSaga.Client/Data/UI/tactical-battle-pilots.json:4` — Отдельная клиентская карта pilotId -> portrait/female; yun/oleg сейчас мужчины.
- **G-10** `src/DeepSpaceSaga.Client/UI/Screens/Ship/ShipScreen.cs:9` — ShipScreen — shell с PlaceholderLines; готового экрана управления крылом нет.
- **G-11** `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:38` — CurrentSaveFormatVersion = 20; новый writer и миграция требуют явного решения.
- **G-12** `src/DeepSpaceSaga.Engine/Scenario/SaveMigration20.cs:85` — Ammo удаляет item.ammo.torpedo*; legacy combat нельзя смешать с новым inventory.
- **G-13** `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:1195` — EnsureShipDestroyedScreen уже переводит погибший корабль в ShipDestroyedScreen.
- **G-14** `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalShipSprites.cs:25` — Для носителя выбран общий sprite carrier; class/image identity мира не используется.
- **G-15** `src/DeepSpaceSaga.Engine/Content/ShipClassDefinition.cs:6` — Класс пока содержит только typeId, комментарий относит durability к модулям. Общая прочность корпуса — новое требование.
- **G-16** `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialBattleWinnabilityTests.cs:14` — Есть сценарии EnterOnlyPlayerWinsTutorialBattle и HintFollowingPlayerWinsTutorialBattle. Это код проверок, не свидетельство их прохождения в этой задаче.

Применимые контракты: [EngineRequirements](../../Documentation/01-Requirements/EngineRequirements.md), [TacticalBattle](../../Documentation/04-Engineering/TacticalBattle.md), [SaveFormat](../../Documentation/04-Engineering/SaveFormat.md). Это исходные требования; данный эпик явно проектирует изменения lifetime боя и writer, не утверждает, что нормативные документы уже обновлены.

## Пересечения и приоритет в Board

| Источник | Пересечение | Решение в этом эпике |
|---|---|---|
| [EP-0021](../EP-0021-drone-combat-economy/Documentation.md), US-0003/0004/0005/0011 | Ангары, owned wing, sortie, settlement, save | Эти части будут реализованы здесь для готового стартового корабля. Не реализовывать их второй раз по старому контракту. Старый текст остаётся draft; при продолжении EP-0021 его задачам нужна сверка с завершённым EP-0029. |
| EP-0021 D-01 и US-0008/0009/0010/0012 | Пустой старт, верфь, закупка, найм | Не являются блокирующими зависимостями. Пустой старт остаётся отдельным будущим режимом; Default/Tutorial в EP-0029 получают готовый комплект. |
| EP-0021 US-0003 AC-01 | Один уникальный ангар на дрон | Уточняется: уникальное место (HangarModuleId, SlotIndex); два дрона могут находиться в одном M. |
| EP-0021 US-0005 и описание эпика | Мир остаётся доступен после поражения; тренировочные бои без потерь | При живом носителе продолжение возможно; погибший носитель завершает кампанию. Tutorial больше не исключение из settlement. |
| [EP-0027](../EP-0027-tutorial-start/Documentation.md) | Tutorial сразу открывает бой, после него карта | Карта только при живом носителе. Досрочный выход в меню сохраняет последствия перед dispose. |
| [EP-0028](../EP-0028-battle-tutorial/Documentation.md) | Ирина и ненавязчивые RU/EN подсказки | Сохранить её имя/портрет и поведение подсказок; теперь это реальный член crew, тексты описывают конечные торпеды и потери. |
| [EP-0025](../EP-0025-modular-character-portraits/Documentation.md) | Модульные портреты | Переиспользовать существующие W4 PNG, workshop и генерация новых активов не нужны. |

Все эти ссылки — задачи только из DB/Board. Никакая зависимость не считается выполненной потому, что существует её документ. Этот эпик не зависит от исполнения всего EP-0021.

## Целевая модель и ownership

Engine хранит единственный authoritative CarrierState у мирового ShipId. Внутри — корпус, ссылки на реальные installed modules, roster, reserved cabin slots, owned drones, equipment, magazines, повреждения, receipts и revision. Definitions дают типовые параметры, runtime — текущие остатки и состояние.

Поток: **scenario manifest → Engine world → CarrierOperationsSnapshot → Client**. Вылет: **world carrier → OwnedBattleAssembly → TacticalBattleSession → atomic settlement → тот же world carrier → checkpoint**. Client не вычитает ресурсы и не пишет мир.

Contracts передаёт immutable DTO и команды. LocalClient выполняет transport и запись checkpoint. Motion остаётся общей математикой: новые masses подаются в существующий TacticalLimits, формулы попадания и движения не переписываются.

Предлагаемые API ниже и в тикетах являются **проектируемыми**, а не обнаруженными существующими методами:
- CarrierOperationsSnapshot и OwnedDroneSnapshot — единая read-only проекция.
- StartTacticalBattleRequest.SourceShipId/ExpectedCarrierRevision — привязка к миру.
- RearmDroneRequest(CommandId, ShipId, DroneId, Quantity, ExpectedRevision).
- IGameSessionConnection.RearmDroneAsync → CarrierCommandResponse.
- SortieSettlementSnapshot — квитанция применённых последствий, не гарантия успешной записи файла.

UI и логика отличают отсутствующие данные старого payload от нулевого магазина. Не использовать null как бесконечный боезапас. Stable IDs не зависят от имён/языка.

## Стартовый корабль и размещение

Мировая identity: **SPC-0001**, класс **ship.tetrarch**, display name **Skitalets**. AppearanceId получается из того же world image, не из отдельного боевого carrier silhouette. Начальный hull400/400, тактическая манёвренность35 берутся из общего class profile (A-01). Добавление кают не увеличивает hull.

Manifest один: `src/DeepSpaceSaga.Client/Data/Ships/starter-carrier.json`; Tutorial и Default ссылаются на него. New Game материализует копию в мир; Save хранит развёрнутые данные. Default_500, прочие scenarios и старые saves не переоснащаются автоматически.

Раскладка — техническое A-01, координаты от нуля. Grid остаётся9×9; доступные cells по строкам: y0:x4; y1:x3..5; y2:x1..7; y3:x1..7; y4:x3..5; y5:x3..5. Это 24 cells, остальные отсутствуют.

| ModuleId | Тип | Anchor / footprint | Вместимость |
|---|---|---|---:|
| MOD-PLAYER-BRIDGE-01 | module.bridge.navigation.computer.basic | (4,0),1×1 | — |
| MOD-PLAYER-LIVING-QUARTERS-01 | living.quarters.mk1.four-cabins | (3,1),1×1 | 4 человека |
| MOD-PLAYER-LIVING-QUARTERS-02 | living.quarters.mk1.four-cabins | (5,1),1×1 | 4 человека |
| MOD-PLAYER-CARGO-01 | module.container.basic | (4,2),1×1 | Каталог контейнера |
| MOD-PLAYER-SCANNER-01 | module.scanner.mk1 | (4,3),1×1 | — |
| MOD-PLAYER-GENERATOR-01 | module.generator.basic | (4,4),1×1 | — |
| MOD-PLAYER-ENGINE-01 | module.engine.basic | (4,5),1×1 | fuel750кг |
| MOD-PLAYER-BAY-01 | module.drone-bay.medium | (1,2),2×2 | 2 дрона |
| MOD-PLAYER-BAY-02 | module.drone-bay.medium | (6,2),2×2 | 2 дрона |

Всего9 установленных модулей,15 занятых клеток,9 свободных клеток. Форма кают1×1 сохраняет существующую slotSize схему; это не физический масштаб комнат. Starter modules начинаются исправными с соответствующим SPmax. Деньги2000, energy cells1000, food200 и fuel750 сохраняются; добавляется ровно42 торпеды в cargo.

Ангар S: footprint1×2, capacity1, mass2500кг, SP150. Ангар M:2×2, capacity2, mass4500кг, SP250. Это явные предварительные числа A-01; M выгоднее по массе на место, но занимает связную область. Цены/производство ангаров не вводятся.

## Экипаж и портреты

| CrewId | Имя и роль | Portrait suffix в Images/Persons/W4/Portraits | Accuracy/Maneuver/Initiative | Дрон |
|---|---|---|---|---|
| CHR-0001 | Dunkan Su, капитан | Существующий captain portrait | Прежние | — |
| CHR-PLAYER-DEFENSE | Irina Volskaya, офицер связи | CHR-20260915-104309-12BAB0.png | Прежние/не боевая пилот | — |
| CHR-PLAYER-EVELYN | Evelyn Ward, старшая пилот | CHR-20260915-102445-3C5C5D.png | 55/52/55 | DRN-PLAYER-01 |
| CHR-PLAYER-AUDREY | Audrey Bennett, пилот | CHR-20260915-102448-E2757F.png | 53/55/53 | DRN-PLAYER-02 |
| CHR-PLAYER-CLAIRE | Claire Morgan, пилот | CHR-20260915-104315-4DF131.png | 59/54/59 | DRN-PLAYER-03 |
| CHR-PLAYER-RACHEL | Rachel Hayes, пилот | CHR-20260915-102447-5F3DEF.png | 50/51/50 | DRN-PLAYER-04 |

Все4 пилота female. Ирина сохраняет собственный портрет и tutorial speaker identity. Её role становится реальным crew role; она не пятый пилот. Legacy keys vera/yun/marta/oleg остаются aliases диагностического каталога, не являются owned crew IDs.

Cabin assignment: первый жилой модуль — капитан, Ирина, Evelyn, Audrey; второй — Claire, Rachel и reserved slots2/3. **Всего8, занято6, резерв2, свободно для обычного назначения0, фактических пленных0.** Резерв сохраняется и не расходует пайки как человек. Повреждение жилья показывает дефицит и запрещает новые назначения, не удаляет экипаж.

## Крыло и торпеды

| DroneId | Bay / slot | Frame/layout | Щиты | Магазин |
|---|---|---|---:|---:|
| DRN-PLAYER-01 | BAY-01 / 0 | heavy.sword.shield | 2 | 12/12 |
| DRN-PLAYER-02 | BAY-01 / 1 | heavy.spear.shield | 2 | 12/12 |
| DRN-PLAYER-03 | BAY-02 / 0 | light.sword.shield | 1 | 6/6 |
| DRN-PLAYER-04 | BAY-02 / 1 | light.spear | 1 | 6/6 |

У всех plate.medium,2×drive.standard,decoy.t1 с2 зарядами. У light.spear щит тоже есть. Пилот управляет дистанционно: потеря дрона не убивает пилота.

Товар `item.drone-torpedo.t1`, целые штуки, unitMass1кг, базовая цена60 Tokens (A-02, техническая величина каталога, не обещание рыночной доступности). Один тип совместим с текущими Меч/Копьё и прочими оружиями дронов; новая баллистика/новое оружие не вводятся. Light6/heavy12 — D-06; enemy thug6 — A-03. Начало: **12+12+6+6+42=78**.

Масса magazine входит в массу дрона; cargo ammo учитывается только в контейнере. При rearm масса переносится, не дублируется; после выстрела уменьшается. Не менять мировую Vmax/географию из-за этой задачи. Если сухая масса корпуса в текущем мире неизвестна, показывать известную массу оснащения отдельно, не выдумывать полный тоннаж.

### Момент расхода и порядок

1. Весь пакет Fire валидируется до затрат, журнала и RNG.
2. Инициатива, гибель стрелка/цели, jam, dust, evade, hold и окончание боя проверяются в прежнем порядке.
3. При действительно допустимом candidate и ammo>0 списать1, затем выполнять absorption/hit/damage RNG.
4. Hit, miss, absorption, decoy не возвращают торпеду. Нет candidate/skip — нет расхода.
5. UnitsAfter данного шага содержит новое ammo; replay только отображает.

При ammo0 — движение/E/D доступны по прежним условиям, огонь недоступен. Одна сторона без ammo ещё может получить урон. Если обе стороны не имеют active,!jammed дрона с ammo>0, после проверки обычной победы наступает Draw/NoCombatCapability. Dust и расстояние не являются истощением. SeriesTurnCap применяется к interactive path, чтобы не оставлять бесконечное уклонение. Никакой автоматической сдачи стороны только из-за ammo0.

### Перезарядка

В космосе или доке, только между боями, ручной запрос на конкретный дрон. В первой версии мгновенно, без временной стоимости (A-04). quantity>0, q<=недостача и q<=доступный cargo stock; иначе весь запрос отклоняется. Disabled дрон можно пополнить, но это не чинит его. Destroyed нельзя.

UI по умолчанию предлагает min(недостача,stock), позволяет уменьшить q, показывает preview. Engine повторно проверяет identity/revision/состояние и переносит ровно q. Контейнеры расходуются по ordinal ModuleId, lots по FIFO с сохранением basis. Повтор CommandId идемпотентен, другой payload с тем же ID — conflict. В бою recharge запрещён даже если на борту42.

Примеры: пустое крыло + cargo42 -> полные36 + cargo6; light2/6,cargo42,q4 ->6/6,cargo38; q5 при свободных4 — атомарный отказ. Нет автоматического refill при входе в бой/Load. Повторная покупка/производство торпед и верфь — backlog EP-0021, а не скрытое бесконечное снабжение.

## Потери, завершение и сохранения

- При живом носителе все surviving/disabled свои дроны возвращаются в свои slots (A-05, recovery без отдельной эвакуационной игры). HP/system damage/ammo/decoy сохраняются; disabled не боеготов.
- Destroyed дрон остаётся tombstone; его remaining ammo считается lost with magazine, не fired и не cargo. Пилот остаётся в crew.
- Потеря всего крыла означает поражение в бою, но при живом носителе кампанию можно продолжать. Новый вылет отклоняется до появления пригодного крыла; его восстановление через верфь вне этого эпика.
- Hull носителя0 -> world IsDestroyed, запрет полёта/торговли/rearm/new sortie, terminal screen. Не заменять корабль и не выдавать страховку. Адресное повреждение установленных ангаров в этом эпике не симулируется.
- Terminal outcome атомарно settled в Engine один раз, abort settled перед Exit; сохраняется BattleId receipt и carrier revision. World coordinates не подменяются тактическими.
- На окончании exit освобождает active battle, LocalClient записывает отдельный campaign checkpoint atomic temp+replace. Save внутри боя запрещён. После успеха можно закрыть session/перейти на карту или terminal screen.
- При ошибке storage: committed world остаётся в памяти, показывается Retry, session не dispose; retry не повторяет расход. Нет гарантии восстановления активного боя после принудительного убийства процесса.
- Старые ручные saves не перезаписываются, их загрузка разрешена. Ironman/запрет загрузки/удаление старых saves не вводятся.
- Save21/schema1 хранит expanded состояние и receipts. Reader18/19/20 проходит явную миграцию без выдачи starter assets. Старый reader20 не должен молча проглотить новый21.
- New SKU не использует legacy prefix `item.ammo.torpedo`; миграция18/19 продолжает удалять старые realtime projectiles. Известный старый economic fingerprint совместим только по проверяемой allowlist, generic bypass запрещён.
- Старый мировой корабль без wing получает объяснение отсутствия крыла; F8 не выдаёт ему временные четыре дрона. Diagnostic сценарный battle остаётся внутренним/test path.

## Assumptions, риски и backlog

- **A-01:** технические размеры/массы/SP ангаров, расширенный hull footprint,400HP/35maneuver и одна стартовая модель Default/Tutorial. Эти числа требуют будущего balance review.
- **A-02:** единый SKU,1кг и60 Tokens; loaded ammo влияет на tactical mass. Значения не выдаются за утверждённый пользователем баланс.
- **A-03:** thug magazine6, finite ammo с обеих сторон; правила stalemate/turn cap выше.
- **A-04:** мгновенная ручная перезарядка между боями, не автоматическая и без лишней кнопки refill-all.
- **A-05:** surviving disabled дроны возвращаются при живом носителе; salvage/эвакуация не вводятся.
- **A-06:** отдельный automatic continuation checkpoint после каждого sortie для сохранения реальных последствий; manual saves остаются.
- **R-01:** finite ammo меняет исходы seed1 и экономический срок жизни крыла. Existing tutorial tests нужно реально прогнать; не обещать прежнюю победу до проверки.
- **R-02:** Save21 и fingerprint migration затрагивают старые saves, нужна матрица18/19/20/21 и unknown fingerprints.
- **R-03:** ShipScreen сейчас shell; собственный read model/layout/pending UI входят в scope.
- **R-04:** общий HP корпуса — новое состояние в мире. Жильё не служит бронёй; полноценная повреждаемость конкретных modules — backlog.
- **R-05:** storage failure между settlement и checkpoint не может отменить committed мир; retry и user-visible error обязательны.
- **Backlog:** захват/обмен/выкуп пленных; верфь и покупка новых дронов; продажа/производство торпед; найм; новые портреты; ремонт; адресные попадания по ангару; сохранение внутри боя; real-time world combat; Ironman.
- **Блокирующих продуктовых вопросов для подготовки документации нет.** Native/product/balance acceptance остаются будущими воротами, не причиной прекращать планирование.

## Истории и зависимости

| Story | Результат | Зависимости |
|---|---|---|
| [US-0001 — Модульный корпус и ангарные места](EP-0029-US-0001-carrier-modules/EP-0029-US-0001-carrier-modules.md) | Игрок получает носитель с реальной схемой модулей и видит, чем ограничена вместимость крыла. | none |
| [US-0002 — Шесть членов экипажа и два зарезервированных места](EP-0029-US-0002-crew-and-reserved-cabins/EP-0029-US-0002-crew-and-reserved-cabins.md) | Игрок узнаёт тех же четырёх женщин-пилотов на корабле и в бою. | US-0001 |
| [US-0003 — Постоянное крыло корабля](EP-0029-US-0003-persistent-wing/EP-0029-US-0003-persistent-wing.md) | Игрок владеет четырьмя конкретными дронами, размещёнными по ангарным местам. | US-0001, US-0002 |
| [US-0004 — Конечный боезапас в пошаговом бою](EP-0029-US-0004-finite-torpedoes/EP-0029-US-0004-finite-torpedoes.md) | Игрок видит конечный магазин дрона и осмысленно выбирает выстрел. | US-0003 |
| [US-0005 — Бой из реального корабля и его крыла](EP-0029-US-0005-world-owned-sortie/EP-0029-US-0005-world-owned-sortie.md) | Учебный бой и обычный вход с карты используют того же носителя и принадлежащих ему дронов. | US-0001, US-0002, US-0003, US-0004 |
| [US-0006 — Необратимые последствия завершённого боя](EP-0029-US-0006-battle-consequences/EP-0029-US-0006-battle-consequences.md) | Уцелевший корабль возвращается в систему с фактическим ущербом и остатком боезапаса. | US-0005 |
| [US-0007 — Сохранение корабля, потерь и боезапаса](EP-0029-US-0007-save-continuity/EP-0029-US-0007-save-continuity.md) | Игрок продолжает кампанию с теми же людьми, дронами, повреждениями и остатком торпед. | US-0006 |
| [US-0008 — Перезарядка дронов из грузового трюма](EP-0029-US-0008-rearm-between-battles/EP-0029-US-0008-rearm-between-battles.md) | Между боями игрок выбирает дрон и пополняет его магазин из реального груза корабля. | US-0007 |
| [US-0009 — Экран корабля, ангаров и экипажа](EP-0029-US-0009-ship-management-ui/EP-0029-US-0009-ship-management-ui.md) | Игрок открывает корабль и видит реальный корпус, два ангара, четыре пилота, два жилых модуля и торпеды в трюме. | US-0008 |
| [US-0010 — Корабль, пилоты и боезапас на экране боя](EP-0029-US-0010-tactical-presentation/EP-0029-US-0010-tactical-presentation.md) | Игрок узнаёт свой корабль и экипаж в тактическом бою. | US-0005, US-0006, US-0009 |
| [US-0011 — Готовый учебный Tetrarch и стартовый состав](EP-0029-US-0011-starter-loadout/EP-0029-US-0011-starter-loadout.md) | Новая учебная кампания начинает реальный бой на оснащённом модульном корабле. | US-0001, US-0002, US-0003, US-0004, US-0007 |
| [US-0012 — Реальный учебный запуск и надёжное завершение](EP-0029-US-0012-campaign-lifecycle/EP-0029-US-0012-campaign-lifecycle.md) | Игрок начинает обучение без обхода ownership, а после боя получает сохранённые последствия. | US-0006, US-0007, US-0008, US-0010, US-0011 |
| [US-0013 — Сквозная приёмка постоянного носителя](EP-0029-US-0013-acceptance-and-regression/EP-0029-US-0013-acceptance-and-regression.md) | Игрок может пройти цепочку обучение → карта → корабль → перезарядка → второй бой → сохранение/загрузка без потери identity и без выдачи ресурсов. | US-0012 |

Порядок по возрастанию US и TK — допустимая последовательная топологическая очередь. Зависимости каждого тикета включают предыдущий тикет истории и/или конечные тикеты prerequisite stories. Работа с общими файлами выполняется последовательно. Все проектируемые API определены до потребителей; не начинать сразу с переименования JSON и считать интеграцию завершённой.

## Приёмка и проверяемые примеры

| Gate | Действие / независимый oracle | Ответственная история |
|---|---|---|
| V-01 starter | new Tutorial/Default:9 modules,24 hull cells,15 occupied,8beds/6crew/2reserve,4drones,36+42ammo | US-0001/0002/0011 |
| V-02 identity | compare SourceShipId/DroneId/CrewId, hull/image/name на карте, в бою, после Load | US-0003/0005/0010/0013 |
| V-03 ammo | hit/miss/absorb по1; killed-before-slot/hold/evade/dust/jam/target gone по0; invalid batch без всех затрат | US-0004 |
| V-04 rearm | light2/6 +4 из42 ->6/6,38; empty wing +36 -> full,6; duplicate не меняет второй раз | US-0008/0009 |
| V-05 conservation |78 = current magazines + hold + actually fired + ammo lost in destroyed magazines; после нескольких боёв и rearm | US-0006/0013 |
| V-06 defeat | drones0+shipalive -> map; ship0 -> terminal; обе стороны уничтожены -> draw боевого outcome, player terminal | US-0006/0012 |
| V-07 save |18/19/20->21 без grants;21 roundtrip; corrupted refs/qty/fingerprint reject; original file неизменен | US-0007 |
| V-08 lifecycle | abort/checkpoint/menu, duplicate Exit, out-of-order snapshot, disk write fail/retry, cold load checkpoint | US-0012 |
| V-09 tutorial | seed1 Enter-only и hint-following на production path, actual outcome и расходы; seeds1..100 как балансный отчёт | US-0013 |
| V-10 native | RU/EN,1280×720 и1920×1080, mouse/keyboard rearm, portrait/name, ammo replay, defeat screen | US-0009/0010/0013 |

Named tests и точные будущие команды находятся в тикетах. Каждому test process задавать process-tree cap300000ms; при превышении делить по именам tests. Outputs только в `D:/DeepSpaceSaga/artifacts/validation/ep-0029/<run>/`, создать каталог до запуска. Не запускать build/test ради этой planning-only задачи.

Native запуск разрешён только при проверенном меньшем мониторе, отдельном профиле и `DSS_VALIDATION_SMALL_MONITOR=1`. Нет меньшего монитора — только headless, native gate OPEN. CPU/snapshot доказательства не равны physical input/GPU/owner acceptance. Snapshot baseline не заменять автоматически.

## Review log и epic review log

- 2026-11-14T19:42:24Z: записаны согласованные D-01..D-07 и исходные сообщения. Пользователь поручил создать весь пакет после снятия вопроса о пленных; повторный поштучный запрос approval не требуется для материализации.
- Детализация, filenames, APIs, A-01..A-06, порядок историй и ticket maps подготовлены агентом. `stage:draft` эпика/историй честно отличает эти новые детали от принятой концепции. `stage:approved` тикетов означает **workflow approval плана**, не human review и не разрешение на выполнение.
- Реализация не начата; код, игра, сборка, tests и native acceptance в этой planning-задаче не выполнялись.
- Документы исполнения впоследствии должны синхронизировать TacticalBattle/SaveFormat/EngineRequirements с реально принятой реализацией; настоящее поручение не меняет нормативные файлы.

## Проверка подготовленных документов

В planning-сессии выполнены read-only проверки через Node: 62 Markdown-файла, 13 историй, 47 тикетов; соответствие ID/frontmatter/папок/имён; обязательные разделы; локальные ссылки Board; полное покрытие story AC картами тикетов; ацикличность всех зависимостей. Ошибок не обнаружено. 65 ссылок на ещё не созданные source/test файлы проверены как явно проектируемые пути, а не существующая реализация.

Проверены один production layer и matching test project на тикет, максимум5 implementation files (включая тесты) и максимум5 тикетов на историю. Независимый расчёт предложенной геометрии:24 клетки корпуса,15 занятых,9 модулей, пересечений и выхода за корпус нет. Числовой oracle старта:12+12+6+6+42=78 торпед. Проверка whitespace новых файлов и git diff --check пройдены. Это проверка плана; игровые tests, build, запуск игры и балансные прогоны не выполнялись.
