# Контракт реализации EP-0017

Этот документ уточняет утверждённые три истории для создания тикетов. Это часть Board; production-код, runtime JSON и нормативные требования в этом поручении не меняются. User decisions D-01…D-09 находятся в [Documentation.md](Documentation.md). A-05…A-11 ниже — решения детализации агента, не выдуманные ответы пользователя.

## Утверждённые числа и правила

- Шкала инициативы — целые 0..100. Вера55, Юн53, Марта59, Олег50; каждый thug1…thug6 имеет диапазон [40,55] с включёнными концами.
- Инициатива — самостоятельное поле пилота; именные значения fixed, диапазон материализуется один раз при старте.
- Общая очередь обеих сторон — убывание инициативы; равные получают seeded-перестановку один раз при старте боя.
- После каждого выстрела немедленно применяются броня, корпус, состояния и допустимые системные эффекты.
- Выбывший стрелок и заклинившее оружие не стреляют. Выбывшая выбранная цель означает пропуск без автоматического переназначения, в том числе для ИИ.
- При достижении победы бой заканчивается на этом шаге; дальнейших боевых эффектов нет.
- Игрок видит очередь, инициативу, промежуточные повреждения и причины пропусков. Прогноз условный; skip/replay не пересчитывают бой.

## Разрешение прежних gaps

| Gap | Итог |
|---|---|
| G-01 числа | Закрыты D-09. A-05: missing JSON initiative — typed content error до публикации каталога, без скрытого default. Старый транспортный DTO без поля всё ещё читается с null. |
| G-02…G-04 | Закрыты D-06: нет retarget, jam сразу, победа останавливает очередь. |
| G-05 эталоны | A-08/A-11: неизменённые V4 Reference и standalone остаются историческими; новые ручные oracle cases, массовая parity новых путей и runtime exporter покрыты тикетами. Старые win totals не предъявляются новой механике. |
| G-06 прогноз | A-09: «Планируемые выстрелы могут отмениться из-за более ранних попаданий.»; семантика утверждена D-07. |

## Ownership и точка интеграции

Текущие source anchors: EngineRequirements:11,110–112; TacticalBattleSalvo.Fire:42–120; TacticalBattleSession.Fire/ResolveFire:98–161; TacticalBattleSnapshotBuilder.Units/Options:10,84. Полные пути даны в тикетах. Это свидетельства исходного поведения, не утверждение, что новый API уже реализован.

Engine владеет материализацией, RNG, планами, очередью, валидацией, уроном, skips, статистикой и исходом. Contracts несёт immutable определения, очередь и ленту. LocalClient передаёт существующие DTO без нового метода; правка этого слоя не планируется. Client использует одну временную проекцию всех поверхностей. Motion не меняется.

## A-05 — схема и совместимость

На диске поле initiative обязательно: integer или ровно два integer endpoint, 0 <= min <= max <= 100. Отсутствующее/null/дробное/строковое поле невалидно. Ошибка содержит pilots.json и $.<pilotId>.initiative; не публиковать частичный каталог. World loading не должен падать из-за unavailable tactical content.

Программный BattlePilot может сохранить текущий positional constructor и default PilotSkill.Fixed(50) в init-property для существующих тестовых fixtures; loader всегда требует поле, поэтому этот default не маскирует дефект JSON. DTO Initiative nullable означает старый payload или носитель; 0 — известное допустимое значение. Save20 не меняется.

## A-06 — потоки RNG и lifetime

Основной Battle.Random остаётся существующим Mulberry32(seed), включая прежнюю сборку accuracy/maneuver/scatter/roles. Добавляется battle-owned InitiativeRandom = new Mulberry32(unchecked((uint)seed ^ 0x494E4954u)). Существующее приведение seed к uint сохраняется; полное long-значение по-прежнему хранится в snapshot, уникальность всех 64-битных seeds не обещается.

После legacy-сборки инициативы материализуются в каноническом battle.Drones order: fixed — 0 initiative draws; range — 1 RandInt(min,max), в том числе [50,50]. Программный setter TacticalDrone.Initiative используется только при сборке. Оба потока принадлежат конкретному бою и не переиспользуются между сессиями.

Затем группы по initiative descending; внутри исходный Drones order; Fisher–Yates i=n-1…1, j=InitiativeRandom.RandInt(0,i). Ровно sum(n-1) initiative draws для групп размера n>1. Записать стабильный полный rank. Новая фаза только фильтрует неактивных; не пересортировывать battle.Drones и не жеребить повторно. Factory и ручные тестовые fixtures завершают InitializeFireOrder до query/Fire; lazy RNG в Capture запрещён.

