using BepInEx;
using Menu;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using UnityEngine;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace JollySleeping // Todo: Actual descriptive comments
{
	[BepInPlugin("sabreml.jollysleeping", "JollySleeping", "0.1.0")]
	public class JollySleepingMod : BaseUnityPlugin
	{
		// List of the slugcat types of each player.
		private List<string> playerSlugcatTypes;

		// Dictionary of slugcat illustration positions
		private Dictionary<string, Vector2> illustrationPositions;


		/* ########## -SETUP- ########## */

		public void OnEnable()
		{
			On.RainWorld.OnModsInit += OnInit;
			On.StoryGameSession.CreateJollySlugStats += CreateJollySlugStatsHK;
			On.Menu.MenuScene.ctor += MenuSceneHK;

			// Temporary for testing/debugging.
			TestingTools.InitHooks();
		}

		private void OnInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
		{
			orig(self);
			illustrationPositions = ReadPositionsFile();
			VerifyFileNames();
		}

		// positions file but reversed (better comment later)
		private Dictionary<string, Vector2> ReadPositionsFile()
		{
			Dictionary<string, Vector2> output = new Dictionary<string, Vector2>();
			string[] fileLines = File.ReadAllLines(AssetManager.ResolveFilePath($"Scenes{Path.DirectorySeparatorChar}Sleep Screen - JollySleeping{Path.DirectorySeparatorChar}positions.txt"));

			// Parse the data
			foreach (string line in fileLines)
			{
				GroupCollection lineData = Regex.Match(line, @"(-*\d+), (-*\d+): (.*)").Groups;

				Vector2 coordinates = new Vector2(float.Parse(lineData[1].Value), float.Parse(lineData[2].Value));
				List<string> illustrations = Regex.Split(lineData[3].Value, ", ").ToList();
				foreach (string illustration in illustrations)
				{
					output.Add(illustration, coordinates);
				}
			}
			return output;
		}

		private void VerifyFileNames()
		{
			// Verify that all of the new PNG files are properly formatted. (Alphabetical name order with dashes)
			string[] fileList = AssetManager.ListDirectory($"Scenes{Path.DirectorySeparatorChar}Sleep Screen - JollySleeping");
			foreach (string filePath in fileList)
			{
				// Get just the file name without any slashes or the file extension.
				string fileName = Regex.Match(filePath, string.Format(@"\{0}(?!.*\{0})(.*-.*).png", Path.DirectorySeparatorChar)).Groups[1].Value;
				if (fileName == "")
				{
					continue; // Either not a PNG, or not properly formatted.
				}
				string[] fileNameParts = fileName.Split('-');

				// Check for alphabetical-ness.
				bool alphabetical = true;
				for (int i = 0; i < fileNameParts.Count() - 1; i++)
				{
					if (System.StringComparer.Ordinal.Compare(fileNameParts[i], fileNameParts[i + 1]) > 0)
					{ // If the first string comes after the second alphabetically.
						alphabetical = false;
						break;
					}
				}
				if (!alphabetical)
				{
					Debug.Log($"(JollySleeping) File {fileName}.png is not named alphabetically! This may cause issues later.");
					break;
				}
			}
		}


		/* ########## -HOOKS- ########## */

		private void CreateJollySlugStatsHK(On.StoryGameSession.orig_CreateJollySlugStats orig, StoryGameSession self, bool m)
		{
			orig(self, m);

			// Make a copy of the slugcats being controlled by players.
			playerSlugcatTypes = self.characterStatsJollyplayer
				.Where(entry => entry != null) // Filter out null entries.
				.Select(playerStats => playerStats.name.value.ToLower()) // Get the name of the slugcat they're playing as.
				.Distinct() // Duplicate entires get merged into one for simplicity. (["gourmand", "gourmand", "gourmand", "rivulet"] -> ["gourmand", "rivulet"])
				.ToList();
			playerSlugcatTypes.Sort(); // Sort the list alphabetically.
			Debug.Log("(JollySleeping) Players cached.");
		}

		private void MenuSceneHK(On.Menu.MenuScene.orig_ctor orig, MenuScene self, Menu.Menu menu, MenuObject owner, MenuScene.SceneID sceneID)
		{
			orig(self, menu, owner, sceneID);
			if (self.sceneID != MenuScene.SceneID.SleepScreen || self.flatMode)
			{
				return;
			}
			if (playerSlugcatTypes == null)
			{
				Debug.Log("(JollySleeping) Players not cached!");
				return;
			}

			bool spearmaster = (ModManager.MSC && playerSlugcatTypes.Contains("spear"));
			bool multiplayer = playerSlugcatTypes.Count > 1;

			// Remove the existing foreground grass (1) and slugcat illustration (2).
			string pattern = @"[12]";
			if (spearmaster) // Spearmaster also has custom background grass (3) and their little drawings (d).
			{
				pattern = multiplayer ? @"[12d]" : @"[123d]"; // Don't need to remove the background grass for the multiplayer illustrations.
			}
			self.depthIllustrations.RemoveAll(item => Regex.IsMatch(item.fileName, pattern));
			self.subObjects.RemoveAll(item => Regex.IsMatch((item as MenuDepthIllustration).fileName, pattern));

			if (multiplayer)
			{
				// Use the custom art for different combinations of slugcats.
				AddMultiplayerSlugcats(self, spearmaster);
			}
			else
			{
				// Use the default art for the slugcat they're playing as.
				AddSingleplayerSlugcat(self, spearmaster);
			}
		}

		private void AddMultiplayerSlugcats(MenuScene self, bool spearmaster)
		{
			string folderName = $"Scenes{Path.DirectorySeparatorChar}Sleep Screen - JollySleeping";
			string fileName = string.Join("-", playerSlugcatTypes);
			Debug.Log($"(JollySleeping) Displaying {fileName}");

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
			if (illustrationPositions.ContainsKey(slugcatIllustration.fileName))
			{
				Debug.Log("Old: " + slugcatIllustration.pos);
				slugcatIllustration.pos = illustrationPositions[slugcatIllustration.fileName];
				Debug.Log("New: " + slugcatIllustration.pos);
			}

			// Add the foreground grass back in too. (Grass has to be added afterwards for layering)
			string grassFolder = $"Scenes{Path.DirectorySeparatorChar}Sleep Screen - White";
			self.AddIllustration(new MenuDepthIllustration(self.menu, self, grassFolder, "Sleep - 1", new Vector2(486f, -54f), 1.2f,
				MenuDepthIllustration.MenuShader.Normal));
		}

		private void AddSingleplayerSlugcat(MenuScene self, bool spearmaster)
		{
			string playerSlugcat = playerSlugcatTypes[0];
			self.sceneFolder = $"Scenes{Path.DirectorySeparatorChar}Sleep Screen - {playerSlugcat}";
			Debug.Log($"(JollySleeping) Displaying Sleep - 2 - {playerSlugcat}");

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
			self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, $"Sleep - 2 - {playerSlugcat}", new Vector2(), 1.7f,
				MenuDepthIllustration.MenuShader.Normal));
			// Add the foreground grass back in too. (Grass has to be added afterwards for layering)
			self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Sleep - 1", new Vector2(), 1.2f,
				MenuDepthIllustration.MenuShader.Normal));

			// Fix any positioning issues.
			self.RefreshPositions();
		}

		// Picks and adds a random spearmaster doodle.
		private void AddSpearmasterDoodle(MenuScene self)
		{
			string folderPath = $"Scenes{Path.DirectorySeparatorChar}Sleep Screen - Spear";
			string filePath = $"Sleep - D{Random.Range(1, 11)}";
			self.AddIllustration(new MenuDepthIllustration(self.menu, self, folderPath, filePath, new Vector2(), 2.2f,
				MenuDepthIllustration.MenuShader.Basic)); // Todo: Multiplayer positioning
		}
	}
}