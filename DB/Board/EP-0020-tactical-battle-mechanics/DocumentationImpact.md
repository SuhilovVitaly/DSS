# EP-0020 — влияние на документацию

Инвентаризация итогового diff относительно 601ab778 выполняется последней историей US13 после 40 функциональных тикетов. Runtime diff затрагивает Contracts/Engine/Motion/LocalClient/Client, JSON/V4 data, тесты и инструменты; документация не является доказательством приёмки.

| Область | Решение |
|---|---|
| EngineRequirements; FirstReleaseRequirements | Обновить действующие EV/D/anomaly требования и границы |
| Engineering/TacticalBattle | Один технический контракт, убрать placeholder сознания и старый skip priority |
| Screens/TacticalBattle; TacticalBattleSetup | E/D, раскрытие, аномалии, build card; подготовка не получает несуществующий selector модуля |
| SaveFormat | Save20 без миграции; перечислить эфемерные новые данные |
| Documentation/README | Обновить навигационные названия |
| CarrierWingSim; tools/tactical-battle/README | Текущие validation filters, Balance/LongRunning и граница исторических V4 oracle |
| VData/Planning/V4.0/Data/README и каталог Decoys/Frames | Сверить экспортируемые параметры и новые ссылки; таблицы уже актуализированы функциональными тикетами |
| VData/Planning/V4.0/README | Отделить историческую одновременную модель от runtime расширений |
| GameSessionScreenUI; TacticalBattleSkins; src/DeepSpaceSaga.Client/Skins/README | Процедурное поле, карточка и действующие роли без новой версии skin manifest |
| tools/DeepSpaceSaga.Performance/README | Native E/D, размеры, отдельные human/FPS gates |
| GraphifyRebuild и tools/graphify/rebuild.py | Добавить EP20 corpus, исключить publication ledgers из циклических hashes; свежие source-hash semantics и offline rebuild последним TK4 |
| Корневой README DSS | Отсутствует; новый дублирующий индекс не создаётся |
| Исторические V4 Spec/Reference, датированные макеты, Board прошлых эпиков | Сохраняются историческими; не переписываются под новые исходы. В EP19 review log записана только разрешённая смена M1–M4 baseline |
| VData/Board и VData/Documentation копии | Неавторитетные копии; задачи и контракты редактируются только в DB/Board и DB/Documentation |
| Остальные architecture/world/economy/crew/map docs, save fixtures README, HexWingSimulator README | Инвентаризированы; ownership мира/экономики/часов/crew и самостоятельный инструмент этим эпиком не меняются |

Физический ввод, human/art/independent review, другие ОС и 80FPS остаются отдельными gates. История запусков находится в ExecutionStatus/FinalValidation; curated Documentation содержит только текущие описания.
