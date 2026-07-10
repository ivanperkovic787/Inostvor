using PlasmaCAM.Kernel.Primitives;

namespace PlasmaCAM.Kernel.Spatial;

/// <summary>
/// Binarni AABB tree (BVH) s inkrementalnim umetanjem: silazak do najboljeg brata
/// minimizacijom porasta opsega (perimetar je za 2D bolja metrika od površine jer
/// ispravno tretira degenerirane, tanke kutije poput vodoravnih linija).
/// ODLUKA M1: bez rebalansirajućih rotacija — korektnost upita ne ovisi o balansu,
/// samo performanse; benchmark AabbTreeBenchmarks čuva prag. Rotacije se dodaju
/// tek ako mjerenja na stvarnim dokumentima pokažu degradaciju.
/// </summary>
public sealed class AabbTree<T> : ISpatialIndex<T>
{
    private sealed class Node
    {
        public Aabb Box;
        public T? Item;
        public Node? Left;
        public Node? Right;
        public Node? Parent;

        public bool IsLeaf => Left is null;
    }

    private Node? _root;

    public int Count { get; private set; }

    public void Insert(Aabb bounds, T item)
    {
        var leaf = new Node { Box = bounds, Item = item };
        Count++;

        if (_root is null)
        {
            _root = leaf;
            return;
        }

        // Silazak: u svakom čvoru odaberi dijete čiji opseg manje naraste dodavanjem kutije.
        var sibling = _root;
        while (!sibling.IsLeaf)
        {
            var left = sibling.Left!;
            var right = sibling.Right!;
            var costLeft = Aabb.Union(left.Box, bounds).Perimeter - left.Box.Perimeter;
            var costRight = Aabb.Union(right.Box, bounds).Perimeter - right.Box.Perimeter;
            sibling = costLeft <= costRight ? left : right;
        }

        var oldParent = sibling.Parent;
        var newParent = new Node
        {
            Box = Aabb.Union(sibling.Box, bounds),
            Left = sibling,
            Right = leaf,
            Parent = oldParent,
        };
        sibling.Parent = newParent;
        leaf.Parent = newParent;

        if (oldParent is null)
        {
            _root = newParent;
        }
        else if (ReferenceEquals(oldParent.Left, sibling))
        {
            oldParent.Left = newParent;
        }
        else
        {
            oldParent.Right = newParent;
        }

        // Refit AABB-ova prema korijenu.
        for (var node = oldParent; node is not null; node = node.Parent)
        {
            node.Box = Aabb.Union(node.Left!.Box, node.Right!.Box);
        }
    }

    public void Query(Aabb area, ICollection<T> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        if (_root is null)
        {
            return;
        }

        var stack = new Stack<Node>();
        stack.Push(_root);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (!node.Box.Intersects(area))
            {
                continue;
            }

            if (node.IsLeaf)
            {
                results.Add(node.Item!);
            }
            else
            {
                stack.Push(node.Left!);
                stack.Push(node.Right!);
            }
        }
    }

    public void Query(Point2 point, ICollection<T> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        if (_root is null)
        {
            return;
        }

        var stack = new Stack<Node>();
        stack.Push(_root);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (!node.Box.Contains(point, Tolerance.Geometric))
            {
                continue;
            }

            if (node.IsLeaf)
            {
                results.Add(node.Item!);
            }
            else
            {
                stack.Push(node.Left!);
                stack.Push(node.Right!);
            }
        }
    }

    public void Clear()
    {
        _root = null;
        Count = 0;
    }
}
