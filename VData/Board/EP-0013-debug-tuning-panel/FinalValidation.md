# EP-0013 — финальная проверка, 2026-10-22

Общий результат и публикации — [ExecutionStatus](ExecutionStatus.md) и
[PublicationStatus](PublicationStatus.md). Покрытие37 исходных AC —
[AcceptanceCoverage](AcceptanceCoverage.md); итоговый охват документации —
[DocumentationImpact](DocumentationImpact.md).

## Automated

Все процессы тестов ограничены300000мс с завершением дерева при таймауте.
Время процесса, exitCode, stdout/stderr и TRX сохранены в evidence.

| Проверка | Результат | Evidence под `D:/DeepSpaceSaga/artifacts/validation/ep0013/` |
|---|---|---|
| Contracts полный |269/269 PASS|`epic-contracts-final-20261022T132919004`|
| Motion полный |251/251 PASS|`epic-motion-final-20261022T132924662`|
| Client полный после исправлений |2284/2284 PASS|`epic-client-final-20261022T131957133`|
| Engine полный через точные части |2478/2478 PASS|`engine-coverage-audit.json`, `engine-partitions-20261022.json`|
| Performance.Tests полный |9/9 PASS|`epic-performance-tests-final-20261022T133435136`|
| Solution build |PASS,0 warnings/0 errors|`epic-build-final-20261022T133205830`|
| Scoped format всех C# эпика |PASS|`epic-format-whitespace-final-20261022T133110424`, `epic-format-final-20261022T133118048`|
| Board IDs/counts/dependency DAG/links |PASS:7 stories,21 done tickets, DAG,0 broken links|`board-audit.json`|
| Graphify источники/экспорты/query/path/explain |PASS:1162 source hashes,70 semantic inputs,0 stale/corrupt|`graph-final/verification.json`, `graph-audit.json`|

Engine inventory содержит2478 cases /175 классов. Успешные
обычные группы:618+514+518+716, четыре дополнительных corpus facts и двенадцать
сценарных групп: шесть×8 cluster + шесть×10 system/full-map. Финальный аудит
сравнивает мультимножество Passed TRX имён с inventory: пропуски и лишние случаи
отсутствуют, все2478 test IDs уникальны. XUnit сокращает длинные JSON аргументы:
четыре display names повторяются в исходном inventory, всего2470 различных имён.
Их кратность сохранена, сокращённое имя не считается самостоятельным case ID.
Каждый успешный процесс уложился в300000мс.
Это полное покрытие через части, а не PASS исходного монолитного запуска.

### Обнаруженные дефекты и прежние неуспешные попытки

- Начальный Engine monolith `epic-engine-full-20261022T130003557` и слишком
  крупные части `epic-engine-part-0-20261022T131055970`,
  `epic-engine-part-3-20261022T131733496` — TIMEOUT/124 при300000мс. Эти результаты
  сохранены как нарушение бюджета тех запусков, не переписаны как PASS.
- Начальный Client `epic-client-full-20261022T130504029` —6 failed/2278 passed.
  Исправлены освобождение debug resources при Defeat из-под модального экрана
  и приоритет клавиатурной навигации сфокусированного accordion докладов.
  Обновлены старые assertions bare S/step-turn под новый контракт; ожидание
  journal durability теперь сохраняет старый prefix, требует единственный
  последующий reload-completed и сохраняет точную проверку отсутствия replay.
- Focused повтор `final-review-client-fixed-20261022T131220913` —55/55 PASS,
  затем полный Client2284/2284 PASS. Отдельный reload/report admission повтор
  также прошёл; финальный полный прогон подтверждает интеграцию.
- Sandbox VSTest/Roslyn named-pipe ограничения устранены разрешённым внешним
  запуском. Такие infrastructure попытки не выдаются за ошибки продукта.

### Дополнительные экономические проверки

