// Copyright (c) 2022 George Churochkin. All rights reserved.
// Licensed under the MIT License (https://opensource.org/licenses/MIT)
// Full license text: see LICENSE file in the root folder of this repository

using System;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

enum Threat 
{
    Neither = 0,
    You = 1,
    Opponent = 2
}

enum MapTriangle
{
    TopRight = 0,
    BottomLeft = 1
}

class Position
{
    protected const int MAXX = 17630;
    protected const int MAXY = 9000;

    public double X { get; protected set; }
    public double Y { get; protected set; }  

    public static Position Center()
    {
        return new Position(MAXX / 2, MAXY / 2);
    }

    public static Position TopRight()
    {
        return new Position(MAXX, 0);
    }

    public static Position BottomLeft()
    {
        return new Position(0, MAXY);
    }

    public static Position BottomRight()
    {
        return new Position(MAXX, MAXY);
    }

    public Position(double x, double y)
    {
        X = x;
        Y = y;
    }    

        public bool IsValid()
    {
        return (X >= 0 && X <= MAXX && Y >= 0 && Y <= MAXY);
    }

    public double GetDistance(Position from)
    {
        var dx = from.X - X;
        var dy = from.Y - Y;
        return Math.Sqrt((double)(dx * dx + dy * dy));
    }

    public double GetDistanceFromLine(Position start, Position end)
    {
        var a = start.GetDistance(end);
        var b = start.GetDistance(this);
        var c = end.GetDistance(this);
        var s = (a + b + c) / 2;
        return 2 * Math.Sqrt(s * (s-a) * (s-b) * (s-c)) / a;
    }

    public double GetCos(Position left, Position right)
    {
        var b = left.GetDistance(right);
        var a = GetDistance(left);
        var c = GetDistance(right);
        return (b * b - a * a - c * c) / (-2d * a * c);
    }

    public MapTriangle GetContainingMapTriangle()
    {
        var xp = MAXX * Y - MAXY * X;
        return xp > 0 ? MapTriangle.TopRight : MapTriangle.BottomLeft;
    }
}

static class PositionEnumerableExtensions
{
    public static Position GetAveragePosition(this IEnumerable<Position> positions)
    {
        if (positions == null || !positions.Any()) return null;
        double x = 0;
        double y = 0;
        int count = 0;
        foreach (var p in positions)
        {
            x += p.X;
            y += p.Y;
            count++;
        }
        return new Position(x / count, y / count);
    }
}

enum Command
{
    [Description("Confundo")]
    Wait = 0,
    [Description("Accio")]
    Move = 1,
    [Description("Leviossa")]
    SpellWind = 2,
    [Description("Patronum")]
    SpellShield = 3,
    [Description("Imperius")]
    SpellControl = 4
}

static class CommandExtensions
{
    private static string GetPrefix(this Command c)
    {
        var t = c.ToString();
        var sb = new StringBuilder(t.Length * 2);
        sb.Append(t[0]);
        for (var i = 1; i < t.Length; i++)
        {
            if (char.IsUpper(t[i])) sb.Append(' ');
            sb.Append(char.ToUpper(t[i]));
        }
        sb.Append(' ');
        return sb.ToString();
    }

    private static string GetDescription(this Command c)
    {
        return ((DescriptionAttribute)Attribute.GetCustomAttribute((c.GetType().GetField(c.ToString())), typeof(DescriptionAttribute))).Description;
    }

    public static void Send(this Command c, IEnumerable<object> commandParameters, string heroName)
    {
        Console.WriteLine(c.GetPrefix() + 
            string.Join(' ', 
                commandParameters.Aggregate(new List<string>(), 
                    (acc, el) => 
                    {
                        if (el is double)
                        {
                            acc.Add(((int)(Math.Round((double)el))).ToString());
                        }
                        else
                        {
                            acc.Add(el.ToString());
                        }
                        return acc;
                    }
                ).Concat(new string[]{ heroName, c.GetDescription() })
            )
        );
    }
}

class Player
{
    const int TYPE_MONSTER = 0;
    const int TYPE_HERO = 1;
    const int TYPE_OPPONENT = 2;

