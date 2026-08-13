using FortRise;
using Microsoft.Xna.Framework;
using TowerFall;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// Le mode FOOT dans la liste des modes versus.
  ///
  /// C'est un mode par EQUIPES : le jeu s'occupe alors lui-meme de repartir les
  /// archers en bleus et rouges a l'ecran de selection, et de compter les points
  /// par equipe. Un contre un marche donc sans rien de special - une equipe d'un
  /// joueur reste une equipe - d'ou le minimum ramene a deux.
  /// </summary>
  public class SoccerGameMode : IVersusGameMode, IRegisterable
  {
    public static IVersusGameModeEntry Mode { get; private set; } = null!;

    public string Name => "SOCCER";
    public Color NameColor => Color.LightGreen;
    public ISubtextureEntry Icon => TextureRegistry.GameMode;
    public bool IsTeamMode => true;

    /// <summary>Deux joueurs suffisent : un par equipe, un but chacun a defendre.</summary>
    public int GetMinimumTeamPlayers(MatchSettings matchSettings) => 2;
    public int GetMinimumPlayers(MatchSettings matchSettings) => 2;

    public void OnStartGame(Session session)
    {
      SoccerMatch.Reset();
    }

    public RoundLogic OnCreateRoundLogic(Session session)
    {
      return new SoccerRoundLogic(session);
    }

    public static void Register(IModContent content, IModRegistry registry)
    {
      Mode = registry.GameModes.RegisterVersusGameMode(new SoccerGameMode());
    }
  }
}
