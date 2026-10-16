# EP-0010 — статус реализации

Дата: 2026-10-16. **EXECUTING: contracts; остальной эпик OPEN.**

| Область | Статус |
|---|---|
| ТЗ и декомпозиция 12/27 | CREATED |
| Структурная проверка | См. PlanningValidation.md |
| Production implementation | NOT STARTED |
| PNG assets | NOT CREATED — 0/8 |
| Автоматические продуктовые проверки/build | NOT RUN |
| Native UI | NOT RUN / OPEN |
| Human art/gameplay acceptance | OPEN |
| 80 FPS/frame-time | NOT RUN / OPEN |
| Commit/push | Authorized; preparation f6cd7ea published and remote verified |
| Graphify rebuild | NOT RUN |

## Ticket evidence

Каждый тикет ниже планируемый; после выполнения добавить реальные команды, результаты, diff/commit при наличии и ссылки на доказательства. Planning audit не является доказательством реализации.

| Ticket | Implementation | Automated | Native/human |
|---|---|---|---|
| [EP-0010-US-0001-TK-0001-immutable-report](EP-0010-US-0001-report-contracts/EP-0010-US-0001-TK-0001-immutable-report/EP-0010-US-0001-TK-0001-immutable-report.md) | IMPLEMENTED | 13 focused / 207 suite PASS | OPEN / по scope |
| [EP-0010-US-0001-TK-0002-snapshot-batch](EP-0010-US-0001-report-contracts/EP-0010-US-0001-TK-0002-snapshot-batch/EP-0010-US-0001-TK-0002-snapshot-batch.md) | IMPLEMENTED | 3 focused / 210 suite PASS | OPEN / по scope |
| [EP-0010-US-0002-TK-0001-report-outbox](EP-0010-US-0002-range-reports/EP-0010-US-0002-TK-0001-report-outbox/EP-0010-US-0002-TK-0001-report-outbox.md) | IMPLEMENTED | 4 focused PASS; build/format PASS | OPEN / по scope |
| [EP-0010-US-0002-TK-0002-first-range-entry](EP-0010-US-0002-range-reports/EP-0010-US-0002-TK-0002-first-range-entry/EP-0010-US-0002-TK-0002-first-range-entry.md) | IMPLEMENTED | 7 focused + 4 outbox + 14 regressions PASS | OPEN / по scope |
| [EP-0010-US-0003-TK-0001-captured-reporters](EP-0010-US-0003-launch-reports/EP-0010-US-0003-TK-0001-captured-reporters/EP-0010-US-0003-TK-0001-captured-reporters.md) | IMPLEMENTED | 5 focused + 11 regressions PASS | OPEN / по scope |
| [EP-0010-US-0003-TK-0002-confirmed-launches](EP-0010-US-0003-launch-reports/EP-0010-US-0003-TK-0002-confirmed-launches/EP-0010-US-0003-TK-0002-confirmed-launches.md) | IMPLEMENTED | 5 focused; 26 report/combat PASS | OPEN / по scope |
| [EP-0010-US-0004-TK-0001-intercept-results](EP-0010-US-0004-combat-result-reports/EP-0010-US-0004-TK-0001-intercept-results/EP-0010-US-0004-TK-0001-intercept-results.md) | IMPLEMENTED | 5 focused; 26 reports PASS | OPEN / по scope |
| [EP-0010-US-0004-TK-0002-contact-summary](EP-0010-US-0004-combat-result-reports/EP-0010-US-0004-TK-0002-contact-summary/EP-0010-US-0004-TK-0002-contact-summary.md) | IMPLEMENTED | 6 focused; 60 report/combat PASS | OPEN / по scope |
| [EP-0010-US-0005-TK-0001-save-schema-validation](EP-0010-US-0005-report-save-continuation/EP-0010-US-0005-TK-0001-save-schema-validation/EP-0010-US-0005-TK-0001-save-schema-validation.md) | IMPLEMENTED | 13 focused PASS | OPEN / по scope |
| [EP-0010-US-0005-TK-0002-capture-and-restore](EP-0010-US-0005-report-save-continuation/EP-0010-US-0005-TK-0002-capture-and-restore/EP-0010-US-0005-TK-0002-capture-and-restore.md) | NOT STARTED | NOT RUN | OPEN / по scope |
| [EP-0010-US-0006-TK-0001-mailbox-report-merge](EP-0010-US-0006-reliable-report-delivery/EP-0010-US-0006-TK-0001-mailbox-report-merge/EP-0010-US-0006-TK-0001-mailbox-report-merge.md) | NOT STARTED | NOT RUN | OPEN / по scope |
| [EP-0010-US-0006-TK-0002-buffer-pending-reports](EP-0010-US-0006-reliable-report-delivery/EP-0010-US-0006-TK-0002-buffer-pending-reports/EP-0010-US-0006-TK-0002-buffer-pending-reports.md) | NOT STARTED | NOT RUN | OPEN / по scope |
| [EP-0010-US-0007-TK-0001-history-state](EP-0010-US-0007-report-history-and-text/EP-0010-US-0007-TK-0001-history-state/EP-0010-US-0007-TK-0001-history-state.md) | NOT STARTED | NOT RUN | OPEN / по scope |
| [EP-0010-US-0007-TK-0002-localized-report-text](EP-0010-US-0007-report-history-and-text/EP-0010-US-0007-TK-0002-localized-report-text/EP-0010-US-0007-TK-0002-localized-report-text.md) | NOT STARTED | NOT RUN | OPEN / по scope |
| [EP-0010-US-0008-TK-0001-launch-art](EP-0010-US-0008-report-illustrations/EP-0010-US-0008-TK-0001-launch-art/EP-0010-US-0008-TK-0001-launch-art.md) | NOT STARTED | NOT RUN | OPEN / по scope |
| [EP-0010-US-0008-TK-0002-defense-art](EP-0010-US-0008-report-illustrations/EP-0010-US-0008-TK-0002-defense-art/EP-0010-US-0008-TK-0002-defense-art.md) | NOT STARTED | NOT RUN | OPEN / по scope |
| [EP-0010-US-0008-TK-0003-damage-art](EP-0010-US-0008-report-illustrations/EP-0010-US-0008-TK-0003-damage-art/EP-0010-US-0008-TK-0003-damage-art.md) | NOT STARTED | NOT RUN | OPEN / по scope |
| [EP-0010-US-0008-TK-0004-catalog-and-cache](EP-0010-US-0008-report-illustrations/EP-0010-US-0008-TK-0004-catalog-and-cache/EP-0010-US-0008-TK-0004-catalog-and-cache.md) | NOT STARTED | NOT RUN | OPEN / по scope |
| [EP-0010-US-0009-TK-0001-card-rendering](EP-0010-US-0009-report-accordion/EP-0010-US-0009-TK-0001-card-rendering/EP-0010-US-0009-TK-0001-card-rendering.md) | NOT STARTED | NOT RUN | OPEN / по scope |
| [EP-0010-US-0009-TK-0002-accordion-input](EP-0010-US-0009-report-accordion/EP-0010-US-0009-TK-0002-accordion-input/EP-0010-US-0009-TK-0002-accordion-input.md) | NOT STARTED | NOT RUN | OPEN / по scope |
| [EP-0010-US-0010-TK-0001-right-column-layout](EP-0010-US-0010-tactical-screen-integration/EP-0010-US-0010-TK-0001-right-column-layout/EP-0010-US-0010-TK-0001-right-column-layout.md) | NOT STARTED | NOT RUN | OPEN / по scope |
| [EP-0010-US-0010-TK-0002-screen-render-and-input](EP-0010-US-0010-tactical-screen-integration/EP-0010-US-0010-TK-0002-screen-render-and-input/EP-0010-US-0010-TK-0002-screen-render-and-input.md) | NOT STARTED | NOT RUN | OPEN / по scope |
| [EP-0010-US-0010-TK-0003-session-reset-and-defeat](EP-0010-US-0010-tactical-screen-integration/EP-0010-US-0010-TK-0003-session-reset-and-defeat/EP-0010-US-0010-TK-0003-session-reset-and-defeat.md) | NOT STARTED | NOT RUN | OPEN / по scope |
| [EP-0010-US-0011-TK-0001-engine-scenario-proof](EP-0010-US-0011-report-acceptance/EP-0010-US-0011-TK-0001-engine-scenario-proof/EP-0010-US-0011-TK-0001-engine-scenario-proof.md) | NOT STARTED | NOT RUN | OPEN / по scope |
| [EP-0010-US-0011-TK-0002-client-integration-proof](EP-0010-US-0011-report-acceptance/EP-0010-US-0011-TK-0002-client-integration-proof/EP-0010-US-0011-TK-0002-client-integration-proof.md) | NOT STARTED | NOT RUN | OPEN / по scope |
| [EP-0010-US-0011-TK-0003-native-visual-and-performance](EP-0010-US-0011-report-acceptance/EP-0010-US-0011-TK-0003-native-visual-and-performance/EP-0010-US-0011-TK-0003-native-visual-and-performance.md) | NOT STARTED | NOT RUN | OPEN / по scope |
| [EP-0010-US-0012-TK-0001-project-documentation](EP-0010-US-0012-documentation-and-handoff/EP-0010-US-0012-TK-0001-project-documentation/EP-0010-US-0012-TK-0001-project-documentation.md) | NOT STARTED | NOT RUN | OPEN / по scope |

