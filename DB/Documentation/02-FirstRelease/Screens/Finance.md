# Финансы

Читает подтверждённый баланс, cargo basis и историю проводок/рейсов. Разделяет прибыль, выручку и расходы; неизвестная себестоимость остаётся неизвестной. Для реальной котировки нужно открыть Trade текущей станции.

Отчёт рейса показывает выплаты пассажирских и грузовых контрактов демо в строке «Выплаты по контрактам» и штрафы отмены/просрочки в строке «Штрафы по контрактам» (EN: Contract payouts / Contract penalties; ключи `Finance.PassengerPayout`/`Finance.PassengerPenalty` сохранены). Долг по контрактам показывает [Contracts](Contracts.md). [Перевозки](../Mechanics/DemoTransportContracts.md).

См. [каталог экранов](ScreenCatalog.md) и [основные требования](../../01-Requirements/EngineRequirements.md).

Источники: [FinanceScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/Finance/FinanceScreen.cs).
