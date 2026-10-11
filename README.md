# CraftFix

A small client-side mod for Vintage Story 1.22. It fixes the cause of the "Uncraftable items" bug
([VintageStory-Issues#9175](https://github.com/anegostudios/VintageStory-Issues/issues/9175))
that is triggered by the handbook: after looking at certain handbook pages, the crafting grid no
longer produces a result until the world is reloaded.

**Just want to use it, without any programming?** Go to the
[quick start](#quick-start-no-programming-needed) at the end of this page.

## Background

Since 1.22 all grid recipes share one `ItemStack` per distinct ingredient. The attributes of such
a stack normally are the static field `CraftingRecipeIngredient.defaultEmptyAttributes`, which has
to stay empty. If anything writes to that field, the server's recipe search fails for nearly every
ingredient and no longer finds a recipe. The client keeps matching, shows the result for a moment
and gets it cleared by the server.

The handbook is such a writer. `CollectibleBehaviorHandbookTextAndExtraInfo.BuildBarrelRecipesText`
takes `CraftingRecipeIngredient.ResolvedItemStack` of each barrel recipe ingredient and hands it to
the item display without cloning it. For these ingredients the getter returns a stack whose
attributes are the static field. Drawing an item that perishes, dries or otherwise changes over
time updates its `transitionstate`, and that write goes into the static field. In singleplayer the
client and the integrated server share the field, so a page in the handbook breaks crafting on the
server.

This happens on every handbook page whose item is the output of a barrel recipe with such an
ingredient, for example curdled milk, cottage cheese, cider, cured meat, pickled vegetables,
soaked hides or linen fiber pulp.

## What the mod does

One Harmony postfix on `BuildBarrelRecipesText`. After the game has built the barrel recipes of a
handbook page, the patch replaces the item stacks of the returned item displays with clones, before
anything is drawn. A clone has attributes of its own, so drawing it no longer touches the static
field. This is the missing `Clone()`, applied from outside.

The patch is the only use of Harmony in the mod and lives in `src/Patches/`. The mod has no
commands, no settings and no files of its own. It loads on the client only.

When the patch is in place, `client-main.log` contains

    [CraftFix] patched CollectibleBehaviorHandbookTextAndExtraInfo.BuildBarrelRecipesText.

and, when the first handbook page with a barrel recipe is opened,

    [CraftFix] first barrel recipe page: <n> item stacks replaced by clones.

If the game changes and the method can no longer be patched, the mod logs an error and does
nothing else.

## What it does not do

- It fixes the handbook as a trigger, nothing else. If something else writes to the static field,
  for example another mod, crafting can still break. The diagnostic mod
  [CraftWatch](https://github.com/ChristianJog/vs-craftwatch) logs who writes to the field and can
  undo the damage for the running session with `/cw repair`. Both mods can be used together.
- It does not repair a session in which crafting is already broken. Reload the world.

## Status

Tested against Vintage Story 1.22.7 in singleplayer on Windows, observed with CraftWatch:

- Handbook pages from nine of the fourteen groups of pages that trigger the bug: curdled milk,
  cottage cheese, fruit cider, cured meat, pickled vegetables, pickled soybeans, soaked hides,
  taxidermy fish and linen fiber pulp. Without the mod such pages write to the static field and
  break crafting (measured for seven of these nine); with the mod there was no write and crafting
  kept working. This includes a page left open for almost a minute and the tooltips of the
  ingredients.
- The barrel recipes in the handbook are displayed as usual.
- Together with the mods Enhanced Handbook 1.8.0 and PomNotes 0.7.5.

Not tested: the remaining groups of pages (grain cider, soaked cassava, the orange, yellow, pink
and purple dyes), which go through the same method; "Open to LAN"; dedicated servers; other game
versions. The mod depends on the name and result type of one method of the game's survival mod,
which may change with any game update.

## Build

Requires the .NET 10 SDK and the game installed at `%APPDATA%\Vintagestory`.

    dotnet build -c Release

The result is in `bin\craftfix\`. Zip `CraftFix.dll`, `modinfo.json` and `LICENSE` and put the zip
into `%APPDATA%\VintagestoryData\Mods`. For a different install location of the game pass
`-p:VSPath=...`.

## License

MIT, see [LICENSE](LICENSE).

## Quick start (no programming needed)

This is for players who want crafting to stop breaking after they used the handbook. You do not
need to build anything. The steps are written for Windows.

**1. Download the mod**

Open the [latest release](https://github.com/ChristianJog/vs-craftfix/releases/latest) and
download the file `craftfix_<version>.zip`. Do not unpack it.

**2. Put it into your Mods folder**

Close Vintage Story. Press `Win` + `R`, paste the following line and press Enter:

    %APPDATA%\VintagestoryData\Mods

A folder opens. Move the downloaded zip file into it.

**3. Start the game and play**

Start Vintage Story. In the Mod Manager, CraftFix should be listed and switched on. Then play as
usual. There is nothing to configure and nothing to type; the mod works from the moment a world is
loaded.

**How do I know it works?**

1. Open the handbook, go to the page for curdled milk and wait a few seconds. Close the handbook.
2. Put a flint spear head and a stick into the crafting grid.

The spear appears and can be taken out. Without the mod it does not appear, or only flashes for a
moment.

**Do I need it in multiplayer?**

The mod only has to be installed on your own computer; a server does not need it. The bug as
described here needs the game and the server to run in the same program, which is the case in
singleplayer and for the player who hosts an "Open to LAN" game. Multiplayer has not been tested.

**Crafting is broken although the mod is installed**

Reloading the world gets crafting back. The handbook is one cause of this bug, and there may be
others that this mod does not cover. If it happens to you, the diagnostic mod
[CraftWatch](https://github.com/ChristianJog/vs-craftwatch) can record what caused it.

**Removing the mod**

Delete the zip file from the Mods folder.
