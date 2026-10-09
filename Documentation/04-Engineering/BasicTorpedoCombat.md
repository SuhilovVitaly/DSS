# EP-0006: проверка базового торпедного боя

> Исторический отчёт первого этапа. С 2026-10-04 действуют [операторы и противоракетная защита EP-0007](CountermeasureCombat.md). Ниже сохранены исходные результаты EP6; утверждения о trails/chance/loadout относятся к тому этапу. Старые Engine-fixtures теперь явно отключают auto defense; новый сценарий проверяется отдельно с защитой.

## Статус и границы доказательства

Дата: 2026-10-02. Ветка: `base-fight`. Production baseline: `d190f8b`.
TK-0001: `02978ec` (отправлен в `origin/base-fight`). TK-0002 добавляет только
`BasicCombatUiFlowTests.cs` и этот runbook. Production и Board не изменяются.

**Native acceptance: NOT RUN; TK-0002 и AC-0003 пока не приняты окончательно.**
Попытка запуска собранного клиента через Windows Computer Use вернула
`Computer Use was not approved to use client`. Native-скриншоты, видео и результаты
GPU-проверки не получены. После явного разрешения пользователя повторный запуск
вернул `Computer Use app approval timed out`. Дальнейшая native-проверка блокируется
доступом инструмента к приложению. Растровые тесты Skia ниже не заменяют native smoke.

Среда автоматической проверки: Windows, .NET SDK `10.0.400`, target `net8.0`, Debug.
SHA-256 проверяемой `src/DeepSpaceSaga.Client/bin/Debug/net8.0/DeepSpaceSaga.Client.dll`:
`32DD76CCCDA6649DAA7FFB0F1B1C60BF12B8A4A29A1B10B5A4782FD67852987A`.

## Автоматическое доказательство

`BasicCombatEndToEndTests` загружает настоящий `PlayerShipOnly/scenario.json` и
реестр из `Settings.json`. Единственное дополнение сценария — `masterSeed=42`.
Исходные координаты, скорость, курс, HP и модули не изменяются. Три настоящие
`PlayerCommand` проходят через Engine API; управляется только монотонный clock.

- Три разных projectile ID, три контакта с пиратом, HP 450→300→150→удаление;
  один wreck с новым ID, speed=0, heading=0 и координатами цели в момент контакта.
- Геометрический oracle использует исходное прямолинейное движение цели:
  `x=x0+vMps/100000*tMs*sin(heading)`, `y=y0-vMps/100000*tMs*cos(heading)`.
  Контакт — расстояние 5 world units с допуском `1e-6`; это не 5 экранных пикселей.
- Launcher освобождается после каждого контакта; повтор identity и новая busy-команда
  не создают дополнительных торпед. Игрок продолжает двигаться и сохраняет 450 HP;
  счётчик всего из трёх projectile исключает дополнительные NPC-пуски.
- Все скорости 1×/5×/20×/100×, пауза и разные разбиения времени дают одинаковые
  идентичности, HP, busy, маршруты и полный след. Допуски: координаты/курс `1e-6`,
  времена контакта/длительности аналитического следа `1e-4` мс.
- `Real_combat_with_save_mid_second_flight_matches_continuous_run` сохраняет файл
  после каждого попадания и посередине второго полёта, затем читает его через
  ScenarioLoader в новый Engine. Проверяются counters, receipts, маршрут/след,
  состояние RNG, очередность новых событий и отсутствие повторного урона.
  В PlayerShipOnly нет торговых/resource RNG streams; непустые именованные streams
  отдельно проверяет существующий `Combat_restore_preserves_named_world_rng_states_and_next_draws`.

`BasicCombatUiFlowTests` использует настоящий путь
`GameSessionScreen → GameSessionHandle → LocalGameSessionConnection → Engine →
ReadSnapshotsAsync → SnapshotBuffer → GameSessionScreen`. Нет mock transport,
ручной подстановки snapshots, телепортации или правки HP.

