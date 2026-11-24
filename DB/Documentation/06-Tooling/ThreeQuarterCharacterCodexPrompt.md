# W8 · Производство портретов для двух поз

Выполни заказ на указанное число персонажей. Для каждого создай одну игровую куклу с общей внешностью и тоном кожи, двумя ракурсами головы и пятью эмоциями в каждом. Готовый персонаж должен собираться с любым зарегистрированным костюмом своей позы без индивидуального смещения, растяжения или дорисовки тела.

## До начала партии

Работай из `D:/DeepSpaceSaga/DSS-Images/Characters/W8/Antropik` проектным Python. Источники геометрии — [манифест стойки](../../../../DSS-Images/Characters/W8/Antropik/three-quarter-right/pose.json) и [манифест пленного](../../../src/DeepSpaceSaga.Client/Images/Persons/W8/Clothes/Prisoner/Templates/pose.json). Цвет и порядок наложения слоёв описаны в [контракте skinTone](ThreeQuarterCharacterSkinTone.md).

| Поза | Холст тела | Ячейка | Вставка | Игровой атлас, RGBA8 sRGB |
|---|---|---|---|---|
| Три четверти вправо | 282×747 | 144×190 | (69, 0) | `threequarter.png`, 720×190 |
| Пленный | 416×747 | 136×144 | (48, 416) | `prisoner.png`, 680×144 |

1. Составь список заказанных внешностей и уникальных ID из шести символов `A–Z0–9`. Проверь каждый ID в партии, в клиентском `W8/Portraits`, в `pose.json → characters`, в `work/provenance.json` и в рабочей папке исходников. Существующий ID изменяй только при явно заказанной ревизии. В путях имя персонажа строчное.
2. Для каждого ID используй отдельный рабочий каталог `work/imagegen/<id>/`. Здесь сохраняй фактические запросы в `prompts.md`, оригиналы в `raw/standing/` и `raw/prisoner/`, подложки, принятые слои и маски. Зарезервируй по одному файлу на эмоцию в каждой папке исходников. Если эмоции получены листом, сохрани также оригинал листа.
3. Перед серией сверь клиентский `Clothes/ThreeQuarterOutfit/GREEN.png` с рабочим `work/outfit/GREEN.uncut.png`, который читает импортёр. Проверь доступность тел GREEN, GRAY и IVORY в обоих каталогах поз, направляющих и масок. Зафиксируй хеши общих ресурсов и ранее готовых атласов для проверки в конце партии.

## Сначала две нейтральные головы

[Лист девяти портретов](../../../../DSS-Images/Characters/W8/Antropik/prompts/references/portrait-style-nine-heads.png) задаёт рисовку. Рабочие мастера ER2BKF и NJIY63 в `work/masters/<id>/neutral.GREEN.png` задают игровой масштаб, палитру контура и освещение. Используй заданные черты, возраст, пол и причёску. Фото, если приложено, служит референсом личности. Для ImageGen передавай изображения вместе с запросом. К каждой из двух инструкций ниже добавь этот общий блок:

```text
W8 semi-realistic hand-painted game portrait of <appearance>, natural adult proportions.
Matte skin, diffuse upper-front-left light, warm shadow from jaw onto neck. Dark brown
line (#5D3726, deepest #3C2215), heavier on hair silhouette and upper lid, fading on
the lit cheek. Almond eyes with small catchlights, softly shaded nose and nostrils,
muted lips with a single mouth line. Hair in large masses, tapered strands and broad
muted highlights. Match the supplied style sheet. Calm expression, closed lips.
Transparent antialiased cutout. Keep clothing, backdrop, labels and accessories out
of the portrait. Avoid photographic pores, glossy 3D, exaggerated eyes and rim light.
```

**Стойка.** Вырежи `(40, 0, 242, 260)` из `work/masters/er2bkf/neutral.GREEN.png`, увеличь ×5 методом NEAREST на фоне `#E8E2D4`. В прямоугольниках правая и нижняя границы исключаются. Сохрани `underlay.png`.

