namespace Stranne.EcsMemory.Test;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Chickensoft.GodotTestDriver;
using Chickensoft.GodotTestDriver.Drivers;
using Godot;
using Shouldly;

public class GameTest(Node testScene) : TestClass(testScene) {
  private Game _game = null!;
  private Fixture _fixture = null!;

  [SetupAll]
  public async Task Setup() {
    _fixture = new Fixture(TestScene.GetTree());
    _game = await _fixture.LoadAndAddScene<Game>();
  }

  [CleanupAll]
  public void Cleanup() => _fixture.Cleanup();

  [Test]
  public void TestButtonUpdatesCounter() {
    var buttonDriver = new ButtonDriver(() => _game.TestButton);
    buttonDriver.ClickCenter();
    _game.ButtonPresses.ShouldBe(1);
  }
}
