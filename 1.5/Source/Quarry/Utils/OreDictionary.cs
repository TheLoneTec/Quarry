using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Quarry
{

    public static class OreDictionary
    {

        public const int MaxWeight = 1000;

        private static System.Random rand = new System.Random();

        private static SimpleCurve commonalityCurve = new SimpleCurve {
            {new CurvePoint(0.0f, 10f)},
            {new CurvePoint(0.02f, 9f)},
            {new CurvePoint(0.04f, 8f)},
            {new CurvePoint(0.06f, 6f)},
            {new CurvePoint(0.08f, 3f)},
            {new CurvePoint(float.MaxValue, 1f)}
        };

        private static Predicate<ThingDef> validOre = (
            (ThingDef def) => def.mineable &&
                              def != QuarryDefOf.MineableComponentsIndustrial &&
                              def.building != null &&
                              def.building.isResourceRock &&
                              def.building.mineableThing != null &&
                              def.building.mineableScatterCommonality > 0
        );


        public static void Build()
        {
            List<ThingCountExposable> oreDictionary = new List<ThingCountExposable>();

            // Get all ThingDefs that have mineable resources
            IEnumerable<ThingDef> ores = DefDatabase<ThingDef>.AllDefs.Where((ThingDef def) => validOre(def));


            // Assign commonality values for ores
            foreach (ThingDef ore in ores)
            {
                oreDictionary.Add(new ThingCountExposable(ore.building.mineableThing, ValueForMineableOre(ore)));
            }

            // Get the rarest ore in the list
            int num = MaxWeight;
            for (int i = 0; i < oreDictionary.Count; i++)
            {
                if (oreDictionary[i].count < num)
                {
                    num = oreDictionary[i].count;
                }
            }

            num += num / 2;

            // Manually add components
            oreDictionary.Add(new ThingCountExposable(ThingDefOf.ComponentIndustrial, num));


            //var results = oreDictionary.GroupBy(x => x.thingDef.label).Select(x => x.First()).ToList();
            var results = oreDictionary.GroupBy(x => x.thingDef).Select(x => x.MaxBy(t => t.thingDef.building?.mineableScatterCommonality)).ToList();

            //foreach (var x in results)
            //{
            //    Log.Message("x: " + x.ToString() + " label: " + x.thingDef.label.ToString());
            //}
            // Assign this dictionary for the mod to use
            QuarrySettings.oreDictionary = results;
        }

        public static int ValueForMineableOre(ThingDef def)
        {
            if (!validOre(def))
            {
                Log.Error($"{Static.Quarry}:: Unable to process def {def.LabelCap} as a mineable resource rock.");
                return 0;
            }
            // Log.Message("def: " + def.ToString() + " label: " + def.label + " deepCommonality: " + def.building.mineableThing.deepCommonality.ToString() + " mineableScatterCommonality: " + def.building.mineableScatterCommonality.ToString() + " BaseMarketValue: " + def.building.mineableThing.BaseMarketValue.ToString());
            float valDeep = Mathf.Clamp(def.building.mineableThing.deepCommonality, 0f, 1.5f);
            float valScatter = def.building.mineableScatterCommonality *
                               commonalityCurve.Evaluate(def.building.mineableScatterCommonality);
            float valMarket = Math.Max(def.building.mineableThing.BaseMarketValue / 5f, 2f);
            var result = (int)((valDeep * valScatter * 55f) / valMarket);
            // Log.Message("def: " + def.ToString() + " valDeep: " + valDeep.ToString() + " valScatter: " + valScatter.ToString() + " valMarket: " + valMarket.ToString() + " result: " + result.ToString());
            return (int)result;
        }


        public static float WeightAsPercentageOf(this List<ThingCountExposable> dictionary, int weight)
        {
            float sum = 0;

            for (int i = 0; i < dictionary.Count; i++)
            {
                sum += dictionary[i].count;
            }

            return (weight / sum) * 100f;
        }


        public static ThingDef TakeOne()
        {
            // Make sure there is a dictionary to work from
            if (QuarrySettings.oreDictionary == null)
            {
                Build();
            }

            // Sorts the weight list
            List<ThingCountExposable> sortedWeights = Sort(QuarrySettings.oreDictionary);

            // Sums all weights
            int sum = 0;
            for (int i = 0; i < QuarrySettings.oreDictionary.Count; i++)
            {
                sum += QuarrySettings.oreDictionary[i].count;
            }

            // Randomizes a number from Zero to Sum
            int roll = rand.Next(0, sum);

            // Finds chosen item based on weight
            ThingDef selected = sortedWeights[sortedWeights.Count - 1].thingDef;
            for (int j = 0; j < sortedWeights.Count; j++)
            {
                if (roll < sortedWeights[j].count)
                {
                    selected = sortedWeights[j].thingDef;
                    break;
                }

                roll -= sortedWeights[j].count;
            }

            // Returns the selected item
            return selected;
        }


        private static List<ThingCountExposable> Sort(List<ThingCountExposable> weights)
        {
            List<ThingCountExposable> list = new List<ThingCountExposable>(weights);

            // Sorts the Weights List for randomization later
            list.Sort(
                delegate (ThingCountExposable firstPair,
                    ThingCountExposable nextPair)
                {
                    return firstPair.count.CompareTo(nextPair.count);
                }
            );

            return list;
        }

    }

}