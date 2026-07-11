using Inostvor.Core.Model.Import;

namespace Inostvor.Import.NetDxf;

/// <summary>Sakuplja upozorenja s gornjom granicom — patološke datoteke ne smiju zatrpati memoriju.</summary>
internal sealed class WarningCollector
{
    private readonly List<ImportWarning> _items;
    private readonly int _max;
    private bool _limitReported;

    public WarningCollector(int max)
    {
        _max = max;
        _items = new List<ImportWarning>(Math.Min(max, 64));
    }

    public IReadOnlyList<ImportWarning> Items => _items;

    public void Add(string code, string message, string? handle = null)
    {
        if (_items.Count >= _max)
        {
            if (!_limitReported)
            {
                _items.Add(new ImportWarning(
                    ImportWarningCodes.WarningLimitReached,
                    FormattableString.Invariant($"Dosegnuta granica od {_max} upozorenja — daljnja se ne bilježe.")));
                _limitReported = true;
            }

            return;
        }

        _items.Add(new ImportWarning(code, message, handle));
    }
}
