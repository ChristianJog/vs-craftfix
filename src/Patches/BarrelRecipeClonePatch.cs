using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace CraftFix.Patches;

/// <summary>
/// The handbook builds the barrel recipes of a page from CraftingRecipeIngredient.ResolvedItemStack
/// without cloning it. For these ingredients the getter returns a stack whose attributes are the
/// static CraftingRecipeIngredient.defaultEmptyAttributes. Drawing such a stack updates its
/// transition state and thereby writes into that static field, which nearly all recipe ingredients
/// of the server share in singleplayer: the server no longer finds a recipe.
///
/// The patch runs after BuildBarrelRecipesText and replaces the stacks of the item components it
/// returned with clones, before anything is drawn. A clone has its own attributes.
///
/// Everything in the mod that uses Harmony lives in this folder.
/// </summary>
public static class BarrelRecipeClonePatch
{
    private const string HarmonyId = "craftfix.barrelrecipeclone";
    private const string TargetMethod = "BuildBarrelRecipesText";

    private static Harmony? harmony;
    private static ILogger? logger;
    private static bool reported;

    public static void Install(ILogger log)
    {
        if (harmony != null) return;
        logger = log;
        reported = false;

        MethodInfo? target = AccessTools.Method(typeof(CollectibleBehaviorHandbookTextAndExtraInfo), TargetMethod);
        if (target == null)
        {
            log.Error($"[CraftFix] {nameof(CollectibleBehaviorHandbookTextAndExtraInfo)}.{TargetMethod} not found, the fix is not active.");
            return;
        }

        try
        {
            var patcher = new Harmony(HarmonyId);
            patcher.Patch(target, postfix: new HarmonyMethod(typeof(BarrelRecipeClonePatch).GetMethod(nameof(CloneStacks), BindingFlags.Static | BindingFlags.NonPublic)));
            harmony = patcher;
            log.Notification($"[CraftFix] patched {nameof(CollectibleBehaviorHandbookTextAndExtraInfo)}.{TargetMethod}.");
        }
        catch (Exception e)
        {
            log.Error($"[CraftFix] patching {TargetMethod} failed, the fix is not active: {e.GetType().Name}: {e.Message}");
        }
    }

    public static void Uninstall()
    {
        harmony?.UnpatchAll(HarmonyId);
        harmony = null;
        logger = null;
    }

    private static void CloneStacks(List<RichTextComponentBase>? __result)
    {
        if (__result == null) return;

        int cloned = 0;
        foreach (RichTextComponentBase component in __result)
        {
            if (component is not SlideshowItemstackTextComponent slideshow || slideshow.Itemstacks == null) continue;

            // The arrays were created by BuildBarrelRecipesText for this component alone.
            ItemStack[] stacks = slideshow.Itemstacks;
            for (int i = 0; i < stacks.Length; i++)
            {
                if (stacks[i] == null) continue;
                stacks[i] = stacks[i].Clone();
                cloned++;
            }
        }

        if (cloned == 0 || reported) return;
        reported = true;
        logger?.Notification($"[CraftFix] first barrel recipe page: {cloned} item stacks replaced by clones.");
    }
}
