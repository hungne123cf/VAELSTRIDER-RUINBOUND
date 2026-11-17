---
name: unity-android-zero-bug-release
description: >
  Bộ quy tắc và quy trình chuẩn xuất bản game Unity sang Android (APK/AAB) đạt chuẩn Zero-Bug:
  Khắc phục 100% lỗi khác biệt giữa PC và Android: màn hình đen mất map, mất hình nền, văng game (crash SIGSEGV),
  không tự xoay màn hình, mất nút di chuyển cảm ứng (Virtual Joystick), sai icon ứng dụng, lỗi nạp tài nguyên,
  và đồng bộ 100% chế độ Online PvP Đối Kháng (Photon Fusion 2) giữa PC Test và Android APK.
  Áp dụng cho mọi dự án game Unity Android.
---

# 📱 Unity Android Zero-Bug Release — Quy Chuẩn Đóng Gói & Xuất Bản Di Động
> **VAI TRÒ CHỦ ĐẠO (SYSTEM PERSONA):** Bạn là một **Nhà Phát Triển Game Chuyên Nghiệp với 20 năm kinh nghiệm (Senior Game Architect & Network Engineer 20+ Years Experience)**. Mọi quyết định thiết kế kiến trúc, tối ưu mã nguồn C#, netcode đồng bộ nhiều người chơi (Multiplayer Co-op / PvP), xử lý đồ họa di động, và giao diện người dùng (UI/UX) đều phải tuân thủ tiêu chuẩn phòng ngừa lỗi từ gốc rễ (Defensive Programming), loại bỏ 100% bug hiện tại và đón đầu ngăn chặn triệt để mọi bug tiềm ẩn trong tương lai.

Tài liệu này đúc kết toàn bộ bài học thực chiến khi chuyển đổi (porting) game từ PC / Unity Editor sang Android APK, đảm bảo game chạy **0 lỗi, 100% mượt mà** ngay từ lần cài đặt đầu tiên (kể cả chế độ Offline lẫn Online PvP Đối Kháng & Co-op Hầm Ngục).

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
Vừa bấm "Bắt đầu game" hoặc khi chuyển Scene từ Login sang Menu/Gameplay/PvP Arena thì game **tự động thoát / văng ngay lập tức** mà không có thông báo.

### Nguyên nhân:
Unity 6 (và URP) mặc định ưu tiên dùng Graphics API **Vulkan**. Trên chip đồ họa điện thoại (Mali, Adreno, PowerVR), việc phân bổ bộ nhớ đệm Pipeline State Objects (PSO) của Vulkan khi tải các hiệu ứng ánh sáng 2D / Shader URP dễ gây xung đột driver và sập ứng dụng (SIGSEGV crash).

### Cấu hình bắt buộc cho Android:
1. **Chuyển Graphics API sang OpenGLES3 Only (hoặc OpenGLES3 đứng trước Vulkan):**
   * Vào `Project Settings -> Player -> Android -> Other Settings`.
   * Bỏ tích `Auto Graphics API`.
   * Xóa Vulkan, chỉ để lại **OpenGLES3**.
2. **Gán Render Pipeline vào Graphics Settings:**
   Đảm bảo `ProjectSettings/GraphicsSettings.asset` có trường `m_CustomRenderPipeline` trỏ đúng vào `UniversalRP.asset` để không bị lỗi shader tím hoặc rớt shader trên di động.
3. **Bảo vệ `Shader.Find` không gây NullReferenceException trên Android:**
   Khi khởi tạo `Material` bằng code, không bao giờ viết thẳng `new Material(Shader.Find("Sprites/Default"))` mà không kiểm tra `null`. Luôn dùng:
   ```csharp
   Shader sh = Shader.Find("Sprites/Default");
   if (sh != null) sr.material = new Material(sh);
   ```

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

### Quy tắc bất di bất dịch:
1. **TUYỆT ĐỐI CẤM SỬ DỤNG EMOJI & KÝ TỰ BIỂU TƯỢNG TRONG TEXT UI:**
   * Tuyệt đối không chèn emoji điện thoại hoặc ký tự vẽ hình unicode vào text (`string`).
   * Luôn dùng từ ngữ tiếng Việt / tiếng Anh rõ nghĩa (`[PHÍM]`, `BÀN PHÍM`, `THU GỌN`, `[X]`, `XÓA`, `DÁN MÃ`, `MÃ CÓ SẴN`, `DẤU CÁCH`, `ĐỔI CODE`).
   * Nếu cần hiển thị icon đồ họa, **bắt buộc dùng Component `Image`** nạp sprite từ `Resources` hoặc dùng TMP Sprite Asset (`<sprite=0>`).
