---
epic: EP-0009-module-combat
title: "Модульный бой в духе Battle Brothers: броня, броски навыков, режимы, Мухи и Шмели"
stage: draft
created: 2026-10-11
revision: 2
revision_note: "rev2: EP-0008 считается реализованным; EP-0009 переопределяет его правила боя и переиспользует его инфраструктуру"
baseline: "base-fight после завершения EP-0008 (на origin/base-fight @ 99e15a2 опубликованы только EP-0008 TK-0001/TK-0002 — сверить HEAD перед стартом)"
depends_on_epic: EP-0008-player-defense-and-hostile-combat
validation_status: planning-only
design_source: "C:/DSS/docs/fight/CombatDesign-v1 (букмарк 1) + рабочие данные src/DeepSpaceSaga.Client/Data/CombatDesign (модульные HP, Шмель)"
---

# EP-0009 — Модульный бой в духе Battle Brothers

Статус: **planning-only / draft**, revision 2, 2026-10-11. 12 историй, 38 тикетов. Всё в одном файле: контракт, решения, истории и тикеты. Документ только описывает план, тикеты пока не выполнялись.

> **Baseline.** Эпик исходит из того, что EP-0008 реализован полностью и в соответствии со своим контрактом (`Board/EP-0008-player-defense-and-hostile-combat/Documentation.md`). На `origin/base-fight @ 99e15a2` опубликованы только EP-0008-US-0001-TK-0001 и TK-0002. **Перед US-0001 исполнитель обязан** взять HEAD с завершённым EP-0008, заново сверить все пути, номера строк и имена API этого файла и записать расхождения в Code context тикета. Номера строк ниже взяты с `99e15a2`: это подсказка, а не факт.

## 1. Цель

Перенести в движок боевую модель, согласованную с автором и проверенную Python-симулятором (`src/DeepSpaceSaga.CombatSim`, 400 боёв на вариант). Для этого EP-0009 **переопределяет боевые правила EP-0008** и **переиспользует его инфраструктуру**:
- враждебный ИИ и стрельбу NPC;
- две ПРО с панелями;
- ручную ПРО;
- поражение;
- шлюз формата сохранений;
- журнал.

Что нового по сравнению с EP-0008:
- **Боец = модуль + оператор.** Броня и структура у каждого модуля, HP корпуса больше нет.
- **Броски навыков:** `шанс = 50 + атака − защита`, ограничение 0–100%. Навыки операторов снова участвуют в броске. Попадание торпеды решает бросок, а не геометрия.
- **Темп стрельбы.** Перезарядка считается от пуска, правило «один снаряд на аппарат» снято. ПРО двухствольная и стреляет очередью из 2 противоракет.
- **Режимы торпедного аппарата:** Стандарт, Манёвренность, Перегрев.
- **Враждебные микроистребители** «Муха» и «Шмель» на базе ИИ EP-0008.
- **Два сценария:** Тетрарх против 3 Мух и Тетрарх против 3 Шмелей.

Эталон симулятора, режим «Стандарт»: Тетрарх выигрывает у 3 Мух в **81,2%** боёв, у 3 Шмелей — в **49,5%**.

## 2. Что EP-0009 делает с EP-0008

### 2.1 Переопределяется (правила EP-0008 перестают действовать)

| Правило EP-0008 (Documentation.md / Decisions.md) | Новое правило EP-0009 | Где |
|---|---|---|
| Шанс `clamp(Accuracy − CapturedTorpedoManeuverability, 0, 100)`, без +50, навыки не влияют (Q20/Q21, A03 «skill только metadata») | `clamp(50 + attack − defense, 0, 100)`, навыки операторов входят в атаку и защиту (D04–D06) | US-0004 |
| Попадание торпеды в корабль = геометрический контакт, урон идёт в hull | Контакт → бросок попадания → урон в случайный модуль (D05, D08–D10, D23) | US-0003, US-0004 |
| HP корпуса Тетрарха 450; «hull-grid module damage» вне scope | HP корпуса удалены; броня и структура у каждого модуля; гибель корабля по модулям (D08–D11) | US-0002, US-0003 |
| Один активный снаряд на аппарат (торпеда и ПРО); следующий пуск NPC после окончания полёта, без cooldown (A07) | Торпедный аппарат: перезарядка `launcherReloadMs` от пуска, в полёте может быть несколько торпед (D12). ПРО: очередь стволов, перезарядка от первого выстрела (D13, D14) | US-0005 |
| Перезарядка ПРО начинается после удаления противоракеты | Перезарядка ПРО отсчитывается от первого выстрела очереди (D13) | US-0005 |
| Общий auto-attempt flag: одна автоматическая попытка на торпеду | Флаг удалён. Автоматика может повторить попытку после промаха, но не стреляет, пока по торпеде летит живая противоракета (D15) | US-0005 |
| Торпеда: манёвренность 5, скорость 3 км/с, дальность пуска 200 км; у NPC полёт бесконечный | Манёвренность 50, 10 км/с, дальность 150 км, время жизни 120 с у всех (D25, D27) | US-0002 |
| ПРО: точность 55; дальность авто 100 / ручная 200 км; 12 км/с; полёт 60 с; перезарядка 10 с | Точность 58; дальность авто = ручная = **150 км**; 20 км/с; полёт 30 с; перезарядка 14,5 с; 2 ствола (D25, D28) | US-0002 |
| Двигатель basic 4000 м/с | 6000 м/с, манёвренность 10; новый двигатель «Стриж» (D26, O2) | US-0002 |
| Орбита ИИ радиусом `0.5 × TorpedoRangeKm` (100 км), пуск «внутри диапазона» | Радиус орбиты из сценария (`standoffKm` 50 ± `bandKm` 10), заход с флангов; пуск только при достижимом перехвате за ≤ 0,8 × время жизни (D33) | US-0008 |
| Save 16, defense payload 2, hostile state 1 | Save **17**, defense payload 3, combat payload 2, hostile state 2; сохранения формата 16 отвергаются без миграции | US-0011 |

### 2.2 Переиспользуется без изменения смысла

- Общий внутренний исполнитель пуска за публичным guard владельца; NPC стреляют без пользовательских команд.
- Враждебность через `relationToPlayer` и `OwnerPlayerRelationAtLaunch` (A02 EP-0008).
- Арбитраж двух ПРО: угроза с ближайшим попаданием в корпус → установка с самым ранним контактом → наибольший шанс → стабильный module ID.
- Ручной `defense.fire(objectId, moduleId, targetTorpedoId, commandId)` и его правила на паузе (A08 EP-0008). Ручной пуск по-прежнему может преследовать цель, по которой уже летит противоракета.
- Панели по экземплярам ПРО, превью на hover, таймеры полёта и перезарядки.
- Терминальное поражение: остановка часов, окно «Загрузить / Главное меню», запрет сохранения после гибели.
- Комплектация «Player Ship and Pirate»: 2 ПРО, 3 оператора, `living.quarters.mk1.three-cabins`.
- MissedCoast 2 с: остаётся визуальным эффектом, на перезарядку больше не влияет.
- Таблица порядка событий одного физического времени (EP-0008 §«Поражение, сохранения»), дополненная новыми событиями (§3.2).

### 2.3 Статус EP-0008 после EP-0009

Завершающая история US-0012 добавляет в `Board/EP-0008-…/ImplementationStatus.md` раздел «Superseded by EP-0009» со ссылкой на §2.1. Код EP-0008, который EP-0009 заменяет, удаляется, а не остаётся рядом мёртвым кодом.

## 3. Решения (источник — обсуждение с автором 2026-10-07…11)

