using PlasmaCAM.Kernel.Primitives;
using PlasmaCAM.Kernel.Spatial;
using Shouldly;
using Xunit;

namespace PlasmaCAM.Kernel.Tests;

public sealed class AabbTreeTests
{
    [Fact]
    public void PraznoStablo_UpitVracaNista()
    {
        var tree = new AabbTree<int>();
        var results = new List<int>();

        tree.Query(new Aabb(0, 0, 10, 10), results);

        results.ShouldBeEmpty();
        tree.Count.ShouldBe(0);
    }

    [Fact]
    public void Insert_PovecavaCount_ClearResetira()
    {
        var tree = new AabbTree<string>();
        tree.Insert(new Aabb(0, 0, 1, 1), "a");
        tree.Insert(new Aabb(2, 2, 3, 3), "b");

        tree.Count.ShouldBe(2);

        tree.Clear();
        tree.Count.ShouldBe(0);

        var results = new List<string>();
        tree.Query(new Aabb(0, 0, 5, 5), results);
        results.ShouldBeEmpty();
    }

    [Fact]
    public void QueryAabb_VracaSamoPreklapajuce()
    {
        var tree = new AabbTree<string>();
        tree.Insert(new Aabb(0, 0, 1, 1), "lijevo-dolje");
        tree.Insert(new Aabb(5, 5, 6, 6), "desno-gore");
        tree.Insert(new Aabb(0.5, 0.5, 5.5, 5.5), "veliki");

        var results = new List<string>();
        tree.Query(new Aabb(0, 0, 2, 2), results);

        results.ShouldBe(["lijevo-dolje", "veliki"], ignoreOrder: true);
    }

    [Fact]
    public void QueryPoint_UkljucujeRubove()
    {
        var tree = new AabbTree<int>();
        tree.Insert(new Aabb(0, 0, 2, 2), 1);
        tree.Insert(new Aabb(2, 0, 4, 2), 2); // dijele rub x=2
        tree.Insert(new Aabb(10, 10, 11, 11), 3);

        var results = new List<int>();
        tree.Query(new Point2(2, 1), results);

        results.ShouldBe([1, 2], ignoreOrder: true);
    }

    [Fact]
    public void RandomiziranaUsporedba_SBruteForceReferencom()
    {
        // Deterministički seed: rezultati stabla moraju biti IDENTIČNI linearnom skenu.
        var rng = new Random(42);
        var boxes = new List<(Aabb Box, int Id)>();
        var tree = new AabbTree<int>();

        for (var i = 0; i < 300; i++)
        {
            var x = rng.NextDouble() * 100.0;
            var y = rng.NextDouble() * 100.0;
            var w = rng.NextDouble() * 10.0;
            var h = rng.NextDouble() * 10.0;
            var box = new Aabb(x, y, x + w, y + h);
            boxes.Add((box, i));
            tree.Insert(box, i);
        }

        for (var q = 0; q < 30; q++)
        {
            var x = rng.NextDouble() * 100.0;
            var y = rng.NextDouble() * 100.0;
            var area = new Aabb(x, y, x + (rng.NextDouble() * 20.0), y + (rng.NextDouble() * 20.0));

            var expected = boxes.Where(b => b.Box.Intersects(area)).Select(b => b.Id).OrderBy(i => i).ToList();

            var actual = new List<int>();
            tree.Query(area, actual);
            actual.Sort();

            actual.ShouldBe(expected);
        }
    }

    [Fact]
    public void DegeneriraneKutije_VodoravneLinije_RadeIspravno()
    {
        // Kutije nulte visine (vodoravni segmenti) — perimetar-heuristika ih mora podnijeti.
        var tree = new AabbTree<int>();
        for (var i = 0; i < 20; i++)
        {
            tree.Insert(new Aabb(0, i, 10, i), i);
        }

        var results = new List<int>();
        tree.Query(new Aabb(3, 4.5, 5, 7.5), results);

        results.ShouldBe([5, 6, 7], ignoreOrder: true);
    }
}