2. **BẮT BUỘC DÙNG `RectMask2D` & TỰ ĐỘNG THU NHỎ CHỮ (AUTO-SIZING) TRÊN MỌI INPUT FIELD:**
   * Gắn `RectMask2D` trực tiếp vào `TextArea` của InputField để khóa cứng vùng hiển thị.
   * Cấu hình `enableAutoSizing = true`, `overflowMode = TextOverflowModes.Truncate`.

---

## 8. BẢO TOÀN ĐỒNG BỘ 100% TƯỚNG (HERO RUNTIME PARITY): KHÔNG ĐỂ SPAWN SAI NHÂN VẬT TRÊN APK

### Hiện tượng lỗi:
Ở Unity Editor chọn nhân vật nào (Player 2..7) thì vào trận đúng nhân vật đó. Nhưng khi xuất ra APK Android, vào trận lại biến thành Player 1 (Vaelstrider) hoặc mất vũ khí/kỹ năng.

### Quy tắc bất di bất dịch:
1. **Toàn bộ Animator Controllers của tất cả tướng phải nằm trong `Assets/Resources/Controllers/`:**
   Nạp runtime bằng `Resources.Load<RuntimeAnimatorController>("Controllers/PlayerX")`.
2. **Spritesheets và Sprites kỹ năng phải nằm trong `Assets/Resources/`:**
   Nạp an toàn bằng `Resources.LoadAll<Sprite>(...)`.
3. **Không bao giờ âm thầm ghi đè nhân vật người chơi đã chọn:**
   Lưu bền vững vào `PlayerPrefs` và `AccountDatabase`, giữ nguyên 100% khi sang Scene gameplay.

---

## 9. NGUYÊN TẮC ĐỐI KHÁNG ONLINE (ONLINE PVP MULTIPLAYER PARITY: PC TEST ↔ ANDROID APK)

Khi phát triển chế độ **Online PvP Đối Kháng (Photon Fusion 2 / Multiplayer)**, phải tuân thủ tuyệt đối 5 nguyên tắc sau để bản APK trên điện thoại kết nối và chiến đấu y hệt 100% như khi test trên PC:

### 9.1. Khóa cứng Region (`FixedRegion = "asia"`) để PC và Android APK luôn thấy phòng của nhau
* **Hiện tượng lỗi:** Mở 1 bản trên PC và 1 bản APK trên điện thoại Android, nhập cùng mã phòng 6 số hoặc cùng bấm Tìm Trận Nhanh nhưng **không bao giờ ghép trúng nhau** (mỗi máy tự tạo phòng riêng).
* **Nguyên nhân:** Photon Cloud mặc định đo ping tự động (`Best Region`). Mạng cáp quang PC có thể nối vào `jp` hoặc `kr`, trong khi mạng 4G/WiFi điện thoại nối vào `asia` (Singapore) hoặc `hk`. Khác cụm máy chủ vùng (Region) thì không thể nhìn thấy phòng của nhau.
* **Quy tắc bắt buộc:** Khi khởi tạo `NetworkRunner.StartGame(StartGameArgs)`, bắt buộc ép `CustomPhotonAppSettings` (hoặc `PhotonAppSettings.Global.AppSettings`) về một vùng cố định duy nhất, ví dụ `FixedRegion = "asia"`, để 100% máy PC, Editor và Android APK đều vào chung một server khu vực.

