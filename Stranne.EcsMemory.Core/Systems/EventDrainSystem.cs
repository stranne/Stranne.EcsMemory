using Arch.Core;
using Arch.System;
using Stranne.EcsMemory.Contracts.Event;
using Stranne.EcsMemory.Core.Components.Events;

namespace Stranne.EcsMemory.Core.Systems;
internal sealed class EventDrainSystem(World world)
    : BaseSystem<World, float>(world)
{
    private static readonly QueryDescription WonQuery = new QueryDescription().WithAll<EventWon, EventMetadata>();

    private List<IGameEvent> Sink { get; } = [];

    public override void Update(in float _)
    {
        World.Query(in WonQuery, (Entity entity, ref EventWon eventWon, ref EventMetadata eventMetadata) =>
        {
            Sink.Add(new WonEvent(eventWon.Moves, eventWon.TotalCards, eventMetadata.StateVersion));
            World.Destroy(entity);
        });
    }

    public List<IGameEvent> PopEvents()
    {
        var events = new List<IGameEvent>(Sink);
        Sink.Clear();
        return events;
    }
}
