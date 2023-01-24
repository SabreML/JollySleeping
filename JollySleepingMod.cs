using BepInEx;
using Menu;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

[BepInPlugin("sabreml.jollysleeping", "JollySleeping", "0.1.0")]
public class JollySleepingMod : BaseUnityPlugin
{
	// List of the slugcat types of each player.
	private List<string> playerSlugcatTypes;

	public void OnEnable()
	{
		On.RainWorld.OnModsInit += OnInit;
		On.StoryGameSession.CreateJollySlugStats += CreateJollySlugStatsHK;
		On.Menu.MenuScene.ctor += MenuSceneHK;
		On.Menu.MenuScene.SaveToFile += MenuScene_SaveToFile; // temp for testing
		On.Menu.MenuScene.Update += MenuScene_Update; // temp for testing
	}

	private void OnInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
	{
		orig(self);

		// Verify that all of the new PNG files are properly formatted. (Alphabetical name order with dashes)
		string[] fileList = AssetManager.ListDirectory($"Scenes{Path.DirectorySeparatorChar}Sleep Screen - JollySleeping");
		foreach (string filePath in fileList)
		{
			// Get just the file name without any slashes or the file extension.
			string fileName = Regex.Match(filePath, string.Format("\\{0}(?!.*\\{0})(.*-.*).png", Path.DirectorySeparatorChar)).Groups[1].Value;
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
				Debug.Log($"(JollySleeping) File {fileName}.png is not named alphabetically! This may cause bugs later.");
				break;
			}
		}
	}

	private void CreateJollySlugStatsHK(On.StoryGameSession.orig_CreateJollySlugStats orig, StoryGameSession self, bool m)
	{
		orig(self, m);

		// Make a copy of the slugcats being controlled by players.
		playerSlugcatTypes = (from playerStats in self.characterStatsJollyplayer where playerStats != null select playerStats.name.value).ToList();
		playerSlugcatTypes.Sort(); // Sort the list alphabetically.
		Debug.Log("(JollySleeping) Player stats cached.");
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
			Debug.Log("(JollySleeping) Player stats not cached!");
			return;
		}
		if (playerSlugcatTypes.Count < 2) // Don't mess with anything if it's just singleplayer.
		{
			Debug.Log("(JollySleeping) < 2 players detected, using default illustration.");
			return;
		}

		// Remove the existing slugcat illustration (2), foreground grass (1), and the Spearmaster's doodles if present (d).
		string pattern = @"[21d]";
		self.depthIllustrations.RemoveAll(item => Regex.IsMatch(item.fileName, pattern));
		self.subObjects.RemoveAll(item => Regex.IsMatch((item as MenuDepthIllustration).fileName, pattern));

		AddSlugcatIllustrations(self);
	}

	private void AddSlugcatIllustrations(MenuScene self)
	{
		string folderName = $"Scenes{Path.DirectorySeparatorChar}Sleep Screen - jollysleeping";
		string fileName = string.Join("-", playerSlugcatTypes);
		Debug.Log($"(JollySleeping) displaying {fileName}");

		// Spearmaster doodles
		if (ModManager.MSC && playerSlugcatTypes.Contains("Spear"))
		{
			List<string> doodlePaths = GetRandomDoodle();

			self.AddIllustration(new MenuDepthIllustration(self.menu, self, doodlePaths[0], doodlePaths[1], new Vector2(), 2.2f, MenuDepthIllustration.MenuShader.Basic)); // Todo: Positioning
		}

		// Add the slugcats to the scene.
		self.AddIllustration(new MenuDepthIllustration(self.menu, self, folderName, fileName, new Vector2(782f, 117f), 1.7f, MenuDepthIllustration.MenuShader.Normal)); // Todo: Positioning

		// Add the foreground grass back in too. (Grass has to be added afterwards for layering)
		string grassFolder = $"Scenes{Path.DirectorySeparatorChar}Sleep Screen - White";
		self.AddIllustration(new MenuDepthIllustration(self.menu, self, grassFolder, "Sleep - 1", new Vector2(486f, -54f), 1.2f, MenuDepthIllustration.MenuShader.Normal));
	}

	// Returns the folder and file path for a random spearmaster doodle.
	private List<string> GetRandomDoodle()
	{
		string folderPath = $"Scenes{Path.DirectorySeparatorChar}Sleep Screen - Spear";
		string filePath = $"Sleep - D{Random.Range(1, 11)}";
		return new List<string> { folderPath, filePath };
	}



	// ########## --DEBUGGING/TESTING METHODS BELOW-- ##########

	private void MenuScene_SaveToFile(On.Menu.MenuScene.orig_SaveToFile orig, MenuScene self)
	{
		Debug.Log($"Positions : {self.sceneID.value}");
		for (int i = 0; i < self.depthIllustrations.Count; i++)
		{
			MenuDepthIllustration target = self.depthIllustrations[i];
			Debug.Log($"{target.fileName}   {target.pos}");
		}
	}

	// Used to loop through every illustration in the folder. Useful for quickly testing positioning.
	private void MenuScene_Update(On.Menu.MenuScene.orig_Update orig, MenuScene self)
	{
		orig(self);
		if (Input.GetKeyDown("l"))
		{
			GetNewIllustration(self);
		}
	}
	private void GetNewIllustration(MenuScene self)
	{
		MenuDepthIllustration currentIllust = self.depthIllustrations.Find(item => Regex.IsMatch(item.fileName, @"sleep - 2|(artificer|saint|gourmand|rivulet|spear|white|red|yellow)", RegexOptions.IgnoreCase));
		MenuDepthIllustration grassIllust = self.depthIllustrations.Find(item => item.fileName.Contains("- 1")); // grass stuff

		List<string> dirList = AssetManager.ListDirectory($"Scenes{Path.DirectorySeparatorChar}Sleep Screen - jollysleeping").ToList();
		dirList.RemoveAll(item => item.Contains("positions.txt"));
		int currentFileIndex = dirList.FindIndex(item => item.Contains(currentIllust.fileName.ToLower() + ".png"));

		string targetFile = Regex.Match(dirList[(currentFileIndex + 1) % dirList.Count], @"\\(?!.*\\)(.*-.*).png").Groups[1].Value;

		Debug.Log($"{currentIllust.fileName} -> {targetFile}");
		self.AddIllustration(new MenuDepthIllustration(self.menu, self, currentIllust.folderName, targetFile, currentIllust.pos, currentIllust.depth, currentIllust.shader));
		self.AddIllustration(new MenuDepthIllustration(self.menu, self, grassIllust.folderName, grassIllust.fileName, grassIllust.pos, grassIllust.depth, grassIllust.shader)); //grass

		currentIllust.RemoveSprites();
		self.depthIllustrations.Remove(currentIllust);
		self.RemoveSubObject(currentIllust);
		currentIllust = null; // probably don't need to null this but eh
		grassIllust.RemoveSprites();
		self.depthIllustrations.Remove(grassIllust);
		self.RemoveSubObject(grassIllust);
		grassIllust = null;
	}
}
