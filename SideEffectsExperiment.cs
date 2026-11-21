using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

namespace IndependentWork12;

public static class SideEffectsExperiment
{
    private const int Count = 1_000_000; 
    private const int Runs = 5;          

    public static void Run()
    {
        var data = Enumerable.Range(0, Count).ToList();

        Console.WriteLine($"Очікуване значення лічильника в кожному запуску: {Format(Count)}");
        Console.WriteLine();

        DemoUnsafeCounter(data);
        DemoUnsafeList(data);
        DemoFixes(data);
    }


    private static void DemoUnsafeCounter(List<int> data)
    {
        Console.WriteLine("--- Проблема 1: counter++ у лямбді PLINQ без синхронізації ---");

        for (int run = 1; run <= Runs; run++)
        {
            int counter = 0;

            data.AsParallel()
                .Select(x =>
                {
                    counter++;
                    return x;
                })
                .ToList();

            string status = counter == Count ? "випадково збіглося" : $"втрачено {Format(Count - counter)} збільшень";
            Console.WriteLine($"  Запуск {run}: counter = {Format(counter)} ({status})");
        }
    }


    private static void DemoUnsafeList(List<int> data)
    {
        Console.WriteLine();
        Console.WriteLine("--- Проблема 2: List<int>.Add з кількох потоків без синхронізації ---");

        for (int run = 1; run <= Runs; run++)
        {
            var shared = new List<int>();

            try
            {
                data.AsParallel().ForAll(x => shared.Add(x));
                Console.WriteLine($"  Запуск {run}: у списку {Format(shared.Count)} елементів із {Format(Count)}");
            }
            catch (Exception ex)
            {
                string inner = ex.InnerException?.GetType().Name ?? "-";
                Console.WriteLine($"  Запуск {run}: виняток {ex.GetType().Name} (внутрішній: {inner})");
            }
        }
    }


    private static void DemoFixes(List<int> data)
    {
        Console.WriteLine();
        Console.WriteLine("--- Виправлення: потокобезпечні варіанти ---");

        var sync = new object();
        int lockedCounter = 0;
        var watch = Stopwatch.StartNew();
        data.AsParallel()
            .Select(x =>
            {
                lock (sync)
                {
                    lockedCounter++;
                }
                return x;
            })
            .ToList();
        watch.Stop();
        PrintFix("lock", lockedCounter, watch.Elapsed.TotalMilliseconds);

        int interlockedCounter = 0;
        watch = Stopwatch.StartNew();
        data.AsParallel()
            .Select(x =>
            {
                Interlocked.Increment(ref interlockedCounter);
                return x;
            })
            .ToList();
        watch.Stop();
        PrintFix("Interlocked.Increment", interlockedCounter, watch.Elapsed.TotalMilliseconds);

        var bag = new ConcurrentBag<int>();
        watch = Stopwatch.StartNew();
        data.AsParallel().ForAll(x => bag.Add(x));
        watch.Stop();
        PrintFix("ConcurrentBag<int>", bag.Count, watch.Elapsed.TotalMilliseconds);

        watch = Stopwatch.StartNew();
        int counted = data.AsParallel().Select(x => x).Count();
        watch.Stop();
        PrintFix("Count() без спільного стану", counted, watch.Elapsed.TotalMilliseconds);
    }

    private static void PrintFix(string name, int result, double ms)
    {
        string verdict = result == Count ? "коректно" : "ПОМИЛКА";
        Console.WriteLine(
            $"  {name,-30} результат = {Format(result)} ({verdict}), " +
            $"час {ms.ToString("F1", CultureInfo.InvariantCulture)} мс");
    }

    private static string Format(int number)
    {
        return number.ToString("N0", CultureInfo.InvariantCulture);
    }
}