| ID | Решение | Значение |
|---|---|---|
| D01 | Боец = модуль корабля + его оператор | навык оператора складывается со статом модуля |
| D02 | Весь бой дальний: торпеды и ПРО | ближний бой отложен |
| D03 | Гибридная модель | полёт, наведение и контакт как в EP-0006/0007/0008; исход контакта решает бросок |
| D04 | Формула шанса | `chanceTenths = round(clamp(base + attack − defense, min, max) × 10)`; по умолчанию base 50, min 0, max 100; бросок 1..1000 ≤ шанс |
| D05 | Попадание торпеды | attack = `accuracy` аппарата + навык `accuracy` оператора аппарата + бонус режима; defense = `maneuverability` двигателя цели (0, если двигатель выключен или уничтожен) + навык `maneuverability` пилота (оператора двигателя) |
| D06 | Перехват | attack = `accuracy` ПРО + навык `accuracy` оператора ПРО; defense = `torpedoManeuverability` аппарата + бонус манёвренности режима + навык `maneuverability` оператора аппарата |
| D07 | Захват при пуске | атака торпеды и защита торпеды от перехвата фиксируются при её пуске; атака противоракеты — при её пуске; защита цели при броске попадания берётся на момент контакта |
| D08 | HP в модулях | у каждого установленного модуля есть `armor` и `structure`; HP корпуса удалены |
| D09 | Урон по образцу BB | `direct = dmg × pierce%`; `rest = dmg − direct`; `absorbed = min(armor, rest × armorDamage%)`; `overflow = rest − absorbed / armorDamage%`; `armor −= round(absorbed)`; `structure −= round(direct + overflow)` |
| D10 | Любое попадание выключает модуль на 60 с | даже если броня поглотила весь урон; время физическое |
| D11 | Гибель корабля | уничтожен модуль с ролью `bridge` или `engine`, **или** уничтожено не меньше `ceil(N × 1/3)` из N установленных модулей. Для игрока это терминальное поражение EP-0008 |
| D12 | Торпедный аппарат | перезарядка `launcherReloadMs × reloadMultiplier` режима считается от пуска; в полёте может быть несколько торпед |
| D13 | ПРО | перезарядка от первого выстрела очереди: `ReadyAt = t1 + max(reload, interval × (barrels − 1))` |
| D14 | Стволы ПРО | `barrels` 2: одна установка ведёт одну торпеду; второй выстрел через `burstIntervalMs` 2000; не стреляет, если цель уже сбита; очередь отменяется при выключении установки |
| D15 | Автоматика ПРО | угроза выбирается по правилу EP-0008, но только среди торпед, по которым **не летит живая противоракета**; после промаха повторная попытка разрешена |
| D16 | Режимы торпедного аппарата | Стандарт `(0, 0, 0%, ×1)`; Манёвренность `(+0 acc, +10 man, 0%, ×1.25)`; Перегрев `(+10, +10, 10%, ×1.25)`. Формат: (точность, манёвренность, шанс поломки, множитель перезарядки) |
| D17 | Выбор режима | до боя; смена в бою стоит один цикл перезарядки аппарата |
| D18 | Режимы есть только у торпедного аппарата | ПРО всегда в «Стандарте» |
| D19 | Перегрев | пуск происходит; затем с шансом 10% аппарат ломается до конца боя (чинится только в доке) |
| D20 | Ремонт брони и структуры | только в доке |
| D21 | Классы брони | `none` = 0, `light` = 60 |
| D22 | Правило 1/2 | модуль без брони погибает с 1 попадания, с бронёй `light` — с 2. Для этого `structurePointsMax` = 150 у всех модулей, урон торпеды 150, `armorPiercing` 20% |
| D23 | Модуль под попаданием | случайный живой модуль, вес = `massKg`; свой поток ГСЧ |
| D24 | ПРО | игрок стреляет вручную (EP-0008), автоматика включается и выключается |
| D25 | Скорости и дистанции | торпеда 10 км/с, поворот 90°/с, время жизни 120 с, дальность пуска 150 км; противоракета 20 км/с, поворот 90°/с, время жизни 30 с; дальность ПРО авто = ручная = 150 км |
| D26 | Двигатели | `module.engine.basic`: манёвренность 10, 6000 м/с. Новый `module.engine.strizh`: манёвренность 40, 8000 м/с, разгон 800 м/с², поворот 12°/с, масса 4000 кг |
| D27 | Торпедный аппарат | точность 40, `torpedoManeuverability` 50, урон 150, перезарядка 15000 мс |
| D28 | ПРО | точность 58, перезарядка 14500 мс, 2 ствола. Новый `module.countermeasure.launcher.single`: то же, но 1 ствол |
| D29 | Навыки операторов | `accuracy`, `maneuverability`, шкала 0–30; в боевых сценариях 20. Навыки EP-0008 (metadata) заменяются |
| D30 | Тетрарх | комплектация EP-0008 (8 модулей + вторая ПРО = 9), у всех модулей броня `light` |
| D31 | Муха | двигатель «Стриж» + торпедный аппарат, без брони |
| D32 | Шмель | Муха + одноствольная ПРО, без брони |
| D33 | ИИ противника | контроллер EP-0008 с параметрами из сценария: `standoffKm` 50, `bandKm` 10, `flankOffsetDeg` (−35 / 0 / +35). Дальше `standoff + band` — упреждение и фланговое смещение `flank × min(1, (d − standoff)/100)`; ближе `standoff − band` — отход; иначе — орбита. Пуск, если цель в пределах дальности пуска и расчётный перехват ≤ 0,8 × время жизни торпеды |

### 3.1 Технические допущения (из симулятора; автор явно не утверждал)

| ID | Допущение |
|---|---|
| A01 | При промахе торпеда удаляется (`TerminationKind.Miss`), урона нет |
| A02 | Торпеда поражает только свою цель; столкновения с другими объектами игнорируются |
| A03 | Выключенный двигатель: корабль летит по инерции, без поворота и разгона; ИИ не маневрирует |
| A04 | Выключенный мостик ничего не блокирует; он важен только для правила гибели |
| A05 | `disabledUntil = max(текущее, t + 60 с)`; выключение не накапливается |
| A06 | Второй выстрел очереди по уже сбитой торпеде не происходит и перезарядку не меняет |
| A07 | Гибель NPC оставляет обломок (как в EP-0006); гибель игрока — терминальное поражение EP-0008 |

### 3.2 Порядок событий одного физического времени (дополняет таблицу EP-0008)

1. Контакт торпеды с кораблём → бросок попадания → урон и выключение модуля. Гибель игрока на этом шаге терминальна.
2. Удаление противоракет, потерявших цель или носитель.
3. Контакт противоракеты с живой торпедой → бросок перехвата (стабильный порядок по projectileID / targetID).
4. Истечение времени жизни противоракет и торпед, конец MissedCoast.
5. Отложенные выстрелы очередей ПРО (по времени, затем по module ID).
6. Готовность аппаратов, новые автоматические пуски, решения ИИ (стабильный порядок).

### 3.3 Открытые решения (за автором, до старта US-0001)

| ID | Вопрос | Рекомендация |
|---|---|---|
| O2 | Менять скорость `module.engine.basic` 4 → 6 км/с глобально (затрагивает рейсы и экономику) или завести отдельный боевой двигатель | глобально, с регрессией рейсов в US-0002-TK-0002 |
| O3 | `structurePointsMax` = 150 у всех модулей глобально | да; в игре структура нужна только как условие готовности модуля |
| O4 | У Мухи и Шмеля по члену экипажа на модуль | да, как у Тетрарха |
| O5 | Что делать с Python-симулятором и `Data/CombatDesign` после реализации | симулятор читает настоящие данные игры, `Data/CombatDesign` удаляется (US-0011-TK-0003) |
| O6 | Пират в `PlayerShipOnly` после EP-0008 | оставить контроллер EP-0008 с параметрами по умолчанию; новые числа модулей он получит автоматически |
| O7 | Раздельные авто- и ручная дальность ПРО из EP-0008: два параметра или один | два параметра, оба 150 км |

## 4. Правила исполнения (`Board/EpicExecutionPrompt.md` + политика компании)

- **Перед кодом** прочитать `Documentation/**`, этот файл, `Board/EP-0008-…/` (Documentation, Decisions, ImplementationStatus), `AGENTS.md` и `CLAUDE.md`. Сверить baseline с кодом (см. врезку вверху).
- **Тикет = один production-слой и не больше 5 файлов вместе с тестами.** Расширение допустимо только с обоснованием, записанным в карточку до изменения (A12 EP-0008).
- **Цикл тикета:**
  1. проверить зависимости;
  2. сделать реальную реализацию, без mock и stub;
  3. обновить старые тесты, которые сломал этот тикет, не ослабляя их;
  4. прогнать целевые тесты, build и `git diff --check`;
  5. сделать self-review, не помечая его APPROVED;
  6. записать evidence с фактическими командами;
  7. сделать один commit с полным ID тикета.
- **Push в `origin` (github.com) агент не выполняет:** это запрещено политикой компании. Пункты EpicExecutionPrompt про push для агента не действуют. Публикацию делает человек, через внутренний Bitbucket.
- **Инварианты EP-0006…0008 сохраняются:**
  - Engine владеет исходами и ГСЧ;
  - именованные потоки ГСЧ сохраняются вместе со счётчиками;
  - ничьи разрешаются по ordinal ID;
  - результат не зависит от разбиения интервала;
  - пауза не двигает ни физику, ни ИИ;
  - `CommandId` идемпотентен;
  - загрузка атомарна;
  - `UnmappedMemberHandling.Disallow`;
  - шаблоны version 0 загружаются;
  - на загрузке должен быть корабль игрока;
  - после гибели сохранение запрещено.
- **Native-проверка** — в реальном окне. Если окно недоступно, ставится `NOT RUN` с причиной.

```powershell
dotnet build DeepSpaceSaga.sln --no-restore
dotnet test tests/<Project>/<Project>.csproj --no-restore --filter "FullyQualifiedName~<Class>"
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <изменённые .cs>
git diff --check
```

## 5. Сводная таблица (порядок последовательный)

