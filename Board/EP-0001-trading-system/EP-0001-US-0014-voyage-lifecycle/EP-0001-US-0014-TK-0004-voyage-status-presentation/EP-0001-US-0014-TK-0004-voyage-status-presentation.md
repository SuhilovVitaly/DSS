---
epic: EP-0001-trading-system
story: EP-0001-US-0014-voyage-lifecycle
ticket: EP-0001-US-0014-TK-0004-voyage-status-presentation
title: Статус рейса на GameSession
stage: approved
layer: client
depends_on: [EP-0001-US-0014-TK-0001-voyage-contract, EP-0001-US-0014-TK-0002-authoritative-voyage-lifecycle]
files_touched: 2
serves: [AC-03, AC-04, AC-06, AC-08]
created: 2026-09-21T14:55:44Z
revision: 1
---

# Статус рейса на GameSession

## Why

Сделать active voyage наблюдаемым вне Station modal. End state: существующая info panel показывает authoritative phase, destination, progress и понятный blocker после ухода, во время docking и после отклонённой попытки, не предсказывая gameplay-state между snapshots.

Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Пути относительно `D:/DeepSpaceSaga/DSS`.

## Decisions

Точное сообщение пользователя зафиксировано в story; отдельных решений нет.

## Assumptions

Для MVP используется существующая top-left info panel и её `BuildPanelLines`, а не новый modal/HUD framework. Phase constants отображаются как `Docked`, `Undocking`, `In transit`, `Docking`; progress — целый процент с одной десятичной точностью из permille.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs | :2019–2068 рисует и строит info-panel lines только из buffered snapshot/camera | Добавить pure voyage line/reason formatting и строки из `snapshot.Voyage` |
| tests/DeepSpaceSaga.Client.Tests/InfoPanelTests.cs | :207–235 проверяет layout и `BuildPanelLines` | Synthetic snapshot tests всех phases/progress/reasons/legacy null |

## Public API after the change

No public API change. Разрешён internal test seam в `GameSessionScreen`:

```csharp
internal static string VoyageReasonText(string? reasonCode);
```

`BuildPanelLines(BufferedSnapshot?)` остаётся существующей сигнатурой.

## Implementation steps

1. Если `snapshot.Voyage` null, не добавлять voyage lines — legacy snapshots сохраняют текущую панель.
2. Для active `Undocking/InTransit/Docking` добавить `Voyage`, `Destination`, `Progress`; destination берётся только из snapshot display name/ID fallback, progress clamp для presentation `0..1000` без записи обратно.
3. Для `Docked` без blocker route-status lines не добавлять, чтобы не перегружать панель. При non-null `BlockReasonCode` добавить `Departure` с понятным текстом даже после немедленного закрытия Station modal.
4. Map six TK-0001 reason codes: destination required/unavailable, already active, wrong destination, outstanding debt, insufficient fuel. Unknown nonblank code показывать как `Departure unavailable (<code>)`, null/blank — `Departure unavailable`; diagnostic detail не терять.
5. Не интерполировать progress по render time, ETA или ship coordinates. Новый authoritative snapshot заменяет строки напрямую; motion predictor остаётся только визуальным.

## Out of scope

Route selection, button dispatch, progress calculation, notification/toast framework, localization files, Finance/TradeJournal, map marker/path drawing и изменение panel positioning beyond its existing dynamic line count.

## Invariants

- Renderer читает только client memory и snapshot; unconfirmed commands не меняют prediction: `CLAUDE.md:96–103`.
- Existing panel is built from `BufferedSnapshot` and rendered line-by-line: `GameSessionScreen.cs:2019–2068`.
- Voyage status is presentation only; `ObjectMotionSnapshot`/ApproachRoute and `_renderStates` не меняются.
- Unknown authoritative reason не скрывается fallback-текстом без code.

## Tests

В `InfoPanelTests`:

- `Active_voyage_lines_show_phase_destination_and_authoritative_progress` (Theory Undocking/InTransit/Docking; AC-03/04/08).
- `Docked_without_blocker_keeps_panel_compact`.
- `Docked_rejection_shows_each_known_departure_reason` (Theory; AC-06).
- `Unknown_reason_keeps_machine_code_visible` (AC-06).
- `Legacy_null_voyage_keeps_existing_panel_lines`.
- `Presentation_clamps_malformed_progress_without_mutating_snapshot`.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~InfoPanelTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все implementation steps выполнены только в двух разрешённых файлах.
- AC-03/04/06/08 покрыты named line/reason/legacy tests.
- Named tests, matching layer build и format проходят либо конкретный baseline failure записан отдельно.
- UI не вычисляет lifecycle/progress и не меняет motion/prediction; unknown reason остаётся диагностируемым.
- Нет скрытых файлов, нового screen framework или незаписанных assumptions; результат проверяем `BuildPanelLines` и render smoke.

## Self-containment check

Место UI, условия показа, exact labels/format/reason mapping и tests определены. Для реализации не нужен доступ к Engine, save или дополнительное продуктовое решение.
