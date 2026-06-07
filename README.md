# EarlyNades

A CS2 plugin that allow players to throw grenades a little sooner
after switching to one. CS2 normally forces about a **1.0 second** wait between
pulling out a nade and being able to throw it; this plugin shortens that to
**0.8 seconds** (configurable).

---

## Install

Put the dll here:

```
addons/counterstrikesharp/plugins/EarlyNades/EarlyNades.dll
```

Then restart the server, or from the server console:

```
css_plugins load EarlyNades
```

---

## Tuning the delay

Default is `0.8`.

```
css_earlynades_delay 0.8     // set to 0.8 seconds
css_earlynades_delay         // show the current value
```

Allowed range is 0.0–2.0 seconds.

---