| Шаг | История / тикет | Layer | Files | Depends on | Serves |
|---|---|---|---:|---|---|
| — | **US-0001 Правила боя как данные** | | | | |
| 1 | TK-0001 Контракты состояния модуля и бросков | contracts | 5 | EP-0008 done | US1-AC1, US1-AC3 |
| 2 | TK-0002 Контракты темпа, очереди и режимов | contracts | 5 | US1-TK1 | US1-AC2, US1-AC3 |
| 3 | TK-0003 Правила боя, классы брони, режимы | engine | 5 | US1-TK2 | US1-AC1 |
| 4 | TK-0004 Расширение схемы модулей EP-0008 | engine | 4 | US1-TK3 | US1-AC2 |
| 5 | TK-0005 Навыки экипажа и броня в сценарии | engine | 5 | US1-TK4 | US1-AC3 |
| — | **US-0002 Каталог модулей и кораблей** | | | | |
| 6 | TK-0001 Аппарат и ПРО: числа EP-0009 вместо EP-0008 | content-data | 4 | US1-TK5 | US2-AC1 |
| 7 | TK-0002 Двигатели «Буксир» и «Стриж» | content-data | 4 | US2-TK1 | US2-AC1 |
| 8 | TK-0003 Структура 150 у всех модулей | content-data | 5 | US2-TK2 | US2-AC2 |
| 9 | TK-0004 Классы Муха и Шмель, без HP корпуса | content-data | 5 | US2-TK3 | US2-AC3 |
| — | **US-0003 HP в модулях** | | | | |
| 10 | TK-0001 Броня и структура в runtime и snapshot | engine | 5 | US2-TK4 | US3-AC1 |
| 11 | TK-0002 Урон, выбор модуля, выключение | engine | 4 | US3-TK1 | US3-AC2 |
| 12 | TK-0003 Гибель по модулям → обломок или поражение EP-0008 | engine | 5 | US3-TK2 | US3-AC3 |
| 13 | TK-0004 Последствия выключения и уничтожения | engine | 4 | US3-TK3 | US3-AC4 |
| — | **US-0004 Броски навыков** | | | | |
| 14 | TK-0001 Формула шанса вместо формулы EP-0008 | engine | 4 | US3-TK4 | US4-AC1 |
| 15 | TK-0002 Бросок попадания торпеды | engine | 4 | US4-TK1 | US4-AC2 |
| 16 | TK-0003 Бросок перехвата с навыками и превью | engine | 5 | US4-TK2 | US4-AC3 |
| — | **US-0005 Темп стрельбы** | | | | |
| 17 | TK-0001 Перезарядка аппарата от пуска (снять «один снаряд» EP-0008) | engine | 4 | US4-TK3 | US5-AC1 |
| 18 | TK-0002 Очередь стволов ПРО | engine | 4 | US5-TK1 | US5-AC2 |
| 19 | TK-0003 Перезарядка ПРО от первого выстрела, замена auto-attempt flag | engine | 5 | US5-TK2 | US5-AC3 |
| — | **US-0006 Режимы торпедного аппарата** | | | | |
| 20 | TK-0001 Режим в runtime: бонусы, цена, Перегрев | engine | 5 | US5-TK3 | US6-AC1, US6-AC2 |
| 21 | TK-0002 Команда смены режима | engine | 4 | US6-TK1 | US6-AC3 |
| 22 | TK-0003 Выбор режима на панели аппарата | client | 5 | US6-TK2 | US6-AC3 |
| — | **US-0007 Ручная ПРО EP-0008 под очередь** | | | | |
| 23 | TK-0001 `defense.fire` запускает очередь | engine | 4 | US6-TK3 | US7-AC1 |
| 24 | TK-0002 Панели ПРО: стволы, новый шанс, перезарядка | client | 5 | US7-TK1 | US7-AC2 |
| — | **US-0008 Мухи и Шмели на ИИ EP-0008** | | | | |
| 25 | TK-0001 Темп и условие пуска NPC, время жизни торпед | engine | 4 | US7-TK2 | US8-AC1 |
| 26 | TK-0002 Фланговый заход и радиус из параметров | motion | 3 | US8-TK1 | US8-AC2 |
| 27 | TK-0003 Параметры ИИ из сценария, выключенный двигатель | engine | 5 | US8-TK2 | US8-AC2, US8-AC3 |
| — | **US-0009 Боевые сценарии** | | | | |
| 28 | TK-0001 «Тетрарх против 3 Мух» | content-data | 3 | US8-TK3 | US9-AC1 |
| 29 | TK-0002 «Тетрарх против 3 Шмелей» | content-data | 3 | US9-TK1 | US9-AC2 |
| 30 | TK-0003 Остальные сценарии под новую схему | content-data | 5 | US9-TK2 | US9-AC3 |
| — | **US-0010 Интерфейс модулей и журнал** | | | | |
| 31 | TK-0001 Поражение EP-0008 по правилу гибели D11 | engine | 3 | US9-TK3 | US10-AC1 |
| 32 | TK-0002 Состояние модулей на карте и в панели | client | 5 | US10-TK1 | US10-AC2 |
| 33 | TK-0003 Журнал бросков, урона и модулей | client | 4 | US10-TK2 | US10-AC3 |
| — | **US-0011 Сохранения и приёмка** | | | | |
| 34 | TK-0001 Формат сохранения 17 | engine | 5 | US10-TK3 | US11-AC1 |
| 35 | TK-0002 Сквозные бои и сверка с симулятором | engine | 3 | US11-TK1 | US11-AC2 |
| 36 | TK-0003 Симулятор на данных игры, нативная приёмка | tooling | 5 | US11-TK2 | US11-AC3 |
| — | **US-0012 Документация** | | | | |
| 37 | TK-0001 Документы проекта и статус EP-0008 | documentation | inventory | US11-TK3 | US12-AC1 |
| 38 | TK-0002 Граф и ссылки | tooling | inventory | US12-TK1 | US12-AC2 |

Полный ID тикета: `EP-0009-US-00XX-TK-000Y-<slug>`; slug указан в заголовке тикета.

---

## US-0001 — Правила боя как данные

**User story.** Как дизайнер, я задаю правила боя, классы брони, режимы и боевые параметры модулей в JSON, а движок отвергает некорректные значения при загрузке.

**Acceptance criteria**
- **US1-AC1:** Файл правил загружается через `Settings.json`. На каждое невалидное значение — `ContentException` с указанием файла и поля:
  - база шанса не в диапазоне 0..100;
  - min > max;
  - `hitDisableMs` < 0;
  - доля для правила гибели вне (0, 1];
  - неизвестная роль модуля;
  - класс брони < 0.
- **US1-AC2:** Схема модулей EP-0008 расширена полями EP-0009, каждое проверяется только для своей категории:
  - торпедный аппарат: `accuracy`, `armorPiercingPercent` 0..100, `armorDamagePercent` > 0, `launcherReloadMs` > 0, `torpedoMaxFlightTimeMs` > 0;
  - ПРО: `barrels` ≥ 1, `burstIntervalMs` ≥ 0;
  - двигатель: `maneuverability`;
  - мостик: `combatRole`.

  Поля EP-0008 (`accuracy` ПРО, `torpedoManeuverability`, дальности, `maxFlightTimeMs`) переиспользуются, а не дублируются.
- **US1-AC3:** У экипажа есть навыки `accuracySkill` и `maneuverabilitySkill` (0..30), и они влияют на бросок. У установленного модуля в сценарии есть `armorClass`. Сценарии без этих полей загружаются со значениями по умолчанию: `none` и навыки 0.

### TK-0001 — `module-health-contracts` · contracts · 5 файлов
**Задача.**
1. `InstalledModuleSnapshot` получает поля `ArmorCurrent`, `ArmorMax`, `StructureCurrent`, `StructureMax`, `DisabledUntilMotionTimeMs?`, `IsDestroyed`, `CombatRole?`.
2. `CombatEventType` получает новые значения после существующих (включая значения EP-0008): `TorpedoHitRoll`, `ModuleHit`, `ModuleDestroyed`, `ShipDestroyed`, `BurstShotSkipped`, `LauncherBreakdown`.
3. `CombatJournalEntry` получает поля `Attack`, `Defense`, `Roll`, `HitModuleId?`, `ArmorBefore/After`, `StructureBefore/After`, `DestroyReason?`. Поля EP-0008 (`Accuracy`, `TargetManeuverability`, `LaunchMode`, `TerminationKind`) остаются; в doc-комментариях их смысл уточняется как составляющих атаки и защиты.
4. Hull-поля удаляются из snapshot (`HullCombatSnapshot` или эквивалент на HEAD).

**Файлы:** файл с `InstalledModuleSnapshot`, `CombatJournalEntry.cs`, `CombatSnapshot.cs`, `tests/DeepSpaceSaga.Contracts.Tests/CombatSnapshotTests.cs`, `tests/DeepSpaceSaga.Contracts.Tests/CombatEventTests.cs`.
**Тесты:** `Module_health_fields_roundtrip_json`, `New_event_types_after_ep8_values`, `Journal_roll_breakdown_roundtrip`, `No_hull_fields_in_snapshot`.
**DoD:** изменены только Contracts. Потребители hull, которые сломаются, чинятся в US-0003; это фиксируется в evidence.

