namespace IndependentWork12;

/// <summary>
/// Генерація великої колекції випадкових чисел із фіксованим Seed,
/// щоб експерименти можна було відтворити.
/// </summary>
public static class DataGenerator
{
    public const int Seed = 42;

    public static List<int> GenerateNumbers(int count)
    {
        var rnd = new Random(Seed);
        var numbers = new List<int>(count);

        for (int i = 0; i < count; i++)
        {
            numbers.Add(rnd.Next(1, 1_000_000));
        }

        return numbers;
    }
}
