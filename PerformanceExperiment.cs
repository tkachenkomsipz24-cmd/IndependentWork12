using System.Diagnostics;
using System.Globalization;

namespace IndependentWork12;

/// <summary>
/// Порівняння продуктивності LINQ та PLINQ на різних обсягах даних.
/// </summary>
public static class PerformanceExperiment
{
    private const int Repeats = 3; // кількість вимірювань для усереднення

    public static void Run(int[] sizes)
    {
        Console.WriteLine($"Логічних ядер процесора: {Environment.ProcessorCount}");
        Console.WriteLine($"Кожне вимірювання усереднюється за {Repeats} запуски (після прогріву).");

        // Прогрів: JIT-компіляція обох варіантів на малій колекції, щоб не псувати перші вимірювання
        var warmUp = DataGenerator.GenerateNumbers(10_000);
        RunLinq(warmUp);
        RunPlinq(warmUp);
        RunCheapLinq(warmUp);
        RunCheapPlinq(warmUp);

        Console.WriteLine();
        Console.WriteLine("--- Експеримент 1: обчислювально інтенсивна операція (HeavyCompute) ---");
        Console.WriteLine($"{"Елементів",12} | {"LINQ, мс",12} | {"PLINQ, мс",12} | {"Прискорення",11} | Результати");

        foreach (int size in sizes)
        {
            var data = DataGenerator.GenerateNumbers(size);

            var (linqMs, linqResult) = Measure(() => RunLinq(data));
            var (plinqMs, plinqResult) = Measure(() => RunPlinq(data));

            double speedup = linqMs / plinqMs;
            bool match = ResultsMatch(linqResult, plinqResult);

            Console.WriteLine(
                $"{Format(size),12} | {linqMs.ToString("F1", CultureInfo.InvariantCulture),12} | " +
                $"{plinqMs.ToString("F1", CultureInfo.InvariantCulture),12} | " +
                $"{speedup.ToString("F2", CultureInfo.InvariantCulture) + "x",11} | " +
                (match ? "збігаються" : "РІЗНІ!"));
        }

        Console.WriteLine();
        Console.WriteLine("--- Експеримент 2 (додатковий): проста операція x * 2 ---");
        Console.WriteLine($"{"Елементів",12} | {"LINQ, мс",12} | {"PLINQ, мс",12} | {"Прискорення",11}");

        foreach (int size in sizes)
        {
            var data = DataGenerator.GenerateNumbers(size);

            var (linqMs, _) = Measure(() => RunCheapLinq(data));
            var (plinqMs, _) = Measure(() => RunCheapPlinq(data));

            double speedup = linqMs / plinqMs;

            Console.WriteLine(
                $"{Format(size),12} | {linqMs.ToString("F1", CultureInfo.InvariantCulture),12} | " +
                $"{plinqMs.ToString("F1", CultureInfo.InvariantCulture),12} | " +
                $"{speedup.ToString("F2", CultureInfo.InvariantCulture) + "x",11}");
        }
    }

    // ---------- Інтенсивна операція ----------

    /// <summary>
    /// Обчислювально інтенсивна чиста функція (без побічних ефектів):
    /// 20 ітерацій з квадратним коренем, синусом і косинусом.
    /// </summary>
    private static double HeavyCompute(int x)
    {
        double result = x;

        for (int i = 0; i < 20; i++)
        {
            result = Math.Sqrt(Math.Abs(Math.Sin(result) * 1000.0 + i)) + Math.Cos(i);
        }

        return result;
    }

    private static List<double> RunLinq(List<int> data)
    {
        return data
            .Where(x => x % 2 == 0)
            .Select(x => HeavyCompute(x))
            .ToList();
    }

    private static List<double> RunPlinq(List<int> data)
    {
        return data
            .AsParallel()
            .Where(x => x % 2 == 0)
            .Select(x => HeavyCompute(x))
            .ToList();
    }

    // ---------- Проста операція (для порівняння) ----------

    private static List<int> RunCheapLinq(List<int> data)
    {
        return data.Where(x => x % 2 == 0).Select(x => x * 2).ToList();
    }

    private static List<int> RunCheapPlinq(List<int> data)
    {
        return data.AsParallel().Where(x => x % 2 == 0).Select(x => x * 2).ToList();
    }

    // ---------- Допоміжні методи ----------

    /// <summary>
    /// Вимірює середній час запиту. Для кожного вимірювання створюється новий Stopwatch.
    /// </summary>
    private static (double AvgMs, T Result) Measure<T>(Func<T> query) where T : new()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();

        T last = new T();
        double total = 0;

        for (int i = 0; i < Repeats; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            last = query();
            stopwatch.Stop();
            total += stopwatch.Elapsed.TotalMilliseconds;
        }

        return (total / Repeats, last);
    }

    /// <summary>
    /// PLINQ без AsOrdered не гарантує порядок, тому порівнюємо кількість і суму
    /// (з невеликою похибкою через різний порядок додавання double).
    /// </summary>
    private static bool ResultsMatch(List<double> linq, List<double> plinq)
    {
        if (linq.Count != plinq.Count)
        {
            return false;
        }

        double sumLinq = linq.Sum();
        double sumPlinq = plinq.Sum();
        return Math.Abs(sumLinq - sumPlinq) <= 1e-6 * Math.Abs(sumLinq);
    }

    private static string Format(int number)
    {
        return number.ToString("N0", CultureInfo.InvariantCulture);
    }
}
