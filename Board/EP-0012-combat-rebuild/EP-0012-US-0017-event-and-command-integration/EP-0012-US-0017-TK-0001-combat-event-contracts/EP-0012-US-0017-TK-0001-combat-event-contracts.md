---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0017-event-and-command-integration"
ticket: "EP-0012-US-0017-TK-0001-combat-event-contracts"
title: "combat-event-contracts"
stage: "implemented"
layer: "contracts-and-localization"
depends_on: ["EP-0012-US-0001-TK-0005-launcher-profile-schema"]
files_touched: 6
serves: ["US17-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0017-TK-0001-combat-event-contracts

## Why

[Контракты событий, серверные доклады и доставка](../EP-0012-US-0017-event-and-command-integration.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: combat-event-contracts; доказать US17-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Актуальный allowlist 6 файлов: исходные Contracts + matching tests и два locale consumer файла. 2026-10-20: полный Client run после расширения enum воспроизвёл missing Combat.Event.TorpedoDetonated (2128 passed / 1 failed). Scope расширен до RU/EN event labels до исправления; существующий PhaseThreeLocaleTests не ослабляется. Отдельный fix commit привязан к этому опубликованному тикету.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Contracts/CombatJournalEntry.cs](../../../../src/DeepSpaceSaga.Contracts/CombatJournalEntry.cs) | Существует: src/DeepSpaceSaga.Contracts/CombatJournalEntry.cs:3; public enum CombatEventType | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Contracts/BattleReportSnapshot.cs](../../../../src/DeepSpaceSaga.Contracts/BattleReportSnapshot.cs) | Существует: src/DeepSpaceSaga.Contracts/BattleReportSnapshot.cs:7; public enum BattleReportKind | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Contracts/CombatPresentationUpdate.cs](../../../../src/DeepSpaceSaga.Contracts/CombatPresentationUpdate.cs) | Существует: src/DeepSpaceSaga.Contracts/CombatPresentationUpdate.cs:12; public sealed record CombatPresentationUpdate( | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0017TK0001Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |
| [src/DeepSpaceSaga.Client/Data/Locale/English.json](../../../../src/DeepSpaceSaga.Client/Data/Locale/English.json) | Existing enum-to-label consumer | Four new event names. |
| [src/DeepSpaceSaga.Client/Data/Locale/Russian.json](../../../../src/DeepSpaceSaga.Client/Data/Locale/Russian.json) | Existing enum-to-label consumer | Matching Russian labels. |

## Public API after the change

Proposed CombatEventType добавляет TorpedoDetonated, CountermissileDetonated, ShotDown, ManuallyDetonated; event payload position+projectile IDs+публичные итоги. При own damage можно передавать module детали; enemy damage numbers запрещены.

### Уточнение исполнения — 2026-10-20

Dependency US1-TK5 опубликован: `f06c235c84d5010d8497731b68be549930874b77`, remote SHA проверен. Новые enum values добавляются после прежних без перенумерации. Journal/report получают optional kind/profile/radius и typed Core/Edge/Miss/ShotDown outcome; X/Y и MotionTime — уже случившийся факт, не будущая точка/deadline. Reports сохраняют E01–E07 kind identities, без новых карточек.

Nullable private module/damage/rating поля при отсутствии не сериализуются. Contracts не определяет права наблюдателя и не редактирует мир: named test проверяет roundtrip уже спроецированного публичного envelope и сохранение own damage; настоящая permission masking проверяется в US-0005, producers — US17-TK2. Старый enum/API сохраняется только до предусмотренной consumer migration. Новые факты проходят существующий CombatPresentationFrame.State, отдельный transport не вводится.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Расширить события для TorpedoDetonated и CountermissileDetonated по US3/US4; каждый hit/destroyed элемент имеет object identity и public result. Приватный Engine hit payload не включать в enemy-visible DTO.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: contracts; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

План проверок; фактические результаты исполнения приведены в Status / Evidence.

- **Combat_events_roundtrip_and_redact_enemy_details**: новый event envelope сериализуется; чужие module/damage поля отсутствуют.

Matching project: `tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj`; proposed test class `PointCombatUS0017TK0001Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0017TK0001Tests"
dotnet build tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Contracts/CombatJournalEntry.cs src/DeepSpaceSaga.Contracts/BattleReportSnapshot.cs src/DeepSpaceSaga.Contracts/CombatPresentationUpdate.cs tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0017TK0001Tests.cs
git diff --check
```

## Definition of Done

- Served AC US17-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: Contracts implementation и self-review выполнены. Named tests PASS 6/6; полный Contracts PASS 245/245 (0 failed/skipped); owning build 0 warnings/errors; scoped whitespace/full formatter и diff check PASS; hard cap 300000 ms.

[Команды и stdout](../../evidence/us0017-tk0001-results.json). Self-review: old enum numeric identities/E01–E07 сохранены; точное physical fact time и явные removals проходят existing envelope; public projected journal/report JSON не содержит null private module/damage/chance keys; own damage roundtrip сохраняет детали. Нет future endpoint/deadline новых полей и нет permission/RNG/world logic в Contracts. Это не проверка реальной Engine redaction: US5 и producers/delivery gates OPEN. Review исполнителя не является независимым. Native/product/balance OPEN; SHA проверяется после отдельного commit/push.

2026-10-20 corrective review: новые event enum values теперь покрыты English/Russian source и deployed JSON. Исходный full Client FAIL 2128/2129; после исправления существующий PhaseThreeLocaleTests PASS 3/3, без изменения assertions. [Reproduction/fix evidence](../../evidence/us0017-tk0001-locale-fix.json). Data-only correction; JSON duplicate/nonblank/source-output parity проверены самим тестом, owning compile PASS. Отдельный fix commit/push; полный Client повтор включён в текущий launch gate.
