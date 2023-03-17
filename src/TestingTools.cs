using Menu;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

// This whole file contains awful code with 0 quality control. Take solace in the fact that it will be deleted once the mod releases.

namespace JollySleeping
{
	internal class TestingTools
	{
		private MoreSlugcats.ConsoleVisualizer console;
		private bool consoleVisible = false;

		private bool disableDepth = false;

		public TestingTools()
		{
			On.Menu.MenuScene.Update += MenuScene_UpdateHK;
			On.Menu.MenuScene.SaveToFile += MenuScene_SaveToFileHK;
			On.Menu.MenuScene.RefreshPositions += MenuScene_RefreshPositionsHK;

			On.Menu.MenuScene.GrafUpdate += MenuScene_GrafUpdateHK;
			On.Menu.MenuScene.CamPos += MenuScene_CamPosHK;
		}

		public void Update()
		{
			if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is Menu.Menu menu))
			{
				return;
			}
			if (Input.GetKeyDown("d"))
			{
				SetDefaultPos(menu.scene);
			}
			if (Input.GetKeyDown("f"))
			{
				SetActualPos(menu.scene);
			}
			if (Input.GetKeyDown("p"))
			{
				CycleIllustrationPos(menu.scene);
			}
			if (Input.GetKeyDown("l"))
			{
				if (Input.GetKey(KeyCode.LeftShift))
				{
					GetNewIllustration(menu.scene, true);
				}
				else
				{
					GetNewIllustration(menu.scene);
				}
			}
			if (Input.GetKeyDown("m"))
			{
				ToggleMonk(menu.scene);
			}
			if (Input.GetKeyDown("k"))
			{
				if (console == null)
				{
					console = new MoreSlugcats.ConsoleVisualizer();
				}
				consoleVisible = !consoleVisible;
				console.Visibility(consoleVisible);
			}
			if (Input.GetKeyDown("j"))
			{
				disableDepth = !disableDepth;
				Debug.Log("Scene depth " + (disableDepth ? "disabled." : "enabled."));
			}
		}

		private void MenuScene_UpdateHK(On.Menu.MenuScene.orig_Update orig, MenuScene self)
		{
			orig(self);
			if (consoleVisible)
			{
				console.Update();
			}
		}

		private void MenuScene_SaveToFileHK(On.Menu.MenuScene.orig_SaveToFile orig, MenuScene self)
		{
			Debug.Log($"Positions : {self.sceneID.value}");
			for (int i = 0; i < self.depthIllustrations.Count; i++)
			{
				MenuDepthIllustration target = self.depthIllustrations[i];
				Debug.Log($"{target.fileName}   {target.pos}");
			}
		}

		private void SetDefaultPos(MenuScene self)
		{
			MenuDepthIllustration currentIllust = GetCurrentIllustration(self);
			currentIllust.pos = currentIllust.fileName.Contains("spear") ? new Vector2(671f, 77f) : new Vector2(568f, 33f);
			Debug.Log("Position set to default.");
		}

		private void SetActualPos(MenuScene self, bool log = true)
		{
			MenuDepthIllustration currentIllust = GetCurrentIllustration(self);
			bool spearmaster = currentIllust.fileName.Contains("spear");
			if (JollySleepingMod.IllustrationPositions.ContainsKey(currentIllust.fileName))
			{
				Vector2 newPos = JollySleepingMod.IllustrationPositions[currentIllust.fileName];
				if (currentIllust.pos == newPos)
				{
					if (log) { Debug.Log("Position already at custom."); }
					return;
				}
				currentIllust.pos = JollySleepingMod.IllustrationPositions[currentIllust.fileName];
				if (log) { Debug.Log("Position set to custom."); }
				return;
			}
			Vector2 defaultPos = spearmaster ? new Vector2(671f, 77f) : new Vector2(568f, 33f);
			if (currentIllust.pos == defaultPos)
			{
				if (log) { Debug.Log("Position already at default."); }
				return;
			}
			currentIllust.pos = defaultPos;
			if (log) { Debug.Log("Position set to default."); }
		}

		private void CycleIllustrationPos(MenuScene self)
		{
			MenuDepthIllustration currentIllust = GetCurrentIllustration(self);
			Dictionary<string, Vector2> positions = JollySleepingMod.IllustrationPositions;

			int nextIndex = -1;
			for (int i = 0; i < positions.Count; i++)
			{
				if (positions.ElementAt(i).Value == currentIllust.pos)
				{
					nextIndex = (i + 1) % positions.Count;
					while (positions.ElementAt(nextIndex).Value == currentIllust.pos) // Get past any others with the same coordinates.
					{
						nextIndex = (nextIndex + 1) % positions.Count;
					}
					break;
				}
			}
			if (nextIndex == -1) // haven't found anything
			{
				nextIndex = 0;
			}

			KeyValuePair<string, Vector2> nextElement = positions.ElementAt(nextIndex);
			Debug.Log($"Illustration pos: {currentIllust.pos} --> {nextElement.Value} | {nextElement.Key}");
			currentIllust.pos = nextElement.Value;
		}

		private void GetNewIllustration(MenuScene self, bool setPos = false)
		{
			MenuDepthIllustration currentIllust = GetCurrentIllustration(self);
			MenuDepthIllustration grassIllust = self.depthIllustrations.Find(item => item.fileName.Contains("- 1")); // grass stuff

			List<string> dirList = AssetManager.ListDirectory($"Scenes/Sleep Screen - JollySleeping").ToList();
			dirList.RemoveAll(item => item.Contains("positions.txt"));
			int currentFileIndex = dirList.FindIndex(item => Regex.Match(item, $@"[/\\].+[/\\]{currentIllust.fileName}.png").Success);

			string targetFile = Regex.Match(dirList[(currentFileIndex + 1) % dirList.Count], @"[/\\].+[/\\](.+)\.png").Groups[1].Value;

			// add new stuff
			Debug.Log($"{currentIllust.fileName} -> {targetFile}");

			// spearmaster things
			bool spearmaster = targetFile.Contains("spear");
			if (spearmaster)
			{
				SleepSceneBuilder.SetupSpearmasterDoodle(self);
			}
			else
			{
				// just in case it's switching from a spearmaster image
				RemoveIllustration(self, self.depthIllustrations.Find(item => item.fileName.Contains("- D")));
				Futile.atlasManager.UnloadAtlas("Sleep - 3");
				RemoveIllustration(self, self.depthIllustrations.Find(item => item.fileName.Contains("- 3")));
				self.AddIllustration(new MenuDepthIllustration(self.menu, self, "Scenes/Sleep Screen - White", "Sleep - 3", new Vector2(696f, 118f), 2.2f, MenuDepthIllustration.MenuShader.Normal));
				Futile.atlasManager.UnloadAtlas("Sleep - 1");
			}
			// add the slugcat and foreground grass
			self.AddIllustration(new MenuDepthIllustration(self.menu, self, "Scenes/Sleep Screen - JollySleeping", targetFile, currentIllust.pos, currentIllust.depth, currentIllust.shader));

			string grassFolder = spearmaster ? "scenes/sleep screen - spear" : "scenes/sleep screen - white";
			self.AddIllustration(new MenuDepthIllustration(self.menu, self, grassFolder, grassIllust.fileName, grassIllust.pos, grassIllust.depth, grassIllust.shader)); //grass

			// remove old stuff
			RemoveIllustration(self, currentIllust);
			RemoveIllustration(self, grassIllust);

			if (setPos)
			{
				SetActualPos(self, false);
			}
		}

		private void RemoveIllustration(MenuScene self, MenuDepthIllustration illustToRemove)
		{
			if (illustToRemove != null)
			{
				illustToRemove.RemoveSprites();
				self.depthIllustrations.Remove(illustToRemove);
				self.RemoveSubObject(illustToRemove);
			}
		}

		private MenuDepthIllustration tempMonk;
		private void ToggleMonk(MenuScene self)
		{
			if (tempMonk != null)
			{
				tempMonk.RemoveSprites();
				self.depthIllustrations.Remove(tempMonk);
				self.RemoveSubObject(tempMonk);
				tempMonk = null;
				Debug.Log("!konM");
			}
			else
			{
				tempMonk = new MenuDepthIllustration(self.menu, self, "Scenes/Sleep Screen - yellow", "sleep - 2 - yellow", new Vector2(782f, 117f), 1.7f, MenuDepthIllustration.MenuShader.Normal);
				self.AddIllustration(tempMonk);
				Debug.Log("Monk!");
			}
		}

		private void MenuScene_RefreshPositionsHK(On.Menu.MenuScene.orig_RefreshPositions orig, MenuScene self)
		{
			if (self.sceneID != MenuScene.SceneID.SleepScreen)
			{
				orig(self);
				return;
			}

			Dictionary<string, string> before = LogIllustrationPos(self);
			orig(self);
			Dictionary<string, string> after = LogIllustrationPos(self);

			foreach (KeyValuePair<string, string> pair in before)
			{
				if (pair.Value != after[pair.Key])
				{
					Debug.Log($"{pair.Key} pos changed from {pair.Value} --> {after[pair.Key]}");
				}
			}
		}

		private MenuDepthIllustration GetCurrentIllustration(MenuScene scene)
		{
			return scene.depthIllustrations.Find(item => Regex.IsMatch(item.fileName, @"sleep - 2|(artificer|saint|gourmand|rivulet|spear|white|red|yellow)", RegexOptions.IgnoreCase));
		}

		private Dictionary<string, string> LogIllustrationPos(MenuScene self)
		{
			// illustration.fileName: illustration.pos
			Dictionary<string, string> result = new Dictionary<string, string>();
			foreach (MenuDepthIllustration illustration in self.depthIllustrations)
			{
				result.Add(illustration.fileName, illustration.pos.ToString());
			}
			return result;
		}

		private void MenuScene_GrafUpdateHK(On.Menu.MenuScene.orig_GrafUpdate orig, MenuScene self, float timeStacker)
		{
			orig(self, timeStacker);
			if (disableDepth)
			{
				Shader.SetGlobalFloat("_BlurDepth", 0f);
				Shader.SetGlobalFloat("_BlurRange", 0f);
				Shader.SetGlobalVector("_MenuCamPos", Vector2.zero);
			}
		}

		private Vector2 MenuScene_CamPosHK(On.Menu.MenuScene.orig_CamPos orig, MenuScene self, float timeStacker)
		{
			if (disableDepth)
			{
				return Vector2.zero;
			}
			return orig(self, timeStacker);
		}
	}
}