```text
Input 1 is the standing underlay, input 2 the W8 style sheet. Draw one head facing
screen-right in three-quarter view. Match skull scale, ear and chin height against
the collar. Preserve both edges of the exposed neck. Its screen-left edge runs
vertically down to the front collar lip. Keep the contact shadow inside the neck.
Separate locks crossing the collar from the skin.
```

Привяжи голову один раз переносом и равномерным масштабом. На сборке neutral с текущим GREEN отметь нижнюю точку подбородка, мочку ближнего уха и одну и ту же точку края воротника, что у принятой сборки-эталона. Разница просвета `y воротника − y подбородка` — **до 2 игровых пикселей**. Сверь масштаб черепа и высоту уха. Проверь оба края кожи от челюсти до воротника. Контроль слева на текущем теле при y=102…105 — x=120 при альфе тела ≥128. На маске кожи при y=102…120 первый пиксель с альфой ≥124 в окне x=110…139 — x=121. Диапазон столбцов `neckSeam` определяет разделение слоёв. Ширину шеи определяет рисунок тела на каждой строке.

Если после приёмки высоты головы кожа выступает у ворота, исправь нижнюю кромку кожи, сохранив челюсть и боковые края общего тела. На полном холсте сделай маску `standing-neck-check.png` (L). Включи нижнюю шею, оба видимых края и полосу сразу за каждым из них. Исключи реальные перекрытия лица и прядей. Проверь маску поверх тела и зафиксируй до генерации эмоций.

**Пленный.** Собери текущий GREEN с первой ячейкой проверенного клиентского `prisoner.png` в точке из манифеста (например, KA4V7N). При отсутствии такого атласа `referenceHead` из шаблона уже размещён на полном холсте и накладывается в (0, 0). Подложка задаёт геометрию позы. Вырежи `(41, 412, 211, 581)`, увеличь ×5 методом NEAREST на том же фоне и сохрани `prisoner-underlay.png`.

```text
Input 1 is the prisoner underlay, input 2 the accepted standing neutral, input 3 the
W8 style sheet. Draw the same person with head bowed deeply down-left, crown visible.
Match camera, foreshortening, skull scale and placement at the collar and raised
shoulder. The near ear sits above and right of the near eye. The bridge partly hides
the far eye. The nose tip is left of the chin. Preserve the full near cheekbone,
jaw angle and rounded chin. Paint a short continuous patch of upper throat down to
the Adam’s apple, with a soft shadow below the chin. Let the patch overlap exposed
body skin. Keep nape locks attached and skin behind the ear clean. Trace this
character’s own silhouette. Leave collar, sleeve and shoulder on the body layer.
```

Сверь обе головы по носу, скулам, подбородку, уху, линии роста волос и приметам. По исходному рисунку пленного проследи скулу до угла челюсти, шею под подбородком и пряди за ухом. Зарегистрируй голову по черепу, уху, подбородку и верхнему срезу шеи. На светлом и тёмном фоне проверь собственный непрерывный контур, затем примерь его на три тела с `skinTone`. Тень заканчивается на открытой коже. У левого на экране плеча виден естественный переход в рукав. Ступенька под челюстью, прямая срезанная скула, прорези в волосах или отдельные пиксели на затылке требуют исправления нейтрального слоя. Сохрани принятый полный холст как `prisoner-neutral-fit.png`. Присваивать голове альфу чужого `portraitAlpha`, вычитать из неё силуэт тела или достраивать шею растяжением крайнего пикселя запрещено.

## Четыре изменения мимики на каждую позу

Начинай с принятого neutral **этой позы**. Общий порядок ячеек слева направо — `neutral, pain, anger, joy, pleasure`. Сохрани тот же источник света, положение головы, причёску, нос, ухо, подбородок и кожу у стыка. Можно получить четыре варианта одним листом на позу.

```text
Edit the accepted neutral of this pose. Lock hair, nose, ears, silhouette, pose,
neck, skin tone and lighting. Change brows, lids and mouth as coherent features.
Pain: inner brows raised and drawn together, squint, tense or parted lips.
Anger: brows lowered towards the bridge, narrowed eyes, tight lips.
Joy: raised cheeks, broad smile, upper teeth read as one warm light shape.
Pleasure: relaxed brows, half-closed lids, soft closed smile.
```

