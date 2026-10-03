---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0008-basic-combat-proof
ticket: EP-0006-US-0008-TK-0002-native-combat-acceptance
title: "Проверка интерфейса и инструкция воспроизведения"
stage: draft
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: ["EP-0006-US-0008-TK-0001-three-hit-integration"]
files_touched: 2
serves: ["AC-0001","AC-0002","AC-0003"]
priority: P1
created: 2026-10-02T08:22:58Z
revision: 1
validation_status: not-run
---

# EP-0006-US-0008-TK-0002-native-combat-acceptance

Проверка интерфейса и инструкция воспроизведения.

## Why

Результат тикета: **Проверка интерфейса и инструкция воспроизведения**. Он нужен для истории [Полный бой в существующем сценарии](../EP-0006-US-0008-basic-combat-proof.md) и покрывает указанные `serves` в своей части; полные критерии story завершаются совокупностью её тикетов.

- **AC-0001** — Сквозной прогон использует реальный scenario/content, PlayerCommand и session boundary; никаких teleport, direct HP edits или подмены торпед.
- **AC-0002** — Зафиксированы три пуска, три первых контакта, HP 450/300/150/0, единственный stationary wreck и готовый launcher после взрывов.
- **AC-0003** — Проведён native/UI smoke движения, паузы, ускорения, gray preview, цветов после restart, selection, real-time effects и save/load; headless success не заменяет UI smoke.

## Decisions

- **D01:** «нам нужно добавить новый модуль торпедного аппарата в схему Тетрарха»; «для ланчера нужна своя панель»; новое помещение — «новое», место — «выбери сам». Один launcher в новом помещении стандартного Тетрарха; отдельная command panel с кнопкой Пуск.
- **D02:** «пока она летит вторую он выпустить не может»; «после взрыва выпущенной торпеды следующая уже готова»; условной занятости — «пока условной достаточно»; боезапас — «бесконечные». Одна активная торпеда на аппарат, без реального crew assignment, расхода боезапаса и отдельного ожидания КД. Готов → Наведение → Готов; pending submit не позволяет double click.
- **D03:** «у нас уже есть сценарий с кораблем пиратом и кораблем игрока»; на вопрос о движении игрока — «может»; пиратский аппарат неактивен — «пока да». Использовать PlayerShipOnly/Player Ship and Pirate. Пират летит прямо и не стреляет/не защищается; игрок управляет навигацией обычным способом.
- **D04:** «самонаведение с предсказанием»; «фиксированным. пусть будет 3 километра в секунду пока»; «давай по курсу корабля»; 90°/с — «хорошо»; точка пуска — «с центра». Торпеда стартует из центра по текущему курсу носителя, мгновенно имеет фиксированную собственную скорость 3 км/с и поворачивает максимум 90°/с; скорость носителя не прибавляется.
- **D05:** «Кнопка пуск должна быть активной только при выбранной цели»; допустимая цель — «любой объект»; исключить свой корабль — «да»; сохранить цель после смены выбора — «да». Цель — существующий выбранный объект, кроме собственного корабля. TargetObjectId фиксируется при принятии launch command, hover/смена selection не перенаводит активную торпеду.
- **D06:** Полёт следует паузе/ускорению — «да»; пуск на паузе — «да». Физика следует MotionTimeMs/SimulationTimeMs. На паузе команда создаёт торпеду и busy, но не продвигает полёт; никаких UI-tick gameplay updates.
- **D07:** Ограничить дальность — «нет»; недостижимая цель — «пока этим принебрегаем потом добавим кнопку самоуничтожения». Нет range, TTL, fuel limit, проверки достижимости как условия пуска или кнопки self-destruct. Бесконечное преследование и занятый аппарат допустимы для MVP.
- **D08:** «гарантированно попадает»; «давай центр»; «нужна погрешность скажем 5 пикселей»; фиксированный масштаб — «хорошо»; «может столкнуться. но не с кораблем игрока»; одинаковый допуск/урон для препятствий — «да». Цель наведения — центр. Контакт при расстоянии <=5 world units (500 м, 5 px при 100 м/px) с любым чужим объектом; self projectile и owner исключены. Первый контакт взрывает торпеду, без случайного броска и без сквозного полёта.
- **D09:** «корабль пиратов был уничтожен с трех попаданий»; «пока только на корабль типа Тетрарх. Все остальные объекты неуязвимые»; «превращается в новый объект врек со скоростью и направлением 0». Только явный ship.tetrarch имеет HP=450 и теряет по150: 450→300→150→0. При нуле живой корабль удаляется, новый stationary Wreck имеет новый ID, координаты погибшего корабля на момент контакта, speed=0, heading=0°.
- **D10:** «предсказательная траектория с местом предполагаемого пересечения с траекторией цели и отрисованная собственная пройденная траектория»; «желтая с размытием диаметр 5 пикселей»; оформление yellow solid/dashed/cross — «пока да»; показывать всегда — «все время». Жёлтая точка диаметром5px с blur; executed path solid yellow, remaining prediction dashed yellow, encounter cross yellow. Прогноз исходной цели до встречи тоже виден независимо от selection. Viewport clipping допустим.
- **D11:** След «исчезает через 2 секунды», время — «реального»; взрыв — «расходящийся из точки взрыва круг красного цвета»; «2 секунды от 0 пикселей до 50 радиуса плавно». После контакта прогноз/крест убираются, след живёт2000мс реального времени. Красное кольцо плавно расширяется0→50px радиуса и тускнеет до нуля за2000мс, независимо от pause/speed.
- **D12:** Сохранять торпеду, busy и урон — «да»; сохранять весь активный след — «да»; «при сохранении взрыв сохранять не нужно». Сохраняются authoritative flight/target/owner/module/route/history/distance/captured parameters, HP, wreck и ID counters. Взрыв/real-time animation state не сохраняются.
- **D13:** Показывать до выстрела при hover Пуск — «да но отличающимся цветом. например серым». При hover доступного Пуск показывается gray preview с точкой встречи и нужной траекторией цели. Чистая client prediction, без launch side effects.
- **D14:** «под лейблом ... зеленую полоску хитпоинтов»; у всех Тетрархов/игрока/full HP — «да»; врек — «серый круг диаметром 5 пикселей»; выбирать и обстреливать wreck — «да»; «просто продолжается»; звуки — «пока нет». Полоски HP постоянно под labels известных Тетрархов; wreck5px selectable и invulnerable. Игра продолжается без victory screen, лута или звуков.
- **D15:** «цветовые решения нужно вынести в отдельный файл который можно будет редактировать не компилируя код»; способ применения — «перезапуск». Отдельный combat palette JSON для torpedo/trail/prediction/intercept/preview/HP/explosion/wreck; читать при startup, без hot reload или build.
- **D16:** Выделение торпеды — «должна быть»; к цели/скорости/пройденному пути/ETA — «добавь еще шанс попадания пока 100%». Торпеда selectable; info panel показывает цель, скорость, пройденный путь, ETA (— если неизвестно), шанс100%. Это вероятность при достижении, не обещание игнорировать препятствия.
- **D17:** «для тетрарха в настройки тетрарха, для торпеды в настройки торпедного аппарата который является модулем тетрарха»; скорость и turn rate в модуле — «да». HP450 принадлежит class config Тетрарха; damage150/speed3/turnRate90 — конфигурации module.torpedo.launcher.basic, а не экземпляру палитры/константам Client.