### TK-0002 — `fire-contracts` · contracts · 5 файлов
**Задача.**
1. `LauncherCombatSnapshot` получает `ModeId`, `ReadyAtMotionTimeMs`, `IsBrokenUntilDock`, `ActiveTorpedoIds[]`. `ActiveTorpedoObjectId` (EP-0006/0008) удаляется.
2. `DefenseSnapshot` получает `Barrels`, `PendingBurstShots[]` (время, ID торпеды, номер ствола) и `ReadyAtMotionTimeMs`. Перезарядка EP-0008 «после удаления» в контракте больше не описывается.
3. `TorpedoSnapshot`: константа `HitChancePercent` (100 в EP-0006) заменяется полем `HitChanceTenths`. Захваченная атака хранится рядом с `CapturedManeuverability` из EP-0008.
4. Константа `TorpedoLauncherCommandTypes.SetMode = "torpedo.setMode"` и параметр `ModeId` в `PlayerCommand`, если на HEAD нет общего поля параметра.
5. `DefenseLaunchPreview` (EP-0008, `02c89d4`) получает разложение шанса: атака, защита, база.

**Файлы:** `CombatSnapshot.cs`, `CountermeasureSnapshot.cs` (или файл с `DefenseSnapshot`), `PlayerCommand.cs`, `tests/DeepSpaceSaga.Contracts.Tests/CombatSnapshotTests.cs`, `tests/DeepSpaceSaga.Contracts.Tests/DefenseLaunchPreviewTests.cs`.
**Тесты:** `Launcher_mode_ready_and_multiple_torpedoes_roundtrip`, `Defense_burst_queue_roundtrip`, `Torpedo_hit_chance_not_constant`, `Preview_has_attack_defense_breakdown`.

### TK-0003 — `combat-rules-content` · engine · 5 файлов
**Задача.**
1. `Settings.json` получает ключи `typeData.combatRules` → `Data/Combat/combat-rules.json` и `typeData.fireModes` → `Data/Combat/fire-modes.json`.
   ```json
   {"chance":{"base":50,"min":0,"max":100},
    "damage":{"hitDisableMs":60000,"hitLocationWeighting":"mass"},
    "shipDestruction":{"criticalRoles":["bridge","engine"],"destroyedModuleFraction":0.3333},
    "armorClasses":{"none":0,"light":60}}
   ```
2. В `fire-modes.json` поля: `modeId`, `displayName`, `accuracyBonus`, `maneuverabilityBonus`, `breakdownChancePercent`, `reloadMultiplier`, `appliesTo[]`. Значения D16. «Стандарт» доступен торпедному аппарату и ПРО, остальные режимы — только торпедному аппарату.
3. `CombatRulesDefinition` и `FireModeDefinition` в `Engine/Content/`. Загрузка в `EngineContentLoader` рядом с `LoadShipClasses`, регистрация в `GameDataRegistry`, валидация по US1-AC1.

**Файлы:** `Engine/Content/CombatRulesDefinition.cs` (новый), `EngineContentLoader.cs`, `GameDataRegistry.cs`, `Client/Settings.json`, `tests/DeepSpaceSaga.Engine.Tests/CombatRulesContentTests.cs` (новый; тесты на временной копии контента, как в `TorpedoLauncherContentTests`).
**Тесты:** `Rules_load`, `Invalid_chance_bounds_rejected`, `Unknown_role_rejected`, `Mode_for_unknown_category_rejected`, `Missing_settings_key_names_file`.

### TK-0004 — `module-combat-schema` · engine · 4 файла
**Задача.**
1. В `ModuleTypeDefinition` и DTO загрузчика к полям EP-0008 добавить:
   - торпедный аппарат: `accuracy`, `armorPiercingPercent`, `armorDamagePercent`, `launcherReloadMs`, `torpedoMaxFlightTimeMs`;
   - ПРО: `barrels`, `burstIntervalMs`;
   - двигатель: `maneuverability`;
   - любой модуль: `combatRole`.
2. Расширить валидацию торпедного аппарата и ПРО. На `99e15a2` это `ValidateTorpedoParameters` и `GameDataRegistry.ValidateWeaponRatings`, на HEAD — их наследники из EP-0008-US-0001-TK-0003.
3. Если после EP-0008 остался `*BaseRating`, он удаляется в US-0002-TK-0001.

**Файлы:** `ModuleTypeDefinition.cs`, `EngineContentLoader.cs`, `GameDataRegistry.cs`, `tests/DeepSpaceSaga.Engine.Tests/CombatContentLoaderTests.cs`.
**Тесты:** `Launcher_new_fields_validated_for_launcher_only`, `Barrels_zero_rejected`, `Piercing_over_100_rejected`, `Engine_maneuverability_negative_rejected`.

### TK-0005 — `crew-skills-and-armor-schema` · engine · 5 файлов
**Задача.**
1. У экипажа появляются `accuracySkill` и `maneuverabilitySkill` (0..30). Навыки, которые EP-0008 оставил «только данными» (`torpedoSkill`/`countermeasureSkill` или их переименования на HEAD), загрузчик продолжает принимать до US-0009-TK-0003, но рантайм их не читает.
2. У установленного модуля появляется необязательное поле `armorClass`, по умолчанию `none`.
3. Валидация в `ScenarioLoader`: неизвестный класс брони или навык вне 0..30 → `ScenarioException`. Правило «один член экипажа на модуль» сохраняется.

**Файлы:** `Scenario/ScenarioData.cs`, `Scenario/ScenarioLoader.cs`, `SimulationEngine.cs` (сборка), `tests/DeepSpaceSaga.Engine.Tests/WeaponOperatorSchemaTests.cs`, `tests/DeepSpaceSaga.Engine.Tests/ScenarioArmorSchemaTests.cs` (новый).
**Тесты:** `Unknown_armor_class_rejected`, `Skill_31_rejected`, `Legacy_scenario_defaults`, `One_crew_per_module_kept`.

---

## US-0002 — Каталог модулей и кораблей

**User story.** Как игрок, я получаю модули и корабли с числами, проверенными симуляцией.

**Acceptance criteria**
- **US2-AC1:** Торпедный аппарат, обе ПРО (двух- и одноствольная) и двигатели имеют значения D25–D28 вместо чисел EP-0008 (55/5, 200/100/200 км, 12 км/с, 60 с, 10 с).
- **US2-AC2:** `structurePointsMax` = 150 у всех модулей. Тест проверяет правило 1/2 (D22) для каждого модуля боевых сценариев.
- **US2-AC3:** Классы `ship.mukha` и `ship.shmel` имеют раскладку корпуса и изображение. Поле `hullHitPointsMax` удалено из схемы и данных.

### TK-0001 — `weapon-catalog` · content-data · 4 файла
**Задача.**
1. Торпедный аппарат:
   - `accuracy` 40;
   - `torpedoManeuverability` 50 (было 5);
   - урон 150;
   - 10 км/с (было 3), поворот 90;
   - дальность пуска 150 км (было 200);
   - `armorPiercingPercent` 20, `armorDamagePercent` 100;
   - `launcherReloadMs` 15000;
   - `torpedoMaxFlightTimeMs` 120000.
2. ПРО:
   - `accuracy` 58 (было 55);
   - дальность авто/ручная 150/150 (было 100/200, O7);
   - 20 км/с (было 12);
   - `maxFlightTimeMs` 30000 (было 60000);
   - `countermeasureReloadMs` 14500 (было 10000);
   - `barrels` 2, `burstIntervalMs` 2000.
3. Новый модуль `module.countermeasure.launcher.single` («ПРО одноствольная»): те же значения, но `barrels` 1. Категория та же, поэтому команды общие.
4. Удалить `*BaseRating`, если он остался.

**Файлы:** `modules-torpedolauncher.json`, `modules-countermeasurelauncher.json`, `EngineContentLoader.cs` (удаление полей), `tests/DeepSpaceSaga.Client.Tests/TorpedoLauncherContentTests.cs`.
**Тесты:** `Launcher_catalog_matches_ep9`, `Pd_catalog_matches_ep9`, `Single_barrel_pd_exists`.

### TK-0002 — `engine-catalog` · content-data · 4 файла
**Задача.**
1. `module.engine.basic`: `maneuverability` 10, `maxSpeedMps` 6000 (O2).
2. Новый `module.engine.strizh` со значениями D26.
3. Проверки typeId `"module.engine.basic"` в Engine (на `99e15a2`: `SimulationEngine.cs:2955`, `:3108`) и выбор двигателя контроллером EP-0008 (A05: стабильный ordinal) переводятся на категорию `module.engine`. Тикет расширен на Engine по A12.
4. Регрессия рейсов, топлива и экономики (скорость 6 км/с) и орбиты ИИ EP-0008 (лимиты двигателя).

