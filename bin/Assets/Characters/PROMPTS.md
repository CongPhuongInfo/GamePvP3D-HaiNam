# Prompt tạo ảnh nhân vật - GamePvP3D-HaiNam

Tổng hợp toàn bộ prompt đã dùng để tạo 24 ảnh nhân vật hiện có trong `Assets\Characters\`.
Giữ file này lại để khi cần tạo thêm slot mới, đổi trang phục, hoặc tạo lại ảnh bị lỗi thì
có sẵn prompt gốc mà sửa, không phải viết lại từ đầu.

## Quy ước đặt tên file (khớp với `LoadCharacterTexture()` trong `Form1.vb`)

Mỗi slot `N` (0-3) có tối đa 12 ảnh, thiếu ảnh nào thì code tự fallback (xem README.md):

| File | Hướng | Trạng thái |
|---|---|---|
| `character_N.png` | Trước | Đứng yên |
| `character_N_side.png` | Ngang (chỉ vẽ 1 bên, code tự lật gương bên kia) | Đứng yên |
| `character_N_back.png` | Sau | Đứng yên |
| `character_N_walk.png` | Trước | Đang bước |
| `character_N_side_walk.png` | Ngang (chỉ vẽ 1 bên) | Đang bước |
| `character_N_back_walk.png` | Sau | Đang bước |
| `character_N_crouch.png` | Trước | Đang ngồi |
| `character_N_side_crouch.png` | Ngang (chỉ vẽ 1 bên) | Đang ngồi |
| `character_N_back_crouch.png` | Sau | Đang ngồi |
| `character_N_jump.png` | Trước | Đang nhảy |
| `character_N_side_jump.png` | Ngang (chỉ vẽ 1 bên) | Đang nhảy |
| `character_N_back_jump.png` | Sau | Đang nhảy |

## Bảng phân slot

| Slot | Giới tính | Màu định danh (`SlotColor`) | Đặc điểm tóc |
|---|---|---|---|
| 0 | Nam | Xanh dương | Tóc rối ngắn, đội băng trán |
| 1 | Nữ | Đỏ (crimson) | Tóc tết dài buộc đuôi ngựa |
| 2 | Nam | Xanh lá | Tóc dài buộc sau |
| 3 | Nữ | Vàng cam (amber) | Tóc ngắn rối màu hạt dẻ |

Khung sườn chung cho mọi prompt: *"wild jungle hunter / huntress"*, da rám nắng phong
trần, dây da bộ tộc (loincloth/wrap top), bao tay da chạm khắc rune phát sáng theo màu
slot, tay không cầm vũ khí, nền trong suốt hoàn toàn, không bóng đổ, phong cách tranh vẽ
game fantasy, ánh sáng viền ấm (warm rim lighting).

---

## SLOT 0 — Nam, xanh dương

### `character_0.png` (trước, đứng yên)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare chest, tribal leather loincloth and leg wraps with deep blue accent
dye, leather bracers with carved rune detail glowing faint blue matching a
fantasy adventurer aesthetic, tousled dark hair, tan weathered skin,
standing in a relaxed neutral front-facing pose, empty hands at the sides,
no weapon, no held item, full body visible from head to feet, viewed
straight-on from the front, painted fantasy game texture style, warm rim
lighting, isolated on a fully transparent background, no ground, no shadow
cast on ground, no other objects, no text, no watermark, single character
only, centered composition, portrait orientation
```

### `character_0_side.png` (ngang, đứng yên)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare chest, tribal leather loincloth and leg wraps with deep blue accent
dye, leather bracers with carved rune detail glowing faint blue matching a
fantasy adventurer aesthetic, tousled dark hair, tan weathered skin,
standing in a relaxed neutral pose, empty hands at the sides, no weapon, no
held item, full body visible from head to feet, viewed from a strict
90-degree side profile facing right, one arm and one leg overlapping the
other in profile, painted fantasy game texture style, warm rim lighting,
isolated on a fully transparent background, no ground, no shadow cast on
ground, no other objects, no text, no watermark, single character only,
centered composition, portrait orientation
```

### `character_0_back.png` (sau, đứng yên)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare back, tribal leather loincloth and leg wraps with deep blue accent dye,
leather bracers with carved rune detail glowing faint blue matching a
fantasy adventurer aesthetic, tousled dark hair, tan weathered skin,
standing in a relaxed neutral pose, empty hands at the sides, no weapon, no
held item, full body visible from head to feet, viewed straight-on from
directly behind, back of head and back muscles clearly visible, painted
fantasy game texture style, warm rim lighting, isolated on a fully
transparent background, no ground, no shadow cast on ground, no other
objects, no text, no watermark, single character only, centered
composition, portrait orientation
```

