# 🧠 Kebab Chef Simulator — AI Agent Memory & Instructions

> **Ціль:** Довести гру до Steam-ready якості. Це Unity 6 (URP 17.3.0) проєкт на C#.  
> **Мова коду:** C# / Unity. Всі UI та системи створюються **runtime з коду** (не через Unity Editor).  
> **Проєкт:** `/Users/maxmariukha/Desktop/pgk2/KebabChefSymulator/`  
> **Скрипти:** `Assets/Scripts/` — основні, `Assets/Scripts/Effects/` — ефекти (нова папка)  
> **Тести:** `Assets/Tests/EditMode/KitchenGameTests.cs` — 29 unit-тестів  
> **Моделі:** `Assets/Resources/Models/` — 21 GLB-модель

---

## 📁 Ключові файли та їх ролі

| Файл | Роль | Singleton? |
|------|------|-----------|
| `KitchenGameBootstrap.cs` | Входна точка. `[RuntimeInitializeOnLoadMethod]` створює всю сцену з коду. `Start()` → `EnsureNetworkSetup()` → `EnsureManagers()` → `BuildEnvironmentIfNeeded()` → `BuildKitchenIfNeeded()` → `BuildOrderBoardIfNeeded()` → `ConfigureLighting()` → `EnsureEffects()` | Ні |
| `NetworkPlayer.cs` | Spawn гравця. `SetupLocalPlayer()` створює Camera, Controller, Interaction, HUD, ShopUI, PlayerListUI. Для remote — capsule+sphere visual. Містить `HeldItemBob` клас внизу файлу | Ні |
| `KitchenStation.cs` | 5 типів стацій (Source, CuttingBoard, Grill, Assembly, Delivery). `Interact()` — основна логіка готування | Ні |
| `OrderManager.cs` | 8 шаблонів замовлень, progressive difficulty, `NoweZamowienie()`, `TryDeliverDish()` | `Instance` |
| `EconomyManager.cs` | Баланс, `AddMoney()`, `SpendMoney()` | `Instance` |
| `ShopManager.cs` | 5 апгрейдів, `GetProcessingSpeedMultiplier()`, `GetRewardMultiplier()` | `Instance` |
| `SaveManager.cs` | JSON save/load кожні 15с | `Instance` |
| `VFXManager.cs` | 5 particle effects: Steam, Chop, Money, DeliverySuccess, DeliveryFail | `Instance` |
| `KitchenHUD.cs` | Top bar HUD, floating money text, upgrade status | Ні |
| `ShopUI.cs` | Магазин (B key), animated panel | Ні |
| `LobbyUI.cs` | Multiplayer lobby (F1 key), Unity Relay join codes | Ні |
| `PlayerListUI.cs` | TAB — список гравців | Ні |
| `SimplePlayerController.cs` | FPS movement, WASD + mouse look, `CharacterController` | Ні |
| `PlayerInteraction.cs` | Raycast E-interact, Q-drop, held item | Ні |
| `RelayManager.cs` | Unity Relay Service (async), create/join room | `Instance` |
| `NetworkSetup.cs` | Netcode NGO setup, `NetworkManager`, prefab handler | `Instance` |
| `NetworkKitchenStation.cs` | Dirty-checking + snapshot sync для стацій | Ні |
| `KitchenOrderBoard.cs` | World-space монітор з замовленнями | Ні |
| `KitchenItemVisualFactory.cs` | Створює 3D візуали предметів (з Resources/Models або fallback primitives) | Static |
| `KitchenGameModels.cs` | Enums (`IngredientKind` 7шт, `IngredientProcessState` 4шт, `KitchenStationType` 5шт), `KitchenItem`, `PreparedIngredientData`, `IngredientRequirement`, `KitchenOrderValidator`, `KitchenNaming` | Static |
| `Effects/PostProcessSetup.cs` | ✅ DONE — URP Volume (Bloom, Vignette, Color Adj, Tonemapping, Film Grain, Chromatic Aberration), `PulseBloom()` | `Instance` |
| `Effects/AmbientParticles.cs` | ✅ DONE — floating dust | `Instance` |
| `Effects/LampFlicker.cs` | ✅ DONE — `LampFlicker` + `LampEmissionPulse` | Ні |

