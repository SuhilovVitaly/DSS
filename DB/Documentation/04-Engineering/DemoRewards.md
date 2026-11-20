# Боевые награды: авторитет, сохранение и продолжение

Engine владеет исходом, RNG, cargo, людьми, деньгами и ledger. Client читает `DemoCampaignSnapshot.Rewards` и отправляет типизированные команды через `ExecuteDemoCampaignAsync`. `CommandId` и `ExpectedRevision` защищают действие от повторного применения; сохранённые receipt/hash различают повтор и конфликт identity. В сохранённых receipts нет производного Rewards: ledger сохраняется один раз. Повтор после загрузки возвращает исходный результат команды с текущими Rewards и revision, без повторного применения. Никакого второго баланса или inventory нет.

## Заказы и выплаты

`AcceptBountyCommand`/`CancelBountyCommand` меняют только Tracking. `ClaimBountyCommand` требует подтверждённого собственного `EncounterResults.PlayerWon`, точной группы и стыковки со станцией-заказчиком. Оплата атомарно меняет существующие PlayerCredits и добавляет один posting `bounty:<id>` в BountyPayments. Overflow отклоняется до изменения. BountyPaymentSnapshot отделяет DueTokens от PaidTokens; порядок принятия/отмены не отменяет earned entitlement. SearchArea содержит центр региона и радиус покрытия patrol, не позицию NPC; появляется только для tracking доступной цели.

## Добыча и люди

`BattleLootData` содержит BattleId, фазу Open/Closed, fingerprint каталога, seed/draw count, замороженное поле и источники. Источник фиксирует ObjectId/layout/pose, исходного пилота, OriginalQuantity/Remaining расходников, исход проверки сохранности, экземпляры модулей и конечные состояния попыток. Именованный поток `battle-loot/v1/<battleId>` независим от боя. Шансы хранятся в basis points.

`OpenBattleResultsCommand` переводит подтверждённый итог из AwaitingOpen в Open. `AcknowledgeBattleResultsCommand` завершает результаты и требует ConfirmDiscard при доступных остатках или живых неарестованных пилотах. Мир разблокируется после подтверждённой записи. Закрытый ledger остаётся аудитом оставленного.

`StartLootOperationsCommand` адресует одну строку или все доступные материальные строки. Engine фиксирует монотонные сроки 3, 5 и 10 секунд без резервирования груза. `AdvanceLootOperationsCommand` обрабатывает следующую истёкшую строку по сроку и индексу замороженного списка. Client периодически запрашивает обработку срока из активной модали. Engine проверяет срок и применяет результат под мировым lock. Сворачивание и прокрутка не участвуют в RNG или порядке начисления.

Расходники помещаются целыми единицами с учётом ёмкости каждой исправной секции и резерва контрактов. Лом и модули используют по одному броску. Успех без места сохраняется как ExtractedWaiting. `ClaimExtractedLootCommand` проверяет вместимость и подбирает готовый трофей без RNG. Salvaged товар получает нулевые acquisition costs в новой партии. Неизвестный cost basis при смешении сохраняется. `ArrestPilotCommand` остаётся отдельным ручным действием.

Prisoners — отдельный массив кампании. SourcePilotId включает battle/drone/pilot; Load проверяет provenance против frozen victory, уникальность, корабль, имя, портрет, пол и время захвата. Cabin availability учитывает crew, фактических пассажиров, пленных и ранее обещанные legacy-посадки. ReservedPrisonerCabins writer равен нулю, прежнее число не материализует людей. Пленные участвуют в календарном расходе пищи, но не payroll. Engineer role хранит SalvageBonusPercentagePoints (10–15, у остальных 0), выбранный один раз `engineer-salvage/v1/<ship>/<crew>`; Load не выдаёт нового сотрудника.

## Save22 и границы атомарности

Writer остаётся Save22. Optional bounty/loot/prisoner fields отсутствуют в старых слотах без выдачи трофеев или людей. EncounterResults хранит PlayerSide; отсутствующее поле старого слота означает A. Проверки победы, вражеского состава и provenance пленных используют эту сторону. Stable cluster fingerprint не меняется; bounty и loot имеют собственные отпечатки. Additive economy compatibility принимает только известные прежние наборы SKU/profile hashes, новые товары получают нулевой stock; произвольный drift каталога отвергается. Контент с encounters обязан содержать loot.json; отсутствие отклоняется при настройке, до боя.

Завершение собственного боя выполняет единый CommitSortieOutcome и убирает активную tactical session. `BattleResultsData` сохраняет BattleId и стадию AwaitingOpen или Open. При наличии добычи карта берётся из соответствующего ledger. В остальных исходах сохраняется замороженный исход с картой. Load проверяет связь с settlement и историей встречи. Открытый ledger проверяется по текущему fingerprint, компоновке и источникам. Закрытый архив совместим со следующей встречей и изменениями каталога. Несовместимые состояния отклоняются до замены мира.

LocalClient сериализует стабильные сохранения, завершение боя и команды добычи через checkpoint gate. Продвижение пачки может быть видно в текущей сессии, пока ещё работают другие таймеры. Запись пачки выполняется после последней операции. Ручной Save, autosave, Load и выход недоступны во время работы или ожидания записи. При IOException клиент получает Unavailable. Повтор той же identity сохраняет применённый результат с прежним RNG cursor. Текущий checkpoint переживает сбой, незавершённые операции остаются runtime-данными.

`RecoverBattleFlowAsync` восстанавливает ожидающую открытия тактическую панель или открытую модаль. Старый Open loot открывается в той же модали со своим ledger. Адаптация стадии при Load выполняется в памяти без записи continuation. Pending encounter возобновляется с предбоевого checkpoint. После утраты учебного носителя сохраняется прежнее правило последнего живого continuation. `RewardReportPresentation` связывает результат и loot по BattleId.

## Отладка

Process-only `SetLootDebugRequest` обслуживает `engineer_bonus`, `loot_force` и чтение `loot_state`. Override не меняет persisted навык, не пересчитывает существующие результаты и не сохраняется как настройка. Каждый новый бросок всё равно продвигает draw count. Engine валидирует значения, а Client запрещает изменение во время pending команды/залпа. [Синтаксис консоли](DebugConsole.md).

Владельцы: [BattleLootData](../../../src/DeepSpaceSaga.Engine/Scenario/BattleLootData.cs), [lifecycle](../../../src/DeepSpaceSaga.Engine/SimulationEngine.BattleLootLifecycle.cs), [loot](../../../src/DeepSpaceSaga.Engine/SimulationEngine.BattleLoot.cs), [prisoners](../../../src/DeepSpaceSaga.Engine/SimulationEngine.Prisoners.cs), [контракты](../../../src/DeepSpaceSaga.Contracts/BattleLootContracts.cs). [Игровой цикл](../02-FirstRelease/Mechanics/DemoBountiesAndLoot.md).
