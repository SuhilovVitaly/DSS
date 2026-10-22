# Валюта и финансы

Пользовательское название валюты — Tokens. Внутренний баланс называется
PlayerCredits, поле сценария/сохранения — playerTokens. Не добавляй вторую
валюту из-за разных внутренних имён. Начальный баланс задаёт сценарий.
Finance читает authoritative snapshot/ledger: торговлю, себестоимость, топливо,
сборы и прочие проводки. Финансовая сводка не является источником новой quote.
Отсутствие basis/provenance означает неизвестный итог, а не прибыль по нулевой цене.

Источники: [ScenarioData.cs](../../../src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs), [FinanceScreen.cs](../../../src/DeepSpaceSaga.Client/UI/Screens/Finance/FinanceScreen.cs).
