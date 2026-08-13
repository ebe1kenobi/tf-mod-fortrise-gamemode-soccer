using System;
using FortRise;
using HarmonyLib;
using Monocle;
using TowerFall;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// Se bousculer prend le ballon. Pietiner ne fait rien.
  ///
  /// <c>Player.PlayerOnPlayer</c> est l'endroit exact ou le jeu tranche deja ce
  /// genre de contact : il y distingue le PIETINEMENT (l'un tombe sur la tete de
  /// l'autre) de la BOUSCULADE laterale, et c'est la qu'il fait deja changer une
  /// fleche de main - <c>StealArrow</c> pour l'adversaire, <c>TradeArrow</c> pour
  /// le coequipier. Le ballon suit exactement la regle de la FLECHE VOLEE, au meme
  /// endroit : rien a deviner sur les vitesses ou les hitbox, le jeu a deja fait ce
  /// travail.
  ///
  /// Seule la bousculade compte donc. Tomber sur une tete ne prend rien : c'est un
  /// rebond, et il doit le rester - sinon sauter sur l'adversaire deviendrait la
  /// facon la plus simple de recuperer le ballon, et le soccer se jouerait en l'air.
  ///
  /// La methode est privee et statique, d'ou le nom en dur. Elle est trop grosse
  /// pour etre incrustee par le JIT : un postfix la voit reellement passer.
  /// </summary>
  public class MyPlayerOnPlayer : IHookable
  {
    /// <summary>
    /// Ecart vertical en dessous duquel le jeu considere un contact comme LATERAL
    /// (voir PlayerOnPlayer). Repris tel quel pour trancher pareil que lui.
    /// </summary>
    private const float STOMP_GAP = 10f;

    public static void Load(IHarmony harmony)
    {
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Player), "PlayerOnPlayer"),
          postfix: new HarmonyMethod(PlayerOnPlayer_patch)
      );
    }

    public static void PlayerOnPlayer_patch(Player a, Player b)
    {
      try
      {
        if (!SoccerMatch.Active)
        {
          return;
        }

        Ball ball = SoccerMatch.Ball;
        Player carrier = ball.Carrier;
        if (carrier == null || (carrier != a && carrier != b))
        {
          return;
        }

        Player other = carrier == a ? b : a;
        if (other.Dead)
        {
          return;
        }

        // Le meme calcul de hauteur que le jeu, bouclage de l'ecran compris : sans
        // lui, deux archers de part et d'autre du bord haut seraient vus a 200
        // pixels l'un de l'autre alors qu'ils se touchent.
        float carrierTop = carrier.Top - carrier.Speed.Y;
        float otherTop = other.Top - other.Speed.Y;
        if (Math.Abs(carrierTop - otherTop) > 200f)
        {
          if (carrierTop > otherTop)
          {
            carrierTop -= 240f;
          }
          else
          {
            otherTop -= 240f;
          }
        }

        // Contact vertical : RIEN. Marcher sur la tete de quelqu'un ne le tue plus
        // (voir MyPlayerDeath) et ne lui prend pas le ballon non plus - on rebondit,
        // c'est tout. Le ballon ne change de pied que par une bousculade franche,
        // comme une fleche qu'on se fait voler.
        if (Math.Abs(carrierTop - otherTop) >= STOMP_GAP)
        {
          return;
        }

        // Contact lateral : celui qui POUSSE vers l'autre prend le ballon, comme le
        // jeu fait deja pour une fleche. Si les deux poussent l'un vers l'autre,
        // personne ne prend rien - ils se repoussent, et c'est tout.
        int towardCarrier = Math.Sign(WrapMath.DiffX(other.X, carrier.X));
        int towardOther = Math.Sign(WrapMath.DiffX(carrier.X, other.X));

        bool otherPushes = MoveXOf(other) == towardCarrier && towardCarrier != 0;
        bool carrierPushes = MoveXOf(carrier) == towardOther && towardOther != 0;

        if (otherPushes && !carrierPushes)
        {
          Steal(ball, other);
        }
      }
      catch (Exception e)
      {
        Logger.Error("MyPlayerOnPlayer: " + e);
      }
    }

    private static void Steal(Ball ball, Player taker)
    {
      ball.Take(taker);
      Sounds.char_arrowCollide.Play(taker.X, 1f);
      TFGame.PlayerInputs[taker.PlayerIndex]?.Rumble(0.4f, 15);
    }

    /// <summary>
    /// La direction horizontale demandee par un archer.
    ///
    /// Lue sur la manette et non sur le champ prive <c>input</c> du joueur : c'est
    /// la meme information, et elle n'oblige pas a fouiller l'objet par reflexion a
    /// chaque contact.
    /// </summary>
    private static int MoveXOf(Player player)
    {
      int index = player.PlayerIndex;
      if (TFGame.PlayerInputs == null || index < 0 || index >= TFGame.PlayerInputs.Length)
      {
        return 0;
      }

      PlayerInput input = TFGame.PlayerInputs[index];
      return input == null ? 0 : input.GetState().MoveX;
    }
  }
}
