using BepInEx;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using UnityEngine;

// TODO: Rewrite documentation at some point so that it's still actually accurate.

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618
namespace JollySleeping
{
	[BepInPlugin("sabreml.jollysleeping", "JollySleeping", "0.1.0")]
	public class JollySleepingMod : BaseUnityPlugin
	{
		/// <summary>List of the slugcat types of each player.</summary>
		/// <remarks>Each entry is lowercase, and is unique in the list.</remarks>
		public static List<string> PlayerSlugcatTypes;

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
		public static Dictionary<string, Vector2> IllustrationPositions;

		/// <summary>Bool indicating if the mod has been initialised yet.</summary>
		private static bool initialised;

		private TestingTools tempTestingTools; // Temporary for testing.


		/* ########## -SETUP- ########## */

		public void Update() // Temporary for testing.
		{
			tempTestingTools?.Update();
		}

		public void OnEnable()
		{
			On.RainWorld.OnModsInit += Init;
			On.StoryGameSession.CreateJollySlugStats += CreateJollySlugStatsHK;

			On.Menu.MenuScene.ctor += SleepSceneBuilder.MenuSceneHK;
		}

		private void Init(On.RainWorld.orig_OnModsInit orig, RainWorld self)
		{
			orig(self);
			if (!initialised)
			{
				IllustrationPositions = ReadPositionsFile();
				initialised = true;

				tempTestingTools = new TestingTools(); // Temporary for testing
			}
		}

		/// <summary>Parses the data from <c>'scenes\sleep screen - jollysleeping\positions.txt'</c> and returns a formatted dictionary.</summary>
		/// <returns>A dictionary containing data from <c>positions.txt</c> but with the keys and values flipped.</returns>
		/// <seealso cref="IllustrationPositions"/>
		private Dictionary<string, Vector2> ReadPositionsFile()
		{
			Dictionary<string, Vector2> output = new Dictionary<string, Vector2>();
			string[] fileLines = File.ReadAllLines(AssetManager.ResolveFilePath($"Scenes{Path.DirectorySeparatorChar}Sleep Screen - JollySleeping{Path.DirectorySeparatorChar}positions.txt"));

			// Parse the data
			foreach (string line in fileLines)
			{
				GroupCollection lineData = Regex.Match(line, @"(-?\d{1,3}), (-?\d{1,3}): (.*)").Groups;

				Vector2 coordinates = new Vector2(float.Parse(lineData[1].Value), float.Parse(lineData[2].Value));
				List<string> illustrations = Regex.Split(lineData[3].Value, ", ").ToList();
				foreach (string illustration in illustrations)
				{
					output.Add(illustration, coordinates);
				}
			}
			return output;
		}

		/// <summary>
		/// Uses the <see cref="StoryGameSession.characterStatsJollyplayer"/> array once it's been created to get the
		/// names of each slugcat controlled by a player, and assigns them to <see cref="PlayerSlugcatTypes"/>.
		/// </summary>
		/// <seealso cref="PlayerSlugcatTypes"/>
		private void CreateJollySlugStatsHK(On.StoryGameSession.orig_CreateJollySlugStats orig, StoryGameSession self, bool m)
		{
			orig(self, m);

			// Make a copy of the slugcats being controlled by players.
			PlayerSlugcatTypes = self.characterStatsJollyplayer
				.Where(entry => entry != null) // Filter out null entries.
				.Select(playerStats => playerStats.name.value.ToLower()) // Get the name of the slugcat they're playing as.
				.Distinct() // Duplicate entires get merged into one for simplicity. (["gourmand", "gourmand", "gourmand", "rivulet"] -> ["gourmand", "rivulet"])
				.ToList();
			PlayerSlugcatTypes.Sort(); // Sort the list alphabetically.
			Debug.Log("(JollySleeping) Player types cached.");
		}
	}
}