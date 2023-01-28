using BepInEx;
using Menu;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace JollySleeping
{
	[BepInPlugin("sabreml.jollysleeping", "JollySleeping", "0.1.0")]
	public class JollySleepingMod : BaseUnityPlugin
	{
		/// <summary>List of the slugcat types of each player.</summary>
		/// <remarks>Each entry is lowercase, and is unique in the list.</remarks>
		private List<string> playerSlugcatTypes;

		/// <summary>Dictionary of slugcat illustration positions.</summary>
		/// <remarks>
		/// The contained data is the same as <c>'scenes\sleep screen - jollysleeping\positions.txt'</c>,
		/// but with the keys and values flipped.<br/>
		/// This is so that multiple illustrations can be easily assigned to a single coordinate in the <c>.txt</c> file without a new line being needed for each.
		/// </remarks>
		/// <value>
		/// <code>
		/// {
		///   "Illustration name 1": Vector2(x, y),
		///   "Illustration name 2": Vector2(x, y)
		/// }
		/// </code>
		/// </value>
		public static Dictionary<string, Vector2> illustrationPositions; // Temporarily `public static` so that `TestingTools` can use it.

		private TestingTools tempTestingTools; // Temporary for testing.

		/* ########## -SETUP- ########## */

		public void Update() // Temporary for testing.
		{
			tempTestingTools?.Update();
		}

		public void OnEnable()
		{
			On.RainWorld.OnModsInit += OnInit;
			On.StoryGameSession.CreateJollySlugStats += CreateJollySlugStatsHK;
			On.Menu.MenuScene.ctor += MenuSceneHK;
		}

		private void OnInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
		{
			orig(self);
			illustrationPositions = ReadPositionsFile();
			VerifyFileNames();

			tempTestingTools = new TestingTools(self); // Temporary for testing
		}

		/// <summary>Parses the data from <c>'scenes\sleep screen - jollysleeping\positions.txt'</c> and returns a formatted dictionary.</summary>
		/// <returns>A dictionary containing data from <c>positions.txt</c> but with the keys and values flipped.</returns>
		/// <seealso cref="illustrationPositions"/>
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

		/// <summary>Verifies that all png files in <c>'scenes\sleep screen - jollysleeping'</c> are properly alphabetical.</summary>
		/// <remarks>
		/// Throwing a consistent exception in users' games if the mod creator forgot something is generally a bad idea,
		/// so hopefully this will force me to properly test everything before pushing.
		/// </remarks>
		/// <exception cref="System.Exception">Thrown if a file name is found to be incorrect.</exception>
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
					throw new System.Exception($"(JollySleeping) File {fileName}.png is not named alphabetically!");
				}
			}
		}


		/* ########## -HOOKS- ########## */

		/// <summary>
		/// Uses the <see cref="StoryGameSession.characterStatsJollyplayer"/> array once it's been created to get the
		/// names of each slugcat controlled by a player, and assigns them to <see cref="playerSlugcatTypes"/>.
		/// </summary>
		/// <seealso cref="playerSlugcatTypes"/>
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
			Debug.Log("(JollySleeping) Player types cached.");
		}

		/// <summary>
		/// Removes existing illustrations which are about to be replaced, and calls either <see cref="AddSingleplayerSlugcat(MenuScene, bool)"/><br/>
		/// or <see cref="AddMultiplayerSlugcats(MenuScene, bool)"/> depending on the player count.
		/// </summary>
		private void MenuSceneHK(On.Menu.MenuScene.orig_ctor orig, MenuScene self, Menu.Menu menu, MenuObject owner, MenuScene.SceneID sceneID)
		{
			orig(self, menu, owner, sceneID);
			if (self.sceneID != MenuScene.SceneID.SleepScreen || self.flatMode)
			{
				return;
			}
			if (playerSlugcatTypes == null)
			{
				Debug.Log("(JollySleeping) Player types not cached!");
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
		/// Adds Slugcat and grass illustrations back into the scene, selecting the Slugcat illustration variant based on the combined contents of <see cref="playerSlugcatTypes"/>.
		/// </summary>
		/// <remarks>
		/// The position of the Slugcat illustration will default to <c>Vector2(568f, 33f)</c> unless the variant has an override set in <c>positions.txt</c> (<see cref="illustrationPositions"/>),
		/// in which case that will be used instead.
		/// </remarks>
		/// <seealso cref="illustrationPositions"/>
		/// <param name="spearmaster"><c>bool</c> indicating if the one of the players is using Spearmaster.</param>
		/// <returns>The <see cref="MenuDepthIllustration"/> of the new slugcat image.</returns>
		private MenuDepthIllustration AddMultiplayerSlugcats(MenuScene self, bool spearmaster)
		{
			string folderName = $"Scenes{Path.DirectorySeparatorChar}Sleep Screen - JollySleeping";
			string fileName = string.Join("-", playerSlugcatTypes);

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
				slugcatIllustration.pos = illustrationPositions[slugcatIllustration.fileName];
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
		private MenuDepthIllustration AddSingleplayerSlugcat(MenuScene self, bool spearmaster)
		{
			string playerSlugcat = playerSlugcatTypes[0];
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
		private void AddSpearmasterDoodle(MenuScene self)
		{
			string folderPath = $"Scenes{Path.DirectorySeparatorChar}Sleep Screen - Spear";
			string filePath = $"Sleep - D{Random.Range(1, 11)}";
			self.AddIllustration(new MenuDepthIllustration(self.menu, self, folderPath, filePath, new Vector2(), 2.2f,
				MenuDepthIllustration.MenuShader.Basic)); // Todo: Multiplayer positioning
		}
	}
}