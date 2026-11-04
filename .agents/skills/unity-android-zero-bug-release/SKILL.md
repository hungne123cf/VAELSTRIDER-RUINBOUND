---
name: unity-android-zero-bug-release
description: >
  Bộ quy tắc và quy trình chuẩn xuất bản game Unity sang Android (APK/AAB) đạt chuẩn Zero-Bug:
  Khắc phục 100% lỗi khác biệt giữa PC và Android: màn hình đen mất map, mất hình nền, văng game (crash SIGSEGV),
  không tự xoay màn hình, mất nút di chuyển cảm ứng (Virtual Joystick), sai icon ứng dụng và lỗi nạp tài nguyên.
  Áp dụng cho mọi dự án game Unity Android.
---

# 📱 Unity Android Zero-Bug Release — Quy Chuẩn Đóng Gói & Xuất Bản Di Động

Tài liệu này đúc kết toàn bộ bài học thực chiến khi chuyển đổi (porting) game từ PC / Unity Editor sang Android APK, đảm bảo game chạy **0 lỗi, 100% mượt mà** ngay từ lần cài đặt đầu tiên.

---

## 1. NGUYÊN TẮC VÀNG: "NO ASSETDATABASE AT RUNTIME"

### Hiện tượng lỗi:
Trên máy tính game chạy bình thường, nhưng xuất ra APK Android thì **mất hình nền, mất icon, mất bản đồ hoặc biến thành màu đen**.

### Nguyên nhân:
Thư viện `UnityEditor.AssetDatabase` bị Unity loại bỏ 100% khi đóng gói build. Mọi hàm gọi `AssetDatabase.LoadAssetAtPath` sẽ không tồn tại hoặc trả về `null`.

### Quy tắc bất di bất dịch:
1. **Tuyệt đối không để sót `AssetDatabase` trong luồng chạy runtime:**
   Mọi tài nguyên cần nạp động (Dynamic Loading) tại runtime phải nằm trong thư mục `Assets/.../Resources/` hoặc được gán cứng (Serialized Field) trực tiếp vào Scene/Prefab.
2. **Cấm dùng `System.IO.File` đọc asset trên Android:**
   Trên Android, `Application.dataPath` trỏ vào bên trong file nén APK (`/data/app/.../base.apk`). `File.Exists(path)` và `File.ReadAllBytes(path)` luôn trả về `false` hoặc gây lỗi.
3. **Cơ chế Safe Resource Loading cho cả Single và Sliced Sprites:**
   Khi dùng `Resources.Load<Sprite>("path")`, nếu file ảnh được cắt dạng Multiple (Sprite Mode = 2), hàm sẽ trả về `null`. Luôn dùng cấu trúc nạp 3 lớp an toàn:
   ```csharp
   public static Sprite SafeLoadSprite(string resourcePath)
   {
       // 1. Thử nạp dạng Single
       Sprite spr = Resources.Load<Sprite>(resourcePath);
       if (spr != null) return spr;

       // 2. Thử nạp mảng Multiple Sliced Sprites
       Sprite[] all = Resources.LoadAll<Sprite>(resourcePath);
       if (all != null && all.Length > 0) return all[0];

       // 3. Fallback theo tên không kèm folder
       string fileName = System.IO.Path.GetFileNameWithoutExtension(resourcePath);
       return Resources.Load<Sprite>(fileName);
   }
   ```

---

## 2. NGUYÊN TẮC: "ZERO-DESTRUCTIVE SCENE LOADING" (BẢO TOÀN MAP)

### Hiện tượng lỗi:
Vào trận trên Android thì **toàn bộ bản đồ biến mất**, chỉ thấy nhân vật trôi nổi giữa màn hình đen.

### Nguyên nhân:
Script khởi tạo map gọi lệnh dọn dẹp `Tilemap.ClearAllTiles()` trong `Awake()` hoặc `Start()`, nhưng lại phụ thuộc vào `AssetDatabase` để nạp gạch mới. Khi nạp gạch thất bại, code đã lỡ xóa sạch gạch cũ và tô vào đó toàn bộ gạch rỗng (`null`).

### Quy tắc bất di bất dịch:
1. **Không bao giờ xóa Map đã bake sẵn trong Scene:**
   ```csharp
   int existingCount = groundTilemap.GetUsedTilesCount();
   if (existingCount > 0)
   {
       // ĐÃ CÓ GẠCH TRONG SCENE -> GIỮ NGUYÊN 100%, KHÔNG CHẠY ClearAllTiles()!
       return;
   }
   ```