    const double GOLDEN_RATIO = 1.618d;

    const double COS_SIN_45 = 0.707106781d;

    const double COS_15 = 0.96592582d;
    const double SIN_15 = 0.25881904d;

    const double COS_225 = 0.92387953d;
    const double SIN_225 = 0.38268343d;

    const int SPELL_COST = 10;

    const int WIND_SPEED = 2200;
    const int WIND_DAMAGE_RANGE = 1280;

    const int SHIELD_DAMAGE_RANGE = 2200;
    const int SHIELD_LIFE = 12;

    const int CONTROL_DAMAGE_RANGE = 2200;

    class Base: Position
    {
        public int Health { get;  set; }

        public Base(int x, int y): base(x, y)
        {
        }
    }

    class Entity: Position
    {
        public int ID { get; protected set; }

        public Entity(int id, int x, int y): base(x, y)
        {
            ID = id;
        }
    }

    struct Character
    {
        public double Attacker { get; private set; } // attacks opponents base
        public double Defender { get; private set; } // defends own base
        public double Duelist { get; private set; } // attacks opponent's heroes
        public double Hunter { get; private set; } // attacks monsters
        public double Prodigal { get; private set; } // saves less mana
        public double Conservator { get; private set; } // tends to return to initial position

        public Character(double attacker, double defender, double duelist, double hunter, double prodigal, double conservator)
        {
            Attacker = attacker;
            Defender = defender;
            Duelist = duelist;
            Hunter = hunter;
            Prodigal = prodigal;
            Conservator = conservator;
        }
    }

    struct Consequence
    {
        public IList<int> Targeted {get; private set;}
        public int ManaCost {get; private set;}
        public Consequence(IList<int> targeted, int manaCost)
        {
            Targeted = targeted;
            ManaCost = manaCost;
        }
    }

    class Hero: Entity
    {
        public const int SPEED = 800;
        public const int DAMAGE = 2;
        public const int DAMAGE_RANGE = 800;
        public const int VISIBILITY_RANGE = 2200;

        public readonly string Name;

        public readonly Position GuardPosition;
        public readonly Character Character;

        public int ShieldLife {get; private set;}

        public Hero(int id, int number, int x, int y, int shieldLife, Position myBase): base(id, x, y) 
        {
            ShieldLife = shieldLife;

            var radius = (int)(Hero.VISIBILITY_RANGE * GOLDEN_RATIO);
            var side1 = (int)(radius * COS_225 * GOLDEN_RATIO);
            var side2 = (int)(radius * SIN_225 * GOLDEN_RATIO);
            var guardX = (int)radius;
            var guardY = guardX;
            if (number == 0)
            {
                guardX = (int)(Hero.VISIBILITY_RANGE);
                guardY = (int)(Hero.VISIBILITY_RANGE);
                Character = new Character(5, 40000, 1000000, 0.5, 0.0001, 0.0000002);
                Name = "REX";

            }
            else if (number == 1)
            {
                guardX = (int)(Position.Center().X * GOLDEN_RATIO);
                guardY = (int)(Position.Center().Y * GOLDEN_RATIO);
                Character = new Character(2000, 3, 1000000, 20, 0.001, 0.0000002);
                Name = "TEX";
            }
            else 
            {
                guardX = side2;
                guardY = side1;
                Character = new Character(50, 40000, 100000, 1, 0.0001, 0.0000002);
                Name = "TAU";
            }

            if (myBase.X == 0)
            {
                GuardPosition = new Position(guardX, guardY);
            }
            else
            {
                GuardPosition = new Position(myBase.X - guardX, myBase.Y - guardY);
            }

        }

        // if monster will not change direction
        public double ApproxTimeToReachMonster(Monster m)
        {
            var cosA = m.GetCos(m.V, this);
            var distance = m.GetDistance(this) - WIND_DAMAGE_RANGE; // we should consider wind spell range here cause it's more than sword range
            if (distance < 0) return 0;
            var a = SPEED * SPEED - Monster.SPEED * Monster.SPEED;
            var b = 2 * distance * Monster.SPEED * cosA;
            var c = - distance * distance;
            var d = b - 4 * a * c;
            if (d < 0) return -1;
            return (-b + Math.Sqrt(d)) / (2 * a);
        }


