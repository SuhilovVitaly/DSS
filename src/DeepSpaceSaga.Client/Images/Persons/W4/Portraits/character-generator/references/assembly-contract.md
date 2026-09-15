# W4/M4 character assembly contract

Read before generating, positioning or accepting a portrait component. These coordinates describe the current three shared costumes; they are asset interfaces, not universal human anatomical ratios.

## What the game actually assembles

The finished character consists of two independently stored layers:

1. **Portrait:** one coherent painted head, face, ears, jaw, hair and its own neck, on a transparent 1024×1024 canvas.
2. **Clothes:** one existing 1024×1024 costume, drawn over the portrait at the same origin and scale.

The outer canvas is the registration frame. The entire image maps to the render rectangle. There is no runtime face detection, bounding-box fitting, anatomical-anchor snapping, neck stretching or seam correction. Named historical anchors in portrait-style.json do not reposition these whole-head layers.

For output size R, each source point maps to (xR/1024, yR/1024). The workshop normally renders at R=512; DialoguePortraitComposer renders at R=300 for docking and other dialogues. A 20px error in a source component remains a 10px error in the workshop and about 5.86px in a dialogue.

Layer alpha follows source-over compositing: a_result = a_clothes + a_portrait × (1 − a_clothes). Transparent neck pixels inside an open collar remain holes. A neck painted outside the costume's silhouette remains visible; changing output resolution cannot fix either defect.

## Coordinate system and anatomical registration

Origin (0,0) is the upper left. X increases rightward, Y downward. Pixel indices are 0–1023; continuous canvas boundaries are 0–1024. Left/right below mean image left/right, not the character's anatomical left/right. Normalized coordinates are u=x/1024, v=y/1024.

| Feature | Target / acceptance band on the final canvas | Meaning |
|---|---|---|
| Canvas registration | Top left (0,0), bottom right boundary (1024,1024) | Keep the full square, including transparent lower space |
| Head/neck centerline | x512; anatomical head center within ±12px | Align to the costume center; do not recenter by asymmetric hair volume |
| Skull crown, excluding hair | Target (512,60); derived as chin_y − skull_height | Nominal chin 555 and height 495; accepted combinations imply crown y20–100 |
| Anatomical head width | Women 355–390px; men 350–410px | Exclude ears, hair and neck; nominal lateral limits x327–697 for a 370px head, x322–702 for 380px |
| Skull-to-chin height | 470–520px, target 495px | Vertical anatomical head dimension, excluding hair |
| Lowest chin | Target (512,555); y540–570 | Main vertical placement reference; do not move facial features separately |
| Hair silhouette | At least 12px top and side margin; target top y12–30 | Preserve the full hairstyle; large hair cannot substitute for adequate head size |
| Neck coverage rectangle | x430–594, y575–625 inclusive | Continuous opaque skin in the raw Portrait layer, including the portion hidden under clothes |
| Hidden neck endpoint | Centered near (512,675); end y660–690 | Skin extends below the visible front collar; lower unused canvas is transparent |

Eyes, eyebrows, nose and mouth are **not separate attachment parts** in W4/M4. Keep their natural relationships within the painted head. Do not impose obsolete modular face-part coordinates on a new identity.

## Costume attachment and occlusion landmarks

Measured directly from clothes-01.png, clothes-02.png and clothes-03.png using alpha >=128 to locate the rim. All three currently have the same boundary coordinates; M4 uses matching costume copies. Soft antialiased pixels surround these measurements, so use the actual PNGs for final acceptance rather than drawing a polygon through the samples.

| Costume guide | Pixel coordinate | Normalized (u,v) |
|---|---|---|
| Upper left collar tip | approximately (391,528) | (0.38184,0.51563) |
| Upper right collar tip | approximately (615,528) | (0.60059,0.51563) |
| Front rim under left neck | (430,584) | (0.41992,0.57031) |
| Front rim under right neck | (594,576) | (0.58008,0.56250) |
| Front collar center | (512,612) | (0.50000,0.59766) |
| Neck coverage top corners | (430,575), (594,575) | (0.41992,0.56152), (0.58008,0.56152) |
| Neck coverage bottom corners | (430,625), (594,625) | (0.41992,0.61035), (0.58008,0.61035) |
| Nominal hidden neck endpoint | (512,675) | (0.50000,0.65918) |