## Execution started — 2026-10-16

Запрошено полное автономное выполнение по Board/EpicExecutionPrompt.md. Подготовительное чтение и аудит продолжаются; runtime ещё не изменён, тикеты не завершены. Последняя история US0012 расширена до §6, включая Graphify.

- Baseline HEAD: af594de8465421f45cd362e82c6c1dbfdd8631ed; branch base-fight, upstream origin/base-fight.
- Origin: https://github.com/SuhilovVitaly/DSS. Сетевой доступ из sandbox не разрешил DNS; разрешённый повтор git ls-remote подтвердил тот же удалённый SHA.
- Исходное дерево: изменены Documentation/01-Requirements/EngineRequirements.md и Documentation/README.md; неотслеживаемые целевой Board и TacticalCombatReports.md. Это входные материалы пользователя; tracked правки пока сохраняются вне служебного коммита. Целевой Board и его исходное ТЗ фиксируются как утверждённая база исполнения.
- Commit/push разрешены текущим запросом; planning-only запреты карточек заменены этим поручением. Нативные и человеческие gates остаются OPEN до фактических проверок.
- Обязательное полное чтение Documentation и эпика ещё не завершено. Production implementation не начинается до завершения preflight.

### Current checkpoint

Подготовительный f6cd7ea41a705aca7d6c25689c96de5bc35f1306 опубликован в origin/base-fight и SHA подтверждён git ls-remote. Первичный auto-review отказ push снят после read-only GitHub проверки: connected user=owner SuhilovVitaly, repository permissions admin/push=true. Обязательное чтение Documentation и карточек выполнено частями; текущая реализация соответствует contract US0001. Baseline Contracts194/194, после первого DTO207/207. Полный solution baseline в sandbox завершился exit1 без diagnostics; scoped test в sandbox завис на IPC и был отменён, разрешённый host run PASS.