Полный журнал, исходный scope и определения D/A — [Documentation.md](../../Documentation.md). Не переносить ранние предложения о вероятностях/КД/дальности из черновика обратно в MVP.

## Assumptions

Применяются A01–A12 эпика в части этого тикета. Все API ниже **предлагаемые**, кроме прямо названных существующих методов. Naming/overload можно уточнить без смены семантики и allowlist. Если для решения нужен дополнительный production/test файл, сначала уточнить scope; не скрывать его за количеством `files_touched`.

Manual checks: жёлтый core 5px, blur, full/2⁄3/1⁄3 HP, grey preview, yellow dashed forecast/target intersection, red ring0→50px/2 real sec на паузе, trail removal at2s, wreck5px speed0 heading0. Звук и victory overlay отсутствуют. В runbook предупредить: исходная дистанция делает бой длиннее старого ориентира2–3мин; пользоваться штатным ускорением, не телепортацией.

## Code context

Полный write allowlist от корня DSS; 2 файлов, включая тесты. Другие файлы — read-only. Новые файлы из зависимостей должны реально существовать к началу исполнения.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/BasicCombatUiFlowTests.cs` | Новый файл этого тикета; на baseline отсутствует | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |
| `Documentation/04-Engineering/BasicTorpedoCombat.md` | Новый файл этого тикета; на baseline отсутствует | Только runbook и фактическое evidence EP-0006 |

Контекст для чтения: [grounding эпика](../../Documentation.md), [история](../EP-0006-US-0008-basic-combat-proof.md), `Documentation/01-Requirements/EngineRequirements.md` (§2, §52, §55–57, §60), `Documentation/00-Process/CLAUDE.md`. Code context определяет границу записи, ссылки для чтения её не расширяют.

## Dependencies

- [EP-0006-US-0008-TK-0001-three-hit-integration](../../EP-0006-US-0008-basic-combat-proof/EP-0006-US-0008-TK-0001-three-hit-integration/EP-0006-US-0008-TK-0001-three-hit-integration.md)

Нужны реализованные и проверенные результаты зависимостей. При их отсутствии остановить этот implementation ticket с конкретным gap, не имитировать готовность mock-объектами и не дописывать зависимость за его пределами.

## Public API after the change

No API change; этот тикет добавляет integration evidence и пошаговый runbook, production rendering не изменяет.

## Implementation steps

1. Проверить UI command→real session→snapshot→map путь, включая смену selection, курс носителя, недоступность own target, pending/busy и hover preview.
2. Запустить native приложение в существующем сценарии; выполнить три пуска, pause fire, accelerate, save/load, wreck selection/shot и цветовой restart. Проверить 1280x720 и 1920x1080, несколько zoom/UI-scale значений.
3. Документировать команды, версии сборки, screenshot/evidence locations и фактический результат каждого acceptance пункта; отсутствие native smoke оставляет validation незавершённой.
4. Если обнаружен дефект вне allowlist — оформить targeted follow-up в нужном слое, не прятать production fix в evidence тикете. Не заявлять выполненные тесты заранее.

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

- [ ] `Ui_fire_flows_through_real_session_to_torpedo_snapshot`
- [ ] `Ui_selection_changes_do_not_retarget_active_torpedo`

Из корня `D:/DeepSpaceSaga/DSS` (PowerShell):

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~Ui_fire_flows_through_real_session_to_torpedo_snapshot|FullyQualifiedName~Ui_selection_changes_do_not_retarget_active_torpedo'
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Client.Tests/BasicCombatUiFlowTests.cs
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