**Файлы:** `modules-engine.json`, `SimulationEngine.cs`, `tests/DeepSpaceSaga.Engine.Tests/EngineCategoryDispatchTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/VoyageFuelAccountingTests.cs`.
**Тесты:** `Strizh_accepts_engine_commands`, `Basic_engine_six_kms`, регрессия рейсов и орбиты.

### TK-0003 — `structure-normalization` · content-data · 5 файлов за проход
**Задача.**
1. `structurePointsMax` = 150 у всех модулей `Data/Modules/**`, включая `living.quarters.mk1.three-cabins` из EP-0008.
2. `combatRole: "bridge"` у мостика.
3. Текущие `structurePoints` > 150 в сценариях исправляются в US-0009-TK-0003. Здесь только составляется список мест.

**Файлы:** JSON модулей (больше 5 — по A12, с обоснованием), `tests/DeepSpaceSaga.Client.Tests/ModuleStructureCatalogTests.cs` (новый).
**Тесты:** `All_modules_structure_150`, `Rule_one_hit_unarmored_two_hits_light`.

### TK-0004 — `hostile-ship-classes` · content-data · 5 файлов
**Задача.**
1. `ship-classes.json`: удалить `hullHitPointsMax`, добавить `ship.mukha` и `ship.shmel`. `ShipClassDefinition` → `(TypeId)`, проверка `> 0` удаляется.
2. Раскладка корпуса: у Мухи 2 модуля, у Шмеля 3. Проверить ограничения клеток.
3. Изображение по `shipClassId`. Если спрайта нет, временно использовать спрайт малого корабля и записать это в evidence.

**Файлы:** `Data/Ships/ship-classes.json`, `Content/ShipClassDefinition.cs`, `GameDataRegistry.cs`, `EngineContentLoader.cs`, `tests/DeepSpaceSaga.Client.Tests/TetrarchClassContentTests.cs`.
**Тесты:** `Ship_classes_have_no_hull_hp`, `Mukha_and_shmel_load`.

---

## US-0003 — HP в модулях

**User story.** Как игрок, я вижу, что торпеда попадает в конкретный модуль. Броня гасит урон, модуль выключается на минуту и может погибнуть. Корабль гибнет, если потерял мостик, двигатель или треть модулей.

**Acceptance criteria**
- **US3-AC1:** Каждый установленный модуль имеет `ArmorMax` = значение класса брони и `StructureMax` = `structurePointsMax`. Текущие значения берутся из сценария или сохранения. Hull-состояние удалено (`_hullCombat` и его наследник из EP-0008).
- **US3-AC2:** Попадание выбирает живой модуль с весом по массе через поток `combat.hit.location.v1`. Урон по D09, выключение по A05. Пример: 150 урона по броне `light` — броня 60 → 0, структура 150 → 60; второе попадание уничтожает модуль.
- **US3-AC3:** Корабль гибнет по D11. Есть один путь гибели `DestroyShip(objectId, cause, t)`, его используют и бой, и охрана станции:
  - NPC оставляет обломок;
  - для игрока срабатывает терминальное поражение EP-0008 (US-0009 EP-0008), у которого lethal-условие «HP = 0» заменено на D11.

  Хардкод `"ship.tetrarch"` удалён из Engine и Client.
- **US3-AC4:** Выключенный или уничтоженный модуль не выполняет команд. ПРО при выключении отменяет очередь. Двигатель ведёт себя по A03: контроллер ИИ EP-0008 при выключенном двигателе не маневрирует.

### TK-0001 — `module-health-runtime` · engine · 5 файлов
**Задача.**
1. Runtime установленного модуля (`InstalledModuleRuntime`) получает `Armor`, `ArmorMax`, `DisabledUntilMs`, `Destroyed`.
2. Броня берётся из класса брони в сценарии.
3. Новые поля проецируются в snapshot.
4. Hull-состояние и его сборка удаляются (на `99e15a2`: `Combat.cs:10`, `BuildCombatState` `:453-494`).

**Файлы:** `SimulationEngine.cs`, `SimulationEngine.Combat.cs`, `tests/DeepSpaceSaga.Engine.Tests/CombatBootstrapTests.cs`, `tests/DeepSpaceSaga.Engine.Tests/ModuleHealthBootstrapTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/BasicCombatEndToEndTests.cs`.
**Тесты:** `Light_armor_module_starts_60_150`, `Legacy_modules_no_armor`, `No_hull_state`.

### TK-0002 — `module-damage` · engine · 4 файла
**Задача.**
1. Применение урона в hull заменяется на `ApplyModuleHit(ship, captured, t)`.
2. Выбор модуля: живые модули в ordinal-порядке по `moduleId`, вес `massKg`. Новый поток ГСЧ `combat.hit.location.v1` (`RngStreamNames`), его состояние сохраняется.
3. Урон по D09 с параметрами, захваченными при пуске.
4. Выключение по A05; при структуре ≤ 0 модуль уничтожен.
5. Записи журнала `ModuleHit` и `ModuleDestroyed`.

**Файлы:** `SimulationEngine.Combat.cs`, `Rng/RngStreamNames.cs`, `Combat/ModuleDamageMath.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/ModuleDamageTests.cs` (новый).
**Тесты:** `Damage_split_150_vs_light`, `Unarmored_dies_one_hit`, `Hit_location_by_mass_deterministic`, `Disable_extends_not_accumulates`.

### TK-0003 — `ship-destruction` · engine · 5 файлов
**Задача.**
1. Правило D11: N — число модулей на начало боя, порог `ceil(N × fraction − 1e-9)`.
2. `DestroyShip` объединяет создание обломка, гибель от охраны станции и терминальный переход игрока EP-0008. Lethal-условие EP-0008 переводится с HP корпуса на D11. Поражение срабатывает один раз, часы останавливаются на событии — это правило EP-0008 сохраняется.
3. Запись журнала `ShipDestroyed` с причиной `critical:<moduleId>` или `threshold:<dead>/<N>`.

**Файлы:** `SimulationEngine.Combat.cs`, `Dialogue/StationSecuritySystem.cs`, файл терминального перехода EP-0008 (на HEAD), `tests/DeepSpaceSaga.Engine.Tests/ShipDestructionRulesTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/PlayerDefeatTests.cs` (EP-0008; адаптировать).
**Тесты:** `Two_of_nine_survives_three_kills`, `Bridge_kills`, `Engine_kills`, `Mukha_one_module_kills`, `Npc_becomes_wreck`, `Player_module_death_triggers_ep8_defeat_once`.

### TK-0004 — `disabled-module-effects` · engine · 4 файла
**Задача.**
1. Условие готовности модуля (`CanExecuteModuleCommand`) дополнительно требует `!Destroyed && t ≥ DisabledUntilMs`. Новые коды причин: `ModuleDisabled`, `ModuleDestroyed`.
2. Корабль с выключенным двигателем летит по инерции.
3. Контроллер ИИ EP-0008 пропускает решения о движении, пока двигатель выключен. Решения о стрельбе продолжаются, если аппарат жив.
4. Активный цикл выключенного модуля прерывается.

**Файлы:** `SimulationEngine.cs`, `SimulationEngine.Countermeasures.cs`, файл контроллера ИИ EP-0008 (на HEAD), `tests/DeepSpaceSaga.Engine.Tests/DisabledModuleTests.cs` (новый).
**Тесты:** `Disabled_launcher_rejects_fire`, `Disabled_pd_cancels_burst`, `Disabled_engine_coasts_and_ai_holds`, `Module_back_after_60s`.

---

## US-0004 — Броски навыков

**User story.** Как игрок, я понимаю, почему торпеда попала или промахнулась. Журнал и превью показывают атаку, защиту, шанс и бросок, а навыки экипажа на них влияют.

**Acceptance criteria**
- **US4-AC1:** Одна функция шанса D04 заменяет формулу EP-0008 `clamp(Accuracy − Maneuverability)` во всех вызовах, включая превью. Тесты: 60 против 60 → 500, 200 против 0 → 1000, 0 против 200 → 0. Пример EP-0008 «55 − 5 = 50%» больше не действует.
- **US4-AC2:** Бросок D05 выполняется в момент контакта торпеды с кораблём, в потоке `combat.torpedo.hit.v1`. Аппарат 40 + оператор 20 против «Стрижа» 40 + пилот 20 → 50%. Промах — по A01.
- **US4-AC3:** Перехват считается по D06 в существующем потоке EP-0008 `combat.countermeasure.intercept.v1`. ПРО 58 + оператор 20 против торпеды 50 + оператор 20 → 58%. Правило EP-0008 «при контакте с шансом 0% или 100% бросок всё равно расходуется» сохраняется.

### TK-0001 — `chance-math` · engine · 4 файла
**Задача.**
1. Функция шанса EP-0008 (на HEAD — наследник `InterceptionMath.ChanceTenths`) заменяется на `CombatChanceMath.ChanceTenths(attack, defense, rules)`.
2. Убирается обнуление влияния навыков (EP-0008 TK-0005): навыки прибавляются напрямую. Формулу `base × skill/50` из EP-0007 не возвращать.

