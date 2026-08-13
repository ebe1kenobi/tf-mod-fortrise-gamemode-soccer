using System;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// L'etat d'une manche de soccer : le ballon, les deux buts, la charge de tir de
  /// chaque archer, et le but marque.
  ///
  /// Un seul endroit statique, remis a zero a chaque manche. Les patchs Harmony
  /// (tir, vol du ballon) n'ont pas d'autre moyen d'atteindre la manche en cours :
  /// ils ne recoivent qu'un Player.
  ///
  /// Attention : Monocle n'appelle ni Removed ni ne vide Scene en fin de manche
  /// (voir Layer.End). Un etat statique doit donc etre remis a zero explicitement
  /// au DEBUT de chaque manche, et pas seulement a sa fin.
  /// </summary>
  public static class SoccerMatch
  {
    /// <summary>Le ballon de la manche, ou null hors d'une manche de soccer.</summary>
    public static Ball Ball;

    public static Goal LeftGoal;
    public static Goal RightGoal;

    /// <summary>
    /// Images d'appui sur le bouton de tir, par archer.
    ///
    /// Huit et non quatre : WiderSet porte la partie a huit joueurs, et ce tableau est
    /// indexe par PlayerIndex. A quatre, le cinquieme joueur qui touchait le ballon
    /// faisait lever une IndexOutOfRange en pleine manche.
    /// </summary>
    private static readonly float[] charge = new float[8];

    /// <summary>Equipe qui vient de marquer, ou Neutral tant que rien n'est marque.</summary>
    public static Allegiance Scorer = Allegiance.Neutral;

    /// <summary>Vrai des qu'un but est entre : la manche est jouee.</summary>
    public static bool Scored;

    /// <summary>Charge maximale, en images. Au-dela, la puissance ne monte plus.</summary>
    public const float MAX_CHARGE = 45f;

    /// <summary>Puissance d'un tir a peine effleure, et d'un tir a pleine charge.</summary>
    public const float MIN_POWER = 2.2f;
    public const float MAX_POWER = 8.5f;

    /// <summary>
    /// Vrai quand une manche de soccer est en cours, ICI et MAINTENANT.
    ///
    /// La scene est comparee a celle du jeu, et ce n'est pas de la prudence
    /// inutile : Monocle ne remet pas Scene a null en fin de manche (voir
    /// Layer.End), donc un ballon de la manche precedente aurait encore l'air
    /// vivant. Sans cette comparaison, le patch du tir croirait etre en soccer dans
    /// le match SUIVANT - un Last Man Standing ou plus aucune fleche ne partirait.
    /// </summary>
    public static bool Active
    {
      get
      {
        return Ball != null
            && Ball.Scene != null
            && Engine.Instance != null
            && ReferenceEquals(Ball.Scene, Engine.Instance.Scene);
      }
    }

    public static void Reset()
    {
      Ball = null;
      LeftGoal = null;
      RightGoal = null;
      Scorer = Allegiance.Neutral;
      Scored = false;
      Array.Clear(charge, 0, charge.Length);
    }

    /// <summary>
    /// Pose les buts et le ballon. Appele une fois par manche, au chargement du
    /// niveau, quand la geometrie est en place.
    /// </summary>
    public static void Setup(Level level)
    {
      Reset();

      Vector2 leftPos = GoalPlacement.Find(level, true);
      Vector2 rightPos = GoalPlacement.Find(level, false);

      LeftGoal = new Goal(leftPos, Allegiance.Blue, true);
      RightGoal = new Goal(rightPos, Allegiance.Red, false);
      level.Add(LeftGoal);
      level.Add(RightGoal);

      // La cage est taillee dans le mur : les buts affleurent le bord du terrain, et
      // la pierre qui les recouvrait s'en va.
      GoalPlacement.Carve(level, LeftGoal);
      GoalPlacement.Carve(level, RightGoal);

      // Le ballon nait au milieu des deux buts, en hauteur : il tombe alors au
      // centre du terrain et la manche part d'une position symetrique.
      Vector2 center = new Vector2((leftPos.X + rightPos.X) / 2f,
                                   Math.Min(leftPos.Y, rightPos.Y) - 24f);
      if (center.Y < 16f)
      {
        center.Y = 16f;
      }

      Ball = new Ball(center);
      level.Add(Ball);

      // Le centre du terrain n'est pas toujours vide : une plateforme, une colonne, et
      // le ballon naissait dans la pierre. Le recalage vient APRES l'ajout au niveau,
      // la recherche interrogeant les solides de la scene.
      Ball.PlaceNear(center);
    }

    /// <summary>
    /// Avance la charge de tir d'un archer. Il vise deja - c'est le jeu qui pose
    /// Aiming quand on maintient le bouton de tir - donc on ne fait qu'en mesurer
    /// la duree.
    /// </summary>
    public static void Charge(Player player)
    {
      int i = player.PlayerIndex;
      if (i < 0 || i >= charge.Length)
      {
        return;
      }

      charge[i] = Math.Min(charge[i] + Engine.TimeMult, MAX_CHARGE);
    }

    /// <summary>Charge courante, de 0 a 1. Sert au tir et a la jauge.</summary>
    public static float ChargeOf(int playerIndex)
    {
      if (playerIndex < 0 || playerIndex >= charge.Length)
      {
        return 0f;
      }

      return charge[playerIndex] / MAX_CHARGE;
    }

    public static void ResetCharge(int playerIndex)
    {
      if (playerIndex >= 0 && playerIndex < charge.Length)
      {
        charge[playerIndex] = 0f;
      }
    }

    /// <summary>
    /// Le coup de pied : direction de visee, puissance selon le temps d'appui.
    /// Sans effet si cet archer ne porte pas le ballon.
    /// </summary>
    public static void Kick(Player player)
    {
      float power = MIN_POWER + (MAX_POWER - MIN_POWER) * ChargeOf(player.PlayerIndex);
      ResetCharge(player.PlayerIndex);

      if (!Active || Ball.Carrier != player)
      {
        return;
      }

      Ball.Kick(player, player.AimDirection, power);
    }

    /// <summary>
    /// Le ballon est-il entre quelque part ? Rend l'equipe qui MARQUE, c'est-a-dire
    /// celle qui ne defend pas ce but.
    /// </summary>
    public static void CheckGoals()
    {
      if (!Active || Scored)
      {
        return;
      }

      // Un ballon porte ne compte pas : on ne marque pas en entrant dans le but
      // avec, sinon il n'y aurait plus rien a jouer.
      if (Ball.Carrier != null)
      {
        return;
      }

      Goal entered = null;
      if (LeftGoal != null && LeftGoal.Contains(Ball.Position))
      {
        entered = LeftGoal;
      }
      else if (RightGoal != null && RightGoal.Contains(Ball.Position))
      {
        entered = RightGoal;
      }

      if (entered == null)
      {
        return;
      }

      Scored = true;

      // Le ballon RESTE dans le but : plus de gravite, plus de rebond, plus de
      // reprise. Un dernier rebond qui le ferait ressortir donnerait a voir le
      // contraire de ce qui vient d'etre marque.
      Ball.Settle();

      // C'est le but ENCAISSE qui designe le marqueur : l'equipe adverse de celle
      // qui defend. Un contre son camp compte donc pour l'adversaire, comme au
      // soccerball.
      Scorer = entered.Team == Allegiance.Blue ? Allegiance.Red : Allegiance.Blue;

      Sounds.sfx_gemCollect.Play(Ball.X, 1f);
    }
  }
}
