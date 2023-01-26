using Menu;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace JollySleeping
{
	internal class TestingTools
	{
		public static void InitHooks()
		{
			On.Menu.MenuScene.SaveToFile += MenuScene_SaveToFile; // temp for testing
			On.Menu.MenuScene.Update += MenuScene_Update; // temp for testing
			On.Menu.MenuScene.RefreshPositions += MenuScene_RefreshPositions1;
		}

		private static void MenuScene_SaveToFile(On.Menu.MenuScene.orig_SaveToFile orig, MenuScene self)
		{
			Debug.Log($"Positions : {self.sceneID.value}");
			for (int i = 0; i < self.depthIllustrations.Count; i++)
			{
				MenuDepthIllustration target = self.depthIllustrations[i];
				Debug.Log($"{target.fileName}   {target.pos}");
			}
		}

		private static void MenuScene_Update(On.Menu.MenuScene.orig_Update orig, MenuScene self)
		{
			orig(self);
			if (Input.GetKeyDown("l"))
			{
				GetNewIllustration(self);
			}
			if (Input.GetKeyDown("m"))
			{
				ToggleMonk(self);
			}
		}
		private static void GetNewIllustration(MenuScene self)
		{
			MenuDepthIllustration currentIllust = self.depthIllustrations.Find(item => Regex.IsMatch(item.fileName, @"sleep - 2|(artificer|saint|gourmand|rivulet|spear|white|red|yellow)", RegexOptions.IgnoreCase));
			MenuDepthIllustration grassIllust = self.depthIllustrations.Find(item => item.fileName.Contains("- 1")); // grass stuff

			List<string> dirList = AssetManager.ListDirectory($"Scenes/Sleep Screen - JollySleeping").ToList();
			dirList.RemoveAll(item => item.Contains("positions.txt"));
			int currentFileIndex = dirList.FindIndex(item => item.Contains(currentIllust.fileName.ToLower() + ".png"));

			string targetFile = Regex.Match(dirList[(currentFileIndex + 1) % dirList.Count], @"\\(?!.*\\)(.*-.*).png").Groups[1].Value;

			// add new stuff
			Debug.Log($"{currentIllust.fileName} -> {targetFile}");
			self.AddIllustration(new MenuDepthIllustration(self.menu, self, currentIllust.folderName, targetFile, currentIllust.pos, currentIllust.depth, currentIllust.shader));
			self.AddIllustration(new MenuDepthIllustration(self.menu, self, grassIllust.folderName, grassIllust.fileName, grassIllust.pos, grassIllust.depth, grassIllust.shader)); //grass

			// remove old stuff
			currentIllust.RemoveSprites();
			self.depthIllustrations.Remove(currentIllust);
			self.RemoveSubObject(currentIllust);
			currentIllust = null; // probably don't need to null this but eh
			grassIllust.RemoveSprites();
			self.depthIllustrations.Remove(grassIllust);
			self.RemoveSubObject(grassIllust);
			grassIllust = null;
		}

		private static MenuDepthIllustration tempMonk;

		private static void ToggleMonk(MenuScene self)
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

		private static void MenuScene_RefreshPositions1(On.Menu.MenuScene.orig_RefreshPositions orig, MenuScene self)
		{
			if (self.sceneID != MenuScene.SceneID.SleepScreen)
			{
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

		private static Dictionary<string, string> LogIllustrationPos(MenuScene self)
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
