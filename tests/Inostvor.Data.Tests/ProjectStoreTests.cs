using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inostvor.Core.Model.Machines;
using Inostvor.Core.Model.Project;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Data.Project;
using Shouldly;
using Xunit;

namespace Inostvor.Data.Tests;

public sealed class ProjectStoreTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("inostvor_test_").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string WriteFakeDxf(string name, string content = "0\nSECTION\n0\nEOF\n")
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    private static ProjectDocument Document(string dxfPath) => new()
    {
        Name = "Testni projekt",
        DxfSources = [ProjectDxfSource.Create(Path.GetFileName(dxfPath), dxfPath, CacheKey.HashFile(dxfPath))],
        Technology = TechnologySettings.Default with { KerfWidth = 1.7, CutOrderStrategyId = "nearest-neighbor" },
        Machine = new MachineProfile
        {
            Name = "EC300 Plasma (900x1200)",
            PostProcessorId = "inostvor.post.ec300",
            Manufacturer = "DIY",
            HasThc = false,
            TableWidth = 900,
            TableHeight = 1200,
        },
        SimulationTimeSeconds = 12.5,
        SimulationSpeed = 2.0,
    };

    [Fact]
    public async Task RoundTrip_DokumentIDxfBajtoviOcuvani()
    {
        var dxf = WriteFakeDxf("ploca.dxf", "ORIGINALNI DXF SADRZAJ 123");
        var path = Path.Combine(_dir, "projekt.ino");
        var store = new ProjectStore([]);

        await store.SaveAsync(Document(dxf), path);
        var loaded = await store.LoadAsync(path);

        loaded.FormatVersion.ShouldBe(1);
        var doc = loaded.Document;
        doc.Name.ShouldBe("Testni projekt");
        doc.Technology.KerfWidth.ShouldBe(1.7);
        doc.Technology.CutOrderStrategyId.ShouldBe("nearest-neighbor");
        doc.Machine.Name.ShouldBe("EC300 Plasma (900x1200)");
        doc.Machine.Manufacturer.ShouldBe("DIY");
        doc.SimulationTimeSeconds.ShouldBe(12.5);

        var extracted = doc.DxfSources.ShouldHaveSingleItem();
        File.ReadAllText(extracted.SourcePath).ShouldBe("ORIGINALNI DXF SADRZAJ 123");
    }

    [Fact]
    public async Task NepoznateExtensions_OcuvaneURoundTripu()
    {
        // Sekcija budućeg modula (nesting) — ova verzija je NE razumije, ali je NE SMIJE izgubiti.
        var dxf = WriteFakeDxf("a.dxf");
        var nesting = JsonSerializer.Deserialize<JsonElement>("""{"sheets":[{"w":1500,"h":3000}],"algo":"rect"}""");
        var doc = Document(dxf) with
        {
            Extensions = new Dictionary<string, JsonElement>(StringComparer.Ordinal) { ["nesting"] = nesting },
        };
        var path = Path.Combine(_dir, "p.ino");
        var store = new ProjectStore([]);

        await store.SaveAsync(doc, path);
        var loaded = await store.LoadAsync(path);

        var preserved = loaded.Document.Extensions.ShouldContainKey("nesting");
        loaded.Document.Extensions["nesting"].GetProperty("algo").GetString().ShouldBe("rect");
        loaded.Document.Extensions["nesting"].GetProperty("sheets")[0].GetProperty("w").GetDouble().ShouldBe(1500);
    }

    [Fact]
    public async Task NovijiFormat_JasnaGreska()
    {
        var path = Path.Combine(_dir, "buducnost.ino");
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            await using (var w = new StreamWriter(zip.CreateEntry("manifest.json").Open()))
            {
                await w.WriteAsync("""{ "formatVersion": 99 }""");
            }

            await using (var w = new StreamWriter(zip.CreateEntry("project.json").Open()))
            {
                await w.WriteAsync("{}");
            }
        }

        var ex = await Should.ThrowAsync<InvalidDataException>(() => new ProjectStore([]).LoadAsync(path));
        ex.Message.ShouldContain("v99");
    }

    [Fact]
    public async Task Migracija_StarijiFormat_ProlaziKrozLanac()
    {
        // Ručno složen "v0" projekt: polje se zvalo "title" umjesto "Name".
        var path = Path.Combine(_dir, "stari.ino");
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            await using (var w = new StreamWriter(zip.CreateEntry("manifest.json").Open()))
            {
                await w.WriteAsync("""{ "formatVersion": 0 }""");
            }

            await using (var w = new StreamWriter(zip.CreateEntry("project.json").Open()))
            {
                await w.WriteAsync("""{ "title": "Stari projekt" }""");
            }
        }

        var store = new ProjectStore([new RenameTitleMigration()]);
        var loaded = await store.LoadAsync(path);

        loaded.Document.Name.ShouldBe("Stari projekt");
        loaded.FormatVersion.ShouldBe(1);
    }

    [Fact]
    public async Task Migracija_Nedostaje_JasnaGreska()
    {
        var path = Path.Combine(_dir, "stari2.ino");
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            await using (var w = new StreamWriter(zip.CreateEntry("manifest.json").Open()))
            {
                await w.WriteAsync("""{ "formatVersion": 0 }""");
            }

            await using (var w = new StreamWriter(zip.CreateEntry("project.json").Open()))
            {
                await w.WriteAsync("{}");
            }
        }

        var ex = await Should.ThrowAsync<InvalidDataException>(() => new ProjectStore([]).LoadAsync(path));
        ex.Message.ShouldContain("v0");
    }

    [Fact]
    public async Task Spremanje_JeAtomarno_TmpNeOstaje()
    {
        var dxf = WriteFakeDxf("b.dxf");
        var path = Path.Combine(_dir, "atomarno.ino");

        await new ProjectStore([]).SaveAsync(Document(dxf), path);

        File.Exists(path).ShouldBeTrue();
        File.Exists(path + ".tmp").ShouldBeFalse();
    }

    private sealed class RenameTitleMigration : IProjectMigration
    {
        public int FromVersion => 0;

        public JsonNode Migrate(JsonNode projectJson)
        {
            var obj = projectJson.AsObject();
            if (obj.TryGetPropertyValue("title", out var title))
            {
                obj.Remove("title");
                obj["Name"] = title?.GetValue<string>() ?? "Bez imena";
            }

            return obj;
        }
    }
}