US0001 review: оба AC проверены, Contracts210/210, build/format/diff PASS. TK0001 a49debeb6a6df8b01b150487b6d002420a53cfcd —push и remote SHA PASS. TK0002 e944f2827c7ab294e6e7b05c153a5236fd4650ac опубликован, remote SHA PASS. US0002 выполняется.

US0002: TK0001 5f682d2dfe8e47ba5dda007390e12c31c3e3b2ad published/remote verified. TK0002 E01 implemented and self-reviewed; focused11 + existing combat14 PASS. Full Engine suite cancelled without result; not a green gate.

US0002 TK0002 00d3c9d8afdfe6ba992e126bffc68c207ccd06fc published/remote verified; story self-review complete. US0003 identity helper implemented, 16 report cases PASS.

US0003 TK0001 4b6b1d0d6419ff51eef8c026ad9d300a2645e0ac published/remote verified. TK0002 launches implemented, 26 cases PASS, story self-review complete.

US0003 launch TK0002 5cf06b1a04ec892aab59a7a380fc030eb1aa50ca published/remote verified. US0004 E05 implemented, report suite26 PASS.

US0004 TK0001 caadaa025d86a6388ac1a714092cc81a87aa70d6 published/remote verified. Contact summaries implemented, story self-review complete.

US0004 summary 518efe5495721825e2fefc562ad5004a7f345fe5 published/remote verified. US0005 schema13 PASS; continuation capture/restore next.
