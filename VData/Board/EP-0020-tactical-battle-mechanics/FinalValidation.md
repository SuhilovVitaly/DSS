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

Закрытие: все 44 тикета опубликованы, последний TK4 `fb631c893f21c5e86052b6d959f2619bcf78e262` проверен в origin. Cleanup receipt `D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/cleanup.json`: 331 temporary directories и 1053 disposable files удалены, task-created worktrees 0, все task processes завершены. Сохранены validation/native/graph evidence и требуемые подписи/отчёты.


## Повторная проверка 2026-11-09

Motion прошёл 1283 из 1283, включая исходный `ForbiddenCallsTests`. В клиентских расчётах использованы `PyMath` и явное вещественное деление. Полный Client suite прошёл 2253 из 2253.

Декодированные портреты стали неизменяемыми для повторного использования текстур. Сетка рисуется одним путём. При движении камеры поле аномалий использует проекцию готовой текстуры и уточняется в фоне после стабилизации. Большое смещение камеры и эталоны проверены отдельно.

Окно 1920×1080 запускалось на меньшем мониторе 2560×1440. В каждом сценарии измерены 600 интервалов между завершениями `SwapBuffers` после 120 прогревочных кадров. Все четыре сценария уложились в бюджет 12,5 мс по p99.

| Сценарий | p99 без VSync, мс | p99 с VSync, мс |
|---|---:|---:|
| Покой | 6.69 | 11.61 |
| Панорамирование | 5.86 | 11.41 |
| Масштабирование | 6.35 | 11.46 |
| Залп | 6.37 | 11.39 |

При VSync средняя частота завершения обмена буферов составила около 100 кадров/с. Частота выбранной панели и второго меньшего монитора составляет 59 Гц, поэтому физический вывод 80 кадров/с остаётся непроверенным. Правило выбора монитора закреплено в инструкциях проекта.

[Исходный оконный замер](D:/DeepSpaceSaga/artifacts/validation/tactical-fps/20261109-01/baseline-small/report.json) · [замер без VSync](D:/DeepSpaceSaga/artifacts/validation/tactical-fps/20261109-01/measured-uncapped/report.json) · [замер с VSync](D:/DeepSpaceSaga/artifacts/validation/tactical-fps/20261109-01/measured-vsync/report.json) · [Motion](D:/DeepSpaceSaga/artifacts/validation/tactical-fps/20261109-01/motion-final-pass/tests.trx) · [Client](D:/DeepSpaceSaga/artifacts/validation/tactical-fps/20261109-01/client-full/tests.trx).