### 9.2. Cấm dùng `FindAnyObjectByType<PlayerController>()` mù quáng khi có 2 Người Chơi trong Đấu Trường
* **Hiện tượng lỗi:** Vào trận PvP trên điện thoại Android, kéo `VirtualJoystick` hoặc bấm nút đánh/kỹ năng thì nhân vật của mình đứng im, hoặc Camera chạy sang nhìn đối thủ, hoặc thanh máu HUD hiển thị máu của đối thủ.
* **Nguyên nhân:** Các script UI (`VirtualJoystick`, `MobileActionButton`, `HealthBarUI`, `PlayerEnergyUI`, `CameraFollow`) trong chế độ đi ải Offline thường gọi `FindAnyObjectByType<PlayerController>()`. Khi có 2 nhân vật trong scene PvP, hàm này bốc ngẫu nhiên nhân vật được tạo trước/sau trong bộ nhớ (có thể là đối thủ Remote Player).
* **Quy tắc bắt buộc:**
  1. Nhân vật **Local Player** (máy mình điều khiển, `Object.HasInputAuthority == true`) và **Remote Opponent** (đối thủ, `Object.HasInputAuthority == false`) phải được phân tách rõ ràng:
     * **Remote Opponent** tuyệt đối không bật `PlayerController` / `PlayerCombat` đọc bàn phím hay Joystick của máy cục bộ (hoặc dùng avatar đồng bộ chuyên biệt `PvPRemoteFighter` chỉ nhận vị trí, hướng nhìn, hoạt ảnh và hiệu ứng chiêu qua mạng).
     * **Local Player** giữ nguyên 100% `PlayerController`, `PlayerCombat`, `PlayerHealth`, `PlayerEnergySystem` và các script kỹ năng tướng để mọi nút cảm ứng (`VirtualJoystick`, `MobileActionButton`, `SkillAimIndicator`) và `CameraFollow` hoạt động chuẩn xác 100% như Offline.
  2. Ngoại hình của **Remote Opponent** phải được nạp dựa trên biến mạng `[Networked] HeroId` của chính đối thủ đó (nạp từ `Resources/`), tuyệt đối không đọc `CharacterManager.Instance.GetSelectedCharacterId()` của máy mình đắp lên đối thủ (nếu không cả 2 người sẽ biến thành cùng 1 tướng).

### 9.3. Nạp Prefab Mạng & Khởi Tạo Đấu Trường bằng Code + `Resources/` (Không lệ thuộc `AssetDatabase`)
* **Hiện tượng lỗi:** Trên PC Editor tạo vật thể mạng hoặc hiệu ứng chiêu thức chạy bình thường, nhưng trên APK Android thì báo lỗi `NetworkPrefab not found` hoặc đối thủ bị tàng hình.
* **Quy tắc bắt buộc:**
  1. Mọi tài nguyên hình ảnh, hoạt ảnh của cả 7 tướng (Vaelstrider, GaleRonin, AzureBlade, TitanBerserker, DuneAssassin, CelestialMage, AbyssalMech) khi hiển thị cho đối thủ (`Remote Opponent`) đều phải nạp qua `Resources.Load` / `Resources.LoadAll`, tuyệt đối không có đường dẫn `Assets/ART/...` trần.
  2. Scene đấu trường (`PvP_Arena.unity` hoặc chế độ đấu trường tích hợp) bắt buộc phải có mặt trong `EditorBuildSettings.scenes` và `BuildScenesAutoRegistrar.cs`.

### 9.4. Cấp quyền Internet Android & Chạy Nền (`runInBackground`)
* Trong `ProjectSettings.asset`: Đặt `androidInternetPermission: 1` (Require) để file APK xuất ra luôn có quyền `android.permission.INTERNET` và `android.permission.ACCESS_NETWORK_STATE`.
* Trong code khởi tạo mạng: Luôn đặt `Application.runInBackground = true` để khi test song song giữa PC và điện thoại (hoặc khi người chơi vuốt thanh thông báo trên mobile), kết nối Photon không bị ngắt timeout.

### 9.5. Chống lỗi `DisconnectMessage. Code: 104 Msg: "Kick Player [Player:1][1] from Session: ServerLogic"`
* **Hiện tượng lỗi:** Vừa mở phòng hoặc ghép trận là bị Photon Cloud đá văng với lỗi `Code: 104 Msg: "Kick Player [Player:1][1] from Session: ServerLogic"`.
* **Nguyên nhân gốc rễ:**
  1. Trong `GameMode.Shared`, máy chủ Photon Cloud chạy plugin `ServerLogic` chỉ hỗ trợ TickRate chuẩn `32 Hz` và kiểm soát gắt gao việc gửi gói tin qua server (`server-proxied reliable data`). Nếu `NetworkProjectConfig.fusion` để `"Client": 64` hoặc code gọi `SendReliableDataToPlayer` liên tục / gọi ngay khi chưa có người chơi thứ 2, `ServerLogic` sẽ lập tức kick `[Player:1]`.
