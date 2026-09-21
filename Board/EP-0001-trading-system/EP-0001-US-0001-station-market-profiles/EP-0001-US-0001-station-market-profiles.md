---
epic: EP-0001-trading-system
story: EP-0001-US-0001-station-market-profiles
title: Пять различающихся локальных рынков
stage: approved
dependencies: []
created: 2026-09-21T08:33:04Z
source_request: 'Сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0001-station-market-profiles\EP-0001-US-0001-station-market-profiles.md эпик D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\Documentation.md'
current_review: complete
revision: 2
---

# Пять различающихся локальных рынков

## Входное техническое задание

Точный запрос пользователя сохранён в source_request. 2026-09-21T08:33:04Z — время регистрации в planning log, не время отправки сообщения. Эпик: ../Documentation.md:92–118. Все пути к коду/требованиям ниже относительны D:/DeepSpaceSaga/DSS.

Исходная связь с ТЗ: Concept:38–96 и MVP:69–187, 435–437: общий каталог и пять экономических ролей. Concept и MVP — Documentation/02-FirstRelease/Mechanics/TradingSystemConcept.md и TradingSystemMvpStories.md. Торговые единицы и параметры электроники определены в Documentation/01-Requirements/EngineRequirements.md:5128; каталог подключён в src/DeepSpaceSaga.Client/Settings.json:5.

Исходная user story сохранена: «Как игрок, я хочу встречать добывающую, промышленную, гидропонную, транзитную и научно-военную станции с различающимися предложением и потребностями, чтобы выбирать место покупки и продажи груза. На каждой станции я вижу её локальный ассортимент и доступный запас через существующий экран торговли. Товары имеют понятные научно-фантастические названия и единицы количества, а уже известные идентификаторы сохраняются. Автор сценария может назначить станции один из пяти профилей и получает понятную ошибку до запуска, если профиль ссылается на неизвестный товар».

Исходные completion evidence: пять ролей с различающимися предложениями/потребностями; Station → Trade показывает ассортимент, запасы и единицы, операции доступны при достаточных средствах/вместимости; неверная товарная ссылка не допускает запуск. Ограничения: 12 базовых позиций, electronics / ITM-3006; explicit inventory имеет приоритет; Fuel — refuel-service без обратной продажи; автоматическое изменение запасов, котировки и процедурное размещение не входят.

## User story

Игрок сравнивает пять явно заданных локальных рынков через существующий Station → Trade. Профиль задаёт стартовый ассортимент, характерные запасы и бюджет, а обычные Buy/Sell/Refuel работают с authoritative состоянием. Товары имеют понятные Sci-Fi названия и единицы в обоих языках интерфейса. Автор сценария назначает профиль стабильным ID и получает ошибку конфигурации до старта сессии.

## Acceptance criteria

- AC-01: единый каталог разрешает 12 базовых MVP ID: пять Resource и семь Good, включая item.electronics / ITM-3006 / Good / Piece («блок») / Cargo / 1 кг / 150 Credits / шаги 1. Старые ID и остальные экономические параметры сохраняются.
- AC-02: JSON задаёт пять профилей Mining, Industrial, Hydroponic, Transit, Scientific/Military со списками supply/demand по таблице эпика :112–116. Ассортимент и стартовые количества различаются, будущие rates не исполняются.
- AC-03: неизвестные профиль/товар, лишние поля, дубликаты и недопустимые количества отклоняются до запуска с понятным контекстом. Неудачная загрузка не заменяет существующий мир.
- AC-04: профильный ассортимент содержит только профильные позиции плюс explicit overrides. Явные quantity (включая 0) и credits приоритетны; без профиля legacy fallback неизменен. Default/Docked и их обязательные остатки сохраняются.
- AC-05: один demo-сценарий содержит все пять явно расположенных станций. Для каждой через публичную границу сессии проверяются локальный snapshot, Buy/Sell по 1 единице и Refuel. Доступные ресурсы достаточны; отсутствующая позиция не торгуется. Используется существующий Station → Trade.
- AC-06: русский и английский UI показывают Sci-Fi названия 12 позиций и различают кг, рацион, ячейку и блок. Количество не смешивается с массой, Fuel остаётся отдельным Refuel.
- AC-07: минимальное сохранение профильных данных не сбрасывает ассортимент/деньги/stock после сделки. Несовместимые catalog/profile fingerprints отклоняются; legacy сценарии без профиля работают. Полная persistence экономики относится к US-0012.

## Non-goals

Почасовые flows, enforcement target/maxStock и восстановление budget (US-0002); динамические цены/спред/curve (US-0015/0003); генерация карты/полей (US-0004/0005); Undock/рейсы (US-0014/0006), события, ledger, remote knowledge и полная экономика Save/Load. Нет новых экранов, рецептов, картинок и performance-задач. Supply/demand описывает специализацию, а не запрещает обратное направление Buy/Sell внутри ассортимента.

