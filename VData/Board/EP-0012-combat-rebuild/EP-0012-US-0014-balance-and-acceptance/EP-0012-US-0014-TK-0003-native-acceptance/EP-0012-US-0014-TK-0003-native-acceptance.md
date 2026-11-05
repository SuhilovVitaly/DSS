---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0014-balance-and-acceptance"
ticket: "EP-0012-US-0014-TK-0003-native-acceptance"
title: "native-acceptance"
stage: "implemented"
layer: "client-tooling"
depends_on: ["EP-0012-US-0014-TK-0002-tools-analyzer"]
files_touched: 9
serves: ["US14-AC3"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0014-TK-0003-native-acceptance

## Why

[Приёмка и баланс](../EP-0012-US-0014-balance-and-acceptance.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: native-acceptance; доказать US14-AC3 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (9 файлов). Baseline ee6763f43963ca990d651820bfe82e9b699ee22f. Текущий пользовательский EpicExecutionPrompt§5 разрешает необходимые native проверки; старое planning-only требование отдельного разрешения на запуск этим заменено. Реальный SkiaWindow/OpenGL, production catalogs/CombatMukha seed3001 и настоящие Engine snapshots; managed input и контролируемые часы явно отличаются от ручной OS/input/audio приёмки. Никаких поддельных combat snapshots или исходов.

| File | Current state | Allowed change |
|---|---|---|
| `DB/Board/EP-0012-combat-rebuild/evidence/native.md` | NEW | Every UI AC/subcriterion mapped to actual evidence or explicit NOT RUN reason; product/balance stays separate. |
| `tools/DeepSpaceSaga.Performance/PointCombatNativeEvidence.cs` | NEW | Real native renderer + current point-command UI checks; authoritative Engine playback and native PNG/JSON evidence. |
| `tools/DeepSpaceSaga.Performance/Program.cs` | Retired guided combat-window entry throws | Route to current point-combat native harness; preserve unrelated tool commands. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.PointProjectiles.cs` | Native1280×720/UI1.5 screenshot clips profile/ETA at right edge | Clamp only visible projectile label text to physical viewport; preserve all geometry, clocks and privacy; expose label bounds for validation. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Encounter.cs` | Actual native Won52500ms has hidden summary at1280×720/UI1.5 | Select a free region satisfying existing summary minimum dimensions; preserve nonmodal acknowledgement semantics. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/MapViewGeometry.cs` | Largest-area free rectangle can be too flat for HUD | Add optional minimum dimensions, leaving default map framing behavior unchanged. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0005Tests.cs` | Existing encounter HUD/ack tests | Reproduce production-module layout that hides Won summary; verify supported scales. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0010TK0002Tests.cs` | Existing point rendering tests | Reproduce and retain own/foreign label edge regression at supported viewports/scales. |
| `tools/DeepSpaceSaga.Performance/DeepSpaceSaga.Performance.csproj` | Existing tool, already Engine/Client friend | Link unchanged shared production input policy for deterministic actual win/defeat replay; no external simulator. |

## Public API after the change

Tool CLI: <repo> <report.json> --combat-window <uiScale> <WxH> <ru|en> <input|win|defeat> [--audio] [--clean-flight]. Native process hard cap5min plus internal3min guard. Input uses managed callbacks; terminal modes replay production command tapes through a fresh actual Engine. No public gameplay API change. Derived PNG/JSON artifacts are ticket evidence.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Составить native checklist из самодостаточных UI AC US10–US13/US16; включить масштаб, input, паузу, accessibility, latency и итог боя. Native исполнение разрешено текущим запросом; неподтверждённые физические/ручные пункты остаются NOT RUN с причиной.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: tooling; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Named checklist recorded in evidence/native.md; actual automated/native results below. Physical/manual gates explicitly NOT RUN.

- **Native_checklist_maps_every_UI_AC_to_evidence**: разрешённый реальный прогон или NOT RUN с причиной; headless не заменяет native.

Документарный/ручной gate: проверить перечисленные критерии и локальные ссылки; `git diff --check`. Product build не требуется для evidence-only изменений. Native выполняется в рамках текущего поручения; нельзя подменять human/OS/input/audio acceptance управляемыми callback checks. Graphify output проверяется при фактическом rebuild.

## Definition of Done

- Served AC US14-AC3 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-21: implementation и self-review выполнены. PASS 30 named / 2225 full owning tests (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0014-tk0003-results.json).

Native24/24 плюс clean-flight PASS; каждый UI AC/subcriterion сопоставлен с evidence либо явным NOT RUN в evidence/native.md. Исправлены два подтверждённых native дефекта подписи/итогов Won. Human/physical input/audio/deuteranopia/latency и balance acceptance OPEN; callbacks не заменяют ручную приёмку.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.

Native finding: Intel Arc140V OpenGL3.3 actually rendered1280×720/UI1.5 EN. Initial macro had unit-coordinate and held-Alt assumptions; correct those in the harness and leave physical Alt NOT RUN. Separately the native confirmed-flights PNG visibly clips own profile/ETA at the right viewport edge. Add focused edge-label regression before fixing this product defect; no map-layout redesign or gameplay changes. Original native report/PNG retained externally as before evidence.

Second native finding: seed3001 dodge replay reaches actual Won at52500ms, but native1280×720/UI1.5 EN renders no summary/Continue. Largest free rectangle is too flat for CombatSummaryPanel minimum100logical-pixel height, while a smaller-area tall free rectangle exists. Defeat replay passes actual window transition. Add production-module layout regression before fixing only HUD viewport selection.

Harness configuration correction: runtime Settings uses Russian/English, not ru/en aliases. Actual supported UI scales from both SkiaWindow and GameSessionScreen are0.8/1/1.2/1.5;1.25 falls back to1.0 and is not accepted as a distinct scale. Archive the initial mislabeled native matrix outside the repository and rerun24 cases with actual locale/scale checks. No product localization or scale-support change.
