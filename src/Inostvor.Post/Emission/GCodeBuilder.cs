using System.Globalization;
using System.Text;
using Inostvor.Sdk.Post;

namespace Inostvor.Post.Emission;

/// <summary>
/// Sastavljanje redaka G-koda prema dijalektu: linijski brojevi, komentari,
/// invariant formatiranje brojeva (deterministički — golden testovi ovise o tome).
/// Redci se odvajaju sa "\n" (fiksno, neovisno o OS-u).
/// </summary>
public sealed class GCodeBuilder
{
    private readonly StringBuilder _text = new();
    private readonly GCodeDialect _dialect;
    private readonly string _numberFormat;
    private int _lineNumber;

    public GCodeBuilder(GCodeDialect dialect)
    {
        ArgumentNullException.ThrowIfNull(dialect);
        _dialect = dialect;
        _numberFormat = "0." + new string('#', Math.Clamp(dialect.Decimals, 0, 9));
        _lineNumber = dialect.LineNumberStart;
    }

    public void Line(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (_dialect.UseLineNumbers)
        {
            _text.Append(_dialect.LineNumberPrefix)
                 .Append(_lineNumber.ToString(CultureInfo.InvariantCulture))
                 .Append(' ');
            _lineNumber += _dialect.LineNumberStep;
        }

        _text.Append(content).Append('\n');
    }

    public void Comment(string text)
    {
        if (!_dialect.EmitComments)
        {
            return;
        }

        Line(_dialect.CommentEnd.Length > 0
            ? _dialect.CommentStart + text + _dialect.CommentEnd
            : _dialect.CommentStart + text);
    }

    public void BlankLine()
    {
        // Prazan red BEZ linijskog broja (čitljivost).
        _text.Append('\n');
    }

    /// <summary>Broj u invariant formatu dijalekta (npr. 10 → "10", 10.5 → "10.5").</summary>
    public string Number(double value)
    {
        // Izbjegni "-0" u izlazu (kvantizacija oko nule).
        var rounded = Math.Round(value, Math.Clamp(_dialect.Decimals, 0, 9));
        if (rounded == 0.0)
        {
            rounded = 0.0;
        }

        return rounded.ToString(_numberFormat, CultureInfo.InvariantCulture);
    }

    public override string ToString() => _text.ToString();
}
