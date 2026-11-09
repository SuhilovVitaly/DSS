---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0007-decoy-ui
ticket: EP-0020-US-0007-TK-0003-salvo-decoy-reveal
title: Ловушки в полёте залпа и результатах
stage: done
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0020-US-0007-TK-0002-fire-panel-decoy]
files_touched: 6
serves: [US7-AC3]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Ловушки в полёте залпа и результатах

## Why

Эффект ловушки должен быть объясним: игрок видит, какой выстрел ушёл в ловушку и как изменился шанс (исходный запрос механики).

## Decisions

Применяются [D-15–D-23](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Источники — `DecoysLaunched`, `TacticalShotSnapshot.Decoyed/ChanceWithoutDecoy` текущего залпа. Метки показываются только в фазах полёта залпа и результатов этого залпа.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleResults.cs` | проекция результатов после US-0004 TK-0003 | Множество пустивших ловушку; поглощённые выстрелы. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs` | метка из TK-0002 | Свои в полёте, все в результатах; выноски UiSpec §12.2. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Salvo.cs` | EP-0019: очерёдность огня (DesignSpec §6.4) и результат выстрела (§6.5) | Шанс выстрела по ловушке — UiSpec §12.1; строки «ловушка цели» — UiSpec §12.3. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/HoverCard.cs` | после US-0004 TK-0003 | Значок «Ловушка» UiSpec §12.4. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleLogFormatter.cs` | события журнала | `DecoyLaunched` и пометка поглощённого выстрела. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ResultsPhaseTests.cs` | — | Раскрытие только после разрешения. |

## Public API after the change

No public API change.

## Implementation steps

1. Проекция ловушек залпа.
2. Очерёдность огня, выноски, панель результата, карточка наведения, журнал.
3. Проверка: в фазе стрельбы и полёте ловушка противника не видна и шанс по ней — без ловушки; в результатах видна.

## Out of scope

Новые регрессионные снимки (TK-0004).

## Invariants

Сцены залпа и результатов без ловушек не меняются, кроме изменений данных seed 1.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `ResultsPhaseTests.Enemy_decoy_is_revealed_only_in_results`
- `ResultsPhaseTests.Decoyed_miss_gets_callout_and_panel_line`
- `ResultsPhaseTests.Decoy_marks_disappear_after_results`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~ResultsPhaseTests|FullyQualifiedName~SidesAndLogTests|FullyQualifiedName~FirePanelDecoyTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us7-tk3" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleResults.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleLogFormatter.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/HoverCard.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US7-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Источник данных и вид заданы.

## Исполнение

В процессе. Scope уточнён до реализации: реальные файлы BattleMapRenderer.cs/.Fire.cs, HoverCard.cs. Для typed journal пометки нужен init TacticalLogShot.Decoyed(defaultfalse), Engine session projection и compatibility/Journal tests; без него formatter(entry) не знает факт ловушки старых ходов. Полёт редактирует только presentation DTO: enemy decoy IDs/charges/шансы/метки журнала скрыты до Results, authoritative snapshot не меняется. Zero-shot с пуском сохраняет мгновенные Results как M1 EV, без фиктивного miss/projectile.

Завершено: Results/SidesAndLog/FirePanelDecoy31/31PASS; EngineSession/Journal44/44PASS;Contracts27/27PASS; build/все scoped-format/diff-checkPASS. Новые owning Results проверки вынесены в partial ResultsPhaseTests.Decoys.cs. Проверены enemy hidden IDs/charge/chance/log→Results reveal, свой оплаченный charge/mark в полёте, Green chance собственной ловушки/обычный chance чужой в очереди, две выноски/панель ручной сцены UiSpec, replay без нового Fire/расхода, очистка послеResults и zero-shotD без fake miss/projectile. Map marks сохраняются у уничтоженных пустивших дронов до конца Results; callouts учитывают occupied/markers/q по §12.2. Самопроверка неизменности authoritative snapshot, последовательных impact states и журнала выполнена; production/manual render просмотрен. US7-AC3 покрыт. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us7-tk3/final/tests.trx,engine/tests.trx,contracts/tests.trx,format-*.log. Старые seed1 строки/снимки актуализируются следующимTK4.
