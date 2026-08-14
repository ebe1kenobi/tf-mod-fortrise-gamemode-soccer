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

    /// <summary>
    /// Degagement creuse DEVANT la cage, cote terrain.
    ///
    /// La marge de deux pixels suffisait a poser le but, pas a l'atteindre : sur une
    /// tour dont un mur passe juste devant, la cage etait taillee dans la pierre mais
    /// restait inaccessible - on voyait un but qu'on ne pouvait ni viser ni franchir.
    ///
    /// Trois cellules : de quoi entrer et tirer, sans eventrer la tour. Le terrain doit
    /// rester un terrain de TowerFall, avec ses murs et ses plateformes.
    /// </summary>
    private const float APPROACH = 3f * CELL;

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

      // Une tuile de retrait, et pas colle au bord.
      //
      // L'ecran de TowerFall BOUCLE : un but pose sur la tranche laissait passer les
      // joueurs a travers son filet, puisque sortir par la gauche revient a rentrer
      // par la droite. On les avance donc d'une cellule, et Carve mure ce qui reste
      // derriere - un but doit avoir un fond.
      float inset = CELL + Goal.WIDTH / 2f;

      float x = left ? inset : size.X - inset;

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

        // Le degagement ne se creuse que du cote TERRAIN : de l'autre, c'est le fond
        // du but, qu'on mure juste apres.
        float x0 = goal.X - Goal.WIDTH / 2f - MARGIN - (goal.Left ? 0f : APPROACH);
        float x1 = goal.X + Goal.WIDTH / 2f + MARGIN + (goal.Left ? APPROACH : 0f);
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

        // Le fond du but : on MURE la bande qui reste entre la cage et le bord.
        //
        // Sans elle, le bouclage de l'ecran fait du filet une porte : un archer qui
        // sort par la gauche rentre par la droite, ce qui revient a traverser le but.
        // C'est aussi ce qui rendait les IA incomprehensibles - elles passaient au
        // travers sans que rien ne les arrete.
        int filled = Fill(grid, bits, goal, y0, y1);

        if (cleared.Count == 0 && filled == 0)
        {
          return;
        }

        // Retuile toute la carte depuis bitData : ReloadTiles est ajoute par
        // FortRise exactement pour ca. Une fois par manche et par but, le cout ne
        // se voit pas.
        tiles.ReloadTiles();
        Logger.Info($"But {(goal.Left ? "gauche" : "droit")} : {cleared.Count} cellules creusees, "
            + $"{filled} murees derriere");
      }
      catch (Exception e)
      {
        // Un but pose sur un mur plein reste jouable - il faudra tirer plus haut -
        // la ou une exception ici empecherait le niveau de se charger.
        Logger.Error("GoalPlacement.Carve: " + e);
      }
    }

    /// <summary>
    /// Mure la bande entre le fond de la cage et le bord du terrain.
    ///
    /// Meme mecanique que le creusement, en sens inverse : on allume la cellule dans la
    /// Grid, qui porte les collisions, ET dans le bitData, qui porte le dessin. Le
    /// retuilage qui suit se charge de choisir les bonnes tuiles - on ne dessine pas de
    /// mur a la main, on dit seulement qu'il y en a un.
    ///
    /// La bande est plus HAUTE que la cage d'une cellule de chaque cote : un archer qui
    /// passerait juste au-dessus du montant retomberait de l'autre cote de l'ecran, et
    /// le but resterait une porte.
    /// </summary>
    private static int Fill(Grid grid, bool[,] bits, Goal goal, float y0, float y1)
    {
      int filled = 0;

      float back = goal.Left
          ? goal.X - Goal.WIDTH / 2f - MARGIN
          : goal.X + Goal.WIDTH / 2f + MARGIN;

      float edge = goal.Left ? 0f : grid.CellsX * grid.CellWidth;

      float from = Math.Min(back, edge);
      float to = Math.Max(back, edge);

      for (float x = from; x <= to; x += CELL / 2f)
      {
        for (float y = y0 - CELL; y <= y1 + CELL; y += CELL / 2f)
        {
          int cx = (int)(x / grid.CellWidth);
          int cy = (int)(y / grid.CellHeight);

          if (cx < 0 || cy < 0 || cx >= grid.CellsX || cy >= grid.CellsY || grid[cx, cy])
          {
            continue;
          }

          grid[cx, cy] = true;

          if (bits != null)
          {
            bits[cx, cy] = true;
          }

          filled++;
        }
      }

      return filled;
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
