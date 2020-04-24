using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;

namespace IndoorWatering
{
	public partial class ModEntry : Mod
	{
		public static class Sprinkler
		{
			public const int Basic = 599;
			public const int Quality = 621;
			public const int Iridium = 645;
		}

		public class SavedConfig
		{
			public bool sprinkleInside { get; set; } = true;

			public bool rainInside { get; set; } = false;
		}

		private SavedConfig Config;


		public override void Entry(IModHelper helper)
		{
			this.Config = Helper.ReadConfig<SavedConfig>();			

			helper.Events.GameLoop.DayStarted += OnDayStarted;
		}

		private void OnDayStarted(object sender, DayStartedEventArgs e)
		{
			if (!Game1.IsMasterGame)
			{
				return;
			}

			// For each location
			foreach (Building building in Game1.getFarm().buildings)
			{
				foreach (GameLocation location in building.indoors)
				{
					if (location is null)
					{
						continue;
					}

					Monitor.Log($"Triggering sprinklers in {location.NameOrUniqueName} (greenhouse? {location.IsGreenhouse}, outdoors? {location.IsOutdoors})", LogLevel.Trace);

					// If everything should be rained on just do that
					if (Config.rainInside && Game1.isRaining)
					{
						foreach (KeyValuePair<Vector2, TerrainFeature> terrainPair in location.terrainFeatures.Pairs)
						{
							water(terrainPair.Value);
						}

						continue;
					}

					// Otherwise re-do the sprinkler logic in here (from Object.cs:1017)
					if (Config.sprinkleInside)
					{
						foreach (StardewValley.Object obj in location.Objects.Values)
						{
							switch (obj.ParentSheetIndex)
							{
								case Sprinkler.Basic:
									foreach (Vector2 adjacentTileLocation in Utility.getAdjacentTileLocations(obj.TileLocation))
									{
										if (location.terrainFeatures.ContainsKey(adjacentTileLocation))
										{
											water(location.terrainFeatures[adjacentTileLocation]);
										}
									}
									break;

								case Sprinkler.Quality:
									foreach (Vector2 surroundingTileLocation in Utility.getSurroundingTileLocationsArray(obj.TileLocation))
									{
										if (location.terrainFeatures.ContainsKey(surroundingTileLocation))
										{
											water(location.terrainFeatures[surroundingTileLocation]);
										}
									}
									break;

								case Sprinkler.Iridium:
									for (int index1 = (int)obj.tileLocation.X - 2; (double)index1 <= (double)obj.tileLocation.X + 2.0; ++index1)
									{
										for (int index2 = (int)obj.tileLocation.Y - 2; (double)index2 <= (double)obj.tileLocation.Y + 2.0; ++index2)
										{
											Vector2 key = new Vector2((float)index1, (float)index2);

											if (location.terrainFeatures.ContainsKey(key))
											{
												water(location.terrainFeatures[key]);
											}
										}
									}
									break;

								default:
									break;
							}
						}
					}
				}
			}
		}

		private void water(TerrainFeature terrainFeature)
		{
			if (terrainFeature is HoeDirt)
			{
				Monitor.Log($"Watering tile {terrainFeature.currentTileLocation.X},{terrainFeature.currentTileLocation.Y} in {terrainFeature.currentLocation.Name}", LogLevel.Trace);
				((HoeDirt)terrainFeature).state.Value = 1;
			}
		}
	}
}