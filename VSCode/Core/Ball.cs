using System;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// Le ballon. Il n'y en a qu'un par manche, et il ne disparait jamais.
  ///
  /// Trois vies dans une seule entite : PORTE, il se colle devant son porteur ;
  /// LIBRE, il tombe, rebondit et roule ; POSE, il dort au fond d'un but apres un
  /// point. C'est une entite unique et non trois, parce que le ballon garde son
  /// identite d'un etat a l'autre - il ne doit ni clignoter ni changer de place au
  /// moment ou on le prend.
  ///
  /// Il herite d'Actor pour que les solides du jeu le POUSSENT comme ils poussent un
  /// archer - une plateforme mouvante l'emmene au lieu de le traverser. En revanche
  /// il ne se deplace pas avec MoveH/MoveV : il avance par ses propres pas, parce
  /// que ceux du jeu ne connaissent que les solides et ignoreraient les barres des
  /// buts.
  /// </summary>
  public class Ball : Actor
  {
    /// <summary>Devant les archers (-100) : le ballon est ce qu'on doit suivre des yeux.</summary>
    public const int DEPTH = -105;

    /// <summary>Rayon du ballon, en pixels.</summary>
    private const float RADIUS = 4f;

    /// <summary>Chute, en pixels par image et par image.</summary>
    private const float GRAVITY = 0.22f;

    /// <summary>Ce qu'il garde de sa vitesse en rebondissant sur un mur.</summary>
    private const float BOUNCE = 0.62f;

    /// <summary>Freinage au sol, par image. Sans lui le ballon glisse sans fin.</summary>
    private const float GROUND_FRICTION = 0.04f;

    /// <summary>Sous cette vitesse verticale, le ballon cesse de rebondir et se pose.</summary>
    private const float REST_SPEED = 0.6f;

    /// <summary>Vitesse maximale, pour qu'un tir ne traverse jamais un mur mince.</summary>
    private const float MAX_SPEED = 9f;

    /// <summary>
    /// Images pendant lesquelles le ballon ne peut pas etre repris apres un tir.
    ///
    /// Sans ce delai le tireur, qui est encore dessus a l'image suivante, le
    /// rattraperait aussitot : on n'arriverait jamais a s'en separer.
    /// </summary>
    private const float KICK_LOCK = 12f;

    /// <summary>Le porteur, ou null quand le ballon est libre.</summary>
    public Player Carrier { get; private set; }

    /// <summary>Dernier joueur a l'avoir touche.</summary>
    public int LastToucher { get; private set; } = -1;

    /// <summary>
    /// Pose pour de bon : le ballon est au fond d'un but et n'en bouge plus.
    /// Le point est marque, la manche se termine, plus rien ne doit le deplacer -
    /// surtout pas un dernier rebond qui le ferait ressortir sous les yeux de tous.
    /// </summary>
    public bool Settled { get; private set; }

    public Vector2 Speed;

    /// <summary>Ou le ballon revient s'il se retrouve nulle part. Le centre du terrain.</summary>
    public Vector2 Kickoff;

    private readonly Subtexture texture;
    private float lockTimer;
    private float spin;

    /// <summary>Sous-pixels en attente : un ballon avance par pas d'un pixel.</summary>
    private Vector2 remainder;

    public Ball(Vector2 position) : base(position)
    {
      Depth = DEPTH;
      Collider = new Hitbox(RADIUS * 2f, RADIUS * 2f, -RADIUS, -RADIUS);
      Kickoff = position;

      // Le terrain boucle comme tout le reste du jeu : un ballon sorti par un bord
      // ouvert ressort en face.
      ScreenWrap = true;

      texture = TextureRegistry.Ball?.Subtexture;
    }

    /// <summary>
    /// Prise du ballon par un archer. Sans effet pendant le delai qui suit un tir,
    /// si quelqu'un le porte deja, ou une fois le point marque.
    /// </summary>
    public bool TryTake(Player player)
    {
      if (player == null || player.Dead || Settled || lockTimer > 0f || Carrier != null)
      {
        return false;
      }

      Take(player);
      return true;
    }

    /// <summary>Passe le ballon a cet archer, quoi qu'il arrive. Le vol passe par ici.</summary>
    public void Take(Player player)
    {
      if (Settled)
      {
        return;
      }

      Carrier = player;
      LastToucher = player.PlayerIndex;
      Speed = Vector2.Zero;
      remainder = Vector2.Zero;
      lockTimer = 0f;
      Sounds.pu_coin.Play(X, 1f);
    }

    /// <summary>
    /// Le tir. La direction est celle de la visee, la puissance vient du temps
    /// d'appui (voir MyPlayer).
    /// </summary>
    public void Kick(Player kicker, float angle, float power)
    {
      if (Settled)
      {
        return;
      }

      LastToucher = kicker.PlayerIndex;
      Carrier = null;
      lockTimer = KICK_LOCK;

      Position = Muzzle(kicker);
      Speed = Calc.AngleToVector(angle, power);
      Clamp();

      Sounds.char_arrowCollide.Play(X, 1f);
    }

    /// <summary>Le ballon tombe des pieds de son porteur : mort, ou fin de manche.</summary>
    public void Drop()
    {
      if (Carrier == null)
      {
        return;
      }

      Position = Muzzle(Carrier);
      Carrier = null;
      lockTimer = KICK_LOCK * 0.5f;
      Speed = new Vector2(0f, -1f);
    }

    /// <summary>
    /// Immobilise le ballon la ou il est : il vient d'entrer dans un but.
    /// Plus aucune gravite, plus aucun rebond, plus aucune reprise.
    /// </summary>
    public void Settle()
    {
      Settled = true;
      Carrier = null;
      Speed = Vector2.Zero;
      remainder = Vector2.Zero;
    }

    /// <summary>
    /// Ou le ballon se tient quand il est porte : devant l'archer, a hauteur de
    /// pied. C'est ce qui fait qu'on voit tout de suite qui l'a, et dans quel sens
    /// il va tirer.
    /// </summary>
    private static Vector2 Muzzle(Player player)
    {
      return player.Position + new Vector2((int)player.Facing * 7f, -3f);
    }

    public override void Update()
    {
      base.Update();

      // A la premiere image et pas avant : le recalage interroge les solides du
      // niveau, et a l'instant ou le ballon est ajoute ils sont dans le meme lot
      // d'entites en attente que lui - l'ordre entre eux ne nous appartient pas.
      if (toPlace != null)
      {
        Place(toPlace.Value);
        toPlace = null;
      }

      if (Settled)
      {
        return;
      }

      if (lockTimer > 0f)
      {
        lockTimer -= Engine.TimeMult;
      }

      // Le porteur meurt ou quitte la scene : le ballon lui echappe.
      if (Carrier != null && (Carrier.Dead || Carrier.Scene == null))
      {
        Drop();
      }

      if (Carrier != null)
      {
        Position = Muzzle(Carrier);
        spin += (int)Carrier.Facing * Math.Abs(Carrier.Speed.X) * 0.1f;
        return;
      }

      Free();
    }

    /// <summary>Le ballon livre a lui-meme : gravite, rebonds, roulement, ramassage.</summary>
    private void Free()
    {
      Speed.Y += GRAVITY * Engine.TimeMult;
      Clamp();

      Advance();

      // Au sol : il roule et ralentit. Le test est fait apres le deplacement, donc
      // sur la position reelle du ballon.
      if (Blocked(Position + Vector2.UnitY))
      {
        Speed.X = Calc.Approach(Speed.X, 0f, GROUND_FRICTION * Engine.TimeMult);
      }

      spin += Speed.X * 0.12f;

      Player taker = CollideFirst(GameTags.Player) as Player;
      if (taker != null)
      {
        TryTake(taker);
      }
    }

    /// <summary>
    /// Avance d'un pixel a la fois, en s'arretant sur ce qui bloque.
    ///
    /// C'est le MoveH/MoveV d'Actor refait a la main, pour une seule raison : y
    /// ajouter les barres des buts, que les solides du jeu ne connaissent pas. Le pas
    /// d'un pixel garantit aussi qu'un tir a pleine puissance ne traverse jamais un
    /// mur mince.
    /// </summary>
    private void Advance()
    {
      remainder.X += Speed.X * Engine.TimeMult;
      int stepsX = (int)Math.Round(remainder.X);
      remainder.X -= stepsX;

      int dirX = Math.Sign(stepsX);
      for (int i = 0; i < Math.Abs(stepsX); i++)
      {
        if (Blocked(new Vector2(X + dirX, Y)))
        {
          OnHitH();
          break;
        }

        X += dirX;
      }

      remainder.Y += Speed.Y * Engine.TimeMult;
      int stepsY = (int)Math.Round(remainder.Y);
      remainder.Y -= stepsY;

      int dirY = Math.Sign(stepsY);
      for (int i = 0; i < Math.Abs(stepsY); i++)
      {
        if (Blocked(new Vector2(X, Y + dirY)))
        {
          OnHitV();
          break;
        }

        Y += dirY;
      }
    }

    /// <summary>
    /// Deplace le ballon vers l'endroit libre le plus proche du point vise.
    ///
    /// A poser APRES l'ajout au niveau : la recherche interroge les solides de la
    /// scene, qui n'existent pas encore pour une entite qui n'y est pas.
    ///
    /// Le centre du terrain n'est pas toujours vide - une plateforme, une colonne - et
    /// le ballon y naissait alors dans la pierre. On ne le voyait plus, et il fallait
    /// casser le mur pour le retrouver.
    ///
    /// La recherche part du point vise et s'en eloigne par anneaux carres : le premier
    /// endroit libre trouve est donc le plus proche, quelle que soit la direction. Un
    /// balayage vers le haut seulement echouerait sous un plafond.
    /// </summary>
    public void PlaceNear(Vector2 wanted)
    {
      Position = wanted;
      toPlace = wanted;
    }

    /// <summary>Position demandee, tant qu'elle n'a pas ete verifiee.</summary>
    private Vector2? toPlace;

    private void Place(Vector2 wanted)
    {
      if (!Blocked(wanted))
      {
        return;
      }

      // Un demi-ecran de rayon : au-dela, il n'y a plus de "proche du centre" qui
      // tienne, et mieux vaut le laisser dans la pierre - il en sortira au premier
      // coup de pied - que de l'expedier dans un coin.
      for (int ring = 2; ring <= 80; ring += 2)
      {
        for (int dx = -ring; dx <= ring; dx += 2)
        {
          for (int dy = -ring; dy <= ring; dy += 2)
          {
            // Seulement le POURTOUR de l'anneau : l'interieur a deja ete vu au tour
            // precedent.
            if (Math.Abs(dx) != ring && Math.Abs(dy) != ring)
            {
              continue;
            }

            var at = new Vector2(wanted.X + dx, wanted.Y + dy);

            if (at.Y >= 8f && !Blocked(at))
            {
              Position = at;
              return;
            }
          }
        }
      }
    }

    /// <summary>
    /// Quelque chose arrete-t-il le ballon a cette position : un solide du niveau,
    /// ou une barre de but ?
    /// </summary>
    private bool Blocked(Vector2 at)
    {
      if (CollideCheck(GameTags.Solid, at))
      {
        return true;
      }

      return BlockedByGoal(SoccerMatch.LeftGoal, at) || BlockedByGoal(SoccerMatch.RightGoal, at);
    }

    /// <summary>
    /// La barre est testee sur le POURTOUR du ballon et non sur son seul centre :
    /// avec un rayon de quatre pixels et des barres de trois, un centre encore
    /// dehors laisserait le dessin du ballon entrer dans le poteau.
    /// </summary>
    private static bool BlockedByGoal(Goal goal, Vector2 at)
    {
      if (goal == null)
      {
        return false;
      }

      return goal.BlocksBall(at)
          || goal.BlocksBall(at + new Vector2(-RADIUS, 0f))
          || goal.BlocksBall(at + new Vector2(RADIUS, 0f))
          || goal.BlocksBall(at + new Vector2(0f, -RADIUS))
          || goal.BlocksBall(at + new Vector2(0f, RADIUS));
    }

    private void Clamp()
    {
      if (Speed.Length() > MAX_SPEED)
      {
        Speed = Vector2.Normalize(Speed) * MAX_SPEED;
      }
    }

    private void OnHitH()
    {
      if (Math.Abs(Speed.X) > 1.2f)
      {
        Sounds.env_ballTouchJump.Play(X, 1f);
      }

      Speed.X = -Speed.X * BOUNCE;
      remainder.X = 0f;
    }

    private void OnHitV()
    {
      remainder.Y = 0f;

      // Un rebond qui n'a plus de quoi rebondir doit S'ARRETER, sinon le ballon
      // vibre indefiniment d'un demi-pixel sur le sol.
      if (Math.Abs(Speed.Y) < REST_SPEED)
      {
        Speed.Y = 0f;
        return;
      }

      if (Speed.Y > 1.2f)
      {
        Sounds.env_ballTouchJump.Play(X, 1f);
      }

      Speed.Y = -Speed.Y * BOUNCE;
    }

    // ------------------------------------------------------------------
    // L'ecrasement
    // ------------------------------------------------------------------

    /// <summary>
    /// Un Actor ecrase par un solide SE RETIRE DE LA SCENE : c'est ce que fait
    /// Actor.OnSquish*, et c'est ce qui a fait disparaitre le ballon sous une
    /// plateforme ecraseuse. Un ballon ne meurt pas - la manche n'aurait plus de quoi
    /// se jouer - alors on le pousse hors du solide, et s'il n'y a vraiment aucune
    /// place, il repart du centre du terrain.
    /// </summary>
    public override void OnSquishRight(Platform solid) => Escape(Vector2.UnitX);

    public override void OnSquishLeft(Platform solid) => Escape(-Vector2.UnitX);

    public override void OnSquishDown(Platform solid) => Escape(Vector2.UnitY);

    public override void OnSquishUp(Platform solid) => Escape(-Vector2.UnitY);

    /// <summary>
    /// Cherche une place libre autour du ballon, en s'eloignant par cercles
    /// concentriques, la direction de la poussee d'abord. Meme principe que le
    /// DoSquish des archers, qui ne les tue qu'en tout dernier recours.
    /// </summary>
    private void Escape(Vector2 pushed)
    {
      Vector2 from = Position;

      for (int distance = 1; distance <= 24; distance++)
      {
        // Dans le sens de la poussee en premier : c'est par la que la place se
        // libere, puisque le solide vient de l'autre cote.
        if (TryPlace(from + pushed * distance)) return;

        for (int dx = -distance; dx <= distance; dx++)
        {
          for (int dy = -distance; dy <= distance; dy++)
          {
            // Seulement le pourtour du carre : l'interieur a deja ete essaye au
            // tour precedent.
            if (Math.Abs(dx) != distance && Math.Abs(dy) != distance)
            {
              continue;
            }

            if (TryPlace(from + new Vector2(dx, dy))) return;
          }
        }
      }

      // Nulle part ou aller : le ballon revient au centre plutot que de rester
      // enferme dans la pierre.
      Position = Kickoff;
      Speed = Vector2.Zero;
      remainder = Vector2.Zero;
      Carrier = null;
      lockTimer = KICK_LOCK * 0.5f;
    }

    private bool TryPlace(Vector2 at)
    {
      Vector2 wrapped = WrapMath.ApplyWrap(at);
      if (Blocked(wrapped))
      {
        return false;
      }

      Position = wrapped;
      remainder = Vector2.Zero;

      // La vitesse verticale est coupee : le ballon vient d'etre pris entre deux
      // surfaces, le renvoyer a pleine vitesse le ferait repartir dans le solide.
      Speed.Y = 0f;
      return true;
    }

    /// <summary>
    /// Tout le dessin passe par DoWrapRender et non par Render : c'est LevelEntity
    /// qui l'appelle, une fois par copie decalee d'un ecran. Dessiner dans Render
    /// mettrait le ballon une fois de trop, sans son bouclage.
    /// </summary>
    public override void DoWrapRender()
    {
      base.DoWrapRender();

      if (texture != null)
      {
        // La rotation vient de la distance parcourue : le ballon roule au lieu de
        // glisser, et cela seul dit sa vitesse quand il file tout droit.
        Draw.TextureCentered(texture, Position, Color.White, 1f, spin);
        return;
      }

      // Repli sans texture : un carre blanc cercle de noir reste lisible, et le
      // mod continue de tourner meme si le PNG manque.
      Draw.Rect(X - RADIUS, Y - RADIUS, RADIUS * 2f, RADIUS * 2f, Color.Black);
      Draw.Rect(X - RADIUS + 1f, Y - RADIUS + 1f, RADIUS * 2f - 2f, RADIUS * 2f - 2f, Color.White);
    }
  }
}
