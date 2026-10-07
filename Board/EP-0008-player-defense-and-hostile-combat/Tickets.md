# EP-0008 — Порядок выполнения

Статус: **planning-only / draft**, 2026-10-07. 13 историй,44 тикета. [Контракт](Documentation.md), [51 решение](Decisions.md), [промт исполнения](../EpicExecutionPrompt.md).

Порядок последовательный: каждый следующий тикет зависит от предыдущего. Это сознательное ограничение общей работы с Engine partials, а не обещание параллельной готовности. Каждый шаг при будущем исполнении получает свой commit и push. Подготовленный план сейчас не исполнялся.

| Шаг | История / тикет | Layer | Files | Depends on | Serves |
|---|---|---|---|---|---|

| — | [EP-0008-US-0001-module-owned-weapon-rules](EP-0008-US-0001-module-owned-weapon-rules/EP-0008-US-0001-module-owned-weapon-rules.md) — **Характеристики оружия и понятный шанс перехвата** | — | — | — | — |
| 1 | [EP-0008-US-0001-TK-0001-weapon-contract](EP-0008-US-0001-module-owned-weapon-rules/EP-0008-US-0001-TK-0001-weapon-contract/EP-0008-US-0001-TK-0001-weapon-contract.md) — Контракт характеристик торпеды | contracts | 5 | EP6/EP7 runtime | AC-0001, AC-0002, AC-0003 |
| 2 | [EP-0008-US-0001-TK-0002-defense-contract](EP-0008-US-0001-module-owned-weapon-rules/EP-0008-US-0001-TK-0002-defense-contract/EP-0008-US-0001-TK-0002-defense-contract.md) — Контракт пуска ПРО, времени жизни и preview | contracts | 5 | EP-0008-US-0001-TK-0001-weapon-contract | AC-0002, AC-0003 |
| 3 | [EP-0008-US-0001-TK-0003-weapon-content-schema](EP-0008-US-0001-module-owned-weapon-rules/EP-0008-US-0001-TK-0003-weapon-content-schema/EP-0008-US-0001-TK-0003-weapon-content-schema.md) — Схема и валидация новых параметров оружия | engine | 4 | EP-0008-US-0001-TK-0002-defense-contract | AC-0001, AC-0002 |
| 4 | [EP-0008-US-0001-TK-0004-weapon-catalog](EP-0008-US-0001-module-owned-weapon-rules/EP-0008-US-0001-TK-0004-weapon-catalog/EP-0008-US-0001-TK-0004-weapon-catalog.md) — Базовые установки 55/5 и их дальности | content-data | 4 | EP-0008-US-0001-TK-0003-weapon-content-schema | AC-0001, AC-0002 |
| 5 | [EP-0008-US-0001-TK-0005-chance-resolution](EP-0008-US-0001-module-owned-weapon-rules/EP-0008-US-0001-TK-0005-chance-resolution/EP-0008-US-0001-TK-0005-chance-resolution.md) — Новая формула и captured параметры при пуске | engine | 5 | EP-0008-US-0001-TK-0004-weapon-catalog | AC-0001, AC-0002, AC-0003 |

| — | [EP-0008-US-0002-dual-defense-loadout](EP-0008-US-0002-dual-defense-loadout/EP-0008-US-0002-dual-defense-loadout.md) — **Две установки ПРО и три оператора в боевом сценарии** | — | — | — | — |
| 6 | [EP-0008-US-0002-TK-0001-living-variant-and-scenario](EP-0008-US-0002-dual-defense-loadout/EP-0008-US-0002-TK-0001-living-variant-and-scenario/EP-0008-US-0002-TK-0001-living-variant-and-scenario.md) — Новый жилой вариант и комплектация игрока | content-data | 5 | EP-0008-US-0001-TK-0005-chance-resolution | AC-0001, AC-0002, AC-0003 |
| 7 | [EP-0008-US-0002-TK-0002-dual-bootstrap](EP-0008-US-0002-dual-defense-loadout/EP-0008-US-0002-TK-0002-dual-bootstrap/EP-0008-US-0002-TK-0002-dual-bootstrap.md) — Независимое состояние двух аппаратов после New Game | engine | 4 | EP-0008-US-0002-TK-0001-living-variant-and-scenario | AC-0002, AC-0003 |

