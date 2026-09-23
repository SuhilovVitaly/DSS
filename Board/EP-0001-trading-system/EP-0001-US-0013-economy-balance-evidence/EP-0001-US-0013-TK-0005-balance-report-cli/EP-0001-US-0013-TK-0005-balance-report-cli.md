---
epic: EP-0001-trading-system
story: EP-0001-US-0013-economy-balance-evidence
ticket: EP-0001-US-0013-TK-0005-balance-report-cli
title: Канонический отчёт и команда release-gate
stage: approved
layer: tooling
depends_on: [EP-0001-US-0013-TK-0003-market-health-evaluation, EP-0001-US-0013-TK-0004-strategy-balance-evaluation]
files_touched: 5
serves: [AC-01, AC-02, AC-03, AC-04, AC-05]
created: 2026-09-21T14:55:45Z
revision: 1
---

# Канонический отчёт и команда release-gate

## Why

Собрать runner и оба evaluator в одну воспроизводимую команду, которая выполняет полный 24-case corpus, пишет canonical JSON и возвращает machine-readable exit status. End state: автор баланса получает один артефакт с matrix parameters, агрегатами, ledger breakdown и всеми violations; два одинаковых запуска byte-identical.

Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.EconomyBalance.Tests/DeepSpaceSaga.EconomyBalance.Tests.csproj`.

## Decisions

Единственное сообщение пользователя приведено в story; дополнительных решений нет.

## Assumptions

- Generated report передаётся явным output path и не коммитится автоматически.
- Full corpus — release/manual gate; обычные unit tests используют reduced fixture, чтобы solution test не превращался в десятидневный integration run.
- Exit codes: `0=pass`, `1=balance violations`, `2=configuration/runtime failure`. Partial report при code 2 пишется только если schema-valid header и contextual error можно сформировать безопасно.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `tools/DeepSpaceSaga.EconomyBalance/DeepSpaceSaga.EconomyBalance.csproj` | Создан TK-0002 как library | Переключить в Exe, copy `balance-matrix.json` to output; сохранить references/friend |
| `tools/DeepSpaceSaga.EconomyBalance/Program.cs` | Новый файл | Strict args, matrix load, runner/evaluators, canonical JSON write, summary and exit codes |
| `tools/DeepSpaceSaga.EconomyBalance/balance-matrix.json` | Новый файл | Exact schema v1: 12 seeds, two configs, 240h interval/checkpoint and thresholds |
| `tests/DeepSpaceSaga.EconomyBalance.Tests/BalanceReportTests.cs` | Новый файл | Config/schema, byte determinism, exit codes, report context and reduced end-to-end tests |
| `DeepSpaceSaga.sln` | Сейчас перечисляет production и четыре test projects, tooling projects отсутствуют (`:6–28`) | Добавить tool и matching test project в `tools`/`tests` solution folders с all configurations; другие GUID/configuration rows не менять |

## Public API after the change

CLI:

```text
dotnet run --project tools/DeepSpaceSaga.EconomyBalance/DeepSpaceSaga.EconomyBalance.csproj -- \
  <DSS-root> <output.json> [--matrix <matrix.json>]