## Dependencies

Нет внешних story dependencies. Для сравнения рынков используются независимые старты демонстрации, а не ещё не реализованный повторяемый рейс.

## Grounding

Проверено чтением исходников 2026-09-21; runtime и тесты не запускались.

| Факт / ограничение | Evidence |
|---|---|
| Strict JSON и semantic validation до сессии; конфигурация immutable | Documentation/01-Requirements/EngineRequirements.md:224–261 |
| Engine authoritative, граница с клиентом через Contracts/LocalClient | Documentation/00-Process/CLAUDE.md:22–56 |
| Единицы Kilogram/Piece/Ration/EnergyCell, storage и шаг 1 уже валидируются | src/DeepSpaceSaga.Engine/Content/ItemTypeDefinition.cs:17–19; src/DeepSpaceSaga.Engine/Content/ItemCatalogValidation.cs:5–38 |
| Сейчас 6 ресурсов + 6 товаров, electronics отсутствует; uranium нужен старому сценарию | src/DeepSpaceSaga.Client/Data/Items/Resource/items-resource.json:2–74; src/DeepSpaceSaga.Client/Data/Items/Good/items-good.json:2–73; src/DeepSpaceSaga.Client/Scenarios/Docked/scenario.json:165 |
| Economic fingerprint не включает displayName; legacy save требует точного baseline | src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs:31–36; src/DeepSpaceSaga.Engine/SimulationEngine.cs:196–204 |
| Тест привязан к v7 и равенству реального каталога legacy baseline | tests/DeepSpaceSaga.Engine.Tests/CatalogCompatibilityTests.cs:25–40, 58–72 |
| Registry загружается до session; TypeDataPaths пока без профилей | src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs:73–108, 540–549 |
| Inventory охватывает каждый торгуемый item, незаданные quantities случайны 20..500 | src/DeepSpaceSaga.Engine/SimulationEngine.cs:1220–1259 |
| Локальный snapshot строится только из inventory пристыкованной станции | src/DeepSpaceSaga.Engine/SimulationEngine.cs:549–586 |
| Buy/Sell/Refuel уже меняют склад и деньги; отсутствующая inventory-позиция отклоняется | src/DeepSpaceSaga.Engine/SimulationEngine.cs:1927–2048 |
| Имена Trade локализованы через ID, electronics попадает в fallback; quantity label общий | src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeItemPresentation.cs:4–18; TradeScreen.Render.cs:110–159 (та же папка) |
| Data и scenario.json копируются рекурсивно | src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj:258–271 |
| Dock отклоняется для уже пристыкованного корабля; range 200 км | src/DeepSpaceSaga.Engine/SimulationEngine.cs:1774; src/DeepSpaceSaga.Client/Data/Commands/NavigationComputer/commands.json:12 |

## Invariants

- Только Engine меняет экономику (Documentation/00-Process/CLAUDE.md:22–56).
- Fixed-point/decimal и AwayFromZero, без float/double для денег (EngineRequirements.md:5253–5265 в Documentation/01-Requirements).
- Старые TypeId, включая uranium, сохраняются; uranium исключён из новых профилей (эпик :92; Docked/scenario.json:165).
- Default/Docked Large и explicit stocks не меняются (EngineRequirements.md:5315–5317).
- FuelTank, отсутствие обратной продажи Fuel и шаг 1 сохраняются (EngineRequirements.md:5128–5160).
- Не изменяются producingModules, формула цены, время и движение (EngineRequirements.md:5227, 5243–5265).
- Валидация выполняется до замены мира и не обходит catalog identity (SimulationEngine.cs:196–204, 230–232).

## Assumptions

- A-01: 12 позиций — базовый MVP-ассортимент; после добавления electronics каталог содержит 13 определений с legacy uranium.
- A-02: профиль — bootstrap. Supply/demand не создаёт producingModules и не запрещает Buy/Sell для своих позиций.
- A-03: полные списки берутся из таблицы эпика :112–116, в том числе Industrial water/rations. Rates здесь не вводятся.
- A-04: explicit quantities накладываются поверх профиля, включая дополнительные позиции и 0; вне объединения профиля/overrides RNG-позиции не создаются.
- A-05: исходные stocks — tuning: supply 1.5× baseline target, demand 0.5× target, Transit 1× target. Целые количества и Fuel закреплены TK-0004; баланс проверит US-0013.
- A-06: demo начинает корабль не пристыкованным в range всех станций. Для следующего рынка повторяется New Game; Undock не требуется.
- A-07: минимальное сохранение profile ID/fingerprint и resolved stocks предотвращает регрессию новой функции; полная экономика остаётся US-0012.
- A-08: план принят автоматическим workflow StoryBuilder, assumptions не выдаются за явные решения пользователя.

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| EP-0001-US-0001-TK-0001-market-profile-schema | Загрузка и валидация профилей рынка | engine | none | AC-01, AC-02, AC-03 |
| EP-0001-US-0001-TK-0002-profile-market-bootstrap | Профильный рынок и безопасное восстановление | engine | TK-0001 | AC-03, AC-04, AC-05, AC-07 |
| EP-0001-US-0001-TK-0003-market-catalog-content | Базовый каталог и локализованные единицы | content-data | TK-0002 | AC-01, AC-06, AC-07 |
| EP-0001-US-0001-TK-0004-five-market-demo | Пять профилей и демонстрационный сценарий | content-data | TK-0001, TK-0002, TK-0003 | AC-02, AC-04, AC-05 |
| EP-0001-US-0001-TK-0005-profile-trade-presentation | Названия и единицы в существующем Trade | client | TK-0003, TK-0004 | AC-05, AC-06 |