| — | [EP-0008-US-0003-ranged-hostile-torpedo-fire](EP-0008-US-0003-ranged-hostile-torpedo-fire/EP-0008-US-0003-ranged-hostile-torpedo-fire.md) — **Враждебные корабли стреляют по игроку** | — | — | — | — |
| 8 | [EP-0008-US-0003-TK-0001-shared-launch-validation](EP-0008-US-0003-ranged-hostile-torpedo-fire/EP-0008-US-0003-TK-0001-shared-launch-validation/EP-0008-US-0003-TK-0001-shared-launch-validation.md) — Общий validated launch с разделением полномочий | engine | 4 | EP-0008-US-0002-TK-0002-dual-bootstrap | AC-0001, AC-0002 |
| 9 | [EP-0008-US-0003-TK-0002-hostile-fire-scheduler](EP-0008-US-0003-ranged-hostile-torpedo-fire/EP-0008-US-0003-TK-0002-hostile-fire-scheduler/EP-0008-US-0003-TK-0002-hostile-fire-scheduler.md) — Автопуск и повторная стрельба внутри физического интервала | engine | 5 | EP-0008-US-0003-TK-0001-shared-launch-validation | AC-0002, AC-0003 |
| 10 | [EP-0008-US-0003-TK-0003-player-range-feedback](EP-0008-US-0003-ranged-hostile-torpedo-fire/EP-0008-US-0003-TK-0003-player-range-feedback/EP-0008-US-0003-TK-0003-player-range-feedback.md) — Понятная недоступность торпедного пуска вне дальности | client | 3 | EP-0008-US-0003-TK-0002-hostile-fire-scheduler | AC-0001 |

| — | [EP-0008-US-0004-hostile-pursuit-and-orbit](EP-0008-US-0004-hostile-pursuit-and-orbit/EP-0008-US-0004-hostile-pursuit-and-orbit.md) — **Преследование и орбита вокруг игрока** | — | — | — | — |
| 11 | [EP-0008-US-0004-TK-0001-hostile-motion-contract](EP-0008-US-0004-hostile-pursuit-and-orbit/EP-0008-US-0004-TK-0001-hostile-motion-contract/EP-0008-US-0004-TK-0001-hostile-motion-contract.md) — Контракт подтверждённого движения и фаз противника | contracts | 3 | EP-0008-US-0003-TK-0003-player-range-feedback | AC-0002, AC-0003, AC-0004 |
| 12 | [EP-0008-US-0004-TK-0002-relative-orbit-math](EP-0008-US-0004-hostile-pursuit-and-orbit/EP-0008-US-0004-TK-0002-relative-orbit-math/EP-0008-US-0004-TK-0002-relative-orbit-math.md) — Ограниченное двигателем движение вокруг движущейся цели | motion | 3 | EP-0008-US-0004-TK-0001-hostile-motion-contract | AC-0002, AC-0003, AC-0004 |
| 13 | [EP-0008-US-0004-TK-0003-hostile-controller](EP-0008-US-0004-hostile-pursuit-and-orbit/EP-0008-US-0004-TK-0003-hostile-controller/EP-0008-US-0004-TK-0003-hostile-controller.md) — Авторитетное управление двигателем и фазами AI | engine | 5 | EP-0008-US-0004-TK-0002-relative-orbit-math | AC-0001, AC-0002, AC-0003, AC-0004 |
| 14 | [EP-0008-US-0004-TK-0004-combat-path-integration](EP-0008-US-0004-hostile-pursuit-and-orbit/EP-0008-US-0004-TK-0004-combat-path-integration/EP-0008-US-0004-TK-0004-combat-path-integration.md) — Попадания и наведение учитывают маневрирующего противника | engine | 4 | EP-0008-US-0004-TK-0003-hostile-controller | AC-0002, AC-0004 |