**Файлы:** `Combat/CombatChanceMath.cs` (переименование), `SimulationEngine.WeaponOperators.cs`, `SimulationEngine.Countermeasures.cs` (вызовы), `tests/DeepSpaceSaga.Engine.Tests/InterceptionChanceTests.cs` (переписать).
**Тесты:** `Equal_gives_500`, `Clamp_bounds`, `Rules_base_changes_result`, `Skills_change_result`.

### TK-0002 — `torpedo-hit-roll` · engine · 4 файла
**Задача.**
1. При пуске захватывается атака: точность аппарата + навык оператора. Бонус режима до US-0006 равен 0.
2. В момент контакта берётся защита цели по D05.
3. Бросок в потоке `combat.torpedo.hit.v1`.
4. Попадание → `ApplyModuleHit`. Промах → удаление торпеды с `TerminationKind.Miss`.
5. Запись журнала `TorpedoHitRoll`; в snapshot — `HitChanceTenths`.

**Файлы:** `SimulationEngine.Combat.cs`, `Rng/RngStreamNames.cs`, `tests/DeepSpaceSaga.Engine.Tests/TorpedoHitRollTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/TorpedoImpactTests.cs`.
**Тесты:** `Fifty_percent_example`, `Disabled_engine_target_easier`, `Miss_no_damage`, `Partition_invariance`.

### TK-0003 — `intercept-roll-skills` · engine · 5 файлов
**Задача.**
1. Перехват считается по D06. Атака захватывается при пуске противоракеты, защита — при пуске торпеды.
2. `DefenseLaunchPreview` EP-0008 показывает новый шанс и его разложение.

**Файлы:** `SimulationEngine.Countermeasures.cs`, `SimulationEngine.WeaponOperators.cs`, файл построения превью EP-0008 (на HEAD), `tests/DeepSpaceSaga.Engine.Tests/CountermeasureResolutionTests.cs`, `tests/DeepSpaceSaga.Engine.Tests/DefensePreviewTests.cs` (EP-0008; адаптировать).
**Тесты:** `Intercept_58_example`, `Preview_matches_resolution`, `Zero_and_hundred_still_draw`.

---

## US-0005 — Темп стрельбы

**User story.** Как игрок, я стреляю по перезарядке и не жду, пока долетит прошлая торпеда. Моя ПРО бьёт очередью из двух стволов.

**Acceptance criteria**
- **US5-AC1:** Торпедный аппарат игрока или NPC готов, когда `t ≥ ReadyAt`, где `ReadyAt = t_пуска + launcherReloadMs × reloadMultiplier`. Правила EP-0008 «один активный снаряд на аппарат» и «пуск NPC после окончания полёта» (A07 EP-0008) удалены. В полёте может быть несколько торпед.
- **US5-AC2:** Очередь ПРО работает по D14 и A06. Отложенные выстрелы — это события цикла боя с порядком по §3.2.
- **US5-AC3:** Перезарядка ПРО считается по D13, а не «после удаления противоракеты». Общий auto-attempt flag EP-0008 удалён и заменён правилом D15. Арбитраж двух ПРО из EP-0008 сохраняется.

### TK-0001 — `launcher-cooldown` · engine · 4 файла
**Файлы:** `SimulationEngine.Combat.cs` (guard Busy; на `99e15a2` `:40`, `:65`, `:101`, `:281`), `tests/DeepSpaceSaga.Engine.Tests/TorpedoLaunchTests.cs`, `tests/DeepSpaceSaga.Engine.Tests/HostileFireTests.cs` (EP-0008), `tests/DeepSpaceSaga.Engine.Tests/LauncherCooldownTests.cs` (новый).
**Тесты:** `Second_launch_after_15s_while_first_flying`, `Launch_before_ready_rejected_with_reason`, `Npc_fires_on_cooldown_not_after_flight`, `Idempotent_command_id`.

### TK-0002 — `pd-burst` · engine · 4 файла
**Файлы:** `SimulationEngine.Countermeasures.cs`, `SimulationEngine.Combat.cs` (события очереди), `tests/DeepSpaceSaga.Engine.Tests/PdBurstTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/CountermeasureEndToEndTests.cs`.
**Тесты:** `Second_barrel_after_2s_same_torpedo`, `Skipped_if_torpedo_dead`, `Single_barrel_fires_once`, `Disabled_pd_drops_pending`, `Burst_partition_invariance`.

### TK-0003 — `pd-reload-and-attempts` · engine · 5 файлов
**Задача.**
1. Перезарядка ПРО по D13. Перезарядка EP-0008 «после удаления» и её связь с MissedCoast удаляются.
2. Удаляется auto-attempt flag: поле в runtime торпеды и список в сохранении (сохранение меняется в US-0011).
3. Автоматика работает по D15 с арбитражем EP-0008.

**Файлы:** `SimulationEngine.Countermeasures.cs`, `tests/DeepSpaceSaga.Engine.Tests/AutomaticDefenseTests.cs`, `tests/DeepSpaceSaga.Engine.Tests/DefenseArbitrationTests.cs` (EP-0008), `tests/DeepSpaceSaga.Engine.Tests/PdReloadTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/CountermeasureBootstrapTests.cs`.
**Тесты:** `Reload_from_first_shot_14_5s`, `Retry_after_miss_allowed`, `No_auto_while_cm_in_flight`, `Arbitration_unchanged`.

---

## US-0006 — Режимы торпедного аппарата

**User story.** Как игрок, я выбираю режим аппарата («Стандарт», «Манёвренность» или «Перегрев») и понимаю, чем за него плачу: нестандартный режим стреляет реже, а «Перегрев» может сломать аппарат.

**Acceptance criteria**
- **US6-AC1:** Бонусы режима захватываются при пуске: точность — в атаку броска попадания, манёвренность — в защиту торпеды от перехвата. Перезарядка умножается на `reloadMultiplier`.
- **US6-AC2:** «Перегрев»: после пуска делается бросок в потоке `combat.launcher.breakdown.v1`. При поломке аппарат сломан до дока, в журнале появляется запись `LauncherBreakdown`.
- **US6-AC3:** Команда `torpedo.setMode`:
  - вне боя (нет живых торпед и врагов в пределах дальности пуска) действует сразу;
  - в бою выставляет `ReadyAt = max(ReadyAt, t) + launcherReloadMs`;
  - идемпотентна;
  - отклоняется, если режим неприменим к категории модуля;
  - на паузе ведёт себя так же, как ручные команды EP-0008 (A08).

  Клиент показывает выбор режима и задержку.

### TK-0001 — `mode-runtime` · engine · 5 файлов
**Файлы:** `SimulationEngine.Combat.cs`, `Rng/RngStreamNames.cs`, `Combat/FireModeMath.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/FireModeRuntimeTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/TorpedoHitRollTests.cs`.
**Тесты:** `Maneuverability_mode_lowers_intercept_to_48`, `Overheat_breakdown_deterministic`, `Nonstandard_reload_18_75s`, `Broken_launcher_rejects_until_dock`.

### TK-0002 — `set-mode-command` · engine · 4 файла
**Файлы:** `SimulationEngine.Combat.cs`, `Client/Data/Commands/TorpedoLauncher/commands.json`, `SimulationEngine.cs` (диспетчер), `tests/DeepSpaceSaga.Engine.Tests/SetFireModeTests.cs` (новый).
**Тесты:** `Out_of_combat_immediate`, `In_combat_costs_one_cycle`, `Pd_rejects_non_standard`, `Paused_switch_consistent`, `Duplicate_ignored`.

### TK-0003 — `mode-selector-ui` · client · 5 файлов
**Задача.**
1. На панели торпедного аппарата (EP-0006/EP-0008): переключатель трёх режимов, отложенная смена с таймером, состояние «Сломан до дока».
2. Локализация названий режимов.

**Файлы:** `GameSessionScreen.Combat.cs`, панель торпедного аппарата, `Data/Locale/English.json`, `Data/Locale/Russian.json`, `tests/DeepSpaceSaga.Client.Tests/TorpedoCommandPanelTests.cs`.

---

## US-0007 — Ручная ПРО EP-0008 под очередь

**User story.** Как игрок, я по-прежнему сам выбираю торпеду и стреляю из каждой ПРО, но теперь каждый пуск — это очередь из двух стволов.

**Acceptance criteria**
- **US7-AC1:** `defense.fire` из EP-0008 запускает очередь (D14). Ограничения EP-0008 сохраняются:
  - ручная дальность, теперь 150 км;
  - готовность установки и наличие оператора;
  - пуск разрешён, даже если по торпеде уже летит другая противоракета;
  - шанс 0% и неизвестная точка встречи пуск не блокируют.

  Новые причины отказа: `ModuleDisabled`, `ModuleDestroyed`, `NotReady` с `ReadyAt`.
