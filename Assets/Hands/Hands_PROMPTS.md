# Prompt tạo ảnh tay (viewmodel) theo từng slot - GamePvP3D-HaiNam

Bản chỉnh sửa lần 5 - đổi hẳn sang quy trình **ảnh tham chiếu (reference) +
đổi pose**, thay vì viết full mô tả riêng cho từng ảnh như các bản trước:

1. Tạo **1 ảnh gốc (master reference)** cho mỗi slot - pose tay mở
   (`hand_open_N.png`) - mô tả đầy đủ nhân vật/dây quấn/bao tay/khung hình.
2. Từ ảnh gốc đó, dùng tính năng "ảnh tham chiếu" của công cụ tạo ảnh (giữ
   nguyên ảnh trước, chỉ đổi 1 chi tiết) để tạo `hand_fist_N.png` và
   `hand_holding_N.png` - prompt theo sau chỉ cần nói đổi pose, không cần lặp
   lại toàn bộ mô tả nhân vật/dây/bao tay nữa.

Cách này cho kết quả đồng nhất hơn hẳn so với việc viết 3 prompt độc lập cho
mỗi slot (bản lần 1-4) vì bản chất text-to-image luôn lệch ngẫu nhiên giữa các
lần gọi riêng biệt, còn ảnh tham chiếu thì giữ được y hệt nhân vật/ánh sáng/
khung hình giữa các pose.

**Sửa 1 chỗ so với bản gốc bạn đưa**: prompt `hand_holding_` giữ nguyên ý
"cầm cán kiếm" nhưng bỏ cụm dễ khiến AI vẽ **lỗ thủng xuyên bàn tay** (lỗi đã
gặp ở bản lần 4) - thay bằng mô tả khe hở nằm ở rìa mở của nắm tay, không phải
lỗ xuyên thịt.

Đặt tên file: `hand_open_N.png`, `hand_fist_N.png`, `hand_holding_N.png` (N = 0..3).

---

## BƯỚC 1 - Ảnh gốc (master reference), tạo riêng cho từng slot

### SLOT 0 — Nam, xanh dương (`hand_open_0.png`)
```
First-person game viewmodel of the player's own left hand in a relaxed open
pose. Clearly masculine adult hand with broad muscular proportions and thick
fingers, tan weathered skin, palm visible. Brown leather bracer engraved with
faint glowing blue runes on the wrist. Exactly two narrow deep-blue leather
straps wrapped horizontally and perfectly parallel around the mid-forearm
with one visible strip of bare skin between them; no crisscross wrapping, no
buckles, no studs. Painted fantasy game texture style, warm rim lighting,
first-person perspective looking down at the hand. Wrist positioned about
one-third from the left edge, hand extending to the right and downward,
forearm exiting straight down, right third of the image mostly empty.
Transparent background, single hand only, no objects, no text, no watermark.
This is the master reference image for all following poses in this session.
Keep the character, hand identity, proportions, skin tone, bracer, straps,
framing, perspective and lighting identical in all following images. Only
the hand pose may change.
```

### SLOT 1 — Nữ, đỏ (crimson) (`hand_open_1.png`)
```
First-person game viewmodel of the player's own left hand in a relaxed open
pose. Clearly feminine adult hand with slender toned proportions and lean
fingers, tan weathered skin, palm visible. Brown leather bracer engraved with
faint glowing red runes on the wrist. Exactly two narrow deep crimson-red
leather straps wrapped horizontally and perfectly parallel around the
mid-forearm with one visible strip of bare skin between them; no crisscross
wrapping, no buckles, no studs. Painted fantasy game texture style, warm rim
lighting, first-person perspective looking down at the hand. Wrist positioned
about one-third from the left edge, hand extending to the right and downward,
forearm exiting straight down, right third of the image mostly empty.
Transparent background, single hand only, no objects, no text, no watermark.
This is the master reference image for all following poses in this session.
Keep the character, hand identity, proportions, skin tone, bracer, straps,
framing, perspective and lighting identical in all following images. Only
the hand pose may change.
```

### SLOT 2 — Nam, xanh lá (`hand_open_2.png`)
```
First-person game viewmodel of the player's own left hand in a relaxed open
pose. Broad muscular hand with thick fingers, tan weathered skin, palm
visible. Brown leather bracer engraved with faint glowing green runes on the
wrist. Exactly two narrow deep forest green leather straps wrapped
horizontally and perfectly parallel around the mid-forearm with one visible
strip of bare skin between them. Painted fantasy game texture style, warm rim
lighting, first-person perspective looking down at the hand. Wrist positioned
about one-third from the left edge, hand extending to the right and downward,
forearm exiting straight down, right third of the image mostly empty.
Transparent background, single hand only. This is the master reference image
for all following poses in this session. Keep every detail identical in
subsequent images; only the hand pose may change.
```