* **Quy tắc bắt buộc:**
  1. Trong `NetworkProjectConfig.fusion`, luôn đặt `"TickRateSelection": { "Client": 32 }` và `"PlayerCount": 2`.
  2. Đối với đấu trường đối kháng 1v1, ưu tiên dùng **`GameMode.AutoHostOrClient`** (máy tạo phòng làm `Host / IsServer = true`, máy vào sau làm `Client`) kèm theo `NetworkSceneManagerDefault` và `NetworkObjectProviderDefault` gắn trên `NetworkRunner`. Khi đó Photon Cloud chỉ làm Relay truyền gói tin UDP trực tiếp giữa 2 máy mà không chạy plugin `ServerLogic` của Shared Mode.
  3. Phân luồng gửi dữ liệu tin cậy (`ReliableData`):
     * Tuyệt đối **không gửi gói tin** khi phòng chưa đủ 2 người (`GetConnectedPlayerCount() < 2`).
     * Nếu là **Host (`_runner.IsServer == true`)**: Gửi cho đối thủ qua `_runner.SendReliableDataToPlayer(player, key, bytes)`.
     * Nếu là **Client (`_runner.IsServer == false`)**: Gửi thẳng lên Host qua **`_runner.SendReliableDataToServer(key, bytes)`** (tuyệt đối không gọi `SendReliableDataToPlayer` từ máy Client).

### 9.6. Lưu Đồ 9 Bước Giao Diện Đấu Trường PvP (9-Step Modular PvP UI Flow)
* **Nguyên tắc thiết kế:** Tuyệt đối không nhồi nhét toàn bộ nút chọn chế độ, chọn tướng, tạo phòng, bàn phím PIN và tìm trận vào chung 1 bảng duy nhất gây rối mắt. Phải phân tách rõ ràng thành **9 bước tuần tự** có nút `< Quay Lại` ở từng bước:
  1. **Bước 1 — Main Menu (`Chọn PvP`):** Nút `PvP Đối Kháng >` mở Sảnh PvP.
  2. **Bước 2 — Tạo Trận / Chọn Chế Độ (`PvP Đối Kháng`):** Cột trái gồm các tab chế độ (`Đấu Thường Quick Match`, `Tạo Phòng Custom Room`, `Tìm Phòng Mã PIN`, `Đấu Tập AI`); cột phải hiển thị hình minh họa đối kháng, mô tả chế độ và nút `Bắt Đầu >`.
  3. **Bước 3 — Chọn Nhân Vật (`Chọn Nhân Vật`):** Cột trái là lưới 7 Tướng (chân dung nạp từ `Resources/`); cột phải là khung chi tiết nhân vật lớn + chỉ số chuẩn hóa Lôi Đài + nút `Xác Nhận >`.
  4. **Bước 4 — Tạo Phòng (`Custom Room`) / Tìm Phòng (`PIN Keypad`):** Thiết lập `Chế độ`, `Số người (2 người)`, `Bản đồ (Map)`, `Mã Phòng #PIN` + Preview Map + nút `Tạo Phòng >` (hoặc bàn phím số cảm ứng khi Tìm Phòng).
  5. **Bước 5 — Chờ Người Chơi (`Phòng #PIN - 2/2`):** Hiển thị 2 khung thẻ người chơi Trái (Chủ Phòng + Avatar + `Sẵn sàng`) và Phải (Đối Thủ + Avatar + `Sẵn sàng / Chưa sẵn sàng`), kèm 2 nút `Rời Phòng` và `Bắt Đầu Trận >`.
  6. **Bước 6 — Đang Ghép Trận (`Quick Match`):** Vòng tròn Radar xoay tìm trận, đồng hồ đếm thời gian thực, ước tính thời gian và nút `Hủy Tìm Trận`.
  7. **Bước 7 — Ghép Thành Công (`Màn Hình VS`):** Hiển thị `Đã tìm thấy đối thủ!`, khung 2 đấu sĩ đối mặt qua biểu tượng `VS` lớn ở giữa + đếm ngược `Chuẩn bị vào trận... 3, 2, 1`.
  8. **Bước 8 — Trận Đấu (`In-Game Versus HUD`):** Thanh máu + năng lượng đối xứng 2 bên kèm khung Avatar chân dung tướng ở hai đầu và đồng hồ `90` ở chính giữa, trên bản đồ Đấu Trường Bát Giác độc lập hoàn toàn (ẩn toàn bộ UI PvE và tắt `WaveSpawner`).
  9. **Bước 9 — Kết Thúc Trận (`YOU WIN! / DEFEAT`):** Tiêu đề lớn trên đỉnh, hình 2 nhân vật ở hai bên Trái/Phải, hộp `Thưởng Trận Đấu` ở chính giữa (`+Linh Hồn`, `+EXP`), và 2 nút dưới đáy `< Quay Lại` / `Tiếp Tục (Tái Đấu) >`.

