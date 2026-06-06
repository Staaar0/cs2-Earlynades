# EarlyNades

A Counter-Strike 2 server plugin that lets players throw grenades a little sooner
after switching to one. CS2 normally forces about a **1.0 second** wait between
pulling out a nade and being able to throw it; this plugin shortens that to
**0.8 seconds** (configurable).

It is built on **CounterStrikeSharp**, which runs as a Metamod:Source plugin —
so the install order on the server is Metamod → CounterStrikeSharp → this plugin.

---

## Build on GitHub (no local setup needed)

1. Create a new GitHub repository and upload these files, keeping the structure:

   ```
   EarlyNades/EarlyNades.cs
   EarlyNades/EarlyNades.csproj
   .github/workflows/build.yml
   README.md
   ```

2. Push. The **Build EarlyNades** Action runs automatically.
3. Open the run → **Artifacts** → download `EarlyNades`.
4. Inside you only need **`EarlyNades.dll`**.

(To build locally instead: install the .NET 8 SDK and run
`dotnet build EarlyNades/EarlyNades.csproj -c Release`. The dll lands in
`EarlyNades/bin/Release/net8.0/`.)

---

## Install on your server

Requires Metamod:Source and CounterStrikeSharp already installed.

Put the dll here:

```
game/csgo/addons/counterstrikesharp/plugins/EarlyNades/EarlyNades.dll
```

Then restart the server, or from the server console:

```
css_plugins load EarlyNades
```

---

## Tuning the delay

Default is `0.8`. Change it without recompiling, from the server console or rcon:

```
css_earlynades_delay 0.8     // set to 0.8 seconds
css_earlynades_delay         // show the current value
```

Allowed range is 0.0–2.0 seconds. To bake a different default in, edit
`_throwDelay` near the top of `EarlyNades.cs` and rebuild.

The plugin only ever **shortens** the wait — it never makes a nade slower than
the game already allows.

---

## Notes / caveats

- This affects **every player on the server equally** (it's a server-side rule,
  not a per-client advantage).
- It changes the deploy-to-throw delay (the wait after you switch to the nade).
  Smoke/molotov fire-extinguish timing is a separate game mechanic and is not
  touched.
- CounterStrikeSharp's schema field names occasionally change between major
  updates. If a future CS2/CSSharp update makes the throw timing stop changing,
  the two lines to check are the weapon timing fields in `OnTick`
  (`NextPrimaryAttackTick` / `m_nNextPrimaryAttackTick`); update them to match
  the current CSSharp API and rebuild.
- If `css_earlynades_delay` returns "Unknown command", the plugin didn't load —
  check the CSSharp console output on startup.
