# Исполнение EP-0016

Клиент принимает пакеты оформления из `Skins/` рядом с приложением. Игрок выбирает Classic, Contrast или установленный пакет в Settings. Выбор сохраняется между запусками и применяется к следующему активному бою. Внешний вид меняется из манифеста и ресурсов. [Руководство автора](../../Documentation/04-Engineering/TacticalBattleSkins.md)

Ветка `codex/ep-0016-tactical-battle-skins` создана от `81e6d87183504b1e6f2e4977c112d57cd40c7923`, который включает указанную в Grounding базу и последующие исправления EP-0015. Подготовительная карточка документации опубликована в `3287add560fd2ece6afd0914040b17010aef31e6`. Remote — [origin](https://github.com/SuhilovVitaly/DSS/tree/codex/ep-0016-tactical-battle-skins).

## Покрытие критериев

| История | Результат | Свидетельство |
|---|---|---|
| US-0001 | Схема, загрузка, диагностика, Classic и Contrast в поставке | SkinManifestTests, SkinCatalogTests, ShippedSkinTests, `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/final/publish-verification.json` |
| US-0002 | Роли поля, эффектов, элементов управления и всех вкладок | 23/23 совпавших PNG Classic относительно исходной отрисовки, альтернативные растры, [карта потребителей](EP-0016-US-0002-migrate-battle-rendering/EP-0016-US-0002-TK-0005-theme-information-tabs/EP-0016-US-0002-TK-0005-theme-information-tabs.md#исполнение) |
| US-0003 | Настройки и реальный бой, RU/EN, новый бой, перезапуск, замена и удаление пакета | SkinBattleIntegrationTests, SettingsScreenTests, пять проверенных нативных маршрутов |
| US-0004 | Канонический формат, руководство автора, Board и Graphify | Инвентаризация ниже и проверка графа |

### Автоматизированные проверки

- Итоговый тактический набор и Settings: 252/252. Классический растр совпал в 23/23 сценах. `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/us3-tk5/focused.trx`, `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/us3-tk5/classic-parity.json`.
- Release, `Category!=LongRunning`: Client 2051/2051, Motion 1267/1267, Contracts 175/175, тактический Engine 388/388, EconomyBalance 61/61, Performance 9/9. TRX разнесены по каталогам в `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/final/`.
- Небоевой Engine 1765/1765. После достижения общего лимита 300 секунд набор разделён по точным методам и параметризованным DisplayName. Результаты всех 1765 случаев совпадают с discovery поимённо и без пропусков. Всего в итоговой Release-регрессии 5716 проверок. `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/final/engine-discovery/coverage.json`, `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/final/regression-counts.json`.
- Сборки Client, Performance и Release publish прошли. Хеши DLL и файлов Contrast из publish совпали с проверенной нативной сборкой. `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/final/publish-verification.json`.
- Scoped whitespace и diff check прошли на итоговой правке. Старые дефекты форматирования в отдельных тестовых файлах описаны в карточках. Guard Motion, включая production source scan, прошёл. Нестандартные операции в изменённом коде заменены без правки тестового guard.
- Строгие длительные серии баланса EP-0015 находятся вне проверки визуального паритета. Их отдельный статус приведён в [FinalValidation EP-0015](../EP-0015-tactical-carrier-battle/FinalValidation.md).

### Нативная проверка

Windows, Intel Arc 140V, OpenGL 1280×720, масштаб 1. Нативный драйвер использовал реальный SkiaWindow и игровую сессию с программными событиями.

- 38/38 для выбора установленного пакета RU, 37/37 для обновлённого пакета EN после перезапуска и 37/37 для fallback EN после удаления манифеста. В трёх случаях совпали итоги боя и 224 вызова RNG. Пиксели обновлённого PNG подтверждены в кадре, подготовка в двух EN случаях побайтно совпала. `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/native/comparison.json`, `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/native/pixel-comparison.json`.
- После исправлений стиля проверены выбор пакета EN 38/38 и fallback EN 37/37 с тем же исходом и 224 RNG calls. `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/native/reviewed-comparison.json`.
- Проверены новый бой, вкладки, флажки, фазы, подтверждение, возврат камеры и скорости, F5 и F9. Физический ввод, нативный Linux и приёмка заказного художественного референса остаются вне проведённого набора. Торпедные роли v1 ожидают отдельной боевой проекции для проверки реальной траектории.

## Инвентаризация документов

| Поверхность | Актуализация и проверка |
|---|---|
| [Технический контракт скинов](../../Documentation/04-Engineering/TacticalBattleSkins.md) | Схема, лимиты, наследование, порядок установки, роли и ресурсы сверены с парсером, каталогом, рендером и поставляемым пакетом. |
| [Настройки](../../Documentation/02-FirstRelease/Screens/Settings.md), [экран](../../Documentation/02-FirstRelease/Screens/TacticalBattle.md), [архитектура боя](../../Documentation/04-Engineering/TacticalBattle.md) | Выбор, сохранение, fallback, граница активного экрана и владение ресурсами. |
| [Моддинг](../../Documentation/04-Engineering/Modding.md), [ассеты UI](../../Documentation/06-Tooling/GameSessionScreenUI.md), [индекс](../../Documentation/README.md) | Действующая возможность скинов и путь к единому авторскому контракту. |
| [README пакета](../../../src/DeepSpaceSaga.Client/Skins/README.md), [Performance](../../../tools/DeepSpaceSaga.Performance/README.md) | Быстрый старт и воспроизведение нативных проверок в изолированном профиле. |
| Engine, SaveFormat, сценарии, внешние инструменты V4 и связанные исторические карточки | Проверено по итоговому diff. Изменение относится к оформлению Client и его пользовательской настройке, исторические результаты сохраняются. |
| Board EP-0016 | Четыре истории, 14 тикетов, явные зависимости, результаты и реестр SHA. |

Graphify 0.9.66 перестроил граф из 723 файлов кода и 498 Markdown. 17 247 узлов, 51 069 рёбер, 539 сообществ. Проверены хеши 10 входов семантического фрагмента EP-0016. Отсутствующие источники, выход за строки, висячие рёбра и петли в итоговом графе — 0. Семантическое покрытие выборочное. Один прежний C# файл `ModulePanelLocalizationTests.cs` частично извлечён AST-парсером, проект Client.Tests компилируется и проходит тесты. `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/graph-final/verification.json`, `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/graph-semantics/graph-semantic-ep0016.json`.

## Публикация тикетов

| Тикет | SHA | Push |
|---|---|---|
| `EP-0016-US-0001-TK-0001-skin-manifest` | `3842ca16ac2e304fcdc8d7ae7cbdd028bb00a549` | SHA подтверждён на origin |
| `EP-0016-US-0001-TK-0002-discover-and-load-packs` | `0acd25a1f9c0f4653b763bb5676f19767d92ac00` | SHA подтверждён на origin |
| `EP-0016-US-0001-TK-0003-ship-reference-pack` | `e364cdb95891f98bc1c2cc0718097037b917ff36` | SHA подтверждён на origin |
| `EP-0016-US-0002-TK-0001-theme-shared-primitives` | `78dc6f2fc3867d4383d3c531951a5b27ff3c41f1` | SHA подтверждён на origin |
| `EP-0016-US-0002-TK-0002-theme-field-and-salvo` | `85fac4839a02b33dca29dfaea92d07694f56ed14` | SHA подтверждён на origin |
| `EP-0016-US-0002-TK-0003-theme-chrome-and-outcome` | `67eaa754c4d8e672c0d8b9a7d66324b49033a567` | SHA подтверждён на origin |
| `EP-0016-US-0002-TK-0004-theme-order-controls` | `0dab5121f8886da76f9c078f594eb7a92497e6d8` | SHA подтверждён на origin |
| `EP-0016-US-0002-TK-0005-theme-information-tabs` | `ca2479da674c96d08b8642ede4d7a32086b28f86` | SHA подтверждён на origin |
| `EP-0016-US-0003-TK-0001-persist-skin-selection` | `00bac4efa8210d8b60b7c02c7a8d9148e8fbd348` | SHA подтверждён на origin |
| `EP-0016-US-0003-TK-0002-localize-skin-selector` | `cf404a0ffb23cb327a74bb779dc74a91cc877adb` | SHA подтверждён на origin |
| `EP-0016-US-0003-TK-0003-add-skin-selector` | `12a4c55593f1a019d944c93c88677e28dabb4adb` | SHA подтверждён на origin |
| `EP-0016-US-0003-TK-0004-wire-settings-to-battle` | `dd5e35cb992e78380e9fffea6901d02a33932072` | SHA подтверждён на origin |
| `EP-0016-US-0003-TK-0005-correct-shared-style-fallbacks` | `455df74c10e3a390f00c037fc9d5fb1a5823c7ea` | SHA подтверждён на origin |
| `EP-0016-US-0004-TK-0001-update-contracts-and-graph` | SHA определяется через Git по ID | Проверка выполняется после коммита |

Реестр относится к истории этой ветки. Собственный SHA документационного коммита указывается в итоговом отчёте после проверки remote.
