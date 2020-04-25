using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
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
			helper.Events.GameLoop.OneSecondUpdateTicked += OneSecondTick;
		}

		private void OneSecondTick(object sender, OneSecondUpdateTickedEventArgs e)
		{
			if (!Context.IsWorldReady)
				return;

			Monitor.Log($"---", LogLevel.Warn);
			Monitor.Log($"Player Status at {Game1.player.getTileX()},{Game1.player.getTileY()}", LogLevel.Warn);
			foreach (Building building in Game1.getFarm().buildings)
			{
				foreach (GameLocation location in building.indoors)
				{
					// Sometimes indoor locations are in the list, but null?
					if (location is null)
					{
						continue;
					}

					foreach (StardewValley.Object tileObject in location.Objects.Values)
					{
						if (tileObject is IndoorPot && ((IndoorPot)tileObject).hoeDirt.Value != null)
						{
							Monitor.Log($"Indoor Pot Status: {tileObject.TileLocation.X},{tileObject.TileLocation.Y} in {location.Name} = {((IndoorPot)tileObject).hoeDirt.Value.state.Value}", LogLevel.Warn);
						}
					}
				}
			}

			Monitor.Log($"---", LogLevel.Warn);
		}

		private void OnDayStarted(object sender, DayStartedEventArgs e)
		{
			if (!Game1.IsMasterGame)
			{
				return;
			}

			// Handle greenhouse rain
			foreach (GameLocation location in Game1.locations)
			{
				if (location.IsGreenhouse)
				{
					rainOnAllTilesIfNeeded(location);
				}
			}

			// Handle rain and sprinklers in indoor buildings
			foreach (Building building in Game1.getFarm().buildings)
			{
				foreach (GameLocation location in building.indoors)
				{
					// Sometimes indoor locations are in the list, but null?
					if (location is null)
					{
						continue;
					}

					Monitor.Log($"Watering indoors in {location.NameOrUniqueName} (greenhouse? {location.IsGreenhouse}, outdoors? {location.IsOutdoors})", LogLevel.Trace);

					// If everything should be rained on just do that, no need to do the sprinklers individually
					if (rainOnAllTilesIfNeeded(location))
					{
						Monitor.Log($"Rained in {location.NameOrUniqueName}, continuing", LogLevel.Trace);
						continue;
					}

					// Otherwise re-do the sprinkler logic in here (from Object.cs:1017)
					if (Config.sprinkleInside)
					{
						Monitor.Log($"Triggering sprinklers in {location.NameOrUniqueName}", LogLevel.Trace);

						foreach (StardewValley.Object obj in location.Objects.Values)
						{
							switch (obj.ParentSheetIndex)
							{
								case Sprinkler.Basic:
									foreach (Vector2 adjacentTileLocation in Utility.getAdjacentTileLocations(obj.TileLocation))
									{
										waterTile(location, adjacentTileLocation);
									}
									break;

								case Sprinkler.Quality:
									foreach (Vector2 surroundingTileLocation in Utility.getSurroundingTileLocationsArray(obj.TileLocation))
									{
										waterTile(location, surroundingTileLocation);
									}
									break;

								case Sprinkler.Iridium:
									for (int index1 = (int)obj.tileLocation.X - 2; (double)index1 <= (double)obj.tileLocation.X + 2.0; ++index1)
									{
										for (int index2 = (int)obj.tileLocation.Y - 2; (double)index2 <= (double)obj.tileLocation.Y + 2.0; ++index2)
										{
											waterTile(location, new Vector2((float)index1, (float)index2));
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

		private bool rainOnAllTilesIfNeeded(GameLocation location)
		{
			if (Config.rainInside && Game1.isRaining)
			{
				// Water all the terrain tiles
				foreach (KeyValuePair<Vector2, TerrainFeature> terrainPair in location.terrainFeatures.Pairs)
				{
					waterTile(location, terrainPair.Key);
				}

				// And also all the objects, just in case they're pots
				foreach (StardewValley.Object obj in location.Objects.Values)
				{
					waterTile(location, obj.TileLocation);
				}

				return true;
			}

			return false;
		}

		private void waterTile(GameLocation location, Vector2 tileCoordinates)
		{
			// Water indoor pots
			StardewValley.Object tileObject = location.getObjectAtTile((int)tileCoordinates.X, (int)tileCoordinates.Y);

			if (tileObject is IndoorPot && ((IndoorPot)tileObject).hoeDirt.Value != null)
			{
				Monitor.Log($"Watering indoor pot {tileObject.TileLocation.X},{tileObject.TileLocation.Y} in {location.Name}", LogLevel.Trace);
				((IndoorPot)tileObject).hoeDirt.Value.state.Value = HoeDirt.watered;
				
				// Dirt underneath pots stays dry ;)
				return;
			}

			// Water dirt tiles
			if (location.terrainFeatures.ContainsKey(tileCoordinates))
			{
				TerrainFeature terrainFeature = location.terrainFeatures[tileCoordinates];

				if (terrainFeature is HoeDirt)
				{
					Monitor.Log($"Watering terrain {terrainFeature.currentTileLocation.X},{terrainFeature.currentTileLocation.Y} in {location.Name}", LogLevel.Trace);
					((HoeDirt)terrainFeature).state.Value = HoeDirt.watered;
				}
			}
		}
	}
}