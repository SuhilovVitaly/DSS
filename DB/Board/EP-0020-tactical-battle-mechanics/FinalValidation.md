# EP-0020 — итоговая техническая проверка

Код `33c760488ecb62dc8fef4811ba1fd82c792f8981`, Windows, .NET8 target (SDK10.0.400), Intel Arc140V. Публикация и полный ticket-to-SHA — [ExecutionStatus](ExecutionStatus.md).

| Проверка | Фактический результат |
|---|---|
| Ordinary Engine + geometry corpus |2948uniquePASS=2318+630,36TRX; строгий audit IDs/hashes |
| Engine deterministic series |5000direct/explicit парPASS с полнымиSlots; нормализация metadata byte-exact US9 |
| Client |2252/2252PASS,15strictsnapshots different0/maxTile0;12M3 панелей идентичны до baseline update |
| Contracts |199/199PASS |
| Motion |1282PASS/1FAIL ForbiddenCalls UI scanner; источник уже нарушался базой 601ab, UI по-прежнему не удовлетворяет широкому scanner |
| Build/format |DebugClient/ReleasePerformancePASS; scoped whitespace verify/diffPASS. WarningMSB9008 missingModApi — прежний |
| Native |2/2PASS:EN1280×720/S1,RU1920×1080/S1.5, реальный OpenGL; E/D, несовместимость, replay/new battle/exit/F5/F9 и сохранение мира/камеры |
| Draw timing |p9572.9228/90.9693ms;80FPS не подтверждены; это draw/flush, не GPU execution/scanout |
| Save20 |Схема не менялась, эфемерный бой не сохраняется; native exit world byte-exact и F5/F9PASS, обычные save continuation cases включены в Engine |

Self-review: слой Motion содержит чистую математику поля/шансов; Engine фиксирует приказы и атомарно валидирует до RNG/затрат; Contracts обратно совместимы defaults; LocalClient не получает новый world-time путь; UI читает captured/admitted projection, не симулирует исход, скрывает будущие enemyD и не повторяет sideeffects при replay. Соответствие AC фиксируется в каждой из 40 функциональных карточек. Self-review не независимый review.

Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/final-engine/coverage-audit.json`, `us12-tk3/view-audit.json`, `us12-tk3/native-audit.json`, `us12-tk3/client-final/tests.trx`, `final-contracts/tests.trx`, `final-motion/tests.trx` относительно того же абсолютного evidence root. Все test processes capped300000ms; corpus разбит по точным IDs без пропусков.

Human/product/art, physical input, hardware audio, независимый review, otherOS NOT RUN. Известный Motion guard и FPS остаются открытыми техническими ограничениями; ни один не переименован вPASS.

Graph/doc checks:56AC/44tickets/13stories,78canonical dependencies. Offline graph22594nodes/60077edges,814code/755Markdown,1569source hashes и4fresh semantic hashes проверены; derived missing/invalid/dangling/selfloops0. Один partial AST parser warning и разрешённые pruning/unlocated ограничения записаны в `D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us13-tk4/graph-quality.json`. Graph является навигацией, не приёмкой.
