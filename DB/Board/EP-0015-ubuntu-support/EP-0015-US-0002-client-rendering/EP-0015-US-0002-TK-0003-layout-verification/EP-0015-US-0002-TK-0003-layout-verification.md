---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0002-client-rendering
ticket: EP-0015-US-0002-TK-0003-layout-verification
title: "Проверка раскладки после смены шрифтов"
stage: draft
layer: client
depends_on: [EP-0015-US-0002-TK-0002-bundled-fonts]
serves: [AC-03, AC-04]
created: 2026-10-25
---

# Проверка раскладки после смены шрифтов

История: [EP-0015-US-0002-client-rendering](../EP-0015-US-0002-client-rendering.md) · Эпик: [EP-0015](../../Documentation.md)

## Зачем

Inter и JetBrains Mono по ширине отличаются от Segoe UI и Consolas. Нужно найти и поправить места, где текст обрезается или колонки разъезжаются.

## Шаги

1. Снять скриншоты на Windows до и после TK-0002: главное меню, сохранение/загрузка, тактическая карта с панелью торпедного аппарата и панелью отладки (F12), торговля.
2. Поправить жёстко заданные ширины колонок и размеры шрифта там, где текст перестал помещаться.
3. Повторить те же скриншоты на Ubuntu и сравнить с Windows.
4. Прогнать `DeepSpaceSaga.Client.Tests` на обеих ОС; тесты с ожиданиями ширины текста обновить осознанно, с пояснением в тикете.

## Зависимости

[EP-0015-US-0002-TK-0002-bundled-fonts](../EP-0015-US-0002-TK-0002-bundled-fonts/EP-0015-US-0002-TK-0002-bundled-fonts.md)

## Definition of Done

- Набор скриншотов Windows/Ubuntu приложен к тикету; обрезанного текста нет.
- `DeepSpaceSaga.Client.Tests` проходит на обеих ОС.
- Проверено на Windows и Ubuntu; на Windows поведение не ухудшилось.

## Scope and Code context 2026-10-26

ТекущийfullClient2326/2326. Дляпроверяемыхnativecapturesсоставscopeуточнён: tools/DeepSpaceSaga.Performance/PlatformLayoutNativeEvidence.cs иProgram.cs (реальныйSkiaWindow,productionэкраны,LocalGameSessionConnection ифайловыйSave); UI/InterfaceLog.cs (абсолютныйdiagnosticpathoverride,чтобыCWDзагрузкиассетовнеписаллогвbin); existingLogTailReader,GameSessionScalePanelTests,GameSessionZoomTests (чтениефактическогоLogPathбезослабленияassertions); tools/ci/platform-baseline.py (маршрутизациялога дочернегопроцессавегоevidence); CIworkflowдляLinuxnativecaptureпослеbuild. Расширениеболее5файловобусловленореальнымnativeevidence/diagnosticownership,неизменениемEngineправил.

РеальныйсценарийDocked сseed1,pausedEngine,localtransport,настоящийSaveSlotRepository иTradeScreen;callbacksсохранения/загрузкиреальные. CaptureизGPU SKSurface,метрикиfile-backedfonts игеометрияпанелейсохраняютсядляWindows/Linuxсравнения. Xvfb/llvmpipeпомечаетсякакsoftwareOpenGL;неphysical/human/Waylandприёмка. До-фонтныйnativebaselineNOT RUN:Codexcreate_worktreeвернулNot a git repositoryдляchatcwdD:/DeepSpaceSaga;manualcheckout/sourcecopyнеприменяются. После-фонтныйcaptureиindependentmetricscomparisonвыполняютсядоступнымипутями.

## Native findings and scope extension

