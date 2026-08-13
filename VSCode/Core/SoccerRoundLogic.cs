using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// La manche de soccer : deux equipes, un ballon, un but marque et c'est fini.
  ///
  /// Calquee sur TeamDeathmatchRoundLogic - meme squelette de fin de manche, meme
  /// RoundEndCounter, meme facon d'ajouter le point a une equipe - parce que c'est
  /// ce squelette-la que le reste du jeu attend : le decompte des points, l'ecran
  /// de resultats et la couronne en dependent.
  ///
  /// Ce qui change : la manche ne se termine pas sur une mort mais sur un BUT.
  /// </summary>
  internal class SoccerRoundLogic : RoundLogic
  {
    private readonly RoundEndCounter roundEndCounter;
    private bool done;

    /// <summary>
    /// Le second argument est <c>canHaveMiasma</c>, et il vaut FAUX ici.
    ///
    /// Le miasma est la reponse du jeu a une manche qui s'eternise : il monte et
    /// pousse les archers a s'entretuer. En foot, une manche longue est une manche
    /// disputee - et un brouillard mortel n'a rien a y faire, puisque personne ne
    /// meurt. RoundLogic arrete son compte a rebours des le constructeur quand ce
    /// drapeau est faux.
    /// </summary>
    public SoccerRoundLogic(Session session) : base(session, false)
    {
      roundEndCounter = new RoundEndCounter(session);
    }

    public override void OnLevelLoadFinish()
    {
      base.OnLevelLoadFinish();

      Session.CurrentLevel.Add<VersusStart>(new VersusStart(Session));

      // Les buts et le ballon d'ABORD : les archers apparaissent chacun pres du but
      // de son equipe, il faut donc savoir ou ils sont. La geometrie du niveau est
      // deja en place a ce moment, et le terrain doit etre visible pendant la
      // cinematique de debut - on doit savoir ou l'on va avant de pouvoir courir.
      SoccerMatch.Setup(Session.CurrentLevel);

      // Et non SpawnPlayersTeams() : celui du jeu indexe les points d'equipe de la
      // tour sans verifier qu'il y en a assez, et sort du tableau sur toute tour qui
      // n'en declare pas - le niveau ne se chargeait alors jamais. Voir SoccerSpawns.
      Players = SoccerSpawns.Spawn(Session,
          SoccerMatch.LeftGoal.Position, SoccerMatch.RightGoal.Position);

      // Personne ne tire de fleche : le carquois est vide, et le bouton de tir sert
      // a frapper le ballon (voir MyPlayer).
      foreach (Entity entity in Session.CurrentLevel[GameTags.Player])
      {
        Player player = entity as Player;
        player?.Arrows.Arrows.Clear();
      }

      Session.CurrentLevel.Add<SoccerHUD>(new SoccerHUD());
    }

    public override void OnRoundStart()
    {
      base.OnRoundStart();

      // Pas de coffres : ils ne donnent que des fleches et des objets qui tuent,
      // dont ce mode ne veut pas. On ne les fait donc pas apparaitre - c'est
      // SpawnTreasureChestsVersus, delibérément non appelé - et on retire ceux que
      // la TOUR pose elle-meme : une tour procedurale en seme dans son XML, et les
      // variantes de tresor du joueur en ajoutent par-dessus.
      RemoveTreasure();
    }

    /// <summary>
    /// Vide le terrain de tout coffre, d'ou qu'il vienne.
    ///
    /// Plutot que de forcer la variante "No Treasure" du jeu : celle-ci appartient au
    /// joueur, elle est enregistree avec ses reglages de match, et un mode n'a pas a
    /// la changer dans son dos pour les autres modes.
    /// </summary>
    private void RemoveTreasure()
    {
      Level level = Session.CurrentLevel;
      if (level == null)
      {
        return;
      }

      // Monocle differe les retraits : parcourir en retirant est sur.
      foreach (Entity entity in level[GameTags.TreasureChest])
      {
        entity.RemoveSelf();
      }
    }

    public override void OnUpdate()
    {
      SessionStats.TimePlayed += Engine.DeltaTicks;
      base.OnUpdate();

      SoccerMatch.CheckGoals();

      // Un but ferme la manche. Level.Ending est le drapeau que tout le jeu
      // regarde : il fige les archers, coupe la musique et lance le ralenti.
      if (SoccerMatch.Scored && !Session.CurrentLevel.Ending)
      {
        Session.CurrentLevel.Ending = true;
      }

      if (!RoundStarted || done || !Session.CurrentLevel.Ending || !Session.CurrentLevel.CanEnd)
      {
        return;
      }

      if (!roundEndCounter.Finished)
      {
        roundEndCounter.Update();
        return;
      }

      done = true;

      // Le point va a l'EQUIPE, pas au joueur : l'index de score est l'allegiance,
      // comme en match par equipes.
      if (SoccerMatch.Scorer != Allegiance.Neutral)
      {
        AddScore((int)SoccerMatch.Scorer, 1);
      }

      InsertCrownEvent();
      Session.EndRound();
    }

    /// <summary>
    /// Personne ne peut tuer ici - il n'y a pas de fleche - mais une tour peut
    /// avoir ses pieges. Un archer qui meurt lache le ballon, et si toute une equipe
    /// tombe, l'autre remporte la manche : sinon la partie resterait bloquee sur un
    /// terrain sans personne pour jouer.
    /// </summary>
    public override void OnPlayerDeath(Player player, PlayerCorpse corpse, int playerIndex,
        DeathCause deathType, Vector2 position, int killerIndex)
    {
      base.OnPlayerDeath(player, corpse, playerIndex, deathType, position, killerIndex);

      if (SoccerMatch.Active && SoccerMatch.Ball.Carrier == player)
      {
        SoccerMatch.Ball.Drop();
      }

      Allegiance winner;
      if (!SoccerMatch.Scored && TeamCheckForRoundOver(out winner))
      {
        SoccerMatch.Scorer = winner;
        SoccerMatch.Scored = true;
        Session.CurrentLevel.Ending = true;
      }
    }
  }
}
