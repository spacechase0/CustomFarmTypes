using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI;
using xTile;
using Newtonsoft.Json;
using SObject = StardewValley.Object;
using Microsoft.Xna.Framework.Graphics;

namespace CustomFarmTypes
{
    public class FarmType
    {
        public class FarmBehavior
        {
            public class SpawnBehaviorArea
            {
                public double Chance { get; set; } = 1.0;
                public MyRectangle Area { get; set; }
                public List<SpawnBehaviorEntry> Entries { get; set; } = new List<SpawnBehaviorEntry>();

                public SpawnBehaviorArea() { }
                public SpawnBehaviorArea(MyRectangle area)
                {
                    Area = area;
                }
            }

            // This work as sequential ifs. No elses
            public class SpawnBehaviorEntry
            {
                public double Chance { get; set; } = 0;
                public double LuckFactor { get; set; } = 0;
                public bool SkipChanceDecrease { get; set; } = false;

                public bool OnlyTryIfNoneSelectedYet { get; set; } = false;

                // Option A: A single choice
                public int MiningLevelRequirement { get; set; } = -1;
                public int ObjectID { get; set; }
                public int InitialStack { get; set; } = 1;
                public int OreHealth { get; set; } = 0;

                // Option B: More behavior entries, except these act as if, else if.
                // The last option will always be chosen if the others aren't.
                public List<SpawnBehaviorEntry> SubEntries { get; set; }

                public SpawnBehaviorEntry()
                {
                }

                public SObject getObject()
                {
                    if (SubEntries != null)
                    {
                        foreach (var entry in SubEntries)
                        {
                            if (Game1.random.NextDouble() < entry.Chance)
                                return entry.getObject();
                        }
                        return SubEntries.Last().getObject();
                    }

                    return new SObject(new Vector2(), ObjectID, InitialStack) { minutesUntilReady = OreHealth };
                }

                public static SpawnBehaviorEntry Forageable(double chance, int obj)
                {
                    SpawnBehaviorEntry s = new SpawnBehaviorEntry();
                    s.Chance = chance;
                    s.ObjectID = obj;
                    return s;
                }

                public static SpawnBehaviorEntry Ore(double chance, int levelReq, int obj, int health)
                {
                    SpawnBehaviorEntry s = new SpawnBehaviorEntry();
                    s.Chance = chance;
                    s.MiningLevelRequirement = levelReq;
                    s.ObjectID = obj;
                    s.InitialStack = 10;
                    s.OreHealth = health;
                    return s;
                }

                public static SpawnBehaviorEntry chooseEntry(List<SpawnBehaviorEntry> entries)
                {
                    SpawnBehaviorEntry ret = null;
                    foreach (var entry in entries)
                    {
                        if (entry.OnlyTryIfNoneSelectedYet && ret != null)
                            continue;

                        if (Game1.random.NextDouble() <= entry.Chance)
                            ret = entry;
                    }
                    return ret == null ? entries.Last() : ret;
                }
            }

            // Fishing
            public class FishPoolDrawEntry
            {
                public MyRectangle Area { get; set; }
                public int PoolId { get; set; } = 0;
                public int ListFromLocationID { get; set; } = -1;

                public FishPoolDrawEntry() { }
                public FishPoolDrawEntry(MyRectangle area, int id, int list)
                {
                    Area = area;
                    PoolId = id;
                    ListFromLocationID = list;
                }

            }
            public class FishPoolEntry
            {
                public double Chance { get; set; }
                public double LuckFactor { get; set; } = 0;

                public bool OnlyTryIfNoneSelectedYet { get; set; } = false;

                // Option A: Just use the fish type from another location
                public string LocationPreset { get; set; }

                // Option B: A specific object
                public int ObjectID { get; set; }

                public static FishPoolEntry FinalPreset(string loc)
                {
                    FishPoolEntry fish = new FishPoolEntry();
                    fish.Chance = 1.0;
                    fish.OnlyTryIfNoneSelectedYet = true;
                    fish.LocationPreset = loc;
                    return fish;
                }