---

## ✅ Вже зроблено (НЕ повторювати)

1. ✅ Баг `shader` → `GetLitShader()` у Bootstrap L315
2. ✅ +5 order templates + progressive difficulty (time -2%/order, template unlock at 3/6 completed)
3. ✅ +15 unit-тестів (29 загалом)
4. ✅ PostProcessSetup — Bloom, Vignette, Color Adjustments, ACES Tonemapping, Film Grain, Chromatic Aberration
5. ✅ AmbientParticles — floating dust
6. ✅ LampFlicker + LampEmissionPulse
7. ✅ Camera `renderPostProcessing = true` + SMAA
8. ✅ `PulseBloom()` при successful delivery

---

## 📋 Що залишилось — 9 промптів

---

### Промпт ② — 🔇 Audio System

**Створити:**
- `Assets/Scripts/Audio/AudioManager.cs` — singleton

**Архітектура AudioManager:**
```csharp
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    // Один AudioSource для music (loop), один для SFX (one-shot)
    private AudioSource musicSource;
    private AudioSource sfxSource;
    
    // Генерувати звуки процедурно через AudioClip.Create() + SetData()
    // бо в проєкті немає .wav/.ogg файлів!
    // Або використати OnAudioFilterRead для простих синтезованих звуків
}
```

**Звуки для генерації (процедурно, бо немає аудіо-ассетів):**
- `PlayChopSound()` — короткий staccato noise burst (white noise envelope 0.1s)
- `PlayGrillSizzle()` — loop, filtered noise (low-pass)
- `PlayPickup()` — rising tone ~0.15s (sine sweep 400→800Hz)
- `PlayDrop()` — falling tone ~0.15s (sine sweep 600→300Hz)
- `PlayMoneyCashIn()` — bright ding (sine 880Hz + 1320Hz, 0.3s)
- `PlayOrderFail()` — descending 2-note (sine 440→220Hz)
- `PlayNewOrder()` — bell ding (sine 1047Hz, 0.2s, fast decay)
- `PlayButtonClick()` — short tick (noise burst 0.05s)
- `PlayBackgroundMusic()` — simple ambient loop (low sine chords, very subtle)

**Інтеграція — куди додати виклики:**
- `KitchenStation.cs`: `FinishProcessing()` → PlayChopSound (CuttingBoard), PlayGrillSizzle stop (Grill)
- `KitchenStation.cs`: `StartProcessing()` → PlayGrillSizzle start (Grill), PlayChopSound (CuttingBoard)  
- `KitchenStation.cs`: L463 delivery success → PlayMoneyCashIn
- `KitchenStation.cs`: L477 delivery fail → PlayOrderFail
- `KitchenStation.cs`: pickup item → PlayPickup
- `PlayerInteraction.cs`: L139 drop (Q) → PlayDrop
- `OrderManager.cs`: `NoweZamowienie()` → PlayNewOrder
- `ShopUI.cs`: purchase click → PlayButtonClick
- `KitchenGameBootstrap.cs`: `EnsureEffects()` → add `AudioManager`, start music

---

### Промпт ③ — 🏃 Head Bob + Camera Effects

**Створити:**
- `Assets/Scripts/Effects/CameraEffects.cs`

**Архітектура:**
```csharp
public class CameraEffects : MonoBehaviour
{
    public static CameraEffects Instance;
    // Прикріпити до camera object в NetworkPlayer.SetupLocalPlayer()
    
    // Head bob: sine wave на localPosition.y при CharacterController.velocity > 0.1
    // bobFrequency = 8f, bobAmplitude = 0.035f
    // Horizontal bob: localPosition.x sine at half freq
    
    // Screen shake: додати random offset на певний час
    // ShakeCamera(float intensity, float duration)
    
    // Screen flash: full-screen Image overlay з alpha fade
    // FlashScreen(Color color, float duration) — зелений при delivery, червоний при fail
    
    // Landing bob: при isGrounded && wasInAir → quick downward dip
}
```

