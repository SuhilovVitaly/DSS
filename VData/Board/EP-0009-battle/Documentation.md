# EP-0009 canonical documentation

> **Заменено для боя реального времени (2026-11-06).** Действующий контракт — [тактический бой V4](../../Documentation/04-Engineering/TacticalBattle.md), состояние поставки — [EP-0015](../EP-0015-tactical-carrier-battle/ExecutionStatus.md). Эта запись относится к снятому бою и его боевым панелям, настройкам и результатам. Небоевые модули, ручное управление и настройка двигателей описаны в текущей документации. Предыдущие решения и результаты ниже сохранены как история.


[Revision2 specification](EP-0009-module-combat.md) contains all gameplay decisions and acceptance criteria. [Execution decisions](ExecutionDecisions.md) records current baseline, technical assumptions and user overrides. [Tickets](Tickets.md):12 standard stories/38 tickets; final US-0012 owns project documentation and link audit. Graphify rebuilding is explicitly skipped by user instruction.

[Current project contract](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/01-Requirements/EngineRequirements.md#phase-four-module-combat), [module-combat engineering guide](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/04-Engineering/ModuleCombat.md) and [document impact inventory](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/04-Engineering/ModuleCombatDocumentation.md) reflect the implemented EP-0009 rules and Save17. [Engine balance](evidence/balance.md) found a documented Mukha mismatch; [native matrix](evidence/native.md) is NOT RUN by user instruction. Publication and self-review evidence is in [ImplementationStatus](ImplementationStatus.md) and [PublicationStatus](PublicationStatus.md). These are implementation records, not independent/user acceptance.

The [ticket-to-commit ledger](CommitLedger.md) maps all 38 tickets to full pushed SHAs and records the post-review repair separately.

## Продолжение2026-10-16: EP-0010

[Боевые доклады экипажа](../../Documentation/04-Engineering/CombatReports.md) реализованы поверх EP9; optional child Version1 не меняет root Save17. Собственные balance/native gates EP9 этим не закрываются. [Текущий статус EP10](../EP-0010-combat-reports/ImplementationStatus.md).
