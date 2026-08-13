using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// Fait apparaitre les archers, chaque equipe de SON cote du terrain.
  ///
  /// Le SpawnPlayersTeams du jeu ne pouvait pas servir : il lit les points
  /// "TeamSpawnA" et "TeamSpawnB" de la tour et les indexe SANS verifier qu'il y en
  /// a assez - une tour qui n'en declare pas, ou qui en declare moins que de joueurs
  /// dans une equipe, le fait sortir du tableau et le niveau ne se charge jamais.
  /// Toutes les tours ne sont pas prevues pour le jeu par equipes, et une tour
  /// ajoutee par un mod ne l'est presque jamais.
  ///
  /// Ici, tous les points de depart de la tour sont pris ensemble, quel que soit
  /// leur nom, puis tries par abscisse : les bleus partent des plus a GAUCHE, les
  /// rouges des plus a DROITE. Chaque equipe demarre donc pres du but qu'elle
  /// defend - ce que le soccer demande, et que les points d'equipe d'une tour de
  /// deathmatch ne garantissent pas - et il devient impossible de manquer de place :
  /// s'il n'y a qu'un point par cote, deux coequipiers y apparaissent ensemble et le
  /// jeu les ecarte tout seul.
  /// </summary>
  public static class SoccerSpawns
  {
    /// <summary>
    /// Cree les archers. Rend le nombre de joueurs places.
    /// </summary>
    public static int Spawn(Session session, Vector2 leftGoal, Vector2 rightGoal)
    {
      Level level = session.CurrentLevel;

      List<Vector2> points = Gather(level);
      points.Sort((a, b) => a.X.CompareTo(b.X));

      // Aucun point de depart dans la tour : on part des buts eux-memes, decales
      // vers l'interieur. Une tour sans le moindre spawn reste jouable.
      if (points.Count == 0)
      {
        points.Add(leftGoal + new Vector2(24f, 0f));
        points.Add(rightGoal - new Vector2(24f, 0f));
      }

      int fromLeft = 0;
      int fromRight = 0;
      int spawned = 0;

      // Le nombre d'emplacements du JEU, et non quatre : WiderSet le porte a huit, et
      // en fixer quatre ici laissait les joueurs 5 a 8 sans archer sur le terrain -
      // ils existaient dans la partie sans jamais apparaitre.
      for (int i = 0; i < TFGame.Players.Length; i++)
      {
        if (!session.ShouldSpawn(i))
        {
          continue;
        }

        Allegiance team = TeamOf(session, i);

        // Les bleus consomment la liste par le debut, les rouges par la fin. Le
        // modulo est le garde-fou : plus de coequipiers que de points ne peut plus
        // sortir du tableau, on repart simplement du premier point du cote.
        Vector2 at = team == Allegiance.Red
            ? points[points.Count - 1 - (fromRight++ % points.Count)]
            : points[fromLeft++ % points.Count];

        Player player = new Player(i, at + Vector2.UnitY * 2f, team, team,
            session.GetPlayerInventory(i), session.GetSpawnHatState(i), true, true, true);

        level.Add(player);
        spawned++;
      }

      return spawned;
    }

    /// <summary>
    /// Tous les points de depart de la tour, sans doublon.
    ///
    /// Les quatre noms sont ramasses ensemble : une tour de versus declare
    /// "PlayerSpawn", une tour prevue pour les equipes ajoute "TeamSpawnA" et
    /// "TeamSpawnB", et certaines melangent. Prendre tout ce qui existe est ce qui
    /// permet de ne dependre d'aucune convention.
    /// </summary>
    private static List<Vector2> Gather(Level level)
    {
      var points = new List<Vector2>();

      foreach (string name in new[] { "PlayerSpawn", "TeamSpawn", "TeamSpawnA", "TeamSpawnB" })
      {
        try
        {
          List<Vector2> found = level.GetXMLPositions(name);
          if (found == null)
          {
            continue;
          }

          foreach (Vector2 point in found)
          {
            if (!points.Contains(point))
            {
              points.Add(point);
            }
          }
        }
        catch (Exception)
        {
          // Un nom que cette tour ne connait pas : on passe au suivant.
        }
      }

      return points;
    }

    /// <summary>
    /// L'equipe d'un archer.
    ///
    /// Elle vient de l'ecran de selection des equipes, que le jeu impose avant de
    /// lancer un mode par equipes. Le repli - un joueur reste en Neutral - n'est pas
    /// theorique : il suffirait qu'un autre mod court-circuite cet ecran. On repartit
    /// alors en alternance, et on ECRIT le choix dans les reglages du match, sinon le
    /// decompte des points et la fin de manche, qui relisent la meme table,
    /// verraient une equipe que personne ne defend.
    /// </summary>
    private static Allegiance TeamOf(Session session, int playerIndex)
    {
      MatchSettings settings = session.MatchSettings;
      Allegiance team = settings.Teams[playerIndex];

      if (team != Allegiance.Neutral)
      {
        return team;
      }

      team = playerIndex % 2 == 0 ? Allegiance.Blue : Allegiance.Red;
      settings.Teams[playerIndex] = team;
      Logger.Info($"P{playerIndex + 1} sans equipe : place en {team}");

      return team;
    }
  }
}
