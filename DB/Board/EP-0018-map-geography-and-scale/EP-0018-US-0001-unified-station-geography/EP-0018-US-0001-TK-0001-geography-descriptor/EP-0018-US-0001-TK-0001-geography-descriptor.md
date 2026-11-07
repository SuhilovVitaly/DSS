---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0001-unified-station-geography
ticket: EP-0018-US-0001-TK-0001-geography-descriptor
title: "Сериализуемые правила географии нового кластера"
stage: approved
approval_kind: workflow
implementation_status: locally_verified_publication_blocked
layer: contracts
test_project: tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj
depends_on: []
files_touched: 2
serves: [AC-04]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Сериализуемые правила географии нового кластера

## Why
Устранить исключение для старых станций Default и сделать географический минимум реальным расстоянием между каждой парой человеческих станций.
Поставляемый результат: сериализуемые правила географии нового кластера. Тикет покрывает AC-04 [истории](../EP-0018-US-0001-unified-station-geography.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Contracts/StationClusterMap.cs | StationClusterMapSnapshot(..., ResourceBindings=default) | Добавить ClusterGeographyRules и optional Geography по C-02. |
| tests/DeepSpaceSaga.Contracts.Tests/ClusterGeographyContractTests.cs | Существующий contract test suite | Расширить missing/null/v2 roundtrip и explicit camelCase coverage. |

## Public API after the change
Планируемые public records/optional constructor tail: ImplementationContract C-02; wire field geography.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Добавить exact DTO C-02 с JsonPropertyName; optional параметр в конце positional record, не менять порядок старых аргументов.
2. Проверить старый JSON без Geography и v2 с descriptor; immutable arrays/default converters оставить прежними.
3. Закрепить правила единиц в XML summary; semantic validation принадлежит Engine.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- ClusterGeographyContractTests.V2GeographyRulesRoundTrip
- ClusterGeographyContractTests.LegacyMissingGeographyRemainsNull
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0001-TK-0001-geography-descriptor"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj" -c Release --filter "FullyQualifiedName~ClusterGeographyContractTests.V2GeographyRulesRoundTrip|FullyQualifiedName~ClusterGeographyContractTests.LegacyMissingGeographyRemainsNull" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Contracts/StationClusterMap.cs"`

`dotnet format "tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Contracts.Tests/ClusterGeographyContractTests.cs"`

## Definition of Done
- Все Implementation steps выполнены только в allowlist; публичный контракт и зависимости соблюдены.
- AC-04 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
- Focused tests и owning build проходят; исходные падения описаны отдельно и не скрывают новые.
- Diff, scoped format для C#, JSON/Markdown проверки по типу изменения проходят.
- Нет скрытых assumptions или нерешённых блокирующих вопросов; фактические evidence paths записаны в Board.
- Временные файлы очищены; retained evidence находится только в разрешённом validation run.
- Native/product acceptance не объявлена по одним unit tests.

## Self-containment check
Разрешённые файлы, текущие точки входа, конечное поведение, шаги, planned API и точные проверки перечислены здесь. Общий продуктовый контракт — ../../ImplementationContract.md; зависимости — frontmatter. Прежде чем менять API вне этих границ, пересмотреть Board, а не угадывать.

## Фактическое исполнение — 2026-11-07

Реализованы ClusterGeographyRules и optional Geography в конце конструктора,
явные camelCase JSON-поля и XML units/ownership. Legacy missing/null десериализуются
без новых правил; resourceBindings сохраняет прежнюю default-семантику.
V2 descriptor проходит AuthoritativeSnapshot roundtrip. Изменены только два файла
из Code context; Engine validation и activation остаются в следующих тикетах.

Проверки: focused ClusterGeographyContractTests 5/5 PASS, полный Contracts 189/189
PASS; Release owning build PASS (0 warnings/errors), scoped format production/test
PASS, git diff --check PASS. Self-review diff против C-02/AC-04: findings отсутствуют;
независимого review не было. Native/product/FPS NOT RUN и к этому DTO не применяются.

Evidence и точные команды: [реестр](../../ImplementationStatus.md).
Файлы результатов: D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261107-descriptor/.
Начальный sandbox testhost abort и исправленная ошибка тестового ожидания не
объявляются PASS; окончательные успешные TRX имеют имена focused-final.trx и
contracts-full.trx. Test process-tree watchdog 300000ms; оставшийся host проверен,
уже завершён. Disposable scripts/cache/slots не создавались.

Публикация BLOCKED автоматической проверкой безопасности: push в подтверждённый
upstream отклонён дважды даже после проверки Git config и явных полномочий
EpicExecutionPrompt.md. Требуется прямое подтверждение пользователем конкретных
payload и destination. Следующий тикет не начинается до успешного push.