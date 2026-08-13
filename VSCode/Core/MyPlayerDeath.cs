using FortRise;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// En soccer, PERSONNE ne meurt.
  ///
  /// Sans fleche, on croyait le probleme regle : il ne l'etait pas. Tomber sur la
  /// tete d'un adversaire le TUE dans TowerFall - c'est HurtBouncedOn, appele depuis
  /// PlayerOnPlayer - et une tour a par ailleurs sa lave, ses pointes et ses
  /// ecraseurs. Ici, tomber sur une tete prend le ballon (voir MyPlayerOnPlayer) et
  /// rien de plus.
  ///
  /// Toutes les morts passent par <c>Player.Die(DeathCause, int, bool, bool)</c>.
  /// C'est donc lui que l'on coupe, et avec lui les cinq appelants qui utilisent la
  /// depouille qu'il rend : leur laisser un null les ferait tomber sur une reference
  /// nulle juste apres. Couper les deux niveaux est ce qui rend l'archer reellement
  /// increvable, quelle que soit la cause.
  /// </summary>
  public class MyPlayerDeath : IHookable
  {
    public static void Load(IHarmony harmony)
    {
      // Le coeur : toutes les morts finissent la.
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Player), nameof(Player.Die),
              [typeof(DeathCause), typeof(int), typeof(bool), typeof(bool)]),
          prefix: new HarmonyMethod(Die_patch)
      );

      // Les appelants qui deferencent la depouille. Chacun rend void, on peut donc
      // les sauter entierement.
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Player), nameof(Player.HurtBouncedOn)),
          prefix: new HarmonyMethod(Block_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Player), nameof(Player.Die), [typeof(Arrow)]),
          prefix: new HarmonyMethod(Block_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Player), nameof(Player.HurtDelayed), [typeof(Arrow)]),
          prefix: new HarmonyMethod(Block_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Player), nameof(Player.Hurt), [typeof(Lava)]),
          prefix: new HarmonyMethod(Block_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Player), nameof(Player.Hurt),
              [typeof(Explosion), typeof(Vector2)]),
          prefix: new HarmonyMethod(Block_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Player), nameof(Player.Hurt),
              [typeof(DeathCause), typeof(Vector2), typeof(int), typeof(SFX)]),
          prefix: new HarmonyMethod(Block_patch)
      );

      // L'ecrasement : DoSquish pousse l'archer hors du solide et le tue quand il
      // n'y arrive pas. Methode privee, d'ou le nom en dur.
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Player), "DoSquish"),
          prefix: new HarmonyMethod(Block_patch)
      );
    }

    /// <summary>
    /// Rendre false saute la mort. __result est laisse a null : plus aucun appelant
    /// ne le lit, tous ceux qui s'en servaient sont coupes en amont.
    /// </summary>
    public static bool Die_patch(ref PlayerCorpse __result)
    {
      if (!SoccerMatch.Active)
      {
        return true;
      }

      __result = null;
      return false;
    }

    public static bool Block_patch()
    {
      return !SoccerMatch.Active;
    }
  }
}
