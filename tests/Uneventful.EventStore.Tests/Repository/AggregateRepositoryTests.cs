using Uneventful.EventStore.InMemory;
using Uneventful.EventStore.Repository;
using Uneventful.EventStore.Snapshot;
using Uneventful.Snapshot.InMemory;

namespace Uneventful.EventStore.Tests.Repository;

public class AggregateRepositoryTests {

    private readonly AggregateRepository _repository;
    private readonly InMemoryEventStore _eventStore;
    private readonly InMemorySnapshotStore _snapshotStore;
    
    public AggregateRepositoryTests() {
        _eventStore = new InMemoryEventStore();
        _snapshotStore = new InMemorySnapshotStore();
        _repository = new AggregateRepository(_eventStore, _snapshotStore);
    }

    [Fact]
    public async Task CanLoadAggregateFromEventStore() {
        var aggregate = new TestAggregate();
        int[] numbers = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
        aggregate.Create("test", numbers);
        await _eventStore.AppendToStream("test:test", aggregate.Changes.ToArray(), 0);

        aggregate = await _repository.LoadAsync<TestAggregate>("test:test");
        
        Assert.NotNull(aggregate);
        Assert.Equal("test:test", aggregate.StreamId);
        Assert.Equal(numbers, aggregate.Numbers);
        Assert.Equal(11, aggregate.Version);
    }

    [Fact]
    public async Task LoadingNonExistentAggregateReturnsNull() {
        var aggregate = await _repository.LoadAsync<TestAggregate>("test:test");
        
        Assert.Null(aggregate);
    }

    [Fact]
    public async Task SavingAnAggregateWithNoChangesWillResultToNoOp() {
        var aggregate = new TestAggregate();
        int[] numbers = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
        aggregate.Create("test", numbers);
        aggregate.ClearChanges();
        await _repository.SaveAsync(aggregate);
        
        aggregate = await _repository.LoadAsync<TestAggregate>("test:test");
        
        Assert.Null(aggregate);
    }

    [Fact]
    public async Task CanLoadAggregateFromSnapshot() {
        var aggregate = new TestAggregate();
        int[] numbers = [0, 1, 2, 3, 4, 5, 6, 7, 8];
        int[] actual = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
        aggregate.Create("test", numbers);
        aggregate.Version = 10;
        aggregate.ClearChanges();
        await _snapshotStore.SaveSnapshot(aggregate);
        await _eventStore.AppendToStream("test:test", [new TestAggregate.ItemNumberAdded("test", 9)], aggregate.Version);
        
        aggregate = await _repository.LoadAsync<TestAggregate>("test:test");
        
        Assert.NotNull(aggregate);
        Assert.Equal("test:test", aggregate.StreamId);
        Assert.Equal(actual, aggregate.Numbers);
        Assert.Equal(11, aggregate.Version);
    }

    [Fact]
    public async Task CanSaveAggregate() {
        var aggregate = new TestAggregate();
        int[] numbers = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
        aggregate.Create("test", numbers);
        await _repository.SaveAsync(aggregate);
        
        aggregate = await _repository.LoadAsync<TestAggregate>("test:test");
        Assert.NotNull(aggregate);
        Assert.Equal("test:test", aggregate.StreamId);
        Assert.Equal(numbers, aggregate.Numbers);
        Assert.Equal(11, aggregate.Version);
    }
    
    [Fact]
    public async Task CanSaveAggregateWithSnapshotCapableAggregateCondition() {
        var aggregate = new TestAggregate();
        int[] numbers = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        aggregate.Create("test", numbers);
        await _repository.SaveAsync(aggregate);

        aggregate = await _snapshotStore.LoadSnapshot<TestAggregate>("test:test");
        Assert.NotNull(aggregate);
        Assert.Equal("test:test", aggregate.StreamId);
        Assert.Equal(numbers, aggregate.Numbers);
        Assert.Equal(12, aggregate.Version);
    }
    
    [Fact]
    public async Task CanSaveAndForceSnapshotCapableAggregate() {
        var aggregate = new TestAggregate();
        int[] numbers = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
        aggregate.Create("test", numbers);
        await _repository.SaveAndForceSnapshot(aggregate);

        aggregate = await _snapshotStore.LoadSnapshot<TestAggregate>("test:test");
        Assert.NotNull(aggregate);
        Assert.Equal("test:test", aggregate.StreamId);
        Assert.Equal(numbers, aggregate.Numbers);
        Assert.Equal(11, aggregate.Version);
    }

    private class TestAggregate : EventSourced, ISnapshotCapable {
        public override string StreamId => $"test:{Id}";
        public bool? SnapshotWhen => Version == 12;
        
        public string? Id { get; private set; }
        public List<int> Numbers { get; } = [];

        public TestAggregate() {
            RegisterHandler<ItemCreated>(When);
            RegisterHandler<ItemNumberAdded>(When);
        }
        
        public void Create(string id, IEnumerable<int> numbers) {
            if (Id != null) throw new InvalidOperationException("Duplicate Test Aggregate");
            Apply(new ItemCreated(id));
            
            foreach (var number in numbers) {
                Apply(new ItemNumberAdded(Id!, number));
            }
        }

        private void When(ItemCreated @event) {
            Id = @event.Id;
        }
        
        private void When(ItemNumberAdded @event) {
            Numbers.Add(@event.Number);
        }

        public class ItemCreated(string id) : EventBase {
            public string Id { get; } = id;
        }

        public class ItemNumberAdded(string id, int number) : EventBase {
            public string Id { get; } = id;
            public int Number { get; } = number;
        }
    }
}