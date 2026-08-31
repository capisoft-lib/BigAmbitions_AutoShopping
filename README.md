# Auto Shopping (v1.0.1)

In-store shopping assistant for [Big Ambitions](https://store.steampowered.com/app/1331550/Big_Ambitions/). When you enter a supported store, the mod scans every purchasable shelf, builds a live catalog, and lets you plan a shopping run from a game-style HUD panel. Your character then walks, picks items, and checks out automatically.

> ☕ If Auto Shopping has saved you from another lap around the supermarket, [buy me a coffee](https://buymeacoffee.com/capitaine). The mod can automate the shopping; sadly, it still cannot brew one.

| Property | Value |
|---|---|
| **Version** | `1.0.1` |
| **Game** | Big Ambitions **EA 0.11** and **1.0** |
| **Requires** | **LIB BA Unified UI 1.0.0+** as a separate mod |
| **Unity** | `2022.3.62f2` with the official Big Ambitions Mod Builder |

## What's new in 1.0.1

- Added the 19 missing game languages, bringing coverage to all 22 languages, with 69 translated strings per language.
- Includes translated interface labels, status and error messages, mod options, and shortcut controls.
- Uses the game's native locale codes, including Brazilian Portuguese (`pt`) and both Chinese variants (`zh-cn`, `zh-tw`).
- Retains the draggable BAUI panels, customizable shelf route color, four configurable shortcuts, and automatic shopping flow.

Steam release text: [English description](releases/1.0.1/full-description.md), [French description](releases/1.0.1/full-description.fr.md), [English changelog](releases/1.0.1/Steam_ChangeLog.md), [French changelog](releases/1.0.1/Steam_ChangeLog.fr.md).

## Supported stores

| Store type | Examples | Container |
|------------|----------|-----------|
| **Retail self-service** | Supermarkets, convenience stores | Basket or shopping cart |
| **Wholesale** | Metro-style warehouses | Hand truck (up to 8 slots) |
| **Cinema / theater** | Concessions | Paper-bag flow (limited) |

**Not supported:** restaurants, hairdressers, nightclubs, casinos, and any cashier-only business where the player cannot pick items from shelves.

## Quick start

1. Walk into a supported store — the catalog loads automatically.
2. Take a container (**Pick basket** / **Pick container**) or enable **Auto cart on entering shop** in mod options.
3. Use **+** / **−** to set desired quantities; the character walks to shelves and picks or drops items.
4. Press **Pay** to walk to the register and complete the purchase.

## Interface

Two HUD elements appear while you are inside a supported store (hidden during pause menu, smartphone, interior designer, checkout UI, etc.):

- **Toggle HUD** (370 px action panel matching VoogleRoute) — **Show** / **Hide** the main shopping panel.
- **Shopping panel** (740 px BAUI main panel) — search, sort, quantity controls, container bar, totals, and actions; drag its header to move it and keep the saved position.

### Catalog

- Live scan of all purchasable items in the current building
- Icon, display name, unit or box price, and stock state (out-of-stock items are blocked)
- **Search** bar to filter by name
- **Sort** by item name or price (click column headers: none → ascending → descending)
- **Way to** — draws a thin NavMesh route on the floor to the nearest shelf on both supported game versions; its color is configurable (cyan by default), the button turns green while active, and clicking again clears the route
- **+** / **−** — adjust desired quantity; picked quantity syncs as actions complete

### Container bar

Adapts to the store profile:

- **Basket** — small retail stores
- **Shopping cart** / **big cart** — larger self-service stores
- **Hand truck** — wholesale
- **Bare hands** — when no container is required or available

Buttons pick up or put down the active container. Slot usage (`current / max`) reflects container capacity and the mod's 8-item slot cap, whichever is lower.

### Footer

- Running **total** and bank **balance**
- **Clear list** — reset all desired quantities (drops excess picked items)
- **Pay** — queue checkout; walks to the nearest register and opens the purchase UI
- **Cancel actions** — stop the current automation queue
- Status line for the active action (walking, picking, paying, errors, etc.)

## Controls

| Input | Action |
|-------|--------|
| Configurable shortcut (**F8** by default) | Toggle the main shopping panel (ignored while the search field is focused) |
| Configurable **Clear list** shortcut | Invoke the footer Clear list action while the shopping panel is open |
| Configurable **Pay** shortcut | Invoke the footer Pay action while the shopping panel is open |
| Configurable **Cancel actions** shortcut | Invoke the footer Cancel actions action while the shopping panel is open |
| Toggle HUD button | Show / hide the main panel |
| Panel close button | Hide the main panel |

## Mod options

Available in the in-game mod options menu:

| Option | Default | Description |
|--------|---------|-------------|
| **Open AutoShopping on entering shop** | On | Open the main panel when you enter a supported store |
| **Auto cart on entering shop** | On | Automatically walk to and pick up the best available container on entry |
| **Way to path color** | Cyan | Pick and persist the floor-route color with the native game color picker |
| **Show / hide shortcut** | F8 | Rebind or clear the shortcut; the current binding is shown in the HUD button |
| **Clear list shortcut** | Unbound | Rebind the Clear list action; the active binding is shown in the footer button |
| **Pay shortcut** | Unbound | Rebind the Pay action; the active binding is shown in the footer button |
| **Cancel actions shortcut** | Unbound | Rebind the Cancel actions action; the active binding is shown in the footer button |

## Localization

Built-in translations for **all 22 game languages**, following the game's active locale: English, French, German, Spanish (Spain), Korean, Portuguese (Brazil), Russian, Simplified Chinese, Turkish, Italian, Traditional Chinese, Czech, Danish, Dutch, Finnish, Greek, Hungarian, Japanese, Polish, Romanian, Ukrainian, and Lithuanian. Money and number formatting use the same locale rules as the base game.

Every locale contains the same 69 UI, status, error, option, and shortcut strings. Locale filenames match the game's native codes, including `pt.json` for Brazilian Portuguese and `zh-cn.json` / `zh-tw.json` for Chinese.

## Requirements

- Big Ambitions EA 0.11 or 1.0 with mod support
- Install the compiled mod folder into `ModsLocal/AutoShopping/` (see below)
- **LIB BA Unified UI 1.0.0+** must be subscribed and enabled as a separate Workshop mod

## Build & install

Build `LIB_BaUnifiedUI` first to provide its compilation output, then build Auto Shopping. In game, subscribe to and enable **LIB BA Unified UI 1.0.0+** from the Workshop; do not install a duplicate local copy when using the Workshop version. The Auto Shopping package must not contain a `LIB_BaUnifiedUI*.dll` under `Dependencies/`.

The release build uses Unity `2022.3.62f2` and **Big Ambitions → Mod Builder → Build + Install**. Its headless equivalent, from the `bigambitions` project root, is:

```powershell
.\Assets\Mods\AutoShopping\tools\build-official.ps1
```

This produces the same official player package and installs it to:

`%USERPROFILE%\AppData\LocalLow\Hovgaard Games\Big Ambitions\ModsLocal\AutoShopping\`

The legacy fast compiler remains available for diagnostics only:

```powershell
.\scripts\compile-install-auto-shopping.ps1
```

Restart the game after installing to load the new mod DLL. A successful build and installation do not constitute a gameplay or visual validation, and do not publish the mod to Steam.

## License

MIT — see [LICENSE](LICENSE).
