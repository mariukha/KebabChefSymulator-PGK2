# Sprawozdanie z postępów prac — Kamień Milowy 2

**Projekt:** KebabChefSymulator  
**Przedmiot:** Programowanie Gier Komputerowych 2 (PGK2)  
**Data:** 9 maja 2026  

---

## 1. Podsumowanie zrealizowanych funkcjonalności

Drugi kamień milowy obejmował sześć kluczowych obszarów rozwoju gry. Poniżej opisano stan realizacji każdego z nich.

| # | Funkcjonalność | Status |
|---|---------------|--------|
| 1 | System ekonomiczny | ✅ Zrealizowany |
| 2 | Sklep z ulepszeniami | ✅ Zrealizowany |
| 3 | Tryb multiplayer | ✅ Zrealizowany |
| 4 | Efekty wizualne | ✅ Zrealizowany |
| 5 | Optymalizacja i poprawki sieciowe | ✅ Zrealizowany |
| 6 | Testy jednostkowe | ✅ Zrealizowany |

---

## 2. System ekonomiczny

### Architektura

System ekonomiczny opiera się na klasie `EconomyManager` (singleton, `NetworkBehaviour`), która zarządza stanem konta gracza za pomocą zmiennych sieciowych (`NetworkVariable<float>`).

**Kluczowe elementy:**
- **`netBalance`** — aktualny stan konta, synchronizowany ze wszystkimi klientami
- **`netTotalEarned`** — łączna suma zarobionych pieniędzy
- **`totalSpent`** — łączna suma wydanych pieniędzy

**Przepływ zarabiania pieniędzy:**
1. Gracz dostarcza gotowego kebaba na stanowisko wydania (`KitchenStation.HandleDelivery`)
2. `OrderManager.TryDeliverDish()` waliduje zgodność dania z zamówieniem
3. Jeśli walidacja przechodzi, `EconomyManager.AddMoney(reward)` dodaje nagrodę
4. Nagroda jest obliczana dynamicznie na podstawie składników + mnożnika z ulepszeń

**Walidacja zamówień:**
Klasa `KitchenOrderValidator` porównuje zawartość dostarczonego kebaba z wymaganiami zamówienia, weryfikując zarówno rodzaj składników, jak i ich stan przygotowania (surowy/pokrojony/upieczony).

### Zapis stanu

Klasa `SaveManager` obsługuje automatyczny zapis co 15 sekund oraz zapis przy wyjściu z gry. Dane ekonomiczne serializowane są do formatu JSON.

---

## 3. Sklep z ulepszeniami

### Dostępne ulepszenia

| Ulepszenie | Opis | Max poz. | Bazowy koszt |
|-----------|------|----------|-------------|
| Szybszy Doner | Skraca czas ścinania mięsa z donera | 4 | 45 zł |
| Szybsze Krojenie | Skraca czas krojenia warzyw | 4 | 35 zł |
| Lepsza Reputacja | Zwiększa nagrodę za zamówienia | 5 | 60 zł |
| Więcej Czasu | Dodaje czas na realizację zamówień | 4 | 40 zł |
| Większa Porcja | Więcej porcji mięsa z jednego ścięcia | 3 | 55 zł |

### Architektura sklepu

- **`ShopManager`** — logika zakupów, definicje ulepszeń, obliczanie kosztów (skalowanie wykładnicze: `baseCost * costScaling^level`)
- **`ShopUI`** — interfejs graficzny tworzony programistycznie (bez prefabów), z animacjami otwarcia/zamknięcia i wizualnym feedbackiem zakupów
- **Synchronizacja sieciowa** — poziomy ulepszeń przechowywane w `NetworkVariable<int>`, zakupy realizowane przez `ServerRpc`

### Wpływ ulepszeń na gameplay

Ulepszenia modyfikują parametry stacji kuchennych w czasie rzeczywistym:
- Mnożnik prędkości przetwarzania: `KitchenStation.GetShopSpeedMultiplier()`
- Mnożnik nagrody: `OrderManager.CalculateReward()` używa `ShopManager.GetRewardMultiplier()`
- Bonus czasu: `OrderManager.NoweZamowienie()` dodaje `ShopManager.GetOrderTimeBonus()`

---

## 4. Tryb multiplayer

