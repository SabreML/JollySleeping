using Menu;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace JollySleeping
{
	public class SleepSceneBuilder
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
			if (JollySleepingMod.playerSlugcatTypes == null)
			{
				Debug.Log("(JollySleeping) Player types not cached!");
				return;
			}

			bool spearmaster = (ModManager.MSC && JollySleepingMod.playerSlugcatTypes.Contains("spear"));
			bool multiplayer = JollySleepingMod.playerSlugcatTypes.Count > 1;

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
		/// combined contents of <see cref="JollySleepingMod.playerSlugcatTypes"/>.
		/// </summary>
		/// <remarks>
		/// The position of the Slugcat illustration will default to <c>Vector2(568f, 33f)</c> unless the variant has <br/>
		/// an override set in <c>positions.txt</c> (<see cref="JollySleepingMod.illustrationPositions"/>), in which case that will be used instead.
		/// </remarks>
		/// <seealso cref="JollySleepingMod.illustrationPositions"/>
		/// <param name="spearmaster"><c>bool</c> indicating if the one of the players is using Spearmaster.</param>
		/// <returns>The <see cref="MenuDepthIllustration"/> of the new slugcat image.</returns>
		private static MenuDepthIllustration AddMultiplayerSlugcats(MenuScene self, bool spearmaster)
		{
			string folderName = $"Scenes{Path.DirectorySeparatorChar}Sleep Screen - JollySleeping";
			string fileName = string.Join("-", JollySleepingMod.playerSlugcatTypes);

			// Spearmaster doodles
			//if (spearmaster)
			//{
			//	AddSpearmasterDoodle(self);
			//}

			// Add the slugcats to the scene.
			MenuDepthIllustration slugcatIllustration = new MenuDepthIllustration(self.menu, self, folderName, fileName, new Vector2(568f, 33f), 1.7f,
				MenuDepthIllustration.MenuShader.Normal);
			self.AddIllustration(slugcatIllustration);

			// Check if this illustration has a position override set.
			if (JollySleepingMod.illustrationPositions.ContainsKey(slugcatIllustration.fileName))
			{
				slugcatIllustration.pos = JollySleepingMod.illustrationPositions[slugcatIllustration.fileName];
			}

			// Add the foreground grass back in too. (Grass has to be added afterwards for layering)
			string grassFolder = $"Scenes{Path.DirectorySeparatorChar}Sleep Screen - White";
			self.AddIllustration(new MenuDepthIllustration(self.menu, self, grassFolder, "Sleep - 1", new Vector2(486f, -54f), 1.2f,
				MenuDepthIllustration.MenuShader.Normal));

			return slugcatIllustration;
		}

		/// <summary>
		/// Adds Slugcat and grass illustrations back into the scene from <c>playerSlugcatTypes[0]</c>'s 'Sleep Screen' folder, and calls <see cref="MenuScene.RefreshPositions"/> to <br/>
		/// position everything identically to the sleep scene in singleplayer.
		/// </summary>
		/// <remarks>
		/// If the player is using Spearmaster (<paramref name="spearmaster"/> param), the existing foreground and background grass textures are unloaded from the game's atlas manager <br/>
		/// in order to allow Spearmaster's custom versions to replace them. <br/>
		/// A random drawing is also added via <see cref="AddSpearmasterDoodle(MenuScene)"/>.
		/// </remarks>
		/// <param name="spearmaster"><c>bool</c> indicating if the player is using Spearmaster.</param>
		/// <returns>The <see cref="MenuDepthIllustration"/> of the new slugcat image.</returns>
		private static MenuDepthIllustration AddSingleplayerSlugcat(MenuScene self, bool spearmaster)
		{
			string playerSlugcat = JollySleepingMod.playerSlugcatTypes[0];
			self.sceneFolder = $"Scenes{Path.DirectorySeparatorChar}Sleep Screen - {playerSlugcat}";

			// Spearmaster doodles
			if (spearmaster)
			{
				// Unload and replace the cached 'Sleep - 3' atlas.
				Futile.atlasManager.UnloadAtlas("Sleep - 3");
				self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Sleep - 3", new Vector2(697f, 78f), 2.2f,
					MenuDepthIllustration.MenuShader.Normal));

				AddSpearmasterDoodle(self);

				// Also unload the foreground grass atlas so that spearmaster's version can replace it below.
				Futile.atlasManager.UnloadAtlas("Sleep - 1");
			}

			// Add the slugcat to the scene.
			MenuDepthIllustration slugcatIllustration = new MenuDepthIllustration(self.menu, self, self.sceneFolder, $"Sleep - 2 - {playerSlugcat}", new Vector2(), 1.7f,
				MenuDepthIllustration.MenuShader.Normal);
			self.AddIllustration(slugcatIllustration);

			// Add the foreground grass back in too. (Grass has to be added afterwards for layering)
			self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Sleep - 1", new Vector2(), 1.2f,
				MenuDepthIllustration.MenuShader.Normal));

			// Fix any positioning issues.
			self.RefreshPositions();

			return slugcatIllustration;
		}

		/// <summary>
		/// Picks a random drawing (<c>'sleep - d1-11.png'</c>) from  <c>'scenes\sleep screen - spear'</c> and adds it to the scene.
		/// </summary>
		private static void AddSpearmasterDoodle(MenuScene self)
		{
			string folderPath = $"Scenes{Path.DirectorySeparatorChar}Sleep Screen - Spear";
			string filePath = $"Sleep - D{Random.Range(1, 11)}";
			self.AddIllustration(new MenuDepthIllustration(self.menu, self, folderPath, filePath, new Vector2(), 2.2f,
				MenuDepthIllustration.MenuShader.Basic)); // Todo: Multiplayer positioning
		}
	}
}
