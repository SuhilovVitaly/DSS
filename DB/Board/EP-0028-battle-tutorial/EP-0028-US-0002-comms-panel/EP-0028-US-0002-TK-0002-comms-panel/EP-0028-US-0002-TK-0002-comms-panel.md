---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0002-comms-panel
ticket: EP-0028-US-0002-TK-0002-comms-panel
title: Панель связи
stage: approved
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0028-US-0001-TK-0004-screen-integration, EP-0028-US-0002-TK-0001-ui-texts]
implementation_files: 2
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialCommsPanel.cs
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSession.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialCommsPanelTests.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialScreenIntegrationTests.cs
serves: [US-0002/AC-02]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Панель связи

[История](../EP-0028-US-0002-comms-panel.md) · [Эпик](../../Documentation.md)

## Цель

Нарисовать текущую реплику директора в компактной панели у нижнего края поля: портрет офицера, имя и должность, до двух строк текста, кнопки «Понял» и «Подсказки: выкл». Без реплики — свёрнутый значок. Панель ловит только свои клики.

## Decisions

D-01 (говорит только офицер «Скитальца»), D-02, D-06 — [эпик](../../Documentation.md#решения-пользователя-обязательные). A-03, A-06.

## Assumptions

- Геометрия в дизайн-координатах 1280×720 (`BattleViewport`, `BattleDesign.cs:8-11`): развёрнутая панель 460×100, левый нижний угол (206, 714); ширина ограничена `designWidth - 246 - 206` (правая панель начинается в `Wd - 240`, `TacticalBattleScreen.cs:118`; колонка пилотов кончается на x=196, `PilotColumnLayout.cs:9`). Свёрнутый значок 190×30 в том же углу.
- Портрет 72×72 слева; область текста шириной 352 (константа `TutorialCommsPanel.TextWidth`); шрифт — `BattleTypography.TextPaint(13, false)` (`BattleDesign.cs:92-95`), перенос — `ControlText.Wrap` (`UI/Controls/DropDown.cs:298-320`, применяет `SafeSymbols`). Если текст длиннее двух строк — вторая строка обрезается `ControlText.Ellipsize` (контент-тесты US-0003 не допускают этого в поставке).
- Портрет: `DialoguePortraitComposer.Compose(script.Officer.Portrait, PersonSex.Female, script.Officer.PortraitKey)` (`UI/Portraits/DialoguePortraitComposer.cs:25-55`, результат 300×300) вызывается один раз в конструкторе сессии (переход окна, не кадр отрисовки); `Release()` освобождает bitmap, следующий `Draw` создаёт заново. `null` (нет officer, нет головы, ошибка) — заглушка: рамка `BattleDesign.PortraitBg` и первая буква локализованного имени.
- Кнопка «Подсказки: выкл» в этом тикете выключает `Director.Enabled` в памяти и сворачивает панель в значок «Подсказки: вкл», клик по которому включает обратно (A-03). Сохранение — US-0004-TK-0003.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialCommsPanel.cs` | Нет файла. Примитивы: `BattleDesign.Round/T/Measure` (`BattleDesign.cs:92-110`), цвета `Panel/PanelEdge/Text/Muted/PortraitBg` (`:76-83`) | Новый класс отрисовки и hit-test |
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSession.cs` | После US-0001-TK-0004: пустые `Bounds`, хуки false | `Bounds` = прямоугольник панели/значка; `Draw` рисует панель; `OnMouseDown` маршрутизирует «Понял»/«выкл»/«вкл»; композиция портрета |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialCommsPanelTests.cs` | Нет файла | Новые тесты |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialScreenIntegrationTests.cs` | Из US-0001-TK-0004 | Тест непересечения в живом Tabletop-бою |

Ключи текста: `Tutorial.Panel.Title/Acknowledge/HintsOff/HintsOn/JournalHint` (US-0002-TK-0001); имя/должность — `script.Officer.NameKey/RoleKey` через `Localization.Get` (`Localization.cs:16-18`).

## Public API after the change

```csharp
internal sealed class TutorialCommsPanel
{
    internal const float Width = 460, Height = 100, TextWidth = 352, Left = 206, Bottom = 714;
    internal static ImmutableArray<string> Wrap(string text);           // перенос в TextWidth тем же шрифтом
    internal SKRect Bounds { get; }                                      // развёрнутая панель или значок
    internal SKRect AcknowledgeBounds { get; }
    internal SKRect HintsBounds { get; }
    internal void Draw(SKCanvas c, BattleLayout layout, float designWidth, TutorialLine? line, bool hintsEnabled, SKBitmap? portrait, TutorialOfficer? officer);
    internal string? Hit(SKPoint designPoint);                           // "ack" | "hints-off" | "hints-on" | null
}
```

Ключи `Layout.Rects`: `tutorial:panel`, `tutorial:ack`, `tutorial:hints`, `tutorial:chip`.

## Implementation steps

1. `TutorialCommsPanel.Draw`: при `line != null && hintsEnabled` — развёрнутая панель (портрет, «Имя · должность», `Wrap(Localization.Get(line.Step.LineKey))` до 2 строк, кнопки); иначе — значок: при `hintsEnabled` «СВЯЗЬ · H — журнал связи», при выключенных — «Подсказки: вкл». Запись ключей в `layout.Rects`.
2. `TutorialSession`: хранит панель и портрет; `Bounds` возвращает `panel.Bounds`; `OnMouseDown` (левая кнопка) — `ack` → `Director.Acknowledge()`, `hints-off` → `Director.Enabled = false`, `hints-on` → `true`; true только при попадании в панель.
3. Тесты рендера: растр 1280×720 в Tabletop, RU и EN.

## Acceptance criteria

1. Развёрнутая панель показывает портрет или заглушку, имя, реплику (≤2 строк) и две кнопки на текущем языке.
2. Прямоугольник панели не пересекает `pilot:*`, `pilot-panel:*`, `phase`, `minimap`, `hover`, `action` в фазах Move, Fire, Salvo/Results и Outcome Tabletop-боя 1280×720 и в окне 2560×1080 (дизайн-ширина ≈1707).
3. Клик в панели не доходит до карты/карточек; клик вне панели ведёт себя как без сессии.
4. «Понял» закрывает реплику; «Подсказки: выкл» сворачивает в «Подсказки: вкл»; повторный клик возвращает подсказки.
5. Отсутствие портрета не роняет отрисовку.

## Verification signal

Первым красным — `TutorialCommsPanelTests.ExpandedPanelShowsOfficerLineAndButtonsInCurrentLanguage` (RU и EN, проверка `Layout.Texts` и ключей `tutorial:*`). Затем `PanelNeverOverlapsPilotCardsPhasePanelOrMinimap` (живой бой, `TutorialScreenIntegrationTests`), `ClicksInsidePanelAreCapturedOutsideAreNot`, `AcknowledgeAndHintsButtonsDriveTheDirector`, `MissingPortraitDrawsPlaceholder`.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0002-tk0002-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial|FullyQualifiedName~TutorialFlowTests" --results-directory "$RUN" --logger "trx;LogFileName=panel.trx"
```

Тесты, переключающие язык, — в `[Collection("Battle localization")]`. Process-tree cap 300000 ms.

## Out of scope

Подсветка цели (TK-0003), тишина/сворачивание по таймеру (TK-0004), журнал (TK-0005), сохранение переключателя (US-0004), правки `UI/Screens/TacticalBattle/`.

## Invariants

- Код только в `UI/Screens/Tutorial/` — ForbiddenCalls не затрагивается.
- Новых бинарных ассетов нет (A-06).
- Без сессии экран идентичен базе.

## Known fallout

Нет: хуки ввода и `OverlayRects` уже подключены в US-0001-TK-0004.

## Definition of Done

- Шаги выполнены в разрешённых файлах; named tests зелёные в RU и EN; сборка Client без новых предупреждений.
- Отдельный коммит и `git push`.

## Self-containment check

Геометрия, шрифт, перенос, источник портрета, ключи текста и ключи `Layout.Rects` заданы явно.

## Журнал

2026-11-14T15:04:43Z — тикет создан.
