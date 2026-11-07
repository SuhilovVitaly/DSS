# Исполнение EP-0018

2026-11-07. Статус: IN PROGRESS. Завершено и опубликовано 1/29
тикетов, 0/9 историй. Первый тикет локально реализован и проверен.

Основание: прямое поручение пользователя выполнить весь эпик по EpicExecutionPrompt.md.
База: c43e8af5cfbc76844e6a9109921fbae41153d370. Ветка/upstream:
`codex/ep-0016-tactical-battle-skins` / `origin/codex/ep-0016-tactical-battle-skins`.
Remote: https://github.com/SuhilovVitaly/DSS. До изменений remote SHA проверен через
`git ls-remote --heads origin codex/ep-0016-tactical-battle-skins` и совпал с базой.
Исходно только untracked папка данного эпика; посторонних изменений кода нет.

## Публикация и точный остаток

| Область | Коммит | Push |
|---|---|---|
| Подготовительный Board и gates | 2bb0ec17c67a05093d99e55af063086904bd8aee | BLOCKED: auto-review rejected |
| EP-0018-US-0001-TK-0001-geography-descriptor | 7d42085a273bc86cff19b9aa083ff8060fe159f8 | PASS, remote SHA verified |

Автоматическая проверка дважды отклонила действие
`git push origin HEAD:refs/heads/codex/ep-0016-tactical-battle-skins`:
экспорт внутренних Board-документов в непроверенный GitHub destination и несвязанную
ветку без доверенного прямого разрешения с конкретными payload/destination.
Повтор был выполнен только после дополнительной проверки Git remote/branch config
и строк 28,55,72 указанного пользователем EpicExecutionPrompt.md; отказ сохранился.
Обходов публикации нет. Последний опубликованный тикет EP-0018: отсутствует.
Нужно прямое разрешение пользователя публиковать папку Board EP-0018 и относящиеся
к нему изменения в origin/codex/ep-0016-tactical-battle-skins указанного репозитория.
До успешного push первого тикета следующий тикет не начинается.
Остаток: публикация подготовки/первого тикета, затем ещё 28 тикетов из Tickets.md.

## Локальный результат первого тикета

`ClusterGeographyRules` с reference speed в км/с и расстояниями в календарных днях;
optional `StationClusterMapSnapshot.Geography` в конце конструктора. Явные JSON
поля; missing/null legacy и v2 AuthoritativeSnapshot roundtrip. Shipped content
и генерация остаются v1. Self-review по C-02/AC-04: findings отсутствуют;
независимого review нет. Существующие аргументы/array converter не изменены.

## Validation

Абсолютный evidence root:
D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261107-descriptor/.
Все команды из D:/DeepSpaceSaga/DSS. Внешний test watchdog 300000ms с Kill(true).

| Команда | Фактический результат | Retained evidence |
|---|---|---|
| dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj -c Release --no-build --no-restore --filter FullyQualifiedName~ClusterGeographyContractTests --results-directory D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261107-descriptor --logger trx;LogFileName=focused-final.trx | PASS 5/5 | focused-final.trx, focused-final.log |
| dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj -c Release --no-restore --results-directory D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261107-descriptor --logger trx;LogFileName=contracts-full.trx | PASS 189/189 | contracts-full.trx, contracts-full.log |
| dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj -c Release --no-restore | PASS 0 warnings/errors | build.log |
| dotnet format src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --verify-no-changes --no-restore --include D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Contracts/StationClusterMap.cs | PASS exit0 вне sandbox | format-contracts-unsandboxed.log |
| dotnet format tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --verify-no-changes --no-restore --include D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Contracts.Tests/ClusterGeographyContractTests.cs | PASS exit0 вне sandbox | format-tests.log |
| git diff --check | PASS | Board запись |

Предварительные неуспешные попытки сохранены честно: focused.trx/log/err — sandbox
vstest не смог подключиться к testhost за 90s, abort; format-contracts.log — sandbox
MSBuild workspace error. Повтор вне sandbox выполняет те же локальные проверки.
focused-unsandboxed.trx/log/err — 2 FAIL из-за ошибочного нового ожидания IsEmpty
для отсутствующего legacy resourceBindings; исправлено на прежний IsDefault,
окончательные 5/5 и 189/189 выше. Это не изменение runtime семантики массива.

## Остальные gates и cleanup

Native, physical input, human/product и FPS NOT RUN. География, Save/Load v2, UI,
corpus и завершающая документация ещё не реализованы и не проверены.
Временные scripts/cache/slots/worktrees не создавались; task testhost PID32764
после abort уже завершён. Сохраняется только перечисленное evidence, пустые stderr
и лишние outputs удаляются. Новых временных файлов в workspace/repository roots нет.
Документационная история US-0009 должна остаться последней: полный inventory всего
репозитория, текущие контракты, соседние README/комментарии, Board, navigation и граф
после итогового diff; исходные allowlists расширяются с обоснованием до этих gates.
## Продолжение после прямого разрешения публикации

Пользователь прямо ответил «Да, разрешаю публикацию всего EP-0018 в указанную ветку»
на вопрос с точным repo/payload/веткой. Оба предыдущих push rejection — история;
текущего блокера публикации нет. 2bb0ec17 и7d42085a опубликованы, remote head
7d42085a273bc86cff19b9aa083ff8060fe159f8 подтверждён. Scope финальной документации
расширен отдельным0a8695f2d021a3cf90ef186b60b07bca12855fdd, push/remote SHA PASS.

TK-0002 локально завершён: focused12/12, Engine non-corpus2070/2070, build/format/diff
PASS. Полный корпус не запущен завершённо: broad attempt прерван для шардирования,
не подменяет будущий exact-ID corpus. Evidence — 20261107-generation-input.
Команда регрессии: dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
-c Release --no-build --no-restore --filter "Category!=LongRunning&FullyQualifiedName!~Corpus"
--results-directory D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261107-generation-input
--logger "trx;LogFileName=engine-non-corpus.trx". Точные build/format — карточка TK0002.
Следом TK0003: единое размещение станций. Остальные27тикетов не завершены.