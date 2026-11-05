# EP-0012 native evidence — 2026-10-21

**Checklist recorded; product/human acceptance OPEN.** Real Intel Arc140V16GB OpenGL3.3 native SkiaWindow, production CombatMukha seed3001, actual Engine snapshots and public commands. Controlled Engine/render clocks and managed screen callbacks; no fabricated outcomes. These checks do not establish physical OS input, normal session transport or manual gameplay acceptance.

24/24 runs PASS:16 input =1280×720 and1920×1080 × scales0.8/1/1.2/1.5 × RU/EN;8 terminal = both outcomes/locales at1280×720/1.5 and1920×1080/1.0. Actual scale, locale, viewport and GPU checked in each JSON. Additional clean-flight native probe PASS. Settings bytes restored; SHA256 `23c8ae516b1349b9069136bc1121f3c530c719a03f9b0e9d63b13ad3483a602d`.

[Machine results and exact commands](us0014-tk0003-results.json). [Native JSON/PNG directory](native/). Each JSON lists captures, checks, actual clocks, commands and limitations.

## Reproduction

`dotnet run --no-build --project tools/DeepSpaceSaga.Performance -- <repo> <absolute-report.json> --combat-window <0.8|1|1.2|1.5> <1280x720|1920x1080> <ru|en> <input|win|defeat> [--audio] [--clean-flight]`

Build tool first; process cap300000ms, internal3min guard. Tool temporarily changes only deployed Settings and restores original bytes in finally. Terminal replay uses unchanged production balance policy tape and fresh Engine: seed3001 dodge Won52500ms physical; static Defeat21791.02558ms physical. Managed callbacks are intentional test instrumentation.

## UI acceptance coverage

PASS refers only to the stated evidence. NOT RUN means that native/human subcriterion remains open even when its headless counterpart passed. Test classes are in `tests/DeepSpaceSaga.Client.Tests/PointCombat<US/TK>Tests.cs`; their complete owning suite passed2225/2225.