- **US7-AC2:** Панели двух ПРО из EP-0008 показывают число стволов, отложенный второй выстрел, перезарядку от первого выстрела и шанс D06 с разложением.

### TK-0001 — `manual-fire-burst` · engine · 4 файла
**Файлы:** `SimulationEngine.Countermeasures.cs`, обработчик `defense.fire` из EP-0008 (на HEAD), `tests/DeepSpaceSaga.Engine.Tests/ManualDefenseFireTests.cs` (EP-0008; адаптировать), `tests/DeepSpaceSaga.Engine.Tests/PdBurstTests.cs`.
**Тесты:** `Manual_fire_starts_burst`, `Manual_allowed_while_other_cm_in_flight`, `Rejects_disabled_pd`, `Idempotent`.

### TK-0002 — `defense-panels-burst-ui` · client · 5 файлов
**Файлы:** панель ПРО из EP-0008 (на HEAD), `GameSessionScreen.Countermeasures.cs`, `ObjectInfoPanel.cs`, `Data/Locale/Russian.json`, `tests/DeepSpaceSaga.Client.Tests/DefenseCommandPanelTests.cs`.

---

## US-0008 — Мухи и Шмели на ИИ EP-0008

**User story.** Как игрок, я встречаю вражеские истребители. Они заходят с флангов, кружат на расстоянии 50 км и стреляют по перезарядке. ПРО Шмеля защищает его от моих торпед.

**Acceptance criteria**
- **US8-AC1:** NPC стреляют через исполнитель EP-0008. Пуск разрешён, если аппарат готов (US5-AC1), цель не дальше дальности пуска и расчётный перехват ≤ 0,8 × `torpedoMaxFlightTimeMs` (D33). Торпеды NPC живут 120 с; исключение EP-0008 «NPC не получает TTL торпед» отменено.
- **US8-AC2:** Контроллер EP-0008 получает из сценария параметры `standoffKm`, `bandKm` и `flankOffsetDeg`. По умолчанию поведение остаётся как в EP-0008: радиус `0.5 × TorpedoRangeKm`, без смещения. Фланговый заход и отход — по D33. Орбита, выбор направления, гистерезис и шаг решений 100 мс остаются как в EP-0008.
- **US8-AC3:** При выключенном двигателе ИИ не маневрирует (US3-AC4). Состояние ИИ (фаза, направление, параметры) сохраняется (US-0011).

### TK-0001 — `npc-fire-cadence` · engine · 4 файла
**Файлы:** файл планировщика огня NPC из EP-0008 (на HEAD), `SimulationEngine.Combat.cs`, `tests/DeepSpaceSaga.Engine.Tests/HostileFireTests.cs` (EP-0008; адаптировать), `tests/DeepSpaceSaga.Engine.Tests/NpcTorpedoLifetimeTests.cs` (новый).
**Тесты:** `Npc_fires_every_15s_when_reachable`, `No_fire_when_intercept_unreachable`, `Npc_torpedo_expires_120s`.

### TK-0002 — `flank-approach-math` · motion · 3 файла
**Задача.** В Motion-математику относительной орбиты EP-0008 (US-0004-TK-0002 EP-0008) добавить параметр радиуса и фазу захода с фланговым смещением. Формулы должны совпадать с `src/DeepSpaceSaga.CombatSim/combatsim/ai.py` (`AttackAI`). Курс: 0° — вверх, отсчёт по часовой стрелке.
**Файлы:** файл относительной орбиты из EP-0008 (на HEAD), `tests/DeepSpaceSaga.Motion.Tests/<RelativeOrbit>Tests.cs` (EP-0008; дополнить), `tests/DeepSpaceSaga.Motion.Tests/FlankApproachTests.cs` (новый).
**Тесты:** `Default_params_equal_ep8_behavior`, `Far_target_lead_with_flank`, `Inside_band_orbits`, `Too_close_retreats`.

### TK-0003 — `ai-params-from-scenario` · engine · 5 файлов
**Файлы:** `Scenario/ScenarioData.cs` (блок `ai` у NPC), `Scenario/ScenarioLoader.cs` (валидация), файл контроллера ИИ EP-0008 (на HEAD), `tests/DeepSpaceSaga.Engine.Tests/HostileAttackAiTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/PirateScenarioTests.cs`.
**Тесты:** `Mukha_closes_to_50km_and_orbits`, `Flank_offsets_spread_attackers`, `Missing_ai_block_uses_ep8_defaults`, `Partition_invariance`.

---

## US-0009 — Боевые сценарии

**User story.** Как игрок, в New Game я выбираю бой «Тетрарх против 3 Мух» (лёгкий) или «Тетрарх против 3 Шмелей» (равный).

**Acceptance criteria**
- **US9-AC1:** `Scenarios/CombatMukha/scenario.json` виден в New Game и имеет описание.
  - Тетрарх: комплектация EP-0008 (2 ПРО, 3 оператора + пилот), броня `light` на всех 9 модулях, позиция (0, 0) км, курс 0°, 2 км/с. Бой начинается на паузе, как в EP-0008.
  - Три Мухи с `relationToPlayer: enemy`: позиции (−40, 150), (0, 165) и (40, 150) км, курс 180°, 3 км/с, ИИ `standoffKm` 50, `bandKm` 10, `flankOffsetDeg` −35 / 0 / +35, броня `none`.
  - Навыки всех операторов 20.
- **US9-AC2:** `Scenarios/CombatShmel/scenario.json`: то же, но с тремя Шмелями (одноствольная ПРО и её оператор).
- **US9-AC3:** Остальные сценарии загружаются по новой схеме:
  - `structurePoints` ≤ 150;
  - у экипажа навыки `accuracy` и `maneuverability`;
  - устаревшие поля навыков удалены из данных и схемы;
  - `PlayerShipOnly` — по решению O6.

### TK-0001 — `scenario-combat-mukha` · content-data · 3 файла
**Файлы:** `Client/Scenarios/CombatMukha/scenario.json` (новый), `tests/DeepSpaceSaga.Client.Tests/CombatScenariosContentTests.cs` (новый), `Client/DeepSpaceSaga.Client.csproj` (только если копирование `Scenarios\**` не подхватит новую папку).
**Тесты:** `Mukha_scenario_listed_and_loads`, `Positions_ai_armor_match_spec`.

### TK-0002 — `scenario-combat-shmel` · content-data · 3 файла
**Файлы:** `Client/Scenarios/CombatShmel/scenario.json` (новый), `CombatScenariosContentTests.cs`, `tests/DeepSpaceSaga.Engine.Tests/HostileAttackAiTests.cs` (фикстура).
**Тесты:** `Shmel_scenario_loads`, `Shmel_has_single_barrel_pd_and_operator`.

### TK-0003 — `scenarios-migrate-schema` · content-data · 5 файлов за проход
**Файлы:** `Scenarios/{Default, Default_500, Docked, Undocked, MarketProfiles, PlayerShipOnly}/scenario.json` (больше 5 — по A12), `tests/DeepSpaceSaga.Client.Tests/TetrarchLoadoutTests.cs`; удаление устаревших навыков из `ScenarioData.cs` (по A12).
**Тесты:** `All_scenarios_load`, `Player_tetrarch_light_armor`, `No_legacy_skill_fields`.

---

## US-0010 — Интерфейс модулей и журнал

**User story.** Как игрок, я вижу состояние каждого модуля и броски в журнале, а поражение наступает по новым правилам гибели.

**Acceptance criteria**
- **US10-AC1:** Окно поражения EP-0008 открывается, когда игрок гибнет по D11. Его поведение не меняется: кнопки «Загрузить» и «Главное меню», сохранение запрещено.
- **US10-AC2:** У выбранного корабля для каждого модуля видны броня, структура, таймер выключения и отметка «уничтожен». Полоса HP корпуса у метки на карте (на `99e15a2`: `ObjectLabelRenderer.cs:16-18`, `:348-357`) заменяется суммарной полосой модулей и больше не зависит от `"ship.tetrarch"`.
- **US10-AC3:** В журнале EP-0007/0008 появляются строки:
  - бросок попадания (атака, защита, шанс, бросок);
  - перехват по новой формуле;
  - попадание в модуль (было → стало);
  - уничтожение модуля;
  - гибель корабля с причиной;
  - пропуск выстрела очереди;
  - поломка при «Перегреве».

### TK-0001 — `defeat-from-module-rule` · engine · 3 файла
**Задача.** Доказать граничные случаи сквозного пути «D11 → терминальный переход EP-0008 → snapshot поражения». Сам переход сделан в US3-TK3.
**Файлы:** файл терминального перехода EP-0008, `tests/DeepSpaceSaga.Engine.Tests/PlayerDefeatTests.cs`, `tests/DeepSpaceSaga.Engine.Tests/DefeatClockBoundaryTests.cs` (EP-0008).
**Тесты:** `Threshold_death_stops_clock_at_event`, `Critical_engine_death_once`, `No_events_after_defeat_in_same_interval`.