### SLOT 3 — Nữ, vàng cam (amber) (`hand_open_3.png`)
```
First-person game viewmodel of the player's own left hand in a relaxed open
pose. Slender toned hand with lean fingers, tan weathered skin, palm visible.
Brown leather bracer engraved with faint glowing amber runes on the wrist.
Exactly two narrow warm amber orange leather straps wrapped horizontally and
perfectly parallel around the mid-forearm with one visible strip of bare
skin between them. Painted fantasy game texture style, warm rim lighting,
first-person perspective looking down at the hand. Wrist positioned about
one-third from the left edge, hand extending to the right and downward,
forearm exiting straight down, right third of the image mostly empty.
Transparent background, single hand only. This is the master reference image
for all following poses in this session. Keep every detail identical in
subsequent images; only the hand pose may change.
```

---

## BƯỚC 2 - Đổi pose từ ảnh gốc (dùng chung cho cả 4 slot)

Sau khi có `hand_open_N.png` ưng ý, mở phiên làm việc mới cho từng slot, đưa
ảnh đó làm ảnh tham chiếu, rồi lần lượt gõ 2 prompt sau (không cần đổi gì
theo slot, ảnh tham chiếu đã giữ sẵn màu/dáng tay).

### `hand_fist_N.png`
```
Use the previous image as the exact reference. Keep everything identical.
Only change the hand pose to a tightly closed fist.
```

### `hand_holding_N.png`
```
Use the previous image as the exact reference. Keep everything identical.
Only change the hand pose to a loose cylindrical grip as if holding a sword
hilt, while the hand remains empty.
```

---

## BƯỚC 3 - Tay cầm vũ khí (bản 6 - dùng 2 ẢNH THAM CHIẾU: tay + vật phẩm
thật), thay hẳn cách "chỉ đổi pose suông" ở bản 5 phía trên

Lý do đổi cách làm: cây cung/dao/kiếm phải khớp đúng hình dạng với icon vật
phẩm đã có trong `Assets\Items\` (`bow.png`, `dagger.png`, `sword.png`) - mô
tả bằng chữ không đảm bảo AI vẽ đúng hình dạng đó. Bản 6 đưa thẳng **2 ảnh
tham chiếu cùng lúc**: Image 1 = ảnh tay (`hand_open_N.png`), Image 2 = ảnh
vật phẩm thật lấy trong `Assets\Items\`, rồi ra lệnh AI CHỈ được sửa phần
ngón tay/cổ tay để ôm lấy vật phẩm có sẵn, cấm tạo tay mới/pose mới/lật
tay - tránh đúng các lỗi đã gặp ở bản 5 (cắt cụt cung, nền đen, sai giải
phẫu bàn tay).

Vẫn còn đúng như các bước trên: cả 3 loại đều tạo theo khung **tay trái**,
dùng `hand_open_N.png` của đúng slot đang làm làm Image 1. Cung giữ nguyên
tay trái khi hiển thị trong game; dao/kiếm sẽ được code tự lật gương sang
tay phải lúc hiển thị (xem `handHoldingDaggerImgMirrorBySlot` /
`handHoldingSwordImgMirrorBySlot` trong `GameAssets.vb`) - bạn không cần tự
lật hay đổi gì ở đây, cứ vẽ theo khung tay trái như mọi lần.

**Lưu ý khi chạy prompt dao/kiếm cho từng slot**: 2 prompt bên dưới có ghi
cứng cụm `"green straps"` (khớp ảnh tham chiếu SLOT 2 - xanh lá). Khi chạy
cho 3 slot còn lại, đổi cụm đó thành đúng màu dây của slot đang làm:
- SLOT 0: `"deep-blue leather straps"`
- SLOT 1: `"crimson-red leather straps"`
- SLOT 2: `"green straps"` (giữ nguyên)
- SLOT 3: `"amber orange leather straps"`

Đặt tên file kết quả: `hand_holding_bow_N.png`, `hand_holding_dagger_N.png`,
`hand_holding_sword_N.png` (N = 0..3), dùng cho tay trái.

### `hand_holding_bow_N.png` - Image 1 = `hand_open_N.png`, Image 2 = `Assets\Items\bow.png`
```
Image 1 is the LEFT hand reference.
Image 2 is the bow reference.

Use Image 1 as the primary reference for the LEFT hand and Image 2 as the exact reference for the bow.

