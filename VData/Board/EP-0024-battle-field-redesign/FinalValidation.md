# Финальная проверка EP-0024

Продолжение US4-TK2 проверено отдельно:32/32 focused,46/46 native; GPU12ms остаётся OPEN. Актуальная оптимизация, метрики и evidence — [CloudOptimization](CloudOptimization.md). Ниже — историческая проверка полного эпика до этой оптимизации.

Run: `D:/DeepSpaceSaga/artifacts/validation/ep-0024/20261112-implementation-01`. Runtime tested commit `0823924b`; последующие изменения документационные.

| Проверка | Результат | Evidence в run |
|---|---|---|
| Полный TacticalBattle |548/548 PASS,296.42s с лимитом300s; до выделения неизменного NoiseAlpha helper |final-tactical/client.trx; final-tactical-test.log |
| Остальной Client |1856/1864 PASS,8 FAIL |final-nontactical/client.trx; final-nontactical-test.log |
| Финальный helper, панели, PNG |30/30 PASS, включая9 новых numeric alpha probes |us9-cloud-final/client.trx |
| Инвентарь всего Client |2421/2421 IDs покрыты;2413 PASS,8 FAIL; missing/unexpected0 |FinalVerification.json; final-client-list.log |
| Регрессия31 сцен/панелей |31/31 PASS; Classic geometry/text parity сохранён |us9-tk2/client.trx |
| Space |3/3 PASS: Classic,Tabletop,custom-slim; без repeat/modifiers/перехода следующего хода |us9-keyboard-final/client.trx |
| Release Client/Performance |0 warnings/errors |final-native-build.log |
| Scoped format/diff |PASS для затронутых файлов |us9-tk2-format.log; us9-tk2-client-format.log |
| Native final-native-tabletop-720 |46/46 PASS; draw p95=6.889ms, max=21.2699ms |final-native-tabletop-720.json; final-native-tabletop-720.*.png |
| Native final-native-tabletop-1080 |46/46 PASS; draw p95=12.5892ms, max=31.5037ms |final-native-tabletop-1080.json; final-native-tabletop-1080.*.png |
| Native final-native-classic-720 |46/46 PASS; draw p95=14.7499ms, max=64.6155ms |final-native-classic-720.json; final-native-classic-720.*.png |

Native: Intel Arc140V16GB, OpenGL, меньший monitor1 videoMode1920×1080 origin3440,0. Перед запуском проверен display inventory; DSS_VALIDATION_SMALL_MONITOR=1 и изолированный профиль. Крупнейший3440×1440 не использовался. Прогон не пересекался с build/test. На720/S1 и1080/S1.5 проверены Settings/скин, контролы карточки, E/D, запрет D при evasion, replay/outcome/new battle, возврат мира/камеры и F5/F9 после выхода. Native720 movement просмотрен; scripted pan/zoom затем намеренно сохраняет ручной кадр. Это не физические нажатия, сканирование дисплея или80FPS.

GPU completion wait при пересборке: cold20.609ms, warm[17.3615, 15.196, 13.3976, 8.9116, 7.8765, 8.4986, 7.9751, 9.5258]ms, reuse1.7412ms. BuildCount9, reused=true. Бюджет12ms НЕ выполнен; предыдущий cold17.7917ms также превышал бюджет. `final-native-tabletop-1080-clouds.json`, ранний `native-tabletop-1080-clouds.json` сохранены. CPU raster не считается GPU evidence.

## Соседние ошибки

Все8 failure IDs и сообщения совпали с `D:/DeepSpaceSaga/artifacts/validation/ep-0023/20261111-execution-01/final-client-02/client.trx`. Владельцы: ClusterInteractionEvidenceTests, ApproachCameraIntegrationTests и FullMapRenderEvidenceTests; EP24 не меняет правила/модели карты мира. Точный список и сообщения — FinalVerification.json. Ошибки не скрыты и не объявлены исправленными.

## Review и инварианты

Самопроверка contract→production→test: ID пакета не включает флаги сам по себе; PNG готовятся каталогом; cache владеет изображением, окно контекстом; камера UI не записывает world camera; команды проходят прежние выбранный-пилот/pending/identity guards; вызовы draw не отправляют Engine-команд; расписание/отлетающий урон и admitted projection сохранены; Panel input обрезан до VisiblePanel. Save20/Contracts/Motion/Engine/data/RNG не менялись. Classic15 PNG побайтно совпадают с базойd7895f73.

Исправлены подтверждённые замечания: Space-подсказка стала рабочей; цвет кольца возвращён нормативному Yellow; alpha-unit гипотеза опровергнута9 независимыми пробами без изменения коэффициентов. Самопроверка не является независимым review. Owner, физический ввод и Linux/macOS остаются NOT RUN.

## Документы, Graphify и уборка

Content audit PASS:13 stories/26 tickets, зависимости разрешены без циклов, 514 локальных links существуют,24 PNG идентичны входу,15 Classic эталонов неизменны,8 Tabletop1920×1080, non-Markdown в Documentation0. Offline Graphify PASS: 24128 nodes/63201 edges, 844 code/837 Markdown. Полный JSON и агрегированный HTML, fresh semantics12 sources/11 inferred edges, hashes/locations проверены; missing/dangling/self-loop0. Один parser warning ModulePanelLocalizationTests.cs:25,1278 unresolved AST edges pruned,3162 reference-only symbols без локальной декларации; отсутствие пути не доказывает отсутствие связи. Исторический src/graphify-out не перезаписан. После финального статуса stories экспорт повторён; итоговый verification.json является актуальным receipt. Runtime-профили, save slots, AST cache и временные Python helpers не входят в поставку. Retained evidence: перечисленные здесь/в ticket logs TRX/логи/PNG/JSON и производный Graphify graph с audit sidecars, semantics и verification.
Уборка выполнена: удалены временные native-профили, save slots, Screenshots-дубликаты, промежуточные classic/contrast/xenon renders, AST cache и10 Python/PowerShell helpers. Удаления ограничены собственным run; другие задачи и VData сохранены. Остаточных profile/slots/cache/helper нет. Worktree не создавались; новых временных каталогов в workspace/repository roots и DSS/artifacts нет. Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep-0024/20261112-implementation-01/Cleanup.json`.

Повторная финальная проверка1681 source hashes и18 artifact hashes PASS после обновления статусов:11 stories DONE,24 tickets DONE,2 OPEN, graph_fresh=true. Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep-0024/20261112-implementation-01/FinalAudit.json`. Publication.json заполняется фактическим локальным/удалённым SHA после разрешённого commit/push.