### `character_0_walk.png` (trước, đang bước)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare chest, tribal leather loincloth and leg wraps with deep blue accent
dye, leather bracers with carved rune detail glowing faint blue matching a
fantasy adventurer aesthetic, tousled dark hair, tan weathered skin, caught
mid-stride walking pose, one leg stepping forward with knee bent and the
other leg trailing back, arms swinging in natural counter-motion opposite
the legs, empty hands, no weapon, no held item, full body visible from head
to feet, viewed straight-on from the front, painted fantasy game texture
style, warm rim lighting, isolated on a fully transparent background, no
ground, no shadow cast on ground, no other objects, no text, no watermark,
single character only, centered composition, portrait orientation
```

### `character_0_side_walk.png` (ngang, đang bước)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare chest, tribal leather loincloth and leg wraps with deep blue accent
dye, leather bracers with carved rune detail glowing faint blue matching a
fantasy adventurer aesthetic, tousled dark hair, tan weathered skin, caught
mid-stride walking pose with a clear long stride, front leg bent and
planted, back leg extended and trailing, front arm swung back and back arm
swung forward in natural counter-motion, empty hands, no weapon, no held
item, full body visible from head to feet, viewed from a strict 90-degree
side profile facing right, painted fantasy game texture style, warm rim
lighting, isolated on a fully transparent background, no ground, no shadow
cast on ground, no other objects, no text, no watermark, single character
only, centered composition, portrait orientation
```

### `character_0_back_walk.png` (sau, đang bước)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare back, tribal leather loincloth and leg wraps with deep blue accent dye,
leather bracers with carved rune detail glowing faint blue matching a
fantasy adventurer aesthetic, tousled dark hair, tan weathered skin, caught
mid-stride walking pose, one leg stepping forward with knee bent and the
other leg trailing back, arms swinging in natural counter-motion opposite
the legs, empty hands, no weapon, no held item, full body visible from head
to feet, viewed straight-on from directly behind, back of head and back
muscles clearly visible, painted fantasy game texture style, warm rim
lighting, isolated on a fully transparent background, no ground, no shadow
cast on ground, no other objects, no text, no watermark, single character
only, centered composition, portrait orientation
```

---

## SLOT 1 — Nữ, đỏ

### `character_1.png` (trước, đứng yên)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, deep
crimson red accent dye, leather bracers with carved rune detail glowing
faint red matching a fantasy adventurer aesthetic, long dark hair in a
braided ponytail, tan weathered skin, standing in a relaxed neutral
front-facing pose, empty hands at the sides, no weapon, no held item, full
body visible from head to feet, viewed straight-on from the front, painted
fantasy game texture style, warm rim lighting, isolated on a fully
transparent background, no ground, no shadow cast on ground, no other
objects, no text, no watermark, single character only, centered
composition, portrait orientation
```

### `character_1_side.png` (ngang, đứng yên)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, deep
crimson red accent dye, leather bracers with carved rune detail glowing
faint red matching a fantasy adventurer aesthetic, long dark hair in a
braided ponytail, tan weathered skin, standing in a relaxed neutral pose,
empty hands at the sides, no weapon, no held item, full body visible from
head to feet, viewed from a strict 90-degree side profile facing right, one
arm and one leg overlapping the other in profile, painted fantasy game
texture style, warm rim lighting, isolated on a fully transparent
background, no ground, no shadow cast on ground, no other objects, no text,
no watermark, single character only, centered composition, portrait
orientation
```

### `character_1_back.png` (sau, đứng yên)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, deep
crimson red accent dye, leather bracers with carved rune detail glowing
faint red matching a fantasy adventurer aesthetic, long dark hair in a
braided ponytail, tan weathered skin, standing in a relaxed neutral pose,
empty hands at the sides, no weapon, no held item, full body visible from
head to feet, viewed straight-on from directly behind, back of head and
braid clearly visible, painted fantasy game texture style, warm rim
lighting, isolated on a fully transparent background, no ground, no shadow
cast on ground, no other objects, no text, no watermark, single character
only, centered composition, portrait orientation
```

### `character_1_walk.png` (trước, đang bước)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, deep
crimson red accent dye, leather bracers with carved rune detail glowing
faint red matching a fantasy adventurer aesthetic, long dark hair in a
braided ponytail, tan weathered skin, caught mid-stride walking pose, one
leg stepping forward with knee bent and the other leg trailing back, arms
swinging in natural counter-motion opposite the legs, empty hands, no
weapon, no held item, full body visible from head to feet, viewed
straight-on from the front, painted fantasy game texture style, warm rim
lighting, isolated on a fully transparent background, no ground, no shadow
cast on ground, no other objects, no text, no watermark, single character
only, centered composition, portrait orientation
```

### `character_1_side_walk.png` (ngang, đang bước)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, deep
crimson red accent dye, leather bracers with carved rune detail glowing
faint red matching a fantasy adventurer aesthetic, long dark hair in a
braided ponytail swinging slightly with the motion, tan weathered skin,
caught mid-stride walking pose with a clear long stride, front leg bent and
planted, back leg extended and trailing, front arm swung back and back arm
swung forward in natural counter-motion, empty hands, no weapon, no held
item, full body visible from head to feet, viewed from a strict 90-degree
side profile facing right, painted fantasy game texture style, warm rim
lighting, isolated on a fully transparent background, no ground, no shadow
cast on ground, no other objects, no text, no watermark, single character
only, centered composition, portrait orientation
```

