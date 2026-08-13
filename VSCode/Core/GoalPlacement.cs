using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TowerFall;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// Pose les deux buts CONTRE les bords du terrain, a mi-hauteur, et retire tout ce
  /// qui les gene.
  ///
  /// L'emplacement ne se negocie pas : un but est au bout du terrain et a hauteur du
  /// milieu, les deux se faisant face. Si des tuiles occupent la place, ce sont
  /// ELLES qui partent - la cage est taillee dans la pierre, exactement comme le
  /// fait un tir qui perce un mur dans le mod Power.
  ///
  /// C'est l'inverse de la premiere version, qui cherchait un endroit deja libre :
  /// les buts finissaient la ou la tour voulait bien les laisser - au ras du sol,
  /// parfois a deux hauteurs differentes - et le terrain n'avait plus rien de
  /// symetrique.
  ///
  /// Rien n'est code en dur sur la taille du niveau : les dimensions viennent de la
  /// grille de tuiles, donc une tour elargie par le mod WiderSet (420 de large au
  /// lieu de 320) place ses buts a SES bords sans que ce code le sache.
  /// </summary>
  public static class GoalPlacement
  {
    /// <summary>Cote d'une cellule de la grille du jeu, en pixels.</summary>
    private const int CELL = 10;

    /// <summary>Marge creusee autour de la cage, pour qu'on puisse y entrer.</summary>
    private const float MARGIN = 2f;

    /// <summary>Le terrain, tel que la grille de tuiles le decrit.</summary>
    public static Vector2 LevelSize(Level level)
    {
      Grid grid = level?.Tiles?.Grid;
      if (grid == null)
      {
        return new Vector2(WrapMath.WIDTH, WrapMath.HEIGHT);
      }

      return new Vector2(grid.CellsX * grid.CellWidth, grid.CellsY * grid.CellHeight);
    }

    /// <summary>
    /// Le centre du but de ce cote : colle au bord, a MI-HAUTEUR du terrain.
    ///
    /// La hauteur ne se cherche pas et ne depend d'aucune tour : les deux buts se
    /// font face au milieu, comme sur un vrai terrain. Ce qui gene est retire (voir
    /// Carve) - c'est la geometrie qui s'adapte au but, et non l'inverse. Un but
    /// pose la ou il y avait de la place finissait au ras du sol, ou pire au fond
    /// d'un puits, et les deux cotes n'etaient meme pas a la meme hauteur.
    /// </summary>
    public static Vector2 Find(Level level, bool left)
    {
      Vector2 size = LevelSize(level);

      float x = left
          ? Goal.WIDTH / 2f
          : size.X - Goal.WIDTH / 2f;

      return new Vector2(x, size.Y / 2f);
    }

    /// <summary>
    /// Creuse la cage dans les tuiles du niveau.
    ///
    /// Meme methode que le tir qui perce un mur dans le mod Power : la cellule doit
    /// etre eteinte des DEUX cotes - dans la Grid, qui porte les collisions, et dans
    /// le bitData, qui porte le dessin - parce que le constructeur de Monocle.Grid
    /// clone le tableau de bits. N'eteindre que la Grid laisserait un mur qu'on
    /// traverse ; n'eteindre que le bitData laisserait un mur invisible.
    /// </summary>
    public static void Carve(Level level, Goal goal)
    {
      try
      {
        LevelTiles tiles = level?.Tiles;
        Grid grid = tiles?.Grid;
        if (grid == null)
        {
          return;
        }

        bool[,] bits = TileBits(tiles);
        var cleared = new HashSet<Point>();

        float x0 = goal.X - Goal.WIDTH / 2f - MARGIN;
        float x1 = goal.X + Goal.WIDTH / 2f + MARGIN;
        float y0 = goal.Y - Goal.HEIGHT / 2f - MARGIN;
        float y1 = goal.Y + Goal.HEIGHT / 2f + MARGIN;

        for (float x = x0; x <= x1; x += CELL / 2f)
        {
          for (float y = y0; y <= y1; y += CELL / 2f)
          {
            int cx = (int)(x / grid.CellWidth);
            int cy = (int)(y / grid.CellHeight);

            if (cx < 0 || cy < 0 || cx >= grid.CellsX || cy >= grid.CellsY || !grid[cx, cy])
            {
              continue;
            }

            grid[cx, cy] = false;
            if (bits != null)
            {
              bits[cx, cy] = false;
            }

            cleared.Add(new Point(cx, cy));
          }
        }

        if (cleared.Count == 0)
        {
          return;
        }

        // Retuile toute la carte depuis bitData : ReloadTiles est ajoute par
        // FortRise exactement pour ca. Une fois par manche et par but, le cout ne
        // se voit pas.
        tiles.ReloadTiles();
        Logger.Info($"But {(goal.Left ? "gauche" : "droit")} : {cleared.Count} cellules creusees");
      }
      catch (Exception e)
      {
        // Un but pose sur un mur plein reste jouable - il faudra tirer plus haut -
        // la ou une exception ici empecherait le niveau de se charger.
        Logger.Error("GoalPlacement.Carve: " + e);
      }
    }

    /// <summary>
    /// Le tableau de bits que LevelTiles garde pour son dessin. Prive, d'ou la
    /// lecture par DynamicData.
    /// </summary>
    private static bool[,] TileBits(LevelTiles tiles)
    {
      using var data = DynamicData.For(tiles);
      var bits = data.Get("bitData") as bool[,];

      if (bits == null && !warned)
      {
        warned = true;
        Logger.Error("LevelTiles.bitData introuvable : les buts resteront dessines dans le mur");
      }

      return bits;
    }

    private static bool warned;
  }
}