| — | [EP-0008-US-0005-manual-countermeasure-fire](EP-0008-US-0005-manual-countermeasure-fire/EP-0008-US-0005-manual-countermeasure-fire.md) — **Ручной перехват любой вражеской торпеды** | — | — | — | — |
| 15 | [EP-0008-US-0005-TK-0001-manual-guidance](EP-0008-US-0005-manual-countermeasure-fire/EP-0008-US-0005-TK-0001-manual-guidance/EP-0008-US-0005-TK-0001-manual-guidance.md) — План и преследование без гарантированной встречи | motion | 2 | EP-0008-US-0004-TK-0004-combat-path-integration | AC-0003 |
| 16 | [EP-0008-US-0005-TK-0002-manual-launch-execution](EP-0008-US-0005-manual-countermeasure-fire/EP-0008-US-0005-TK-0002-manual-launch-execution/EP-0008-US-0005-TK-0002-manual-launch-execution.md) — Команда ручного пуска с безопасным повтором | engine | 4 | EP-0008-US-0005-TK-0001-manual-guidance | AC-0001, AC-0002, AC-0003, AC-0004 |
| 17 | [EP-0008-US-0005-TK-0003-defense-fire-catalog](EP-0008-US-0005-manual-countermeasure-fire/EP-0008-US-0005-TK-0003-defense-fire-catalog/EP-0008-US-0005-TK-0003-defense-fire-catalog.md) — Ручная команда в каталоге ПРО | content-data | 2 | EP-0008-US-0005-TK-0002-manual-launch-execution | AC-0001 |
| 18 | [EP-0008-US-0005-TK-0004-defense-preview-projection](EP-0008-US-0005-manual-countermeasure-fire/EP-0008-US-0005-TK-0004-defense-preview-projection/EP-0008-US-0005-TK-0004-defense-preview-projection.md) — Авторитетный шанс и причины недоступности каждого аппарата | engine | 4 | EP-0008-US-0005-TK-0003-defense-fire-catalog | AC-0002, AC-0004 |

| — | [EP-0008-US-0006-lifetime-and-auto-arbitration](EP-0008-US-0006-lifetime-and-auto-arbitration/EP-0008-US-0006-lifetime-and-auto-arbitration.md) — **Ограниченный полёт и согласованная автоматика двух установок** | — | — | — | — |
| 19 | [EP-0008-US-0006-TK-0001-global-lifetime](EP-0008-US-0006-lifetime-and-auto-arbitration/EP-0008-US-0006-TK-0001-global-lifetime/EP-0008-US-0006-TK-0001-global-lifetime.md) — Абсолютный deadline полёта во всех фазах ПРО | engine | 3 | EP-0008-US-0005-TK-0004-defense-preview-projection | AC-0001, AC-0002, AC-0004 |
| 20 | [EP-0008-US-0006-TK-0002-auto-launcher-selection](EP-0008-US-0006-lifetime-and-auto-arbitration/EP-0008-US-0006-TK-0002-auto-launcher-selection/EP-0008-US-0006-TK-0002-auto-launcher-selection.md) — Выбор одного аппарата для автоматического перехвата | engine | 3 | EP-0008-US-0006-TK-0001-global-lifetime | AC-0003, AC-0004 |
| 21 | [EP-0008-US-0006-TK-0003-combat-journal-facts](EP-0008-US-0006-lifetime-and-auto-arbitration/EP-0008-US-0006-TK-0003-combat-journal-facts/EP-0008-US-0006-TK-0003-combat-journal-facts.md) — Журнал ручных пусков и истечения времени полёта | engine | 4 | EP-0008-US-0006-TK-0002-auto-launcher-selection | AC-0001, AC-0002, AC-0004 |

