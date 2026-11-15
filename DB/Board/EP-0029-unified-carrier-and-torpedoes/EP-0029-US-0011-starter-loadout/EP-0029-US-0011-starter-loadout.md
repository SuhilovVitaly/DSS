---
epic: EP-0029-unified-carrier-and-torpedoes
story: EP-0029-US-0011-starter-loadout
title: Готовый учебный Tetrarch и стартовый состав
stage: draft
dependencies: [US-0001, US-0002, US-0003, US-0004, US-0007]
created: 2026-11-14T19:42:24Z
source_request: "Создай подробный эпик со всеми сторисами и тикетами."
current_review: complete
revision: 1
---

# Готовый учебный Tetrarch и стартовый состав

## Входное техническое задание

Часть [EP-0029](../Documentation.md). Утверждены единый корабль, 2 двухместных ангара, 2 жилых модуля по4 места (6 людей+2 резерва), четыре женщины-пилота, реальные потери обучения, магазины6/12 и42 торпеды в трюме. Захват пленных исключён.

## User story

Новая учебная кампания начинает реальный бой на оснащённом модульном корабле. После него игрок открывает ровно тот же корабль, а не другой стартовый объект. История принимается по наблюдаемому состоянию и критериям ниже; существование DTO или документа не означает готовности пользовательского пути.

## Grounding