Перед переносом совмести рисунки по переносице, кончику носа, уху и челюсти. На полном холсте каждой позы зафиксируй две маски L в рабочем каталоге: `standing-expression-mask.png` и `standing-nose-lock.png`, для пленного — те же имена с префиксом `prisoner-`. Белым отметь соответственно области изменения и неподвижный нос. Замени каждую черту целиком вместе с её прежним контуром и уголками. Смягчай переход только на соседней коже. При несовместимом ракурсе переделай исходник эмоции.

При 1:1 и ×4 NEAREST проверь каждую сохранённую ячейку. Брови одиночные, веки сходятся в уголках и охватывают радужку с учётом ракурса. Переносица, кончик и ноздри читаются как один нос. У рта одна линия смыкания или одно открытие, пропорциональные губы и согласованные уголки. Улыбка, прищур и напряжение читаются и в наклоне головы. Альфа всей ячейки, RGBA носа и RGBA за пределами мимики должны совпасть с neutral побайтно.

## Подготовка слоёв и импорт

Убирай фон до уменьшения. Для исходника на цветной подложке проверь отсутствие цветной каймы на светлом и тёмном фоне. Уменьшай зарегистрированный рисунок фильтром BOX в premultiplied alpha, сохраняй PNG в **straight RGBA8 sRGB**. При работе с подложкой ×5 верни рисунок в её исходный прямоугольник. Оставь сглаживание внешнего контура и прозрачные поля. Волосы стойки доходят максимум до плеч. Если рисунок выходит за ячейку, исправь исходник до экспорта.

- **Стойка.** Сохрани `layers/<emotion>/{back-hair,face,front-hair}.png` размером 282×747. Задние волосы проходят за телом, передние пряди лежат поверх. Лицо и собственная тень под челюстью заканчиваются внутри общей шеи. Ткань и ворот принадлежат телу. Импортёр собирает мастер на GREEN, учитывает перекрытие задних волос и включает в портрет кожу тела выше шва.
- **Пленный.** Сохрани пять `prisoner-layers/<emotion>.png` размером 416×747. В слоях остаются голова, волосы и собственный участок верхней шеи. Непрозрачные пиксели головы закрывают верхний срез тела. Эталонная маска контролирует именно перекрытие среза. Остальной силуэт сверяется с собственным `prisoner-neutral-fit.png`.

Для серии сначала собери комплект в рабочем каталоге кандидата. Команды ниже выполняй в одной сессии PowerShell из `Antropik`, подставив ID и выбранный по щеке тон. Импорт одного персонажа выполняй последовательно: `add_character.py` блокирует на время записи общие рабочие манифесты. Даже с `--portraits-dir` он обновляет `characters`, `work/provenance.json` и рабочие мастера. Папка кандидата — место назначения игровых файлов.

```powershell
$py = 'D:/DeepSpaceSaga/DSS-Images/tools/modular-character-pilot/.venv/Scripts/python.exe'
$env:W8_CHARACTER = 'XXXXXX'
$env:W8_SKIN_TONE = '#C27D5A'  # подставь полутон данного персонажа
$idFolder = $env:W8_CHARACTER.ToLowerInvariant()
$workDir = Join-Path (Get-Location) "work/imagegen/$idFolder"
$candidates = Join-Path $workDir 'candidate'
& $py tools/add_character.py --id $env:W8_CHARACTER --layers "$workDir/layers" --skin-tone $env:W8_SKIN_TONE --portraits-dir $candidates
if ($LASTEXITCODE -ne 0) { throw 'Ошибка импорта стойки' }
```

### Экспорт пленного и проверка записанных атласов

Сохрани блок во временном файле `$workDir/check_portraits.py` и выполни из `Antropik` командой `& $py "$workDir/check_portraits.py"` с переменными текущего ID. Продолжай к приёмке только при коде завершения 0. Запускай с включёнными проверками `assert` (обычный режим Python). При ошибке исправь указанный слой того же ID и повтори сборку кандидата.

