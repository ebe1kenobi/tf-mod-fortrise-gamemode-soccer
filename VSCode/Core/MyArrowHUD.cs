using FortRise;
using HarmonyLib;
using TowerFall;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// Efface tout ce qui parle de fleches : le compteur au-dessus de la tete, et l'arc
  /// que l'archer bande en visant.
  ///
  /// Il ne s'agit pas seulement de proprete. Le bouton de tir sert ici a frapper, et
  /// le jeu accompagne toujours ce geste de ses images d'arc : une fleche encochee
  /// apparait a chaque coup de pied. Elle annonce un tir qui n'arrivera jamais.
  ///
  /// C'est <c>ArrowHUD.Render</c> qui est saute, et non <c>Player.HUDRender</c> qui
  /// l'appelle : cette derniere tient en trois lignes, elle est appelee cinq fois de
  /// suite dans la meme boucle, et une methode de cette taille se fait recopier dans
  /// son appelant. La patcher n'aurait rien change - et n'aurait rien dit non plus.
  /// Prendre celle du dessous evite le pari, et laisse au passage l'indicateur de
  /// joueur s'afficher tout seul, alors qu'il fallait le redessiner a la main.
  /// </summary>
  public class MyArrowHUD : IHookable
  {
    public static void Load(IHarmony harmony)
    {
      // Prefix, pour POUVOIR sauter le rendu vanilla - un postfix ne le permet pas.
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(ArrowHUD), nameof(ArrowHUD.Render)),
          prefix: new HarmonyMethod(Render_patch)
      );
    }

    public static bool Render_patch()
    {
      return !SoccerMatch.Active;
    }
  }
}