### 9.7. Tích Hợp Tài Khoản Thương Mại & Bảng Xếp Hạng Microsoft PlayFab (Zero-Dependency REST API)
* **Nguyên tắc chống vỡ APK:** Không nhúng trực tiếp các bộ SDK `.aar`/`.jar` nặng nề dễ gây xung đột `Duplicate class androidx...` khi xuất APK. Sử dụng `UnityWebRequest` gọi thẳng chuẩn HTTPS REST API chính thức của Microsoft PlayFab (`https://{TitleId}.playfabapi.com/Client/...`).
* **Các cổng kết nối thật bắt buộc hỗ trợ (`PlayFabClientService.cs` & `PlayFabAuthLoginUI.cs`):**
  1. **Tài khoản Chay & Email (`/Client/RegisterPlayFabUser`, `/Client/LoginWithPlayFab`, `/Client/LoginWithEmailAddress`, `/Client/SendAccountRecoveryEmail`):** Đăng ký, đăng nhập và khôi phục mật khẩu qua email thật.
  2. **Chơi Ngay (`Guest Device Cloud` qua `/Client/LoginWithCustomId`) & Liên Kết Tài Khoản (`/Client/AddUsernamePassword`):** Tự động gắn với `SystemInfo.deviceUniqueIdentifier` và cho phép nâng cấp lên tài khoản chính thức không mất tiến trình.
  3. **Cổng Google, Facebook & Zalo OAuth 2.0:** Gọi trực tiếp `/Client/LoginWithGoogleAccount`, `/Client/LoginWithFacebook` và xác thực Zalo Graph API (`https://graph.zalo.me/v2.0/me` -> `ZALO_{id}`).
  4. **Đồng bộ Cloud Save 2 chiều (`/Client/UpdateUserData` & `/Client/GetUserData`):** Lưu trữ và khôi phục toàn bộ `AccountData` (Linh Hồn, cả 7 Tướng bao gồm `AbyssalMech`, Trang bị, Kho đồ, Điểm ELO PvP) giữa PC và Android APK.
  5. **Bảng Xếp Hạng Đấu Trường PvP Toàn Cầu (`/Client/UpdatePlayerStatistics` & `/Client/GetLeaderboard`):** Tự động cập nhật điểm `PvP_Rating` và `PvP_Wins` sau mỗi trận đấu.

