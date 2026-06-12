# Auto Shopping (v0.11.0)

In-store shopping assistant for Big Ambitions. When you enter a supported store, the mod scans purchasable items and shows a catalog with icons, prices, and quantity controls.

## Supported stores

- Retail self-service (supermarkets, etc.) — use **Take basket**
- Wholesale (Metro, etc.) — use **Take hand truck** (up to 8 slots)
- Cinema / theater — limited support (paper bag flow)

Not supported: restaurants, hairdressers, nightclubs, casinos (cashier-only flow).

## Controls

- **F8** — toggle the Auto Shopping panel while inside a supported store
- Panel opens automatically on entry (toggle in mod options)

## Build & install

```powershell
powershell -NoProfile -File bigambitions/scripts/compile-install-auto-shopping.ps1
```

Restart the game or reload the city after installing to `ModsLocal/AutoShopping/`.

## Features

- Product list with icon, name, unit/box price, stock state
- `+` / `−` quantity — character auto-walks to pick or drop items
- Basket or hand truck acquisition buttons
- 8-item slot limit (or container capacity, whichever is lower)
- Running total and balance display
- **Pay** — auto-walk to checkout and submit order
- **Clear list** / **Cancel actions**
