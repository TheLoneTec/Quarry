using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Verse;

namespace Quarry
{

    public sealed class QuarryMod : Mod 
	{
		
		private Vector2 scrollPosition = Vector2.zero;
		private float scrollViewHeight = 0f;
		private string baseTimeBuffer;
		private string varTimeBuffer;
		private string reclaimedSoilFertilityBuffer;

		public QuarryMod(ModContentPack mcp) : base(mcp) 
		{
			LongEventHandler.ExecuteWhenFinished(GetSettings);
			LongEventHandler.ExecuteWhenFinished(PushDatabase);
			LongEventHandler.ExecuteWhenFinished(BuildDictionary);
			LongEventHandler.ExecuteWhenFinished(SetFertility);
		}


		public void GetSettings() 
		{
			GetSettings<QuarrySettings>();
		}


		public override void WriteSettings() {
			base.WriteSettings();
			QuarryDefOf.QRY_ReclaimedSoil.fertility = QuarrySettings.reclaimedSoilFertility;
		}


		private void PushDatabase() 
		{
			QuarrySettings.database = DefDatabase<ThingDef>.AllDefsListForReading;

		}

		private void SetFertility()
        {
			QuarryDefOf.QRY_ReclaimedSoil.fertility = QuarrySettings.reclaimedSoilFertility;
		}
		private void BuildDictionary()
		{
			if (QuarrySettings.oreDictionary == null)
			{
				OreDictionary.Build();
			}
			if (QuarrySettings.quarryableStone == null)
			{
				BuildStoneDict();
			}
			QuarryDefOf.QRY_ReclaimedSoil.fertility = QuarrySettings.reclaimedSoilFertility;
		}

