---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0004-materialized-world-compatibility
ticket: EP-0018-US-0004-TK-0003-local-world-roundtrip
title: "Реальные сохранения продолжают географию обеих версий"
stage: approved
approval_kind: workflow
implementation_status: completed
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0018-US-0004-TK-0002-legacy-trade-map-validation, EP-0018-US-0003-TK-0003-voyage-geography]
files_touched: 3
serves: [AC-01, AC-02, AC-03]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Реальные сохранения продолжают географию обеих версий

## Why
Включить новую географию только после доказанной materialized persistence, сохранив старые миры и строгую проверку ссылок.
Поставляемый результат: реальные сохранения продолжают географию обеих версий. Тикет покрывает AC-01, AC-02, AC-03 [истории](../EP-0018-US-0004-materialized-world-compatibility.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| tests/DeepSpaceSaga.Engine.Tests/LocalWorldSaveRoundtripTests.cs | LocalSaveRestoresSameSystem и FailedLoadLeavesSaveUntouched | Добавить six-scenario v2 roundtrip и changed-config coverage. |
| tests/DeepSpaceSaga.Engine.Tests/ClusterLocalResumeTests.cs | Существующий local resume suite | Добавить v1 preserved world и v2 active voyage continuation. |
| tests/DeepSpaceSaga.Engine.Tests/SaveMigration20Tests.cs | Существующие Save18/19/20 migration tests | Закрепить старые solar descriptors и отсутствие перегенерации при migration. |

## Public API after the change
No API change; test-only engine layer, production fixes вне allowlist возвращаются владельцу failing predecessor.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Добавить тесты через существующий LocalGameSessionConnection/save slot API с настоящими файлами в unique validation/temp run, не mocked serializer.
2. Сохранять v1 fixture и v2 worlds, поменять current generation config и Vmax перед Load; сравнить geometry/epochs/reference speed после продолжения времени.
3. Проверить все6 scenarios и negative unknown/mixed version: source bytes/running snapshot прежние.
4. Сохранить Save18/19 migration regression; отдельную версию writer не вводить в test-only тикете.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- LocalWorldSaveRoundtripTests.V2AllScenariosIgnoreChangedGenerationContentOnLoad
- ClusterLocalResumeTests.V1AndV2ResumeSameGeometryAndVoyage
- SaveMigration20Tests.LegacySolarWorldIsNotRegenerated
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0004-TK-0003-local-world-roundtrip"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" -c Release --filter "FullyQualifiedName~LocalWorldSaveRoundtripTests.V2AllScenariosIgnoreChangedGenerationContentOnLoad|FullyQualifiedName~ClusterLocalResumeTests.V1AndV2ResumeSameGeometryAndVoyage|FullyQualifiedName~SaveMigration20Tests.LegacySolarWorldIsNotRegenerated" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/LocalWorldSaveRoundtripTests.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/ClusterLocalResumeTests.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/SaveMigration20Tests.cs"`

## Definition of Done
- Все Implementation steps выполнены только в allowlist; публичный контракт и зависимости соблюдены.
- AC-01, AC-02, AC-03 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
- Focused tests и owning build проходят; исходные падения описаны отдельно и не скрывают новые.
- Diff, scoped format для C#, JSON/Markdown проверки по типу изменения проходят.
- Нет скрытых assumptions или нерешённых блокирующих вопросов; фактические evidence paths записаны в Board.
- Временные файлы очищены; retained evidence находится только в разрешённом validation run.
- Native/product acceptance не объявлена по одним unit tests.

## Self-containment check
Разрешённые файлы, текущие точки входа, конечное поведение, шаги, planned API и точные проверки перечислены здесь. Общий продуктовый контракт — ../../ImplementationContract.md; зависимости — frontmatter. Прежде чем менять API вне этих границ, пересмотреть Board, а не угадывать.

## Исправление реального dependency order — 2026-11-07

Сохранение активногоv2рейса изUS0003TK0003 требует strictfileingress и materialized
trading validation US0004TK0001/0002. Поэтому эти2тикета выполняются послеUS0003TK0002,
затем voyageTK0003, затем local-file-roundtrip/activation. Story4 больше не требует
завершения всейStory3 перед началом; точные частичные gates перечислены в карточках.
Это устраняет логическую зависимость тестов от ещё не реализованногоloader без
переноса FAIL вPASS или объединения тикетов. 9историй/29тикетов остаются прежними.
## Execution evidence
2026-11-07: six v2 worlds saved through LocalGameSessionConnection to real unique slots; production file factory restores, then doubled Vmax registry and invalid new-generation config cannot move loaded world or frozen reference. Continued positions/epochs match. Unknown/mixed version files and live session stay unchanged. v1/v2 in-flight/docked file resume plus actual archived Save18/19 regressions pass. LegacySolarWorldIsNotRegenerated exercises decoded DTO migration unit18/19/20, not a fabricated old wire envelope. Initial failures were fixture misuse of clock and missing retired envelopes; corrected controlled-clock tests use actual published times. Focused-verified33/33 PASS; Release build, scoped format-verified exit0 and diff PASS. Evidence D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261107-local-roundtrip/{focused.trx,focused-final.trx,focused-verified.trx,build.log,format-verified.log}. Slot folders deleted in finally; confirmed no local-save-* directories. Self-review complete.