### `character_1_back_walk.png` (sau, đang bước)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, deep
crimson red accent dye, leather bracers with carved rune detail glowing
faint red matching a fantasy adventurer aesthetic, long dark hair in a
braided ponytail, tan weathered skin, caught mid-stride walking pose, one
leg stepping forward with knee bent and the other leg trailing back, arms
swinging in natural counter-motion opposite the legs, empty hands, no
weapon, no held item, full body visible from head to feet, viewed
straight-on from directly behind, back of head and braid clearly visible,
painted fantasy game texture style, warm rim lighting, isolated on a fully
transparent background, no ground, no shadow cast on ground, no other
objects, no text, no watermark, single character only, centered
composition, portrait orientation
```

---

## SLOT 2 — Nam, xanh lá

### `character_2.png` (trước, đứng yên)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare chest, tribal leather loincloth and leg wraps with deep forest green
accent dye, leather bracers with carved rune detail glowing faint green
matching a fantasy adventurer aesthetic, long dark hair tied back, tan
weathered skin, standing in a relaxed neutral front-facing pose, empty
hands at the sides, no weapon, no held item, full body visible from head to
feet, viewed straight-on from the front, painted fantasy game texture
style, warm rim lighting, isolated on a fully transparent background, no
ground, no shadow cast on ground, no other objects, no text, no watermark,
single character only, centered composition, portrait orientation
```

### `character_2_side.png` (ngang, đứng yên)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare chest, tribal leather loincloth and leg wraps with deep forest green
accent dye, leather bracers with carved rune detail glowing faint green
matching a fantasy adventurer aesthetic, long dark hair tied back, tan
weathered skin, standing in a relaxed neutral pose, empty hands at the
sides, no weapon, no held item, full body visible from head to feet, viewed
from a strict 90-degree side profile facing right, one arm and one leg
overlapping the other in profile, painted fantasy game texture style, warm
rim lighting, isolated on a fully transparent background, no ground, no
shadow cast on ground, no other objects, no text, no watermark, single
character only, centered composition, portrait orientation
```

### `character_2_back.png` (sau, đứng yên)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare back, tribal leather loincloth and leg wraps with deep forest green
accent dye, leather bracers with carved rune detail glowing faint green
matching a fantasy adventurer aesthetic, long dark hair tied back, tan
weathered skin, standing in a relaxed neutral pose, empty hands at the
sides, no weapon, no held item, full body visible from head to feet, viewed
straight-on from directly behind, back of head and back muscles clearly
visible, painted fantasy game texture style, warm rim lighting, isolated on
a fully transparent background, no ground, no shadow cast on ground, no
other objects, no text, no watermark, single character only, centered
composition, portrait orientation
```

### `character_2_walk.png` (trước, đang bước)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare chest, tribal leather loincloth and leg wraps with deep forest green
accent dye, leather bracers with carved rune detail glowing faint green
matching a fantasy adventurer aesthetic, long dark hair tied back, tan
weathered skin, caught mid-stride walking pose, one leg stepping forward
with knee bent and the other leg trailing back, arms swinging in natural
counter-motion opposite the legs, empty hands, no weapon, no held item,
full body visible from head to feet, viewed straight-on from the front,
painted fantasy game texture style, warm rim lighting, isolated on a fully
transparent background, no ground, no shadow cast on ground, no other
objects, no text, no watermark, single character only, centered
composition, portrait orientation
```

### `character_2_side_walk.png` (ngang, đang bước)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare chest, tribal leather loincloth and leg wraps with deep forest green
accent dye, leather bracers with carved rune detail glowing faint green
matching a fantasy adventurer aesthetic, long dark hair tied back swinging
slightly with the motion, tan weathered skin, caught mid-stride walking
pose with a clear long stride, front leg bent and planted, back leg
extended and trailing, front arm swung back and back arm swung forward in
natural counter-motion, empty hands, no weapon, no held item, full body
visible from head to feet, viewed from a strict 90-degree side profile
facing right, painted fantasy game texture style, warm rim lighting,
isolated on a fully transparent background, no ground, no shadow cast on
ground, no other objects, no text, no watermark, single character only,
centered composition, portrait orientation
```

### `character_2_back_walk.png` (sau, đang bước)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare back, tribal leather loincloth and leg wraps with deep forest green
accent dye, leather bracers with carved rune detail glowing faint green
matching a fantasy adventurer aesthetic, long dark hair tied back, tan
weathered skin, caught mid-stride walking pose, one leg stepping forward
with knee bent and the other leg trailing back, arms swinging in natural
counter-motion opposite the legs, empty hands, no weapon, no held item,
full body visible from head to feet, viewed straight-on from directly
behind, back of head and back muscles clearly visible, painted fantasy game
texture style, warm rim lighting, isolated on a fully transparent
background, no ground, no shadow cast on ground, no other objects, no text,
no watermark, single character only, centered composition, portrait
orientation
```

---

## SLOT 3 — Nữ, vàng cam

### `character_3.png` (trước, đứng yên)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, warm
amber orange accent dye, leather bracers with carved rune detail glowing
faint amber matching a fantasy adventurer aesthetic, short tousled
sandy-brown hair, tan weathered skin, standing in a relaxed neutral
front-facing pose, empty hands at the sides, no weapon, no held item, full
body visible from head to feet, viewed straight-on from the front, painted
fantasy game texture style, warm rim lighting, isolated on a fully
transparent background, no ground, no shadow cast on ground, no other
objects, no text, no watermark, single character only, centered
composition, portrait orientation
```

