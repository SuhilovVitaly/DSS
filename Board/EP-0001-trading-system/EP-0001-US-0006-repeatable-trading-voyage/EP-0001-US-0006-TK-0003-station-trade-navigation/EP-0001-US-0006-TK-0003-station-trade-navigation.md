---
epic: EP-0001-trading-system
story: EP-0001-US-0006-repeatable-trading-voyage
ticket: EP-0001-US-0006-TK-0003-station-trade-navigation
title: Повторяемый переход Station → Trade → полёт
stage: approved
layer: client
depends_on: [EP-0001-US-0006-TK-0002-trade-visit-context]
files_touched: 5
serves: [AC-01, AC-02, AC-05, AC-06]
created: 2026-09-21T11:05:34Z
revision: 1
---

# Повторяемый переход Station → Trade → полёт

## Why

Старый Station/Trade не должен открываться или оставаться рабочим после ухода. После прибытия нужен тот же существующий путь Station → Trade с новым контекстом; снятие вложенных экранов не должно случайно возобновлять время до закрытия последнего modal.

## Decisions

Отдельных пользовательских решений нет; точный запрос и UTC записаны в story. Основной UI ухода принадлежит US-0014, здесь интеграция с торговыми окнами.

## Assumptions

- TK-0002 предоставляет TradeModel.ResolveLocalStationId, TradeScreen.HasValidVisit/OpenedForStationObjectId и защиту quote lifecycle.
- US-0014 уже обрабатывает Undock, destination selection, отказ и возврат к полёту. Сохраняется его transition handler; второй handler/state machine не добавляется.
- Сторонние модальные окна (GameMenu/Save/Dialogue/Finance и др.) не выталкивать автоматически. Когда Station/Trade снова становится верхним, проверка удаляет его при устаревшем контексте до следующего input/render.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/SkiaWindow.cs | PollGameSessionAutoTransition:338–351; Push/PopModalAsync:796–828; OpenStation/NavigateToStation/OpenTrade:885–917 | Подключить общий station-context guard и serialized cleanup через существующие modal methods |
| src/DeepSpaceSaga.Client/UI/Screens/Station/StationScreen.cs | Constructor stores buffer/session; Trade hit:128–129 без docking guard; Undock placeholder:95 будет заменён prerequisite US-0014 | Зафиксировать ID/время исходной стоянки, gate Trade click/hover; сохранить реализованный US-0014 Undock |
| src/DeepSpaceSaga.Client/UI/StationTradeNavigation.cs | Новый helper; ScreenStack.Current/Pop/AllBottomToTop уже существуют в UI/ScreenStack.cs:8–61 | Headless policy/controller удаления только invalid верхних Station/Trade с async pop callback |
| tests/DeepSpaceSaga.Client.Tests/StationScreenTests.cs | Trade click/hover fixtures без snapshot: StationScreenTests.cs:106–136; controllable speed pattern ModalTransitionTests.cs:19–51 | Обновить click/hover fixtures на реальную Docked projection; добавить helper/ScreenStack/gated callbacks tests |
| tests/DeepSpaceSaga.Client.Tests/RepeatableTradingVoyageUiTests.cs | Новый файл; existing screen tests используют headless input/SnapshotBuffer | Два UI round trips с настоящими StationScreen/TradeScreen/handle и управляемым connection, плюс smoke checklist evidence |

Matching test project: `D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj`.

## Public API after the change

No public API change. Internal seam в StationScreen:
`string? OpenedForStationObjectId { get; }`, `long? OpenedAtPortFeeGameTimeMs { get; }`, `bool HasValidVisit { get; }`.

Internal `StationTradeNavigation` в новом файле:
- `static bool CanOpen(AuthoritativeSnapshot? snapshot)` — использовать predicate TradeModel.ResolveLocalStationId != null из TK-0002;
- `Task RemoveInvalidOverlaysAsync(ScreenStack stack, Func<AuthoritativeSnapshot?> latest, Func<Task> popModalAsync)` — на UI path, single in-flight operation, повторные вызовы во время ожидания не запускают второй Pop.

Station context validity: совпадают текущий station ID и FirstPortFeeGameTimeMs из PortFees с captured значениями, player жив/Docked, market принадлежит ему, ActiveVoyage не Undocking/InTransit/Docking. Для обычных legacy snapshots без PortFees сохранить проверку station ID; полноценный US-0014 обязан проецировать timestamp новой стоянки. Trade.HasValidVisit дополнительно проверяет epoch/lifetime TK-0002.

## Implementation steps