ПерваяверсияprobeошибочносчиталаblackTrade/Stationвместоmapвалиднымиcapture:Self-reviewPNGвыявилневернуюsessionbinding. Probeисправлен:реальныйMarkettravel,отдельнаяundockedPlayerShipOnlysession,проверкаactualscreenidentity/OpenGL/nonblank/captions. ПервыеJSONсpassedtrueнеявляютсяприёмкой. Windows-en-1-fixedподтвердилactualscreens,нозатемвизуальнообнаруженреальныйcropTrade1280x720:fixed1600x800panelцентрируетсясотрицательнымикоординатами. ЭтоотносящийсякTKдефект;scopeрасширеннаTradeLayout.cs,TradeScreen.cs,TradeScreen.Render.cs иTradeUxTests.cs. Исправление—равномерныйfit-scaleвсегопанелявместесобщимtoolbarиобратныйtransformmouseкоординат;1920x1080legacygeometryнеизменяется. Проверкиновыхразмеровдолжныдоказатьvisibleboundsиitem/exitinputrouting,неснижаяassertions.

## Validation infrastructure refinements

Engine--list-testsимеет2484rows,но2476uniquedisplaynames:8JSONrowscollideиз-заxUnittruncation. HarnessсохраняетmultiplicityиTRXмультимножествоимён;dedupнеподменяетcoverage. CIшардированна6реальныхtestprojects×2ОС,каждыйявноназванвselection;defaultcheck-linux.shпо-прежнемусканируетвсепроектыоднойкомандой. Каждыйtestprocesscap300ссохранён;jobbudget120minнеповышаетprocesscap. NativeLinuxstepтолькоClientshardподXvfb;захваты6экранов×8cases. SDK8/setup/failureevidenceнеотключались.

Diagnostic scope: CombatMetricsWriter.cs and DebugTuningFileWriter.cs now honor the same absolute DSS_DIAGNOSTICS_DIRECTORY override as validation subprocesses. This prevents test output and retention from touching pre-existing bin diagnostics. Production defaults are unchanged until the user-path ticket.

## Windows validation checkpoint

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-layout/. client-layout-full.trx: 2329 passed, 0 failed/skipped (C# 12); format-layout.log: scoped formatter exit 0. trade-fit.trx: 106 passed including three viewport/input cases. windows-matrix.json references eight final native cases with six real OpenGL PNG captures each (48 total), RU/EN and scales 0.8/1/1.2/1.5. Trade fit fixed at 1280x720; selected-item quote and exit input remain tested. Linux captures and pixel/geometry comparison are pending the newly sharded CI. Pre-font native baseline and human/physical-input/Wayland acceptance remain NOT RUN, not inferred from these captures.

Linux native CI40288631755 failed Window.Create under Xvfb: no applicable GLFW/SDL platform. Evidence downloaded intact to D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-layout-linux-ci/. Scope follow-up: install explicit Ubuntu graphics runtime dependencies, collect ldd/glxinfo outside repository, and print native JSON failure in driver. No software/native PASS is claimed from the failed capture.

Cross-OS visual self-review: Linux8 cases have identical panel geometry; raw advances differ up to~0.0014px and opaque QuoteId nonce differs per session, so equality of those values is not a valid acceptance rule. Font comparison uses an explicit1/64px tolerance; compare quote item/price/quantity and require nonempty identity, not equal nonce. Linux standalone Trade-root screenshot also exposed undefined framebuffer margins outside an overlay. Probe correction: render actual docked GameSession→Station→Trade stack via production ScreenStack, rather than treating an overlay as a root. No production rendering behavior is changed. Scope: existing Performance native harness and CI path trigger; capture price/quantity facts for independent comparison and repeat Windows/Linux matrices. Earlier standalone capture is geometry/font evidence, not final composite visual acceptance.

Composite follow-up Windows evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-layout-composite/windows/matrix.json8/8,48 PNG; Trade now has real docked GameSession/Station underlay rather than undefined standalone framebuffer margins. C#12 build.log zero diagnostics; selected quote includes price/quantity facts. Neutral geometry and font advance tolerance documented for cross-OS comparison; opaque QuoteId nonce is deliberately not equality-compared. CI includes Performance harness source changes and disables Python bytecode caches in repository.

Diagnostic scope includes platform-layout.py and platform-publish.py standalone import guards (sys.dont_write_bytecode) to prevent validation imports creating repository caches; no runtime application behavior changed. format.log scoped exit0.