### `character_3_side.png` (ngang, đứng yên)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, warm
amber orange accent dye, leather bracers with carved rune detail glowing
faint amber matching a fantasy adventurer aesthetic, short tousled
sandy-brown hair, tan weathered skin, standing in a relaxed neutral pose,
empty hands at the sides, no weapon, no held item, full body visible from
head to feet, viewed from a strict 90-degree side profile facing right, one
arm and one leg overlapping the other in profile, painted fantasy game
texture style, warm rim lighting, isolated on a fully transparent
background, no ground, no shadow cast on ground, no other objects, no text,
no watermark, single character only, centered composition, portrait
orientation
```

### `character_3_back.png` (sau, đứng yên)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, warm
amber orange accent dye, leather bracers with carved rune detail glowing
faint amber matching a fantasy adventurer aesthetic, short tousled
sandy-brown hair, tan weathered skin, standing in a relaxed neutral pose,
empty hands at the sides, no weapon, no held item, full body visible from
head to feet, viewed straight-on from directly behind, back of head clearly
visible, painted fantasy game texture style, warm rim lighting, isolated on
a fully transparent background, no ground, no shadow cast on ground, no
other objects, no text, no watermark, single character only, centered
composition, portrait orientation
```

### `character_3_walk.png` (trước, đang bước)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, warm
amber orange accent dye, leather bracers with carved rune detail glowing
faint amber matching a fantasy adventurer aesthetic, short tousled
sandy-brown hair, tan weathered skin, caught mid-stride walking pose, one
leg stepping forward with knee bent and the other leg trailing back, arms
swinging in natural counter-motion opposite the legs, empty hands, no
weapon, no held item, full body visible from head to feet, viewed
straight-on from the front, painted fantasy game texture style, warm rim
lighting, isolated on a fully transparent background, no ground, no shadow
cast on ground, no other objects, no text, no watermark, single character
only, centered composition, portrait orientation
```

### `character_3_side_walk.png` (ngang, đang bước)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, warm
amber orange accent dye, leather bracers with carved rune detail glowing
faint amber matching a fantasy adventurer aesthetic, short tousled
sandy-brown hair, tan weathered skin, caught mid-stride walking pose with a
clear long stride, front leg bent and planted, back leg extended and
trailing, front arm swung back and back arm swung forward in natural
counter-motion, empty hands, no weapon, no held item, full body visible
from head to feet, viewed from a strict 90-degree side profile facing
right, painted fantasy game texture style, warm rim lighting, isolated on a
fully transparent background, no ground, no shadow cast on ground, no other
objects, no text, no watermark, single character only, centered
composition, portrait orientation
```

### `character_3_back_walk.png` (sau, đang bước)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, warm
amber orange accent dye, leather bracers with carved rune detail glowing
faint amber matching a fantasy adventurer aesthetic, short tousled
sandy-brown hair, tan weathered skin, caught mid-stride walking pose, one
leg stepping forward with knee bent and the other leg trailing back, arms
swinging in natural counter-motion opposite the legs, empty hands, no
weapon, no held item, full body visible from head to feet, viewed
straight-on from directly behind, back of head clearly visible, painted
fantasy game texture style, warm rim lighting, isolated on a fully
transparent background, no ground, no shadow cast on ground, no other
objects, no text, no watermark, single character only, centered
composition, portrait orientation
```

---

---

## Tư thế NGỒI (crouch) — dùng cho `character_N_crouch.png` / `_side_crouch.png` / `_back_crouch.png`

Bổ sung khi nâng cấp: hiện `crouchAmount`/`rp.Crouch` (phím Ctrl/C) chỉ ảnh hưởng
va chạm và camera cục bộ, người chơi khác nhìn thấy vẫn đứng thẳng. 3 ảnh mới
dưới đây (mỗi slot) cho phép `PickDirectionalTexture()` đổi sang đúng tư thế ngồi
khi `rp.Crouch >= REMOTE_CROUCH_POSE_THRESHOLD` (0.5). Không có bản `_walk` cho
tư thế ngồi (ngồi thì không đổi dáng bước, xem README) nên mỗi slot chỉ cần đúng
3 ảnh: trước / ngang / sau, cùng khung sườn mô tả nhân vật như các ảnh đứng yên,
chỉ đổi phần mô tả tư thế.

### SLOT 0 — xanh dương

### `character_0_crouch.png` (trước, ngồi)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare chest, tribal leather loincloth and leg wraps with deep blue accent
dye, leather bracers with carved rune detail glowing faint blue matching a
fantasy adventurer aesthetic, tousled dark hair, tan weathered skin,
crouching low in a stealthy sneaking pose, knees bent deeply, hips dropped
low, weight centered and balanced, back kept upright, one forearm resting
near a bent knee for balance, empty hands, no weapon, no held item, full
body visible from head to feet, viewed straight-on from the front, painted
fantasy game texture style, warm rim lighting, isolated on a fully
transparent background, no ground, no shadow cast on ground, no other
objects, no text, no watermark, single character only, centered composition,
portrait orientation
```

