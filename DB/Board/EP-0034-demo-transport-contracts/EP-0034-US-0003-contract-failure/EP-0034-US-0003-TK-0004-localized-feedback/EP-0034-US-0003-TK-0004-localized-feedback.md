---
epic: EP-0034-demo-transport-contracts
story: EP-0034-US-0003-contract-failure
ticket: EP-0034-US-0003-TK-0004-localized-feedback
title: RU/EN условия, результаты и причины отказа
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: content-data
matching_test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0034-US-0003-TK-0002-engine]
files_touched: 4
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-16T20:32:46.441Z
revision: 1
---

# RU/EN условия, результаты и причины отказа

## Why

Просрочка и отмена перевозки. Тикет 3/4 даёт проверяемый результат своего слоя. [Story и полный пользовательский результат](../EP-0034-US-0003-contract-failure.md).

## Decisions

Поручение владельца: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Время записи 2026-11-16T20:32:46.441Z, не доказанное время отправки. Разрешены тикеты и необходимые дополнительные истории. Human approval технических деталей/баланса не заявляется; stage approved означает автоматическую готовность workflow планирования. Исполнение и публикация требуют отдельного поручения.

## Assumptions

- Используется сокращённая демо на 60–120 минут, один кластер, готовый Tetrarch; backlog полного продукта не включается.
- Технические имена/новые API ниже — планируемые, не утверждения о текущем коде. Численные A-решения в шагах — baseline для owning плейтеста, не доказанный баланс.
- Сначала prerequisites: EP-0034-US-0003-TK-0002-engine. При drift исходников пересмотреть allowlist в Board до расширения diff.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | Существующий locale dictionary: `src/DeepSpaceSaga.Client/Data/Locale/Russian.json:1`. | Только ключи данной истории, форматные placeholders и тексты отказов. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | Существующий locale dictionary: `src/DeepSpaceSaga.Client/Data/Locale/English.json:1`. | Только ключи данной истории, форматные placeholders и тексты отказов. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0034Us0003Tk0004Tests.cs` | Новый файл этого тикета; на момент grounding отсутствует. | Parity/reason coverage реальных словарей. |

Разрешены ровно 3 файла. Не менять численные правила Engine и не переводить persistent IDs.

## Public API after the change

No API change. Существующие словари Russian/English; язык в Settings — Russian или English, не новые ru/en IDs.

## Implementation steps

1. По контракту этой истории и API предыдущих тикетов добавить ключи для условий, стоимости/срока, доступности, pending, confirmed результата, каждого stable rejection code и empty state. Использовать одинаковые ключи/параметры в Russian.json и English.json.
2. Дать короткие конкретные RU/EN тексты: что произошло, почему действие недоступно и какое действие возможно. Не показывать class names, IDs и implementation jargon вместо объяснения.
3. Проверить все keys, используемые следующим UI тикетом, и отсутствие неизвестных placeholders; не менять чужие переводы. Сюжетные тексты, если есть, остаются в owning dialogue content.

## Served acceptance criteria

- **AC-01:** Пауза не сокращает срок; смена скорости использует игровой календарь.
- **AC-02:** Отмена, просрочка и прибытие на границе срока дают один объяснимый результат и не повторяют штраф.
- **AC-03:** Для каждого исхода определены состояние пассажиров, освобождение кают и возможность продолжать рейсы.
- **AC-04:** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

Вклад этого тикета ограничен слоем `content-data`; полный AC принимается после всей карты истории, включая реальный пользовательский путь. DTO/test scaffold сам по себе не закрывает AC.

## Out of scope

Полная верфь, другие носители, фракционная симуляция, пленные, тактическое отступление, сохранение внутри боя, универсальный квестовый движок и публикация. Не переписывать поставленный EP-0029, не ремонтировать несвязанные файлы и не расширять лимит пяти файлов молча.

## Invariants

- `DB/Documentation/01-Requirements/EngineRequirements.md`, «Архитектура и авторитет» / «Команды, события и воспроизводимость»: Engine владеет исходами/временем/RNG/деньгами; render не мутирует мир.
- `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`: асинхронный transport. `src/DeepSpaceSaga.Engine/SimulationEngine.SortieSettlement.cs:17`: использовать единый settlement, не создавать второй inventory.
- `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:40`: на grounding writer Save21; переход к Save22 принадлежит EP-0032-US-0007. Последующие тикеты не назначают конкурирующие версии writer.
- Reject/duplicate не даёт частичных денег, груза, награды, опыта или повторного звука. Pause не двигает deadlines; calendar и motion units не смешиваются.
- Реализация gameplay-инвариантов — в owning engine ticket; клиентский/content/tooling тикет их не дублирует.

## Tests

Будущие named tests (NOT RUN):

- `DemoEp0034Us0003Tk0004Tests.StoryKeysExistInBothLocales` — все ключи экрана и причины отказа имеют RU/EN значения.
- `DemoEp0034Us0003Tk0004Tests.FormatArgumentsMatch` — placeholders и типы форматируемых значений одинаковы.
- `DemoEp0034Us0003Tk0004Tests.NoRawReasonCodeLeaks` — known rejection code отображается объяснением, не техническим кодом.

Из `D:/DeepSpaceSaga/DSS`: создать уникальный абсолютный `D:/DeepSpaceSaga/artifacts/validation/EP-0034-US-0003-TK-0004-localized-feedback/<UTC-run>/`; команда `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0034Us0003Tk0004Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`. Запускать Start-Process с redirected stdout/stderr в run, WindowStyle Hidden, WaitForExit(300000); при timeout taskkill /PID <pid> /T /F и не объявлять PASS. Content-only: format не нужен; owning build `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path <absolute-run>/build` с тем же process-tree cap. Реальную читаемость проверяет UI тикет и итоговая EP-0040-US-0005; unit parity не закрывает native/product gate.

## Definition of Done

- Все шаги выполнены только в allowlist, тесты проверяют указанные исходы и ошибки.
- Served AC имеют evidence вклада этого слоя; story end-to-end/native/product gates не закрыты автоматически.
- Focused checks и owning build/format проходят либо конкретное исходное падение записано как блокер; timeout не PASS.
- Public API, units, persistence, idempotency и ограничения соблюдены; нет скрытого follow-up вне Board.
- Evidence в `D:/DeepSpaceSaga/artifacts/validation/<ticket>/<unique-run>/`, status/решения в Board, актуальные контракты в Documentation.
- Удалены только собственные disposable scripts/caches/results; сохранены необходимые ссылки evidence, нет мусора в корнях workspace/repo.

## Self-containment check

История, контрактные причины и следующий экран доступны по карте этой story. Allowlist состоит из двух существующих словарей и одного теста; игровые правила и frontend wiring принадлежат соседним тикетам. Новые ключи согласовать с их объявленным API, а не с догадкой о скрытом состоянии.

## Execution result (2026-11-17)

Статус: implemented; **validation pending at story end**. PASS истории здесь не заявляется.

Изменённые файлы:

- `src/DeepSpaceSaga.Client/Data/Locale/English.json`, `Russian.json` — блок `Demo.TransportFailures.*` сразу после `Demo.PassengerContracts.State.unavailable` (аддитивно): `Title`, `Empty`, `Unavailable`, `Calendar` (правило календарных сроков, AC-01), `Terms` {0} штраф отмены, {1} срок, {2} штраф просрочки, `Cancel`, `CancelConfirm` {0}, `CancelNo`, `Pending`, `Cancelled` {0} штраф/{1} оплачено/{2} долг, `Line` (6 параметров), `DisembarkPending` {0}, `Disembarked` {0}/{1} станция/{2} время, `Lost` {0}, `Debt` {0}; `Outcome.cancelled|expired|carrier_lost`; `Reason.*` для каждого кода отказа команды/вердикта условий (`contract_not_found`, `contract_not_active`, `not_cancellable`, `stale_revision`, `unavailable`, `unsupported`, `invalid_identity`, `identity_conflict`, `carrier_destroyed`, `battle_active`). Плюс `Demo.PassengerContracts.State.failed`.
- `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0034Us0003Tk0004Tests.cs` — `StoryKeysExistInBothLocales`, `FormatArgumentsMatch`, `NoRawReasonCodeLeaks`.
- Расширение allowlist: `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0034Us0002Tk0004Tests.cs` — точный список ключей `Demo.PassengerContracts.*` дополнен состоянием `failed` (новый delivery state из TK-0001).

Проверки: `dotnet test tests/DeepSpaceSaga.Client.Tests --filter "DemoEp0034|Locali"` — 71 passed / 5 failed (TRX `D:/DeepSpaceSaga/artifacts/validation/EP-0034/us0003-tk0004/client.trx`); все 5 (`LocalizationCoverageTests` ×3, `SessionHandleBattleTests…Localized` ×2) входят в зафиксированный baseline US-0002 (`D:/DeepSpaceSaga/artifacts/validation/EP-0034/us0002-20261117/story/client-reg.log`). Новые тесты 3/3.

Допущения:

- A-01: отдельный префикс `Demo.TransportFailures.*`, чтобы не ломать точные списки ключей `Demo.Jobs.*` (US-0006) и не смешивать отказы приёма с отказами отмены.
- A-02: подтверждение отмены — повторное нажатие той же кнопки (текст `CancelConfirm`), без модального окна: модальное окно по Modal Pause Rule останавливало бы симуляцию.
