# Сохранения и восстановление

Текущий writer — Save22. Вход сохранения принимает Save22, Save21, Save20, Save19 и Save18; иные версии отвергаются. Файл нового сценария без версии сохранения — отдельный вход New Game.

Save22 сохраняет мир: материализованные объекты, орбиты и карту, календарное и физическое время, seed и курсоры мира, очереди/receipts, модульные циклы, экипаж и структуру модулей, рынки, грузы и рейсы, а также параметры отладки двигателей. Загрузка не генерирует мир заново. Идентичность экономического каталога проверяется перед заменой сессии.

Версия географии не зависит от номера формата сохранения. Поддержаны solar generatorVersion и
cluster rulesVersion 1/2; при наличии обоих они совпадают. Для v2 обязательны
материализованные кластеры и `geography`: frozen referenceSpeedKmS,
minimumStationDays, neighbourMinDays/neighbourMaxDays. Для v1 descriptor отсутствует
или null; non-null v2 descriptor отвергается. Неизвестные/смешанные версии,
неполные descriptors, неверная геометрия/членство/ссылки отклоняются до замены мира.
[Контракт географии](TradingAndSolarSystem.md).

Load восстанавливает позы, орбиты, graph/resource bindings и активные рейсы.
Новые generation settings и текущая Vmax не применяются к материализованной
географии; старые v1 сохранения не переходят на новое размещение автоматически.

Save18 и Save19 проходят проверку исторических секций и преобразуются в Save20 в памяти. Старый боевой код, снаряды, боевые модули, приказы и данные старой брони не загружаются в симуляцию. Миграция сохраняет небоевое состояние и исходный отпечаток каталога; исходный файл не переписывается. Save18 с полями, введёнными в Save19, отклоняется. Save20 отклоняет старые боевые секции и поля, а также неизвестные поля текущей схемы.

[Тактический бой](TacticalBattle.md), его инициатива, tie rank, сознание, EV/D приказы, заряды, аномалии, RNG, FireSteps и Slots не сохраняются. Save22 сохраняет последствия собственного вылета через CommitSortieOutcome: корпус носителя, повреждения и боезапас уцелевших дронов, tombstone уничтоженных дронов и последний settlement. Пока бой активен, Engine запрещает запись, в том числе когда уже показан итог; выход возвращает обычную возможность записи. В обычной игре F5 записывает быстрый слот, F9 загружает его на паузе. LocalClient после принятого выхода из собственного вылета пишет continuation checkpoint; при ошибке повторяет запись без повторного settlement. Неуспешная загрузка оставляет текущую сессию. Имена слотов определены в [SaveSlotNaming](../../../src/DeepSpaceSaga.Engine.LocalClient/SaveSlotNaming.cs). Отдельный файл F12 описан в [DebugTuning](DebugTuning.md).

Владельцы: [ScenarioData](../../../src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs), [ScenarioLoader](../../../src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs), [LegacyCombatSaveReader](../../../src/DeepSpaceSaga.Engine/Scenario/LegacyCombatSaveReader.cs), [SaveMigration20](../../../src/DeepSpaceSaga.Engine/Scenario/SaveMigration20.cs).

Save20 после проверки world-only преобразуется в Save21 через [SaveMigration21](../../../src/DeepSpaceSaga.Engine/Scenario/SaveMigration21.cs). Отсутствующая поверхность носителя остаётся отсутствующей: миграция не выдаёт стартовое имущество. Starter manifest материализуется только на входе New Game и удаляется из metadata до сохранения.

Save21 преобразуется в Save22 в [ScenarioLoader](../../../src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs): добавляется пустая campaign-секция без выдачи имущества и ретроактивной гибели пилотов. Save22 хранит `DemoCampaignData`: schema1, revision/commit sequence, receipts, отпечаток демо-каталогов, мемориал и следующий crew sequence. Пилот мемориала не может одновременно быть живым или назначенным на дрон. Несовместимые ссылки/счётчики/отпечатки отвергаются до замены сессии.

Автосохранение демо после принятого commit/docking использует campaign-continuation, отдельный от quicksave F5/F9 и атомарную temp+replace запись; ручные слоты не затрагиваются. «Сохранено» появляется после завершения записи, F6 повторяет неудачную запись без повторения игрового действия. Continue выбирает самый свежий полностью валидный нетерминальный слот, пропуская повреждённые и терминальные. Перед обычным собственным вылетом сохраняется живой continuation; гибель носителя не перезаписывает его. Captured encounter имеет обязательную замену pending на finished, включая терминальный исход. Load сам по себе не пишет autosave. Подробный контракт — [DemoCampaign](DemoCampaign.md).

## Пиратский мир и durable encounter

Optional `PirateWorldData` version2 сохраняет четыре группы, frozen Vmax, рабочие
модули носителей, патрульный индекс, фазу, private last observed player/time,
motion plan и отдельно наблюдение игрока с позицией/скоростью/временем/телеметрией.
Composition материализуется один раз; reports, approximate count, consumed flag
и pending batch не перебрасываются при Load. Финитность, идентичности, временные
границы и safe corridor валидируются до замены мира. Отсутствующий раздел старого
слота не вызывает spawn. Process-only fog/chance overrides в save не входят.

`WorldEncounterData` связывает группу, encounter/battle IDs, seed и captured,
pending_battle или finished фазу. Перед стартом боя отдельный pending checkpoint
записывается атомарно из полного prebattle мира. Он не содержит ходов тактической
сессии: Load собирает тот же бой с начала. Обычный active-battle save запрещён.
`EncounterResults` хранит actual outcome/units и retired участвовавшей группы,
атомарно с собственным settlement. При ошибке finished write gameplay заблокирован,
повтор только пишет готовый результат; crash оставляет старый pending либо новый
finished. Непрерывная запись каждого боевого хода не обещается.

В `VoyageStateData.ThreatRoute` сохраняются выбранные условия/точки, исходная
позиция, следующий waypoint и приостановка ручным управлением. Топливо остаётся
в существующем fuel reservation ledger; повтор выбора/загрузки не резервирует
его второй раз. [Механика](../02-FirstRelease/Mechanics/DemoEncounters.md).

## Услуги компании

Save22 сохраняет optional service-секции кампании: использованные предложения каждой станции, hired offer IDs, условия employment с captured daily wage и временем найма, payroll accrued/paid/debt и last processed day, счётчик новых дронов, service postings и committed command hashes. Отсутствующие секции совместимых демо-слотов инициализируются без выдачи имущества и ретроактивной зарплаты. `OwnedDroneData.replacesDroneId` связывает замену с погибшим исходным дроном того же места; tombstone сохраняется. Котировки ремонта/ловушек живут только в runtime и после Load требуют повторного запроса. Валидация использует календарное время загружаемого мира до замены сессии. Версия writer остаётся Save22. [Текущие правила](CompanyServices.md).
