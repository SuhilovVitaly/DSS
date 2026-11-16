---
epic: EP-0032-demo-campaign-continuity
story: EP-0032-US-0007-consistent-campaign-actions
ticket: EP-0032-US-0007-TK-0003-campaign-loader
title: Принятие новой версии сохранения
stage: approved
approval_kind: workflow
implementation_status: not-started
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0032-US-0007-TK-0002-campaign-state]
files_touched: 5
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-16T20:32:46.441Z
revision: 1
---

# Принятие новой версии сохранения

## Why

Согласованные действия и сохранённое состояние кампании. Тикет 3/5 даёт проверяемый результат своего слоя. [Story и полный пользовательский результат](../EP-0032-US-0007-consistent-campaign-actions.md).

## Decisions

Поручение владельца: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Время записи 2026-11-16T20:32:46.441Z, не доказанное время отправки. Разрешены тикеты и необходимые дополнительные истории. Human approval технических деталей/баланса не заявляется; stage approved означает автоматическую готовность workflow планирования. Исполнение и публикация требуют отдельного поручения.

## Assumptions

- Используется сокращённая демо на 60–120 минут, один кластер, готовый Tetrarch; backlog полного продукта не включается.
- Технические имена/новые API ниже — планируемые, не утверждения о текущем коде. Численные A-решения в шагах — baseline для owning плейтеста, не доказанный баланс.
- Сначала prerequisites: EP-0032-US-0007-TK-0002-campaign-state. При drift исходников пересмотреть allowlist в Board до расширения diff.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs` | `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs:11` — public static class ScenarioLoader | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs` | Планируемый файл; создаётся в `EP-0032-US-0007-TK-0002-campaign-state`, не текущая реализация. | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Engine/Content/DemoCampaignDefinitions.cs` | Новый файл этого тикета; на момент grounding отсутствует. | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs` | `src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs:10` — public static class EngineContentLoader | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0032Us0007Tk0003Tests.cs` | Новый файл этого тикета; на момент grounding отсутствует. | Именованные проверки этого тикета; не менять production из тестов. |

Разрешены только эти 5 implementation/documentation files. Board evidence/status этого тикета обновляются отдельно как каноническая история исполнения; не дают разрешения править произвольный production. Read-only архитектурные источники: `DB/Documentation/00-Process/CLAUDE.md`, `DB/Documentation/01-Requirements/EngineRequirements.md` и [общая карта](../../../EP-0041-demo-validation-and-release/TicketPlan.md).

## Public API after the change

No new public API; расширение существующего ScenarioLoader.

## Implementation steps

1. Расширить поддерживаемые версии loader до Save22; для Save21 применять только additive defaults раздела DemoCampaign.
2. До LoadScenario проверять schema, duplicate receipt IDs, монотонность revision/commitSequence и корректность typed sections; не исправлять повреждённые суммы молча. Сохранить существующие миграции поддерживаемых старых версий.
3. Загрузить optional Data/Demo/{cluster,services,jobs,encounters,progression}.json из content root до публикации demo сессии. В DemoCampaignDefinitions задать typed records ровно для полей схем, перечисленных в TicketPlan и owning content tickets: limits/rates/offers/station roles/loadouts/XP. Validate references, nonnegative prices, finite geometry, unique IDs; отсутствие демо-файлов допустимо для non-demo сценария. При demo scenario требуемые definitions обязательны; сохранять catalog fingerprint, не перегружать баланс незаметно при Load.

## Served acceptance criteria

- **AC-01:** Новая команда кампании имеет identity, revision, типизированный результат и причину отказа; повтор с тем же payload возвращает исходный receipt, изменённый payload с той же identity отклоняется.
- **AC-02:** Состояние демо, квитанции и версии сохраняются атомарно; Save21 мигрирует без выдачи нового стартового имущества, неподдерживаемая версия не заменяет сессию.
- **AC-03:** Доставка через LocalClient и GameSessionHandle асинхронна; UI не списывает ресурсы по pending и не обращается к Engine из рендера.
- **AC-04:** Новые обязанности обрабатываются теми же календарными границами при обычном кадре и CaptureSaveState; нет параллельной модели мира.

Вклад этого тикета ограничен слоем `engine`; полный AC принимается после всей карты истории, включая реальный пользовательский путь. DTO/test scaffold сам по себе не закрывает AC.

## Out of scope

Полная верфь, другие носители, фракционная симуляция, пленные, тактическое отступление, сохранение внутри боя, универсальный квестовый движок и публикация. Не переписывать поставленный EP-0029, не ремонтировать несвязанные файлы и не расширять лимит пяти файлов молча.

## Invariants

- `DB/Documentation/01-Requirements/EngineRequirements.md`, «Архитектура и авторитет» / «Команды, события и воспроизводимость»: Engine владеет исходами/временем/RNG/деньгами; render не мутирует мир.
- `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`: асинхронный transport. `src/DeepSpaceSaga.Engine/SimulationEngine.SortieSettlement.cs:17`: использовать единый settlement, не создавать второй inventory.
- `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:40`: на grounding writer Save21; переход к Save22 принадлежит EP-0032-US-0007. Последующие тикеты не назначают конкурирующие версии writer.
- Reject/duplicate не даёт частичных денег, груза, награды, опыта или повторного звука. Pause не двигает deadlines; calendar и motion units не смешиваются.
- Реализация gameplay-инвариантов — в owning engine ticket; клиентский/content/tooling тикет их не дублирует.

## Tests

Будущие тесты (ещё не реализованы/не запущены):

- `DemoEp0032Us0007Tk0003Tests.Save22_RoundTrips` — проверяет шаг 1 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0032Us0007Tk0003Tests.CorruptReceipt_RejectsBeforePublish` — проверяет шаг 2 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0032Us0007Tk0003Tests.ExistingSupportedSaves_StillLoad` — проверяет шаг 3 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.

Запуск из `D:/DeepSpaceSaga/DSS`; создать уникальный run, ограничить **каждый process tree 300000 ms**, не продолжать после timeout/failure. Пример PowerShell для именованного test corpus:

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0032-US-0007-TK-0003-campaign-loader/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Force -Path $run | Out-Null
$p = Start-Process dotnet -ArgumentList @('test','tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj','-c','Release','--filter','FullyQualifiedName~DemoEp0032Us0007Tk0003Tests','--results-directory',($run + '/results'),'--artifacts-path',($run + '/build')) -PassThru -WindowStyle Hidden -RedirectStandardOutput ($run + '/test.log') -RedirectStandardError ($run + '/test.err')
if (-not $p.WaitForExit(300000)) { taskkill /PID $p.Id /T /F; throw 'test process-tree timeout' }
if ($p.ExitCode -ne 0) { throw 'focused tests failed' }
```