### 9.8. Quy Trình Đăng Nhập, Đặt Tên Hiển Thị, Profile & Rank Chuẩn Liên Quân Mobile (`PlayFabAuthLoginUI.cs` & `LoginManager.cs`)
* **Nguyên tắc Tách Bạch Giao Diện (Tuyệt đối không gộp chung gây rối mắt):**
  1. **Màn Hình Đăng Nhập Gọn Sạch Xuất Hiện TRƯỚC Nút "Bắt Đầu Trò Chơi" (`LoginScene`):**
     * Khi vừa mở game, nếu chưa đăng nhập, ẩn nút `Bắt Đầu Trò Chơi` và hiện ngay **Khung Đăng Nhập nhỏ gọn ở giữa màn hình** (`LoginWizardWindow`).
     * Chỉ gồm: Nút `Đăng Nhập Tài Khoản`, Nút `Đăng Ký Tài Khoản Mới` (4 ô: Tên tài khoản, Mật khẩu, Nhập lại mật khẩu, Email tùy chọn), và hàng nút 1 chạm `GOOGLE`, `FACEBOOK`, `ZALO` cùng nút `Tài Khoản Chơi Ngay (Guest)` (**tuyệt đối không hiện ô bắt dán Token hay bảng Cấu hình Server**).
     * **Tuyệt đối không mở trang `http://localhost:52140/auth/` trắng tinh trên trình duyệt:** Bắt buộc bật `Application.runInBackground = true` trong `LoginManager.Awake()`, `OAuthAuthenticator.Awake()` và `PlayFabClientService.Awake()`. Khi người chơi bấm `GOOGLE`, `FACEBOOK`, hoặc `ZALO` mà chưa điền `Client ID` / `App ID` OAuth thật, phải mở ngay **Bảng Chọn Tài Khoản Liên Kết Ngay Trong Game (`Step2_SocialAccountPicker`)** (1 chạm chọn tài khoản đã lưu trên thiết bị hoặc nhập Gmail / Facebook / Zalo kết nối thẳng lên PlayFab `1BE56D`). Chỉ mở trình duyệt ra địa chỉ thật (`accounts.google.com`, `facebook.com`, `oauth.zaloapp.com`) khi đã cấu hình `Client ID` / `App ID` thật.
  2. **Bước Đặt Tên Hiển Thị Trong Game (`Ingame Nickname`):**
     * Ngay sau khi đăng nhập/đăng ký xong, hiển thị tên tài khoản vừa đăng nhập và cho người chơi nhập **Tên Hiển Thị Trong Game (Tên Chiến Binh)**, đồng bộ thẳng lên PlayFab (`UpdateUserTitleDisplayName` & `AccountData.displayName`).
  3. **Màn Hình "Bắt Đầu Trò Chơi" (`Tap To Start`):**
     * Chỉ xuất hiện **SAU KHI** đã đăng nhập và đặt Tên Hiển Thị xong. Hiển thị rõ `CHIẾN BINH: <Tên Hiển Thị> | TÀI KHOẢN: <Username>` phía trên nút `BẮT ĐẦU TRÒ CHƠI` và nút `ĐỔI TÀI KHOẢN` ở góc trên phải.
  4. **Trong Sảnh Chính (`MainMenuScene`) — Tách Riêng Mục Profile và Mục Rank:**
     * **Góc trên bên trái (`TopLeftPlayerProfileBadge`):** Hiển thị `Avatar Tướng + Lv. X • <Bậc Rank> + <Tên Hiển Thị Trong Game>`. Bấm vào sẽ mở **Mục Quản Lý Profile (`OpenProfileModal`)** để xem thành tích, đổi tên hiển thị trong game, liên kết tài khoản Guest hoặc Đăng xuất / Đổi tài khoản.
     * **Góc trên bên phải (`Btn_PlayFabCloud` - `RANK / BXH`):** Bấm vào mở **Bảng Xếp Hạng Rank (`OpenRankModal`)** hoàn toàn độc lập.
  5. **Cấm dùng ký tự ngoặc vuông `[` `]` trong Text UI:** Font chữ nghệ thuật của game ánh xạ ký tự `[` thành chữ `Z`, do đó mọi chuỗi UI phải dùng ngoặc tròn `(` `)` hoặc trình bày sạch không ngoặc vuông.

### 9.9. Chuẩn Hóa Đăng Nhập Google Native Trên Điện Thoại Android (`GoogleAccountPickerFragment.java` & `AndroidGoogleSignInBridge.cs`)
* **Hiện tượng lỗi:** Bấm `ĐĂNG NHẬP BẰNG GOOGLE` trên điện thoại Android, hiện popup chọn tài khoản Google nhưng khi bấm chọn tài khoản xong thì tự đóng lại, **không đăng nhập được và cũng không hiển thị bất kỳ lý do lỗi nào**.
* **Nguyên nhân gốc rễ:**
  1. Khi gọi `GoogleSignIn` (`com.google.android.gms.auth.api.signin`) trên bản APK chưa đăng ký mã băm `SHA-1` của Keystore trên Google Cloud Console, `SignInHubActivity` từ chối với mã lỗi `DEVELOPER_ERROR (10)` nhưng lại trả về `resultCode == Activity.RESULT_CANCELED (0)`. Nếu code Java kiểm tra `if (resultCode == Activity.RESULT_CANCELED)` trước thì sẽ hiểu nhầm là người chơi tự đóng bảng chọn (`User canceled`), nuốt mất lỗi thật và không bao giờ chạy tới nhánh fallback `AccountManager`.
  2. Callback từ `AndroidJavaProxy` chạy trên **Android UI Thread** (khác với **Unity Main Thread**). Nếu gọi `OAuthAuthenticator.EnsureInstance()` (`FindAnyObjectByType` / `new GameObject`) từ Android UI Thread khi `Instance == null`, Unity IL2CPP sẽ ném `UnityException` ngầm làm đứt toàn bộ luồng callback đăng nhập.