**Інтеграція:**
- `NetworkPlayer.SetupLocalPlayer()` → add CameraEffects to camera
- `SimplePlayerController.HandleMovement()` → feed velocity to CameraEffects
- `KitchenStation.cs` delivery success → `CameraEffects.Instance.ShakeCamera(0.1f, 0.3f)` + `FlashScreen(green)`
- `KitchenStation.cs` delivery fail → `FlashScreen(red)`
- `KitchenStation.cs` chop finish → `ShakeCamera(0.05f, 0.15f)`

---

### Промпт ④ — 🎯 Outline Highlight + Animated Crosshair

**Створити:**
- `Assets/Scripts/Effects/InteractionHighlight.cs`

**Підхід (без custom shaders, бо runtime-only):**
- При hover на interactable → створити outline через другий mesh з inverted normals + solid color material (або scale *1.03 з unlit bright material)
- Або простіше: змінювати emission color на renderer при hover
- Crosshair: змінити `crosshairText` в KitchenHUD — scale up + color change при `currentPrompt != ""`
- Додати dot-to-ring animation на crosshair

**Інтеграція:**
- `PlayerInteraction.HandleRaycast()` → notify InteractionHighlight про current target
- `KitchenHUD.RefreshTexts()` → animate crosshair based on prompt state

---

### Промпт ⑤ — 🍕 Item Animations

**Створити:**
- `Assets/Scripts/Effects/ItemAnimator.cs`

**Функції:**
- `AnimateSpawn(GameObject obj)` — scale 0→1 з overshoot bounce (AnimationCurve)
- `AnimatePickup(GameObject obj, Transform target)` — lerp position до руки + scale down
- `AnimateDrop(GameObject obj)` — scale 1→0 за 0.2s
- `AnimateAssemblyAdd(Vector3 from, Vector3 to)` — ingredient arc trajectory до assembly

**Інтеграція:**
- `KitchenStation.UpdateDynamicVisuals()` → AnimateSpawn при появі нового візуалу
- `NetworkPlayer.UpdateHeldItemVisual()` → AnimateSpawn при створенні held visual
- Station items: `RefreshVisualState()` → animate new visuals

---

### Промпт ⑥ — 🖥️ Main Menu

**Створити:**
- `Assets/Scripts/UI/MainMenuUI.cs`

**Архітектура:**
- Canvas overlay, пріоритет 200 (вище за все)
- Анімований background (gradient shift або particle effect)
- Title "KEBAB CHEF SIMULATOR" з gold glow
- Buttons: "▶ GRAJ" (hide menu, show lobby), "⚙ USTAWIENIA", "✕ WYJDZ"  
- При натисканні Play → ховає себе, показує LobbyUI
- Freeze game поки відкрито (Time.timeScale = 0)
- `KitchenGameBootstrap.Start()` → show MainMenuUI замість прямого `lobby.ShowLobby()`

---

### Промпт ⑦ — ⏸️ Pause + Settings

**Створити:**
- `Assets/Scripts/UI/PauseMenuUI.cs`
- `Assets/Scripts/UI/SettingsUI.cs`
- `Assets/Scripts/GameSettings.cs` — static клас з PlayerPrefs

**Функції:**
- Esc → PauseMenu (Resume, Settings, Quit to Menu, Quit to Desktop)
- Time.timeScale = 0 при pause
- Settings: Master Volume (slider 0-1), Music Volume, SFX Volume, Mouse Sensitivity (slider 0.5-5), Graphics Quality (Low/Med/High → QualitySettings)
- Save/Load з PlayerPrefs
- Інтеграція з AudioManager (volume) і SimplePlayerController (sensitivity)

---

### Промпт ⑧ — 📊 HUD Upgrade

**Модифікувати:**
- `Assets/Scripts/KitchenHUD.cs`

