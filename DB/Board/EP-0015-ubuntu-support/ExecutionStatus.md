# EP-0015 — исполнение

Дата: 2026-10-26. Статус: PREPARATION, не завершён.

Пакет перенесён в Board; 5 историй, 14 тикетов. Production-код не менялся, тесты ещё не запускались. Подготовка и реализация учитываются отдельно.

## Publication

Upstream: origin/codex/ep-0013-debug-tuning-panel. Начальный remote SHA: 5c77fccc9bf46ceb19e41b3bf1b710044d237099. SHA подготовки и тикетов проверяются после коммита и push, не записываются заранее.

## Gates

Ubuntu baseline NOT RUN: WSL не установлен, docker/podman не найдены. Проверяется альтернатива GitHub Actions. Windows automated NOT RUN. Native X11/XWayland, сравнение раскладки, physical input, audio и human acceptance NOT RUN. Полное завершение не заявляется.

## 2026-10-26 baseline checkpoint

US-0001-TK-0001: baseline измерен на Ubuntu24.04 и Windows2022, SDK8.0.427: restore0/build1 с одинаковыми5 compiler diagnostics (2Client и3fixture). Тесты SDK8 NOT RUN, это не зелёная сборка. Добавлен US-0001-TK-0004 (Client compile), TK-0002 расширен на nullable fixture. Всего5 историй/15 тикетов. Следующий: US-0001-TK-0004, затемTK-0002/TK-0003. Остальные истории ещё не реализованы.

Published и remote verified: подготовка661f3fe86cb5351abf087b5aefd465250313e8cd; harness d026651ab4c60e429f357dd112f9ce8420304e90, e2cd785c56374b51f739003650e76b63652e3fbb, 8eaa374008e81ac242d6010beedeae8cacf5dabc, 13cdb4a8bc3718d1a69583b89e56be18c6866442; corrective card3558240c77f86a2fa0c9016d4abecc0ced816741. Completion report SHA проверяется после commit/push.

Локально SDK10: build0 warnings/errors, Contracts273/273, Motion251/251, Client2322/2322; Engine12 method partitions прошли,2 достигли300с и не считаютсяPASS. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-baseline/. Проверенный exact-case Contracts harness: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-runsettings-contracts/. SDK8 CI: https://github.com/SuhilovVitaly/DSS/actions/runs/40282243373 . Полные job logs сохранены локально.

Auto-review отклонил только вспомогательную отмену устаревшегоCI через credentials: действие не выполнено, credentials не извлекались, обход не применялся. Это не блокирует реализацию. Статусы native/audio/human остаютсяNOT RUN.