- `Ui_fire_flows_through_real_session_to_torpedo_snapshot`: 1280×720 при UI 100/150%
  и 1920×1080 при UI 100/120%; выбор себя запрещает пуск, hover строит preview,
  двойной клик не обходит pending, подтверждённый полёт блокирует launcher.
  Три пуска заканчиваются wreck; четвёртый пуск по нему не наносит урон.
  После контакта исчезают прогноз/крест, след и эффект истекают через 2000 мс
  контролируемого monotonic clock на паузе.
- `Ui_selection_changes_do_not_retarget_active_torpedo`: смена выбора на игрока
  и торпеду, а также поворот носителя штатной клавишей Right сохраняют исходную
  цель; начало turn-cycle подтверждается snapshot, затем физическое продвижение
  действительно меняет курс. Info показывает 100%, путь и ETA;
  настоящий `SaveAsync` и `CreateFromSaveFile` восстанавливают flight и его след.
- Выбор делается по видимому объекту после штатного zoom/pan. Панели временно
  сворачиваются штатными кнопками для доступа к карте; проверка не требует,
  чтобы все панели одновременно раскрывались без перекрытий при 150% на 720p.
  Это отдельное ограничение layout, которое необходимо оценить в native smoke.
- Ожидание транспорта ограничено 15 сек; физическое время и срок эффектов
  не проверяются с помощью wall-time sleep.

Команды из корня `D:\DeepSpaceSaga\DSS`:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter 'FullyQualifiedName~BasicCombatEndToEndTests|FullyQualifiedName~Real_combat_with_save_mid_second_flight_matches_continuous_run'
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~CombatPresentationResumeTests|FullyQualifiedName~CombatSessionRoundtripTests'
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Engine.Tests/BasicCombatEndToEndTests.cs tests/DeepSpaceSaga.Engine.Tests/CombatSaveLoadTests.cs
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter FullyQualifiedName~BasicCombatUiFlowTests
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Client.Tests/BasicCombatUiFlowTests.cs
git diff --check
```

Фактические результаты TK-0001: focused **6/6**, Engine **1070/1070**, Motion
**133/133**, Client resume/session dependency **5/5**; Engine build — 0 warnings,
0 errors; scoped format и diff check — exit 0. Полный Engine включает регрессии
торговли, docking, surveys и navigation; Motion включает Approach.

Фактические результаты TK-0002: все **5/5** новых случаев прошли в полном Client
suite **1542/1542** (42 сек, 0 failed/0 skipped). Последняя отдельная проверка
`--filter FullyQualifiedName~Ui_selection_changes_do_not_retarget_active_torpedo`
после добавления поворота — **1/1**. Client build — 0 warnings, 0 errors;
scoped whitespace format — exit 0; `git diff --check` — exit 0.
До этого исправлены ошибки самого нового test harness: выбор скрытого панелью
объекта/label hit при pan и ожидание завершённого turn receipt на паузе вместо
подтверждённого начала active cycle. Production исправления не потребовались.

## Native runbook

1. Из корня репозитория собрать клиент командой выше. Запустить
   `src/DeepSpaceSaga.Client/bin/Debug/net8.0/DeepSpaceSaga.Client.exe`.
   Выбрать **NEW GAME → Player Ship and Pirate**. Не менять scenario/content для
   сокращения дистанции: исходные ~1118 км делают бой длиннее 2–3 минут при 1×.
   Пользоваться штатным ускорением, не телепортацией и не правкой save.
2. На паузе проверить полный HP известных Тетрархов. Без цели и при выборе своего
   корабля **Fire/Пуск** недоступен. Отдалить карту (`-` или колесо), найти и выбрать
   пирата; при необходимости перетащить свободную карту. `Home` возвращает к игроку.
   `End` вписывает систему и не гарантирует включения пирата в кадр.
3. Навести курсор на доступный Пуск: gray preview, точка встречи и траектория цели;
   торпеда ещё не существует. Выполнить пуск на паузе: старт в центре игрока по его
   текущему курсу, launcher **Guiding**, повторный клик заблокирован; полёт стоит.
4. `1/2/3/4` включают 1×/5×/20×/100×, `Space` переключает паузу. Проверить активное
   движение игрока/торпеды, yellow core диаметром 5 canvas px с blur, полный solid
   trail, dashed prediction, encounter cross и прогноз цели. Сменить выбор:
   исходная цель остаётся прежней. Выбрать торпеду — цель, скорость 3 км/с, путь,
   ETA и chance 100%. Управление курсом игрока стрелками не перенаводит торпеду.
5. Первое попадание: 300/450 HP, launcher **Ready**. Записать видео контакта и
   немедленной паузы: красное кольцо расширяется 0→50 canvas px и тускнеет за
   2 реальные секунды; terminal trail исчезает через те же 2 секунды.
   Прогноз и крест исчезают сразу. Повторить при ускорении.
6. После первого попадания сохранить через меню Save в отдельный слот и загрузить.
   Старого взрыва/следа нет. Во втором полёте сохранить другой слот и загрузить:
   активная торпеда, прежняя цель, полный active trail, busy и 300 HP сохраняются.
   Не перезаписывать существующие пользовательские слоты. F5/F9 — quicksave/load,
   их использовать только если quicksave пользователя можно заменить.
7. Второе попадание даёт 150/450, третье удаляет пирата и создаёт один серый
   wreck диаметром 5 canvas px, speed=0, heading=0. Игрок продолжает игру;
   victory overlay, loot и combat sound отсутствуют. Выбрать wreck и выстрелить:
   он неуязвим, попадание снова освобождает аппарат. Загрузить отдельный save
   после третьего попадания: один wreck, нет старого эффекта или повторного урона.
8. Повторить визуальные пункты при 1280×720 и 1920×1080, UI 100/120/150%, нескольких
   zoom. Зафиксировать фактические window/framebuffer размеры и OS DPI;
   если среда не позволяет получить размер, отметить NOT RUN, а не PASS.
   Маркеры 5 px и кольцо 50 px не должны умножаться на zoom/UI scale.
9. Закрыть клиент, сохранить копию **выходного**
   `bin/Debug/net8.0/Data/UI/combat-visuals.json`. Изменить, например, `torpedo`
   с `#FFFF00FF` на `#00FFFFFF`; запустить тот же exe **без пересборки**.
   Подтвердить новый цвет, затем восстановить копию и ещё раз перезапустить.
   Не менять исходный tracked JSON ради smoke; параметры HP/оружия находятся
   в ship/module content, а не в палитре.

