# EP-0010 — текущая документация и grounding

2026-10-16: production path реализован. [Текущая архитектура](../../Documentation/04-Engineering/CombatReports.md), [status/tests](ImplementationStatus.md), [публикация](Publication.md), [инвентаризация](DocumentationInventory.md), [Graphify](../../Documentation/06-Tooling/TradingSolarGraphify.md). Самостоятельные игровые запуски запрещены пользователем; native/human/FPS OPEN.

## Grounding

Текущие точки: Contracts/BattleReportSnapshot + AuthoritativeSnapshot; SimulationEngine.CombatReports/CombatReportRange/CombatReportIdentity/CombatReportPersistence; Scenario/CombatReportsSaveData/CombatReportsSaveValidation; LocalClient/SnapshotMailbox; Client/CombatReportInbox/SnapshotBuffer; GameSession/CombatReportStore/Formatter/Assets, Controls/CombatReportPanel/Layout, GameSessionScreen.CombatReports/MapView и SkiaWindow.PrepareCombatReports. Все пути подтверждены текущим кодом и финальной сборкой. Восемь PNG созданы и копируются в output/publish. Root Save17 с optional child Version1 реализован. Современные line locations — в перестроенном Graphify; старые line numbers ниже относятся только к planning baseline.

## Исходный документ — история, superseded current summary выше

# EP-0010 — документация и grounding

[Эпик](EP-0010-combat-reports.md) · [ТЗ](../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md) · [Тикеты](Tickets.md) · [Статус](ImplementationStatus.md)

## Исторический grounding планирования

Planning baseline: HEAD `af594de8465421f45cd362e82c6c1dbfdd8631ed` на финальной проверке 2026-10-16. Во время планирования существовавшие исходные изменения были включены во внешний коммит af594de; эта задача исходники не меняла. Подтверждены те же точки входа в актуальном исходном дереве. Номера строк — навигационные ориентиры на эту дату, перед execution сверять заново. Обязательные документы прочитаны: AGENTS/CLAUDE/EngineRequirements; источник игрового боя — Phase Four Module Combat.

- `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11` — `public sealed record AuthoritativeSnapshot`.
- `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10` — `public interface IGameSessionConnection`.
- `src/DeepSpaceSaga.Engine/SimulationEngine.cs:17` — `public sealed partial class SimulationEngine`.
- `src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs:8` — `private void AppendCombatEvent`.
- `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs:146` — `CombatEventType.Launch`.
- `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:66` — `public sealed record GameStateData`.
- `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs:92` — `internal static ScenarioFile ValidateAndNormalize`.
- `src/DeepSpaceSaga.Engine.LocalClient/SnapshotMailbox.cs:14` — `internal void Publish`.
- `src/DeepSpaceSaga.Client/SnapshotBuffer.cs:81` — `public void Update`.
- `src/DeepSpaceSaga.Client/GameSessionHandle.cs:295` — `ReadSnapshotsAsync`.
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:1348` — `_objectInfoPanel.Render`.
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs:359` — `public void Render`.
- `src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj:264` — `Data\**\*.json`.
- `src/DeepSpaceSaga.Client/Data/Locale/Russian.json:1` — `{`.
- `src/DeepSpaceSaga.Client/Data/Locale/English.json:1` — `{`.

Дополнительные наблюдения из исходников:

- `SimulationEngine.CombatJournal.cs` хранит список фактов и присваивает EventId; в текущем enum нет range-entry. `SimulationEngine.cs` публикует полный CombatJournal, что не следует путать с будущими report deltas.
- `SimulationEngine.Combat.cs` пишет Hit, ModuleHit, ModuleDestroyed, ShipDestroyed для одного контакта; итоговый доклад следует выпускать после полного применения этого контакта.
- `SimulationEngine.cs` содержит CrewMemberRuntime(Id, DisplayName, AccuracySkill, ManeuverabilitySkill), без portrait. CaptainPortraitImage есть отдельно. Отсутствующие данные не следует достраивать новым NPC.
- `SnapshotMailbox` удерживает latest snapshot и сливает недоставленные CommandResults/ShipEvents/DialogueEvents с EventLimit4096; будущие доклады нужно включить явно.
- `SnapshotBuffer.Update` заменяет latest snapshot под синхронизацией; накопитель report deltas нужен до Render. `GameSessionHandle` передаёт receive snapshots в Buffer.Update.
- `ObjectInfoPanel.Render` ограничивает высоту и ширину; сейчас общий доступный reports Bounds не выделен. Parent screen рисует journal и Object Info отдельно.
- Существующая локализация Russian/English статическая по настройке при bootstrap; live language switching не вводится.
- Root save version сейчас17. Новый optional child state Version1 — техническое A07, не утверждение о текущей реализации.

Существующий `src/graphify-out/graph.json` дал навигационные узлы CombatJournalPanel/ObjectInfoPanel/CombatEffectStore и связь GameSessionScreen, но старые source_location отличаются от текущих. Исходники проверены непосредственно; граф не перестраивался.

## Планируемые документы и ресурсы

Спецификация — единственный полный текст продуктового контракта; stories/tickets ссылаются на R/E/A ID. Assets будут в `src/DeepSpaceSaga.Client/Images/UI/CombatReports`, visual catalog — `Data/UI/combat-report-visuals.json`. Реальные PNG сейчас не созданы. [Native](evidence/NativeAcceptance.md), [Performance](evidence/Performance.md), [planning audit](PlanningValidation.md) различают будущую приёмку и проверку плана.

Дополнительные точки интеграции: `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:292` — OnRender и `:351` — screen.Render; `src/DeepSpaceSaga.Client/UI/Screens/IScreen.cs:6` — IScreen не содержит Update. Подготовку докладов вызвать перед screen.Render через новый internal метод GameSessionScreen, на UI thread. `SimulationEngine.cs:3421` — AdvanceMotionTo имеет быстрый путь; оценка E01 должна охватывать и его, и физические подшаги.