2. **Luôn kiểm tra gạch trước khi xóa:**
   Chỉ cho phép `ClearAllTiles()` khi `floorTile != null && wallTile != null`. Nếu tile nguồn bị `null`, lập tức hủy thao tác để bảo vệ cảnh quan.

---

## 3. CẤU HÌNH ĐỒ HỌA & GPU: PHÒNG CHỐNG CRASH SIGSEGV

### Hiện tượng lỗi:
Vừa bấm "Bắt đầu game" hoặc khi chuyển Scene từ Login sang Menu/Gameplay thì game **tự động thoát / văng ngay lập tức** mà không có thông báo.

### Nguyên nhân:
Unity 6 (và URP) mặc định ưu tiên dùng Graphics API **Vulkan**. Trên chip đồ họa điện thoại (Mali, Adreno, PowerVR), việc phân bổ bộ nhớ đệm Pipeline State Objects (PSO) của Vulkan khi tải các hiệu ứng ánh sáng 2D / Shader URP dễ gây xung đột driver và sập ứng dụng (SIGSEGV crash).

### Cấu hình bắt buộc cho Android:
1. **Chuyển Graphics API sang OpenGLES3 Only (hoặc OpenGLES3 đứng trước Vulkan):**
   * Vào `Project Settings -> Player -> Android -> Other Settings`.
   * Bỏ tích `Auto Graphics API`.
   * Xóa Vulkan, chỉ để lại **OpenGLES3**.
2. **Gán Render Pipeline vào Graphics Settings:**
   Đảm bảo `ProjectSettings/GraphicsSettings.asset` có trường `m_CustomRenderPipeline` trỏ đúng vào `UniversalRP.asset` để không bị lỗi shader tím hoặc rớt shader trên di động.

---

## 4. KHÓA HƯỚNG MÀN HÌNH (SCREEN ORIENTATION LOCKDOWN)

### Hiện tượng lỗi:
Vào game không tự xoay ngang, người chơi phải bật thủ công tính năng xoay màn hình trên điện thoại.

### Cấu hình bắt buộc:
1. **Trong PlayerSettings:**
   * `Default Interface Orientation`: **Landscape Left**
   * `Allowed Autorotate to Portrait`: **False**
   * `Allowed Autorotate to Portrait Upside Down`: **False**
   * `Allowed Autorotate to Landscape Left`: **True**
   * `Allowed Autorotate to Landscape Right`: **True**
2. **Khóa cưỡng bức trong mã C# ngay trước khi nạp Scene đầu tiên:**
   ```csharp
   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
   public static void ForceLandscapeMode()
   {
       Screen.orientation = ScreenOrientation.LandscapeLeft;
       Screen.autorotateToPortrait = false;
       Screen.autorotateToPortraitUpsideDown = false;
       Screen.autorotateToLandscapeLeft = true;
       Screen.autorotateToLandscapeRight = true;
   }
   ```

---

## 5. BỘ ĐIỀU KHIỂN CẢM ỨNG DI ĐỘNG (RESPONSIVE TOUCH CONTROLS)

### Hiện tượng lỗi:
Mất cần di chuyển (Virtual Joystick), các nút bấm quá nhỏ hoặc bị mờ tịt không nhìn thấy.

### Quy tắc bất di bất dịch:
1. **Ưu tiên đúng tài nguyên của người dùng:**
   Khi dự án có nhiều ảnh joystick (ví dụ ảnh pixel do user tự vẽ và ảnh cũ của engine), luôn nạp đúng tên file pixel do người dùng vẽ trước.
2. **Cơ chế Procedural Circle Fallback (Không bao giờ để nút biến mất):**
   Nếu file ảnh joystick bị hỏng hoặc mất liên kết, code phải tự động sinh ra một vòng tròn phát sáng bằng code (`Texture2D.SetPixel`) để người chơi vẫn di chuyển được, không bao giờ bị mất nút.
3. **Bảo vệ độ mờ an toàn trong PlayerPrefs:**
   ```csharp
   float opacity = PlayerPrefs.GetFloat("Game_Controls_Opacity", 1.0f);
   if (opacity < 0.25f) opacity = 1.0f; // Chống người dùng vô tình kéo mờ tịt
   ```