## Native evidence, ожидающее заполнения

Для каждого прогона записать commit/build hash, время, разрешение framebuffer,
UI scale, zoom, speed и исходный scenario/seed. F10 сохраняет PNG в `Screenshots`
относительно рабочего каталога; фактический путь находится в `Interface.log`.
Кнопка snapshot в toolbar карты сохраняет диагностику карты — её путь также
брать из лога. Сохранять вместе PNG, diagnostic JSON, видео движения/эффектов и
отдельные save-файлы. Статичный кадр на паузе не доказывает плавность движения.

| Проверка | Результат | Артефакт |
|---|---|---|
| Движение, курс, pause fire, ускорение, fixed target | NOT RUN native | Нет |
| Preview, yellow trajectories, selectable torpedo/info | NOT RUN native | Нет |
| HP 450/300/150, три контакта, Ready, один wreck | NOT RUN native | Нет |
| Кольцо/terminal trail за 2 реальные секунды на паузе и ускорении | NOT RUN native | Нет |
| File save/load midflight и после 1/2/3 контактов | NOT RUN native | Нет |
| Wreck selection/shot, продолжение игры, отсутствие звука/победы | NOT RUN native | Нет |
| 1280×720 и 1920×1080, zoom, UI scale | NOT RUN native | Нет |
| Изменение палитры и restart без rebuild | NOT RUN native | Нет |

Дефект production, обнаруженный здесь, требует targeted follow-up с отдельным
allowlist; TK-0002 не разрешает исправлять renderer, Engine или content.

## EP-0005 rendering integration — 2026-10-09

Combat poses/effects now flow through the coherent Client frame and prepared display list; input/capture use the last presented frame. Layer order and combat raster regression fixtures pass in Client 1827/1827. Engine combat outcomes, RNG, pending commands and save contracts are unchanged. This is rendering regression evidence, not new gameplay/manual acceptance. [Pipeline and lifetime](../06-Tooling/GameSessionScreenUI.md); [native limitations](../../Board/EP-0005-optimization/PerformanceEvidence.md).
