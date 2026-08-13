using System;
using System.Diagnostics;
using FortRise;
using Microsoft.Extensions.Logging;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// Le soccer : un ballon, deux buts, pas une seule fleche.
  ///
  /// Le ballon se prend en marchant dessus, se garde devant soi, se frappe au
  /// bouton de tir - d'autant plus fort qu'on l'a maintenu - et se perd quand on se
  /// fait bousculer ou pietiner. Il n'y a plus rien pour tuer : la manche se gagne
  /// en marquant.
  /// </summary>
  public class TFModFortRiseGameModeSoccerModule : Mod
  {
    public static TFModFortRiseGameModeSoccerModule Instance;

    // TextureRegistry en premier : le mode versus reference son illustration.
    private static Type[] Registerables = [
        typeof(TextureRegistry),
        typeof(SoccerGameMode),
    ];

    internal Type[] Hookables = [
        typeof(MyPlayer),
        typeof(MyPlayerOnPlayer),
        typeof(MyPlayerDeath),
        typeof(MyArrowHUD),
    ];

    public TFModFortRiseGameModeSoccerModule(IModContent content, IModuleContext context, ILogger logger)
        : base(content, context, logger)
    {
      if (!Debugger.IsAttached)
      {
        //Debugger.Launch(); // Proposera d'attacher Visual Studio
      }

      Instance = this;
      TFModFortRiseGameModeSoccer.Logger.Init(logger);

      foreach (var hookable in Hookables)
      {
        hookable.GetMethod(nameof(IHookable.Load))!.Invoke(null, [context.Harmony]);
      }

      foreach (var registerable in Registerables)
      {
        registerable.GetMethod(nameof(IRegisterable.Register))!.Invoke(null, [content, context.Registry]);
      }
    }

    /// <summary>
    /// Ce que les autres mods peuvent savoir du match : ou est le ballon, qui le
    /// porte, et vers quel but aller. L'IA en a besoin pour jouer au foot plutot que
    /// de poursuivre le joueur le plus proche.
    /// </summary>
    public override object GetApi()
    {
      return new ApiImplementation();
    }

  }
}