```python
import json, os, re, sys
from pathlib import Path
from tempfile import TemporaryDirectory

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path("tools").resolve()))
import dss3q as d

cid = os.environ["W8_CHARACTER"]
assert re.fullmatch(r"[A-Z0-9]{6}", cid)
src = Path("work/imagegen") / cid.lower()
dst = src / "candidate" / cid.lower()
tone = json.loads((dst / "settings.json").read_text(encoding="utf-8"))["skinTone"]
assert re.fullmatch(r"#[0-9A-F]{6}", tone) and tone == os.environ["W8_SKIN_TONE"].upper()
standing = d.Pose("three-quarter-right")
template = d.CLIENT_W8_ROOT / "Clothes/Prisoner/Templates"
pr = json.loads((template / "pose.json").read_text(encoding="utf-8"))
emotions = standing.emotions
assert emotions == pr["expressions"] == ["neutral", "pain", "anger", "joy", "pleasure"]

def read(path, mode, size):
    with Image.open(path) as im:
        assert im.mode == mode and im.size == tuple(size), path
        return np.array(im)

def check_cells(path, pose_name, canvas, origin, cell):
    x, y = origin
    w, h = cell
    pixels = read(path, "RGBA", (w * len(emotions), h))
    frames = np.split(pixels, len(emotions), axis=1)
    masks = [read(src / f"{pose_name}-{k}.png", "L", canvas)[y:y+h, x:x+w] > 0
             for k in ("expression-mask", "nose-lock")]
    editable, nose = masks
    assert editable.any() and nose.any()
    fixed = ~editable | nose
    for emotion, frame in zip(emotions, frames):
        assert np.array_equal(frame[..., 3], frames[0][..., 3]), (pose_name, emotion, "alpha")
        assert np.array_equal(frame[fixed], frames[0][fixed]), (pose_name, emotion, "face")
    assert len({f.tobytes() for f in frames}) == len(emotions), (pose_name, "duplicate emotion")
    return frames

green = read("work/outfit/GREEN.uncut.png", "RGBA", standing.canvas)
guard = read(src / "standing-neck-check.png", "L", standing.canvas) > 0
assert guard.any()
upper = np.zeros_like(green[..., 3])
for x, y in enumerate(standing.seam_y, standing.seam_x0):
    upper[:y, x] = green[:y, x, 3]
x, y = standing.origin
w, h = standing.cell
for emotion, frame in zip(emotions, check_cells(dst / "threequarter.png", "standing",
                          standing.canvas, standing.origin, standing.cell)):
    alpha = np.zeros_like(upper)
    alpha[y:y+h, x:x+w] = frame[..., 3]
    assert np.array_equal(alpha[guard], upper[guard]), (emotion, "standing neck")

p = pr["portrait"]
x, y = p["drawOrigin"]
w, h = p["cellSize"]
canvas = pr["canvas"]
area = np.s_[y:y+h, x:x+w]
mask = lambda key: read(template / pr["masks"][key], "L", canvas)
front, back = (pr["landmarks"][k] for k in ("upperNeckFront", "upperNeckBack"))
yy, xx = np.indices((canvas[1], canvas[0]))
lo, hi = sorted((front[0], back[0]))
assert hi > lo
line = front[1] + (xx-front[0]) * (back[1]-front[1]) / (back[0]-front[0])
seam = (mask("neckProtect") > 0) & (mask("portraitAlpha") == 255)
seam &= (xx >= lo) & (xx <= hi) & (np.abs(yy-line) <= 4)
assert seam[:, lo:hi+1].any(axis=0).all()
reference = read(template / pr["referenceBody"], "RGBA", canvas)
for outfit, rel in pr["outfits"].items():
    assert np.array_equal(read(template / rel, "RGBA", canvas)[seam], reference[seam]), outfit

neutral = read(src / "prisoner-neutral-fit.png", "RGBA", canvas)
cells = []
for emotion in emotions:
    head = read(src / "prisoner-layers" / f"{emotion}.png", "RGBA", canvas)
    assert head[..., 3].any() and np.count_nonzero(head[..., 3]) == np.count_nonzero(head[area][..., 3]), emotion
    if emotion == "neutral":
        assert np.array_equal(head, neutral), "accepted neutral"
    assert (head[..., 3][seam] == 255).all(), (emotion, "prisoner neck")
    cells.append(head[area])
with TemporaryDirectory(dir=dst) as tmp:
    candidate = Path(tmp) / p["file"]
    Image.fromarray(np.concatenate(cells, axis=1)).save(candidate)
    actual = check_cells(candidate, "prisoner", canvas, p["drawOrigin"], p["cellSize"])
    assert all(np.array_equal(a, b) for a, b in zip(actual, cells))
    candidate.replace(dst / p["file"])
assert {f.name for f in dst.iterdir()} == {"threequarter.png", "prisoner.png", "settings.json"}
```

