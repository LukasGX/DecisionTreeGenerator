using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text.Json;

namespace DecisionTreeGenerator.Data;

public class ElementSchema(List<DataAttribute> attributes)
{
    public List<DataAttribute> Attributes = attributes;

    public bool Validate(Element element)
    {
        foreach (DataAttribute attr in Attributes)
        {
            SetAttribute? matching = element.Attributes
                .FirstOrDefault(a => a.Name == attr.Name);

            if (matching == null)
                return false;

            if (!attr.Outcomes.Any(o => o.Name == matching.Outcome.Name))
                return false;
        }

        return true;
    }

    public bool ValidateList(List<Element> elements)
    {
        bool valid = true;

        foreach (Element el in elements)
        {
            if (Validate(el) == false)
                valid = false;
        }

        return valid;
    }

    public static ElementSchema LoadCSV(string filename)
    {
        using var reader = new StreamReader(filename);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = false,
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true
        };

        using var csv = new CsvReader(reader, config);

        List<DataAttribute> attributes = [];
        HashSet<string> attributeNames =
            new(StringComparer.OrdinalIgnoreCase);

        while (csv.Read())
        {
            int line = csv.Parser.Row;

            string attributeName = csv.GetField(0)?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(attributeName))
                throw new FormatException(
                    $"Missing attribute name in line {line}.");

            if (!attributeNames.Add(attributeName))
                throw new FormatException(
                    $"Duplicate attribute '{attributeName}' in line {line}.");

            List<Outcome> outcomes = [];
            HashSet<string> outcomeNames =
                new(StringComparer.OrdinalIgnoreCase);

            for (int i = 1; i < csv.Parser.Count; i++)
            {
                string value = csv.GetField(i)?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(value))
                    throw new FormatException(
                        $"Empty outcome for '{attributeName}' in line {line}.");

                if (!outcomeNames.Add(value))
                    throw new FormatException(
                        $"Duplicate outcome '{value}' for '{attributeName}' " +
                        $"in line {line}.");

                outcomes.Add(new Outcome(value));
            }

            if (outcomes.Count == 0)
                throw new FormatException(
                    $"Attribute '{attributeName}' has no outcomes in line {line}.");

            attributes.Add(new DataAttribute(attributeName, outcomes));
        }

        if (attributes.Count == 0)
            throw new FormatException("Schema file is empty.");

        return new ElementSchema(attributes);
    }

    public static ElementSchema LoadJSON(string filename)
    {
        using FileStream stream = File.OpenRead(filename);

        using JsonDocument document = JsonDocument.Parse(stream);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new FormatException("Schema JSON must be an array.");

        List<DataAttribute> attributes = [];
        HashSet<string> attributeNames =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (JsonElement item in document.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                throw new FormatException(
                    "Each schema entry must be an object.");

            if (!item.TryGetProperty("name", out JsonElement nameElement) ||
                nameElement.ValueKind != JsonValueKind.String)
            {
                throw new FormatException(
                    "Each schema entry must have a string property 'name'.");
            }

            string name = nameElement.GetString()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
                throw new FormatException("Attribute name cannot be empty.");

            if (!attributeNames.Add(name))
                throw new FormatException(
                    $"Duplicate attribute '{name}'.");

            if (!item.TryGetProperty("outcomes", out JsonElement outcomesElement) ||
                outcomesElement.ValueKind != JsonValueKind.Array)
            {
                throw new FormatException(
                    $"Attribute '{name}' must have an array property 'outcomes'.");
            }

            List<Outcome> outcomes = [];
            HashSet<string> outcomeNames =
                new(StringComparer.OrdinalIgnoreCase);

            foreach (JsonElement outcomeElement in outcomesElement.EnumerateArray())
            {
                if (outcomeElement.ValueKind != JsonValueKind.String)
                    throw new FormatException(
                        $"Outcomes for '{name}' must be strings.");

                string value = outcomeElement.GetString()?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(value))
                    throw new FormatException(
                        $"Empty outcome for attribute '{name}'.");

                if (!outcomeNames.Add(value))
                    throw new FormatException(
                        $"Duplicate outcome '{value}' for attribute '{name}'.");

                outcomes.Add(new Outcome(value));
            }

            if (outcomes.Count == 0)
                throw new FormatException(
                    $"Attribute '{name}' must have at least one outcome.");

            attributes.Add(new DataAttribute(name, outcomes));
        }

        if (attributes.Count == 0)
            throw new FormatException("Schema JSON is empty.");

        return new ElementSchema(attributes);
    }
}