        public IList<Monster> GetMonstersInRange(IEnumerable<Monster> monsters, int range, bool step)
        {
            var r = new List<Monster>();
            foreach (var m in monsters)
            {            
                if (m.ApproxPositionAfter(step ? 1 : 0).GetDistance(this) <= range) r.Add(m); // "на ход ноги"  
            }
            return r;
        }

        public IList<Opponent> GetOpponentsInRange(IEnumerable<Opponent> opponents, int range)
        {
            var r = new List<Opponent>();
            foreach (var o in opponents)
            {            
                if (o.GetDistance(this) <= range - Hero.SPEED) r.Add(o);
            }
            return r;
        }

        public IList<Hero> GetHeroesInRange(IEnumerable<Hero> heroes, int range)
        {
            var r = new List<Hero>();
            foreach (var h in heroes)
            {            
                if (h.GetDistance(this) <= range - Hero.SPEED) r.Add(h);
            }
            return r;
        }

        private double InfiniteScalarsToWeight(double moreIsBetter, double lessIsBetter) // max 1, min 0
        {
            return 1 - (1/((moreIsBetter * moreIsBetter) / (lessIsBetter * lessIsBetter) + 1));
        }

        public Consequence MakeDecision(
            IEnumerable<Hero> heroes,
            IEnumerable<Monster> monsters, 
            IEnumerable<Opponent> opponents, 
            Base myBase, 
            Base opBase, 
            int manaLeft,
            int turn)
        {
            Command Command = Command.Wait;
            Object[] Parameters = new object[0];
            IList<int> Targeted  = new List<int>();
            double weight = 0;
            if (manaLeft >= SPELL_COST)
            {
                // check monsters on range of wind spell and throw to opponent
                var monstersInWindRange = GetMonstersInRange(monsters, WIND_DAMAGE_RANGE, false).Where(m => m.ShieldLife <= 1).ToList();
                
                // emergency button
                if (monstersInWindRange.Any(m => m.TimeToReachMyBase <= Math.Ceiling(m.Health / (Hero.DAMAGE * 3d))))
                {
                    Command = Command.SpellWind;
                    Parameters = new object[] {opBase.X, opBase.Y};
                    Command.Send(Parameters, Name);
                    return new Consequence(
                        monstersInWindRange.Select(m => m.ID).ToList(),
                        SPELL_COST);
                }

                /*if (monstersInWindRange.Count > 0 && !monstersInWindRange.Any(m => m.Threat == Threat.Opponent))
                {
                    var nearestMonster = monstersInWindRange.First();
                    int totalMonsterHealth = nearestMonster.Health;
                    foreach (var monster in monstersInWindRange.Skip(1))
                    {
                        totalMonsterHealth += monster.Health;
                        if (nearestMonster.DistanceFromMyBase < monster.DistanceFromMyBase)
                        {
                            nearestMonster = monster;
                        }
                    }

                    var opponentsInWindRange = GetOpponentsInRange(opponents, WIND_DAMAGE_RANGE).Where(o => o.ShieldLife <= 1).ToList();

                    var windWeight = 
                        InfiniteScalarsToWeight(
                            turn *
                            manaLeft *
                            Character.Prodigal *
                            Character.Defender *
                            totalMonsterHealth  *
                            (opponentsInWindRange.Count() + 1 + Character.Duelist), 

                            nearestMonster.DistanceFromMyBase * 
                            myBase.Health * 
                            Character.Hunter);

                    Console.Error.WriteLine($"Monst wind weight = {windWeight} for hero {Name}");
                    if (windWeight > weight)
                    {
                        Targeted = monstersInWindRange.Select(m => m.ID).Concat(opponentsInWindRange.Select(o => o.ID)).ToList();
                        Command = Command.SpellWind;
                        Parameters = new object[] {opBase.X, opBase.Y};
                        weight = windWeight;
                    }
                }*/

                // TODO - check opp hero and move away from monsters (control spell)
                var opponentsInControlRange = GetOpponentsInRange(opponents, CONTROL_DAMAGE_RANGE).Where(o => o.ShieldLife <= 1).ToList();
                if (opponentsInControlRange.Any())
                {
                    var farthestOpponentFromMiddleLine = opponentsInControlRange.First();
                    foreach (var opponent in opponentsInControlRange.Skip(1))
                    {
                        if (farthestOpponentFromMiddleLine.DistanceFromMiddleLine < opponent.DistanceFromMiddleLine)
                        {
                            farthestOpponentFromMiddleLine = opponent;
                        }
                    }

                    var opponentControlWeight = 
                        InfiniteScalarsToWeight(
                            turn *
                            manaLeft *
                            Character.Prodigal *
                            Character.Duelist *
                            farthestOpponentFromMiddleLine.DistanceFromMiddleLine,

                            1
                        );

                    Console.Error.WriteLine($"Op control weight = {opponentControlWeight} for hero {Name}");

                    if (opponentControlWeight > weight)
                    {
                        Command = Command.SpellControl;
                        Targeted = new int[1]{farthestOpponentFromMiddleLine.ID};
                        Parameters = new object[] {farthestOpponentFromMiddleLine.ID, Position.Center().X, Position.Center().Y}; // always move away from any base

                        weight = opponentControlWeight;
                    } 
                }

                // check monsters in shield range and cast shield
                var monstersInShieldRange = GetMonstersInRange(monsters, SHIELD_DAMAGE_RANGE, false).Where(m => m.ShieldLife <= 1).ToList();;
                {
                    foreach (var monster in monstersInShieldRange.Where(m => 
                        heroes.All(h => m.DistanceFromOpBase < opBase.GetDistance(h))))
                    {

                        if ((monster.Threat == Threat.Opponent || monster.TimeToReachOpBase <= SHIELD_LIFE) && 
                            !heroes.Any(h => h.GetDistance(monster) <= Hero.DAMAGE_RANGE + Hero.SPEED) &&
                            !opponents.Any(o => o.GetDistance(monster) <= Hero.DAMAGE_RANGE + Hero.SPEED))
                        {
                            var shieldWeight =
                                InfiniteScalarsToWeight(
                                    turn *
                                    manaLeft *
                                    Character.Prodigal *
                                    monster.Health *
                                    Character.Attacker, 

                                    monster.DistanceFromOpBase *
                                    Character.Hunter
                                );

                            Console.Error.WriteLine($"Shield weight = {shieldWeight} for hero {Name}");

                            if (shieldWeight > weight)
                            {
                                Parameters = new object[] {monster.ID};
                                Targeted = new int[1]{monster.ID};
                                Command = Command.SpellShield;
                                weight = shieldWeight;
                            }  
                        }  
                    }
                }

                // check monsters in control range and send to opps base
                var monstersInControlRange = GetMonstersInRange(monsters, CONTROL_DAMAGE_RANGE, false).Where(m => m.ShieldLife <= 1).ToList();;
                {
                    // emergency button
                    var wantedForControl = monstersInControlRange.FirstOrDefault(m => m.TimeToReachMyBase <= Math.Ceiling(m.Health / (Hero.DAMAGE * 3d)));
                    if (wantedForControl != null)
                    {
                        Command = Command.SpellControl;
                        Targeted = new int[1]{wantedForControl.ID};
                        Parameters = new object[] {wantedForControl.ID, opBase.X, opBase.Y};
                        Command.Send(Parameters, Name);
                        return new Consequence(
                            null, // single control spell is not sufficient to deal with threat
                            SPELL_COST);
                    }

                    foreach (var monster in monstersInControlRange)
                    {

                        if ((monster.Threat == Threat.Neither) && 
                            heroes.All(h => monster.DistanceFromOpBase < opBase.GetDistance(h)) &&
                            !heroes.Any(h => h.GetDistance(monster) <= Hero.DAMAGE_RANGE + Hero.SPEED) &&
                            !opponents.Any(o => o.GetDistance(monster) <= Hero.DAMAGE_RANGE + Hero.SPEED))
                        {
                            var controlWeight = 
                                InfiniteScalarsToWeight(
                                    turn *
                                    manaLeft *
                                    Character.Prodigal *
                                    monster.Health *
                                    Character.Attacker,

                                    monster.DistanceFromOpBase *
                                    Character.Hunter
                                );

                            Console.Error.WriteLine($"Control weight = {controlWeight} for hero {Name}");

                            if (controlWeight > weight)
                            {
                                // point them to corners of op base visibility range (based on the map triangle)
                                double aimX = Monster.VISIBILITY_RANGE / GOLDEN_RATIO;
                                double aimY = aimX;
                                if (monster.GetContainingMapTriangle() == MapTriangle.TopRight)
                                {
                                    aimX = (opBase.X <= double.Epsilon) ? 0 : opBase.X - aimX;
                                    aimY = (opBase.X <= double.Epsilon) ? aimY : opBase.Y;
                                }
                                else // bottom left
                                {
                                    aimX = (opBase.X <= double.Epsilon) ? aimX : opBase.X;
                                    aimY = (opBase.X <= double.Epsilon) ? 0 : opBase.Y - aimY;
                                }

                                Command = Command.SpellControl;
                                Targeted = new int[1]{monster.ID};
                                Parameters = new object[] {monster.ID, aimX, aimY};

                                weight = controlWeight;
                            }  
                        }  
                    }
                }
            }
            
            var monstersInMoveRange = monsters.Where(monster => // no need to seek monsters out of reach
            {
                return double.IsInfinity(monster.TimeToReachMyBase) || (ApproxTimeToReachMonster(monster) <= monster.TimeToReachMyBase);
            }).ToList();

            foreach (var monster in monsters.Where(m => m.Threat != Threat.Opponent))
            {
                var approxTimeToReachMonster = ApproxTimeToReachMonster(monster);

                // check if meeting point is out of the map
                var meetingPointPosition = (approxTimeToReachMonster <= 0) ? 
                    monster.ApproxPositionAfter(1) : 
                    monster.ApproxPositionAfter(approxTimeToReachMonster + 1); // "на ход ноги"

                if (!meetingPointPosition.IsValid()) continue;

                var huntMoveWeight =
                    InfiniteScalarsToWeight(
                        Character.Hunter *
                        monster.Health,

                        turn *
                        manaLeft *
                        GetDistance(monster)
                    );

                var defendMoveWeight =
                    InfiniteScalarsToWeight(
                        turn *
                        Character.Defender,

                        monster.DistanceFromMyBase
                    );
                
                Console.Error.WriteLine($"Defend move weight = {defendMoveWeight} for hero {Name}");
                
                var moveWeight = Math.Max(huntMoveWeight, defendMoveWeight);

                if (moveWeight > weight)
                {
                    Command = Command.Move;
                    Targeted = new int[1]{monster.ID};
                    if (approxTimeToReachMonster <= 0)
                    {
                        // just move towards monster next step
                        var nextMonsterPosition = monster.ApproxPositionAfter(1);
                        Parameters = new object[] {nextMonsterPosition.X, nextMonsterPosition.Y};
                    }
                    else
                    {
                        // move towards approx future meeting position plus next step
                        var meetingMonsterPosition = monster.ApproxPositionAfter(approxTimeToReachMonster + 1);
                        Parameters = new object[] {meetingMonsterPosition.X, meetingMonsterPosition.Y};
                    }
                    weight = moveWeight;
                }
            }

            var guardWeight = 
                InfiniteScalarsToWeight(
                    GetDistance(GuardPosition) *
                    Character.Conservator,

                    turn *
                    Character.Hunter
                );

            Console.Error.WriteLine($"Guard weight = {guardWeight} for hero {Name}");

            if (guardWeight > weight)
            {
                Command = Command.Move;
                Parameters = new object[] {GuardPosition.X, GuardPosition.Y};
            }

            if (!monsters.Any(m => m.Threat != Threat.Opponent) && manaLeft >= SPELL_COST && Command == Command.Wait)
            {
                var unshieldedHeroes = GetHeroesInRange(heroes, SHIELD_DAMAGE_RANGE).Where(h => h.ShieldLife <= 1);
                if (unshieldedHeroes.Any())
                { 
                    var heroToShield = unshieldedHeroes.First();// TODO - choose more wisely
                    Parameters = new object[] {heroToShield.ID};
                    Targeted = new int[1]{heroToShield.ID};
                    Command = Command.SpellShield;
                }
            }

            // apply 
            Command.Send(Parameters, Name);
            return new Consequence(
                ((int)Command > (int)Command.SpellWind) ? Targeted : null,
                ((int)Command > (int)Command.Move) ? 10 : 0);
        }
    }