Main Random.Calls продолжает обозначать только основной поток. Проверки нового потока обращаются к его внутренним State/Calls; transport не обязан расширять outcome.RandomCalls. Прежние assembly42 для seed1 могут сохраняться, но число main draws после нового разрешения стрельбы закономерно меняется.

## A-07 — frozen phase и лента шагов

Все цели обеих сторон выбираются и валидируются по исходному состоянию фазы до RNG, Dry и статистических мутаций. Ошибочный исходный приказ — полный отказ. Отсутствующий/явный null приказ игрока сохраняет текущее значение «не стрелять»; автоматический AI target фиксируется один раз. Потеря цели внутри принятой фазы не является ошибкой запроса.

FireQueue включает активных в начале фазы дронов обеих сторон в фиксированном rank, в том числе jammed/без цели: их пропуски объяснимы. До начала фазы destroyed/disabled в очередь не включаются; выбывшие от ранних попаданий остаются её позициями с причинами пропуска.

Проектируемый внешний контракт:

```csharp
public enum TacticalFireSkipReason {
    None=0, NoTarget=1, Withheld=2, ShooterDestroyed=3, ShooterDisabled=4,
    WeaponJammed=5, TargetUnavailable=6, BattleEnded=7
}
public sealed record TacticalFireQueueEntry(int Index, string ShooterId, int Initiative);
public sealed record TacticalFireStepSnapshot(
    int Index, string ShooterId, string? TargetId, TacticalFireSkipReason SkipReason,
    TacticalShotSnapshot? Shot, ImmutableArray<TacticalUnitSnapshot> UnitsAfter,
    ImmutableArray<TacticalBattleLogEvent> Events, TacticalOutcomeSnapshot? Outcome);
```

Additive init-properties: Unit.Initiative int?; SetupOption.Initiative TacticalSkillRange?; BattleSnapshot.FireQueue/FireSteps (empty default); LogEvent.SkipReason nullable и FireStepIndex nullable. Existing constructor signatures сохраняются. LogKind0..13 неизменны, FireSkipped=14. Steps.Index — позиция в фазе 0-based, UI номер — Index+1.

Каждая позиция даёт один immutable step. Приоритет отказов: BattleEnded → ShooterDestroyed/Disabled → WeaponJammed → NoTarget/Withheld → TargetUnavailable → выстрел. NoTarget — нет начальных candidates; Withheld — candidates были, но приказ null. Skips не расходуют hit/damage/system RNG и не добавляют shot counters.

Для фактического выстрела заново получить conditions сохранённой цели из текущего state. Позиции не двигаются внутри фазы. Сохранить прежние hit/rear/piercing/disabled formulas и их границы. Damage применяется сразу к текущей броне; статистика оружия получает фактически нанесённый этим шагом урон. FirstLoss — первый по времени переход active→inactive, без object-order замены. Systems выполняются только для живой активной цели с подходящей потерей корпуса; уже недействующий effect не создаёт запись.

После каждого полного шага (damage, status, systems, statistics) вызвать CheckVictory. Не перезаписать Complete обратно Planning и не планировать следующий ход после победы. Оставшиеся queue entries получают BattleEnded steps с тем же after-state и без новых боевых бросков. Outcome публикуется на выигравшем step; presentation сохраняет уже показанный outcome и на последующих мгновенных skipped steps.

Internal result фиксирует steps и снимки до выхода из Salvo; Session присваивает журналу уникальные sequence по порядку шага: Shot → Damage → Status → Systems → Outcome. Для skip — типизированный FireSkipped. Полный Log и Events каждого step описывают один набор фактов, не два исполнения.

TacticalShotSnapshot.Killed означает уничтожение именно этим выстрелом на его шаге; LethalImpact обозначает переход в destroyed. Нельзя брать Killed из mutable конечного Target.Hull при построении списка после залпа. BeforeSalvoUnits — начало фазы; FireSteps — завершённая последняя фаза, не будущий прогноз. Данные удерживаются после BeginTurn, очищаются на следующем Move и при выходе/новом бою. FireQueue для завершённой ленты не заменяется очередью следующего хода.

## A-08 — независимое доказательство и V4

Изменение времени урона намеренно нарушает old simultaneous summaries, traces, mutual-destruction assertion и old series win counts. Не регенерировать historical V4 справочник из нового Engine и не выдавать это за независимый oracle. Не добавлять Skip и не ослаблять assertions до сравнения количества объектов.