| — | [EP-0008-US-0007-independent-defense-panels](EP-0008-US-0007-independent-defense-panels/EP-0008-US-0007-independent-defense-panels.md) — **Две адресуемые панели ПРО** | — | — | — | — |
| 22 | [EP-0008-US-0007-TK-0001-panel-instances](EP-0008-US-0007-independent-defense-panels/EP-0008-US-0007-TK-0001-panel-instances/EP-0008-US-0007-TK-0001-panel-instances.md) — Командные панели по конкретным экземплярам модулей | client | 4 | EP-0008-US-0006-TK-0003-combat-journal-facts | AC-0001, AC-0004 |
| 23 | [EP-0008-US-0007-TK-0002-panel-command-routing](EP-0008-US-0007-independent-defense-panels/EP-0008-US-0007-TK-0002-panel-command-routing/EP-0008-US-0007-TK-0002-panel-command-routing.md) — Ручные команды и независимый pending каждого аппарата | client | 4 | EP-0008-US-0007-TK-0001-panel-instances | AC-0001, AC-0002, AC-0003, AC-0004 |
| 24 | [EP-0008-US-0007-TK-0003-combat-info](EP-0008-US-0007-independent-defense-panels/EP-0008-US-0007-TK-0003-combat-info/EP-0008-US-0007-TK-0003-combat-info.md) — Расчёт шанса, lifetime и поведение выбранного врага | client | 4 | EP-0008-US-0007-TK-0002-panel-command-routing | AC-0002, AC-0003 |
| 25 | [EP-0008-US-0007-TK-0004-combat-localization](EP-0008-US-0007-independent-defense-panels/EP-0008-US-0007-TK-0004-combat-localization/EP-0008-US-0007-TK-0004-combat-localization.md) — Локализованные команды, причины и состояния | content-data | 3 | EP-0008-US-0007-TK-0003-combat-info | AC-0002, AC-0003 |

| — | [EP-0008-US-0008-manual-preview-and-map](EP-0008-US-0008-manual-preview-and-map/EP-0008-US-0008-manual-preview-and-map.md) — **Прогноз ручного перехвата и дальности без перегрузки карты** | — | — | — | — |
| 26 | [EP-0008-US-0008-TK-0001-preview-geometry](EP-0008-US-0008-manual-preview-and-map/EP-0008-US-0008-TK-0001-preview-geometry/EP-0008-US-0008-TK-0001-preview-geometry.md) — Геометрия прогноза ручного пуска конкретного аппарата | client | 3 | EP-0008-US-0007-TK-0004-combat-localization | AC-0001, AC-0003 |
| 27 | [EP-0008-US-0008-TK-0002-hover-ranges-and-countdown](EP-0008-US-0008-manual-preview-and-map/EP-0008-US-0008-TK-0002-hover-ranges-and-countdown/EP-0008-US-0008-TK-0002-hover-ranges-and-countdown.md) — Круги hover и время полёта на карте | client | 5 | EP-0008-US-0008-TK-0001-preview-geometry | AC-0002, AC-0003 |
| 28 | [EP-0008-US-0008-TK-0003-journal-presentation](EP-0008-US-0008-manual-preview-and-map/EP-0008-US-0008-TK-0003-journal-presentation/EP-0008-US-0008-TK-0003-journal-presentation.md) — Читаемый журнал новых событий боя | client | 4 | EP-0008-US-0008-TK-0002-hover-ranges-and-countdown | AC-0003 |

