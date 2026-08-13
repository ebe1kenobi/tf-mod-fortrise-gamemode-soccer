using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// Un but. Il y en a deux : celui de gauche appartient a l'equipe bleue, celui de
  /// droite a la rouge.
  ///
  /// Ce n'est pas un solide du jeu : les archers le traversent librement, et c'est
  /// voulu - un gardien doit pouvoir se mettre dedans, sinon defendre reviendrait a
  /// boucher un trou depuis l'exterieur.
  ///
  /// Le BALLON, lui, ne le traverse pas n'importe ou : les trois cotes garnis de
  /// filet - le fond et les deux montants - l'arretent, et seule la face ouverte,
  /// tournee vers le terrain, le laisse entrer. C'est ce qui fait qu'un but se marque
  /// et ne s'obtient pas par un rebond dans le dos. Ces barres ne valent que pour le
  /// ballon, qui les teste lui-meme (voir Ball.Blocked) : rien d'autre dans le jeu ne
  /// les voit.
  /// </summary>
  public class Goal : Entity
  {
    /// <summary>Derriere les archers et le ballon : c'est un decor, pas un acteur.</summary>
    public const int DEPTH = 5;

    public const float WIDTH = 16f;
    public const float HEIGHT = 34f;

    /// <summary>Epaisseur des montants, celle qui arrete le ballon.</summary>
    private const float BAR = 3f;

    /// <summary>L'equipe qui DEFEND ce but. L'autre marque en y mettant le ballon.</summary>
    public readonly Allegiance Team;

    /// <summary>Vrai pour le but de gauche : sa face ouverte regarde a droite.</summary>
    public readonly bool Left;

    private readonly SineWave sine;

    public Goal(Vector2 position, Allegiance team, bool left) : base(position)
    {
      Depth = DEPTH;
      Team = team;
      Left = left;

      Collider = new Hitbox(WIDTH, HEIGHT, -WIDTH / 2f, -HEIGHT / 2f);

      sine = new SineWave(90);
      Add(sine);
    }

    /// <summary>La couleur de l'equipe qui defend, pour que le but se lise de loin.</summary>
    private Color TeamColor
    {
      get
      {
        return Team == Allegiance.Red
            ? Calc.HexToColor("F03C3C")
            : Calc.HexToColor("3CBCFC");
      }
    }

    private float X0 => X - WIDTH / 2f;
    private float Y0 => Y - HEIGHT / 2f;

    /// <summary>
    /// Le ballon est-il DANS le but ? Mesure sur la cage, montants exclus : un
    /// ballon coince contre le poteau exterieur n'est pas un but.
    /// </summary>
    public bool Contains(Vector2 point)
    {
      return point.X >= X0 + (Left ? BAR : 0f) && point.X <= X0 + WIDTH - (Left ? 0f : BAR)
          && point.Y >= Y0 + BAR && point.Y <= Y0 + HEIGHT - BAR;
    }

    /// <summary>
    /// Ce point tombe-t-il sur une barre ? Le fond et les deux montants arretent le
    /// ballon ; la face ouverte, elle, ne renvoie jamais vrai.
    /// </summary>
    public bool BlocksBall(Vector2 point)
    {
      // Hors de la cage, rien a tester : c'est le cas courant, autant sortir vite.
      if (point.X < X0 || point.X > X0 + WIDTH || point.Y < Y0 || point.Y > Y0 + HEIGHT)
      {
        return false;
      }

      // Les deux montants, en haut et en bas.
      if (point.Y <= Y0 + BAR || point.Y >= Y0 + HEIGHT - BAR)
      {
        return true;
      }

      // Le fond, du cote du bord de l'ecran. En face, c'est l'entree.
      return Left ? point.X <= X0 + BAR : point.X >= X0 + WIDTH - BAR;
    }

    public override void Render()
    {
      base.Render();

      Color color = TeamColor;
      Color net = color * (0.22f + 0.05f * sine.Value);

      // Le filet : des barreaux plutot qu'un aplat, pour qu'on voie le ballon au
      // travers et qu'on sache s'il est deja dedans.
      Draw.Rect(X0, Y0, WIDTH, HEIGHT, net * 0.5f);

      for (float i = 2f; i < HEIGHT; i += 4f)
      {
        Draw.Rect(X0, Y0 + i, WIDTH, 1f, net);
      }
      for (float i = 2f; i < WIDTH; i += 4f)
      {
        Draw.Rect(X0 + i, Y0, 1f, HEIGHT, net);
      }

      // Les barres, pleines : ce sont elles qui arretent le ballon, elles doivent
      // donc se voir exactement la ou elles agissent.
      Draw.Rect(X0, Y0, WIDTH, BAR, color);
      Draw.Rect(X0, Y0 + HEIGHT - BAR, WIDTH, BAR, color);
      Draw.Rect(Left ? X0 : X0 + WIDTH - BAR, Y0, BAR, HEIGHT, color);
    }
  }
}