Новые InitiativeReferenceCases.cs содержат заранее рассчитанные малые входы и ожидаемые shot/skip/state/draw deltas. Oracle минимален и test-only, не вызывает Salvo/CheckVictory для вычисления ожидаемого результата. Примеры:

- A80 убивает B60 до его очереди: один выстрел, B60 skip; гарантированный non-rear hit и fixed damage расходуют ровно два main draws, skipped reply — ноль.
- Target armor10, piercing0, damage15 дважды: после первого armor0/hullLoss5, после второго armor0/hullLoss15; общий hullLoss20, без group pre-salvo брони.
- A80 промахнулся: B60 остаётся доступным. Удержанный/NoTarget выстрел не добавляет main draws.
- Другая доступная цель существует, но frozen target выбыл: skip, а не retarget.
- Две независимые причины конца боя: носитель и последний active дрон. Очередь после выигрывающего impact не наносит урон.

GoldenBattle/SeriesParity/LocalSession проверяют новую механику, повторяемость и согласованность direct/API путей; их массовая часть не является независимым oracle урона. Оставить исходные primitive/geometry/reference fingerprint проверки, если их контракт не изменился. Пять LongRunning series выполнять отдельно по точным test IDs, максимум 300000 ms каждый process tree. Если одной тактике тесно, разделить её на детерминированные seed chunks внутри того же test файла и запускать точные новые IDs; cap не повышать.

## A-09/A-10 — условный прогноз и единое отображаемое время

Очередь до Fire берётся из Engine, прогноз показывает планы и предупреждение условности, не расходуя RNG и не просматривая future FireSteps. Обе стороны сохраняют общую очередь.

Во время playback Flow.State остаётся authoritative final state, а CaptureFrame создаёт отдельную presentation-проекцию. До первого impact видны BeforeSalvoUnits; затем последний дошедший UnitsAfter. Видимые Log/Losses/Outcome меняются в тот же момент. Карта, карточки, заголовок и журнал читают эту проекцию, не восстанавливают собственную физику/броню и не откатываются к всему BeforeSalvo из-за флага SalvoFlight.

Initial delay300ms. При N>0 длительность одного полёта min(975,(4500-300)/N)ms; следующий старт не раньше предыдущего impact. Flash450ms может перекрываться с новым полётом. TacticalShotFlight получает собственную DurationMs. Сравнения inclusive на impact; большие скачки времени применяют все истекшие steps по порядку. Skips имеют нулевой полёт и применяются на соответствующей границе. N=0 — мгновенные skips/итог без деления на ноль.

Final damage/outcome и события next planning не показываются заранее. Skip показывает полный итог; Replay начинает с before-state и не отправляет Fire. Дубликат revision не запускает повторно animation/journal. При новом BattleId и Exit сбросить старый cache/очередь. Пакет скина меняет только оформление.

## A-11 — pipeline контента и синхронизация документации

Runtime pilots.json после D-09 является источником только initiative-расширения EP-0017. Export-Content.ps1 читает его до записи и переносит initiative по ordinal ID на V4 base export; остальные runtime поля не копирует. Дополнительный optional -PilotInitiativeFile допускает безопасные тестовые fixtures; default — runtime pilots.json. --Check никогда не пишет. Invalid source/ID — отказ до записи. ContentSyncTests остаётся строгим для всех V4 base fields и единственного объявленного extension.

Standalone V4 Python/HTML, VData/Planning/V4.0/Data и Reference не изменяются. Это явное ограничение: самостоятельный V4 не воспроизводит новую инициативную механику. Автор DSS, меняющий runtime initiative, обязан обновить согласованный баланс и проверки; обратный экспорт не должен стереть поле.

В US-0003/TK-0005 предусмотрена будущая синхронизация EngineRequirements/TacticalBattle/CarrierWingSim с фактически реализованным контрактом. Сейчас они не редактируются. Документация — сопутствующие файлы tooling ticket и входит в его лимит5, а не новый production layer.

## Validation gates и остаточный backlog

Сейчас проверены только Markdown, IDs, зависимости, ссылки и ограничения planning. Future commands записаны в каждом тикете с kill process tree после300000ms и абсолютным run root под D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/. Native UI/input, Classic/contrast, нормальный просмотр/skip/replay и продуктовая понятность принимаются после реализации, отдельно от automated.

Стартовые числа D-09 утверждены для реализации; их долгосрочный баланс ещё не доказан. Out of scope остаются dynamic initiative, ход движения по инициативе, стрельба носителей, сохранение боя, развитие/найм пилотов, редизайн AI и перенос механики в standalone V4.

