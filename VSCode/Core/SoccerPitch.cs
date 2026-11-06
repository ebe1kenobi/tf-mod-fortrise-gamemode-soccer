using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TowerFall;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// Le stade : la tour est videe, muree tout autour, et un terrain est peint au sol.
  ///
  /// <b>On ne construit pas un niveau, on REECRIT celui qui est la.</b> La tour est
  /// deja chargee, avec sa grille de collision, sa planche de tuiles, ses points de
  /// depart et son fond. On efface ses cases et on rallume celles du pourtour, puis on
  /// laisse le jeu repeindre : le choix de chaque tuile depend de ses voisines, donc
  /// les murs prennent les bords, les coins et l'usure de la tour d'origine. Un
  /// rectangle dessine a la main n'aurait rien de tout cela, et se verrait.
  ///
  /// C'est la meme mecanique que le creusement des cages dans <see cref="GoalPlacement"/> :
  /// la cellule doit etre changee des DEUX cotes - dans la <c>Grid</c>, qui porte les
  /// collisions, et dans le <c>bitData</c>, qui porte le dessin - parce que le
  /// constructeur de <c>Monocle.Grid</c> clone le tableau de bits. N'en changer qu'un
  /// donne soit un mur qu'on traverse, soit un mur invisible.
  /// </summary>
  public static class SoccerPitch
  {
    /// <summary>Epaisseur du mur d'enceinte, en cellules.</summary>
    private const int BORDER = 1;

    /// <summary>Le stade est-il demande ?</summary>
    public static bool Enabled
    {
      get
      {
        var settings = TFModFortRiseGameModeSoccerModule.Settings;
        return settings != null && settings.field;
      }
    }

    /// <summary>
    /// Le reglage seul, sans se demander si une manche est en cours.
    ///
    /// Il sert pendant la MISE EN PLACE, ou <see cref="Flat"/> repondrait faux a tort :
    /// le ballon n'existe pas encore, donc la manche n'est pas encore "active". Le
    /// piege a coute la position du ballon a l'engagement, pose vingt-quatre pixels
    /// trop haut pour qu'il retombe - sur un terrain plat, il y serait reste.
    /// </summary>
    public static bool FlatSetting
    {
      get
      {
        var settings = TFModFortRiseGameModeSoccerModule.Settings;
        return settings != null && settings.flat;
      }
    }

    /// <summary>
    /// Le terrain est-il PLAT, ici et maintenant ?
    ///
    /// La condition sur la manche en cours n'est pas de la prudence inutile : les
    /// correctifs de gravite s'appliquent a <c>Player</c>, qui existe dans tous les
    /// modes. Sans elle, un match de deathmatch lance apres une partie de foot se
    /// jouerait en apesanteur.
    /// </summary>
    public static bool Flat => FlatSetting && SoccerMatch.Active;

    /// <summary>
    /// Vide la tour et la mure. A appeler AVANT de poser les buts : ils se creusent
    /// dans les murs qu'on vient d'ecrire.
    /// </summary>
    public static void Build(Level level)
    {
      if (!Enabled)
      {
        return;
      }

      try
      {
        Clear(level);
        Walls(level);
        Repaint(level);

        level.Add(new PitchBackground(Bounds(level)));
      }
      catch (Exception e)
      {
        // Un stade qui ne se construit pas laisse la tour telle quelle : c'est moins
        // beau, cela reste jouable. Une exception ici empecherait le niveau de se
        // charger, et il n'y aurait plus de partie du tout.
        Logger.Error("SoccerPitch.Build: " + e);
      }
    }

    /// <summary>La zone de jeu, murs exclus, en pixels.</summary>
    public static Rectangle Bounds(Level level)
    {
      Grid grid = level?.Tiles?.Grid;

      if (grid == null)
      {
        return new Rectangle(0, 0, (int)WrapMath.WIDTH, (int)WrapMath.HEIGHT);
      }

      // Les cotes de cellule sont des flottants dans Monocle : la conversion est
      // explicite pour que le rectangle tombe sur des pixels entiers, faute de quoi le
      // trace des lignes baverait d'un demi-pixel selon les bords.
      int cellW = (int)grid.CellWidth;
      int cellH = (int)grid.CellHeight;

      return new Rectangle(
          BORDER * cellW,
          BORDER * cellH,
          grid.CellsX * cellW - BORDER * cellW * 2,
          grid.CellsY * cellH - BORDER * cellH * 2);
    }

    // ------------------------------------------------------------------
    // La geometrie
    // ------------------------------------------------------------------

    /// <summary>
    /// Eteint toutes les cases, et retire ce que la tour a pose PAR-DESSUS ses tuiles.
    ///
    /// La grille ne porte que le decor fixe. Les plateformes traversantes, les blocs
    /// mouvants et les pieges sont des entites : les oublier laisserait flotter au
    /// milieu du stade des morceaux de la tour d'origine, ce qui est exactement le
    /// contraire d'une salle vide.
    /// </summary>
    private static void Clear(Level level)
    {
      Grid grid = level.Tiles?.Grid;
      bool[,] bits = TileBits(level.Tiles);

      if (grid != null)
      {
        for (int x = 0; x < grid.CellsX; x++)
        {
          for (int y = 0; y < grid.CellsY; y++)
          {
            grid[x, y] = false;

            if (bits != null)
            {
              bits[x, y] = false;
            }
          }
        }
      }

      // Les tuiles elles-memes portent le tag Solid : les retirer emporterait le sol
      // qu'on vient d'ecrire. On ne touche donc qu'a ce qui n'est PAS LevelTiles.
      Sweep(level, GameTags.Solid);
      Sweep(level, GameTags.JumpThru);
      Sweep(level, GameTags.LavaCollider);
      Sweep(level, GameTags.HotCoals);
      Sweep(level, GameTags.TreasureChest);

      // Cobwebs hang on walls and have no tag of their own, so the sweeps above miss them.
      // With their walls gone, Cobwebs.SceneBegin throws "Cobwebs in invalid position".
      foreach (Layer layer in level.Layers.Values)
      {
        foreach (Entity entity in layer.Entities)
        {
          if (entity is Cobwebs)
          {
            entity.RemoveSelf();
          }
        }
      }
    }

    private static void Sweep(Level level, GameTags tag)
    {
      foreach (Entity entity in level[tag])
      {
        if (entity is LevelTiles || entity is Player || entity is Goal || entity is Ball)
        {
          continue;
        }

        entity.RemoveSelf();
      }
    }

    /// <summary>Le mur d'enceinte : le pourtour de la grille, et lui seul.</summary>
    private static void Walls(Level level)
    {
      Grid grid = level.Tiles?.Grid;
      bool[,] bits = TileBits(level.Tiles);

      if (grid == null)
      {
        return;
      }

      for (int x = 0; x < grid.CellsX; x++)
      {
        for (int y = 0; y < grid.CellsY; y++)
        {
          bool edge = x < BORDER || y < BORDER
                   || x >= grid.CellsX - BORDER || y >= grid.CellsY - BORDER;

          if (!edge)
          {
            continue;
          }

          grid[x, y] = true;

          if (bits != null)
          {
            bits[x, y] = true;
          }
        }
      }
    }

    private static void Repaint(Level level)
    {
      level.Tiles?.ReloadTiles();
    }

    /// <summary>
    /// Le tableau de bits que LevelTiles garde pour son dessin. Prive, d'ou la lecture
    /// par DynamicData - une fois par manche, pas dans une boucle de rendu.
    /// </summary>
    private static bool[,] TileBits(LevelTiles tiles)
    {
      if (tiles == null)
      {
        return null;
      }

      using var data = DynamicData.For(tiles);
      return data.Get("bitData") as bool[,];
    }

    // ------------------------------------------------------------------
    // Les places de depart
    // ------------------------------------------------------------------

    /// <summary>
    /// Le placement d'avant-match, comme une equipe de foot.
    ///
    /// <b>Les points de depart de la TOUR n'ont plus de sens sur un stade</b> : ils
    /// etaient poses par rapport a des plateformes qui n'existent plus, et ceux qui
    /// frolaient un bord se retrouvent dans le mur d'enceinte.
    ///
    /// <b>Les roles sont ordonnes du plus avance au plus recule</b>, et c'est ce qui
    /// fait que la formation se degrade proprement quand on est moins nombreux : a un
    /// contre un il n'y a qu'un attaquant, a deux un attaquant et un gardien, a trois on
    /// ajoute les deux arrieres - jamais un gardien sans personne devant lui.
    ///
    /// Les distances sont des FRACTIONS de la moitie de terrain, pas des pixels : un
    /// stade elargi par WiderSet place ses joueurs au bon endroit sans que ce code ait a
    /// le savoir.
    /// </summary>
    private static readonly (float Depth, float Side)[] Roles =
    {
      // Attaquant : pres du rond central, au milieu. C'est lui qui dispute
      // l'engagement.
      (0.72f, 0f),

      // Les deux arrieres, de part et d'autre.
      (0.38f, -0.26f),
      (0.38f, 0.26f),

      // Gardien : devant sa cage.
      (0.06f, 0f),

      // Au-dela de quatre, on etoffe le milieu plutot que d'empiler les gardiens.
      (0.55f, -0.34f),
      (0.55f, 0.34f),
      (0.24f, -0.14f),
      (0.24f, 0.14f),
    };

    /// <summary>
    /// Les places d'une equipe, de l'attaquant au gardien.
    /// </summary>
    /// <param name="goal">Le but que cette equipe defend.</param>
    public static List<Vector2> Formation(Level level, Vector2 goal, int count)
    {
      Rectangle area = Bounds(level);
      var points = new List<Vector2>();

      float middleX = area.Left + area.Width / 2f;
      float middleY = area.Top + area.Height / 2f;

      // Distance du but au centre : c'est la profondeur dont l'equipe dispose.
      float half = middleX - goal.X;

      for (int i = 0; i < count && i < Roles.Length; i++)
      {
        (float depth, float side) = Roles[i];

        float x = goal.X + half * depth;
        float y = middleY + area.Height * side;

        // Jamais dans le mur : la marge vaut une case, comme l'enceinte.
        y = MathHelper.Clamp(y, area.Top + 12f, area.Bottom - 12f);
        x = MathHelper.Clamp(x, area.Left + 12f, area.Right - 12f);

        points.Add(new Vector2(x, y));
      }

      return points;
    }
  }
}