### TK-0002 — `module-health-ui` · client · 5 файлов
**Файлы:** `ObjectInfoPanel.cs`, `ObjectLabelRenderer.cs`, `GameSessionScreen.cs`, `MapView.cs`, `tests/DeepSpaceSaga.Client.Tests/CombatMapVisualTests.cs`.

### TK-0003 — `combat-journal-lines` · client · 4 файла
**Файлы:** `CombatJournalPanel.cs`, `Data/Locale/Russian.json`, `Data/Locale/English.json`, `tests/DeepSpaceSaga.Client.Tests/CombatJournalPanelTests.cs`.

---

## US-0011 — Сохранения и приёмка

**User story.** Как игрок, я сохраняюсь посреди боя и продолжаю его без потерь. Как автор, я вижу, что бой в игре соответствует симулятору.

**Acceptance criteria**
- **US11-AC1:** Формат сохранения 16 (EP-0008) → **17**; payload: defense 3, combat 2, hostile state 2. Формат 17 хранит:
  - броню, структуру, выключение и уничтожение модулей;
  - для аппарата: `ReadyAt`, режим, поломку и список летящих торпед;
  - очереди ПРО;
  - потоки `combat.hit.location.v1`, `combat.torpedo.hit.v1`, `combat.launcher.breakdown.v1` со счётчиками;
  - параметры ИИ.

  Hull и auto-attempt flag удалены. Сохранения формата 16 и ниже отвергаются с понятным сообщением, без миграции (как Q49 EP-0008). Проверка `HitChancePercent == 100` заменена проверкой диапазона. Запрет сохранения после гибели (EP-0008) сохраняется.
- **US11-AC2:** Сквозные Engine-тесты обоих сценариев на настоящем ИИ. Прогон по 100 сидам на каждый сценарий сравнивается с эталоном §7, допуск ±10 п.п. Если допуск превышен, это фиксируется с причиной и не блокирует тикет. Возможные причины: A02/A03, контроллер EP-0008 вместо `AttackAI` симулятора, дискретные повороты.
- **US11-AC3:** Симулятор читает настоящие данные игры (O5). Нативная приёмка в реальном окне:
  - оба сценария;
  - смена режима в бою;
  - ручная очередь двух ПРО;
  - выключение модуля;
  - гибель по порогу и по ключевому модулю;
  - окно поражения.

### TK-0001 — `save-format-17` · engine · 5 файлов
**Файлы:** `Scenario/ScenarioData.cs`, `Scenario/ScenarioLoader.cs`, `Scenario/CombatSaveData.cs`, `Scenario/CountermeasureSaveData.cs`, `SimulationEngine.CombatPersistence.cs`. Тесты по A12, шестым файлом: `tests/DeepSpaceSaga.Engine.Tests/CombatSaveLoadTests.cs`.
**Тесты:** `Roundtrip_mid_burst`, `Roundtrip_disabled_module_timer`, `Roundtrip_broken_overheat`, `Roundtrip_ai_params`, `V16_rejected_cleanly`, `Rng_counters_continue`.

### TK-0002 — `combat-acceptance-and-balance` · engine · 3 файла
**Файлы:** `tests/DeepSpaceSaga.Engine.Tests/ModuleCombatEndToEndTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs` (новый; категория `Balance`, вне быстрого набора), `Board/EP-0009-module-combat/evidence/balance.md`.

### TK-0003 — `simulator-on-game-data-and-native` · tooling · 5 файлов
**Задача.**
1. `src/DeepSpaceSaga.CombatSim/combatsim/gamedata.py` читает `Data/Modules`, `Data/Combat`, `Data/Ships` и `Scenarios/Combat*`.
2. `Data/CombatDesign/` удаляется.
3. Тесты симулятора обновляются.
4. Нативная матрица по US11-AC3: каждый пункт PASS / FAIL / NOT RUN с причиной.

**Файлы:** `gamedata.py`, `tests/test_core.py`, `README.md` симулятора, удаление `Data/CombatDesign/*.json`, `Board/EP-0009-module-combat/evidence/native.md`.

---

## US-0012 — Документация (обязательная завершающая история)

**Acceptance criteria**
- **US12-AC1:** Обновлены релевантные документы проекта:
  - `Documentation/00-Process/CLAUDE.md` (данные `combatRules`, `fireModes`);
  - `Documentation/04-Engineering/BasicTorpedoCombat.md`, `CountermeasureCombat.md` (или новый `ModuleCombat.md`);
  - `Documentation/02-FirstRelease/Mechanics`;
  - `Documentation/01-Requirements` — если там описаны формула или HP;
  - `Board/EP-0008-…/ImplementationStatus.md`: раздел «Superseded by EP-0009» со ссылкой на §2.1.
- **US12-AC2:** Граф (Graphify, `src/graphify-out/`) перестроен, ссылки проверены.

### TK-0001 — `update-project-documents` · documentation · inventory
Составить список документов, где упоминается хотя бы одно из:
- HP корпуса;
- формула EP-0008 «точность − манёвренность»;
- «навыки не влияют»;
- один снаряд на аппарат;
- auto-attempt flag;
- `ship.tetrarch` как единственный повреждаемый класс;
- числа 55/5, 200/100/200 км, 12 км/с, 60 с, 10 с.

Каждый документ обновить или указать, почему он не меняется.

### TK-0002 — `refresh-graph-and-links` · tooling · inventory
Перестроить граф и проверить ссылки, как в EP-0007 US-0010 TK-0002.

---

## 6. Тесты, которые сломаются — чинить в тикете, который их ломает

Ниже тесты, найденные на `99e15a2`. На HEAD с завершённым EP-0008 их нужно найти заново: добавятся тесты EP-0008 на формулу 55/5, правило «один снаряд», auto-attempt flag, NPC без cooldown, hull-lethal поражение и сохранения 16.

| Причина | Тесты (`99e15a2`) и тесты EP-0008 | Тикет |
|---|---|---|
| HP 450 и hull-lethal | `TetrarchClassContentTests`, `BasicCombatEndToEndTests`, `CombatBootstrapTests`, `CombatContentLoaderTests`, `BasicCombatUiFlowTests`, `CombatPresentationResumeTests`, `CombatMapVisualTests`, `TetrarchLoadoutTests`, `PirateScenarioTests`, поражение EP-0008 | US2-TK4, US3-TK1, US3-TK3, US10-TK2 |
| Урон проходит только по `ship.tetrarch` | `CombatMapVisualTests` | US10-TK2 |
| Один снаряд на аппарат, NPC стреляет после окончания полёта | `TorpedoLaunchTests`, `BasicCombatEndToEndTests`, `CombatSaveLoadTests`, огонь NPC EP-0008 | US5-TK1, US8-TK1, US11-TK1 |
| Формула шанса (EP-0007 и EP-0008) | `InterceptionChanceTests`, `WeaponOperatorRuntimeTests`, `CountermeasureBootstrapTests`, `TetrarchLoadoutTests`, превью EP-0008 | US4-TK1, US4-TK3 |
| Auto-attempt flag и перезарядка после удаления | `CountermeasureResolutionTests`, `AutomaticDefenseTests`, арбитраж EP-0008 | US5-TK3 |
| `HitChancePercent == 100`, сохранения 16 | `CombatSaveSchemaTests`, сохранения EP-0008 | US11-TK1 |

## 7. Эталон симулятора (для US11-AC2)

400 боёв на сценарий, все операторы 20, Тетрарх в режиме «Стандарт», данные D21–D33, ИИ симулятора `AttackAI`:

| Сценарий | Победы Тетрарха | Попаданий в Тетрарха | Торпед противника | Пусков ПРО Тетрарха | Бой (медиана) |
|---|---|---|---|---|---|
| 3 Мухи | 81,2% | 3,07 | 12,4 | 16,7 | 81 с |
| 3 Шмеля | 49,5% | 5,33 | 19,5 | 24,8 | 114 с |
| 3 Шмеля, режим «Манёвренность» | 43,5% | 5,51 | 19,3 | 24,1 | 108 с |

Ожидаемые причины расхождения с движком:
- допущения A02 и A03;
- в движке контроллер орбиты EP-0008, а не `AttackAI`;
- дискретные повороты;
- торпеда стартует по курсу носителя;
- раздельные дальности ПРО: автоматическая и ручная.

## 8. Self-containment

- Решения, числа, порядок событий, допущения, write scope, зависимости и проверки — в этом файле.
- Правила EP-0008 — в его `Documentation.md` и `Decisions.md`; что из них заменено, а что сохранено, сведено в §2.
- Ход обсуждения: `C:/DSS/docs/fight/CombatDesign-v1/` (букмарк 1) и `src/DeepSpaceSaga.CombatSim/`.
- Открытые решения O2–O7 автор закрывает до старта. O1 из revision 1 снят: EP-0008 реализован, EP-0009 его переопределяет.
