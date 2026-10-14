namespace DecisionTreeGenerator.Data;

public class ConfusionMatrix(List<Outcome> outcomes, int classCount)
{
    public List<Outcome> Outcomes = outcomes;
    public int[,] matrix = new int[classCount, classCount];

    public void Print()
    {
        int labelWidth = Math.Max(
            "↓ Actual".Length,
            Outcomes.Max(o => o.Name.Length)
        );

        int cellWidth = Math.Max(
            Outcomes.Max(o => o.Name.Length),
            matrix.Cast<int>().Max().ToString().Length
        ) + 2;

        Console.Write("↓ Actual".PadRight(labelWidth + 2));
        Console.Write("Prediction".PadLeft(cellWidth * Outcomes.Count / 2
            + "Prediction".Length / 2 + 1));

        Console.WriteLine();

        Console.Write("".PadRight(labelWidth + 2));
        foreach (Outcome outcome in Outcomes)
            Console.Write(outcome.Name.PadLeft(cellWidth));

        Console.WriteLine();
        Console.WriteLine(new string('-', labelWidth + 2 + cellWidth * Outcomes.Count));

        for (int row = 0; row < Outcomes.Count; row++)
        {
            Console.Write(Outcomes[row].Name.PadRight(labelWidth));
            Console.Write("  ");

            for (int col = 0; col < Outcomes.Count; col++)
                Console.Write(matrix[row, col].ToString().PadLeft(cellWidth));

            Console.WriteLine();
        }
    }

    public void Export(string filename)
    {
        string extension = Path.GetExtension(filename);

        switch (extension.ToLowerInvariant())
        {
            case ".md":
                ExportMarkdown(filename);
                break;

            case ".csv":
                ExportCSV(filename);
                break;

            default:
                throw new FormatException(
                    $"Unsupported confusion matrix export format: '{extension}'. " +
                    "Allowed formats are .md and .csv."
                );
        }
    }

    private void ExportMarkdown(string filename)
    {
        using StreamWriter writer = new(filename);

        writer.WriteLine("# Confusion Matrix");
        writer.WriteLine();
        writer.Write("| Actual \\ Predicted |");

        foreach (Outcome outcome in Outcomes)
            writer.Write($" {outcome.Name} |");

        writer.WriteLine();

        writer.Write("|---|");
        foreach (Outcome _ in Outcomes)
            writer.Write("---:|");

        writer.WriteLine();

        for (int row = 0; row < Outcomes.Count; row++)
        {
            writer.Write($"| {Outcomes[row].Name} |");

            for (int col = 0; col < Outcomes.Count; col++)
                writer.Write($" {matrix[row, col]} |");

            writer.WriteLine();
        }
    }

    private void ExportCSV(string filename)
    {
        using StreamWriter writer = new(filename);

        writer.Write("Actual \\ Predicted");

        foreach (Outcome outcome in Outcomes)
            writer.Write($";{EscapeCsv(outcome.Name)}");

        writer.WriteLine();

        for (int row = 0; row < Outcomes.Count; row++)
        {
            writer.Write(EscapeCsv(Outcomes[row].Name));

            for (int col = 0; col < Outcomes.Count; col++)
                writer.Write($";{matrix[row, col]}");

            writer.WriteLine();
        }
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(';') || value.Contains('"') ||
            value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}