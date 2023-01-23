﻿using BepInEx;
using Menu;
using MoreSlugcats;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

[BepInPlugin("sabreml.jollysleeping", "JollySleeping", "0.1.0")]
public class JollySleepingMod : BaseUnityPlugin
{
	// Dictionary of slugcat types and their position on the sleeping screen. The order of the entries determines layering.
	private readonly Dictionary<string, Vector2> slugcatPositions = new Dictionary<string, Vector2>
		{
			{ "Rivulet",    new Vector2(623f, 151f) },
			{ "Saint",		new Vector2(803f, 134f) },
			{ "Spear",		new Vector2(822f, 63f) },
			{ "Gourmand",	new Vector2(708f, 53f) },
			{ "Artificer",	new Vector2(782f, 117f) },
			{ "White",		new Vector2(677f, 63f) },
			{ "Yellow",		new Vector2(782f, 117f) },
			{ "Red",		new Vector2(817f, 112f) }
		};
	/* Default positions
	{ "Saint",		new Vector2(811f, 66f) },
	{ "Spear",		new Vector2(671f, 77f) },
	{ "Rivulet",	new Vector2(805f, 109f) },
	{ "Gourmand",	new Vector2(748f, 115f) },
	{ "Artificer",	new Vector2(782f, 117f) },
	{ "White",		new Vector2(677f, 63f) },
	{ "Yellow",		new Vector2(782f, 117f) },
	{ "Red",		new Vector2(817f, 112f) }
	*/

	// List of the slugcat types of each player.
	private List<string> playerSlugcatTypes;

	public void OnEnable()
	{
		On.StoryGameSession.CreateJollySlugStats += CreateJollySlugStatsHK;
		On.Menu.MenuScene.ctor += MenuSceneHK;
		On.Menu.MenuScene.SaveToFile += MenuScene_SaveToFile;
	}

	private void MenuScene_SaveToFile(On.Menu.MenuScene.orig_SaveToFile orig, MenuScene self)
	{
		self.sceneFolder = "";
		orig(self);
	}

	private void CreateJollySlugStatsHK(On.StoryGameSession.orig_CreateJollySlugStats orig, StoryGameSession self, bool m)
	{
		orig(self, m);

		playerSlugcatTypes = self.characterStatsJollyplayer.Select(playerStats => playerStats?.name.value).ToList();
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
			Debug.Log("(JollySleeping) < 2 players detected, setting default illustration."); // todo: this doesn't work
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
		bool spearmaster = false;
		// Looping through `slugcatPositions` rather than `playerSlugcatTypes` so that layering works.
		foreach (KeyValuePair<string, Vector2> positionEntry in slugcatPositions)
		{
			string playerSlugcat = playerSlugcatTypes.Find(entry => entry == positionEntry.Key);
			if (playerSlugcat == null)
			{
				continue;
			}
			self.sceneFolder = $"Scenes{Path.DirectorySeparatorChar}Sleep Screen - {playerSlugcat}";

			if (ModManager.MSC && playerSlugcat == "Spear")
			{
				spearmaster = true;
				List<string> doodlePaths = GetRandomDoodle();
				Vector2 doodlePos = slugcatPositions["Spear"] - new Vector2(-294f, 17f);

				// Spearmaster doodles
				self.AddIllustration(new MenuDepthIllustration(self.menu, self, doodlePaths[0], doodlePaths[1], doodlePos, 2.2f, MenuDepthIllustration.MenuShader.Basic));
			}

			// Add the new slugcat image to the scene.
			self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, $"Sleep - 2 - {playerSlugcat}", positionEntry.Value, 1.7f, MenuDepthIllustration.MenuShader.Normal));

		}

		// Add the foreground grass back in. (Spearmaster's grass has a small gap in it)
		if (spearmaster && Futile.atlasManager.GetAtlasWithName("Sleep - 1") != null)
		{
			Futile.atlasManager.UnloadAtlas("Sleep - 1"); // Replace the old (non-spearmaster) version.
		}
		string grassPath = $"Scenes{Path.DirectorySeparatorChar}Sleep Screen - Spear";
		self.AddIllustration(new MenuDepthIllustration(self.menu, self, grassPath, "Sleep - 1", new Vector2(486f, -54f), 1.2f, MenuDepthIllustration.MenuShader.Normal));
	}

	// Returns the folder and file path for a random spearmaster doodle.
	private List<string> GetRandomDoodle()
	{
		string folderPath = $"Scenes{Path.DirectorySeparatorChar}Sleep Screen - Spear";
		string filePath = $"Sleep - D{Random.Range(1, 11)}";
		return new List<string> { folderPath, filePath };
	}
}