Owning build: `dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path <absolute-run>/build`. Format: `dotnet format src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --include src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs src/DeepSpaceSaga.Engine/Content/DemoCampaignDefinitions.cs src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0032Us0007Tk0003Tests.cs`; тот же timeout wrapper и абсолютные stdout/stderr. Не запускать format для content-only. Длительный corpus делить по указанным полным IDs, не увеличивать cap. Native/manual сценарий: AC-01: Новая команда кампании имеет identity, revision, типизированный результат и причину отказа; повтор с тем же payload возвращает исходный receipt, изменённый payload с той же identity отклоняется. AC-02: Состояние демо, квитанции и версии сохраняются атомарно; Save21 мигрирует без выдачи нового стартового имущества, неподдерживаемая версия не заменяет сессию. AC-03: Доставка через LocalClient и GameSessionHandle асинхронна; UI не списывает ресурсы по pending и не обращается к Engine из рендера. AC-04: Новые обязанности обрабатываются теми же календарными границами при обычном кадре и CaptureSaveState; нет параллельной модели мира.

Native запуск — только после проверки display layout, на меньшем мониторе с isolated profile/explicit index и `DSS_VALIDATION_SMALL_MONITOR=1`; иначе NOT RUN. Автотест не закрывает native, слуховую, независимую или продуктовую приёмку.

## Definition of Done

- Все шаги выполнены только в allowlist, тесты проверяют указанные исходы и ошибки.
- Served AC имеют evidence вклада этого слоя; story end-to-end/native/product gates не закрыты автоматически.
- Focused checks и owning build/format проходят либо конкретное исходное падение записано как блокер; timeout не PASS.
- Public API, units, persistence, idempotency и ограничения соблюдены; нет скрытого follow-up вне Board.
- Evidence в `D:/DeepSpaceSaga/artifacts/validation/<ticket>/<unique-run>/`, status/решения в Board, актуальные контракты в Documentation.
- Удалены только собственные disposable scripts/caches/results; сохранены необходимые ссылки evidence, нет мусора в корнях workspace/repo.

## Self-containment check

Prerequisites, allowlist, новые схемы/команды, concrete steps, критерии и named tests перечислены здесь. Для новых файлов указан создатель; их наличие до исполнения dependency не предполагается. Чтение источников допустимо для проверки drift, но новый продуктовый выбор или расширение files требует явной правки Board.
