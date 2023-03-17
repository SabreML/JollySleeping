using Menu;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace JollySleeping
{
	public static class SleepSceneBuilder
	{
		/// <summary>
		/// Removes existing illustrations which are about to be replaced, and calls either <see cref="AddSingleplayerSlugcat(MenuScene, bool)"/><br/>
		/// or <see cref="AddMultiplayerSlugcats(MenuScene, bool)"/> depending on the player count.
		/// </summary>
		public static void MenuSceneHK(On.Menu.MenuScene.orig_ctor orig, MenuScene self, Menu.Menu menu, MenuObject owner, MenuScene.SceneID sceneID)
		{
			orig(self, menu, owner, sceneID);
			if (self.sceneID != MenuScene.SceneID.SleepScreen || self.flatMode)
			{
				return;
			}
			if (JollySleepingMod.PlayerSlugcatTypes == null)
			{
				Debug.Log("(JollySleeping) Player types not cached!");
				return;
			}

			bool spearmaster = (ModManager.MSC && JollySleepingMod.PlayerSlugcatTypes.Contains("spear"));
			bool multiplayer = JollySleepingMod.PlayerSlugcatTypes.Count > 1;

			// Remove the existing foreground grass (1) and slugcat illustration (2).
			string pattern = @"[12]";
			if (spearmaster) // Spearmaster also has custom background grass (3) and their little drawings (d).
			{
				pattern = multiplayer ? @"[12d]" : @"[123d]"; // Don't need to remove the background grass for the multiplayer illustrations.
			}
			self.depthIllustrations.RemoveAll(item => Regex.IsMatch(item.fileName, pattern));
			self.subObjects.RemoveAll(item => Regex.IsMatch((item as MenuDepthIllustration).fileName, pattern));

			MenuDepthIllustration newIllustration;
			if (multiplayer)
			{
				// Use the custom art for different combinations of slugcats.
				newIllustration = AddMultiplayerSlugcats(self, spearmaster);
			}
			else
			{
				// Use the default art for the slugcat they're playing as.
				newIllustration = AddSingleplayerSlugcat(self, spearmaster);
			}


			Debug.Log($"(JollySleeping) Displaying {newIllustration.fileName} at {newIllustration.pos}");
		}

		/// <summary>
		/// Adds Slugcat and grass illustrations back into the scene, selecting the Slugcat illustration variant based on the<br/>
		/// combined contents of <see cref="JollySleepingMod.PlayerSlugcatTypes"/>.
		/// </summary>
		/// <remarks>
		/// The position of the Slugcat illustration will default to <c>Vector2(568f, 33f)</c> unless the variant has <br/>
		/// an override set in <c>positions.txt</c> (<see cref="JollySleepingMod.IllustrationPositions"/>), in which case that will be used instead.
		/// </remarks>
		/// <seealso cref="JollySleepingMod.IllustrationPositions"/>
		/// <param name="spearmaster"><c>bool</c> indicating if the one of the players is using Spearmaster.</param>
		/// <returns>The <see cref="MenuDepthIllustration"/> of the new slugcat image.</returns>
		private static MenuDepthIllustration AddMultiplayerSlugcats(MenuScene self, bool spearmaster)
		{
			string folderName = $"Scenes{Path.DirectorySeparatorChar}Sleep Screen - JollySleeping";
			string fileName = string.Join("-", JollySleepingMod.PlayerSlugcatTypes);

			Vector2 scugPos = new Vector2(568f, 33f);

			// Spearmaster doodles
			if (spearmaster)
			{
				SetupSpearmasterDoodle(self);
				scugPos = new Vector2(671f, 77f); // To make Spearmaster line up with their drawing.
			}

			// Add the slugcats to the scene.
			MenuDepthIllustration slugcatIllustration = new MenuDepthIllustration(self.menu, self, folderName, fileName, scugPos, 1.7f,
				MenuDepthIllustration.MenuShader.Normal);
			self.AddIllustration(slugcatIllustration);

			// If this illustration has a position override set then use that.
			if (JollySleepingMod.IllustrationPositions.ContainsKey(slugcatIllustration.fileName))
			{
				slugcatIllustration.pos = JollySleepingMod.IllustrationPositions[slugcatIllustration.fileName];
			}

			// Add the foreground grass back in too. (Grass has to be added afterwards for layering)
			string grassFolder = $"Scenes{Path.DirectorySeparatorChar}Sleep Screen - {(spearmaster ? "Spear" : "White")}";
			self.AddIllustration(new MenuDepthIllustration(self.menu, self, grassFolder, "Sleep - 1", new Vector2(486f, -54f), 1.2f,
				MenuDepthIllustration.MenuShader.Normal));

			return slugcatIllustration;
		}

		/// <summary>
		/// Adds Slugcat and grass illustrations back into the scene from <c>PlayerSlugcatTypes[0]</c>'s 'Sleep Screen' folder, and calls <see cref="MenuScene.RefreshPositions"/> to <br/>
		/// position everything identically to the sleep scene in singleplayer.
		/// </summary>
		/// <remarks>
		/// If the player is using Spearmaster (<paramref name="spearmaster"/> param), the existing foreground and background grass textures are unloaded from the game's atlas manager <br/>
		/// in order to allow Spearmaster's custom versions to replace them. <br/>
		/// A random drawing is also added via <see cref="SetupSpearmasterDoodle(MenuScene)"/>.
		/// </remarks>
		/// <param name="spearmaster"><c>bool</c> indicating if the player is using Spearmaster.</param>
		/// <returns>The <see cref="MenuDepthIllustration"/> of the new slugcat image.</returns>
		private static MenuDepthIllustration AddSingleplayerSlugcat(MenuScene self, bool spearmaster)
		{
			string playerSlugcat = JollySleepingMod.PlayerSlugcatTypes[0];
			self.sceneFolder = $"Scenes{Path.DirectorySeparatorChar}Sleep Screen - {playerSlugcat}";

			// Spearmaster doodles
			if (spearmaster)
			{
				SetupSpearmasterDoodle(self);
			}

			// Add the slugcat to the scene.
			MenuDepthIllustration slugcatIllustration = new MenuDepthIllustration(self.menu, self, self.sceneFolder, $"Sleep - 2 - {playerSlugcat}", Vector2.zero, 1.7f,
				MenuDepthIllustration.MenuShader.Normal);
			self.AddIllustration(slugcatIllustration);

			// Add the foreground grass back in too. (Grass has to be added afterwards for layering)
			self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Sleep - 1", Vector2.zero, 1.2f,
				MenuDepthIllustration.MenuShader.Normal));

			// Fix any positioning issues.
			self.RefreshPositions();

			return slugcatIllustration;
		}

		/// <summary>
		/// Picks a random drawing (<c>'sleep - d1-11.png'</c>) from  <c>'scenes\sleep screen - spear'</c> and adds it to the scene.
		/// </summary>
		public static void SetupSpearmasterDoodle(MenuScene self) // public for testing
		{
			string folderPath = $"Scenes{Path.DirectorySeparatorChar}Sleep Screen - Spear";

			// Unload and replace the cached 'Sleep - 3' atlas. (Spearmaster's version has an opaque floor to draw on.)
			Futile.atlasManager.UnloadAtlas("Sleep - 3");
			self.AddIllustration(new MenuDepthIllustration(self.menu, self, folderPath, "Sleep - 3", new Vector2(697f, 78f), 2.2f,
				MenuDepthIllustration.MenuShader.Normal));

			// Also unload the cached foreground grass so that spearmaster's version can replace it later.
			Futile.atlasManager.UnloadAtlas("Sleep - 1");

			// Add a random drawing.
			self.AddIllustration(new MenuDepthIllustration(self.menu, self, folderPath, $"Sleep - D{Random.Range(1, 11)}", new Vector2(965f, 60f), 2.2f,
				MenuDepthIllustration.MenuShader.Basic));
		}
	}
}