                public static FishPoolEntry Preset(double chance, string loc, double luck = 0)
                {
                    FishPoolEntry fish = new FishPoolEntry();
                    fish.Chance = chance;
                    fish.LuckFactor = luck;
                    fish.LocationPreset = loc;
                    return fish;
                }

                public static FishPoolEntry FinalObject(int obj)
                {
                    FishPoolEntry fish = new FishPoolEntry();
                    fish.Chance = 1.0;
                    fish.OnlyTryIfNoneSelectedYet = true;
                    fish.ObjectID = obj;
                    return fish;
                }

                public static FishPoolEntry Object(double chance, int obj, double luck = 0)
                {
                    FishPoolEntry fish = new FishPoolEntry();
                    fish.Chance = chance;
                    fish.LuckFactor = luck;
                    fish.ObjectID = obj;
                    return fish;
                }

                public SObject getObject()
                {
                    return new SObject(ObjectID, 1);
                }

                public static FishPoolEntry chooseEntry(List<FishPoolEntry> entries)
                {
                    FishPoolEntry ret = null;
                    foreach (var entry in entries)
                    {
                        if (entry.OnlyTryIfNoneSelectedYet && ret != null)
                            continue;

                        if (Game1.random.NextDouble() < entry.Chance + Game1.dailyLuck * entry.LuckFactor)
                            ret = entry;
                    }
                    return ret;
                }
            }
            public double FishingSplashChance { get; set; }
            public List<FishPoolDrawEntry> FishPoolToDrawFrom;
            public List<List<FishPoolEntry>> FishPools;

            // Foraging
            public bool RepopulateStumps { get; set; }
            public int SpecialWeedCount { get; set; }
            public double ForageableSpawnChanceBase { get; set; }
            public double ForageableSpawnChanceMultiplier { get; set; }
            public Dictionary<string, List<SpawnBehaviorArea>> ForageableSpawnBehavior { get; set; }

            // Mining
            public int NewSaveOreGenRuns { get; set; }
            public double OreSpawnChanceBase { get; set; }
            public double OreSpawnChanceMultiplier { get; set; }
            public List<SpawnBehaviorArea> OreSpawnBehavior { get; set; }

            // Combat
            public bool SpawnMonsters { get; set; }

            // ...
            public static SpawnBehaviorArea chooseSpawnArea(List<SpawnBehaviorArea> areas)
            {
                foreach (var area in areas)
                {
                    if (Game1.random.NextDouble() <= area.Chance)
                        return area;
                }
                return areas.Last();
            }
        }

        [JsonIgnore]
        public string Folder { get; set; }

        public string Name { get; set; }
        public string Description { get; set; } = "";
        public string ID { get; set; }

        [JsonIgnore]
        public virtual Texture2D Icon
        {
            get
            {
                return Mod.instance.Helper.Content.Load<Texture2D>(Folder + "/icon.png", ContentSource.ModFolder);
            }
        }

        // Only valid for farms using CustomFarm
        public int BehaviorPreset { get; set; } = Farm.default_layout;
        public FarmBehavior Behavior
        {
            get
            {
                if (behavior_ == null)
                    behavior_ = getFarmBehaviorFromPreset(BehaviorPreset);
                return behavior_;
            }
            set { behavior_ = value; }
        }
        private FarmBehavior behavior_;

        // TODO: House furniture and stuff
        
        public virtual Map loadMap()
        {
            return Mod.instance.Helper.Content.Load<Map>( Folder + "/map.xnb", ContentSource.ModFolder);
        }

        public virtual Farm getFarm( string loc )
        {
            return new CustomFarm(this, loc);
        }

        public static FarmType getFarmTypeFromPreset( int vanillaId )
        {
            FarmType f = new FarmType();
            f.Name = Farm.getMapNameFromTypeInt(vanillaId);
            f.ID = "StardewValley." + f.Name;
            f.BehaviorPreset = vanillaId;

            return f;
        }

