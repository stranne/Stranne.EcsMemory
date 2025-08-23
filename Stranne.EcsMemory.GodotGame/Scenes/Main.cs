using Chickensoft.GameTools.Displays;
using Chickensoft.GoDotTest;
using Godot;

namespace Stranne.EcsMemory.GodotGame.Scenes;

#if RUN_TESTS
using System.Reflection;
#endif

// This entry-point file is responsible for determining if we should run tests.
//
// If you want to edit your game's main entry-point, please see Game.tscn and
// Game.cs instead.

public partial class Main : Node2D {
  public static Vector2I DesignResolution => Display.UHD4k;
#if RUN_TESTS
  public TestEnvironment Environment = null!;
#endif

  public override void _Ready() {
	// Correct any erroneous scaling and guess sensible defaults.
	GetWindow().LookGood(WindowScaleBehavior.UIFixed, DesignResolution);

#if RUN_TESTS
	// If this is a debug build, use GoDotTest to examine the
	// command line arguments and determine if we should run tests.
	Environment = TestEnvironment.From(OS.GetCmdlineArgs());
	if (Environment.ShouldRunTests) {
	  CallDeferred(nameof(RunTests));
	  return;
	}
#endif

	// If we don't need to run tests, we can just switch to the game scene.
	CallDeferred(nameof(RunScene));
  }

#if RUN_TESTS
  private void RunTests()
	=> _ = GoTest.RunTests(Assembly.GetExecutingAssembly(), this, Environment);
#endif

  private void RunScene()
	=> GetTree().ChangeSceneToFile("res://Scenes/Game.tscn");
}
