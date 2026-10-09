using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text.Json;

namespace DecisionTreeGenerator.Data;

public class DataPool
{
    public List<Element> TrainingData = [];
    public List<Element> TestData = [];

    public void LoadCSV(
        string filename,
        ElementSchema elementSchema,
        bool testData = false
    )
    {
        using var reader = new StreamReader(filename);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true,
            PrepareHeaderForMatch = args => args.Header.Trim(),
            HeaderValidated = null,
            MissingFieldFound = null
        };

        using var csv = new CsvReader(reader, config);

        if (!csv.Read())
            throw new FormatException("CSV file is empty.");

        csv.ReadHeader();

        string[] header = csv.HeaderRecord
            ?? throw new FormatException("CSV header is missing.");

        if (header.Count(h =>
                h.Equals("Label", StringComparison.OrdinalIgnoreCase)) != 1)
        {
            throw new FormatException(
                "CSV must contain exactly one Label column.");
        }

        if (header.Distinct(StringComparer.OrdinalIgnoreCase).Count()
            != header.Length)
        {
            throw new FormatException(
                "CSV contains duplicate column names.");
        }

        string[] csvAttributes = header
            .Where(h => !h.Equals(
                "Label", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        string[] schemaAttributes = elementSchema.Attributes
            .Select(a => a.Name)
            .ToArray();

        if (csvAttributes.Length != schemaAttributes.Length ||
            !csvAttributes.ToHashSet(StringComparer.OrdinalIgnoreCase)
                .SetEquals(schemaAttributes))
        {
            throw new FormatException(
                "CSV columns do not match the element schema.");
        }

        int labelIndex = Array.FindIndex(
            header,
            h => h.Equals("Label", StringComparison.OrdinalIgnoreCase));

        List<Element> elements = [];

        while (csv.Read())
        {
            int line = csv.Parser.Row;

            string label = csv.GetField(labelIndex)?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(label))
            {
                throw new FormatException(
                    $"Missing label in CSV line {line}.");
            }

            Element element = new(label, []);

            for (int i = 0; i < header.Length; i++)
            {
                if (i == labelIndex)
                    continue;

                string attributeName = header[i];
                string value = csv.GetField(i)?.Trim() ?? "";

                DataAttribute attribute = elementSchema.Attributes
                    .Single(a => a.Name.Equals(
                        attributeName,
                        StringComparison.OrdinalIgnoreCase));

                Outcome? outcome = attribute.Outcomes.FirstOrDefault(o =>
                    o.Name.Equals(
                        value,
                        StringComparison.OrdinalIgnoreCase));

                if (outcome is null)
                {
                    throw new FormatException(
                        $"Unknown outcome '{value}' for attribute " +
                        $"'{attributeName}' in CSV line {line}.");
                }

                element.Attributes.Add(new(attribute.Name, outcome));
            }

            elements.Add(element);
        }

        if (testData)
            TestData.AddRange(elements);
        else
            TrainingData.AddRange(elements);
    }

    public void LoadJSON(
        string filename,
        ElementSchema elementSchema,
        bool testData = false
    )
    {
        using FileStream stream = File.OpenRead(filename);
        using JsonDocument document = JsonDocument.Parse(stream);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new FormatException("JSON data must be an array.");

        string[] schemaAttributes = elementSchema.Attributes
            .Select(a => a.Name)
            .ToArray();

        List<Element> elements = [];

        int index = 0;

        foreach (JsonElement item in document.RootElement.EnumerateArray())
        {
            index++;

            if (item.ValueKind != JsonValueKind.Object)
                throw new FormatException(
                    $"Element at index {index} must be an object.");

            Dictionary<string, JsonElement> properties =
                new(StringComparer.OrdinalIgnoreCase);

            foreach (JsonProperty property in item.EnumerateObject())
            {
                if (!properties.TryAdd(property.Name, property.Value))
                    throw new FormatException(
                        $"Duplicate property '{property.Name}' at index {index}.");
            }

            if (!properties.TryGetValue("Label", out JsonElement labelElement) ||
                labelElement.ValueKind != JsonValueKind.String)
            {
                throw new FormatException(
                    $"Missing or invalid Label at index {index}.");
            }

            string label = labelElement.GetString()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(label))
                throw new FormatException(
                    $"Empty Label at index {index}.");

            string[] jsonAttributes = properties.Keys
                .Where(name => !name.Equals(
                    "Label", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (jsonAttributes.Length != schemaAttributes.Length ||
                !jsonAttributes.ToHashSet(StringComparer.OrdinalIgnoreCase)
                    .SetEquals(schemaAttributes))
            {
                throw new FormatException(
                    $"Attributes at index {index} do not match the schema.");
            }

            Element element = new(label, []);

            foreach (DataAttribute attribute in elementSchema.Attributes)
            {
                JsonElement valueElement = properties[attribute.Name];

                if (valueElement.ValueKind != JsonValueKind.String)
                    throw new FormatException(
                        $"Attribute '{attribute.Name}' at index {index} " +
                        "must be a string.");

                string value = valueElement.GetString()?.Trim() ?? "";

                Outcome? outcome = attribute.Outcomes.FirstOrDefault(o =>
                    o.Name.Equals(
                        value,
                        StringComparison.OrdinalIgnoreCase));

                if (outcome is null)
                    throw new FormatException(
                        $"Unknown outcome '{value}' for attribute " +
                        $"'{attribute.Name}' at index {index}.");

                element.Attributes.Add(new(attribute.Name, outcome));
            }

            elements.Add(element);
        }

        if (testData)
            TestData.AddRange(elements);
        else
            TrainingData.AddRange(elements);
    }
}