		private void BuildStoneDict()
        {
			QuarrySettings.quarryableStone = new Dictionary<string, QuarryRockType>();
			List<string> processed = new List<string>();
			List<TerrainDef> terrainDefs = DefDatabase<TerrainDef>.AllDefsListForReading;
			List<ThingDef> rockDefs = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(x => x.building?.mineableThing != null /*&& x.building.mineableThing.defName.Contains("Chunk")*/);
			List<ThingDef> chunkDefs = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(x => x.defName.Contains("Chunk") && rockDefs.Any(y=> y.building.mineableThing == x));
			List<ThingDef> blockDefs = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(x => x.defName.Contains("Blocks"));
			/*
			Log.Message($"Quarry: Checking\n" +
				$"{blockDefs.Count} Blocks {((List<string>)blockDefs.Select(r => r.LabelCap)).ToCommaList()}" +
				$"\n\n{chunkDefs.Count} Chunks {((List<string>)chunkDefs.Select(r => r.LabelCap)).ToCommaList()}" +
				$"\n\n{rockDefs.Count} Rocks {((List<string>)rockDefs.Select(r => r.LabelCap)).ToCommaList()}" +
				$"\n\n{terrainDefs.Count} TerrainDefs {((List<string>)terrainDefs.Select(r => r.LabelCap)).ToCommaList()}");
			*/
			List<QuarryableStoneDef> modDefs = DefDatabase<QuarryableStoneDef>.AllDefsListForReading;
			Log.Message($"Quarry: Checking {rockDefs.Count} rockDefs");
			foreach (var item in rockDefs)
			{
				if (item.building.naturalTerrain == null || QuarrySettings.quarryableStone.Values.Any(x => x.rockDef == item)) continue;
				Log.Message($"Quarry: Checking rockDef: {item} ");
				ThingDef chunkDef = item.building.mineableThing;
				TerrainDef terrainNatural = item.building.naturalTerrain;
				TerrainDef terrainLeave = item.building.leaveTerrain;
				TerrainDef terrainSmoothed = terrainNatural?.smoothedTerrain ?? null;
				ThingDef blockDef = blockDefs.Find(x => x.label.Contains(item.label)) ?? null;
				QuarrySettings.quarryableStone.Add(item.label, new QuarryRockType(item, chunkDef, blockDef));
				Log.Message($"Quarry:: RockDef New: {item.label}:: rockDef: {item}, chunkDef: {chunkDef}, blockDef: {blockDef}");
				processed.Add(item.label);
			}
			Log.Message($"Quarry: Checking {modDefs.Count} QuarryableStoneDefs");
			foreach (var item in modDefs)
			{
				string rockType = QuarryUtility.RockType(item.terrainTag);
				ThingDef rockDef = rockDefs.Find(x => x.defName.Contains(rockType));
				if (rockDef == null || processed.Contains(rockDef.label) || rockType == "Sand")
				{
					continue;
				}
				Log.Message($"Quarry: Checking modDef: {item} ");
				ThingDef chunkDef = chunkDefs.Find(x => x.defName.Contains(rockType));
				ThingDef blockDef = blockDefs.Find(x => x.defName.Contains(rockType));
				if (chunkDef == null && rockDef != null)
				{
					chunkDef = rockDef.building.mineableThing;

				}
				if (chunkDef != null)
				{
					if (!item.terrainDefs.NullOrEmpty() && QuarrySettings.quarryableStone.First(x => x.Value.chunkDef == chunkDef).Value is QuarryRockType quarryRock)
                    {
						quarryRock.terrainDefs.AddRange(item.terrainDefs);
						//	Log.Message($"Quarry:: QuarryableStoneDef Updated: {rockType}:: rockDef: {rockDef}, chunkDef: {chunkDef}, blockDef: {blockDef}");
					}
					else
                    {
						QuarrySettings.quarryableStone.Add(rockDef.label, new QuarryRockType(rockDef, chunkDef, blockDef));
						//	Log.Message($"Quarry:: QuarryableStoneDef New: {rockType}:: rockDef: {rockDef}, chunkDef: {chunkDef}, blockDef: {blockDef}");
					}
					processed.Add(rockDef.label);
				}
			}
			Log.Message($"Quarry: Checking {terrainDefs.Count} terrainDefs");
			foreach (var item in terrainDefs)
			{
				if (QuarrySettings.quarryableStone.Values.Any(x => x.terrainDefs.Contains(item))) continue;

				string rockType = QuarryUtility.RockType(item.defName);

				ThingDef rockDef = rockDefs.Find(x => x.defName.Contains(rockType));
				if (rockDef == null || processed.Contains(rockDef.label) || rockType == "Sand")
				{
					continue;
				}
			//	Log.Message($"Quarry: Checking terrainDef: {item} ");
				ThingDef chunkDef = chunkDefs.Find(x => x.defName.Contains(rockType));
				ThingDef blockDef = blockDefs.Find(x => x.defName.Contains(rockType));
                if (chunkDef == null && rockDef != null && ((rockDef.building.naturalTerrain != null && rockDef.building.naturalTerrain == item) || (rockDef.building.leaveTerrain != null && rockDef.building.leaveTerrain == item)))
                {
					chunkDef = rockDef.building.mineableThing;

				}
				if (chunkDef != null)
                {
                    if (QuarrySettings.quarryableStone.First(x=> x.Value.chunkDef == chunkDef).Value is QuarryRockType quarryRock)
                    {
						quarryRock.terrainDefs.Add(item);
						Log.Message($"Quarry:: TerrainDef Updated: {rockType}:: rockDef: {rockDef}, chunkDef: {chunkDef}, blockDef: {blockDef}");
					}
                    else
                    {
						QuarrySettings.quarryableStone.Add(rockDef.label, new QuarryRockType(rockDef, chunkDef, blockDef));
						Log.Message($"Quarry:: TerrainDef New: {rockType}:: rockDef: {rockDef}, chunkDef: {chunkDef}, blockDef: {blockDef}");
					}
					processed.Add(rockDef.label);
				}

			}
			Log.Message($"Quarry:: Total {processed.Count} quarryable terrain found, processed: {processed.ToCommaList()}" + (modDefs.Count > 0? $"\n{modDefs.Count} QuarryableStoneDef's found" :""));
		}

		public void processRock()
        {

        }

		public override string SettingsCategory() 
		{
			return Static.Quarry;
		}


