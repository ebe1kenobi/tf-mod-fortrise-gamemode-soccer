using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// Ce qu'il faut voir sans quitter le ballon des yeux : qui le porte, et avec
  /// quelle force il va frapper.
  ///
  /// Une seule entite pour tous les joueurs, dessinee dans le niveau et non dans le
  /// HUD du jeu : la jauge doit se trouver AU-DESSUS de l'archer qui charge, la ou
  /// son regard est deja, et pas dans un coin de l'ecran.
  /// </summary>
  public class SoccerHUD : Entity
  {
    /// <summary>Devant tout le reste, ballon compris.</summary>
    public const int DEPTH = -120;

    private const float BAR_WIDTH = 16f;
    private const float BAR_HEIGHT = 2f;

    public SoccerHUD() : base(Vector2.Zero)
    {
      Depth = DEPTH;
    }

    public override void Render()
    {
      base.Render();

      if (!SoccerMatch.Active)
      {
        return;
      }

      Level level = Scene as Level;
      if (level == null)
      {
        return;
      }

      foreach (Entity entity in level[GameTags.Player])
      {
        Player player = entity as Player;
        if (player == null || player.Dead)
        {
          continue;
        }

        DrawCarrierMark(player);
        DrawCharge(player);
      }
    }

    /// <summary>Une fleche au-dessus du porteur : d'un coup d'oeil, qui a le ballon.</summary>
    private static void DrawCarrierMark(Player player)
    {
      if (SoccerMatch.Ball.Carrier != player)
      {
        return;
      }

      Color color = ArcherData.GetColorA(player.PlayerIndex, player.TeamColor);
      Vector2 top = player.Position + new Vector2(0f, -22f);

      Draw.Rect(top.X - 3f, top.Y, 6f, 1f, color);
      Draw.Rect(top.X - 2f, top.Y + 1f, 4f, 1f, color);
      Draw.Rect(top.X - 1f, top.Y + 2f, 2f, 1f, color);
    }

    /// <summary>
    /// La jauge de puissance, au-dessus du porteur qui charge.
    ///
    /// Elle n'apparait QUE pour celui qui porte le ballon : un archer sans ballon
    /// peut viser aussi - le jeu le laisse faire - mais sa charge ne sert a rien, et
    /// une jauge lui promettrait quelque chose qui n'arrivera pas.
    ///
    /// Elle etait sous les pieds, ou personne ne l'a jamais vue : l'origine d'un
    /// archer EST au niveau du sol, donc douze pixels plus bas la met dans le decor.
    /// </summary>
    private static void DrawCharge(Player player)
    {
      if (SoccerMatch.Ball.Carrier != player || !player.Aiming)
      {
        return;
      }

      float charge = SoccerMatch.ChargeOf(player.PlayerIndex);
      if (charge <= 0f)
      {
        return;
      }

      // Au-dessus de la fleche du porteur, qui occupe les trois pixels sous -22.
      Vector2 at = player.Position + new Vector2(-BAR_WIDTH / 2f, -29f);

      Draw.Rect(at.X - 1f, at.Y - 1f, BAR_WIDTH + 2f, BAR_HEIGHT + 2f, Color.Black * 0.6f);
      Draw.Rect(at.X, at.Y, BAR_WIDTH, BAR_HEIGHT, Color.Black);

      // Du blanc vers le rouge : a pleine charge, la jauge crie qu'il faut lacher.
      Color color = Color.Lerp(Color.White, Calc.HexToColor("FF4040"), charge);
      Draw.Rect(at.X, at.Y, BAR_WIDTH * charge, BAR_HEIGHT, color);
    }
  }
}
