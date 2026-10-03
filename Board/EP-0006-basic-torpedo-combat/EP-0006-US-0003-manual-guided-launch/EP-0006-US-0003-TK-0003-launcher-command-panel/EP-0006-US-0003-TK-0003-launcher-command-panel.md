---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0003-manual-guided-launch
ticket: EP-0006-US-0003-TK-0003-launcher-command-panel
title: "Отдельная панель аппарата и кнопка Пуск"
stage: draft
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: ["EP-0006-US-0003-TK-0002-authoritative-launch"]
files_touched: 5
serves: ["AC-0002","AC-0003"]
priority: P1
created: 2026-10-02T08:22:58Z
revision: 1
validation_status: not-run
---

# EP-0006-US-0003-TK-0003-launcher-command-panel

Отдельная панель аппарата и кнопка Пуск.

## Why

Результат тикета: **Отдельная панель аппарата и кнопка Пуск**. Он нужен для истории [Ручной пуск торпеды с упреждением](../EP-0006-US-0003-manual-guided-launch.md) и покрывает указанные `serves` в своей части; полные критерии story завершаются совокупностью её тикетов.

- **AC-0002** — Выбранная цель закреплена при пуске; исключён собственный корабль. Отсутствие решения не запрещает пуск; нет дальности/таймера/самоуничтожения.
- **AC-0003** — Один аппарат сопровождает не более одной торпеды. UI и Engine блокируют повторный пуск; на паузе команда принимается без продвижения MotionTimeMs.

## Decisions

- **D01:** «нам нужно добавить новый модуль торпедного аппарата в схему Тетрарха»; «для ланчера нужна своя панель»; новое помещение — «новое», место — «выбери сам». Один launcher в новом помещении стандартного Тетрарха; отдельная command panel с кнопкой Пуск.
- **D02:** «пока она летит вторую он выпустить не может»; «после взрыва выпущенной торпеды следующая уже готова»; условной занятости — «пока условной достаточно»; боезапас — «бесконечные». Одна активная торпеда на аппарат, без реального crew assignment, расхода боезапаса и отдельного ожидания КД. Готов → Наведение → Готов; pending submit не позволяет double click.
- **D05:** «Кнопка пуск должна быть активной только при выбранной цели»; допустимая цель — «любой объект»; исключить свой корабль — «да»; сохранить цель после смены выбора — «да». Цель — существующий выбранный объект, кроме собственного корабля. TargetObjectId фиксируется при принятии launch command, hover/смена selection не перенаводит активную торпеду.
- **D06:** Полёт следует паузе/ускорению — «да»; пуск на паузе — «да». Физика следует MotionTimeMs/SimulationTimeMs. На паузе команда создаёт торпеду и busy, но не продвигает полёт; никаких UI-tick gameplay updates.
- **D07:** Ограничить дальность — «нет»; недостижимая цель — «пока этим принебрегаем потом добавим кнопку самоуничтожения». Нет range, TTL, fuel limit, проверки достижимости как условия пуска или кнопки self-destruct. Бесконечное преследование и занятый аппарат допустимы для MVP.

Полный журнал, исходный scope и определения D/A — [Documentation.md](../../Documentation.md). Не переносить ранние предложения о вероятностях/КД/дальности из черновика обратно в MVP.

## Assumptions

Применяются A01–A12 эпика в части этого тикета. Все API ниже **предлагаемые**, кроме прямо названных существующих методов. Naming/overload можно уточнить без смены семантики и allowlist. Если для решения нужен дополнительный production/test файл, сначала уточнить scope; не скрывать его за количеством `files_touched`.

Новых продуктовых решений сверх согласованного объёма этот тикет не вводит. Порядок тестов и внутренние helper names выбираются в пределах заявленного слоя.

## Code context

Полный write allowlist от корня DSS; 5 файлов, включая тесты. Другие файлы — read-only. Новые файлы из зависимостей должны реально существовать к началу исполнения.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs` | Существует: `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs:55` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Существует: `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:706` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs` | Новый файл этого тикета; на baseline отсутствует | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `tests/DeepSpaceSaga.Client.Tests/CommandsPanelSkeletonTests.cs` | Существует: `tests/DeepSpaceSaga.Client.Tests/CommandsPanelSkeletonTests.cs:1` | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |
| `tests/DeepSpaceSaga.Client.Tests/TorpedoCommandPanelTests.cs` | Новый файл этого тикета; на baseline отсутствует | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |

Контекст для чтения: [grounding эпика](../../Documentation.md), [история](../EP-0006-US-0003-manual-guided-launch.md), `Documentation/01-Requirements/EngineRequirements.md` (§2, §52, §55–57, §60), `Documentation/00-Process/CLAUDE.md`. Code context определяет границу записи, ссылки для чтения её не расширяют.

## Dependencies