    class Monster: Entity
    {
        public const int SPEED = 400;
        public const int DAMAGE_RANGE = 300;
        public const int VISIBILITY_RANGE = 5000;

        public static readonly double TIME_FROM_VISION_TO_DAMAGE = (VISIBILITY_RANGE - DAMAGE_RANGE) / (double)(SPEED);        

        public int Health { get; private set; }
        public Position V { get; private set; }
        public bool NearBase { get; private set; }
        public Threat Threat { get; private set; }
        public int ShieldLife { get; private set; }

        public double TimeToReachMyBase { get; private set; }
        public double TimeToReachOpBase { get; set; }

        public double DistanceFromMyBase { get; private set; }
        public double DistanceFromOpBase { get; private set; }         

        public Monster(int id, int x, int y, int health, int vx, int vy, int nearBase, int threatFor, int shieldLife, Position myBase, Position opBase): base(id, x, y)
        {
            Health = health;
            V = new Position(vx, vy);
            NearBase = nearBase != 0;
            Threat = (Threat)threatFor;
            ShieldLife = shieldLife;

            // calculate times to reach base
            DistanceFromMyBase = myBase.GetDistance(this);
            DistanceFromOpBase = opBase.GetDistance(this);
            if (NearBase)
            {
                if (DistanceFromMyBase <= DAMAGE_RANGE) 
                {
                    TimeToReachMyBase = 0;
                    TimeToReachOpBase = double.PositiveInfinity;
                    return;
                }
                if (DistanceFromMyBase <= VISIBILITY_RANGE) 
                {
                    TimeToReachMyBase = (DistanceFromMyBase - DAMAGE_RANGE) / SPEED;
                    TimeToReachOpBase = double.PositiveInfinity;
                    return;
                }
                if (DistanceFromOpBase <= DAMAGE_RANGE) 
                {
                    TimeToReachOpBase = 0;
                    TimeToReachMyBase = double.PositiveInfinity;
                    return;
                }
                TimeToReachOpBase = (DistanceFromOpBase - DAMAGE_RANGE) / SPEED;
                TimeToReachMyBase = double.PositiveInfinity;
                return;
            }
            if (Threat == Threat.Neither)
            {
                TimeToReachMyBase = double.PositiveInfinity;
                TimeToReachOpBase = double.PositiveInfinity;
                return;
            }
            if (Threat == Threat.You)
            {
                TimeToReachOpBase = double.PositiveInfinity;
                TimeToReachMyBase = (DistanceFromMyBase - DAMAGE_RANGE) / SPEED;
                // TODO - more precise calc
                return;
            }
            if (Threat == Threat.Opponent)
            {
                TimeToReachMyBase = double.PositiveInfinity;
                TimeToReachOpBase = (DistanceFromOpBase - DAMAGE_RANGE) / SPEED;
                // TODO - more precise calc
                return;
            }
        }