### Architektura sieciowa

Gra wykorzystuje **Unity Netcode for GameObjects** z transportem **UnityTransport** (UDP, port 7777).

**Komponenty sieciowe:**

| Komponent | Odpowiedzialność |
|-----------|-----------------|
| `NetworkSetup` | Konfiguracja NetworkManager, tworzenie prefabu gracza, obsługa połączeń |
| `NetworkPlayer` | Wrapper sieciowy gracza — kamera, input, wizualizacja zdalnych graczy |
| `NetworkKitchenStation` | Server-authoritative synchronizacja stanu stacji kuchennych |
| `NetworkItemSerializer` | Serializacja `KitchenItem` do struktury `NetworkItemState` (INetworkSerializable) |
| `LobbyUI` | Interfejs lobby — Host/Join/Disconnect, wyświetlanie statusu połączenia |

**Model autorytarnego serwera:**
1. Klient wysyła żądanie interakcji przez `InteractServerRpc(clientId, heldItemState)`
2. Serwer przetwarza interakcję lokalnie na `KitchenStation`
3. Serwer synchronizuje zaktualizowany stan stacji przez `NetworkVariable`
4. Serwer odsyła feedback i zaktualizowany held item do klienta przez `ClientRpc`

### Obsługa graczy

- Do 4 graczy w sieci LAN
- Każdy gracz ma unikalne spawn point i kolor ciała
- Zdalni gracze widoczni jako kolorowe kapsuły z etykietą imienia
- Etykiety automatycznie zwracają się ku kamerze (`BillboardLabel`)

---

## 5. Efekty wizualne

### System cząsteczkowy (`VFXManager`)

Centralny manager efektów wizualnych tworzący `ParticleSystem` programistycznie:

| Efekt | Opis | Wyzwalacz |
|-------|------|-----------|
| Steam | Biała para unosząca się nad grillem | Rozpoczęcie ścinania mięsa z donera |
| Chop | Kolorowe odpryski składników | Rozpoczęcie krojenia warzyw |
| Money | Złote cząsteczki wznoszące się | Udana dostawa zamówienia |
| DeliverySuccess | Zielony rozbłysk | Udana dostawa zamówienia |
| DeliveryFail | Czerwony rozbłysk | Nieudana dostawa |

### Efekty UI

- **Floating Money Text** — animowany tekst „+X zł" unoszący się od środka ekranu z efektem fade-out
- **Balance Pulse** — pulsowanie koloru tekstu salda z zielonego do białego przy zmianie wartości
- **Station Pulse** — subtelne pulsowanie skali stacji z gotowym przedmiotem do odebrania

---

## 6. Optymalizacja i poprawki synchronizacji sieciowej

### Naprawione problemy

1. **Wyciek pamięci delegatów** (`ShopManager`) — Lambda expressions w `OnNetworkDespawn` tworzyły nowe instancje delegatów, przez co `-=` nie odsubskrybowywało oryginalnych listenerów. Zastąpiono lambdy cached metodami.

2. **Brak NetworkObject na stacjach** (`KitchenGameBootstrap`) — Dynamicznie tworzone stacje miały `NetworkKitchenStation` (NetworkBehaviour) bez wymaganego `NetworkObject`, co powodowało ciche błędy w `IsServer`/`IsOwner` i RPCs.

3. **Nadmierny ruch sieciowy** (`NetworkKitchenStation`) — Stan stacji był synchronizowany co klatkę bez sprawdzania zmian. Dodano dirty-checking i throttling (max co 0.1s), co redukuje ruch sieciowy o ~90%.

4. **Nadmierny ruch sieciowy held item** (`NetworkPlayer`) — Held item był synchronizowany co klatkę. Dodano throttling (max co 0.15s).

5. **Brak routingu sieciowego interakcji** (`PlayerInteraction`) — Klient wywoływał `Interact()` bezpośrednio zamiast przez `InteractServerRpc()`. Dodano detekcję trybu multiplayer i automatyczny routing przez serwer.

---

## 7. Testy jednostkowe

### Istniejące testy (Kamień Milowy 1)

