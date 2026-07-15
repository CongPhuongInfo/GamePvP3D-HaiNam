# GamePvP3D_HaiNam (Ban test single-player)

Game phieu luu 3D goc nhin thu nhat kieu raycasting (giong Wolfenstein 3D
doi), nhan vat di trong me cung va nhat nam de len diem, du so luong thi
len cap va duoc buff toc do. Toan bo phan render la GDI+ thuan (khong dung
DirectX/OpenGL/thu vien ngoai), bien dich truc tiep bang `vbc.exe`, khong
can Visual Studio.

## Tinh nang ban test

- Engine raycasting DDA tu viet, do phan giai noi bo 320x200 rồi phong to
  len cua so 960x600 (giu phong cach retro, giu hieu nang tot).
- Va cham tuong truot theo tung truc (khong bi dinh cung khi di sat tuong).
- Nam moc len map ngau nhien, ve dang billboard sprite (luon quay mat ve
  camera) co z-buffer de bi tuong che khuat dung cach.
- HUD hien diem, cap do, he so toc do, so nam con lai.
- Minimap goc tren-phai hien vi tri nguoi choi, huong nhin, vi tri nam.
- He thong len cap don gian: cu 10 nam la +1 cap va +0.15 he so toc do.

## Dieu khien

| Phim / Chuot     | Chuc nang                                   |
|-------------------|----------------------------------------------|
| W / Up            | Di toi                                       |
| S / Down          | Di lui                                       |
| A                 | Di ngang trai                                |
| D                 | Di ngang phai                                |
| Left / Right      | Xoay camera                                  |
| Chuot phai        | Nhay                                         |
| Ctrl / C (giu)    | Ngoi xuong                                   |
| Chuot trai        | Dung dung cu / vu khi dang cam (danh cho nang cap sau) |
| ESC               | Thoat game                                   |

## Truc Z that (khong con chi la hieu ung hinh anh)

Ban do co them 2 loai o moi, dung de test co che nhay/ngoi that su anh huong
den va cham chu khong chi doi camera:

- **Loai 3 - Kien hang thap** (mau cam tren minimap): chan duong binh
  thuong, chi vuot qua duoc khi dang nhay du cao (`playerZ >= 0.45`).
- **Loai 4 - Khe chui** (mau xanh nhat tren minimap): chan duong khi dung
  thang, chi chui qua duoc khi dang ngoi du thap (`crouchAmount >= 0.6`).

Co mot doan test san trong me cung (hang tren cung, gan diem xuat phat):
di thang se gap kien hang phai nhay qua, roi den khe phai ngoi xuong moi
chui qua duoc. Tia raycasting cung duoc chinh de "nhin xuyen" cac o nay khi
nguoi choi du dieu kien vuot qua, dong thoi van ve dung khoi nua-chieu-cao
tai vi tri cua no (kien hang chiem nua duoi, khe chui chiem nua tren) thay
vi bien mat hoan toan - giu cam giac chuong ngai vat that.

## Dung cu / vu khi (chuot trai)

Bien `heldItemName` va sub `UseHeldItem()` trong `Form1.vb` da duoc de san
lam noi moc: hien tai chuot trai chua lam gi ca (chua trang bi item nao).
Khi phat trien them he thong item/vu khi, chi can:
1. Doi `heldItemName` khi nguoi choi nhat/trang bi do.
2. Vien logic tan cong/su dung (kiem tra va cham, hoat canh, sat thuong...)
   vao ben trong `UseHeldItem()`.

## Build

Chay `build.bat` (yeu cau da cai .NET Framework 4.x, script tu do tim
`vbc.exe` trong `Framework64` hoac `Framework`). File exe se duoc xuat ra
ngay thu muc goc, khong tao thu muc `bin\`.

## Huong nang cap tiep theo (goi y)

- **He thong item/skill that**: thay `speedMultiplier` bang inventory,
  hieu ung tam thoi, hoac nam co loai khac nhau (nam do, nam vang, nam doc...).
- **Texture that cho tuong/sprite**: hien tai tuong to mau phang theo do
  sau (fog), nam ve thu cong bang toan hoc; co the thay bang anh bitmap
  nap tu thu muc `Assets\` (dung `xcopy` trong build.bat nhu cac game khac).
- **PvP nhieu nguoi choi**: gan `NetworkHub.vb` / `NetworkPeer.vb` theo kien
  truc star-topology dang dung o cac game GamePvP khac (Contra, Mario,
  Tarzan...), dong bo vi tri nguoi choi + trang thai nam qua mang thay vi
  chi chay local.
- **Va cham/dam va PvP**: them projectile hoac melee giua cac nguoi choi
  khi da co lop network.