**Зміни:**
- Замінити текст "BALANCE" на іконку монети (Unicode ● з золотим кольором) + число
- Додати mini progress bar для активного замовлення (під top bar)
- Додати streak counter "🔥 3 z rzędu!" при 3+ consecutive deliveries
- Urgency flash: HUD timer блимає червоним при < 15с
- Smooth number animation при зміні балансу (lerp)

---

### Промпт ⑨ — 👤 Remote Player Visual

**Модифікувати:**
- `NetworkPlayer.CreateRemoteVisual()`

**Зміни:**
- Замість capsule+sphere → побудувати "кухаря" з примітивів:
  - Body: capsule (тіло) + cube (фартух, білий/кольоровий)
  - Head: sphere + маленький cylinder (шапка кухаря, білий)
  - Arms: 2 тонких capsule з боків
- Idle animation: subtle Y-axis breathing (scale pulse 0.5%)
- Walk detection: якщо position delta > threshold → tilt body forward 5°
- Color per player index (вже є `PlayerColors`)

---

### Промпт ⑩ — 🏆 Final Polish

**Створити:**
- `Assets/Scripts/UI/LoadingScreen.cs` — fade-in/out чорний overlay при старті
- `Assets/Scripts/UI/AchievementPopup.cs` — toast notification

**Achievement triggers:**
- Перше замовлення → "Pierwszy kebab!"
- 10 замовлень → "Doswiadczony kucharz"
- 5 без помилок → "Perfekcjonista"
- Перший апгрейд → "Inwestor"
- Всі апгрейди max → "Imperium kebabowe"

**Customer reactions:**
- `KitchenGameBootstrap.CreateCustomer()` — додати animation component
- Happy reaction: scale bounce + particle hearts при delivery success
- Angry reaction: red tint + shake при fail

---

## 🔧 Важливі патерни коду

### Як додавати нову систему:
1. Створи клас з `public static Instance` (singleton)
2. В `KitchenGameBootstrap.EnsureManagers()` або `EnsureEffects()` додай `AddComponent`
3. В `Awake()`: `if (Instance != null) { Destroy(gameObject); return; } Instance = this;`

### Як створювати UI runtime:
```csharp
// Всі UI створюються з коду, НЕ через prefab:
Canvas canvas = new GameObject("MyCanvas").AddComponent<Canvas>();
canvas.renderMode = RenderMode.ScreenSpaceOverlay;
canvas.sortingOrder = 150; // вище за HUD (10), нижче за Lobby (100)
canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
canvasObj.AddComponent<GraphicRaycaster>();

// Font:
Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
```

### Як генерувати звуки процедурно:
```csharp
AudioClip clip = AudioClip.Create("name", sampleCount, 1, 44100, false);
float[] data = new float[sampleCount];
for (int i = 0; i < sampleCount; i++)
{
    float t = (float)i / sampleCount;
    data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope(t);
}
clip.SetData(data, 0);
```

### Клавіші вже зайняті:
- `E` — interact
- `Q` — drop item  
- `B` — shop
- `TAB` — player list
- `F1` — lobby
- `F11` — fullscreen toggle
- `Esc` — unlock cursor (треба переробити на Pause Menu)
- `WASD` — рух
- `Mouse` — камера

---

## ⚠️ Gotchas

1. **Ніколи не використовуй Scene або Prefab** — вся гра створюється runtime через `[RuntimeInitializeOnLoadMethod]` в `KitchenGameBootstrap`
2. **NetworkPlayer** має 2 режими: `IsOwner` (local) vs remote. Camera тільки у local.
3. **ShopUI/LobbyUI** блокують input — перевіряй `IsShopOpen`/`IsLobbyOpen` у нових системах
4. **`EnsureEffects()` в Bootstrap** — місце для додавання нових ефект-систем
5. **`EnsureManagers()` в Bootstrap** — місце для core managers
6. **`SetupLocalPlayer()` в NetworkPlayer** — місце для camera-attached компонентів (bob, shake)
7. **URP 17.3.0 (Unity 6)** — використовуй `UnityEngine.Rendering.Universal` namespace