| — | [EP-0008-US-0009-authoritative-player-defeat](EP-0008-US-0009-authoritative-player-defeat/EP-0008-US-0009-authoritative-player-defeat.md) — **Поражение останавливает весь игровой мир** | — | — | — | — |
| 29 | [EP-0008-US-0009-TK-0001-defeat-contract](EP-0008-US-0009-authoritative-player-defeat/EP-0008-US-0009-TK-0001-defeat-contract/EP-0008-US-0009-TK-0001-defeat-contract.md) — Контракт терминального состояния сессии | contracts | 4 | EP-0008-US-0008-TK-0003-journal-presentation | AC-0001, AC-0003 |
| 30 | [EP-0008-US-0009-TK-0002-defeat-transition](EP-0008-US-0009-authoritative-player-defeat/EP-0008-US-0009-TK-0002-defeat-transition/EP-0008-US-0009-TK-0002-defeat-transition.md) — Атомарная гибель игрока и запрет действий | engine | 4 | EP-0008-US-0009-TK-0001-defeat-contract | AC-0001, AC-0002, AC-0003 |
| 31 | [EP-0008-US-0009-TK-0003-defeat-clock-boundary](EP-0008-US-0009-authoritative-player-defeat/EP-0008-US-0009-TK-0003-defeat-clock-boundary/EP-0008-US-0009-TK-0003-defeat-clock-boundary.md) — Остановка времени на событии поражения | engine | 5 | EP-0008-US-0009-TK-0002-defeat-transition | AC-0001, AC-0002 |

| — | [EP-0008-US-0010-defeat-screen-and-recovery](EP-0008-US-0010-defeat-screen-and-recovery/EP-0008-US-0010-defeat-screen-and-recovery.md) — **Окно поражения и загрузка живого состояния** | — | — | — | — |
| 32 | [EP-0008-US-0010-TK-0001-defeated-session-save-guard](EP-0008-US-0010-defeat-screen-and-recovery/EP-0008-US-0010-TK-0001-defeated-session-save-guard/EP-0008-US-0010-TK-0001-defeated-session-save-guard.md) — Защита файлового сохранения и session lifecycle | engine-localclient | 2 | EP-0008-US-0009-TK-0003-defeat-clock-boundary | AC-0002, AC-0003 |
| 33 | [EP-0008-US-0010-TK-0002-defeat-modal](EP-0008-US-0010-defeat-screen-and-recovery/EP-0008-US-0010-TK-0002-defeat-modal/EP-0008-US-0010-TK-0002-defeat-modal.md) — Экран поражения с двумя действиями | client | 4 | EP-0008-US-0010-TK-0001-defeated-session-save-guard | AC-0001 |
| 34 | [EP-0008-US-0010-TK-0003-defeat-navigation](EP-0008-US-0010-defeat-screen-and-recovery/EP-0008-US-0010-TK-0003-defeat-navigation/EP-0008-US-0010-TK-0003-defeat-navigation.md) — Открытие поражения, загрузка и выход через реальную оболочку | client | 5 | EP-0008-US-0010-TK-0002-defeat-modal | AC-0001, AC-0002, AC-0003 |

| — | [EP-0008-US-0011-phase-three-save-continuation](EP-0008-US-0011-phase-three-save-continuation/EP-0008-US-0011-phase-three-save-continuation.md) — **Новые сохранения продолжают весь бой без миграции старых** | — | — | — | — |
| 35 | [EP-0008-US-0011-TK-0001-new-save-version-gate](EP-0008-US-0011-phase-three-save-continuation/EP-0008-US-0011-TK-0001-new-save-version-gate/EP-0008-US-0011-TK-0001-new-save-version-gate.md) — Новая версия и явный отказ старым сохранениям | engine | 5 | EP-0008-US-0010-TK-0003-defeat-navigation | AC-0001, AC-0003 |
| 36 | [EP-0008-US-0011-TK-0002-save-combat-validation](EP-0008-US-0011-phase-three-save-continuation/EP-0008-US-0011-TK-0002-save-combat-validation/EP-0008-US-0011-TK-0002-save-combat-validation.md) — Полная схема и preflight боя новой фазы | engine | 5 | EP-0008-US-0011-TK-0001-new-save-version-gate | AC-0002, AC-0003 |
| 37 | [EP-0008-US-0011-TK-0003-save-runtime-continuation](EP-0008-US-0011-phase-three-save-continuation/EP-0008-US-0011-TK-0003-save-runtime-continuation/EP-0008-US-0011-TK-0003-save-runtime-continuation.md) — Атомарное восстановление активного боя и AI | engine | 5 | EP-0008-US-0011-TK-0002-save-combat-validation | AC-0002, AC-0003, AC-0004 |
| 38 | [EP-0008-US-0011-TK-0004-load-ui-and-transport](EP-0008-US-0011-phase-three-save-continuation/EP-0008-US-0011-TK-0004-load-ui-and-transport/EP-0008-US-0011-TK-0004-load-ui-and-transport.md) — Файловая загрузка и понятное сообщение несовместимости | client | 5 | EP-0008-US-0011-TK-0003-save-runtime-continuation | AC-0001, AC-0002, AC-0003, AC-0004 |

