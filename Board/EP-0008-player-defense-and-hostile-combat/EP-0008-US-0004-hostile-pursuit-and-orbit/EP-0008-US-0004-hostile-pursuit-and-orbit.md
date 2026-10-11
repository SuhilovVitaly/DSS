---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0004-hostile-pursuit-and-orbit
title: "Преследование и орбита вокруг игрока"
stage: implemented
dependencies: ["EP-0008-US-0003-ranged-hostile-torpedo-fire"]
created: 2026-10-07
revision: 1
current_review: complete
validation_status: automated-pass
---

# Преследование и орбита вокруг игрока

## User story

Противник начинает сближение после снятия стартовой паузы. Он управляет двигателем, выходит на движущуюся вместе с игроком орбиту радиусом половины дальности оружия и возвращается к преследованию, если удержание невозможно.

## Источник и границы

Решения Q18–Q20,Q39–Q45,Q51; полный контекст в [журнале решений](../Decisions.md) и [контракте эпика](../Documentation.md). Технические допущения A01–A12 отделены от пользовательских ответов. Scope ограничен результатом этой истории; смежные механики из Non-goals эпика не добавляются.

## Acceptance criteria

- **AC-0001:** Все Enemy armed NPC участвуют независимо от дальности обнаружения; Friend/Neutral не получают AI.
- **AC-0002:** Используются реальные max speed/acceleration/turn limits без нового расхода fuel; радиус=0.5*торпедная дальность.
- **AC-0003:** Направление выбирается по минимальному первоначальному развороту и сохраняется; невозможная орбита даёт максимальное допустимое преследование.
- **AC-0004:** Shared motion и authoritative states Сближение/Выход/Удержание воспроизводимы при pause/split steps и доступны selected-info; неподвижной телепортации/идеального binding к игроку нет.

## Зависимости

- [EP-0008-US-0003-ranged-hostile-torpedo-fire](../EP-0008-US-0003-ranged-hostile-torpedo-fire/EP-0008-US-0003-ranged-hostile-torpedo-fire.md): production-результат зависимости должен быть реализован и проверен до этой истории.

## Карта тикетов и покрытия

| Шаг | Тикет | Layer | Serves |
|---|---|---|---|
| 11 | [EP-0008-US-0004-TK-0001-hostile-motion-contract](EP-0008-US-0004-TK-0001-hostile-motion-contract/EP-0008-US-0004-TK-0001-hostile-motion-contract.md) — Контракт подтверждённого движения и фаз противника | contracts | AC-0002, AC-0003, AC-0004 |
| 12 | [EP-0008-US-0004-TK-0002-relative-orbit-math](EP-0008-US-0004-TK-0002-relative-orbit-math/EP-0008-US-0004-TK-0002-relative-orbit-math.md) — Ограниченное двигателем движение вокруг движущейся цели | motion | AC-0002, AC-0003, AC-0004 |
| 13 | [EP-0008-US-0004-TK-0003-hostile-controller](EP-0008-US-0004-TK-0003-hostile-controller/EP-0008-US-0004-TK-0003-hostile-controller.md) — Авторитетное управление двигателем и фазами AI | engine | AC-0001, AC-0002, AC-0003, AC-0004 |
| 14 | [EP-0008-US-0004-TK-0004-combat-path-integration](EP-0008-US-0004-TK-0004-combat-path-integration/EP-0008-US-0004-TK-0004-combat-path-integration.md) — Попадания и наведение учитывают маневрирующего противника | engine | AC-0002, AC-0004 |

| 15 | [EP-0008-US-0004-TK-0005-hostile-selected-info](EP-0008-US-0004-TK-0005-hostile-selected-info/EP-0008-US-0004-TK-0005-hostile-selected-info.md) — Фаза и радиус выбранного врага | client | AC-0004 |

## Completion evidence

Все AC доказаны связанными тикетами, проверками реального результата и review. Конкретные команды и запланированные регрессии находятся в карточках. Native/UI требования подтверждаются реальным окном отдельно от automated tests; при недоступности это NOT RUN, не PASS.

## Gaps и исполнение

Блокирующих продуктовых вопросов для планирования нет. Proposed API/названия файлов уточняются по актуальному baseline; изменения в scope фиксируются в карточке. Общие числовые/временные инварианты находятся в эпике, включая current-format save и terminal clock. История draft и не является утверждением о пройденных тестах.

При будущем запуске по [EpicExecutionPrompt](../../EpicExecutionPrompt.md) каждый тикет выполняется, проверяется, коммитится и публикуется отдельно. Текущая задача только создаёт план.

Execution review 2026-10-11: runtime AC-0001/2/3 covered by TK-0001..0004; selected-info part of AC-0004 is covered by added TK-0005. Persistence/native remain assigned to US-0011/0012.

## Story execution review — 2026-10-11

AC-0001: all armed Enemy including undetected NPC, stable internal selection, Friend/Neutral and public ownership guards. AC-0002: configured engine limits and radius, no fuel cost, bounded analytic motion and sweeps. AC-0003: shortest-turn direction with CW tie, retention through phase/fallback, geometric convergence and 3 full orbit band checks. AC-0004: fixed physical boundaries, pause/split equality, real torpedo/PR integration and selected-info. Evidence: Contracts182/182; Motion162/162 plus final14/14; Engine1779/1779; Client1770/1770 before final UI, affected58/58 plus expanded10/10. Builds/format/diff PASS. Self-review complete; current-format persistence and final corpus/native acceptance still assigned downstream.
