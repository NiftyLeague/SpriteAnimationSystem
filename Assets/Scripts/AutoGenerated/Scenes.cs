//This class is auto-generated do not modify
using UnityEngine.SceneManagement;

namespace NiftyLeague
{
	public static class Scenes
	{
		public const string Launcher = "Launcher";
		public const string Lobby = "Lobby";
		public const string ScoreScreen = "ScoreScreen";
		public const string LevelSushi = "LevelSushi";
		public const string LevelAlien = "LevelAlien";
		public const string LevelApe = "LevelApe";
		public const string LevelMars = "LevelMars";
		public const string LevelRoadmap = "LevelRoadmap";
		public const string LevelSatoshi = "LevelSatoshi";
		public const string LevelDoge = "LevelDoge";
		public const string LevelTrain = "LevelTrain";
		public const string WinnerScene = "WinnerScene";

		public const int TotalScenes = 11;


		public static int NextSceneIndex()
		{
			if( SceneManager.GetActiveScene().buildIndex + 1 == TotalScenes )
				return 0;
			return SceneManager.GetActiveScene().buildIndex + 1;
		}
	}
}
