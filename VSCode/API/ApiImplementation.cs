using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseGameModeSoccer;

public class ApiImplementation : ISoccerApi
{
  public bool IsSoccerMatch()
  {
    return SoccerMatch.Active;
  }

  public Vector2 BallPosition()
  {
    return SoccerMatch.Active ? SoccerMatch.Ball.Position : Vector2.Zero;
  }

  public int BallCarrier()
  {
    if (!SoccerMatch.Active)
    {
      return -1;
    }

    Player carrier = SoccerMatch.Ball.Carrier;
    return carrier == null ? -1 : carrier.PlayerIndex;
  }

  /// <summary>
  /// Le but ou ce joueur doit marquer, c'est-a-dire celui que l'AUTRE equipe defend.
  ///
  /// Un joueur sans equipe - partie libre - se voit attribuer le but de droite : il
  /// faut bien viser quelque part, et tout le monde vise alors le meme, ce qui donne
  /// une melee plutot qu'une immobilite.
  /// </summary>
  public Vector2 TargetGoal(int playerIndex)
  {
    if (!SoccerMatch.Active || SoccerMatch.LeftGoal == null || SoccerMatch.RightGoal == null)
    {
      return Vector2.Zero;
    }

    Allegiance team = TeamOf(playerIndex);

    Goal target = team == Allegiance.Blue ? SoccerMatch.RightGoal : SoccerMatch.LeftGoal;
    return target.Position;
  }

  private static Allegiance TeamOf(int playerIndex)
  {
    var level = Engine.Instance?.Scene as Level;

    if (level != null)
    {
      foreach (Entity entity in level[GameTags.Player])
      {
        if (entity is Player player && player.PlayerIndex == playerIndex)
        {
          return player.TeamColor;
        }
      }
    }

    return Allegiance.Blue;
  }
}