The collar is slightly asymmetric. Do not mirror one side to invent the other. The upper tips flank the jaw/neck region; they are not destinations for the chin or ears.

Horizontal slices of the collar (inclusive X ranges, alpha >=128):

| Y | Left opaque rim | Transparent interval between rims | Right opaque rim |
|---|---|---|---|
| 528 | 389–393 | 394–613 | 614–616 |
| 540 | 383–393 | 394–613 | 614–624 |
| 555 | 380–396 | 397–610 | 611–630 |
| 575 | 375–416 | 417–594 | 595–637 |
| 600 | 366–467 | 468–542 | 543–645 |
| 615 | Continuous costume from x349 through x662 | Closed at center | Same continuous costume |

The top opening may show natural background beside a tapered neck. Do not fill it with a rectangular slab of skin. The required lower neck rectangle and assembled central seam region below must be fully covered. Continue skin a few pixels behind the overlapping antialiased rim; never trim the neck to butt exactly against a thresholded costume edge.

At and below y575, inspect for exposed skin outside the outer costume silhouette. Neck sides should already have tapered naturally from behind the jaw. Do not fix broad rectangular necks by imposing a sudden horizontal cut at the collar tips: this creates square ledges higher up. Regenerate the incompatible neck contour. Hair hanging in front of the collar is also incompatible with the current two-layer draw order; use a suitable hairstyle instead of silently cutting it away.

## Fitting a different source resolution

Measure the source head center cx, anatomical crown yt, lowest chin yc, and head width ws. Select target chin Y in 540–570 (normally 555) and target head height H in 470–520 (normally 495):

~~~text
s  = H / (yc − yt)
tx = 512 − s × cx
ty = Y − s × yc
x_final = s × x_source + tx
y_final = s × y_source + ty
head_width_final = s × ws
~~~

Apply the **same s** to both axes and transform the whole source into a transparent 1024×1024 destination. Check that the resulting width, hair margins and neck coverage also pass. If one uniform transform cannot satisfy these constraints while retaining believable anatomy, regenerate the component. Do not stretch, relocate the chin independently or paste another person's neck.

Resizing an already correctly registered square N×N canvas to 1024 uses a scale of 1024/N. This is resolution normalization only. It does not make an unregistered head fit. Never apply a blanket 0.56 or 0.5 placement factor based only on source dimensions.

## Acceptance and precision

- **Raw component:** actual RGBA transparency, full 1024×1024 canvas, no baked checkerboard, background or clothing. Corners and unused exterior are transparent. Skin interiors should have alpha 255; the helper tolerates >=254 at sampled seam positions. Natural silhouette edges may be antialiased.
- **Neck overlap:** the raw portrait must cover the entire x430–594/y575–625 rectangle. The assembled character must have no transparent gaps throughout x440–580/y568–656 with each of the three costumes. The latter region includes pixels hidden by opaque clothing, so it does not replace the raw-layer check.
- **Anatomy and registration:** inspect head width without hair, crown-to-chin height, head center, chin Y, complete hair silhouette, short visible neck, natural under-chin shadow and no exposed skin ledges. Do not accept a small face inside large hair as a correctly sized head.
- **All combinations:** review all three costume composites on a dark neutral background, plus the seam at source resolution. Judge the character at 512×512 and 300×300 as well. One good costume combination is insufficient.
- **Helper limitations:** prepare.ps1 validates supplied landmark values rather than detecting them. It samples its transparency regions at 2px spacing and selected exterior points; passing it cannot prove that every seam pixel, the hairstyle or the whole background is correct. Inspect the full regions when a defect is suspected. It normalizes canvas resolution but does not calculate the fitting transform above.
- **Delivery:** accept the component only after geometry and appearance both pass. Save only the final head/neck PNG in the chosen Portraits folder. Costumes remain shared; do not alter them to accommodate one portrait. Delete temporary composites/previews after review; do not introduce Sources or Generated folders.

If costume art or runtime composition changes, remeasure the three source PNGs and update this contract before relying on these coordinates. Do not silently reuse an old calibration.