4. **Canvas Scaler chuẩn điện thoại:**
   * `UI Scale Mode`: **Scale With Screen Size**
   * `Reference Resolution`: **1920 x 1080**
   * `Screen Match Mode`: **Match Width Or Height (0.5)**
   Điều này đảm bảo các nút bấm nằm đúng vị trí ngón tay trên cả màn hình 16:9 lẫn màn hình tràn viền 20:9.

---

## 6. ĐỊNH DANH ỨNG DỤNG & APP ICON CHUẨN GOOGLE PLAY / ITCH.IO

### Hiện tượng lỗi:
Logo lập phương màu trắng của Unity đè lên chính giữa hình icon của game trên màn hình điện thoại; hoặc khi mở game hiện logo "Made with Unity".

### Nguyên nhân:
1. Từ Android 8.0 trở lên, Android sử dụng cơ chế **Adaptive Icon** gồm 2 lớp: **Background (Nền)** và **Foreground (Tiền cảnh)**. Khi ta chỉ cài ảnh nền mà bỏ trống tiền cảnh, Unity tự động gán logo 3D Cube của Unity vào lớp Foreground!
2. Unity mặc định bật màn hình chờ Splash Screen có logo Unity.

### Quy tắc bất di bất dịch:
1. **Triệt tiêu Logo Unity trên Adaptive Icon:**
   * Lớp Background (`m_Textures[0]`): Gán ảnh bìa / nhân vật game (`AppIcon.png` 512x512).
   * Lớp Foreground (`m_Textures[1]`): Bắt buộc gán một file ảnh **PNG trong suốt hoàn toàn 100%** (`TransparentForeground.png` kích thước 512x512, RGBA = 0,0,0,0). Điều này giúp icon hiển thị 100% hình vẽ game sạch sẽ, không có bất kỳ logo Unity nào chèn vào.
2. **Tắt Unity Splash Screen Logo (Unity 6 / Personal):**
   * Trong `ProjectSettings.asset`, đặt `m_ShowUnitySplashScreen: 0` và `m_ShowUnitySplashLogo: 0`.
3. **Định danh Package Name & Version Code:**
   * Package Name: `com.[TenTacGia].[TenGame]` (viết thường, không dấu, không khoảng trắng).
   * Mỗi lần xuất bản APK mới, bắt buộc tăng `bundleVersion` (ví dụ `1.0` -> `1.1`) và `bundleVersionCode` (ví dụ `1` -> `2`) để điện thoại nhận diện bản cập nhật thay vì xung đột cache cũ.
4. **Lưu ý bộ nhớ đệm Launcher trên điện thoại:**
   * Android Launcher cache icon theo Package Name. Khi test APK mới đổi icon, người dùng nên gỡ bản cũ trên điện thoại rồi mới cài bản mới để xóa sạch icon cache cũ.

---

## 7. NGUYÊN TẮC "ZERO-TOFU": TRIỆT TIÊU 100% KÝ TỰ Ô VUÔNG & BẢO VỆ TEXT FIELD KHÔNG TRÀN CHỮ

### Hiện tượng lỗi:
1. Trên giao diện UI (nút bấm, nhãn chữ, thông báo, tiêu đề) xuất hiện các **ký tự ô vuông rỗng `▯` (hiện tượng Tofu)** thay cho các biểu tượng/icon.
2. Khi người chơi gõ chữ vào ô nhập (`TMP_InputField`), chữ dài bị **tràn qua mép khung, lem đè lên các nút bấm và chữ khác**.

### Nguyên nhân:
1. **Thiếu Glyph trong Font Asset TextMesh Pro:** Font chữ mặc định của game (`LiberationSans SDF`) hoặc font Việt hóa chỉ chứa bảng mã ký tự cơ bản (ASCII, Latin và các nguyên âm tiếng Việt). Font này **hoàn toàn KHÔNG chứa** các ký tự Emoji hoặc Unicode đặc biệt (ví dụ: `⌨`, `📱`, `📋`, `🎁`, `␣`, `✔`, `✕`, `⌫`, `⚡`, `⚠️`, `❖`, `←`, `✓`...). Khi gặp ký tự thiếu, TextMesh Pro mặc định vẽ hình ô vuông `▯`.
2. **Thiếu `RectMask2D` và thiếu `enableAutoSizing` trên InputField:** Đối tượng `TextArea` bên trong `TMP_InputField` nếu không có component `RectMask2D` sẽ để cho các ký tự vượt ngoài kích thước khung vẽ tự do ra ngoài màn hình. Đồng thời nếu không bật co chữ tự động, chữ sẽ giữ nguyên size to và bị lem ra ngoài.

