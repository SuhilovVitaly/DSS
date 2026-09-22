---
epic: EP-0001-trading-system
story: EP-0001-US-0005-station-resource-fields
ticket: EP-0001-US-0005-TK-0005-resource-info-panel
title: Состав в существующей панели объекта
stage: approved
layer: client
depends_on: [EP-0001-US-0005-TK-0004-structural-resource-survey]
files_touched: 4
serves: [AC-04, AC-06, AC-07]
created: 2026-09-21T09:56:58Z
revision: 1
---

# Состав в существующей панели объекта

## Why

Игрок должен видеть массу ресурсного астероида сразу, неизвестный состав до StructuralScan и реальные доли после успеха. Существующая Object Info и кнопка Structural Scan уже дают нужные места взаимодействия; новый экран не нужен.

## Decisions

Отдельных решений пользователя нет; исходный запрос сохранён в story. Client реализует принятую карту тикетов, не расширяет scanner scope.

## Assumptions

- Survey=null означает legacy/non-field object: существующее отображение сохраняется. Client не определяет состав по картинке, ID, имени станции или сохранению.
- Подписи Object Info сохраняют текущий стиль Name/Speed/Direction (Controls/ObjectInfoPanel.cs:161–180). Для generated fields вместо Direction показать Mass/Composition и известные resources. Новые строки Mass/Composition/Unknown; названия ресурсов — существующий TradeItemPresentation.ItemDisplayName(string).
- На основной карте астероид остаётся подписан ID. Name и другие подсказки не раскрывают field kind.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs | CommandsPanel callbacks:321; eligibility:702; sender:736; ToObjectInfoPanelData:2102–2108 | Передать Survey из Pose; добавить StructuralScan gating и защиту отправки с явным выбранным target |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs | BuildLines:161–180 показывает три строки; ObjectInfoPanelData в конце файла содержит Image optional; Render вызывает BuildLines:296 | Additive Survey в data, unknown/known formatting и размещение текста в существующей строке панели |
| tests/DeepSpaceSaga.Client.Tests/ObjectInfoPanelTests.cs | Pure formatting tests:56–108 | Покрытие неизвестного/известного состава, legacy compatibility, размеров строки и headless real-content flow |
| tests/DeepSpaceSaga.Client.Tests/CommandsPanelSkeletonTests.cs | Scanner fixtures:45–46,69–85 и существующие проверки command selection | Обновить StructuralScan fixtures и проверки eligibility/явного target/repeated click |

Matching test project: `D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj`.

## Public API after the change

В существующем ObjectInfoPanel.cs record ObjectInfoPanelData получает последний optional параметр `AsteroidSurveySnapshot? Survey = null`. Остальные параметры/значения по умолчанию сохраняются. Contracts не меняются, используется TK-0001.

ObjectInfoPanel.BuildLines(ObjectInfoPanelData? data) сохраняет тип результата List<(string Label,string Value)>.

## Implementation steps

1. ToObjectInfoPanelData передаёт p.Survey вместе с остальными существующими полями. Данные берутся из authoritative snapshot, сохранённого прогнозом; не читать Engine save/manifest и не пересчитывать fractions. Для Survey targets Name=ObjectId даже при неожиданном DisplayName; Image использовать только переданное Engine значение.
2. Survey!=null: строки Name, Speed, Mass (целое MassKg с единицей kg), Composition. Unknown: Composition=Unknown и никаких строк ресурсов, даже если повреждённый DTO принёс nonempty Resources. Known: coarse CompositionType и resource rows в ordinal ItemTypeId order, label=TradeItemPresentation.ItemDisplayName(id), value=Permille/10 процентов с максимум одним десятичным знаком. Не интерпретировать проценты как доступную добычу/количество груза. Не показывать Direction generated stationary астероида как отдельный результат StructuralScan. Survey=null и data=null сохраняют прежние три строки.
3. Использовать существующую текстовую колонку и spacing; самый длинный реальный variant имеет четыре ресурса, всего восемь строк. При большем payload вычислять требуемую высоту body из BuildLines.Count с существующими padding/line height, без обрезания доступных знаний. Сохранить hide/show, collapse, hit rectangles и image box. Изменения layout ограничены ObjectInfoPanel.cs; не менять общий экран/главные labels ради этой задачи.
4. В IsModuleCommandEnabled только для scanner.structuralScan требовать выбранный живой объект с Survey{CompositionKnown:false,CanStructuralScan:true}. Найденный текущим ResolveModuleId модуль должен быть On/Ready/StructurePoints>0/ActiveCommandType=null. Сохраняется существующее правило выбора первого module по Position; не вводить автопоиск другого scanner. Legacy Survey=null, no selection, busy, unavailable, range false, already known => disabled. Другие команды используют прежнюю логику.
5. SendCommandFromPanel повторно проверяет StructuralScan eligibility перед отправкой. Command передаёт moduleId и _selectedObjectId явно; hover/active panel object не подменяет selection. Пока snapshot не подтвердил busy, повторный клик может дойти до Engine; Engine гарантирует Busy/idempotency (TK-0004), Client не раскрывает состав оптимистично. После success кнопка disabled, после Failed разрешается новая попытка по свежему snapshot.
6. Headless integration в существующем ObjectInfoPanelTests: загрузить реальные Settings/MVP scenario после US-0004/TK-0003, взять unknown field snapshot, передать через screen projection в панель; отправить scanner command, продвинуть Engine до календарного due с фиксированным seed успешного draw, снова спроецировать snapshot и проверить строки. Save/load повторяет известный состав. Контрольный legacy DTO без Survey проходит прежние tests. Helpers разместить в одном из двух разрешённых test files.

