# Tetrarch Class

## Current EP-0012 contract — 2026-10-21

The canonical CombatMukha player is51t with9 modules, light armor60/structure150, four crew with all ten skills20 and dedicated module.engine.tetrarch (5km/s,600m/s²,20°/s, cruise3km/s). Two manual CM launchers and one point torpedo launcher use current profile/reload catalog values. CombatMukhaSingle retains the same player and SPC-MUKHA-01 only. CombatShmel has been removed. PlayerShipOnly keeps its own geometry/crew and receives current point loadout/Tetrarch engine; Default500 content remains unchanged. Hull grid geometry below remains applicable; old scenario counts/skills50/basic-engine/EP8 weapon statements are historical, not current loadout requirements. [Point-aim combat contract](../../04-Engineering/PointAimCombat.md).

## Актуализация EP-0009 — 2026-10-12

В `CombatMukha` и `CombatShmel` Тетрарх имеет9 модулей с `light` бронёй, четыре члена экипажа/каюты и назначения пилота и трёх операторов оружия. Он больше не использует Hull HP450 как правило гибели: важны структура модулей, критические роли и порог уничтожения. Остальные сценарии сохраняют собственные комплектации EP-0008; таблица ниже является датированным срезом. [Текущие требования](../../01-Requirements/EngineRequirements.md#phase-four-module-combat) · [реальные данные](../../04-Engineering/ModuleCombat.md).

## Актуализация EP-0008 — 2026-10-12

Базовая таблица ниже остаётся комплектом остальных пяти сценариев. PlayerShipOnly добавляет вторую `module.countermeasure.launcher.basic` в(3,1): всего9 модулей из12 клеток. Жилой модуль в(4,1) заменён на `living.quarters.mk1.three-cabins`, третий отдельный оператор назначен второй ПРО. У пирата остаётся одна ПРО. На этапе EP-0008 ещё действовал Hull HP450; он заменён моделью EP-0009 выше.

[Текущий контракт](../../01-Requirements/EngineRequirements.md#phase-three-combat) · [Проверки и ограничения](../../04-Engineering/PhaseThreeCombatAcceptance.md).


Статус: стартовая комплектация первого релиза зафиксирована в основном requirements-документе и `DefaultScenario`.

Основной источник: раздел 57 `Documentation/01-Requirements/EngineRequirements.md`.

## Назначение

`Tetrarch Class` - стартовый корабль игрока в первом релизе. Он задает минимальный игровой скелет: навигация, маневрирование, груз, сканирование, торпедный бой, противоракетная защита, энергия, топливо и жилое пространство для людей.

## Hull grid

Корабль использует hull grid `9x9`. Доступные клетки корпуса:

| y/x | X0 | X1 | X2 | X3 | X4 | X5 | X6 | X7 | X8 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Y0 |  |  |  |  | P |  |  |  |  |
| Y1 |  |  |  | P | P | P |  |  |  |
| Y2 |  |  |  | P | P | P |  |  |  |
| Y3 |  |  |  |  | P |  |  |  |  |
| Y4 |  |  |  |  | P |  |  |  |  |
| Y5 |  |  |  | P | P | P |  |  |  |
| Y6 |  |  |  |  |  |  |  |  |  |
| Y7 |  |  |  |  |  |  |  |  |  |
| Y8 |  |  |  |  |  |  |  |  |  |

## Стартовые модули

| Координата | Код | Модуль | `typeId` | Роль |
| --- | --- | --- | --- | --- |
| `(4,0)` | N | Navigation Computer | `module.bridge.navigation.computer.basic` | Active |
| `(4,1)` | L | Living quarters | `living.quarters.mk1` | Passive |
| `(3,2)` | T | Torpedo Launcher | `module.torpedo.launcher.basic` | Active |
| `(5,2)` | D | Countermeasure Launcher | `module.countermeasure.launcher.basic` | Active |
| `(4,2)` | C | Cargo hold | `module.container.basic` | Passive |
| `(4,3)` | S | Scanner | `module.scanner.mk1` | Active |
| `(4,4)` | R | Reactor | `module.generator.basic` | Passive |
| `(4,5)` | E | Engine | `module.engine.basic` | Active |

## Схема модулей

| y/x | X0 | X1 | X2 | X3 | X4 | X5 | X6 | X7 | X8 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Y0 |  |  |  |  | N |  |  |  |  |
| Y1 |  |  |  | P | L | P |  |  |  |
| Y2 |  |  |  | T | C | D |  |  |  |
| Y3 |  |  |  |  | S |  |  |  |  |
| Y4 |  |  |  |  | R |  |  |  |  |
| Y5 |  |  |  | P | E | P |  |  |  |
| Y6 |  |  |  |  |  |  |  |  |  |
| Y7 |  |  |  |  |  |  |  |  |  |
| Y8 |  |  |  |  |  |  |  |  |  |

## Требования первого релиза

- UI и релизная документация должны считать Tetrarch стартовым кораблем игрока.
- Старые стартовые модули `Battery`, `Drilling Unit`, `Combat Laser` и старый `Habitation Module` не входят в стартовую комплектацию.
- `Living quarters MK I` имеет `cabines = 2` и задает вместимость людей на стартовом корабле.
- На старте два разных члена экипажа: назначенные операторы торпедного и противоракетного аппаратов с навыками50.
- `Cargo hold` хранит cargo-товары, включая `Energy Cells`; `Fuel` в cargo не хранится.
- `Reactor` в UI первого релиза соответствует техническому модулю `module.generator.basic`.
- `Engine` хранит `Fuel` только во внутренних баках.
- `Scanner` дает команды сканирования и обнаружения сигнатур.
- Добыча льда реализуется через отдельный Drilling Unit и mining commands, но этот модуль не входит в текущую стартовую комплектацию Tetrarch.
- В первом релизе игрок не получает Drilling Unit бесплатно; его можно купить и установить на станции.

## За рамками первого релиза

- Детальная цена и условия установки `module.drilling.unit.basic` на станции.
Боевой контракт EP-0007 и проверка пяти стандартных сценариев: [CountermeasureCombat](../../04-Engineering/CountermeasureCombat.md). Корпус содержит12 клеток, установлено8 модулей.
