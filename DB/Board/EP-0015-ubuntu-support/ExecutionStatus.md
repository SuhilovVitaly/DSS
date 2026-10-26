# EP-0015 — исполнение

Дата: 2026-10-26. **Реализация опубликована; окончательная проверка в работе. Приёмка открыта.**

Canonical Board:5 историй,16 тикетов, включая два дополнительных prerequisites и завершающую документационную историю. Stage draft сохранён; пользователь явно разрешил реализацию и публикацию. Ни self-review, ни автоматизированные проверки не являются human approval.

## Текущий результат

| История | Реализация | Оставшаяся проверка |
|---|---|---|
| US-0001 | SDK8 compilation, portability, bounded exact-case check script/CI | Полные финальные Engine/EconomyBalance и Ubuntu22.04 corpus |
| US-0002 | Linux native Skia, bundled fonts, shared typeface lifetime, Trade viewport/input | Финальная composite native матрица Ubuntu после уточнения probe |
| US-0003 | OpenAL worker/cache/epochs, platform folder launcher, portable PS asset tools | Linux bundled-Soft loader repeat; новые platform scripts сравнить по PNG hashes |
| US-0004 | contentRoot, XDG/Windows/portable paths, no-overwrite migration, self-contained publisher и Linux guide | Clean Ubuntu24.04 container без .NET: startup/save/load PASS; final source repeat в очереди |
| US-0005 | Current contracts и датированная история документации опубликованы | Граф и аудит PASS; согласование ожидающих CI результатов и публикация evidence |

Последний опубликованный функциональный checkpoint:4995e0be93d1b14da152416b78d53be65deb1530. Полные соответствия тикет→SHA→push: [PublicationStatus](PublicationStatus.md). Текущие статусы и AC: [Tickets](Tickets.md).

## Подтверждённые проверки

- Windows C#12 builds: ноль warnings/errors на целевых и интеграционных сборках.
- Final Windows Client:2349/2349 в20261026-final-client-windows/summary.json после всех source fixes; audio boundary/bundled-loader6/6 плюс unavailable1/1. Final C#12 solution build0 warnings/errors в20261026-final-build/build.log.
- Contracts:273/273, Motion:251/251 baseline; adaptive Contracts orchestration также273 реальных cases с точным leaf coverage.
- Full Engine baseline CI40288631755: Windows и Ubuntu24.04 по2484/2484 в181 leaf partitions. Это source e9a8d236 до contentRoot; новый полный corpus и targeted loader regression учитываются отдельно.
- External Settings content:93/93, включая реальный bootstrap/portraits/save/load.
- Ubuntu24.04 SDK8.0.427: Contracts/Motion/Performance и полный Client проходят в40290044041. Общий job Client был красным из-за отдельного OpenAL-null check; full corpus и native layouts учитываются отдельно.
- Native: Windows8/8 cases,48 PNG в20261026-layout-composite/windows. Ubuntu40290044041 native8/8,48 PNG в20261026-linux-native-deps; standalone Trade margin не является финальным production-composite visual acceptance. Composite harness уточнён и опубликован отдельно.
- Actual Windows production Main: GPU main menu и реальный save/load round-trip проходят под изолированным LOCALAPPDATA в20261026-publish/windows-startup.json.
- Cross-publish linux-x64: native/runtime/content inventory PASS; deliberate missing Skia rejects package. Запуск Linux executable этим не доказан.
- Отдельный настоящий запуск self-contained executable в clean Ubuntu24.04 без dotnet проходит: read-only installation, unprivileged user,13 GPU frames и production Save/Load, XDG config/data. Evidence20261026-linux-distribution-ci/extracted/distribution/summary.json, CI40291292917.
- Финальный review исправил directory-only portrait discovery (существующий independent test1/1) и ложную Windows shell ошибку при reuse Explorer (repro failure →12/12). Оба fix опубликованы отдельными коммитами.
- Documentation: локальные ссылки0 broken, corpus Markdown-only; [impact/history](DocumentationImpact.md).

Все каталоги выше находятся под D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/. [Подробный audit](FinalValidation.md) содержит точные пути и ограничения.

## Открытые gates

Финальные CI runs стоят в очереди за ранними длительными corpus jobs. Ubuntu22.04 уже проходит Client/Contracts/Motion/EconomyBalance/Performance в40291596107; Engine ещё выполняется. Финальная composite layout comparison и bundled OpenAL-null repeat пока PENDING, не PASS. Полный локальный Engine corpus выполняется с300s cap на процесс; найденный loader regression исправлен и отдельно проверен, активный старый binary не подменялся.

NOT RUN/OPEN: до-фонтный native Windows baseline (Codex-managed worktree не создан: tool ответил Not a git repository для chat cwd), физическая мышь/клавиатура, слышимость на Windows/PulseAudio/PipeWire, реальный файловый менеджер Ubuntu, native Wayland, arm64, user/product acceptance и независимое review. Xvfb/llvmpipe — реальный software OpenGL, не hardware/physical evidence.

## История решений и baseline

2026-10-26:661f3fe8 перенёс внешний пакет в Board и добавил US-0005. Первый SDK8 baseline на обеих ОС выявил2 CS0121 и3 nullable diagnostics; последующая компиляция обнаружила CS1998. Эти failures сохранены и исправлены отдельными коммитами, не переписаны как PASS. Локальный SDK10 baseline изначально проходил и не подменял SDK8.

2026-10-26: native/raster review нашёл glyph gap для♦ и Trade crop1280×720; исправлены производные DSS Mono glyph mapping и общий fit/input transform без изменения игровых правил. Self-review выявил точную Nyquist границу22050Hz; она исправлена. Linux native проверка выявила отсутствие graphics dependencies и неверный default OpenAL library-name selection; зависимости и explicit soft:true исправлены.

## Ограничение вспомогательной операции

Автоматическая проверка разрешений отклонила попытку отменить устаревший CI через сохранённые Git credentials: извлечение credentials и внешняя отмена не были отдельно разрешены. Операция не выполнена, credentials не извлекались, обход не применялся. Отмена не является частью deliverable; проверки продолжаются с фактическим ожиданием очереди.
