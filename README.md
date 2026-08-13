# Soccer

Un mode versus **football** pour TowerFall, sous FortRise 5.

Pas une seule flèche : un ballon, deux buts, deux équipes. On prend le ballon en
marchant dessus, on le garde devant soi, on frappe au bouton de tir — d'autant plus
fort qu'on l'a maintenu — et on le perd quand on se fait bousculer ou piétiner. Le
but marqué termine la manche.

Un mod pour **FortRise 5** (>= 5.3.3).

## Installation

1. Installer FortRise 5 et lancer le jeu par `FortRise.exe`.
2. Copier `release/tf-mod-fortrise-gamemode-soccer` dans `<TowerFall>/FortRise/Mods/`.

## Jouer

Le mode **SOCCER** s'ajoute à la liste des modes versus, à côté de Last Man Standing et
Team Deathmatch. C'est un **mode par équipes** : le jeu répartit lui-même les archers
en bleus et rouges à l'écran de sélection, et compte les points par équipe.

**Deux joueurs suffisent** — une équipe d'un joueur reste une équipe — mais tout se
joue jusqu'à quatre, à deux contre deux.

| | |
|---|---|
| **Bleu** | défend le but de gauche |
| **Rouge** | défend le but de droite |

### Le ballon

- Il tombe au **centre du terrain** au début de la manche.
- **Marcher dessus** le prend. Il se tient alors devant l'archer, à hauteur de pied,
  et suit son sens : on voit du premier coup d'œil qui l'a et où il va tirer.
- **Le bouton de tir** frappe. Maintenu, il charge : la jauge sous les pieds va du
  blanc au rouge, et la puissance suit le temps d'appui. La direction est celle de la
  visée — huit directions, ou libre avec la variante *Free Aiming* du jeu.
- Une fois frappé, le ballon vole **comme une flèche** : gravité, rebonds sur les
  murs, roulement au sol, et il ressort en face quand il sort par un bord ouvert.
- Il ne peut pas être repris pendant une douzaine d'images après un tir, sinon le
  tireur — encore dessus — le rattraperait aussitôt.

### Se le prendre

Personne ne peut tuer personne : il n'y a rien pour ça. Le ballon change de pied par
contact, aux deux endroits où le jeu fait déjà changer une flèche de main :

- **Piétiner** quelqu'un (lui tomber sur la tête) lui prend le ballon ;
- **Bousculer** quelqu'un de côté le lui prend aussi, à condition de pousser vers
  lui. Si les deux poussent l'un vers l'autre, personne ne prend rien : ils se
  repoussent, et voilà tout.

Ce n'est pas un choix arbitraire : `Player.PlayerOnPlayer` est l'endroit exact où le
jeu distingue déjà le piétinement de la bousculade, et où il fait déjà voler une
flèche d'un archer à l'autre. Le ballon suit les mêmes règles, au même endroit — rien
à deviner sur les vitesses ou les hitbox.

### Marquer

Le ballon qui entre dans un but **termine la manche** et donne le point à l'équipe
adverse de celle qui défendait ce but — un contre son camp compte donc pour
l'adversaire, comme au soccerball. Un ballon *porté* dans le but ne compte pas : il faut
le frapper.

Le décompte des points, l'écran de résultats et la couronne sont ceux du jeu : la
manche est bâtie sur le squelette de `TeamDeathmatchRoundLogic`, c'est lui que tout le
reste attend.

## Les buts sur une tour qui n'a pas été dessinée pour ça

Un but posé à une position fixe ne marcherait que sur les tours aux côtés ouverts ;
ailleurs il finirait noyé dans la pierre, et le ballon ne pourrait jamais y entrer.

Le mod **cherche** donc l'emplacement : de chaque côté, il s'éloigne du bord par pas
de 4 pixels jusqu'à trouver un rectangle libre de tout solide, marge comprise. À
distance égale du bord, la hauteur la plus proche du milieu de l'écran l'emporte — un
but à hauteur d'homme se défend, un but collé au plafond ne se défend pas.

Rien là-dedans ne connaît la géométrie des tours : tout passe par `CollideCheck` sur
les solides, donc une tour ajoutée par un autre mod fonctionne aussi. Si vraiment
aucun emplacement n'est libre, le but se pose quand même à mi-hauteur du bord.

Le **ballon** naît au milieu des deux buts, en hauteur. Ce point n'est pas toujours
vide — une plateforme, une colonne — et il y naissait alors dans la pierre : on ne le
voyait plus, il fallait casser le mur pour le retrouver. Il cherche maintenant
l'endroit libre le plus proche, par anneaux carrés, à la première image du niveau : à
l'instant où il est ajouté, les solides sont dans le même lot d'entités que lui et
l'ordre entre eux ne nous appartient pas.

## Ce qui est neutralisé

- **Les flèches** : le carquois est vidé au chargement du niveau, et `ShootArrow` ne
  crée jamais de flèche — le bouton sert au coup de pied.
- **Tout ce qui parle de flèches à l'écran** : le compteur au-dessus de la tête, l'arc,
  et le viseur qui suit la direction visée. Le geste est le même que celui du tir, donc
  le jeu accompagnait chaque coup de pied d'une flèche encochée : elle annonçait un tir
  qui n'arriverait jamais.
- **Les coffres** : ils ne donneraient que des flèches et des objets qui tuent.

Les pièges de la tour, eux, restent : une tour à lave ou à pointes peut encore tuer.
Un archer qui meurt lâche le ballon, et si toute une équipe tombe, l'autre remporte la
manche — sinon la partie resterait bloquée sur un terrain sans personne pour jouer.

## Ce qui s'affiche

- Une **flèche** au-dessus du porteur : d'un coup d'œil, qui a le ballon.
- Une **jauge de puissance** au-dessus de lui pendant qu'il charge, du blanc au rouge.
  Elle était sous les pieds, donc dans le sol — l'origine d'un archer *est* au niveau
  du décor, et douze pixels plus bas la mettait hors de vue.

## Réglages

Aucun. Le mode se choisit dans la liste des modes versus, et tout le reste est figé
dans le code : `Ball.cs` pour la gravité, les rebonds et le freinage, `SoccerMatch.cs`
pour la puissance de frappe, `Goal.cs` pour la taille des buts.

## API pour les autres mods

Le mod expose `ISoccerApi`, de quoi jouer au ballon sans connaître le mode :

| Membre | Ce qu'il rend |
|--------|---------------|
| `IsSoccerMatch()` | le match en cours est-il un match de foot |
| `BallPosition()` | où est le ballon |
| `BallCarrier()` | qui le porte, ou `-1` s'il roule librement |
| `TargetGoal(playerIndex)` | le but où ce joueur doit marquer — celui d'en face |

Le but est rendu **par joueur** et non par équipe : l'appelant sait de quel archer il
parle, pas de quelle couleur, et la correspondance appartient à ce mod.

Il sert à **AIJimmy**, qui va chercher le ballon puis le but adverse. Sans lui l'IA
poursuivait le joueur le plus proche et appuyait sur tir — c'est-à-dire qu'elle
frappait dans le vide, le ballon étant ailleurs.

## Construire et déployer

| Script | Rôle |
|--------|------|
| `script/release.bat` | compile, puis assemble dans `release/` |
| `script/deploy.bat` | copie `release/` dans le dossier `Mods` de TowerFall |
| `script/release_deploy.bat` | les deux à la suite |

Les chemins (dossier du jeu, nom du module) sont dans `script/config.bat`.
