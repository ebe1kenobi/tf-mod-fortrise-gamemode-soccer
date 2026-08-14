# Soccer

A **football** versus mode for TowerFall, on FortRise 5.

Not a single arrow: one ball, two goals, two teams. You take the ball by walking over
it, you carry it in front of you, you kick with the shoot button - the harder the longer
you held it - and you lose it when someone shoves you sideways. A goal ends the round.

A mod for **FortRise 5** (>= 5.3.3).

## Installation

1. Install FortRise 5 and start the game through `FortRise.exe`.
2. Copy `release/tf-mod-fortrise-gamemode-soccer` into `<TowerFall>/FortRise/Mods/`.

## Playing

**SOCCER** joins the list of versus modes, next to Last Man Standing and Team
Deathmatch. It is a **team mode**: the game splits the archers into blue and red on the
select screen, and counts points per team.

**Two players are enough** - a team of one is still a team - and it goes up to as many
slots as the game has: four normally, **eight with WiderSet**.

> Eight really means eight. Two spots still assumed four: the spawn loop, which left
> players 5 to 8 without an archer on the pitch, and the charge array, which threw an
> `IndexOutOfRange` the moment a fifth player touched the ball.

| | |
|---|---|
| **Blue** | defends the left goal |
| **Red** | defends the right goal |

### The ball

- It drops in the **middle of the pitch** at the start of the round.
- **Walking over it** takes it. It then sits in front of the archer, at foot height, and
  follows the way they face: you can see at a glance who has it and where they will kick.
- **The shoot button** kicks. Held, it charges: the bar above the archer runs from white
  to red, and the power follows how long you held. The direction is the aiming direction
  - eight ways, or free with the game's *Free Aiming* variant.
- Once kicked, the ball flies **like an arrow**: gravity, bouncing off walls, rolling on
  the ground, and it comes back in on the far side when it leaves through an open edge.
- It cannot be picked up again for about a dozen frames after a kick, or the kicker -
  still standing on it - would catch it straight back.

### Taking it off someone

Nobody can kill anybody: there is nothing to do it with. The ball changes feet by
contact, at the place where the game already makes an arrow change hands:

- **Shoving** someone sideways takes it, provided you are pushing *towards* them. If
  both push towards each other, nobody takes anything: they bounce apart, and that is
  all.
- **Stomping** someone - landing on their head - takes **nothing**. It is a bounce and it
  stays one. Making it steal the ball would have turned jumping on people into the
  easiest way to get it back, and soccer would be played in the air.

That split is not arbitrary: `Player.PlayerOnPlayer` is exactly where the game already
tells a stomp from a shove, and where it already makes an arrow fly from one archer to
the other. The ball follows the same rule, in the same place - nothing to guess about
speeds or hitboxes.

### Scoring

A ball that enters a goal **ends the round** and gives the point to the team opposite
the one defending that goal - so an own goal counts for the other side, as in football.
A ball *carried* into the goal does not count: it has to be kicked.

Scoring, the results screen and the crown are the game's own: the round is built on the
skeleton of `TeamDeathmatchRoundLogic`, which is what everything else expects.

## Goals on a tower that was never drawn for them

A goal at a fixed position would only work on towers with open sides; anywhere else it
would end up buried in stone, and the ball could never get in.

So the position **is not negotiated**: a goal sits at the end of the pitch, at
mid-height, the two facing each other. Whatever tiles are in the way are **carved out** -
the cage is cut into the stone, exactly the way a beam punches through a wall in the
Power mod. A cell is switched off on both sides: in the `Grid`, which carries collisions,
and in the `bitData`, which carries the drawing, because Monocle's `Grid` constructor
clones the bit array. Then `ReloadTiles()` re-tiles the map.

An earlier version looked for a spot that was already free. The goals ended up wherever
the tower would let them - at floor level, sometimes at two different heights - and the
pitch was not symmetric.

**Three cells are cleared in front of the cage**, on the pitch side. The two-pixel margin
was enough to place the goal, not to reach it: on a tower with a wall running just in
front, the cage was cut into the stone and still unreachable - a goal you could see but
neither aim at nor cross. Three, and no more: the pitch has to stay a TowerFall pitch,
with its walls and its platforms.

### Why the goals are not flush against the edge

They are inset by **one tile**, and the strip behind them is **walled in**.

The screen wraps. A goal flush against the edge turns its own net into a door: leaving on
the left means coming back on the right, which amounts to walking through the goal. The
AIs were not cheating - they were using a passage that really existed.

The back wall runs **one cell above and below** the cage: without that, an archer passing
just over the post would come back on the other side of the screen, and the goal would
still be a door.

Nothing in there knows the geometry of any tower: it all goes through the tile grid, so a
tower added by another mod works too, and a level widened by WiderSet places its goals at
*its* edges without this code knowing.

The **ball** is born midway between the two goals, up high. That point is not always
empty - a platform, a column - and it used to be born inside the stone: you could not
see it, and you had to break the wall to get it back. It now looks for the nearest free
spot, in square rings, on the level's first frame: at the moment it is added, the solids
are in the same batch of entities as it is, and the order between them is not ours.

## What is switched off

- **Arrows**: the quiver is emptied when the level loads, and `ShootArrow` never creates
  an arrow - the button is the kick.
- **Everything on screen that talks about arrows**: the counter above the head, the bow,
  and the aimer that follows the aiming direction. The gesture is the same as a shot, so
  the game used to draw a nocked arrow on every kick: it announced a shot that would
  never come.
- **Chests**: they would only give arrows and things that kill.

The tower's traps stay: a lava or spike tower can still kill. An archer who dies drops
the ball, and if a whole team falls the other one takes the round - otherwise the match
would be stuck on a pitch with nobody to play.

## What is drawn

- An **arrow** above the carrier: at a glance, who has the ball.
- A **power bar** above them while charging, white to red. It used to be under their
  feet, which meant inside the ground - an archer's origin *is* at scenery level, and
  twelve pixels lower put it out of sight.

## Settings

None. The mode is picked from the versus mode list, and everything else is fixed in
code: `Ball.cs` for gravity, bouncing and braking, `SoccerMatch.cs` for kick power,
`Goal.cs` for goal size.

## API for other mods

The mod exposes `ISoccerApi`, enough to play with the ball without knowing the mode:

| Member | What it gives |
|--------|---------------|
| `IsSoccerMatch()` | is the current match a soccer match |
| `BallPosition()` | where the ball is |
| `BallCarrier()` | who carries it, or `-1` if it is loose |
| `TargetGoal(playerIndex)` | the goal this player must score in - the far one |

The goal is given **per player** and not per team: the caller knows which archer it is
talking about, not which colour, and that mapping belongs to this mod.

It is used by **AIJimmy**, which goes for the ball and then for the far goal. Without
it the AI chased the nearest player and pressed shoot - that is, it kicked at thin air,
the ball being somewhere else.

## Build / deployment

| Script | Purpose |
|--------|---------|
| `script/release.bat` | build, then assemble into `release/` |
| `script/deploy.bat` | copy `release/` into the TowerFall `Mods` folder |

Paths (game folder, module name) are set in `script/config.bat`.