- [EP-0006-US-0003-TK-0002-authoritative-launch](../../EP-0006-US-0003-manual-guided-launch/EP-0006-US-0003-TK-0002-authoritative-launch/EP-0006-US-0003-TK-0002-authoritative-launch.md)

Нужны реализованные и проверенные результаты зависимостей. При их отсутствии остановить этот implementation ticket с конкретным gap, не имитировать готовность mock-объектами и не дописывать зависимость за его пределами.

## Public API after the change

No new transport API: GameSessionHandle.SendCommandAsync(..., torpedo.fire, selectedObjectId). Новая Torpedo Launcher group с Пуск и Готов/Наведение; internal hover-command identity seam для US-0006.

## Implementation steps

1. Добавить панель в существующий CommandsPanel; показывать actual installed module capability, не выдуманный module ID. Текстовая кнопка/простой glyph допустимы без нового art-pack.
2. Включать Пуск только при valid selected non-self object, готовом launcher и отсутствии pending submit; на паузе кнопка не блокируется.
3. На click закрепить selected ID в команде. Pending disabled снимается по confirmation/rejection; UI busy не заменяет Engine guard. Не добавлять auto-fire.
4. Обеспечить доступность пятой панели в малом viewport через существующее сворачивание/адаптацию; сохранить правила других групп и обновить тесты фиксированного количества панелей.

## Out of scope

Файлы вне allowlist; production слои вне `client`; AI пирата, защита/ПР, вероятность/баланс RNG, дальность/боезапас/TTL, реальные операторы, self-destruct, area damage, salvage, звуки, victory UI и общий refactor карты. Не менять существующие Board-статусы/requirements или commit/push в рамках реализации без соответствующего поручения.

## Invariants

- Gameplay HP, projectile lifecycle и collision принадлежат Engine; UI работает через immutable Contracts/session и shared Motion (CLAUDE.md, Architecture).
- Flight time — MotionTimeMs; real-time эффекты — monotonic UI clock (SimulationClock.Update; AuthoritativeSnapshot.MotionTimeMs). Пауза не продвигает мир, но session infrastructure работает (§52).
- `TargetObjectId` фиксируется в явной команде; current selection не заменяет цель полёта (PlayerCommand, строка7).
- Owner/self исключены из контактов; первый contact применяется один раз; chance100% не расходует RNG. Размеры5px/50px UI не являются collider world radius.
- Корабль Тетрарх определяется class identity, не картинкой; неизвестные свойства остаются masked. Legacy defaults не дают скрытой новой боевой способности.
- Сохраняются независимые worktree изменения. При ошибке baseline тестов записать конкретный результат, не объявлять весь suite зелёным и не править чужой fixture вне scope.

## Tests

Имена ниже — планируемые тесты, а не уже существующие результаты:

- [ ] `Launcher_panel_requires_selected_nonself_target`
- [ ] `Busy_and_pending_launcher_disable_fire`
- [ ] `Paused_fire_uses_selected_target_not_hover`
- [ ] `Rejected_submission_returns_panel_to_authoritative_state`

Из корня `D:/DeepSpaceSaga/DSS` (PowerShell):

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~Launcher_panel_requires_selected_nonself_target|FullyQualifiedName~Busy_and_pending_launcher_disable_fire|FullyQualifiedName~Paused_fire_uses_selected_target_not_hover|FullyQualifiedName~Rejected_submission_returns_panel_to_authoritative_state'
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs tests/DeepSpaceSaga.Client.Tests/CommandsPanelSkeletonTests.cs tests/DeepSpaceSaga.Client.Tests/TorpedoCommandPanelTests.cs
git diff --check
```

Визуальные пункты требуют native/manual smoke и записанного evidence; headless tests не заменяют этот gate. Сроки эффектов проверять fake monotonic clock, без sleep/flaky wall-time assertions.

## Definition of Done

- [ ] Все шаги выполнены только в Code context; число файлов/слой совпадают с frontmatter.
- [ ] Наблюдаемый end state и вклад в каждый `serves` подтверждены named tests.
- [ ] Точные команды/результаты test/build/scoped format/diff check записаны при реализации; baseline debt отделён.
- [ ] Согласованные API, units/time, deterministic ordering, legacy defaults и out-of-scope соблюдены.
- [ ] Для визуального результата пройден native smoke, либо тикет явно остаётся без окончательной приёмки.
- [ ] Нет скрытых зависимостей/дополнительных файлов, незаписанных assumptions или фиктивного evidence.

## Self-containment check

Родительские epic/story связаны; решения, scope, proposed API, шаги, tests, layer и dependencies указаны. Grounding различает существующий source и будущие helpers. Чтение контекста допустимо, но новые продуктовые решения и расширение allowlist не требуются без изменения baseline.

**Текущий статус:** draft planning; implementation, automated tests и native validation не выполнялись.