## Приёмка и выдача

1. Перечитай **файлы кандидата**. Собери для каждой позы каждую эмоцию на каждом теле из её манифеста. При трёх костюмах получаются 30 сочетаний на ID. Положение ячейки и масштаб одинаковы на всех костюмах позы. Для стойки используй `Pose.portrait_underlay` и `dss3q.over`, для пленного — полное тело с ячейкой поверх. Примени [контракт тонирования](ThreeQuarterCharacterSkinTone.md). При базовом тоне собранный GREEN стойки должен совпадать с рабочим мастером по видимым каналам с допуском 1 уровень на сглаживании. Для GREEN/GRAY смена ткани сохраняет альфу и пиксели вне маски ткани.
2. Осмотри все выражения в игровом масштабе и крупно. Проверь пропорции, стык с обеих сторон, участок под подбородком, затылок и плечи. Сравни нейтральные головы между позами. Для проверки базовой сборки можно использовать `tools/review.py` с отдельной временной конфигурацией на текущий ID и явным путём к атласу кандидата. Его проверки GREEN/GRAY дополни IVORY и позой пленного. Собирай кадры один раз, для увеличения вырезай области из тех же сборок. На светлом и тёмном фоне должны сохраняться естественный контур и чистые промежутки между прядями. При дефекте исправь соответствующий исходник и повтори проверку этого ID.
3. После приёмки нового ID перенеси целиком проверенную папку кандидата в клиентский каталог на том же диске. Команда ниже завершается ошибкой, если целевая папка уже существует. После прерывания сверь рабочие исходники и запись в `imagegenCharacters` и продолжи тот же ID с незавершённого этапа. Если папка уже опубликована, проверь её содержимое и хеши. Ревизия существующего персонажа требует отдельного запроса и сохранения предыдущего комплекта.
4. Проверь клиентский путь, хеши трёх файлов и результат валидатора. Засчитывай персонажа только после успешной проверки обеих поз. По партии укажи число заказанных и готовых ID, причину каждого пропуска. Сверь сохранность ранее готовых ассетов. Временные матрицы держи в `D:/DeepSpaceSaga/artifacts/validation/<task>/<run>/` и удаляй после проверки. Итоговые картинки сохраняй и показывай по запросу, вне игровой папки. В `Portraits/<id>/` остаются ровно два атласа и один JSON.

```powershell
$clientRoot = 'D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/Images/Persons/W8/Portraits'
$sourceDir = Join-Path $candidates $idFolder
$gameDir = Join-Path $clientRoot $idFolder
$hashes = Get-ChildItem -LiteralPath $sourceDir -File | Get-FileHash
[IO.Directory]::Move($sourceDir, $gameDir)
foreach ($entry in $hashes) {
    $published = Join-Path $gameDir ([IO.Path]::GetFileName($entry.Path))
    if ((Get-FileHash -LiteralPath $published).Hash -ne $entry.Hash) { throw "Расхождение в $published" }
}
& $py tools/dss3q.py validate --pose three-quarter-right --character $env:W8_CHARACTER
if ($LASTEXITCODE -ne 0) { throw 'Ошибка проверки опубликованной стойки' }
```