Preserve the exact identity of the LEFT hand, including its skin tone, leather bracer, glowing rune bracelet, first-person game viewmodel perspective, fantasy painted texture, lighting, proportions, and overall art style from Image 1.

Preserve the exact shape, proportions, design, wood texture, bowstring, and all details of the bow from Image 2.

The bow must be held ONLY in the LEFT hand. Do not mirror, replace, or convert the hand into a right hand.

Re-pose the LEFT hand into a realistic archery grip. Do not simply insert the bow into the existing hand pose. Adjust the wrist, thumb, palm, and every finger so they naturally wrap around the center riser exactly as a real archer would hold a recurve bow before drawing the string. Maintain anatomically correct finger placement and a firm, natural grip.

Rotate the bow to approximately 35 degrees so it fits naturally within the frame. Do NOT change the canvas to square or portrait - keep the exact same landscape canvas dimensions and aspect ratio as Image 1, and keep the hand at the same position within the frame. Zoom the camera out slightly only as much as necessary, within this same landscape frame, so nearly the entire bow is visible. Do not crop the upper or lower limbs of the bow. Keep the bow large and close to the camera like a modern FPS game weapon viewmodel.

Blend the LEFT hand and the bow seamlessly into a single coherent game asset with perfectly matched lighting, shadows, perspective, texture quality, and fantasy painted style.

The background must be fully transparent (alpha channel), not black, not white, not any solid color. Export as a PNG image with a transparent background.
```

### `hand_holding_sword_N.png` - Image 1 = `hand_open_N.png`, Image 2 = `Assets\Items\sword.png`
```
Edit Image 1 only.

Image 1 is the BASE image and must remain the final image.
Image 2 is the sword asset to insert.

Do NOT create a new hand.
Do NOT generate a different pose.
Do NOT mirror the hand.
Do NOT replace the hand.
Do NOT convert it into a right hand.
Do NOT add a second hand.

The existing LEFT hand in Image 1 is the ONLY hand allowed in the final image.

Keep the forearm, wrist, leather bracer, green straps, skin tone, proportions, first-person game viewmodel perspective, lighting, and fantasy painted texture exactly as they appear in Image 1.

Insert the sword from Image 2 into the existing LEFT hand.

Modify ONLY the fingers, thumb, and wrist enough to create a natural one-handed sword grip. Keep the rest of the hand unchanged.

Fit the sword to the hand.
Do NOT fit the hand to the sword.

The sword must appear as if it is naturally held by the existing LEFT hand from Image 1.

Position the sword diagonally at approximately 35 degrees in a natural first-person combat stance.

Do NOT change the canvas to portrait or square. Keep the exact same landscape canvas dimensions, aspect ratio, and wrist position within the frame as Image 1 - do not zoom in, zoom out, crop, or recompose the frame. If the sword does not fully fit at its natural size, rotate or angle the sword further, or scale the sword down slightly, but never change the canvas shape or the hand's position in it. The entire sword, including the pommel, grip, guard, and blade, must remain fully visible inside this same frame without cropping.

Blend the sword seamlessly into Image 1 with perfectly matched lighting, shadows, perspective, texture quality, and fantasy painted style.

The background must be fully transparent (alpha channel), not black, not white, and not any solid color. Export as a PNG image with a transparent background.
```

### `hand_holding_dagger_N.png` - Image 1 = `hand_open_N.png`, Image 2 = `Assets\Items\dagger.png`
```
Edit Image 1 only.

Image 1 is the BASE image and must remain the final image.
Image 2 is the dagger asset to insert.

Do NOT create a new hand.
Do NOT generate a different pose.
Do NOT mirror the hand.
Do NOT replace the hand.
Do NOT convert it into a right hand.
Do NOT add a second hand.

The existing LEFT hand in Image 1 is the ONLY hand allowed in the final image.

Keep the forearm, wrist, leather bracer, green straps, skin tone, proportions, first-person game viewmodel perspective, lighting, and fantasy painted texture exactly as they appear in Image 1.

Insert the dagger from Image 2 into the existing LEFT hand.

Modify ONLY the fingers, thumb, and wrist enough to create a natural combat dagger grip. Keep the rest of the hand unchanged.

Fit the dagger to the hand.
Do NOT fit the hand to the dagger.

The dagger must appear as if it is naturally held by the existing LEFT hand from Image 1.

Position the dagger diagonally upward and slightly forward in a natural first-person combat stance. Do NOT change the canvas to square or portrait - keep the exact same landscape canvas dimensions and aspect ratio as Image 1, and keep the hand at the same position within the frame. Keep the dagger large and close to the camera like a modern FPS game weapon viewmodel. Ensure the entire dagger, including the pommel, handle, guard, and blade, remains fully visible inside this same frame without cropping.