### Quy tắc bất di bất dịch:
1. **TUYỆT ĐỐI CẤM SỬ DỤNG EMOJI & KÝ TỰ BIỂU TƯỢNG TRONG TEXT UI:**
   * Tuyệt đối không chèn emoji điện thoại hoặc ký tự vẽ hình unicode vào text (`string`).
   * Luôn dùng từ ngữ tiếng Việt / tiếng Anh rõ nghĩa:
     * Dùng `[PHÍM]` thay vì `[⌨ PHÍM]`
     * Dùng `BÀN PHÍM` thay vì `📱 BÀN PHÍM`
     * Dùng `THU GỌN` hoặc `[X]` thay vì `✕ THU GỌN`
     * Dùng `XÓA` thay vì `⌫ XÓA`
     * Dùng `DÁN MÃ` thay vì `📋 DÁN MÃ`
     * Dùng `MÃ CÓ SẴN` thay vì `🎁 MÃ CÓ SẴN`
     * Dùng `DẤU CÁCH` thay vì `␣ DẤU CÁCH`
     * Dùng `ĐỔI CODE` thay vì `✔ ĐỔI CODE`
   * Nếu cần hiển thị icon đồ họa, **bắt buộc dùng Component `Image`** nạp sprite từ `Resources` hoặc dùng TMP Sprite Asset (`<sprite=0>`), tuyệt đối không gõ emoji vào chuỗi.

2. **BẮT BUỘC DÙNG `RectMask2D` & TỰ ĐỘNG THU NHỎ CHỮ (AUTO-SIZING) TRÊN MỌI INPUT FIELD:**
   * Gắn `RectMask2D` trực tiếp vào `TextArea` của InputField để khóa cứng vùng hiển thị, không bao giờ để chữ lem ra ngoài:
     ```csharp
     GameObject taObj = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
     ```
   * Cấu hình tự động co chữ (`enableAutoSizing = true`) và cắt chữ gọn (`overflowMode = Truncate`):
     ```csharp
     itTmp.enableAutoSizing = true;
     itTmp.fontSizeMin = 8.5f;
     itTmp.fontSizeMax = 13.5f;
     itTmp.overflowMode = TextOverflowModes.Truncate;
     ```
   * Kết hợp hàm điều chỉnh font size theo độ dài chuỗi trong sự kiện `onValueChanged` để chữ dài tự động thu nhỏ vừa khít với lòng khung nhập.

---

## 8. CHECKLIST 9 BƯỚC "ZERO-BUG" TRƯỚC KHI BẤM BUILD APK

Trước khi xuất bất kỳ bản APK nào cho người dùng, AI Agent bắt buộc phải duyệt qua 9 bước:

| Bước | Hạng mục kiểm tra | Tiêu chuẩn đạt |
|---|---|---|
| **1** | **Missing Scripts** | Quét toàn bộ `.unity` và `.prefab` để đảm bảo không có bất kỳ component nào bị `m_Script: {fileID: 0}`. |
| **2** | **Build Scenes List** | `EditorBuildSettings.scenes` chứa đầy đủ các màn chơi theo đúng thứ tự: Scene khởi động đầu tiên ở index 0. |
| **3** | **No AssetDatabase** | Không có đoạn code runtime nào gọi `AssetDatabase` mà thiếu guard `#if UNITY_EDITOR`. |
| **4** | **Tilemap & Background** | Không có hàm nào tự tiện xóa gạch (`ClearAllTiles()`) khi đã có gạch sẵn trong scene. Hình nền nằm trong `Resources`. |
| **5** | **Graphics API** | Đã chọn `OpenGLES3` cho Android, không dùng `Vulkan` mặc định nếu chưa bake shader. |
| **6** | **Screen Orientation** | Đã khóa cứng `LandscapeLeft` (hoặc `Portrait` nếu là game dọc). |
| **7** | **Touch Controls** | Virtual Joystick và Action Buttons có Procedural Fallback và có độ mờ an toàn $\ge 0.25$. |
| **8** | **Adaptive Icon & Splash** | Cả 6 cấp Adaptive Icon có đủ 2 layer (Layer 1 = Art, Layer 2 = Transparent PNG). Đã tắt Unity Splash. |
| **9** | **Zero-Tofu & No Text Leak** | 100% chuỗi UI không chứa emoji/ký tự lạ gây ô vuông `▯`. Mọi InputField đều có `RectMask2D` và Auto-Sizing chống lem chữ. |