### `character_0_side_crouch.png` (ngang, ngồi)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare chest, tribal leather loincloth and leg wraps with deep blue accent
dye, leather bracers with carved rune detail glowing faint blue matching a
fantasy adventurer aesthetic, tousled dark hair, tan weathered skin,
crouching low in a stealthy sneaking pose, knees bent deeply, hips dropped
low, weight centered and balanced, back kept upright, one forearm resting
near a bent knee for balance, empty hands, no weapon, no held item, full
body visible from head to feet, viewed from a strict 90-degree side profile
facing right, painted fantasy game texture style, warm rim lighting,
isolated on a fully transparent background, no ground, no shadow cast on
ground, no other objects, no text, no watermark, single character only,
centered composition, portrait orientation
```

### `character_0_back_crouch.png` (sau, ngồi)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare chest, tribal leather loincloth and leg wraps with deep blue accent
dye, leather bracers with carved rune detail glowing faint blue matching a
fantasy adventurer aesthetic, tousled dark hair, tan weathered skin,
crouching low in a stealthy sneaking pose, knees bent deeply, hips dropped
low, weight centered and balanced, back kept upright, one forearm resting
near a bent knee for balance, empty hands, no weapon, no held item, full
body visible from head to feet, viewed straight-on from directly behind,
back of head and back muscles clearly visible, painted fantasy game texture
style, warm rim lighting, isolated on a fully transparent background, no
ground, no shadow cast on ground, no other objects, no text, no watermark,
single character only, centered composition, portrait orientation
```

---

### SLOT 1 — đỏ

### `character_1_crouch.png` (trước, ngồi)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, deep
crimson red accent dye, leather bracers with carved rune detail glowing
faint red matching a fantasy adventurer aesthetic, long dark hair in a
braided ponytail, tan weathered skin, crouching low in a stealthy sneaking
pose, knees bent deeply, hips dropped low, weight centered and balanced,
back kept upright, one forearm resting near a bent knee for balance, empty
hands, no weapon, no held item, full body visible from head to feet, viewed
straight-on from the front, painted fantasy game texture style, warm rim
lighting, isolated on a fully transparent background, no ground, no shadow
cast on ground, no other objects, no text, no watermark, single character
only, centered composition, portrait orientation
```

### `character_1_side_crouch.png` (ngang, ngồi)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, deep
crimson red accent dye, leather bracers with carved rune detail glowing
faint red matching a fantasy adventurer aesthetic, long dark hair in a
braided ponytail, tan weathered skin, crouching low in a stealthy sneaking
pose, knees bent deeply, hips dropped low, weight centered and balanced,
back kept upright, one forearm resting near a bent knee for balance, empty
hands, no weapon, no held item, full body visible from head to feet, viewed
from a strict 90-degree side profile facing right, painted fantasy game
texture style, warm rim lighting, isolated on a fully transparent
background, no ground, no shadow cast on ground, no other objects, no text,
no watermark, single character only, centered composition, portrait
orientation
```

### `character_1_back_crouch.png` (sau, ngồi)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, deep
crimson red accent dye, leather bracers with carved rune detail glowing
faint red matching a fantasy adventurer aesthetic, long dark hair in a
braided ponytail, tan weathered skin, crouching low in a stealthy sneaking
pose, knees bent deeply, hips dropped low, weight centered and balanced,
back kept upright, one forearm resting near a bent knee for balance, empty
hands, no weapon, no held item, full body visible from head to feet, viewed
straight-on from directly behind, back of head and braid clearly visible,
painted fantasy game texture style, warm rim lighting, isolated on a fully
transparent background, no ground, no shadow cast on ground, no other
objects, no text, no watermark, single character only, centered composition,
portrait orientation
```

---

### SLOT 2 — xanh lá

### `character_2_crouch.png` (trước, ngồi)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare chest, tribal leather loincloth and leg wraps with deep forest green
accent dye, leather bracers with carved rune detail glowing faint green
matching a fantasy adventurer aesthetic, long dark hair tied back, tan
weathered skin, crouching low in a stealthy sneaking pose, knees bent
deeply, hips dropped low, weight centered and balanced, back kept upright,
one forearm resting near a bent knee for balance, empty hands, no weapon, no
held item, full body visible from head to feet, viewed straight-on from the
front, painted fantasy game texture style, warm rim lighting, isolated on a
fully transparent background, no ground, no shadow cast on ground, no other
objects, no text, no watermark, single character only, centered composition,
portrait orientation
```