| Criterion/subcriteria | Actual result and boundary | Evidence |
|---|---|---|
| US10-AC1 own trace/R/core/countdown; foreign trace/profile; CM circle | PASS native confirmed-flights/counter-flight, ownPhysicalRadiusAndCore + foreignHasNoEndpointOrDeadlineVisual. Foreign endpoint/deadline absent. | All16 input reports |
| US10-AC1 torpedo/CM explosion; shot-down effect | PASS native actual manual-blast/counter-blast. Shot-down native variant NOT RUN: seed/script does not force interception; headless rendering/event tests separate. | US0010TK0002; US0013TK0003 tests |
| US10-AC1 R within1px; separate minimum6px marker | PASS geometry in16 native runs and minimum-markers capture; independent clean native four-axis orange edge offsets −0.875/−0.625/+0.125/−0.125px. This sample does not certify all angles/scales. Headless bitmap geometry regressions PASS13. | Clean report/PNG and label-after |
| US10-AC2 persistent70/30km rings; module toggles; session state | PASS native ranges70And30/rangeToggleSessionState; PNGs and session callback sequence. | All16 input reports |
| US10-AC3 own velocity/5physical-second arc; F fit | F exercised in native input; visible path captured. Exact5s/arc geometry headless PASS; independent native temporal oracle NOT RUN. | US0010TK0004 tests |
| US10-AC4 data-only colors/styles | Source/data and headless tests PASS; native colors rendered. Independent audit of every GPU color NOT RUN. | Data/UI/combat-visuals.json, US0010TK0001 tests |
| US11-AC1 Q/W first-ready, specific-panel aim; profiles1–3/wheel; click/cancel; data map; reload aim/no launch | PASS native Q/W, profile/wheel, left click, Esc, reloadAimDoesNotSend; 1–3 preserve speed. Specific-module panel, right-click and every first-ready fallback native combination NOT RUN; headless PASS. | US0011TK0001 tests;16 input reports |
| US11-AC2 R/scatter/trace/real ETA; enemy linear shadows/coverage; out-of-range; friendly two-click1s; no chance | PASS native R/ETA/trace/out-of-range preview+no-send, screenshots show current point labels without chance. Exact scatter/shadow coverage and friendly two-click native variants NOT RUN; headless PASS. | US0011TK0002 tests |
| US11-AC3 trace snap12px; BlastThreatMath danger/frontier; arrival/margin/late/unreachable; Alt; ×N; own torpedo warning; en-route interception | PASS native counterPreviewAndTraceSnap + counter-aim PNG (arrival/margin/unreachable). Exact12px, late/×N/friendly/interception variant native checks NOT RUN; headless PASS. Physical held Alt NOT RUN: native window polls actual modifier hardware, managed modifier injection cannot prove it. | US0011TK0003 tests;report.notRun |
| US11-AC4 point/profile commands; localized rejection; <=100ms feedback; authoritative birth | PASS actual torpedo.launch/defense.launch and manual command tape, noProjectileBeforeAuthoritativeSnapshot. Managed dispatch maximum 2.2954ms. Physical click-to-photon and normal transport latency NOT RUN; callback timing does not prove their100ms gate. Localized reject variants headless PASS, native NOT RUN. | US0011TK0004 tests;16 input reports |
| US12-AC1 A/D current/default threat; click/Tab; held preview/release command; arrows/Shift; base arrow; disabled engine timer | PASS native A preview/release actual evade/base course, held Right actual heading change, Shift command. D/Tab/click/disabled-engine native variants NOT RUN; headless PASS. | US0012TK0001 tests |
| US12-AC2 threat time order/profile/interception/buttons; <=200ms scene admission; R+3 filter | Threat panel appears in actual native frames. Exact sorting/filter/buttons/interception and <=200ms scene-admission native timing NOT RUN; headless tests PASS, not OS end-to-end timing. | US0012TK0002 tests |
| US12-AC3 danger/near/miss styles distinguishable at deuteranopia; enemy flash/offscreen arrow/audio; urgent<=3s;1.5s throttle | Native styles/launch flash captured; one run requests actual CombatAlertAudio cues. Hearing, deuteranopia perception, offscreen and urgency cadence human/native acceptance NOT RUN. Headless styling/state/throttle tests PASS. | US0012TK0003 tests;1280x720-s1p5-en-input.json audio |
| US13-AC1 own armor/structure/disabled/destroyed; critical border; N/3; hit flash/−N module; no hull HP | Native module tiles/critical borders/N/3 and actual player-hit frames captured; actual terminal defeat. Exact disabled timer/damage label blink timeline native oracle NOT RUN; headless PASS; no-hull-HP source/headless verified. | US0013TK0001 tests; terminal player-hit captures |
| US13-AC2 reload<=0.1s; profile/operator/accuracy/scatter/state/hotkey/range toggle; no legacy controls/chance; manual detonate | Native current panels and actual manual detonation PASS; modern-only rows asserted. Exact0.1s reload precision headless PASS, independent native clock measurement NOT RUN. | US0013TK0002 tests |
| US13-AC3 enemy damaged/immobilized/destroyed; core/edge/miss/shot-down labels; no numeric enemy damage | Native actual effects visible but all status/result combinations NOT RUN. Headless exact privacy/labels PASS; no fabricated native variants. | US0013TK0003 tests |
| US13-AC4 journal and EP10 reports launch/detonation/interception/hit, no chance/roll | Actual native journal/report launch+manual/CM blasts and hits captured. Full interception/report vocabulary variants headless PASS; native exhaustive variants NOT RUN. | US0013TK0004 tests |
| US13-AC5 battle banner/audio/border; real timer; nonmodal Won/Continue; Defeat stats; x5+disabled/tooltips | PASS8 terminal native runs with actual Engine outcomes; Won Continue actual acknowledgement, DefeatScreen stats; Active banner/speed lock asserted in16 input runs. Hearing and physical tooltip hover NOT RUN. | US0013TK0005 tests;17PASS after Won layout fix |
| US16-AC1 Localization RU/EN, no visible combat literals/fallback | PASS native actual Russian/English labels and screenshots across all4 supported scales/two sizes; headless localization catalog/no-fallback coverage PASS. Human exhaustive proofreading NOT RUN; scenario names/crew identities are content. | US0016TK0001 tests |

## Confirmed defects and verification

Visible projectile label clipped at right edge: focused regression2FAIL before; rendering class13PASS after clamping labels to physical viewport. Actual Won summary absent at1280×720/1.5: focused regression1FAIL1PASS before; HUD class17PASS after choosing a sufficiently tall free rectangle (cached by layout hash). Native reruns show actual summary/Continue and acknowledgement. Default map framing, Engine, catalogs and input policy unchanged.

Initial harness mistakes (coordinate units, hardware Alt assumptions, ru/en Settings aliases and unsupported1.25 scale) were corrected; initial matrix is archived outside repo and is not locale/scale evidence. Initial overlapping aim-preview ring made a naive all-angle radius oracle ambiguous. Clean-flight capture cancels preview and independent Pillow four-axis sample is within1px; do not generalize it to all pixels.

FullClient2225PASS, whole solution build0warnings/errors, scoped whitespace/full formatter and diff check PASS. Review is implementer self-review, not independent. Windows PowerShell runner attempt lacked ArgumentList and launched no dotnet; rerun with pwsh passed.

## Remaining acceptance

Physical keyboard/mouse (including held Alt), real input-to-photon latency, normal transport/menu/bootstrap/save-load UI, audio hearing/cadence, deuteranopia perception and cramped-layout usability remain NOT RUN/OPEN. Script does not exhaust every combination above. EP11 native/palette acceptance remains unchanged. Balance remains OPEN: dodge96.6% andP907.4933realmin are outside target72–88% and<=5.5min; static5.9% meets its<=10% corridor. No balance retuning was performed. See [production balance](balance.md).
