# Экипаж и жилые модули

Пилоты дронов временного [тактического боя](../../04-Engineering/TacticalBattle.md) имеют самостоятельную инициативу, фиксируемую при старте боя; это не система найма или развития экипажа мира.

Уничтожение собственного дрона переносит пилота в постоянный мемориал при settlement, освобождает его место и снимает назначение. Disabled оставляет пилота живым. Save/Load сохраняет потери без повторного применения. [Контракт кампании](../../04-Engineering/DemoCampaign.md).

Персонажи и боевые операторы имеют identity и skills; роль оператора влияет
на соответствующий боевой расчёт. Диалог использует стабильный портрет и
identity говорящего. Hire сейчас показывает not available yet; полноценный найм
и содержание экипажа не завершены. Коммуникация с персонажем использует общий
диалог и не является отдельным уже завершённым экраном.

Люди размещаются в исправных Living quarters по cabin capacity. Демо-носитель
стартует с дополнительным жилым модулем: шесть членов экипажа, два резервных
места и четыре свободные каюты. Engine на каждом snapshot делит каюты на экипаж,
пассажиров, резерв и свободные и сообщает причину, если новых людей нельзя
принять: нет свободных кают, каюты переполнены (повреждённые модули) или пайков
не хватает на сутки всем на борту и каждой свободной каюте. Ship показывает это
разбиение. Пассажирский контракт принимается только при свободной каюте и
сразу сажает реальных пассажиров, которые занимают каюты и потребляют пайки
наравне с экипажем; условия, выплата и размещение сохраняются. Правила —
[перевозки](DemoTransportContracts.md).

Источники: [modules-livingquarters.json](../../../../src/DeepSpaceSaga.Client/Data/Modules/LivingQuarters/modules-livingquarters.json), [demo-starter-carrier.json](../../../../src/DeepSpaceSaga.Client/Data/Ships/demo-starter-carrier.json), [SimulationEngine.PassengerCapacity.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.PassengerCapacity.cs), [HireScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/Hire/HireScreen.cs), [ContractsScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/Contracts/ContractsScreen.cs).