## Out of scope

Новый scanner экран/прогресс-бар, main-map labels, GeneralScan, client-side probability/RNG/knowledge cache, raw manifest access, добыча, trading UI, LocalClient transport и translations refactor. CommandsPanel.cs не требует правки: existing callbacks достаточны.

## Invariants

- Передача команды с explicit target: src/DeepSpaceSaga.Contracts/PlayerCommand.cs:14–22; экранный sender GameSessionScreen.cs:736–751.
- Client display-only knowledge; authoritative Survey из TK-0001/TK-0004. InstalledModuleSnapshot содержит PowerState/OperationalState/StructurePoints/ActiveCommandType: Contracts/InstalledModuleSnapshot.cs:18–21.
- Имена ресурсов уже поддерживает UI/Screens/Trade/TradeItemPresentation.cs:4; этот файл не редактировать.
- Основная подпись астероида — ID: Documentation/01-Requirements/EngineRequirements.md:1918; работа панели не меняет основной renderer.

## Tests

ObjectInfoPanelTests:
- `Unknown_resource_field_shows_id_speed_mass_and_unknown_composition` — AC-04/06.
- `Unknown_flag_hides_inconsistent_resource_payload` — AC-06, defense against accidental UI leakage.
- `Known_resource_field_shows_authoritative_fractions_in_stable_order` — AC-06; percentages total100, label fallback через existing helper.
- `Legacy_and_empty_panel_data_keep_existing_lines` — AC-06.
- `Resource_rows_fit_expanded_body_and_preserve_hit_regions` — AC-06; actual four-item variant плюс synthetic extended payload.
- `Real_content_scan_and_reload_update_existing_info_panel` — AC-04/07; production bootstrap + engine outcome + screen projection + formatting.

CommandsPanelSkeletonTests:
- `Structural_scan_requires_eligible_survey_and_ready_idle_scanner` — AC-04/06; matrix missing/legacy/known/out-of-range/off/broken/busy.
- `Structural_scan_sends_selected_target_and_resolved_module` — AC-04; different selected versus hovered object.
- `Structural_scan_disables_after_success_and_reenables_after_failure` — AC-04/06.
- `Structural_scan_sender_rechecks_latest_eligibility` — AC-04; race between layout and click, no send when stale.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~ObjectInfoPanelTests|FullyQualifiedName~CommandsPanelSkeletonTests|FullyQualifiedName~StationResourceFieldContentTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

Manual smoke при исполнении тикета: New Game с materialized map → выбрать generated asteroid → mass/Unknown → Structural Scan → success раскрывает доли → save/load сохраняет строки. На паузе состав не раскрывается от ожидания реального времени. RNG failure допустим и не считается UI failure; успешный путь проверяется deterministic headless test.

## Definition of Done

- Steps реализованы только в четырёх разрешённых файлах; существующая панель показывает все нужные строки.
- Все served criteria покрыты named tests, real content flow и указанным smoke; отсутствие выполненного smoke явно отмечено, не выдаётся за проверку.
- Tests/build/format проходят либо конкретные baseline failures записаны отдельно.
- API/invariants/out-of-scope соблюдены; нет скрытых догадок о составе, optimistic reveal и сломанного legacy formatting.
- Нет незаписанных assumptions/блокирующих вопросов; результат наблюдаем в панели и по отправленной команде.

## Self-containment check

Передача DTO, формат каждой строки, источник labels, exact eligibility и существующие точки UI подключения описаны. Contracts/Engine/content готовы по dependency order; новые production files, локализационные таблицы или изменения CommandsPanel не нужны.
