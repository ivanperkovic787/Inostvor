using BenchmarkDotNet.Running;

namespace Inostvor.Benchmarks;

/// <summary>
/// Ulazna točka benchmark projekta. BenchmarkSwitcher automatski pronalazi sve
/// klase s [Benchmark] metodama u ovom assemblyju — svaki modul dodaje svoje.
/// Pokretanje: dotnet run -c Release --project benchmarks/Inostvor.Benchmarks
/// </summary>
public static class Program
{
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
