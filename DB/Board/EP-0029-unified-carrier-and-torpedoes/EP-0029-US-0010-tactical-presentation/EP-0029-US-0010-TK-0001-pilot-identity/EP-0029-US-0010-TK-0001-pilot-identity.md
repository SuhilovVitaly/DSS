---
epic: EP-0029-unified-carrier-and-torpedoes
story: EP-0029-US-0010-tactical-presentation
ticket: EP-0029-US-0010-TK-0001-pilot-identity
title: Портреты и имена из общей записи экипажа
stage: approved
approval_kind: workflow-only
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0029-US-0005-TK-0003-owned-entry, EP-0029-US-0006-TK-0003-post-battle-state, EP-0029-US-0009-TK-0004-ship-localization]
files_touched: 4
serves: [AC-01]
created: 2026-11-14T19:42:24Z
revision: 1
implementation_status: not-started
---

# Портреты и имена из общей записи экипажа

## Why

Игрок узнаёт свой корабль и экипаж в тактическом бою. Карточки показывают текущий боезапас, журнал объясняет пропуски, итог показывает действительные потери. Этот тикет обеспечивает **Портреты и имена из общей записи экипажа**. Источники: [история](../EP-0029-US-0010-tactical-presentation.md), [полный контракт EP-0029](../../Documentation.md).

## Decisions

Пользователь утвердил концепцию с уточнениями: «два модуля жилых каждый на 4 места», «Учебный бой настоящий», light6/heavy12/cargo42. Последнее точное сообщение: «Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.». Время записи 2026-11-14T19:42:24Z; исходное время отправки не установлено. Planning не разрешает production implementation или публикацию.

## Assumptions

Применять A-01..A-06 эпика в пределах тикета. Новые filenames/APIs проектируются. Стартовое крыло имеет36 торпед; cargo42 отдельно. Другие боевые правила сохраняются кроме явно описанных ammo/mass/stalemate изменений.

## Code context

Исчерпывающая allowlist implementation+tests (максимум5), пути относительно D:/DeepSpaceSaga/DSS. Один production layer: **client**. Read-only зависимости читать можно; менять другие layers запрещено.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotPortraitCatalog.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotPortraitCatalog.cs) | `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotPortraitCatalog.cs:1` — существующий owning file; сверить сигнатуру с Grounding перед изменением. | Только «Портреты и имена из общей записи экипажа» по шагам ниже, без соседних историй. |
| [src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardModel.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardModel.cs) | `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardModel.cs:1` — существующий owning file; сверить сигнатуру с Grounding перед изменением. | Только «Портреты и имена из общей записи экипажа» по шагам ниже, без соседних историй. |
| [src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs) | `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs:1` — существующий owning file; сверить сигнатуру с Grounding перед изменением. | Только «Портреты и имена из общей записи экипажа» по шагам ниже, без соседних историй. |
| [tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0010Tk0001Tests.cs](../../../../../tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0010Tk0001Tests.cs) | Новый проектируемый файл; до выполнения отсутствует. | Named tests Ep0029Us0010Tk0001Tests ниже с независимым oracle. |

## Public API after the change

No API change; uses US-0002 DTO.

Новые поля/сигнатуры проектируемые. Существующие numeric enum identities не меняются; новые значения append. Default/nullable payload не выдаёт имущество.

## Implementation steps

1. Прочитать [контракт эпика](../../Documentation.md) и prerequisites; проверить реализацию их API, не только наличие файлов.
2. Owned mode берёт portrait/female/name из TacticalBuildSnapshot crew fields; legacy pilot alias fallback только для diagnostic payload. Cache key asset+crew identity, декодирование до render. Проверить грамматику реплик и no duplicate portraits. Отсутствие PNG даёт заглушку, без смены пола или имени.
3. Реализовать named tests на owning слое. Численные ожидания вычислять независимо от проверяемого helper.
4. Проверить часть критериев AC-01 по этому тикету; общий критерий может требовать нескольких тикетов карты.
5. Записать actual commands/results/evidence и открытые native/product gates здесь, не выдавать план за тест.

## Dependencies