```

Default matrix is packaged `balance-matrix.json`:

```json
{
  "schemaVersion": 1,
  "seeds": [1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233],
  "shipConfigurations": [
    { "id": "starter", "cargoCapacityMultiplierPermille": 1000, "fuelEfficiencyMultiplierPermille": 1000 },
    { "id": "cargo-upgrade", "cargoCapacityMultiplierPermille": 2000, "fuelEfficiencyMultiplierPermille": 1000 }
  ],
  "horizonHours": 240,
  "sampleIntervalHours": 1,
  "saveLoadCheckpointHours": 120,
  "zeroStockMaximumPermille": 250,
  "shortMarginMinimumPermille": 50,
  "shortMarginMaximumPermille": 150,
  "mediumMarginMinimumPermille": 150,
  "mediumMarginMaximumPermille": 350,
  "maximumToMedianMultiplierPermille": 2000
}
```

Report root fields: `schemaVersion`, `status`, `matrix`, `summary`, `cases`, `violations`. `cases` sorted seed/config; nested samples/strategies/ledgers use TK-0002 order. No generated-at timestamp, OS, temp path or absolute repository path. `violations` use shared `BalanceViolation` schema.

## Implementation steps

1. Convert tool to Exe and deserialize matrix with case-sensitive names, unknown-member rejection and invariant numeric parsing. Require schemaVersion1 and exact story constants for default full matrix; custom reduced matrix permitted only when explicit `--matrix` is supplied and labelled `isAcceptanceCorpus=false` in report.
2. Resolve `<DSS-root>` absolute, then fixed repository inputs `src/DeepSpaceSaga.Client/Settings.json` and the shipped trading scenario selected by Settings after prerequisite implementation. Reject missing/wrong scenario with code2; do not fall back to `Default_500` or synthetic content.
3. Run `EconomyBalanceRunner`, `MarketHealthEvaluator` per case and `StrategyBalanceEvaluator.EvaluateCase/EvaluateCorpus`. Aggregate all violations; do not stop on first balance failure.
4. Build immutable report and recursively sort dictionaries/arrays by documented keys. Serialize UTF-8 without BOM, LF newline, indented stable property order. Write to temp sibling then atomic replace/move; failed run must not truncate an existing good report.
5. Status `pass` only for exact acceptance corpus with zero violations and all 24 cases. Reduced/custom zero-violation run is `diagnostic-pass`, never release pass. Print one-line counts/path; code0 only `pass`, code1 for any balance violation or non-acceptance diagnostic result, code2 for malformed config/runtime.
6. Every case includes continuous/save-load hashes, station/stock/event summaries and all strategy ledger components. Every violation references seed/config plus route/item/station/time where applicable and exact expected/observed strings.
7. Add both projects to solution through `dotnet sln ... add`, then nest tool under a new/existing `tools` solution folder and tests under `tests`; preserve existing project GUID rows.
8. Run reduced end-to-end test, then the full acceptance command twice to different temp outputs and byte-compare. Delete generated temp reports after verification unless user explicitly asks to retain them.

## Out of scope

CI workflow, committed generated report, dashboard/UI, CSV/HTML, automatic coefficient editing, external database, telemetry, performance target or production gameplay changes.

## Invariants

- Default full thresholds come from story AC-01…AC-04 and `Board/EP-0001-trading-system/Documentation.md:104`; CLI flags cannot silently weaken them.
- Settings is the authoritative content entrypoint: `src/DeepSpaceSaga.Client/Settings.json:1–14`; `Default_500` remains stress-only per technical assignment.
- Atomic output preserves last good artifact on error; repository save/scenario files are read-only inputs.
- Five implementation files exactly, all changes belong to tooling integration and its tests.

## Tests

`BalanceReportTests`:

- `Packaged_matrix_is_exact_acceptance_corpus` (AC-01).
- `Unknown_property_duplicate_seed_or_changed_default_threshold_is_code_two` (AC-01/05).
- `Reduced_fixture_writes_schema_complete_diagnostic_report_but_not_release_pass` (AC-05).
- `Same_evidence_serializes_byte_identically_without_machine_or_time_fields` (AC-01/05).
- `Violation_report_contains_seed_config_route_item_ledger_expected_and_observed` (AC-05).
- `Balance_failure_is_code_one_and_collects_health_and_strategy_violations` (AC-02/03/04/05).
- `Runtime_failure_is_code_two_and_preserves_existing_output` (AC-05).
- `Solution_build_discovers_tool_and_matching_tests` (structural).

Commands:

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.EconomyBalance.Tests\DeepSpaceSaga.EconomyBalance.Tests.csproj --no-restore
dotnet build D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
dotnet run --project D:\DeepSpaceSaga\DSS\tools\DeepSpaceSaga.EconomyBalance\DeepSpaceSaga.EconomyBalance.csproj -- D:\DeepSpaceSaga\DSS <output.json>
```

Acceptance evidence: выполнить full command дважды с разными temp output paths, подтвердить code0, `status=pass`, `cases=24`, `violations=0`, byte equality; затем удалить оба generated JSON. Если текущий tuning не проходит, ticket implementation и report считаются технически готовыми при code1/schema-valid evidence, но story/эпик balance sign-off не считается достигнутым и G-03 открывает отдельный content-data tuning ticket.

## Definition of Done

- Все steps выполнены ровно в пяти allowed files; generated reports не остаются в repo.
- AC-01…AC-05 покрыты named tests и full acceptance command.
- Tool/test/solution build, tests и format проходят либо конкретное baseline failure записано отдельно.
- Exit codes, atomic write, canonical order и pass-vs-diagnostic semantics соблюдены.
- Public gameplay API/content/UI не изменены; thresholds нельзя ослабить скрытым CLI option.
- Нет незаписанных assumptions, unresolved blocking questions или работы вне `Code context`.
- Результат проверяется одним documented command и содержит достаточный reproduction context.

## Self-containment check

Пять файлов, exact matrix JSON, CLI syntax, input resolution, report schema/order, exit codes, atomic-write policy, full/reduced distinction и verification commands заданы. Implementer не выбирает seeds, thresholds, upgrade profile или критерий release pass.
