using System.Collections.Generic;
using Verse;

namespace Quarry
{
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
}