        public static FarmBehavior getFarmBehaviorFromPreset(int vanillaId)
        {
            MyRectangle mapRect = new MyRectangle(0, 0, 80, 65);

            FarmBehavior b = new FarmBehavior();
            switch (vanillaId)
            {
                case Farm.default_layout:
                    break;
                case Farm.riverlands_layout:
                    {
                        b.FishingSplashChance = 0.5;
                        b.FishPoolToDrawFrom = new List<FarmBehavior.FishPoolDrawEntry>();
                        b.FishPoolToDrawFrom.Add(new FarmBehavior.FishPoolDrawEntry(mapRect, 0, 1));
                        var fish = new List<FarmBehavior.FishPoolEntry>();
                        fish.Add(FarmBehavior.FishPoolEntry.Preset(0.3, "Forest"));
                        fish.Add(FarmBehavior.FishPoolEntry.FinalPreset("Town"));
                        b.FishPools = new List<List<FarmBehavior.FishPoolEntry>>();
                        b.FishPools.Add(fish);
                    }
                    break;
                case Farm.forest_layout:
                    {
                        b.FishPoolToDrawFrom = new List<FarmBehavior.FishPoolDrawEntry>();
                        b.FishPoolToDrawFrom.Add(new FarmBehavior.FishPoolDrawEntry(mapRect, 0, 1));
                        var fish = new List<FarmBehavior.FishPoolEntry>();
                        fish.Add(FarmBehavior.FishPoolEntry.Object(0.5, 734, 1.0));
                        fish.Add(FarmBehavior.FishPoolEntry.Preset(0.5, "Forest"));
                        b.FishPools = new List<List<FarmBehavior.FishPoolEntry>>();
                        b.FishPools.Add(fish);

                        b.RepopulateStumps = true;
                        b.SpecialWeedCount = 6;

                        b.ForageableSpawnChanceBase = 0.75;
                        b.ForageableSpawnChanceMultiplier = 1.0;
                        b.ForageableSpawnBehavior = new Dictionary<string, List<FarmBehavior.SpawnBehaviorArea>>();

                        var spring = new FarmBehavior.SpawnBehaviorArea(mapRect);
                        spring.Entries.Add(FarmBehavior.SpawnBehaviorEntry.Forageable(0.25, 16));
                        spring.Entries.Add(FarmBehavior.SpawnBehaviorEntry.Forageable(0.25, 22));
                        spring.Entries.Add(FarmBehavior.SpawnBehaviorEntry.Forageable(0.25, 20));
                        spring.Entries.Add(FarmBehavior.SpawnBehaviorEntry.Forageable(0.00, 257)); // The last one is always chosen if none of the others are. No need to give it an extra 25% chance as well.
                        var springAreas = new List<FarmBehavior.SpawnBehaviorArea>();
                        springAreas.Add(spring);
                        b.ForageableSpawnBehavior.Add("spring", springAreas);

                        var summer = new FarmBehavior.SpawnBehaviorArea(mapRect);
                        summer.Entries.Add(FarmBehavior.SpawnBehaviorEntry.Forageable(0.25, 402));
                        summer.Entries.Add(FarmBehavior.SpawnBehaviorEntry.Forageable(0.25, 396));
                        summer.Entries.Add(FarmBehavior.SpawnBehaviorEntry.Forageable(0.25, 398));
                        summer.Entries.Add(FarmBehavior.SpawnBehaviorEntry.Forageable(0.00, 404));
                        var summerAreas = new List<FarmBehavior.SpawnBehaviorArea>();
                        summerAreas.Add(summer);
                        b.ForageableSpawnBehavior.Add("summer", summerAreas);

                        var fall = new FarmBehavior.SpawnBehaviorArea(mapRect);
                        fall.Entries.Add(FarmBehavior.SpawnBehaviorEntry.Forageable(0.25, 281));
                        fall.Entries.Add(FarmBehavior.SpawnBehaviorEntry.Forageable(0.25, 420));
                        fall.Entries.Add(FarmBehavior.SpawnBehaviorEntry.Forageable(0.25, 422));
                        fall.Entries.Add(FarmBehavior.SpawnBehaviorEntry.Forageable(0.00, 404));
                        var fallAreas = new List<FarmBehavior.SpawnBehaviorArea>();
                        fallAreas.Add(fall);
                        b.ForageableSpawnBehavior.Add("fall", fallAreas);
                    }
                    break;
                case Farm.mountains_layout:
                    {
                        b.FishPoolToDrawFrom = new List<FarmBehavior.FishPoolDrawEntry>();
                        b.FishPoolToDrawFrom.Add(new FarmBehavior.FishPoolDrawEntry(mapRect, 0, 1));
                        var fish = new List<FarmBehavior.FishPoolEntry>();
                        fish.Add(FarmBehavior.FishPoolEntry.Preset(0.5, "Forest"));
                        b.FishPools = new List<List<FarmBehavior.FishPoolEntry>>();
                        b.FishPools.Add(fish);

                        b.OreSpawnChanceBase = 1;
                        b.OreSpawnChanceMultiplier = 0.66;
                        b.NewSaveOreGenRuns = 28;

                        var chances = new FarmBehavior.SpawnBehaviorArea(new MyRectangle(5, 37, 22, 8));
                        var firstChance = FarmBehavior.SpawnBehaviorEntry.Ore(.15, -1, 590, 0);
                        firstChance.SkipChanceDecrease = true;
                        firstChance.InitialStack = 1;
                        chances.Entries.Add(firstChance);
                        chances.Entries.Add(FarmBehavior.SpawnBehaviorEntry.Ore(.5, -1, 670, 0));
                        var multiChance = new FarmBehavior.SpawnBehaviorEntry();
                        multiChance.Chance = .1;
                        multiChance.SubEntries = new List<FarmBehavior.SpawnBehaviorEntry>();
                        multiChance.SubEntries.Add(FarmBehavior.SpawnBehaviorEntry.Ore(.33, 8, 77, 7));
                        multiChance.SubEntries.Add(FarmBehavior.SpawnBehaviorEntry.Ore(.5, 5, 76, 5));
                        multiChance.SubEntries.Add(FarmBehavior.SpawnBehaviorEntry.Ore(1, -1, 75, 3));
                        chances.Entries.Add(multiChance);
                        chances.Entries.Add(FarmBehavior.SpawnBehaviorEntry.Ore(.21, -1, 751, 3));
                        chances.Entries.Add(FarmBehavior.SpawnBehaviorEntry.Ore(.15, 4, 290, 4));
                        chances.Entries.Add(FarmBehavior.SpawnBehaviorEntry.Ore(.1, 7, 764, 8));
                        chances.Entries.Add(FarmBehavior.SpawnBehaviorEntry.Ore(.01, 10, 765, 16));
                        chances.Entries.Add(FarmBehavior.SpawnBehaviorEntry.Ore(1.0, -1, 668, 2));

                        b.OreSpawnBehavior = new List< FarmBehavior.SpawnBehaviorArea >();
                        b.OreSpawnBehavior.Add(chances);
                    }
                    break;
                case Farm.combat_layout:
                    {
                        b.FishPoolToDrawFrom = new List<FarmBehavior.FishPoolDrawEntry>();
                        b.FishPoolToDrawFrom.Add(new FarmBehavior.FishPoolDrawEntry(mapRect, 0, 0));
                        var fish = new List<FarmBehavior.FishPoolEntry>();
                        fish.Add(FarmBehavior.FishPoolEntry.Preset(0.35, "Mountain"));
                        b.FishPools = new List<List<FarmBehavior.FishPoolEntry>>();
                        b.FishPools.Add(fish);

                        b.SpawnMonsters = true;
                    }
                    break;
            }

            return b;
        }

        private static Dictionary<string, FarmType> types = new Dictionary<string, FarmType>();

        public static void register( FarmType type )
        {
            if ( types.ContainsKey( type.ID ) )
            {
                Log.error("Type \"" + type.ID + "\" already registered.");
                return;
            }
            types.Add(type.ID, type);
        }

        public static FarmType getType( string id )
        {
            return types.ContainsKey( id ) ? types[id] : null;
        }

        public static List< FarmType > getTypes()
        {
            return types.Values.ToList();
        }
    }
}
