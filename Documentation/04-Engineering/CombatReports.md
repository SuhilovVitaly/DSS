# Боевые доклады экипажа — EP-0010

Актуальная реализация 2026-10-16. [Продуктовый контракт R01–R24 / E01–E07](../02-FirstRelease/TechnicalTasks/TacticalCombatReports.md), [Board](../../Board/EP-0010-combat-reports/EP-0010-combat-reports.md), [результаты и публикация](../../Board/EP-0010-combat-reports/ImplementationStatus.md). Это расширение [модульного боя](ModuleCombat.md). Native/human/FPS acceptance OPEN. Пользователь запретил самостоятельный запуск игры; агентам разрешены headless тесты и сборки, но запрещён запуск игрового окна и native/performance harness без нового явного поручения пользователя.

## Авторитетные факты

`BattleReportSnapshot` — immutable DTO Contracts: отдельные Sequence, MotionTimeMs, Kind, EpisodeId, frozen Reporter, Source/Target, ProjectileId/LauncherModuleId, Variant и необязательные Armor/Structure damage facts. Ни Skia, ни live object references через boundary не проходят. Report sequence независим от snapshot sequence и CombatJournal.EventId. UI не меняет RNG, часы и исходы боя.

| Событие | Источник истины | Докладчик |
|---|---|---|
| E01 EnemyInRange | Первый вход известного живого enemy в configured range рабочего player launcher, при рабочем сканере; inclusive boundary, stable target order | Оператор сканера |
| E02 EnemyTorpedoLaunch | Фактический пуск известного enemy по игроку, видимый рабочему сканеру | Оператор сканера |
| E03 PlayerTorpedoLaunch | Реальный торпедный projectile после успешного пуска | Захваченный оператор пусковой |
| E04 PlayerCountermeasureLaunch | Каждый фактический projectile очереди ПРО | Захваченный оператор ПРО |
| E05 EnemyTorpedoIntercepted | Подтверждённый успешный перехват вражеской торпеды player ПРО до удаления projectile | Захваченный оператор ПРО |
| E06 EnemyShipHit | Один итог после полного контакта player torpedo с enemy; Hit/Destroyed, включая допустимое нулевое воздействие | Захваченный оператор торпеды |
| E07 PlayerShipDamaged | Реальная потеря Armor/Structure модулей игрока; Captain фиксируется до Defeated | Капитан |

Рабочий launcher для E01 означает отсутствие поломки/disable; reload и остаток ammo не меняют configured дальность. Только уже известные цели; выбор/hover не влияет. CrewMemberRuntime не содержит portrait: отсутствующее изображение даёт role fallback, выдуманные NPC не создаются. Имена/ID неизвестных участников и модулей скрываются в Engine. Formatter использует только captured facts и RU/EN catalog, не читает текущий мир.

Точки реализации: `SimulationEngine.CombatReports.cs`, `.CombatReportRange.cs`, `.CombatReportIdentity.cs`, `.CombatReportPersistence.cs`, `.Combat.cs`. Навигация — [Graphify](../06-Tooling/TradingSolarGraphify.md).

## Доставка и ограничение памяти

`CombatReportBatchSnapshot` в `AuthoritativeSnapshot.CombatReports` optional для старых consumers. Batch несёт фиксированный на сессию BaselineSequence, LatestSequence и reports. Engine outbox очищается только после формирования snapshot, Save capture его не дренирует. Mailbox сливает недоставленные reports по sequence. `SnapshotBuffer` принимает reports до отбрасывания устаревшего world snapshot, а UI отдельно дренирует inbox в последовательном порядке. Отсутствующий batch ничего не стирает.

Outbox/mailbox/inbox имеют явный PendingLimit4096; переполнение и противоречивые дубликаты дают ошибку вместо тихого пропуска. Inbox удерживает gaps до непрерывного drain и ограниченный recent payload cache. Потребитель UI хранит последние200 карточек newest-first. Новый пакет раскрывает последнюю новую карточку и сбрасывает scroll наверх; duplicate не меняет состояние. Expand оставляет ровно одну логически раскрытую карточку; клавиатурный выбор отделён от раскрытия.

## Сохранения и сессия

Root SaveFormatVersion остаётся17. Optional `GameStateData.CombatReportsState` содержит child Version1, Sequence и ReportedRangeTargetIds; все поля child обязательны. Validator проверяет version, nonnegative sequence, ненулевой список, непустые trimmed IDs без control characters и case-insensitive duplicates до изменения мира. Удалённые target IDs допустимы как once-per-target история. Старый Save17 без child поддерживается; несовместимые <=16 отклоняются прежним контрактом.

При Load сначала полностью проверяется и материализуется мир. Текущие подходящие range contacts тихо объединяются с сохранёнными marks; baseline восстанавливается из sequence. История карточек, pending deltas и UI focus не сохраняются, старые события не проигрываются повторно. New Game сбрасывает marks/sequence/UI; закрытие/Defeated освобождают panel/assets и прекращают приём в старый экран. Открытие модального экрана сохраняет историю, очищая focus. При переходе на Defeat SkiaWindow явно освобождает ресурсы игрового экрана до удаления корня из ScreenStack, даже если он уже деактивирован под одним или несколькими overlay.

## Экран и ресурсы

Панель следует за фактическим низом Object Info справа. Минимум80 logical px резервируется докладам; Object Info при нехватке высоты использует ограниченное прокручиваемое тело. Report bounds исключены из AvailableMapRect, journal ограничен до правой колонки; footer/map toolbar учитываются и без AiMap. Empty/tiny widths не вызывают исключение переноса текста.

`SkiaWindow.OnRender` вызывает `PrepareCombatReports` на UI thread до screen.Render: drain, formatter и подготовка изображений. Draw выполняет чтение подготовленных ресурсов без file I/O/decode. Восемь opaque PNG1024×576 перечислены в `Data/UI/combat-report-visuals.json`; catalog и изображения копируются в output/publish. Один набор illustrations на session, portrait LRU32 включая negative cache; большой portrait уменьшается до128. Paint/bitmap освобождаются при закрытии.

Заголовок содержит время/тип, раскрытая карточка — illustration16:9, отдельный portrait48/role icon, captured reporter (имя при наличии и локализованная должность отдельной строкой), текст/модульные damage details. Click/drag/wheel внутри панели не уходят на карту или команды. Tab переводит keyboard focus между картой и панелью; Escape в панели возвращает focus карте, повторный Escape открывает игровое меню. Up/Down и Enter действуют только при focus панели. Последующие новые reports не захватывают keyboard focus. Повторный click по открытой карточке сохраняет её открытой.

## Проверка и открытая приёмка

Текущий полный Engine:2197/2197 PASS; Contracts210/210 PASS. Client1973/1973 PASS при исключении пяти торговых сценариев с общим15s refuel deadline; baseline и current reruns подтверждают этот сторонний timeout. Целевой repair226/226 PASS. Production CombatMukha и редкие исходы, privacy/capture, pause/SaveLoad, order/gaps,200-history,48 combinations RU/EN/resolution/scale и lifecycle проверены headless. Полный перечень команд и TRX — в Board status/evidence.

Сохранён единственный ранее сделанный native sample1280×720/UI100/EN:64frames, проверка UI fixtures на paused CombatMukha seed1. Он не доказывает production E01–E07 в native, всю матрицу, Save/Load/Defeated или FPS. 600-frame perf sample не измерен; targetPassed=false в этом JSON означает отсутствие измерения. Human art/gameplay, полный native matrix, GPU/scanout/frame-time и80FPS остаются OPEN. EP9 баланс этим эпиком не пересматривался.