Полный необязательный для EP13 EconomyBalance запуск
`epic-economy-final-20261022T132934706` завершён TIMEOUT/124 за300396мс.
Дополнительный scoped запуск `epic-economy-scoped-final-20261022T133945895`
также TIMEOUT/124 за300329мс. Это не PASS баланса; частичные passed строки не
суммируются как завершённая suite. Целевой cargo/catalog continuation повтор
`epic-cargo-continuation-final-20261022T134446345` —4/4 PASS за48921мс.
Продуктовый баланс и длинные voyage матрицы не закрываются этим эпиком.

## Native

После финальной сборки оба настоящих Skia/OpenGL запуска прошли на
`Intel(R) Arc(TM) 140V GPU (16GB)`:

- `native-ru-final-20261022/report.json`: RU, UI scale1;12/12 checks PASS.
- `native-en-final-20261022/report.json`: EN, UI scale1.5;12/12 checks PASS.
- Wrapper logs: `native-ru-final-20261022T133407629`,
  `native-en-final-20261022T133412178`.

Проверены реальный OpenGL context, F12, размер/scale/locale, подтверждение5.5,
захват wheel без zoom, реальные Engine pose при W+D=(40°,4.2км/с), confirmed
reset с удалением current JSON и неизменным defaults reference, Esc.
GPU captures `panel.png`, `axes.png`, `reset.png` сохранены в этих каталогах;
RU/EN layout просмотрен. Предыдущие нативные runs — исторические, текущими
считаются перечисленные повторные запуски после исправлений.

Ввод — managed key-state edges через production tracker/screen и реальные
Engine commands, контролируемое время. Это не hardware input injection.
Human visual/product acceptance, физические клавиатура/мышь и звук **NOT RUN**:
требуют человека/физического ввода; доступная автоматизация этого не подтверждает.
Независимый code review **NOT RUN**; итоговый review здесь — self-review.
Статус полной приёмки эпика остаётся OPEN.

## Graphify и документация

Свежий граф находится в `D:/DeepSpaceSaga/artifacts/validation/ep0013/graph-final/`.
Экспорт:16178 nodes,52504 edges,538 communities;841 code и321 Markdown files.
Он объединяет AST выбранного кода, Markdown headings/links и три host-agent
semantic fragments (`graph-semantic-1.json`…`graph-semantic-3.json`): полностью
прочитаны55 текущих Documentation файлов и15 исходников, проверяются70 source
hashes. `GraphifyRebuild.md` и обновляемые execution/publication/evidence ledgers
исключены из structural source hashes против циклических обновлений.

Граф — навигация по фактическому рабочему дереву, включая чужую текущую курацию.
Он не заявляет чистый опубликованный checkout, exhaustive semantics остальных
Board/code, image vision, runtime traces или gameplay acceptance. HTML агрегирует
communities, полный граф находится в JSON. AST unresolved endpoints и reference-only
символы явно учтены sidecars; dangling export edges, out-of-range locations,
missing source files и self-loops должны быть0. Token usage не измерялся.
Свежие fragments заменяют старый EP12 selective input, а не просто обновляют
его hashes. Структурный экспорт перестраивается после финальных кода и текстов.

## Evidence и уборка

Корень evidence: `D:/DeepSpaceSaga/artifacts/validation/ep0013/`.
Сохранены referenced ticket runs, начальные failures/timeouts, окончательные
TRX/logs, reading/semantic inputs, native captures и graph exports/audits.
Их полный retained manifest — `retained-evidence.json`; disposable scripts,
cache и ненужные intermediate fixtures удаляются после проверки.
Новые task scratch файлы в корне репозитория не требуются. Чужие изменения,
предсуществующие файлы и активные артефакты других задач сохраняются.
Git index содержит только EP13; общие документы публикуются адресными EP13
дополнениями. После commit выполняются немедленный push и проверка remote SHA;
фактическое подтверждение — `publication-audit.json`, сформированное после commit.