- [EP-0029-US-0005-TK-0003-owned-entry](../../EP-0029-US-0005-world-owned-sortie/EP-0029-US-0005-TK-0003-owned-entry/EP-0029-US-0005-TK-0003-owned-entry.md) — завершить до этого тикета.
- [EP-0029-US-0006-TK-0003-post-battle-state](../../EP-0029-US-0006-battle-consequences/EP-0029-US-0006-TK-0003-post-battle-state/EP-0029-US-0006-TK-0003-post-battle-state.md) — завершить до этого тикета.
- [EP-0029-US-0009-TK-0004-ship-localization](../../EP-0029-US-0009-ship-management-ui/EP-0029-US-0009-TK-0004-ship-localization/EP-0029-US-0009-TK-0004-ship-localization.md) — завершить до этого тикета.

## Out of scope

Layers кроме client; файлы вне Code context; пленные, найм, верфь, ремонт, новая баллистика, покупки/производство торпед, Ironman и in-battle saves. Запрещены sandbox fallback и бесплатный refill. Snapshot baselines не обновлять без решения Board. Не менять нормативные документы без отдельного разрешения на их синхронизацию.

## Invariants

- Engine авторитетен: EngineRequirements, раздел «Архитектура и авторитет».
- Fire атомарен; точка расхода при candidate до RNG: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs:181` (учёта ammo в исходном коде ещё нет).
- Мир блокируется боем: `SimulationEngine.StartTacticalBattle` / `ThrowIfTacticalBattleActive`. World/tactical координаты различны.
- Новый SKU не удаляется legacy очисткой: `src/DeepSpaceSaga.Engine/Scenario/SaveMigration20.cs:85`.
- Identity не выводится из display name; повтор команды не расходует ресурсы. Потеря дрона не убивает дистанционного пилота.

## Tests

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Новый test class: **Ep0029Us0010Tk0001Tests**.

- `Ep0029Us0010Tk0001Tests.OwnedPortraitOverridesLegacyAlias` — Audrey female вместо yun male.
- `Ep0029Us0010Tk0001Tests.MissingPortraitKeepsIdentity` — имя/пол прежние.
- `Ep0029Us0010Tk0001Tests.SessionSwitchDropsOldPortraitBindings` — другая crew не наследует cache.

Будущие команды из корня DSS; сейчас **не запускались**. Заменить <run> уникальным timestamp, заранее создать абсолютный results directory. На каждый процесс установить process-tree cap300000ms через execution harness. При превышении разделить filter на named methods.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~Ep0029Us0010Tk0001Tests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0029/<run>/US-0010-TK-0001" --logger "trx;LogFileName=focused.trx"
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotPortraitCatalog.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardModel.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs tests/DeepSpaceSaga.Client.Tests/Carrier/Ep0029Us0010Tk0001Tests.cs
```

Stdout/stderr build/format перенаправить в абсолютные файлы того же run. Не писать validation artifacts в репозиторий. Для content также parse JSON и resource links в named tests. Full suite не заменяет focused tests.

Если затронут UI: отдельно native RU/EN,1280×720/1920×1080, мышь/клавиатура, меньший проверенный монитор и DSS_VALIDATION_SMALL_MONITOR=1. Headless этот gate не закрывает.

## Definition of Done

- Шаги выполнены в 4 разрешённых файлах, один production layer.
- Named tests подтверждают часть AC-01; полное покрытие story сверено по карте.
- Focused tests и owning build/format выполнены; исходные падения записаны отдельно.
- API/schema, identity, atomic rejection и остатки ресурсов соответствуют эпику.
- Нет скрытого шестого файла, незаписанных assumptions или неразрешённых blockers.
- Здесь записаны реальные evidence paths и implementation/automated/native/product статусы. Отсутствующее human acceptance остаётся OPEN.

## Self-containment check

Заданы layer, allowlist с test file, API, конкретные действия, named проверки и prerequisites. Общие geometry/roster/численные правила находятся в одной канонической [Documentation.md](../../Documentation.md). Если требуется шестой файл или новая семантика, сначала обновить Board тикет; не реализовывать догадку.

## Execution evidence

NOT STARTED. Создание planning-документа не является implementation, build, test или game launch.
