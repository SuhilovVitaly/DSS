---
epic: EP-0032-demo-campaign-continuity
story: EP-0032-US-0007-consistent-campaign-actions
ticket: EP-0032-US-0007-TK-0004-campaign-load
title: Безопасная загрузка Save22 и транспорт
stage: approved
approval_kind: workflow
implementation_status: implemented-pending-story-validation
layer: local-client
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0032-US-0007-TK-0003-campaign-loader]
files_touched: 3
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-16T20:32:46.441Z
revision: 1
---

# Безопасная загрузка Save22 и транспорт

## Why

Согласованные действия и сохранённое состояние кампании. Тикет 4/5 даёт проверяемый результат своего слоя. [Story и полный пользовательский результат](../EP-0032-US-0007-consistent-campaign-actions.md).

## Decisions

Поручение владельца: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Время записи 2026-11-16T20:32:46.441Z, не доказанное время отправки. Разрешены тикеты и необходимые дополнительные истории. Human approval технических деталей/баланса не заявляется; stage approved означает автоматическую готовность workflow планирования. Исполнение и публикация требуют отдельного поручения.

## Assumptions

- Используется сокращённая демо на 60–120 минут, один кластер, готовый Tetrarch; backlog полного продукта не включается.
- Технические имена/новые API ниже — планируемые, не утверждения о текущем коде. Численные A-решения в шагах — baseline для owning плейтеста, не доказанный баланс.
- Сначала prerequisites: EP-0032-US-0007-TK-0003-campaign-loader. При drift исходников пересмотреть allowlist в Board до расширения diff.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs` | `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs:12` — public sealed class LocalGameSessionConnection : IGameSessionConnection | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Engine.LocalClient/ScenarioRepository.cs` | `src/DeepSpaceSaga.Engine.LocalClient/ScenarioRepository.cs:12` — public static class ScenarioRepository | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0032Us0007Tk0004Tests.cs` | Новый файл этого тикета; на момент grounding отсутствует. | Именованные проверки этого тикета; не менять production из тестов. |

Разрешены только эти 3 implementation/documentation files. Board evidence/status этого тикета обновляются отдельно как каноническая история исполнения; не дают разрешения править произвольный production. Read-only архитектурные источники: `DB/Documentation/00-Process/CLAUDE.md`, `DB/Documentation/01-Requirements/EngineRequirements.md` и [общая карта](../../../EP-0041-demo-validation-and-release/TicketPlan.md).

## Public API after the change

Реализация IGameSessionConnection.ExecuteDemoCampaignAsync.

## Implementation steps

1. Прокинуть ExecuteDemoCampaignAsync в авторитетную сессию, сохранив cancellation/disposal; не вызывать Engine из UI.
2. Передать loader ошибки версии/каталога вызывающему коду; работающая сессия сохраняется при неудаче подготовки новой. Использовать SaveAsync и его _saveGate/temp replace; не заводить второй механизм файловой записи.

## Served acceptance criteria

- **AC-01:** Новая команда кампании имеет identity, revision, типизированный результат и причину отказа; повтор с тем же payload возвращает исходный receipt, изменённый payload с той же identity отклоняется.
- **AC-02:** Состояние демо, квитанции и версии сохраняются атомарно; Save21 мигрирует без выдачи нового стартового имущества, неподдерживаемая версия не заменяет сессию.
- **AC-03:** Доставка через LocalClient и GameSessionHandle асинхронна; UI не списывает ресурсы по pending и не обращается к Engine из рендера.
- **AC-04:** Новые обязанности обрабатываются теми же календарными границами при обычном кадре и CaptureSaveState; нет параллельной модели мира.

Вклад этого тикета ограничен слоем `local-client`; полный AC принимается после всей карты истории, включая реальный пользовательский путь. DTO/test scaffold сам по себе не закрывает AC.

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

- `DemoEp0032Us0007Tk0004Tests.AsyncCommand_ReturnsAuthoritativeReceipt` — проверяет шаг 1 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0032Us0007Tk0004Tests.FailedCandidateLoad_PreservesLiveSession` — проверяет шаг 2 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0032Us0007Tk0004Tests.DisposedSession_RejectsMutation` — проверяет шаг 2 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.

Запуск из `D:/DeepSpaceSaga/DSS`; создать уникальный run, ограничить **каждый process tree 300000 ms**, не продолжать после timeout/failure. Пример PowerShell для именованного test corpus:

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0032-US-0007-TK-0004-campaign-load/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Force -Path $run | Out-Null
$p = Start-Process dotnet -ArgumentList @('test','tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj','-c','Release','--filter','FullyQualifiedName~DemoEp0032Us0007Tk0004Tests','--results-directory',($run + '/results'),'--artifacts-path',($run + '/build')) -PassThru -WindowStyle Hidden -RedirectStandardOutput ($run + '/test.log') -RedirectStandardError ($run + '/test.err')
if (-not $p.WaitForExit(300000)) { taskkill /PID $p.Id /T /F; throw 'test process-tree timeout' }
if ($p.ExitCode -ne 0) { throw 'focused tests failed' }
```

Owning build: `dotnet build src/DeepSpaceSaga.Engine.LocalClient/DeepSpaceSaga.Engine.LocalClient.csproj -c Release --artifacts-path <absolute-run>/build`. Format: `dotnet format src/DeepSpaceSaga.Engine.LocalClient/DeepSpaceSaga.Engine.LocalClient.csproj --verify-no-changes --include src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs src/DeepSpaceSaga.Engine.LocalClient/ScenarioRepository.cs tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0032Us0007Tk0004Tests.cs`; тот же timeout wrapper и абсолютные stdout/stderr. Не запускать format для content-only. Длительный corpus делить по указанным полным IDs, не увеличивать cap. Native/manual сценарий: AC-01: Новая команда кампании имеет identity, revision, типизированный результат и причину отказа; повтор с тем же payload возвращает исходный receipt, изменённый payload с той же identity отклоняется. AC-02: Состояние демо, квитанции и версии сохраняются атомарно; Save21 мигрирует без выдачи нового стартового имущества, неподдерживаемая версия не заменяет сессию. AC-03: Доставка через LocalClient и GameSessionHandle асинхронна; UI не списывает ресурсы по pending и не обращается к Engine из рендера. AC-04: Новые обязанности обрабатываются теми же календарными границами при обычном кадре и CaptureSaveState; нет параллельной модели мира.

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

## Execution — 2026-11-17

Campaign transport исполняется асинхронно вне вызывающего UI потока; cancellation/disposal проверяются до отправки и на worker. Используются существующие factory и SaveAsync, второй writer не введён. ScenarioRepository не требует изменений: фабрика поднимает loader exception до создания connection. Добавлены три named tests, включая реальный файл неподдерживаемой версии и сохранение живой сессии. Dotnet gate — конец US-0007; diff check PASS.
