# Экипаж и жилые модули

Персонажи и боевые операторы имеют identity и skills; роль оператора влияет
на соответствующий боевой расчёт. Жилые модули и cabin capacity существуют
в content. Диалог использует стабильный портрет и identity говорящего.
Принятые планы содержания экипажа, пассажиров и расширенной коммуникации
не следует выводить из наличия DTO или кнопки. Hire и пассажирские Contracts
сейчас показывают not available yet; полноценные системы найма и контрактов
не завершены. Жилой модуль не делает эти действия доступными автоматически.

Источники: [modules-livingquarters.json](../../../../src/DeepSpaceSaga.Client/Data/Modules/LivingQuarters/modules-livingquarters.json), [HireScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/Hire/HireScreen.cs), [ContractsScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/Contracts/ContractsScreen.cs).

Принятые требования к будущему завершению этих систем: люди размещаются в
Living quarters по cabin capacity; пассажирский контракт принимается только
при свободной каюте, создаёт реального пассажира и authoritative условия/выплату,
а active contracts и размещение сохраняются. Пассажиры учитываются в
жизнеобеспечении. Коммуникация с персонажем использует общий диалог и не
является отдельным уже завершённым экраном. Эти требования не меняют текущий
placeholder-статус Hire/Contracts.
