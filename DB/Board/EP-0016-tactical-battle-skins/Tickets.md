# EP-0016 — Карта тикетов

[Эпик и требования](Documentation.md) · 4 истории, 14 тикетов, включая завершающую историю исполнения. Статус карт — автоматический workflow epic-writer. Порядок строк соблюдает зависимости.

Входная база для исполнения и сравнения Classic указана в [Grounding](Documentation.md#grounding). Сверка EP-0015 уточнила охват в действующих тикетах. TK-0003 истории US-0001 готовит contrast с новыми ролями, TK-0001–0003 истории US-0002 фиксируют эталон и переносят карту и флажки, TK-0001 и TK-0004 истории US-0003 проверяют совместимость с сохранением локали и реальным маршрутом боя.

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0016-US-0001-TK-0001-skin-manifest](EP-0016-US-0001-skin-pack-folder/EP-0016-US-0001-TK-0001-skin-manifest/EP-0016-US-0001-TK-0001-skin-manifest.md) | Контракт пакета оформления | client | — | US1-AC1 |
| [EP-0016-US-0001-TK-0002-discover-and-load-packs](EP-0016-US-0001-skin-pack-folder/EP-0016-US-0001-TK-0002-discover-and-load-packs/EP-0016-US-0001-TK-0002-discover-and-load-packs.md) | Обнаружение и проверка пакетов | client | `EP-0016-US-0001-TK-0001-skin-manifest` | US1-AC1, US1-AC2, US1-AC3 |
| [EP-0016-US-0001-TK-0003-ship-reference-pack](EP-0016-US-0001-skin-pack-folder/EP-0016-US-0001-TK-0003-ship-reference-pack/EP-0016-US-0001-TK-0003-ship-reference-pack.md) | Поставка демонстрационного скина | content-data | `EP-0016-US-0001-TK-0002-discover-and-load-packs` | US1-AC2 |
| [EP-0016-US-0002-TK-0001-theme-shared-primitives](EP-0016-US-0002-migrate-battle-rendering/EP-0016-US-0002-TK-0001-theme-shared-primitives/EP-0016-US-0002-TK-0001-theme-shared-primitives.md) | Оформление общих элементов боя | client | `EP-0016-US-0001-TK-0003-ship-reference-pack` | US2-AC1, US2-AC2, US2-AC4 |
| [EP-0016-US-0002-TK-0002-theme-field-and-salvo](EP-0016-US-0002-migrate-battle-rendering/EP-0016-US-0002-TK-0002-theme-field-and-salvo/EP-0016-US-0002-TK-0002-theme-field-and-salvo.md) | Оформление поля и анимации залпа | client | `EP-0016-US-0002-TK-0001-theme-shared-primitives` | US2-AC1, US2-AC2, US2-AC3, US2-AC4, US2-AC5 |
| [EP-0016-US-0002-TK-0003-theme-chrome-and-outcome](EP-0016-US-0002-migrate-battle-rendering/EP-0016-US-0002-TK-0003-theme-chrome-and-outcome/EP-0016-US-0002-TK-0003-theme-chrome-and-outcome.md) | Оформление панелей, окон и результата боя | client | `EP-0016-US-0002-TK-0002-theme-field-and-salvo` | US2-AC1, US2-AC2, US2-AC4 |
| [EP-0016-US-0002-TK-0004-theme-order-controls](EP-0016-US-0002-migrate-battle-rendering/EP-0016-US-0002-TK-0004-theme-order-controls/EP-0016-US-0002-TK-0004-theme-order-controls.md) | Оформление приказов движения и стрельбы | client | `EP-0016-US-0002-TK-0003-theme-chrome-and-outcome` | US2-AC1, US2-AC2, US2-AC4 |
| [EP-0016-US-0002-TK-0005-theme-information-tabs](EP-0016-US-0002-migrate-battle-rendering/EP-0016-US-0002-TK-0005-theme-information-tabs/EP-0016-US-0002-TK-0005-theme-information-tabs.md) | Оформление сторон, журнала и прогноза | client | `EP-0016-US-0002-TK-0004-theme-order-controls` | US2-AC1, US2-AC2, US2-AC4 |
| [EP-0016-US-0003-TK-0001-persist-skin-selection](EP-0016-US-0003-select-skin-in-settings/EP-0016-US-0003-TK-0001-persist-skin-selection/EP-0016-US-0003-TK-0001-persist-skin-selection.md) | Хранение выбранного скина | client | `EP-0016-US-0001-TK-0002-discover-and-load-packs`, `EP-0016-US-0002-TK-0005-theme-information-tabs` | US3-AC2, US3-AC3 |
| [EP-0016-US-0003-TK-0002-localize-skin-selector](EP-0016-US-0003-select-skin-in-settings/EP-0016-US-0003-TK-0002-localize-skin-selector/EP-0016-US-0003-TK-0002-localize-skin-selector.md) | Локализация переключателя | content-data | `EP-0016-US-0003-TK-0001-persist-skin-selection` | US3-AC1, US3-AC3 |
| [EP-0016-US-0003-TK-0003-add-skin-selector](EP-0016-US-0003-select-skin-in-settings/EP-0016-US-0003-TK-0003-add-skin-selector/EP-0016-US-0003-TK-0003-add-skin-selector.md) | Переключатель скина в настройках | client | `EP-0016-US-0003-TK-0002-localize-skin-selector` | US3-AC1, US3-AC3 |
| [EP-0016-US-0003-TK-0004-wire-settings-to-battle](EP-0016-US-0003-select-skin-in-settings/EP-0016-US-0003-TK-0004-wire-settings-to-battle/EP-0016-US-0003-TK-0004-wire-settings-to-battle.md) | Связь настроек с реальным экраном боя | client | `EP-0016-US-0003-TK-0003-add-skin-selector`, `EP-0016-US-0002-TK-0005-theme-information-tabs` | US3-AC1, US3-AC2, US3-AC3, US3-AC4 |
| [EP-0016-US-0003-TK-0005-correct-shared-style-fallbacks](EP-0016-US-0003-select-skin-in-settings/EP-0016-US-0003-TK-0005-correct-shared-style-fallbacks/EP-0016-US-0003-TK-0005-correct-shared-style-fallbacks.md) | Исправление общего наследования и шрифтов | client | `EP-0016-US-0003-TK-0004-wire-settings-to-battle` | US2-AC1/2/4, US3-AC4 |
| [EP-0016-US-0004-project-documentation-TK-0001-update-contracts-and-graph](EP-0016-US-0004-project-documentation/EP-0016-US-0004-project-documentation-TK-0001-update-contracts-and-graph/EP-0016-US-0004-project-documentation-TK-0001-update-contracts-and-graph.md) | Итоговая документация и граф | documentation | `EP-0016-US-0003-TK-0005-correct-shared-style-fallbacks` | US4-AC1, US4-AC2, US4-AC3 |

## Контроль передачи

- **US-0001:** формат пакета, каталог и два доступных вида, включая встроенный Classic и поставляемый contrast.
- **US-0002:** карта визуальных ролей, паритет Classic, рендер альтернативного пакета во всех фазах и виджетах.
- **US-0003:** сохранение и локализация выбора, переключатель, обычный запуск боя и замена дизайна файлами пакета. Raster и нативная проверка ведутся раздельно.

Независимые блокирующие решения отсутствуют. Ограничения торпедной проекции и художественной приёмки записаны в эпике.
