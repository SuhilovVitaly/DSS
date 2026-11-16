---
epic: EP-0040-demo-campaign-onboarding
story: EP-0040-US-0005-demo-ru-en
ticket: EP-0040-US-0005-TK-0002-locale-layout
title: Читаемость и сохранение языка
stage: approved
approval_kind: workflow
implementation_status: not-started
layer: client
matching_test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0040-US-0005-TK-0001-locale-parity]
files_touched: 5
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-16T20:32:46.441Z
revision: 1
---

# Читаемость и сохранение языка

## Why

Демо полностью доступно на русском и английском. Тикет 2/2 даёт проверяемый результат своего слоя. [Story и полный пользовательский результат](../EP-0040-US-0005-demo-ru-en.md).

## Decisions

Поручение владельца: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Время записи 2026-11-16T20:32:46.441Z, не доказанное время отправки. Разрешены тикеты и необходимые дополнительные истории. Human approval технических деталей/баланса не заявляется; stage approved означает автоматическую готовность workflow планирования. Исполнение и публикация требуют отдельного поручения.

## Assumptions

- Используется сокращённая демо на 60–120 минут, один кластер, готовый Tetrarch; backlog полного продукта не включается.
- Технические имена/новые API ниже — планируемые, не утверждения о текущем коде. Численные A-решения в шагах — baseline для owning плейтеста, не доказанный баланс.
- Сначала prerequisites: EP-0040-US-0005-TK-0001-locale-parity. При drift исходников пересмотреть allowlist в Board до расширения diff.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Localization.cs` | `src/DeepSpaceSaga.Client/Localization.cs:10` — public static class Localization | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Client/UI/Screens/Contracts/ContractsScreen.cs` | `src/DeepSpaceSaga.Client/UI/Screens/Contracts/ContractsScreen.cs:22` — public sealed class ContractsScreen : IScreen | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Client/UI/Screens/Finance/FinanceScreen.cs` | `src/DeepSpaceSaga.Client/UI/Screens/Finance/FinanceScreen.cs:20` — public sealed class FinanceScreen : IScreen | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Client/UI/Screens/Settings/SettingsScreen.cs` | `src/DeepSpaceSaga.Client/UI/Screens/Settings/SettingsScreen.cs:9` — public sealed class SettingsScreen : IScreen | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0040Us0005Tk0002Tests.cs` | Новый файл этого тикета; на момент grounding отсутствует. | Именованные проверки этого тикета; не менять production из тестов. |

Разрешены только эти 5 implementation/documentation files. Board evidence/status этого тикета обновляются отдельно как каноническая история исполнения; не дают разрешения править произвольный production. Read-only архитектурные источники: `DB/Documentation/00-Process/CLAUDE.md`, `DB/Documentation/01-Requirements/EngineRequirements.md` и [общая карта](../../../EP-0041-demo-validation-and-release/TicketPlan.md).

## Public API after the change

No API change.

## Implementation steps

1. Проверить длинные RU/EN строки в job board, journal, finance и settings на минимальном поддерживаемом окне; перенос/скролл не перекрывает кнопку.
2. Смена языка сохраняется существующим Settings механизмом, обновляет labels и не сбрасывает текущий contract/choice.
3. Native checklist охватывает intro/finale/ship/battle results; дефекты в иных файлах оформлять owning story follow-up, не расширять allowlist молча.

## Served acceptance criteria

- **AC-01:** RU/EN покрывают обязательства, услуги, угрозы, сюжет, обучение, финал, звук и причины отказа; технические ключи не видны игроку.
- **AC-02:** Длинные строки и числовые значения не закрывают действие в меню, журнале и итогах; скролл и клавиатура позволяют прочитать полный текст.
- **AC-03:** Выбор языка сохраняется; после перезапуска и загрузки используются те же ключи и форматирование единиц.
- **AC-04:** Автоматический parity ключей и ручное прохождение обеих локализаций фиксируются раздельно.

Вклад этого тикета ограничен слоем `client`; полный AC принимается после всей карты истории, включая реальный пользовательский путь. DTO/test scaffold сам по себе не закрывает AC.

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

- `DemoEp0040Us0005Tk0002Tests.LongText_LeavesActionReachable` — проверяет шаг 1 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0040Us0005Tk0002Tests.LanguageRestart_PreservesSelection` — проверяет шаг 2 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0040Us0005Tk0002Tests.LocaleChange_DoesNotMutateCampaign` — проверяет шаг 3 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.

Запуск из `D:/DeepSpaceSaga/DSS`; создать уникальный run, ограничить **каждый process tree 300000 ms**, не продолжать после timeout/failure. Пример PowerShell для именованного test corpus:

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0040-US-0005-TK-0002-locale-layout/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Force -Path $run | Out-Null
$p = Start-Process dotnet -ArgumentList @('test','tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj','-c','Release','--filter','FullyQualifiedName~DemoEp0040Us0005Tk0002Tests','--results-directory',($run + '/results'),'--artifacts-path',($run + '/build')) -PassThru -WindowStyle Hidden -RedirectStandardOutput ($run + '/test.log') -RedirectStandardError ($run + '/test.err')
if (-not $p.WaitForExit(300000)) { taskkill /PID $p.Id /T /F; throw 'test process-tree timeout' }
if ($p.ExitCode -ne 0) { throw 'focused tests failed' }
```

Owning build: `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path <absolute-run>/build`. Format: `dotnet format src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --include src/DeepSpaceSaga.Client/Localization.cs src/DeepSpaceSaga.Client/UI/Screens/Contracts/ContractsScreen.cs src/DeepSpaceSaga.Client/UI/Screens/Finance/FinanceScreen.cs src/DeepSpaceSaga.Client/UI/Screens/Settings/SettingsScreen.cs tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0040Us0005Tk0002Tests.cs`; тот же timeout wrapper и абсолютные stdout/stderr. Не запускать format для content-only. Длительный corpus делить по указанным полным IDs, не увеличивать cap. Native/manual сценарий: AC-01: RU/EN покрывают обязательства, услуги, угрозы, сюжет, обучение, финал, звук и причины отказа; технические ключи не видны игроку. AC-02: Длинные строки и числовые значения не закрывают действие в меню, журнале и итогах; скролл и клавиатура позволяют прочитать полный текст. AC-03: Выбор языка сохраняется; после перезапуска и загрузки используются те же ключи и форматирование единиц. AC-04: Автоматический parity ключей и ручное прохождение обеих локализаций фиксируются раздельно.

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