### `character_2_side_crouch.png` (ngang, ngồi)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare chest, tribal leather loincloth and leg wraps with deep forest green
accent dye, leather bracers with carved rune detail glowing faint green
matching a fantasy adventurer aesthetic, long dark hair tied back, tan
weathered skin, crouching low in a stealthy sneaking pose, knees bent
deeply, hips dropped low, weight centered and balanced, back kept upright,
one forearm resting near a bent knee for balance, empty hands, no weapon, no
held item, full body visible from head to feet, viewed from a strict
90-degree side profile facing right, painted fantasy game texture style,
warm rim lighting, isolated on a fully transparent background, no ground, no
shadow cast on ground, no other objects, no text, no watermark, single
character only, centered composition, portrait orientation
```

### `character_2_back_crouch.png` (sau, ngồi)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare chest, tribal leather loincloth and leg wraps with deep forest green
accent dye, leather bracers with carved rune detail glowing faint green
matching a fantasy adventurer aesthetic, long dark hair tied back, tan
weathered skin, crouching low in a stealthy sneaking pose, knees bent
deeply, hips dropped low, weight centered and balanced, back kept upright,
one forearm resting near a bent knee for balance, empty hands, no weapon, no
held item, full body visible from head to feet, viewed straight-on from
directly behind, back of head and back muscles clearly visible, painted
fantasy game texture style, warm rim lighting, isolated on a fully
transparent background, no ground, no shadow cast on ground, no other
objects, no text, no watermark, single character only, centered composition,
portrait orientation
```

---

### SLOT 3 — vàng cam

### `character_3_crouch.png` (trước, ngồi)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, warm
amber orange accent dye, leather bracers with carved rune detail glowing
faint amber matching a fantasy adventurer aesthetic, short tousled
sandy-brown hair, tan weathered skin, crouching low in a stealthy sneaking
pose, knees bent deeply, hips dropped low, weight centered and balanced,
back kept upright, one forearm resting near a bent knee for balance, empty
hands, no weapon, no held item, full body visible from head to feet, viewed
straight-on from the front, painted fantasy game texture style, warm rim
lighting, isolated on a fully transparent background, no ground, no shadow
cast on ground, no other objects, no text, no watermark, single character
only, centered composition, portrait orientation
```

### `character_3_side_crouch.png` (ngang, ngồi)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, warm
amber orange accent dye, leather bracers with carved rune detail glowing
faint amber matching a fantasy adventurer aesthetic, short tousled
sandy-brown hair, tan weathered skin, crouching low in a stealthy sneaking
pose, knees bent deeply, hips dropped low, weight centered and balanced,
back kept upright, one forearm resting near a bent knee for balance, empty
hands, no weapon, no held item, full body visible from head to feet, viewed
from a strict 90-degree side profile facing right, painted fantasy game
texture style, warm rim lighting, isolated on a fully transparent
background, no ground, no shadow cast on ground, no other objects, no text,
no watermark, single character only, centered composition, portrait
orientation
```

### `character_3_back_crouch.png` (sau, ngồi)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, warm
amber orange accent dye, leather bracers with carved rune detail glowing
faint amber matching a fantasy adventurer aesthetic, short tousled
sandy-brown hair, tan weathered skin, crouching low in a stealthy sneaking
pose, knees bent deeply, hips dropped low, weight centered and balanced,
back kept upright, one forearm resting near a bent knee for balance, empty
hands, no weapon, no held item, full body visible from head to feet, viewed
straight-on from directly behind, back of head clearly visible, painted
fantasy game texture style, warm rim lighting, isolated on a fully
transparent background, no ground, no shadow cast on ground, no other
objects, no text, no watermark, single character only, centered composition,
portrait orientation
```

---

---

## Tư thế NHẢY (jump) — dùng cho `character_N_jump.png` / `_side_jump.png` / `_back_jump.png`

Bổ sung khi nâng cấp: `playerZ`/`rp.Z` (chuột phải để nhảy) trước đây chỉ nâng vị
trí sprite người chơi khác lên (`footShift`) chứ không đổi dáng người - vẫn đứng
thẳng lơ lửng giữa không trung. 3 ảnh mới dưới đây (mỗi slot) cho phép
`PickDirectionalTexture()` đổi sang đúng tư thế nhảy khi `rp.Z >=
REMOTE_JUMP_POSE_THRESHOLD` (0.05) - ưu tiên cao nhất, hơn cả tư thế ngồi và đi
bộ, vì một người đang bay giữa không trung thì không thể đồng thời ngồi/bước chân
một cách hợp lý về mặt hình ảnh. Không có bản `_walk` cho tư thế nhảy (tương tự
ngồi) nên mỗi slot chỉ cần đúng 3 ảnh: trước / ngang / sau.

### SLOT 0 — xanh dương

### `character_0_jump.png` (trước, nhảy)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare chest, tribal leather loincloth and leg wraps with deep blue accent
dye, leather bracers with carved rune detail glowing faint blue matching a
fantasy adventurer aesthetic, tousled dark hair, tan weathered skin, caught
in mid-air jumping pose, both feet lifted off the ground with knees bent and
tucked slightly upward, arms raised or spread out for balance, body leaning
subtly forward with upward momentum, dynamic athletic leap, empty hands, no
weapon, no held item, full body visible from head to feet, viewed
straight-on from the front, painted fantasy game texture style, warm rim
lighting, isolated on a fully transparent background, no ground, no shadow
cast on ground, no other objects, no text, no watermark, single character
only, centered composition, portrait orientation
```

