---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0004-hints-toggle
ticket: EP-0028-US-0004-TK-0002-settings-row
title: Строка подсказок в Настройках
stage: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0028-US-0004-TK-0001-hints-preference, EP-0028-US-0002-TK-0001-ui-texts]
implementation_files: 3
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/Settings/SettingsScreen.cs
  - src/DeepSpaceSaga.Client/UI/Screens/Settings/SettingsLayout.cs
  - src/DeepSpaceSaga.Client/UI/SkiaWindow.cs
  - tests/DeepSpaceSaga.Client.Tests/SettingsScreenTests.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialSettingsIntegrationTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SkinBattleIntegrationTests.cs
  - tests/DeepSpaceSaga.Client.Tests/ScreenEventTests.cs
serves: [US-0004/AC-02]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Строка подсказок в Настройках

[История](../EP-0028-US-0004-hints-toggle.md) · [Эпик](../../Documentation.md)

## Цель

В Настройках главного меню появляется компактная строка-переключатель «ПОДСКАЗКИ ОБУЧЕНИЯ»; изменение сразу сохраняется в `Settings.json`.

## Decisions

D-04 — переключатель в Настройках главного меню, без уровней ([эпик](../../Documentation.md#решения-пользователя-обязательные)).

## Assumptions

- Строка — существующий `Toggle` (флажок + подпись, `UI/Controls/Toggle.cs:7-69`) шириной поля скина (`XenonComboBox.NativeWidth` 355, высота 26), подпись `Settings.TutorialHints`; при ошибке записи — сообщение `Settings.TutorialHints.SaveFailed` под строкой, флажок возвращается.
- Строка есть только во внутреннем конструкторе со скинами (его использует окно, `SkiaWindow.cs:1377-1381`); публичный конструктор и `SettingsLayout.ExitY == 396` (`SettingsScreenTests.cs:31-36`, `ScreenEventTests.cs:291`) не меняются.
- Геометрия: внутри панели 500×550 (`SettingsLayout.cs:25-26`), `SkinExitY` ≤ 459, чтобы выход оставался в окне 640×480 (`SettingsScreenTests.cs:104-115`: `exitY + ButtonHeight <= height`, верх панели при 480 равен −35). Свободного места между полем скина (388+34) и выходом (455) мало, а сообщение об ошибке скина занимает `field.Bottom+3…+25` (`SettingsScreen.cs:335-337`), поэтому допустимо сжать вертикальные интервалы строк (`LanguageRowY`, `UiScaleRowY`, `SkinRowY`) — без пересечений меток, полей, сообщений об ошибке и списков.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Settings/SettingsScreen.cs` | Внутренний конструктор со скинами (`:86-111`), `OnMouseDown` (`:159-244`), `OnMouseMove` (`:249-279`), `Render` (`:290-324`), строка скина (`:326-338`) | Необязательные параметры `bool tutorialHints = true, Func<bool, bool>? trySetTutorialHints = null`; строка отрисовывается при наличии колбэка; клик переключает; свойство `TutorialHintsSaveFailed` |
| `src/DeepSpaceSaga.Client/UI/Screens/Settings/SettingsLayout.cs` | `SettingsButton` (`:6-14`), константы строк (`:32-53`), `SkinField` (`:65-69`), `HitTest` (`:74-88`) | `SettingsButton.TutorialHints`; `TutorialHintsRowY`; `TutorialHintsField(width, height)`; hit-test; при необходимости сжатие интервалов (см. Assumptions) |
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | `OpenSettingsAsync` (`:1353-1382`), `TrySelectBattleSkin` (`:1384-1391`) | Передать `TutorialHintsPreference.Read(SettingsFilePath, LogTutorialWarning)` и `TrySetTutorialHints` (`TutorialHintsPreference.TryWrite`) |
| `tests/DeepSpaceSaga.Client.Tests/SettingsScreenTests.cs` | Геометрия и рендер (`:19-115`) | Тесты строки; при сжатии интервалов — обновить ожидания геометрии без ослабления проверок непересечения |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialSettingsIntegrationTests.cs` | Нет файла. Образец профиля и маршрута окна — `SkinBattleIntegrationTests.Profile` и `OpenSettings` (`TacticalBattle/SkinBattleIntegrationTests.cs:177-270`) | Новые тесты через `SkiaWindow` |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SkinBattleIntegrationTests.cs` | Клики по `SettingsLayout.SkinField`/`LanguageRowY` (`:186-197`) | Только если сжатие интервалов меняет расчёт кликов; иначе регрессионный запуск |
| `tests/DeepSpaceSaga.Client.Tests/ScreenEventTests.cs` | Клик «Выход» по `SettingsLayout.ExitY` (`:248-291`) | Только регрессия (ExitY не меняется) |

## Public API after the change

```csharp
// SettingsScreen (internal ctor с новыми необязательными параметрами)
internal SettingsScreen(..., IReadOnlyList<(string Id, string DisplayName)> skins, string effectiveSkinId, Func<string, bool> trySelectSkin,
    bool tutorialHints = true, Func<bool, bool>? trySetTutorialHints = null);
internal bool TutorialHintsEnabled { get; }
internal bool TutorialHintsSaveFailed { get; }
// SettingsLayout
internal const float TutorialHintsRowY;   // значение выбирается по Assumptions
internal static SKRect TutorialHintsField(int width, int height);
public enum SettingsButton { ..., TutorialHints }
```

## Implementation steps

1. Геометрия и hit-test строки; проверить непересечение со всеми строками, сообщениями и выходом при 640×480 и 1280×720.
2. Отрисовка `Toggle` и обработка клика/ховера; при открытом списке клик по строке не обрабатывается (как прочие строки).
3. Колбэк `trySetTutorialHints(value)`: `false` → вернуть флажок и показать сообщение.
4. `SkiaWindow.OpenSettingsAsync` передаёт текущее значение и колбэк.

## Acceptance criteria

1. Строка видна в Настройках главного меню на RU и EN, флажок отражает `Settings.json`.
2. Клик меняет значение и сохраняет его; прочие ключи профиля сохраняются.
3. Ошибка записи показывает сообщение и не меняет флажок.
4. Строка не пересекается с другими элементами; выход доступен при 640×480; `ExitY` публичного пути = 396.

## Verification signal

Первым красным — `SettingsScreenTests.Tutorial_hints_row_fits_between_skin_row_and_exit_without_overlap` (640×480 и 1280×720); затем `Tutorial_hints_click_toggles_and_failed_save_restores`, `TutorialSettingsIntegrationTests.MainMenuSettingsTogglePersistsHintsAndKeepsOtherSettings`.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0004-tk0002-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~SettingsScreenTests|FullyQualifiedName~TutorialSettingsIntegrationTests|FullyQualifiedName~SkinBattleIntegrationTests|FullyQualifiedName~ScreenEventTests|FullyQualifiedName~LocalizationCoverageTests" --results-directory "$RUN" --logger "trx;LogFileName=settings.trx"
```

Process-tree cap 300000 ms.

## Out of scope

Кнопка в бою (TK-0003); новые строки локали (уже US-0002-TK-0001); настройки в игровом меню мира.

## Invariants

- Публичный конструктор и `ExitY` неизменны.
- `UI/Screens/Settings/` вне `/TacticalBattle/`; `SkiaWindow.cs` вне сканирования ForbiddenCalls.

## Known fallout

`SettingsScreenTests` (геометрия 640×480), `SkinBattleIntegrationTests` (клики по строкам настроек), `ScreenEventTests` (выход) — в allowlist заранее.

## Definition of Done

- Шаги выполнены; named tests зелёные; растровые снимки Настроек RU/EN в `$RUN`.
- Отдельный коммит и `git push`.

## Self-containment check

Ограничения геометрии, контрол, ключи, точки вставки и затронутые тесты заданы с `path:line`.

## Результат

- `SettingsLayout.cs`: `SettingsButton.TutorialHints`; `TutorialHintsRowY = 402`, `TutorialHintsRowHeight = 26`; `TutorialHintsField(width, height)` — ширина и центр поля скина; `HitTest(..., hasSkinPicker, hasTutorialHints = false)` распознаёт строку. Интервалы строк под заметкой о перезапуске сжаты до 4 px: `LanguageRowY` 229 → 216, `UiScaleRowY` 312 → 278, `SkinRowY` 388 → 340. Итоговая вертикаль панели (локальные Y): монитор 100–158, заметка ≈168–185, язык 192–250, масштаб 254–312, скин 316–374, ошибка скина 377–399, подсказки 402–428, ошибка подсказок 431–453, выход 455–511. `ExitY` (396) и `SkinExitY` (455) не менялись.
- `SettingsScreen.cs`: внутренний конструктор со скинами получил `bool tutorialHints = true, Func<bool, bool>? trySetTutorialHints = null`; строка (существующий `Toggle`, подпись `Settings.TutorialHints: Settings.TutorialHints.On|Off`) есть только при колбэке; левый клик переключает и сразу сохраняет; `false` от колбэка — флажок не меняется, под строкой `Settings.TutorialHints.SaveFailed`; при открытом списке клик уходит списку (как у прочих строк); ховер подсвечивает строку. Свойства `TutorialHintsEnabled`, `TutorialHintsSaveFailed`. Публичный конструктор строку не показывает.
- `SkiaWindow.cs` (`OpenSettingsAsync`): передаёт `TutorialHintsPreference.Read(SettingsFilePath, …)` и `TrySetTutorialHints` → `TutorialHintsPreference.TryWrite`; сообщения — в InterfaceLog с префиксом `TUTORIAL: `.

Тесты: `SettingsScreenTests.Tutorial_hints_row_fits_between_skin_row_and_exit_without_overlap` (640×480, 1280×720: попарное непересечение меток+полей всех строк, заметки, двух сообщений об ошибке и выхода; всё в окне; hit-test строки/скина/выхода; `ExitY == 396`), `Tutorial_hints_click_toggles_and_failed_save_restores`; `Tutorial/TutorialSettingsIntegrationTests.MainMenuSettingsTogglePersistsHintsAndKeepsOtherSettings` (через `SkiaWindow`: профиль без значения → вкл; клик → `false` в файле, прочие ключи `language/uiScale/tacticalBattleSkinId/tacticalMap/unrelated` без изменений; повторное открытие показывает `false`; обратно `true`; строка отрисована в RU и EN).

Проверки 2026-11-14 (артефакты `D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0004-tk0002-20261114T193953/`, `DSS_VALIDATION_ROOT` = папка прогона):

- Красный старт: компиляция тестов падает на отсутствующих `SettingsButton.TutorialHints`, `TutorialHintsField`, `TutorialHintsEnabled`, `hasTutorialHints`, `tutorialHints` — `red/red.log`.
- `dotnet test … --filter "FullyQualifiedName~SettingsScreenTests|FullyQualifiedName~TutorialSettingsIntegrationTests|FullyQualifiedName~SkinBattleIntegrationTests|FullyQualifiedName~ScreenEventTests|FullyQualifiedName~LocalizationCoverageTests"` — **111/111** — `settings.trx`, `settings.log`. `SkinBattleIntegrationTests` и `ScreenEventTests` прошли без правок (клики считаются от констант).
- Растровые снимки Настроек: `settings-russian.png`, `settings-english.png` (1280×720), `settings-hints-off.png`.
- Правок в `/TacticalBattle/` нет.

## Журнал

2026-11-14T15:04:43Z — тикет создан.

2026-11-14 — реализовано (TDD), `stage: implemented`. Узкие решения исполнителя: (1) два состояния показаны подписью `ПОДСКАЗКИ ОБУЧЕНИЯ: Включены/Выключены` (ключи `Settings.TutorialHints.On/Off` из US-0002-TK-0001) рядом с флажком; (2) сжатие интервалов действует и для публичного конструктора (строки языка и масштаба выше на 13 и 34 px) — его выход (`ExitY` 396) и геометрия проверок не изменились; (3) Space/Enter для строки не добавлялись (в Настройках нет фокуса клавиатуры у строк). `SkinBattleIntegrationTests.cs` и `ScreenEventTests.cs` не менялись — только регрессия.
