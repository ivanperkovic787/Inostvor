namespace PlasmaCAM.Import.NetDxf.Tests;

/// <summary>Pronalazi tests/TestData penjanjem od build output direktorija do korijena repozitorija (PlasmaCAM.sln).</summary>
internal static class TestDataLocator
{
    private static readonly Lazy<string> Root = new(() =>
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PlasmaCAM.sln")))
            {
                return Path.Combine(dir.FullName, "tests", "TestData");
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Korijen repozitorija (PlasmaCAM.sln) nije pronađen iznad " + AppContext.BaseDirectory);
    });

    public static string Get(string category, string fileName) => Path.Combine(Root.Value, category, fileName);
}