### `character_0_side_jump.png` (ngang, nhảy)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare chest, tribal leather loincloth and leg wraps with deep blue accent
dye, leather bracers with carved rune detail glowing faint blue matching a
fantasy adventurer aesthetic, tousled dark hair, tan weathered skin, caught
in mid-air jumping pose, both feet lifted off the ground with knees bent and
tucked slightly upward, arms raised or spread out for balance, body leaning
subtly forward with upward momentum, dynamic athletic leap, empty hands, no
weapon, no held item, full body visible from head to feet, viewed from a
strict 90-degree side profile facing right, painted fantasy game texture
style, warm rim lighting, isolated on a fully transparent background, no
ground, no shadow cast on ground, no other objects, no text, no watermark,
single character only, centered composition, portrait orientation
```

### `character_0_back_jump.png` (sau, nhảy)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare chest, tribal leather loincloth and leg wraps with deep blue accent
dye, leather bracers with carved rune detail glowing faint blue matching a
fantasy adventurer aesthetic, tousled dark hair, tan weathered skin, caught
in mid-air jumping pose, both feet lifted off the ground with knees bent and
tucked slightly upward, arms raised or spread out for balance, body leaning
subtly forward with upward momentum, dynamic athletic leap, empty hands, no
weapon, no held item, full body visible from head to feet, viewed
straight-on from directly behind, back of head and back muscles clearly
visible, painted fantasy game texture style, warm rim lighting, isolated on
a fully transparent background, no ground, no shadow cast on ground, no
other objects, no text, no watermark, single character only, centered
composition, portrait orientation
```

---

### SLOT 1 — đỏ

### `character_1_jump.png` (trước, nhảy)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, deep
crimson red accent dye, leather bracers with carved rune detail glowing
faint red matching a fantasy adventurer aesthetic, long dark hair in a
braided ponytail, tan weathered skin, caught in mid-air jumping pose, both
feet lifted off the ground with knees bent and tucked slightly upward, arms
raised or spread out for balance, body leaning subtly forward with upward
momentum, dynamic athletic leap, empty hands, no weapon, no held item, full
body visible from head to feet, viewed straight-on from the front, painted
fantasy game texture style, warm rim lighting, isolated on a fully
transparent background, no ground, no shadow cast on ground, no other
objects, no text, no watermark, single character only, centered composition,
portrait orientation
```

### `character_1_side_jump.png` (ngang, nhảy)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, deep
crimson red accent dye, leather bracers with carved rune detail glowing
faint red matching a fantasy adventurer aesthetic, long dark hair in a
braided ponytail, tan weathered skin, caught in mid-air jumping pose, both
feet lifted off the ground with knees bent and tucked slightly upward, arms
raised or spread out for balance, body leaning subtly forward with upward
momentum, dynamic athletic leap, empty hands, no weapon, no held item, full
body visible from head to feet, viewed from a strict 90-degree side profile
facing right, painted fantasy game texture style, warm rim lighting,
isolated on a fully transparent background, no ground, no shadow cast on
ground, no other objects, no text, no watermark, single character only,
centered composition, portrait orientation
```

### `character_1_back_jump.png` (sau, nhảy)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, deep
crimson red accent dye, leather bracers with carved rune detail glowing
faint red matching a fantasy adventurer aesthetic, long dark hair in a
braided ponytail, tan weathered skin, caught in mid-air jumping pose, both
feet lifted off the ground with knees bent and tucked slightly upward, arms
raised or spread out for balance, body leaning subtly forward with upward
momentum, dynamic athletic leap, empty hands, no weapon, no held item, full
body visible from head to feet, viewed straight-on from directly behind,
back of head and braid clearly visible, painted fantasy game texture style,
warm rim lighting, isolated on a fully transparent background, no ground, no
shadow cast on ground, no other objects, no text, no watermark, single
character only, centered composition, portrait orientation
```

---

### SLOT 2 — xanh lá

### `character_2_jump.png` (trước, nhảy)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare chest, tribal leather loincloth and leg wraps with deep forest green
accent dye, leather bracers with carved rune detail glowing faint green
matching a fantasy adventurer aesthetic, long dark hair tied back, tan
weathered skin, caught in mid-air jumping pose, both feet lifted off the
ground with knees bent and tucked slightly upward, arms raised or spread out
for balance, body leaning subtly forward with upward momentum, dynamic
athletic leap, empty hands, no weapon, no held item, full body visible from
head to feet, viewed straight-on from the front, painted fantasy game
texture style, warm rim lighting, isolated on a fully transparent
background, no ground, no shadow cast on ground, no other objects, no text,
no watermark, single character only, centered composition, portrait
orientation
```

