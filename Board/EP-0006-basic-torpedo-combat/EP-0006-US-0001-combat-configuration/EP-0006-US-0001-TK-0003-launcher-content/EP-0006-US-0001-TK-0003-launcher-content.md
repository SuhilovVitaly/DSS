---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0001-combat-configuration
ticket: EP-0006-US-0001-TK-0003-launcher-content
title: "Категория, реализация и команда торпедного аппарата"
stage: draft
layer: content-data
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: ["EP-0006-US-0001-TK-0002-combat-content-loader"]
files_touched: 5
serves: ["AC-0002","AC-0003"]
priority: P1
created: 2026-10-02T08:22:58Z
revision: 2
validation_status: not-run
---

# EP-0006-US-0001-TK-0003-launcher-content

Категория, реализация и команда торпедного аппарата.

## Why

Результат тикета: **Категория, реализация и команда торпедного аппарата**. Он нужен для истории [Настраиваемые характеристики Тетрарха и торпеды](../EP-0006-US-0001-combat-configuration.md) и покрывает указанные `serves` в своей части; полные критерии story завершаются совокупностью её тикетов.

- **AC-0002** — Модуль задаёт урон 150, скорость 3 км/с и скорость поворота 90°/с; загрузчик проверяет значения и выдаёт ошибку с путём.
- **AC-0003** — Старый контент без новых необязательных секций загружается; новые данные попадают в output и доступны после перезапуска.

## Decisions

- **D01:** «нам нужно добавить новый модуль торпедного аппарата в схему Тетрарха»; «для ланчера нужна своя панель»; новое помещение — «новое», место — «выбери сам». Один launcher в новом помещении стандартного Тетрарха; отдельная command panel с кнопкой Пуск.
- **D02:** «пока она летит вторую он выпустить не может»; «после взрыва выпущенной торпеды следующая уже готова»; условной занятости — «пока условной достаточно»; боезапас — «бесконечные». Одна активная торпеда на аппарат, без реального crew assignment, расхода боезапаса и отдельного ожидания КД. Готов → Наведение → Готов; pending submit не позволяет double click.
- **D04:** «самонаведение с предсказанием»; «фиксированным. пусть будет 3 километра в секунду пока»; «давай по курсу корабля»; 90°/с — «хорошо»; точка пуска — «с центра». Торпеда стартует из центра по текущему курсу носителя, мгновенно имеет фиксированную собственную скорость 3 км/с и поворачивает максимум 90°/с; скорость носителя не прибавляется.
- **D07:** Ограничить дальность — «нет»; недостижимая цель — «пока этим принебрегаем потом добавим кнопку самоуничтожения». Нет range, TTL, fuel limit, проверки достижимости как условия пуска или кнопки self-destruct. Бесконечное преследование и занятый аппарат допустимы для MVP.
- **D17:** «для тетрарха в настройки тетрарха, для торпеды в настройки торпедного аппарата который является модулем тетрарха»; скорость и turn rate в модуле — «да». HP450 принадлежит class config Тетрарха; damage150/speed3/turnRate90 — конфигурации module.torpedo.launcher.basic, а не экземпляру палитры/константам Client.

Полный журнал, исходный scope и определения D/A — [Documentation.md](../../Documentation.md). Не переносить ранние предложения о вероятностях/КД/дальности из черновика обратно в MVP.

## Assumptions

Применяются A01–A12 эпика в части этого тикета. Все API ниже **предлагаемые**, кроме прямо названных существующих методов. Naming/overload можно уточнить без смены семантики и allowlist. Если для решения нужен дополнительный production/test файл, сначала уточнить scope; не скрывать его за количеством `files_touched`.

Новых продуктовых решений сверх согласованного объёма этот тикет не вводит. Порядок тестов и внутренние helper names выбираются в пределах заявленного слоя.

## Code context