| — | [EP-0008-US-0012-phase-three-acceptance](EP-0008-US-0012-phase-three-acceptance/EP-0008-US-0012-phase-three-acceptance.md) — **Проверяемый двусторонний бой** | — | — | — | — |
| 39 | [EP-0008-US-0012-TK-0001-compatibility-fixtures](EP-0008-US-0012-phase-three-acceptance/EP-0008-US-0012-TK-0001-compatibility-fixtures/EP-0008-US-0012-TK-0001-compatibility-fixtures.md) — Адаптация существующих проверок к новой фазе | validation | inventory (A12) | EP-0008-US-0011-TK-0004-load-ui-and-transport | AC-0002 |
| 40 | [EP-0008-US-0012-TK-0002-end-to-end-combat](EP-0008-US-0012-phase-three-acceptance/EP-0008-US-0012-TK-0002-end-to-end-combat/EP-0008-US-0012-TK-0002-end-to-end-combat.md) — Сквозное доказательство боя с настоящим AI | engine | 3 | EP-0008-US-0012-TK-0001-compatibility-fixtures | AC-0001, AC-0002, AC-0004 |
| 41 | [EP-0008-US-0012-TK-0003-native-ui-acceptance](EP-0008-US-0012-phase-three-acceptance/EP-0008-US-0012-TK-0003-native-ui-acceptance/EP-0008-US-0012-TK-0003-native-ui-acceptance.md) — Нативная приёмка панелей, боя и поражения | client | 2 | EP-0008-US-0012-TK-0002-end-to-end-combat | AC-0001, AC-0003, AC-0004 |
| 42 | [EP-0008-US-0012-TK-0004-epic-review](EP-0008-US-0012-phase-three-acceptance/EP-0008-US-0012-TK-0004-epic-review/EP-0008-US-0012-TK-0004-epic-review.md) — Итоговое review контрактов и границ | validation | 2 | EP-0008-US-0012-TK-0003-native-ui-acceptance | AC-0001, AC-0002, AC-0003, AC-0004 |

| — | [EP-0008-US-0013-project-documentation-and-graph](EP-0008-US-0013-project-documentation-and-graph/EP-0008-US-0013-project-documentation-and-graph.md) — **Актуальная документация всего проекта** | — | — | — | — |
| 43 | [EP-0008-US-0013-TK-0001-update-project-documents](EP-0008-US-0013-project-documentation-and-graph/EP-0008-US-0013-TK-0001-update-project-documents/EP-0008-US-0013-TK-0001-update-project-documents.md) — Обновить релевантные документы во всех папках | documentation | inventory (A12) | EP-0008-US-0012-TK-0004-epic-review | AC-0001, AC-0002, AC-0003 |
| 44 | [EP-0008-US-0013-TK-0002-refresh-graph-and-links](EP-0008-US-0013-project-documentation-and-graph/EP-0008-US-0013-TK-0002-refresh-graph-and-links/EP-0008-US-0013-TK-0002-refresh-graph-and-links.md) — Перестроить граф и проверить навигацию финального проекта | tooling | inventory (A12) | EP-0008-US-0013-TK-0001-update-project-documents | AC-0003 |

## Карта решений

