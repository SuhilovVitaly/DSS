---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0005-reliable-first-battle
ticket: EP-0028-US-0005-TK-0001-forced-tabletop
title: Tabletop для боя обучения
stage: approved
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0028-US-0001-TK-0004-screen-integration]
implementation_files: 1
files_touched:
  - src/DeepSpaceSaga.Client/UI/SkiaWindow.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TutorialFlowTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SkinBattleIntegrationTests.cs
serves: [US-0005/AC-01]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Tabletop для боя обучения

[История](../EP-0028-US-0005-reliable-first-battle.md) · [Эпик](../../Documentation.md)

## Цель

Окно боя обучения всегда создаётся со скином `tabletop`, не трогая сохранённый выбор скина для обычных боёв.

## Decisions

D-03 — бой обучения всегда Tabletop ([эпик](../../Documentation.md#решения-пользователя-обязательные)). A-07 — без пакета Tabletop используется предпочтение с записью в журнал.

## Assumptions

- Пакет ищется тем же каталогом, что и для обычных боёв (`Skins` рядом с `Settings.json`, `SkiaWindow.cs:111-113`); ID `tabletop` (`src/DeepSpaceSaga.Client/Skins/tabletop/skin.json`).
- Существующая фикстура `TutorialFixture` создаёт профиль во временной папке без `Skins` (`TutorialFlowTests.cs:284-290`) — для неё остаётся путь A-07; новый тест копирует `Skins/tabletop` из выхода тестов в папку профиля.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | `CreateTacticalBattleView(flow)` создаёт экран с `_battleSkin` (`:1208-1209`); каталог `_battleSkinCatalog.TryGet(id, out skin)` (`TacticalBattleSkinCatalog.cs:61-66`); тот же метод используется для F8 после подготовки (`:841`) | Для `flow.IsTutorial`: `_battleSkinCatalog.TryGet("tabletop", out var t) ? t : _battleSkin` + `LogTutorialWarning` при отсутствии; константа `TutorialSkinId = "tabletop"` |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TutorialFlowTests.cs` | Фикстура (`:264-360`), профиль `classic` (`:288-289`) | Параметр фикстуры «установить пакет Tabletop»; тесты ниже |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SkinBattleIntegrationTests.cs` | Проверки «настройки → бой F8» (`:48-175`) | Только регрессионный запуск; правка — лишь если тест опирается на создание окна обучения |

## Public API after the change

`internal const string TutorialSkinId = "tabletop";` в `SkiaWindow`. Иного API нет.

## Implementation steps

1. Выбор скина в `CreateTacticalBattleView` по `flow.IsTutorial`.
2. Предупреждение `TUTORIAL: skin 'tabletop' unavailable, using '<id>'` при отсутствии пакета.
3. Тесты.

## Acceptance criteria

1. При профиле `classic` и установленном пакете бой обучения — `SkinId == "tabletop"`.
2. После боя обучения `TacticalBattleSkinPreference.Read(profile) == "classic"`, а бой F8 (через подготовку) — `SkinId == "classic"`.
3. Без пакета — бой обучения в предпочтительном скине, без исключений.

## Verification signal

Первым красным — `TutorialFlowTests.TutorialBattleAlwaysUsesTabletopAndKeepsSkinPreference`; затем `TutorialWithoutTabletopPackFallsBackToPreferredSkin`.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0005-tk0001-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~TutorialFlowTests|FullyQualifiedName~SkinBattleIntegrationTests|FullyQualifiedName~SkinCatalogTests|FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial" --results-directory "$RUN" --logger "trx;LogFileName=tabletop.trx"
```

Process-tree cap 300000 ms.

## Out of scope

Изменение каталога/предпочтения скинов; Engine.

## Invariants

- `gameSettings.tacticalBattleSkinId` не пишется этим тикетом.
- Бой F8 не меняется.

## Known fallout

`TutorialFlowTests` (фикстура), `SkinBattleIntegrationTests` (регрессия) — в allowlist.

## Definition of Done

- Шаги выполнены; named tests зелёные; сборка Client без новых предупреждений.
- Отдельный коммит и `git push`.

## Self-containment check

Точка выбора скина, API каталога и поведение фикстуры указаны с `path:line`.

## Журнал

2026-11-14T15:04:43Z — тикет создан.
