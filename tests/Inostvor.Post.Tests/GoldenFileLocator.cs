namespace Inostvor.Post.Tests;

internal static class GoldenFileLocator
{
    public static string Read(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Inostvor.sln")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException("Korijen repozitorija nije pronađen.");
        }

        var path = Path.Combine(dir.FullName, "tests", "Inostvor.Post.Tests", "GoldenFiles", fileName);
        return File.ReadAllText(path);
    }
}