### `character_2_side_jump.png` (ngang, nhảy)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare chest, tribal leather loincloth and leg wraps with deep forest green
accent dye, leather bracers with carved rune detail glowing faint green
matching a fantasy adventurer aesthetic, long dark hair tied back, tan
weathered skin, caught in mid-air jumping pose, both feet lifted off the
ground with knees bent and tucked slightly upward, arms raised or spread out
for balance, body leaning subtly forward with upward momentum, dynamic
athletic leap, empty hands, no weapon, no held item, full body visible from
head to feet, viewed from a strict 90-degree side profile facing right,
painted fantasy game texture style, warm rim lighting, isolated on a fully
transparent background, no ground, no shadow cast on ground, no other
objects, no text, no watermark, single character only, centered composition,
portrait orientation
```

### `character_2_back_jump.png` (sau, nhảy)
```
Full-body game character sprite of a wild jungle hunter, muscular male body,
bare chest, tribal leather loincloth and leg wraps with deep forest green
accent dye, leather bracers with carved rune detail glowing faint green
matching a fantasy adventurer aesthetic, long dark hair tied back, tan
weathered skin, caught in mid-air jumping pose, both feet lifted off the
ground with knees bent and tucked slightly upward, arms raised or spread out
for balance, body leaning subtly forward with upward momentum, dynamic
athletic leap, empty hands, no weapon, no held item, full body visible from
head to feet, viewed straight-on from directly behind, back of head and back
muscles clearly visible, painted fantasy game texture style, warm rim
lighting, isolated on a fully transparent background, no ground, no shadow
cast on ground, no other objects, no text, no watermark, single character
only, centered composition, portrait orientation
```

---

### SLOT 3 — vàng cam

### `character_3_jump.png` (trước, nhảy)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, warm
amber orange accent dye, leather bracers with carved rune detail glowing
faint amber matching a fantasy adventurer aesthetic, short tousled
sandy-brown hair, tan weathered skin, caught in mid-air jumping pose, both
feet lifted off the ground with knees bent and tucked slightly upward, arms
raised or spread out for balance, body leaning subtly forward with upward
momentum, dynamic athletic leap, empty hands, no weapon, no held item, full
body visible from head to feet, viewed straight-on from the front, painted
fantasy game texture style, warm rim lighting, isolated on a fully
transparent background, no ground, no shadow cast on ground, no other
objects, no text, no watermark, single character only, centered composition,
portrait orientation
```

### `character_3_side_jump.png` (ngang, nhảy)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, warm
amber orange accent dye, leather bracers with carved rune detail glowing
faint amber matching a fantasy adventurer aesthetic, short tousled
sandy-brown hair, tan weathered skin, caught in mid-air jumping pose, both
feet lifted off the ground with knees bent and tucked slightly upward, arms
raised or spread out for balance, body leaning subtly forward with upward
momentum, dynamic athletic leap, empty hands, no weapon, no held item, full
body visible from head to feet, viewed from a strict 90-degree side profile
facing right, painted fantasy game texture style, warm rim lighting,
isolated on a fully transparent background, no ground, no shadow cast on
ground, no other objects, no text, no watermark, single character only,
centered composition, portrait orientation
```

### `character_3_back_jump.png` (sau, nhảy)
```
Full-body game character sprite of a wild jungle huntress, athletic toned
female body, tribal leather wrap top and loincloth with leg wraps, warm
amber orange accent dye, leather bracers with carved rune detail glowing
faint amber matching a fantasy adventurer aesthetic, short tousled
sandy-brown hair, tan weathered skin, caught in mid-air jumping pose, both
feet lifted off the ground with knees bent and tucked slightly upward, arms
raised or spread out for balance, body leaning subtly forward with upward
momentum, dynamic athletic leap, empty hands, no weapon, no held item, full
body visible from head to feet, viewed straight-on from directly behind,
back of head clearly visible, painted fantasy game texture style, warm rim
lighting, isolated on a fully transparent background, no ground, no shadow
cast on ground, no other objects, no text, no watermark, single character
only, centered composition, portrait orientation
```

---

## Ghi chú khi nâng cấp sau này

- Muốn thêm slot 5, 6... (nếu tăng `CHARACTER_SLOT_COUNT`): copy nguyên khung 6 prompt của
  1 slot, chỉ đổi màu accent dye + tên màu trong bracer glow (vd `emerald green`, `royal
  purple`...) và đổi kiểu tóc cho khác biệt.
- Ảnh `_side*` và `_side_walk*` chỉ cần generate 1 bên (facing right) - code tự lật gương
  (`MirrorPixelArray`) tạo bên trái, không cần vẽ/generate thêm.
- Nếu muốn nhân vật cầm vũ khí đúng loại đang trang bị (thay vì luôn tay không) thì đây sẽ là
  thay đổi lớn hơn, cần tách riêng theo từng loại vũ khí (dao/kiếm/cung) x 4 slot x 3 hướng x
  2 trạng thái - nên bàn kỹ phạm vi trước khi làm.
- Tư thế ngồi (`_crouch.png`) không có bản `_walk` riêng vì hệ thống hiện không animate bước
  chân khi đang ngồi (xem `PickDirectionalTexture()` - `rp.Crouch` được kiểm tra trước, ưu
  tiên hơn `walkFrameOn`). Nếu sau này muốn có "bò/lết" khi vừa ngồi vừa di chuyển thì mới
  cần thêm `_crouch_walk` x 3 hướng x 4 slot.
- Tư thế nhảy (`_jump.png`) được ưu tiên cao nhất trong `PickDirectionalTexture()` (kiểm tra
  `rp.Z` trước cả `rp.Crouch`), vì một người đang lơ lửng giữa không trung thì không thể vừa
  ngồi vừa bước chân hợp lý về hình ảnh. Cũng không có bản `_walk` hay `_crouch` kết hợp.