Blend the dagger seamlessly into Image 1 with perfectly matched lighting, shadows, perspective, texture quality, and fantasy painted style.

The background must be fully transparent (alpha channel), not black, not white, and not any solid color. Export as a PNG image with a transparent background.
```

---

## BƯỚC 4 - Tay lúc đang KÉO CUNG (bow draw pose) - dây căng + mũi tên lắp sẵn

Dùng khi thêm hiệu ứng animation lúc giữ chuột trái kéo cung (`isDrawingBow`
trong code, xem `GameHud.vb`/`GameAssets.vb`). Cần 2 bộ ảnh mới, mỗi bộ 4
file theo slot (N = 0..3), **thêm vào** (không thay thế) các ảnh cầm cung
tĩnh đã có ở BƯỚC 3.

Bản prompt dưới đây đã test chạy tốt trên thực tế (khác nhịp với văn phong 2
ảnh tham chiếu ở BƯỚC 3 - đơn giản và dứt khoát hơn, ép rõ "edit only" ngay
từ đầu, ít mô tả tọa độ mơ hồ, cho AI tự suy luận hình học từ chính bố cục
ảnh gốc, chỉ ép đúng 1 quy tắc hướng).

### `hand_holding_bow_drawn_N.png` - Image 1 = `hand_holding_bow_N.png` (đúng slot), Image 2 = `Assets\Items\arrow.png`

```
Image 1 is the base image. Edit Image 1 only. Preserve the existing hand, bow, wrist, bracer, leather straps, lighting, painting style, perspective, composition, and canvas exactly as they are.

Image 2 is a reference only for the arrow's appearance. Copy only the metal arrowhead, wooden shaft, and feather fletching. Do not copy its position or orientation.

Add only two new elements:

• One fully drawn bowstring.
• One nocked arrow.

The bowstring must remain attached to the existing upper and lower limb tips. Pull only the center of the string backward until it touches the back of the arrow nock directly behind the hand, forming a natural drawn bow under tension.

Place the arrow on the bow exactly like a real nocked arrow. The arrow rests across the top of the bow hand.

Critical orientation:
- The nock and feathers stay beside the hand.
- The arrowhead points toward the upper bow limb, away from the hand.
- Never reverse the arrow direction.

Do not change or redraw the hand, fingers, bow grip, bow limbs, or any existing artwork. Only add the bowstring and the arrow.

Blend everything seamlessly with the existing fantasy painted style.

Keep the background fully transparent and export as a PNG with a true alpha channel.
```

### `hand_pulling_string_N.png` - Image 1 = `hand_open_N.png` (đúng slot, VE THEO KHUNG TAY TRAI - code tu lat guong sang tay phai), Image 2 = `Assets\Items\arrow.png`

```
Image 1 is the base image. Edit Image 1 only. Preserve the existing hand,
wrist, bracer, leather straps, skin tone, lighting, painting style,
perspective, composition, and canvas exactly as they are.

Image 2 is a reference only for the arrow's appearance. Copy only the
metal arrowhead, wooden shaft, and feather fletching. Do not copy its
position or orientation.

Add only one change: re-pose the fingers into a bowstring draw grip.

Curl the index, middle, and ring fingers into a hook, as if pinching a
taut bowstring near an arrow's nock and fletching, thumb relaxed and
tucked. Keep the wrist and forearm in the same position as Image 1.

Add a small visible arrow nock and a few feather fletchings tucked between
the curled fingers, matching the style of Image 2, sized small enough to
read as the rear tip of an arrow, not a full arrow.

Critical orientation:
- The nock and feathers point away from the fingers, toward the open side
  of the hand.
- Never show the arrowhead or shaft in this image — only the nock and
  fletching near the fingers.

Do not change or redraw the hand's identity, bracer, straps, or any
existing artwork. Only re-pose the fingers and add the nock/fletching.

Blend everything seamlessly with the existing fantasy painted style.

Keep the background fully transparent and export as a PNG with a true
alpha channel.
```

**Lưu ý:** `hand_pulling_string_N.png` vẽ theo đúng khung tay TRÁI (giống
`hand_open_N.png`) dù trong game nó hiển thị ở tay PHẢI - code tự động lật
gương lúc nạp (`handPullingStringImgMirrorBySlot` trong `GameAssets.vb`),
không cần tự lật tay khi tạo ảnh. Đặt tên file đúng theo slot rồi thả vào
`Assets\Hands\` là engine tự nhận, không cần sửa code thêm (xem
`LoadHandTextures()` - có fallback an toàn nếu thiếu file).
