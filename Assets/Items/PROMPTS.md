# Prompt tạo icon vật phẩm - GamePvP3D-HaiNam

Tổng hợp prompt đã dùng để tạo 5 icon vật phẩm hiện có trong `Assets\Items\`.
Giữ lại để tạo thêm vật phẩm mới hoặc tạo lại ảnh lỗi thì có sẵn khung mà sửa.

## Quy ước đặt tên file (khớp với `InitItemCatalog()` trong `Form1.vb`)

| File | Item Id | Dùng cho |
|---|---|---|
| `dagger.png` | `dagger` | Dao găm (Weapon, cận chiến) |
| `sword.png` | `sword` | Kiếm (Weapon, cận chiến) |
| `bow.png` | `bow` | Cung (Weapon, tầm xa) |
| `potion.png` | `potion` | Bình thuốc (Consumable) |
| `arrow.png` | - | Mũi tên bay (Projectile khi bắn cung, không phải icon inventory) |

Khung sườn chung: *"game item icon"*, phong cách tranh vẽ game fantasy (painted fantasy
game texture style), ánh sáng viền ấm, góc nhìn hơi chéo từ trên xuống (slight top-down
angle), nền trong suốt hoàn toàn, không bóng đổ, bố cục căn giữa, chỉ 1 vật phẩm duy nhất.

---

### `dagger.png` — Dao găm
```
Game item icon of a single dagger, short curved blade with worn leather-wrapped
handle, painted fantasy game texture style, warm rim lighting, viewed from a
slight top-down angle, centered composition, isolated on a fully transparent
background, no other objects, no text, no watermark, no shadow cast on ground,
single item only
```

### `sword.png` — Kiếm
```
Game item icon of a single longsword, straight steel blade with simple crossguard
and leather-wrapped grip, painted fantasy game texture style, warm rim lighting,
viewed from a slight top-down angle, centered composition, isolated on a fully
transparent background, no other objects, no text, no watermark, no shadow cast
on ground, single item only
```

### `potion.png` — Bình thuốc
```
Game item icon of a single glass potion bottle filled with glowing red healing
liquid, cork stopper, small bubbles inside, painted fantasy game texture style,
warm rim lighting, viewed from a slight top-down angle, centered composition,
isolated on a fully transparent background, no other objects, no text, no
watermark, no shadow cast on ground, single item only
```

### `bow.png` — Cung
```
Game item icon of a single wooden recurve bow with a taut bowstring, carved wood
grain detail, painted fantasy game texture style, warm rim lighting, viewed from
a slight top-down angle, centered composition, isolated on a fully transparent
background, no other objects, no text, no watermark, no shadow cast on ground,
single item only
```

### `arrow.png` — Mũi tên (ảnh gốc vẽ ngang, đầu nhọn bên phải)
```
Game item icon of a single arrow lying horizontal, pointing right, sharp
metallic arrowhead (diamond-shaped steel tip) on the right end, straight
wooden shaft with carved wood grain detail in the middle, fletching of
three fanned feathers on the left tail end, painted fantasy game texture
style, warm rim lighting, viewed from a slight top-down angle, centered
composition, isolated on a fully transparent background, no other objects,
no text, no watermark, no shadow cast on ground, single item only
```

**Lưu ý về `arrow.png`**: Form1.vb load ảnh này với quy ước đầu nhọn quay bên PHẢI
(xem comment dòng ~164 `Assets\Items\arrow.png`), code sẽ tự xoay ảnh theo hướng bay
thực tế khi bắn - nếu generate lại mà đầu mũi tên quay sai hướng (trái thay vì phải)
thì mũi tên bay trong game sẽ bị lộn ngược.

---

## Ghi chú khi nâng cấp sau này

- Muốn thêm vật phẩm mới (vd rìu, khiên, thuốc độc...): viết prompt theo đúng khung trên,
  đặt tên file mới, rồi thêm 1 dòng `AddCatalogItem(New ItemDefinition() With {...})` trong
  `InitItemCatalog()` (`Form1.vb`) trỏ `.IconFileName` tới file đó.
- Vật phẩm tầm xa (`IsRanged = True` như cung) cần thêm cả ảnh projectile bay riêng (như
  `arrow.png`) nếu muốn hình ảnh khi bay khác với icon trong túi đồ.
- Muốn nhân vật cầm đúng vũ khí đang trang bị khi người khác nhìn thấy (thay vì luôn tay
  không như hiện tại) thì đây là nâng cấp lớn hơn, xem ghi chú cuối file
  `Assets\Characters\PROMPTS.md`.
