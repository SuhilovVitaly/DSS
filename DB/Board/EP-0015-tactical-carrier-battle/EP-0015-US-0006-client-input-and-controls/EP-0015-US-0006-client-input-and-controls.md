---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0006-client-input-and-controls
title: "Ввод и элементы интерфейса для экранов боя"
stage: in-progress
dependencies: [EP-0015-US-0005-session-contracts]
---

# Ввод и элементы интерфейса для экранов боя

## Связь с техническим заданием

Источник правил, данных и эталонов — `VData/Planning/V4.0` (Spec, Data, Reference). Решения и gaps — [Documentation.md](../Documentation.md). Требования ниже — целевой результат, не утверждение о реализации.

## User story

Как игрок, на экранах боя я пользуюсь клавиатурой и мышью так же, как в справочной странице: Enter, Tab и Shift+Tab, стрелки с автоповтором, 0 — всё поле, колесо — масштаб, правая кнопка — сдвиг; панели используют ползунки, переключатели, вкладки, выпадающие списки и прокручиваемый журнал.

## Зависимости

[EP-0015-US-0005-session-contracts](../EP-0015-US-0005-session-contracts/EP-0015-US-0005-session-contracts.md)

## Completion evidence

- **AC-01.** `KeyboardEdgeTracker` отслеживает `F8`, `Number0`, `Keypad0`, `KeypadEnter` (и уже имеющиеся Enter, Tab, стрелки, Escape, цифры); экраны получают модификаторы (Shift, Ctrl) вместе с клавишей; стрелки повторяются при удержании; при потере фокуса (`ResyncToCurrentState`, `SkiaWindow.cs:514`) сбрасываются автоповтор, перетаскивание и сдвиг. Список клавиш не зависит от старых настроек боя.

- **AC-02.** `Data/UI/tactical-battle-input.json` (`tacticalBattle: "F8"`, задержка повтора 400 мс, период 80 мс) читается `TacticalBattleInputSettings` с проверкой: неизвестная клавиша, дубли и конфликт с клавишами игры (I, F5, F9, F10, F12 и клавиши маневрирования/debugPanel из `combat-input.json`) — понятная ошибка в InterfaceLog и значения по умолчанию.

- **AC-03.** Мышь: `IScreen.OnMouseUp(x, y, button)` — Client передаёт левую и правую кнопки; колесо: множитель масштаба `exp(−scroll.Y · K)`, K подобран так, что одна насечка Silk равна одной насечке браузера в play4 (`exp(100 · 0.0015)`); hover и перетаскивание работают на экранах боя.

- **AC-04.** Элементы: `Slider` (min/max/step 0.5, клавиши, перетаскивание), `Toggle`, `TabStrip`, `DropDown` (открытие, выбор мышью и клавишами, прокрутка длинного списка, обрезка подписи с «…» и полной подписью в подсказке), `ScrollTextPanel` (перенос слов, прокрутка колесом и ползунком, автопрокрутка вниз; прокрутка обобщена из `CombatJournalPanel.cs:72`), кнопка с подсказкой клавиши и всплывающей подсказкой (образец `StationToolbar.DrawTooltips`). Переиспользуются `ProgressBar`, `TextInputBox` (с фильтром цифр и «−»), `GenericButtonTypeA`, `GenericWindowTypeA`; `XenonComboBox` — только как отрисовщик. Учитывают `uiScale`; ширина правой панели = 400 · uiScale; минимальный размер окна и поведение при нём зафиксированы.

- **AC-05.** Шрифты экранов боя (Verdana `MenuStyle` и замена на Linux) содержат «−», «→», «·», «±», «×», «°», «≤» или их замены; числа форматируются инвариантно (точка); округление для показа — `MidpointRounding.ToEven` (Display §1). Это осознанное отличие от справочной страницы (`Math.round` — половина вверх, `play4.html:562,745`), записано в Documentation.md.

Это план будущих наблюдений, а не выполненная проверка. Automated/render, native/input и product/human acceptance фиксируются раздельно.

## Ограничения и gaps

Новые элементы — общие для Client.

## Тикеты

| Ticket | Layer | Story criteria |
|---|---|---|
| [EP-0015-US-0006-TK-0001-keyboard](EP-0015-US-0006-TK-0001-keyboard/EP-0015-US-0006-TK-0001-keyboard.md) | client | AC-01 |
| [EP-0015-US-0006-TK-0002-input-settings](EP-0015-US-0006-TK-0002-input-settings/EP-0015-US-0006-TK-0002-input-settings.md) | client | AC-02 |
| [EP-0015-US-0006-TK-0003-mouse](EP-0015-US-0006-TK-0003-mouse/EP-0015-US-0006-TK-0003-mouse.md) | client | AC-03 |
| [EP-0015-US-0006-TK-0004-widgets-basic](EP-0015-US-0006-TK-0004-widgets-basic/EP-0015-US-0006-TK-0004-widgets-basic.md) | client | AC-04 |
| [EP-0015-US-0006-TK-0005-widgets-lists](EP-0015-US-0006-TK-0005-widgets-lists/EP-0015-US-0006-TK-0005-widgets-lists.md) | client | AC-04 |
| [EP-0015-US-0006-TK-0006-text-and-numbers](EP-0015-US-0006-TK-0006-text-and-numbers/EP-0015-US-0006-TK-0006-text-and-numbers.md) | client | AC-05 |

## Historical planning review

Draft; human approval отсутствует. Решения и сообщения согласования записываются в review log эпика.

## Execution dependency addition — 2026-11-05

[EP-0015-US-0006-TK-0007-linux-native-runtime](EP-0015-US-0006-TK-0007-linux-native-runtime/EP-0015-US-0006-TK-0007-linux-native-runtime.md), client-build, AC-05; depends on TK-0005 and precedes completion of TK-0006. Required recovery from actual Ubuntu DllNotFoundException, no glyph/raster tests excluded.

## Execution review — 2026-11-05

All seven control/input implementation tickets complete, including required Linux native recovery. Number/glyph coverage PASS Windows/Linux, real handler routing verified headlessly, controls/raster verified across scales. AC-01/03 final drag/pan consumers and AC-04 right-panel/minimum-window integration remain in US-0008; physical input/native/human acceptance is not replaced by these checks. See ExecutionStatus.md/PlatformValidation.md and cards for exact source SHA and evidence. Self-review only, not independent approval.
