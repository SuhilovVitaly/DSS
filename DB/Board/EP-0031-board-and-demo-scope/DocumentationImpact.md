# EP-0031 — инвентаризация влияния на документацию

Сверка 2026-11-16 по итоговому diff ветки `feature/ep-0031-board-and-demo-scope`.
Поиск: `grep -rln "первого релиза|FirstReleaseRequirements|EP-0021|Hire" DB/Documentation`,
ссылки на EP-0029/EP-0021 в Board.

## Изменённые документы

| Документ | Что изменилось | Проверка |
|---|---|---|
| [DB/Board/README.md](../README.md) | Новый реестр EP-0001…EP-0031 и EP-maintenance; дорожная карта EP-0032…EP-0044 | 32 строки = 32 каталога `EP-*`; ссылки 0 битых |
| [EP-0029 Documentation](../EP-0029-unified-carrier-and-torpedoes/Documentation.md), [Tickets](../EP-0029-unified-carrier-and-torpedoes/Tickets.md) | Статус «не начат» заменён фактическим | Ссылки 0 битых |
| [EP-0021 Documentation](../EP-0021-drone-combat-economy/Documentation.md) | Пометка «Актуальность 2026-11-16» | Только добавленные строки |
| [FirstReleaseRequirements](../../Documentation/01-Requirements/FirstReleaseRequirements.md) | Раздел «Первая демо-версия», D-01…D-04 | Таблица текущих границ без изменений |
| [Documentation README](../../Documentation/README.md) | Ссылки на границу демо и реестр Board | Ссылки 0 битых |

## Проверены, изменений не требуют от EP-0031

| Документ | Почему без изменений |
|---|---|
| `02-FirstRelease/Screens/Hire.md`, `Contracts.md`, `Mechanics/CrewAndHabitation.md` | Описывают текущую оболочку; демо-требования не реализованы и вынесены в FirstReleaseRequirements |
| `02-FirstRelease/Mechanics/ElectricityAndEnergyCells.md` | «Первого релиза» относится к принятому требованию питания, не к границе демо |
| `01-Requirements/EngineRequirements.md` | Нормативные инварианты не меняются; демо-решения ещё не реализованы |

## Найдено вне scope (не исправлено)

Эти расхождения появились после EP-0029 и не относятся к EP-0031. По правилу
`DB/Documentation/00-Process/AGENTS.md` они должны получить тикет в EP-maintenance.

1. **Save21.** Код пишет версию 21 (`src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:40`),
   а `README.md`, `FirstReleaseRequirements.md`, `EngineRequirements.md`, `SaveFormat.md`,
   `TacticalBattle.md`, `DebugTuning.md` и `00-Process/CLAUDE.md` называют writer Save20.
2. **Экран корабля.** `ShipScreen.cs` содержит перезарядку крыла
   (`src/DeepSpaceSaga.Client/UI/Screens/Ship/ShipScreen.cs:21-23`), а `Screens/Ship.md`,
   `ScreenCatalog.md` и строка Hire/Contracts/Ship в FirstReleaseRequirements называют его оболочкой.

## Gates

| Gate | Результат |
|---|---|
| Ссылки изменённых файлов | 0 битых (`D:/DeepSpaceSaga/artifacts/validation/ep-0031/20261116-links/checklinks.py`, скрипт удалён после проверки) |
| `git diff --check` | Чисто на каждом коммите |
| Build / tests | NOT RUN — изменён только Markdown |
| Graphify | NOT RUN — код не менялся, rebuild требует свежих semantic fragments ([GraphifyRebuild](../../Documentation/06-Tooling/GraphifyRebuild.md)) |
| Native / owner acceptance | Не применимо к документам; реестр не является приёмкой |
| Push | Не выполнен — ждёт решения владельца (A-05) |