        // if monster will not change direction
        public Position ApproxPositionAfter(double t)
        {
            var dx = V.X - X;
            var dy = V.Y - Y;
            var l = Math.Sqrt((double)(dx * dx + dy * dy));
            var ux = dx / l;
            var uy = dy / l;
            return new Position(X + ux * SPEED * t, Y + uy * SPEED * t);
        }
    }

    class Opponent: Entity
    {
        public double DistanceFromMiddleLine { get; private set; }
        public int ShieldLife { get; private set; }

        public Opponent(int id, int x, int y, int shieldLife): base(id, x, y) 
        {
            ShieldLife = shieldLife;
            DistanceFromMiddleLine = this.GetDistanceFromLine(Position.BottomLeft(), Position.TopRight());
        }
    }

    static void Main(string[] args)
    {
        string[] inputs;
        inputs = Console.ReadLine().Split(' ');
        int baseX = int.Parse(inputs[0]); // The corner of the map representing your base
        int baseY = int.Parse(inputs[1]);
        int heroesPerPlayer = int.Parse(Console.ReadLine()); // Always 3

        var myBase = new Base(baseX, baseY);
        var opBase = baseX == 0 ? new Base((int)(Position.BottomRight().X), (int)(Position.BottomRight().Y)): new Base(0, 0);

        Console.Error.WriteLine($"baseX = {baseX}, baseY = {baseY}");

        int turn = 0;

        // game loop
        while (true)
        {
            turn++;

            inputs = Console.ReadLine().Split(' ');
            myBase.Health = int.Parse(inputs[0]); // Your base health
            int manaLeft = int.Parse(inputs[1]); // Your mana

            inputs = Console.ReadLine().Split(' ');
            opBase.Health = int.Parse(inputs[0]); // Opp base health
            int opponentManaLeft = int.Parse(inputs[1]); // Opp mana

            int entityCount = int.Parse(Console.ReadLine()); // Amount of heros and monsters you can see
            
            int heroesCount = 0;
            var heroes = new Hero[heroesPerPlayer];
            var monsters = new List<Monster>(entityCount - heroesPerPlayer);
            var opponents = new List<Opponent>(heroesPerPlayer);


            for (int i = 0; i < entityCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int id = int.Parse(inputs[0]); // Unique identifier
                int type = int.Parse(inputs[1]); // 0=monster, 1=your hero, 2=opponent hero
                int x = int.Parse(inputs[2]); // Position of this entity
                int y = int.Parse(inputs[3]);
                int shieldLife = int.Parse(inputs[4]); // Count down until shield spell fades
                int isControlled = int.Parse(inputs[5]); // Equals 1 when this entity is under a control spell
                int health = int.Parse(inputs[6]); // Remaining health of this monster
                int vx = int.Parse(inputs[7]); // Trajectory of this monster
                int vy = int.Parse(inputs[8]);
                int nearBase = int.Parse(inputs[9]); // 0=monster with no target yet, 1=monster targeting a base
                int threatFor = int.Parse(inputs[10]); // Given this monster's trajectory, is it a threat to 1=your base, 2=your opponent's base, 0=neither

                switch (type) 
                {
                    case TYPE_HERO:
                        heroes[heroesCount] = new Hero(id, heroesCount, x, y, shieldLife, myBase);
                        heroesCount++;
                        break;
                    case TYPE_MONSTER:
                        monsters.Add(new Monster(id, x, y, health, vx, vy, nearBase, threatFor, shieldLife, myBase, opBase));
                        break;
                    case TYPE_OPPONENT:
                        opponents.Add(new Opponent(id, x, y, shieldLife));
                        break;
                }
            }

            // exclude some monsters
            monsters = monsters.Where(m => !(m.TimeToReachMyBase <= double.Epsilon || m.TimeToReachOpBase <= double.Epsilon)).ToList();

            for (int i = 0; i < heroesPerPlayer; i++)
            {
                var consequence = heroes[i].MakeDecision(heroes, monsters, opponents, myBase, opBase, manaLeft, turn);
                manaLeft -= consequence.ManaCost;
                if (consequence.Targeted != null) 
                {
                    monsters = monsters.Where(m => consequence.Targeted.Any(t => t != m.ID)).ToList();
                    opponents = opponents.Where(o => consequence.Targeted.Any(t => t != o.ID)).ToList();
                    // heroes = heroes.Where(h => consequence.Targeted.Any(t => t != h.ID)).ToList();
                }
                
                // To debug: Console.Error.WriteLine("Debug messages...");                
            }
        }
    }
}