| История | Решения интервью |
|---|---|
| [EP-0008-US-0001-module-owned-weapon-rules](EP-0008-US-0001-module-owned-weapon-rules/EP-0008-US-0001-module-owned-weapon-rules.md) | Q14,Q17,Q20–Q25,Q41,Q46,Q48 |
| [EP-0008-US-0002-dual-defense-loadout](EP-0008-US-0002-dual-defense-loadout/EP-0008-US-0002-dual-defense-loadout.md) | Q32–Q34 |
| [EP-0008-US-0003-ranged-hostile-torpedo-fire](EP-0008-US-0003-ranged-hostile-torpedo-fire/EP-0008-US-0003-ranged-hostile-torpedo-fire.md) | Q02–Q04,Q17,Q41,Q44 |
| [EP-0008-US-0004-hostile-pursuit-and-orbit](EP-0008-US-0004-hostile-pursuit-and-orbit/EP-0008-US-0004-hostile-pursuit-and-orbit.md) | Q18–Q20,Q39–Q45,Q51 |
| [EP-0008-US-0005-manual-countermeasure-fire](EP-0008-US-0005-manual-countermeasure-fire/EP-0008-US-0005-manual-countermeasure-fire.md) | Q05–Q11,Q16,Q25–Q26,Q29,Q31,Q35,Q48 |
| [EP-0008-US-0006-lifetime-and-auto-arbitration](EP-0008-US-0006-lifetime-and-auto-arbitration/EP-0008-US-0006-lifetime-and-auto-arbitration.md) | Q08–Q09,Q12–Q15,Q35,Q37,Q47–Q48 |
| [EP-0008-US-0007-independent-defense-panels](EP-0008-US-0007-independent-defense-panels/EP-0008-US-0007-independent-defense-panels.md) | Q06,Q29,Q36,Q38,Q46 |
| [EP-0008-US-0008-manual-preview-and-map](EP-0008-US-0008-manual-preview-and-map/EP-0008-US-0008-manual-preview-and-map.md) | Q30,Q38,Q45–Q46,Q50 |
| [EP-0008-US-0009-authoritative-player-defeat](EP-0008-US-0009-authoritative-player-defeat/EP-0008-US-0009-authoritative-player-defeat.md) | Q27–Q28 |
| [EP-0008-US-0010-defeat-screen-and-recovery](EP-0008-US-0010-defeat-screen-and-recovery/EP-0008-US-0010-defeat-screen-and-recovery.md) | Q27–Q28,Q49 |
| [EP-0008-US-0011-phase-three-save-continuation](EP-0008-US-0011-phase-three-save-continuation/EP-0008-US-0011-phase-three-save-continuation.md) | Q14,Q25,Q28,Q42,Q49 |
| [EP-0008-US-0012-phase-three-acceptance](EP-0008-US-0012-phase-three-acceptance/EP-0008-US-0012-phase-three-acceptance.md) | Q01–Q51 |
| [EP-0008-US-0013-project-documentation-and-graph](EP-0008-US-0013-project-documentation-and-graph/EP-0008-US-0013-project-documentation-and-graph.md) | Обязательная завершающая история по запросу пользователя и EpicExecutionPrompt |

## Обязательные gates

- Подготовка: прочитать документацию/эпик, проверить актуальный код и Git; не использовать старый save-version как baseline.
- Каждый тикет: real implementation → проверки → review/fix → собственный commit/push.
- История: подтверждение всех AC и настоящих зависимостей.
- Приёмка: real battle/current save/0–100%/таймеры/AI/orbit/terminal и отдельная native матрица.
- Последняя история: impact inventory всех релевантных документов **во всём проекте**, их обновление, затем Graphify/links.

Тестовые fixtures адаптируются вместе с вызывающим их изменение тикетом; шаг совместимости является финальным аудитом, не разрешением оставлять suite сломанным до конца. Последний documentation scope и test-only inventory имеют явно описанные исключения A12. Статус ACCEPTED/APPROVED не выставляется автоматически.