- `ValidatorAcceptsDishWithExactIngredients` — walidacja poprawnego kebaba
- `ValidatorRejectsWrongPreparationState` — odrzucenie złego stanu przygotowania
- `SaveDataRoundTripPreservesProgress` — serializacja/deserializacja postępu
- `ShopSaveDataRoundTripPreservesUpgrades` — serializacja ulepszeń
- `GameSaveDataIncludesShopField` — struktura danych zapisu
- `UpgradeDefinitionCostScalesCorrectly` — skalowanie kosztów ulepszeń
- `UpgradeTypeEnumHasFiveValues` — enum ulepszeń

### Nowe testy (Kamień Milowy 2)

- `ShopUpgradeCostScalesExponentially` — weryfikacja wykładniczego skalowania kosztów
- `ShopEffectDescriptionReturnsCorrectText` — opisy efektów ulepszeń
- `ShopMaxLevelBlocksFurtherUpgradeCheck` — blokada na max poziomie
- `EconomySaveDataRoundTrip` — serializacja danych ekonomicznych
- `NetworkItemStateEmptyRoundTrip` — serializacja pustego stanu sieciowego
- `NetworkItemStateDishRoundTrip` — serializacja dania z zawartością
- `ValidatorRejectsExtraIngredients` — odrzucenie nadmiarowych składników
- `ValidatorRejectsNonDishItem` — odrzucenie nie-dania
- `OrderClonePreservesAllFields` — głębokie klonowanie zamówień

**Łącznie: 16 testów jednostkowych** (7 z KM1 + 9 nowych)

---

## 8. Struktura plików projektu

```
Assets/Scripts/
├── EconomyManager.cs          — system ekonomiczny (singleton, NetworkBehaviour)
├── ShopManager.cs             — logika sklepu ulepszeń
├── ShopUI.cs                  — interfejs sklepu (programistyczny uGUI)
├── OrderManager.cs            — zarządzanie zamówieniami
├── Order.cs                   — model zamówienia
├── KitchenStation.cs          — logika stacji kuchennych + VFX integracja
├── KitchenGameBootstrap.cs    — inicjalizacja sceny i środowiska
├── KitchenGameModels.cs       — modele danych (KitchenItem, IngredientRequirement, etc.)
├── KitchenHUD.cs              — HUD gracza + floating money text
├── KitchenOrderBoard.cs       — tablica zamówień (monitor 3D)
├── VFXManager.cs              — system cząsteczkowy (Steam, Chop, Money, Delivery)
├── NetworkSetup.cs            — konfiguracja NetworkManager
├── NetworkPlayer.cs           — wrapper sieciowy gracza
├── NetworkKitchenStation.cs   — sync sieciowy stacji (dirty-checking + throttling)
├── NetworkItemSerializer.cs   — serializacja KitchenItem <-> NetworkItemState
├── PlayerInteraction.cs       — interakcje z obiektami (z routingiem sieciowym)
├── SimplePlayerController.cs  — kontroler FPS
├── SaveManager.cs             — zapis/odczyt stanu gry
├── Interactable.cs            — bazowa klasa interakcji
├── IngredientData.cs          — ScriptableObject składnika
└── DumpHierarchy.cs           — narzędzie debugujące

Assets/Tests/EditMode/
└── KitchenGameTests.cs        — 16 testów jednostkowych
```

---

## 9. Znane ograniczenia

1. **Dynamiczne NetworkObject** — stacje kuchenne tworzone są w runtime, co może powodować problemy z rejestracją prefabów sieciowych w niektórych konfiguracjach NGO
2. **Brak dedykowanego serwera** — gra działa w modelu Host+Client (jeden gracz jest jednocześnie serwerem)
3. **Interfejs UI** — tworzony programistycznie bez prefabów, co utrudnia modyfikacje wizualne w edytorze Unity

---

## 10. Podsumowanie

Wszystkie cele drugiego kamienia milowego zostały zrealizowane. Gra posiada funkcjonalny system ekonomiczny z dynamicznym obliczaniem nagród, sklep z 5 typami ulepszeń wpływającymi na gameplay, podstawowy tryb multiplayer obsługujący do 4 graczy w sieci LAN, system efektów wizualnych oparty na cząsteczkach, oraz zoptymalizowaną synchronizację sieciową. Poprawność kluczowych systemów weryfikowana jest przez 16 testów jednostkowych.
