# Auto Shopping (v0.11.2)

In-store shopping assistant for [Big Ambitions](https://store.steampowered.com/app/1331550/Big_Ambitions/). When you enter a supported store, the mod scans every purchasable shelf, builds a live catalog, and lets you plan a shopping run from a game-style HUD panel. Your character then walks, picks items, and checks out automatically.

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

- **Toggle HUD** (compact side panel) — **Show** / **Hide** the main shopping panel.
- **Shopping panel** (main catalog) — search, sort, quantity controls, container bar, totals, and actions.

### Catalog

- Live scan of all purchasable items in the current building
- Icon, display name, unit or box price, and stock state (out-of-stock items are blocked)
- **Search** bar to filter by name
- **Sort** by item name or price (click column headers: none → ascending → descending)
- **Way to** — draws a NavMesh route on the floor to the nearest shelf for that item; the button turns green while active, click again to clear the route
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
| **F8** | Toggle the main shopping panel (ignored while the search field is focused) |
| Toggle HUD button | Show / hide the main panel |
| Panel close button | Hide the main panel |

## Mod options

Available in the in-game mod options menu:

| Option | Default | Description |
|--------|---------|-------------|
| **Open AutoShopping on entering shop** | On | Open the main panel when you enter a supported store |
| **Auto cart on entering shop** | On | Automatically walk to and pick up the best available container on entry |

## Localization

Built-in strings for **English** and **French**, following the game's active locale. Money and number formatting use the same locale rules as the base game.

## Requirements

- Big Ambitions with mod support (SDK 0.11+)
- Install the compiled mod folder into `ModsLocal/AutoShopping/` (see below)
- **LIB_BaUnifiedUI** is bundled under `Dependencies/` at build time (no separate Workshop subscription)

## Build & install

From the `bigambitions` project root:

```powershell
powershell -NoProfile -File bigambitions/scripts/compile-install-auto-shopping.ps1
```

This compiles a player-mode `AutoShopping.dll`, copies locales and thumbnail, and installs to:

`%USERPROFILE%\AppData\LocalLow\Hovgaard Games\Big Ambitions\ModsLocal\AutoShopping\`

Restart the game or reload the city after installing.

## License

MIT — see [LICENSE](LICENSE).
