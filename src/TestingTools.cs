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
		private readonly ProcessManager processManagerCache;

		public TestingTools(RainWorld rainWorld)
		{
			On.Menu.MenuScene.SaveToFile += MenuScene_SaveToFileHK;
			On.Menu.MenuScene.RefreshPositions += MenuScene_RefreshPositionsHK;

			processManagerCache = rainWorld.processManager;
		}

		public void Update()
		{
			if (!(processManagerCache.currentMainLoop is Menu.Menu menu))
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
			currentIllust.pos.x = 568f;
			currentIllust.pos.y = 33f;
			Debug.Log("Position set to default.");
		}

		private void SetActualPos(MenuScene self, bool log = true)
		{
			MenuDepthIllustration currentIllust = GetCurrentIllustration(self);
			if (JollySleepingMod.illustrationPositions.ContainsKey(currentIllust.fileName))
			{
				Vector2 newPos = JollySleepingMod.illustrationPositions[currentIllust.fileName];
				if (currentIllust.pos == newPos)
				{
					if (log) { Debug.Log("Position already at custom."); }
					return;
				}
				currentIllust.pos = JollySleepingMod.illustrationPositions[currentIllust.fileName];
				if (log) { Debug.Log("Position set to custom."); }
				return;
			}
			Vector2 defaultPos = new Vector2(568f, 33f);
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
			Dictionary<string, Vector2> positions = JollySleepingMod.illustrationPositions;

			int nextIndex = 1000;
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
			if (nextIndex == 1000) // haven't found anything
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
			self.AddIllustration(new MenuDepthIllustration(self.menu, self, "Scenes/Sleep Screen - JollySleeping", targetFile, currentIllust.pos, currentIllust.depth, currentIllust.shader));
			self.AddIllustration(new MenuDepthIllustration(self.menu, self, grassIllust.folderName, grassIllust.fileName, grassIllust.pos, grassIllust.depth, grassIllust.shader)); //grass

			// remove old stuff
			currentIllust.RemoveSprites();
			self.depthIllustrations.Remove(currentIllust);
			self.RemoveSubObject(currentIllust);
			currentIllust = null;
			grassIllust.RemoveSprites();
			self.depthIllustrations.Remove(grassIllust);
			self.RemoveSubObject(grassIllust);
			grassIllust = null;

			if (setPos)
			{
				SetActualPos(self, false);
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
	}
}
