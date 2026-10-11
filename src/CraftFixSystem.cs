using Vintagestory.API.Client;
using Vintagestory.API.Common;
using CraftFix.Patches;

namespace CraftFix;

/// <summary>
/// Client-side fix for the handbook as a cause of "uncraftable items" (VintageStory-Issues 9175).
/// The mod has no state and no commands; all it does is install one Harmony patch.
/// </summary>
public class CraftFixSystem : ModSystem
{
    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    public override void StartClientSide(ICoreClientAPI api) => BarrelRecipeClonePatch.Install(api.Logger);

    public override void Dispose() => BarrelRecipeClonePatch.Uninstall();
}
