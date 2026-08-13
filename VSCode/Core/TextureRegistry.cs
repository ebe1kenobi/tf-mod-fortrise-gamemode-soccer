using FortRise;

namespace TFModFortRiseGameModeSoccer;

public class TextureRegistry : IRegisterable
{
  /// <summary>Le ballon en jeu, 9x9.</summary>
  public static ISubtextureEntry Ball { get; private set; } = null!;

  /// <summary>L'illustration du mode, dans la liste des modes versus.</summary>
  public static ISubtextureEntry GameMode { get; private set; } = null!;

  public static void Register(IModContent content, IModRegistry registry)
  {
    Ball = registry.Subtextures.RegisterTexture(
        content.Root.GetRelativePath("Content/Atlas/ball.png")
    );
    GameMode = registry.Subtextures.RegisterTexture(
        content.Root.GetRelativePath("Content/Atlas/gamemode.png")
    );
  }
}