		public override void DoSettingsWindowContents(Rect rect) 
		{
			Listing_Standard list = new Listing_Standard() 
			{
				ColumnWidth = rect.width
			};

			Rect topRect = rect.TopPart(0.25f);
			Rect bottomRect = rect.BottomPart(0.75f);
			Rect fullRect = list.GetRect(Text.LineHeight).ContractedBy(4);
			Rect leftRect = fullRect.LeftHalf().Rounded();
			Rect rightRect = fullRect.RightHalf().Rounded();

			list.Begin(rect);

			list.QuarryHealthSetting();
			Listing_Standard listtop = list.BeginSection(topRect.height, true);
			{
				listtop.ColumnWidth = fullRect.width * 0.488f;
				listtop.CheckboxLabeled(Static.LetterSent, ref QuarrySettings.letterSent, Static.ToolTipLetter);
				listtop.CheckboxLabeled(Static.AllowRottable, ref QuarrySettings.allowRottable, Static.ToolTipAllowRottable);
				listtop.CheckboxLabeled(Static.PlaceAnywhere, ref QuarrySettings.placeAnywhere, Static.ToolTipPlaceAnywhere);
				listtop.TextFieldNumericLabeled("QRY_SettingsReclaimedSoilFertility".Translate(QuarrySettings.reclaimedSoilFertility), ref QuarrySettings.reclaimedSoilFertility, ref reclaimedSoilFertilityBuffer, 0f, tooltip: Static.ToolTipTicksReclaimedSoilFertility);
				listtop.NewColumn();
				listtop.LabeledScrollbarSetting("QRY_SettingsJunkChance".Translate(QuarrySettings.junkChance), ref QuarrySettings.junkChance, Static.ToolTipJunkChance);
				listtop.LabeledScrollbarSetting("QRY_SettingsChunkChance".Translate(QuarrySettings.chunkChance), ref QuarrySettings.chunkChance, Static.ToolTipChunkChance);
				listtop.TextFieldNumericLabeled("QRY_SettingsMineTimeBase".Translate(QuarrySettings.mineTicksAverage), ref QuarrySettings.mineTicksAverage, ref baseTimeBuffer, 30, tooltip: Static.ToolTipTicksAverage);
				listtop.TextFieldNumericLabeled("QRY_SettingsMineTimeVar".Translate(QuarrySettings.mineTicksVariance), ref QuarrySettings.mineTicksVariance, ref varTimeBuffer, 0, QuarrySettings.mineTicksAverage * 0.9f, tooltip: Static.ToolTipTicksVariance);
			}
			list.EndSection(listtop);
			/*
			Listing_Standard listbottom = list.BeginSection(bottomRect.height, true);
			{

			}
			list.EndSection(listbottom);
			*/
			list.End();
			{

				Rect DictInterfaceRect = bottomRect.TopPart(0.25f).Rounded();
				{
					Rect labelRect = DictInterfaceRect.TopPart(0.75f).Rounded();
					Text.Font = GameFont.Medium;
					Text.Anchor = TextAnchor.MiddleCenter;
					Widgets.Label(labelRect, Static.LabelDictionary);
					Text.Font = GameFont.Small;
					Text.Anchor = TextAnchor.UpperLeft;
				}
				{
					Rect buttonsRect = DictInterfaceRect.BottomPart(0.25f).Rounded();
					Rect addRect = buttonsRect.LeftThird();
					Rect rmvRect = buttonsRect.MiddleThird();
					Rect resRect = buttonsRect.RightThird();

					// Add an entry to the dictionary
					if (Widgets.ButtonText(addRect, Static.LabelAddThing)) {
						List<FloatMenuOption> thingList = new List<FloatMenuOption>();
						foreach (ThingDef current in from t in QuarryUtility.PossibleThingDefs()
																					orderby t.label
																					select t) {

							bool skip = false;
							for (int i = 0; i < QuarrySettings.oreDictionary.Count; i++) {
								if (QuarrySettings.oreDictionary[i].thingDef == current) {
									skip = true;
									break;
								}
							};
							if (skip) continue;

							thingList.Add(new FloatMenuOption(current.LabelCap, delegate {
								QuarrySettings.oreDictionary.Add(new ThingCountExposable(current, 1));
							}));
						}
						FloatMenu menu = new FloatMenu(thingList);
						Find.WindowStack.Add(menu);
					}

					// Remove an entry from the dictionary
					if (Widgets.ButtonText(rmvRect, Static.LabelRemoveThing) && QuarrySettings.oreDictionary.Count >= 2) {
						List<FloatMenuOption> thingList = new List<FloatMenuOption>();
						foreach (ThingCountExposable current in from t in QuarrySettings.oreDictionary
																										orderby t.thingDef.label
																										select t) {
							ThingDef localTd = current.thingDef;
							thingList.Add(new FloatMenuOption(localTd.LabelCap, delegate {
								for (int i = 0; i < QuarrySettings.oreDictionary.Count; i++) {
									if (QuarrySettings.oreDictionary[i].thingDef == localTd) {
										QuarrySettings.oreDictionary.Remove(QuarrySettings.oreDictionary[i]);
										break;
									}
								};
							}));
						}
						FloatMenu menu = new FloatMenu(thingList);
						Find.WindowStack.Add(menu);
					}

					// Reset the dictionary
					if (Widgets.ButtonText(resRect, Static.LabelResetList)) {
						OreDictionary.Build();
					}
				}

				{
					Rect listRect = bottomRect.BottomPart(0.75f).Rounded();
					Rect cRect = listRect.ContractedBy(10f);
					Rect position = listRect;
					Rect outRect = new Rect(0f, 0f, position.width, position.height);
					Rect viewRect = new Rect(0f, 0f, position.width - 16f, scrollViewHeight);

					float num = 0f;
					List<ThingCountExposable> dict = new List<ThingCountExposable>(QuarrySettings.oreDictionary);

					GUI.BeginGroup(position);
					Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, true);

					foreach (var tc in dict.Select((value, index) => new { index, value })) {
						Rect entryRect = new Rect(0f, num, viewRect.width, 32);
						Rect iconRect = entryRect.LeftPartPixels(32);
						Rect labelRect = new Rect(entryRect.LeftThird().x + 33f, entryRect.y, entryRect.LeftThird().width - 33f, entryRect.height);
						Rect texEntryRect = new Rect(entryRect.LeftHalf().RightPartPixels(103).x, entryRect.y, 60f, entryRect.height);
						Rect pctRect = new Rect(entryRect.LeftHalf().RightPartPixels(41).x, entryRect.y, 40f, entryRect.height);
						Rect sliderRect = new Rect(entryRect.RightHalf().x, entryRect.y, entryRect.RightHalf().width, entryRect.height);

						Widgets.ThingIcon(iconRect, tc.value.thingDef);
						Widgets.Label(labelRect, tc.value.thingDef.LabelCap);
						Widgets.Label(pctRect, $"{QuarrySettings.oreDictionary.WeightAsPercentageOf(tc.value.count).ToStringDecimal()}%");
						int val = tc.value.count;
						string nullString = null;
						Widgets.TextFieldNumeric(
							texEntryRect,
							ref QuarrySettings.oreDictionary[tc.index].count,
							ref nullString,
							0, OreDictionary.MaxWeight);
						val = Widgets.HorizontalSlider(
							sliderRect,
							QuarrySettings.oreDictionary[tc.index].count, 0f, OreDictionary.MaxWeight, true
						).RoundToAsInt(1);
						if (val != QuarrySettings.oreDictionary[tc.index].count) {
							QuarrySettings.oreDictionary[tc.index].count = val;
						}

						if (Mouse.IsOver(entryRect)) {
							Widgets.DrawHighlight(entryRect);
						}
						TooltipHandler.TipRegion(entryRect.LeftThird(), tc.value.thingDef.description);

						num += 32f;
						scrollViewHeight = num;
					}

					Widgets.EndScrollView();
					GUI.EndGroup();
				}

				}
		}
	}

	public class QuarryRockType
	{
		public QuarryRockType(ThingDef rockDef, ThingDef chunkDef = null, ThingDef blockDef = null)
		{
			this.rockDef = rockDef;
			this.chunkDef = chunkDef;
			this.blockDef = blockDef;
            if (rockDef != null)
			{
				if (chunkDef == null) this.chunkDef = rockDef.building.mineableThing;
                if (rockDef.building?.naturalTerrain != null)
				{
					terrainDefs.Add(rockDef.building.naturalTerrain);
				}
                if (rockDef.building?.leaveTerrain != null)
				{
					terrainDefs.Add(rockDef.building.leaveTerrain);
				}
                if (rockDef.building?.naturalTerrain?.smoothedTerrain != null)
				{
					terrainDefs.Add(rockDef.building.naturalTerrain.smoothedTerrain);
				}
			}
		}
		public ThingDef rockDef;
		public ThingDef chunkDef;
		public ThingDef blockDef;
		public List<TerrainDef> terrainDefs = new List<TerrainDef>();

	}
	public static class QuarryWidgets
    {
		public static void TextFieldNumericLabeled<T>(this Listing_Standard listing, string label, ref T val, ref string buffer, float min = 0f, float max = 1E+09f, string tooltip = null, float textpart = 0.75f, float boxpart = 0.25f) where T : struct
		{
			TextFieldNumericLabeled<T>(listing.GetRect(Text.LineHeight), label, ref val, ref buffer, min, max, tooltip, textpart, boxpart);
			listing.Gap(listing.verticalSpacing);
		}
		public static void TextFieldNumericLabeled<T>(Rect rect, string label, ref T val, ref string buffer, float min = 0f, float max = 1E+09f, string tooltip = null, float textpart = 0.75f, float boxpart = 0.25f) where T : struct
		{
			Rect rect2 = rect.LeftPart(textpart).Rounded();
			Rect rect3 = rect.RightPart(boxpart).Rounded();
			TextAnchor anchor = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(rect2, label);
			if (tooltip != null)
			{
				TooltipHandler.TipRegion(rect2, tooltip);
			}
			Text.Anchor = anchor;
			Widgets.TextFieldNumeric(rect3, ref val, ref buffer, min, max);
		}
	}
}
