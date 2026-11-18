# Kneeling crew bodies

Seven adult feminine body variants for the EP-0036 prisoner arrest presentation: pilot, engineer, medic, security, explorer, EVA and white athletic top/shorts. The pelvis, thighs and knees turn toward the viewer's right; folded shins and feet trail left. Both knees rest on the floor; the arms and hands are behind the back. These are new assets, not an implementation of the arrest UI.

Each variant contains a complete headless `body.png`, compositing layers `body-back.png` and `collar-front.png`, and `profile.json`. All PNGs use a 1024 x 1536 canvas with real alpha. The empty upper canvas is intentional: use the same canvas and ground baseline as standing crew, rather than fitting each visible bounding box independently.

## Head registration

- Anatomical face scale: eye line to chin = 68 px, unchanged from standing crew.
- Chin anchor: (522.5, 913.7), 451.7 px below the standing anchor (522.5, 462).
- Collar opening center: (522.5, 931.7). The elliptical opening dimensions are recorded per costume.
- Ground baseline: y = 1518. Whole bodies were uniformly scaled and translated; no anisotropic stretching was used.
- Draw `body-back.png`, then the transparent head, then `collar-front.png`. Do not draw the complete `body.png` as the back layer; its front portion would be applied twice.
- Remove the portrait's green background before compositing. Scale using its anatomical landmarks, not its file dimensions or hairstyle bounding box. Move the chin to the profile anchor. Clip the lower neck using `neck_clip` from the profile; keep the hair and face above that band intact.
- `example_head_transform` applies specifically to CHR-20261117-011947-JFG71U.png (235 x 235), with its chin at (117, 186). Other portraits require their own anatomical scale calculation.
- Shorts use skin-feather registration, not the suit ellipse mask. Read the current profile for body fade, portrait fade and upper-neck taper. Collar-front is intentionally transparent. Upper-neck taper is portrait-specific: inspect facial anatomy and never trim a jaw or hair to force the socket.

The profiles are asset metadata. The existing game compositor does not automatically select or load this new pose yet; wire it into the arrest UI under EP-0036 without reusing the standing head placement. No runtime code or existing composition-pack entries were changed.

## Validation and previews

All seven bodies passed size/alpha checks, opaque central neck coverage, and unchanged body pixels below y=971 after head compositing. Overall pose and each neck join were visually inspected with the same portrait from the previous generation. In-game rendering was not run.

Requested previews and the check results are saved in `D:/DeepSpaceSaga/DSS-Images/Characters/KneelingCrew-20261117-v3/`: `overview.jpg`, `neck-joins.jpg`, each costume's `assembled.png`, and `validation.json`. Generation specifications are in `generation-prompts.json`. Bodies were generated with built-in image_gen; registration and previews were assembled deterministically with Python. The previous frontal version remains in the original DSS-Images folder; the game folder contains this revised side-turned pose.

## Proportion correction v3

The user rejected v2 as small-headed. This revision uniformly scales the body to 0.82 of v2 around the shared ground baseline, while keeping the original head size. Head/body relative size increases by approximately 22 percent. No pose redraw or portrait replacement was used. Collar-to-ground is now 586.3 px with an eye-to-chin/body ratio of 0.11598. Outer shoulder envelope/face-width is 2.51..2.61 for light costumes and 2.84 for heavy armor; these costume-specific style measurements are stored in each profile, not asserted as universal anatomy. The user accepted these proportions and the subsequent v4 neck repair on the first portrait. The game still needs pose selection integration.


## Repaired portrait assemblies

`assembled-portraits.json` maps four original portrait IDs to seven transparent, precomposed PNGs each under `assembled/<portrait-id>/<outfit>.png`. These 28 images are the corrected deliverables for prisoner presentation. Paths in the index are relative to this kneeling folder. They already include the head: do not overlay another portrait or apply the head transform again.

The first portrait (CHR-20261117-011947-JFG71U) uses the user-approved v4 assemblies. Three additional portraits (SFGMKX, KBE86V and D8IV97 suffixes) use the same body geometry and 68 px eye-to-chin scale, with a separate neck repair for each portrait/outfit. Repairs modify only a feathered region below the jaw; face pixels and all pixels outside the neck mask remain identical to their original composition. Generic headless bodies and profiles above are retained as source layers; their old clipping alone does not reproduce the repaired join.

Additional portrait previews and validation are in `D:/DeepSpaceSaga/DSS-Images/Characters/KneelingCrew-FourPortraits-20261117/`. Each has a seven-outfit neck sheet and full-pose overview. The asset copies are SHA256-verified. All 21 additional composites pass size, alpha and unchanged-face/outside-mask checks.

Runtime selection remains the separate EP-0036-US-0009-TK-0006 task. PNGs are covered by the existing content glob; the new JSON index is source metadata and must be included in output/publish when that integration consumes it. No runtime code or project packaging was changed here, and the game was not launched.