Полный write allowlist от корня DSS; 5 файлов, включая тесты. Другие файлы — read-only. Новые файлы из зависимостей должны реально существовать к началу исполнения.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/module-types.json` | Существует: `src/DeepSpaceSaga.Client/Data/module-types.json:1` | Только данные, явно названные в Public API и шагах этого тикета |
| `src/DeepSpaceSaga.Client/Data/Modules/TorpedoLauncher/modules-torpedolauncher.json` | Новый файл этого тикета; на baseline отсутствует | Только данные, явно названные в Public API и шагах этого тикета |
| `src/DeepSpaceSaga.Client/Data/Commands/TorpedoLauncher/commands.json` | Новый файл этого тикета; на baseline отсутствует | Только данные, явно названные в Public API и шагах этого тикета |
| `tests/DeepSpaceSaga.Client.Tests/TorpedoLauncherContentTests.cs` | Новый файл этого тикета; на baseline отсутствует | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |
| `tests/DeepSpaceSaga.Engine.Tests/ScenarioEngineTests.cs` | Существует; тест `Real_default_scenario_active_module_types_have_valid_command_type_ids` | Только ожидаемое число активных типов 5 → 6 и проверки `module.torpedo.launcher.basic`: единственная команда `torpedo.fire`, owning category `module.torpedo.launcher`, `target=object`; остальные assertions сохранить |

Расширение с четырёх до пяти файлов явно согласовано пользователем в ходе реализации: «Да, исправить этот тест». Причина — добавление launcher увеличивает число активных типов, проверяемых существующим Engine-тестом. Основной test project остаётся Client.Tests; Engine.Tests добавлен только для указанной регрессии. Production layer остаётся `content-data`.

Контекст для чтения: [grounding эпика](../../Documentation.md), [история](../EP-0006-US-0001-combat-configuration.md), `Documentation/01-Requirements/EngineRequirements.md` (§2, §52, §55–57, §60), `Documentation/00-Process/CLAUDE.md`. Code context определяет границу записи, ссылки для чтения её не расширяют.

## Dependencies

- [EP-0006-US-0001-TK-0002-combat-content-loader](../../EP-0006-US-0001-combat-configuration/EP-0006-US-0001-TK-0002-combat-content-loader/EP-0006-US-0001-TK-0002-combat-content-loader.md)

Нужны реализованные и проверенные результаты зависимостей. При их отсутствии остановить этот implementation ticket с конкретным gap, не имитировать готовность mock-объектами и не дописывать зависимость за его пределами.

## Public API after the change

category module.torpedo.launcher; implementation module.torpedo.launcher.basic; command torpedo.fire, target=object. Числа: torpedoDamage=150, torpedoSpeedKmS=3, torpedoTurnRateDegPerSec=90; slotSize=1. Подробная схема следует TK-0002 и существующим ModuleImplementationDto/CommandDefinitionDto.

## Implementation steps

1. Добавить категорию и реализацию, одну команду без auto-fire, расхода боеприпасов и отдельного cooldown.
2. Технические module defaults (масса, structurePointsMax, powerConsumptionW) задать явно по простому валидному образцу существующего модуля; они не участвуют в формуле урона и не вводят новый баланс.
3. Проверить discover/load через реальные Settings; generic success/cycle metadata не должно запускать старый RNG/cycle execution для torpedo.fire.

## Out of scope

Файлы вне allowlist; production слои вне `content-data`; AI пирата, защита/ПР, вероятность/баланс RNG, дальность/боезапас/TTL, реальные операторы, self-destruct, area damage, salvage, звуки, victory UI и общий refactor карты. Не менять существующие Board-статусы/requirements или commit/push в рамках реализации без соответствующего поручения.

## Invariants

- Gameplay HP, projectile lifecycle и collision принадлежат Engine; UI работает через immutable Contracts/session и shared Motion (CLAUDE.md, Architecture).
- Flight time — MotionTimeMs; real-time эффекты — monotonic UI clock (SimulationClock.Update; AuthoritativeSnapshot.MotionTimeMs). Пауза не продвигает мир, но session infrastructure работает (§52).
- `TargetObjectId` фиксируется в явной команде; current selection не заменяет цель полёта (PlayerCommand, строка7).
- Owner/self исключены из контактов; первый contact применяется один раз; chance100% не расходует RNG. Размеры5px/50px UI не являются collider world radius.
- Корабль Тетрарх определяется class identity, не картинкой; неизвестные свойства остаются masked. Legacy defaults не дают скрытой новой боевой способности.
- Сохраняются независимые worktree изменения. При ошибке baseline тестов записать конкретный результат, не объявлять весь suite зелёным и не править чужой fixture вне scope.

## Tests

Имена ниже — планируемые тесты, а не уже существующие результаты:

- [ ] `Real_launcher_content_exposes_one_targeted_command`
- [ ] `Launcher_parameters_are_150_3_90_and_no_ammo_cost`
- [ ] `Real_default_scenario_active_module_types_have_valid_command_type_ids` — Engine.Tests; ожидаются шесть активных типов, включая новый аппарат с одной адресованной объекту командой.

Из корня `D:/DeepSpaceSaga/DSS` (PowerShell):

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~Real_launcher_content_exposes_one_targeted_command|FullyQualifiedName~Launcher_parameters_are_150_3_90_and_no_ammo_cost'
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter 'FullyQualifiedName~Real_default_scenario_active_module_types_have_valid_command_type_ids'
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Client.Tests/TorpedoLauncherContentTests.cs tests/DeepSpaceSaga.Engine.Tests/ScenarioEngineTests.cs
git diff --check
```

Проверить реальные loader и output копии JSON, не только синтаксис. Tests изменяют временные копии данных, не исходный пользовательский контент.

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

