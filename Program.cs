using System.Text;

namespace IndependentWork12;

public static class Program
{
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        Console.WriteLine("=== Самостійна робота №18: PLINQ, продуктивність та безпека ===");
        Console.WriteLine();

        Console.WriteLine("##### Частина 1. Порівняння продуктивності LINQ і PLINQ #####");
        PerformanceExperiment.Run(new[] { 1_000_000, 5_000_000, 10_000_000 });

        Console.WriteLine();
        Console.WriteLine("##### Частина 2. Побічні ефекти та потокобезпека #####");
        SideEffectsExperiment.Run();
    }
}