Текущая реализация и source pointers находятся в [Grounding эпика](../Documentation.md#grounding--текущая-реализация). Изменяемые файлы перечислены в Code context тикетов. Новые классы/API явно проектируемые.

## Acceptance criteria

- **AC-01**: Tutorial и обычный Default используют одинаковый starter carrier manifest: SPC-0001/Tetrarch, 2 M bays, 2x4 cabins, 6 crew, reserved2, 4 drones, ammo12/12/6/6 и cargo42.
- **AC-02**: Профиль стартового крыла сохраняет нынешние 2 heavy/2 light, меч/копьё, plate.medium, два стандартных двигателя на дрон, 6 щитов суммарно и 4 decoy modules с 2 зарядами каждый.
- **AC-03**: Корпус расширен для размещения ангаров; все occupiedCells внутри hull без overlaps. Начальный ship Hull400/400; новые модули исправны.
- **AC-04**: Default_500/прочие сценарии и старые saves не получают комплект автоматически. Новые данные копируются в новый мир только на New Game, не на Load.

## Non-goals

Захват/обмен пленных, найм, верфь, рыночное пополнение, ремонт, новый тип оружия, realtime бой, Ironman и сохранение посреди боя исключены. История не расширяет scope соседних тикетов.

## Dependencies

- [US-0001 — Модульный корпус и ангарные места](../EP-0029-US-0001-carrier-modules/EP-0029-US-0001-carrier-modules.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.
- [US-0002 — Шесть членов экипажа и два зарезервированных места](../EP-0029-US-0002-crew-and-reserved-cabins/EP-0029-US-0002-crew-and-reserved-cabins.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.
- [US-0003 — Постоянное крыло корабля](../EP-0029-US-0003-persistent-wing/EP-0029-US-0003-persistent-wing.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.
- [US-0004 — Конечный боезапас в пошаговом бою](../EP-0029-US-0004-finite-torpedoes/EP-0029-US-0004-finite-torpedoes.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.
- [US-0007 — Сохранение корабля, потерь и боезапаса](../EP-0029-US-0007-save-continuity/EP-0029-US-0007-save-continuity.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.

## Invariants

Engine владеет ресурсами и итогами; Client показывает immutable snapshots. Один ShipId/DroneId/CrewId сохраняется между поверхностями. Расходы идемпотентны, пакетные отказы атомарны. Реальное обучение не имеет бесплатного reset. Два reserved места не создают пленных. На Load имущество не выдаётся.

## Approved ticket map

Workflow approval автоматический по поручению создать весь пакет; human approval подробной новой карты не заявляется.

| ID / Title | Layer | Depends on | Served criteria |
|---|---|---|---|
| [TK-0001 — Один manifest корабля для двух стартов](EP-0029-US-0011-TK-0001-starter-manifest/EP-0029-US-0011-TK-0001-starter-manifest.md) | engine | [US-0001-TK-0004](../EP-0029-US-0001-carrier-modules/EP-0029-US-0001-TK-0004-module-validation/EP-0029-US-0001-TK-0004-module-validation.md), [US-0002-TK-0003](../EP-0029-US-0002-crew-and-reserved-cabins/EP-0029-US-0002-TK-0003-pilot-catalog/EP-0029-US-0002-TK-0003-pilot-catalog.md), [US-0003-TK-0003](../EP-0029-US-0003-persistent-wing/EP-0029-US-0003-TK-0003-wing-mass/EP-0029-US-0003-TK-0003-wing-mass.md), [US-0004-TK-0005](../EP-0029-US-0004-finite-torpedoes/EP-0029-US-0004-TK-0005-ammo-exhaustion/EP-0029-US-0004-TK-0005-ammo-exhaustion.md), [US-0007-TK-0003](../EP-0029-US-0007-save-continuity/EP-0029-US-0007-TK-0003-catalog-compatibility/EP-0029-US-0007-TK-0003-catalog-compatibility.md) | AC-01, AC-04 |
| [TK-0002 — Раскладка модулей, экипаж и 78 торпед](EP-0029-US-0011-TK-0002-starter-content/EP-0029-US-0011-TK-0002-starter-content.md) | content-data | [US-0011-TK-0001](EP-0029-US-0011-TK-0001-starter-manifest/EP-0029-US-0011-TK-0001-starter-manifest.md) | AC-01, AC-02, AC-03, AC-04 |
| [TK-0003 — Подсказки о настоящем бое и перезарядке](EP-0029-US-0011-TK-0003-tutorial-instructions/EP-0029-US-0011-TK-0003-tutorial-instructions.md) | content-data | [US-0011-TK-0002](EP-0029-US-0011-TK-0002-starter-content/EP-0029-US-0011-TK-0002-starter-content.md) | AC-01 |

## Completion evidence

- AC-01: **automated PASS** — TK-0001 `DefaultAndTutorialResolveSameCarrier` (one manifest expands to both New Game stubs: 24 cells / 9 modules / 6 crew / 4 drones / 78 torpedoes), TK-0002 `StarterHasExactlySeventyEightTorpedoes`, `EightCabinsSixCrewTwoReserved`, `SameCrewPortraitsAsApproved`, TK-0003 `TutorialMentionsRealLosses`, `DisabledHintsStillRealBattle`. Native player path: **OPEN** (no native run).
- AC-02: **automated PASS** — TK-0002 `StarterFootprintsDoNotOverlap` and manifest content: 2 heavy/2 light, sword/spear, plate.medium, 2× drive.standard each, 6 shields, 4 decoy modules with 2 charges.
- AC-03: **automated PASS** — TK-0002 independent 24-cell oracle, 15 occupied, 9 free, no overlaps/outside cells; hullCurrent 400, modules Ready with type-max SP. Engine load of the expanded Default world passed through `ModuleHealthBootstrapTests.Configured_structure_is_projected` and `ScenarioEngineTests.Real_default_scenario_loads_from_the_client_build_output_directory`.
- AC-04: **automated PASS** — TK-0001 `LoadNeverReappliesManifest` (save round-trip keeps fired ammo/consumed cargo; stale manifest reference is not applied), `ConflictingInlineAndManifestReject`, `TraversalRejected`, `LoadFromJsonRejectsNewGameManifest`; Default_500 and other scenarios keep their own content.
- Evidence runs: `D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-038-us0011tk0001/`, `run-039-us0011tk0002/`, `run-040-us0011tk0003/` (focused trx, builds, format, boundary runs).
- Story boundary after TK-0003: focused US-0011 classes + `TutorialScenarioTests` + `TutorialContentTests` + `TutorialLocaleParityTests` are green; only the expected starter-shape reds remain, owned by US-0013: `ScenarioEngineTests` (4), `ModuleHealthBootstrapTests.Default_modules_have_structure`, `TetrarchLoadoutTests` (4), pre-existing `ScenarioModuleCleanupTests.CurrentContentHasOnlyWorldShipIdentityAndNoLegacyDefinitions` (2). No other regression observed.
- implementation/automated: **PASS**; native/product: **OPEN** — native RU/EN input/visual verification and product acceptance are not claimed.

## Decisions and review log

2026-11-14T19:42:24Z — время записи, не отправки. Пользователь: «Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.»
Концепция согласована. Assumptions A-01..A-06 берутся из эпика. Story draft отличает новую детализацию от human approval; ticket approved означает workflow-only. Поручения реализовать/публиковать нет.

## Gaps and backlog

Блокирующих вопросов для planning нет. Native проверка, баланс и совместимость должны быть доказаны исполнением. Если исходники изменились, перечитать owning files и обновить конкретный тикет до правок, сохранив лимит5 файлов; не расширять allowlist молча.

## Evidence corrections (2026-11-15, remediation)

- The Completion evidence sentence above records `TetrarchLoadoutTests` as 4 expected starter-shape reds. The verified count is **5 failing test cases across 3 test methods** (the original sentence is kept as written; this appended note is authoritative). The US-0011-TK-0002 remediation subsection records the same correction with the boundary-run arithmetic (7 client failures = 5 `TetrarchLoadoutTests` + 2 pre-existing `ScenarioModuleCleanupTests`).
- Pilot skills: the starter manifest now carries the Board table (Evelyn 55/52, Audrey 53/55, Claire 59/54, Rachel 50/51; initiative 55/53/59/50; Dunkan/Irina 0/0), the world crew cap is 0..100 (`ScenarioLoader`), and owned drones read identity/skills from the persistent `CrewId` (US-0005 TK-0002 remediation). See the TK-0001/TK-0002 remediation subsections and `Ep0029Us0011Tk0002Tests.PilotSkillsMatchBoardTable`.