Dependency order: TK-0001 → TK-0002 → TK-0003 → TK-0004 → TK-0005.
Implementation files: 5 / 5 / 5 / 4 / 3. Каждый тикет содержит matching test project. План принят автоматически; approved означает готовность planning, не реализацию.

## Gaps and backlog

- G-01 разрешён A-01: uranium сохраняется ради старого контента; расширение изотопной экономики — backlog.
- G-02: tuning проверит US-0013. В baseline эпика нет производителя silicon; здесь есть конечный стартовый запас, устойчивое снабжение — US-0002/0013.
- G-03 закрывается TK-0002/0003: electronics меняет fingerprint. Нельзя подменять legacy stamp новым хешем; старые несовместимые saves отклоняются.
- G-04: миграции, graph/events/ledger и полный config identity — US-0012. Здесь только identity используемого профиля и текущего каталога.
- G-05: requirements-engineer отсутствует среди доступных и найденных локальных skills. Использован явный workflow StoryBuilder.
- G-06: эпик сохраняет историческое ожидание approval; текущий workflow StoryBuilder разрешает автоматическое принятие плана. Epic metadata и другие истории не изменяются.
- G-08 (закрывается TK-0001/0003): ItemCatalogTests.cs:193–239 фиксирует старые count/names; Engine сохраняет проверки legacy ID/economics, а точный shipping assortment и displayName проверяет matching Client content test.
- G-07: build/tests/gameplay не запускались в planning-задаче; команды тикетов предназначены для implementer.
- Блокирующих вопросов и конфликтов идентификаторов нет.

## Decision and review log

| UTC | Event | Result |
|---|---|---|
| 2026-09-21T08:33:04Z | Точное сообщение пользователя в source_request. | D-01: создать тикеты выбранной истории; production work не запрошен. |
| 2026-09-21T08:33:04Z | Прочитаны обязательные источники, эпик, исходная история, релевантный код и тесты. Folder/file/frontmatter IDs совпадают; ticket-папок нет. | Первый свободный номер 0001; assumptions/gaps записаны. |
| 2026-09-21T08:33:04Z | План пяти тикетов принят автоматически, отдельного review-ответа пользователя нет. | plan-review → создание по dependency order. |

| 2026-09-21T08:33:04Z | Создан TK-0001, 4 файла, engine. | approved planning; следующий TK-0002. |
| 2026-09-21T08:41:20.8368950Z | Создан TK-0002, 5 файлов, engine. | approved planning; следующий TK-0003. |
| 2026-09-21T08:43:08.8088801Z | Создан TK-0003, 5 файлов, content-data. | approved planning; следующий TK-0004. |
| 2026-09-21T08:47:18.3133143Z | Создан TK-0004, 4 файла, content-data. | approved planning; следующий TK-0005. |
| 2026-09-21T08:49:02.3372129Z | Создан TK-0005, 3 файла, client. | approved planning; проверка всех артефактов. |
| 2026-09-21T08:50:14.0414654Z | Проверка выявила ItemCatalogTests.cs:193–239. TK-0001 revision 2 включает этот пятый файл и AC-01; карта обновлена. | Все тикеты остаются в лимите 5 файлов; scope истории не расширен. |

| 2026-09-21T08:51:19.8788212Z | TK-0002 revision 2 использует существующий StationEconomyGenerationTests.cs для профильных тестов и исправляет fixed list :74–98; TK-0003 сохраняет порядок старых items. | Скрытой работы за allowlist нет; лимиты 5/5/5/4/3. |
| 2026-09-21T08:52:27.4882601Z | Структурная проверка завершена: 5 одноимённых папок/файлов, metadata IDs, 11 обязательных ticket sections, layer/matching tests, counts 5/5/5/4/3, все AC-01…AC-07, acyclic dependencies и отсутствие trailing whitespace. | complete: planning artifacts готовы. Production/build/gameplay не выполнялись. |

