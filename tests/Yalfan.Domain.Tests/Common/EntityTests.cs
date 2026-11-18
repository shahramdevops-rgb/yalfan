namespace Yalfan.Domain.Tests.Common;

public class EntityTests
{
    private sealed class Bag : Entity
    {
        public Bag()
        {
        }

        public Bag(Guid id)
            : base(id)
        {
        }
    }

    private sealed class Strap : Entity
    {
        public Strap(Guid id)
            : base(id)
        {
        }
    }

    [Fact]
    public void A_new_entity_gets_its_own_id()
    {
        var first = new Bag();
        var second = new Bag();

        Assert.NotEqual(Guid.Empty, first.Id);
        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void New_ids_are_time_ordered()
    {
        Assert.Equal(7, new Bag().Id.Version);
    }

    [Fact]
    public void Entities_with_the_same_type_and_id_are_equal()
    {
        var id = Guid.CreateVersion7();

        var loadedOnce = new Bag(id);
        var loadedAgain = new Bag(id);

        Assert.Equal(loadedOnce, loadedAgain);
        Assert.True(loadedOnce == loadedAgain);
        Assert.Equal(loadedOnce.GetHashCode(), loadedAgain.GetHashCode());
    }

    [Fact]
    public void Different_types_with_the_same_id_are_not_equal()
    {
        var id = Guid.CreateVersion7();

        Assert.False(new Bag(id).Equals(new Strap(id)));
    }

    [Fact]
    public void An_entity_is_never_equal_to_null()
    {
        var bag = new Bag();

        Assert.False(bag.Equals(null));
        Assert.True(bag != null);
    }

    [Fact]
    public void An_empty_id_is_refused()
    {
        Assert.Throws<ArgumentException>(() => new Bag(Guid.Empty));
    }
}
