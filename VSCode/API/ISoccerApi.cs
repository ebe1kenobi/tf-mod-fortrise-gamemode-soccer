using Microsoft.Xna.Framework;

namespace TFModFortRiseGameModeSoccer;

/// <summary>
/// Ce que les autres mods peuvent savoir d'une partie de football.
///
/// Il y en a un qui en a besoin : l'IA. Sans cela elle joue un deathmatch au milieu
/// d'un match de foot - elle poursuit le joueur le plus proche et appuie sur tir,
/// c'est-a-dire qu'elle frappe dans le vide, le ballon etant ailleurs.
///
/// Interface a part, et non des membres de plus sur une interface commune : l'interop
/// construit son proxy sur la FORME des membres, donc un appelant qui declare un
/// membre absent de la version installee n'obtient plus rien du tout.
/// </summary>
public partial interface ISoccerApi
{
  /// <summary>Vrai quand la partie en cours est un match de football.</summary>
  bool IsSoccerMatch();

  /// <summary>Position du ballon. Sans objet hors d'un match.</summary>
  Vector2 BallPosition();

  /// <summary>
  /// L'index du joueur qui porte le ballon, ou -1 s'il roule librement.
  /// </summary>
  int BallCarrier();

  /// <summary>
  /// Le but dans lequel ce joueur doit marquer - celui d'en face, pas le sien.
  ///
  /// Rendu par joueur et non par equipe : l'appelant sait de quel archer il parle, pas
  /// de quelle couleur, et la correspondance appartient a ce mod.
  /// </summary>
  Vector2 TargetGoal(int playerIndex);
}