* **Quy tắc bắt buộc:**
  1. Trong `GoogleAccountPickerFragment.java`, **luôn ưu tiên gọi `AccountManager.newChooseAccountIntent` (`REQ_CHOOSE_ACCOUNT`) làm phương thức số 1** để mở bảng chọn tài khoản Google hệ thống của Android (hoạt động 100% trên mọi bản APK Debug/Release ngay ở lần chạm đầu tiên mà không phụ thuộc SHA-1 trên Google Cloud).
  2. Khởi tạo sẵn `OAuthAuthenticator.EnsureInstance()` trên **Unity Main Thread** trước khi mở `LaunchAccountPicker`, và trong `RunOnMainThread` chỉ đẩy `Action` vào `Queue` thread-safe (`lock (mainThreadActions)`), tuyệt đối không gọi `EnsureInstance()` từ thread phụ.
  3. Khi người chơi đóng bảng chọn hoặc thiết bị không hỗ trợ Google Play Services, phải hiển thị rõ thông báo lý do trên thanh trạng thái và mở bước `Step2_GoogleEmailPrompt` (hỗ trợ cả nút mở lại bảng chọn thiết bị lẫn ô nhập trực tiếp Email Google đồng bộ PlayFab Cloud).

---

## 10. CHECKLIST 12 BƯỚC "ZERO-BUG" TRƯỚC KHI BẤM BUILD APK

Trước khi xuất bất kỳ bản APK nào cho người dùng, AI Agent bắt buộc phải duyệt qua 12 bước:

| Bước | Hạng mục kiểm tra | Tiêu chuẩn đạt |
|---|---|---|
| **1** | **Missing Scripts** | Quét toàn bộ `.unity` và `.prefab` để đảm bảo không có bất kỳ component nào bị `m_Script: {fileID: 0}`. |
| **2** | **Build Scenes List** | `EditorBuildSettings.scenes` và `BuildScenesAutoRegistrar.cs` chứa đầy đủ tất cả Scene (bao gồm cả Scene PvP nếu có). |
| **3** | **No AssetDatabase** | Không có đoạn code runtime nào gọi `AssetDatabase` mà thiếu guard `#if UNITY_EDITOR`. Mọi Animator Controller nằm trong `Assets/Resources/Controllers/`. |
| **4** | **Tilemap & Background** | Không có hàm nào tự tiện xóa gạch (`ClearAllTiles()`) khi đã có gạch sẵn trong scene. Hình nền nằm trong `Resources`. |
| **5** | **Hero Parity (Đồng bộ tướng)** | Chọn tướng nào trong Menu thì vào trận đúng 100% tướng đó (Sprite, Animator Controller, Chiêu thức, Nút kỹ năng di động), không bị rớt về Player 1. |
| **6** | **Graphics API** | Đã chọn `OpenGLES3` cho Android, không dùng `Vulkan` mặc định nếu chưa bake shader. Kiểm tra `Shader.Find` luôn có check `!= null`. |
| **7** | **Screen Orientation** | Đã khóa cứng `LandscapeLeft` (hoặc `Portrait` nếu là game dọc). |
| **8** | **Touch Controls** | Virtual Joystick và Action Buttons có Procedural Fallback và có độ mờ an toàn $\ge 0.25$. |
| **9** | **Adaptive Icon & Splash** | Cả 6 cấp Adaptive Icon có đủ 2 layer (Layer 1 = Art, Layer 2 = Transparent PNG). Đã tắt Unity Splash. |
| **10** | **Zero-Tofu & No Text Leak** | 100% chuỗi UI không chứa emoji/ký tự lạ gây ô vuông `▯`. Mọi InputField đều có `RectMask2D` và Auto-Sizing chống lem chữ. |
| **11** | **PvP Region & Network Auth** | Đã khóa cứng `FixedRegion` (ví dụ `"asia"`) cho Photon Fusion, bật `Application.runInBackground = true` và cấp quyền Internet cho Android APK. |
| **12** | **PvP Local vs Remote Isolation** | Trong đấu trường PvP, Joystick/Buttons/Camera chỉ điều khiển Local Player; ngoại hình & chiêu thức của Remote Opponent nạp 100% từ `Resources/` theo `HeroId` đồng bộ mạng. |