1. StationScreen конструктор фиксирует исходный local station ID и timestamp стоянки из buffer. Перед Trade hit/hover проверить HasValidVisit и актуальный market; invalid => no OpenTrade и no enabled affordance. Не закрывать Station из собственного synchronous input handler с прямым SetSpeed: cleanup управляет composition root. Other buttons и US-0014 Undock не переписывать.
2. В SkiaWindow OpenStationAsync/OpenTradeAsync до Push проверить latest context; перед открытием Trade current должен быть валидный StationScreen той же стоянки. Повтор события при уже открытом Trade не создаёт второй экземпляр. После await первой Pause повторно проверить тот же session handle и context, чтобы поздний event A не открыл окно в полёте/B; если window не был pushed, вернуть предыдущее speed один раз через существующий modal lifecycle. Для этого разрешено расширить private PushModalAsync optional validation callback, сохранив все старые callers/default behavior.
3. NavigateToStationAsync сначала снимает текущий nested Trade штатным Pop. Затем повторно читает snapshot: если existing Station валиден — оставить; если нет — cleanup, не вызывать OpenStation по старому ID. Новый Station разрешено открыть только для актуальной Docked station. При закрытии Trade прежний экран не может принять input раньше этой проверки.
4. Helper по latest snapshot проверяет верхний screen: invalid Trade либо Station => await popModalAsync, затем повторно проверяет новый верхний. Valid или сторонний screen => stop. Он не вызывает ScreenStack.Pop напрямую, не меняет engine speed и не сохраняет snapshot через await. Single-flight guard сбрасывать finally; errors проходят existing session failure path, без бесконечного retry. Пока cleanup выполняется, composition root не принимает повторный station/trade transition; next frame может только присоединиться к текущей задаче.
5. Подключить cleanup в существующий polling и после modal close/US-0014 departure transition. Использовать одну сериализацию с уже имеющимся asynchronous screen event path. Успешный уход удаляет оставшиеся старые торговые окна, отказ оставляет валидную Station. Снять Trade → Station → GameSession: modal depth2→1→0, resume только при0. Если исходно Speed0 — остаётся0. KeepPausedAfterStationTravel и другие действующие причины паузы сохраняются.
6. Обновить существующие Trade click/hover tests в StationScreenTests.cs: положительные fixtures получают согласованные player IsDocked/DockedStationObjectId/DockedStationTrade, а snapshot=null теперь ожидает disabled. Headless tests helper должны использовать настоящий ScreenStack и screens, управляемый callback с теми же Push/Pop ordering, TaskCompletionSource для speed acknowledgement. Проверять количество Pop/SetSpeed, current screen после каждого await, duplicate transition во время ожидания и смену session. UI round-trip tests используют реальный GameSessionHandle/SnapshotBuffer/TradeJournal, click Station Trade и confirm Trade; fake connection выдаёт заранее заданные authoritative snapshots/quotes/receipts и записывает commands. Fake не является доказательством physical flight — его отдельно даёт TK-0001.
7. Провести ручной smoke после реализации dependencies: New Game с generated map, два круга A→B→A с разными исходящим/обратным товарами, существующими Undock/navigation/Dock/dialogue, nested Trade, повторным открытием и history. Проверить отсутствие старого рынка в полёте и stale auto-open после возвращения. Evidence записать в результат выполнения тикета; не создавать постоянные screenshots/QA artifacts без запроса.

## Out of scope

Новый Station/Trade/Finance экран, destination picker и lifecycle US-0014, изменения ScreenStack/Contracts/Engine/LocalClient, migration/save, layouts/локализация, pricing/route fuel. Helper не заменяет общий modal manager и не закрывает произвольные вложенные окна.

## Invariants

- Modal pause once/resume after last: Documentation/00-Process/CLAUDE.md:182–198; SkiaWindow.cs:796–828.
- Реальная торговля только в DockedStationTrade: Contracts/StationTradeSnapshot.cs:6–13.
- ScreenStack.Pop активирует predecessor: UI/ScreenStack.cs:50–61; context guard должен сработать до input stale predecessor.
- Authoritative motion/commands не исполняются Client: CLAUDE.md:93–101. UI fake tests не выдаются за Engine integration evidence.

## Tests

StationScreenTests (новые проверки рядом с существующими):
- `Trade_cannot_open_from_stale_station_click_in_flight` — AC-02/05.
- `Departure_removes_trade_and_station_with_one_final_resume` — AC-06.
- `Rejected_departure_preserves_station_and_pause` — AC-06.
- `Arrival_and_return_open_only_current_station_market` — AC-05.
- `Duplicate_cleanup_while_resume_pending_does_not_pop_twice` — AC-06.
- `Context_or_session_change_during_pause_does_not_push_stale_window` — AC-02/06.
- `Unrelated_modal_is_preserved_until_invalid_station_is_exposed` — AC-06.
- `Initially_paused_session_remains_paused_after_cleanup` — AC-06.

RepeatableTradingVoyageUiTests:
- `Two_round_trips_reuse_station_trade_flow_without_old_quotes` — AC-01/05; UI interaction only, backed by TK-0001 physical proof.
- `Cargo_credits_and_history_follow_current_snapshots_and_receipts` — AC-05.
- `Trade_close_returns_to_current_station_without_resuming_nested_modal` — AC-06.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~RepeatableTradingVoyageUiTests|FullyQualifiedName~TradeVisitContextTests|FullyQualifiedName~ModalPauseTests|FullyQualifiedName~ModalTransitionTests|FullyQualifiedName~StationScreenTests|FullyQualifiedName~GameSessionDockAutoOpenTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Steps выполнены только в пяти разрешённых файлах; dependency Undock handler сохранён и используется.
- Все served criteria покрыты named tests; TK-0001 physical proof и ручной smoke завершены. Не проведённый smoke явно оставляет этот пункт открытым.
- Tests/build/format проходят либо конкретные исходные failures записаны отдельно; modal regression не списывается на baseline.
- API/invariants/out-of-scope соблюдены; нет duplicate screens/pops/resume, stale local market и hidden changes.
- Нет незаписанных assumptions/блокирующих вопросов; поведение проверяется по commands, screen stack и фактическому UI проходу.

## Self-containment check

Все точки подключения, validity predicate, lifetime bindings, async ordering и test seams описаны. Пять файлов включают helper и оба test files; не нужен новый screen/event enum или переписывание общего ScreenStack. External lifecycle/quote contracts заданы в story и предыдущих tickets.
