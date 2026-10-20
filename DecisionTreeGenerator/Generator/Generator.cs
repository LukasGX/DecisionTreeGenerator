using System.Linq.Expressions;
using DecisionTreeGenerator.Data;
using DecisionTreeGenerator.Nodes;
using DecisionTreeGenerator.TreeNS;

namespace DecisionTreeGenerator;

public static class TreeGenerator
{
    public static Tree GenerateTree(
        ElementSchema elementSchema,
        DataPool dataPool,
        bool verbose = false
    )
    {
        if (!elementSchema.ValidateList(dataPool.TrainingData))
            throw new Exception("Training data is invalid.");

        if (!elementSchema.ValidateList(dataPool.TestData))
            throw new Exception("Test data is invalid.");

        Node rootNode = BuildTree(elementSchema, dataPool.TrainingData, elementSchema.Attributes, verbose, true);

        return new Tree(rootNode);
    }

    private static Node BuildTree(
        ElementSchema elementSchema,
        List<Element> elements,
        List<DataAttribute> availableAttributes,
        bool verbose = false,
        bool first = false
    )
    {
        string[] labels = elements
            .Select(e => e.Label)
            .Distinct()
            .ToArray();

        if (labels.Length == 1)
            return new LeafNode(new(labels[0]));

        (DataAttribute? bestSplit,
        double informationGain,
        List<DataAttribute> availableAttributesNew,
        bool bestIsNumeric,
        double bestTreshold) =
            GetBestSplit(elements, availableAttributes, verbose, first);

        if (bestSplit is null || informationGain <= 0)
        {
            string majorityLabel = elements
                .GroupBy(e => e.Label)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;

            return new LeafNode(new(majorityLabel));
        }

        Decision decision = new(bestSplit, bestIsNumeric)
        {
            PossibleOutcomes = bestSplit.Outcomes,
            Treshold = bestTreshold
        };

        if (bestIsNumeric)
        {
            List<Element> lowerSubset = [.. elements.Where(e =>
                e.Attributes.Any(a =>
                    a.Name == bestSplit.Name &&
                    double.Parse(a.Outcome.Name) <= bestTreshold))];

            List<Element> upperSubset = [.. elements.Where(e =>
                e.Attributes.Any(a =>
                    a.Name == bestSplit.Name &&
                    double.Parse(a.Outcome.Name) > bestTreshold))];

            Outcome lowerOutcome = new($"<= {bestTreshold}");
            Outcome upperOutcome = new($"> {bestTreshold}");

            decision.PossibleOutcomes =
            [
                lowerOutcome,
                upperOutcome
            ];

            decision.FollowingNodes[lowerOutcome] =
                lowerSubset.Count == 0
                    ? new LeafNode(new(elements
                        .GroupBy(e => e.Label)
                        .OrderByDescending(g => g.Count())
                        .First().Key))
                    : BuildTree(
                        elementSchema,
                        lowerSubset,
                        availableAttributesNew,
                        verbose);

            decision.FollowingNodes[upperOutcome] =
                upperSubset.Count == 0
                    ? new LeafNode(new(elements
                        .GroupBy(e => e.Label)
                        .OrderByDescending(g => g.Count())
                        .First().Key))
                    : BuildTree(
                        elementSchema,
                        upperSubset,
                        availableAttributesNew,
                        verbose);
        }
        else
        {
            foreach (Outcome outcome in bestSplit.Outcomes)
            {
                List<Element> subset = [.. elements.Where(e =>
                    e.Attributes.Any(a =>
                        a.Name == bestSplit.Name &&
                        a.Outcome.Name == outcome.Name))];

                if (subset.Count == 0)
                {
                    string majorityLabel = elements
                        .GroupBy(e => e.Label)
                        .OrderByDescending(g => g.Count())
                        .First()
                        .Key;

                    decision.FollowingNodes[outcome] =
                        new LeafNode(new(majorityLabel));
                }
                else
                {
                    decision.FollowingNodes[outcome] =
                        BuildTree(elementSchema, subset, availableAttributesNew, verbose);
                }
            }
        }

        return decision;
    }

    
    public static (double, double) CalculateInformationGain(
        DataAttribute attribute,
        List<Element> elements
    )
    {
        if (elements.Count == 0)
            return (0.0, double.MaxValue);

        double entropyBefore = CalculateEntropy(elements);
        double entropyAfter = 0.0;

        bool isNumeric = attribute.Outcomes.All(x => double.TryParse(x.Name, out _));

        double bestThreshold = 0;
        double bestInformationGain = double.MinValue;

        if (isNumeric)
        {
            List<double> values = [.. attribute.Outcomes
                .Select(x => double.Parse(x.Name))
                .Distinct()
                .OrderBy(x => x)];

            List<double> thresholds = [.. values
                .Zip(
                    values.Skip(1),
                    (a, b) => (a + b) / 2
                )];

            foreach (double threshold in thresholds)
            {
                List<Element> lowerSubset = elements
                    .Where(e => e.Attributes.Any(a =>
                        a.Name == attribute.Name &&
                        double.Parse(a.Outcome.Name) <= threshold))
                    .ToList();

                List<Element> upperSubset = elements
                    .Where(e => e.Attributes.Any(a =>
                        a.Name == attribute.Name &&
                        double.Parse(a.Outcome.Name) > threshold))
                    .ToList();

                if (lowerSubset.Count == 0 || upperSubset.Count == 0)
                    continue;

                double lowerWeight = (double)lowerSubset.Count / elements.Count;
                double upperWeight = (double)upperSubset.Count / elements.Count;

                entropyAfter =
                    lowerWeight * CalculateEntropy(lowerSubset) +
                    upperWeight * CalculateEntropy(upperSubset);

                double informationGain = entropyBefore - entropyAfter;

                if (informationGain > bestInformationGain)
                {
                    bestInformationGain = informationGain;
                    bestThreshold = threshold;
                }
            }
        }
        else
        {
            foreach (Outcome outcome in attribute.Outcomes)
            {
                List<Element> subset = elements
                    .Where(e => e.Attributes.Any(a =>
                        a.Name == attribute.Name &&
                        a.Outcome.Name == outcome.Name))
                    .ToList();

                if (subset.Count == 0)
                    continue;

                double weight = (double)subset.Count / elements.Count;

                entropyAfter += weight * CalculateEntropy(subset);
            }
        }

        return (
            isNumeric ? bestInformationGain : entropyBefore - entropyAfter,
            isNumeric ? bestThreshold : double.MaxValue
        );
    }

    private static double CalculateEntropy(List<Element> elements)
    {
        if (elements.Count == 0)
            return 0.0;

        return elements
            .GroupBy(e => e.Label)
            .Select(group =>
            {
                double p = (double)group.Count() / elements.Count;
                return -p * Math.Log2(p);
            })
            .Sum();
    }

    private static (
        DataAttribute?,
        double,
        List<DataAttribute>,
        bool,
        double
    ) GetBestSplit(
        List<Element> elements,
        List<DataAttribute> availableAttributes,
        bool verbose = false,
        bool first = false
    )
    {
        DataAttribute? bestSplit = null;
        double bestInformationGain = 0;
        bool bestIsNumeric = false;
        double bestTresholdR = 0.0;

        if (verbose && !first) Console.Write("\n");

        if (verbose)
            Console.WriteLine($"Entropy: {CalculateEntropy(elements):F5}");

        foreach (DataAttribute attribute in availableAttributes)
        {
            (double informationGain, double bestTreshold) =
                CalculateInformationGain(attribute, elements);

            if (verbose)
            {
                Console.WriteLine($"Information Gain for {attribute.Name}{(bestTreshold < double.MaxValue ? $" (Threshold: {bestTreshold})" : "")}: {informationGain:F5}");
            }

            if (informationGain > bestInformationGain)
            {
                bestSplit = attribute;
                bestInformationGain = informationGain;
                bestIsNumeric = bestTreshold < double.MaxValue;
                bestTresholdR = bestTreshold;
            }
        }

        List<DataAttribute> remainingAttributes = [.. availableAttributes];

        if (bestSplit is not null)
            remainingAttributes.Remove(bestSplit);

        if (verbose)
            Console.WriteLine($"\nBest split: {bestSplit?.Name}\nInformation Gain: {bestInformationGain:F5}");

        return (bestSplit, bestInformationGain, remainingAttributes, bestIsNumeric, bestTresholdR);
    }

    public static Outcome Predict(Tree tree, Element element, bool verbose = false)
    {
        Node currentNode = tree.RootNode;

        while (currentNode is Decision decision)
        {
            SetAttribute? attribute = element.Attributes
                .FirstOrDefault(a => a.Name == decision.Attribute.Name)
                ?? throw new Exception($"Missing attribute: {decision.Attribute.Name}.");

            Outcome outcome;

            if (decision.Numeric)
            {
                double value = double.Parse(attribute.Outcome.Name);

                outcome = value <= decision.Treshold
                    ? decision.PossibleOutcomes[0]
                    : decision.PossibleOutcomes[1];
            }
            else
            {
                outcome = decision.PossibleOutcomes
                    .FirstOrDefault(o => o.Name == attribute.Outcome.Name)
                    ?? throw new Exception(
                        $"Unknown outcome: {attribute.Outcome.Name}.");
            }

            Node? nextNode = decision.FollowingNodes
                .FirstOrDefault(x => x.Key.Name == outcome.Name)
                .Value;

            if (nextNode is null)
                throw new Exception(
                    $"No child node for outcome: {outcome.Name}.");

            currentNode = nextNode;

            if (verbose)
            {
                Console.Write("Attribute ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(attribute.Name);
                Console.ResetColor();
                Console.Write(" = ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(outcome.Name);
                Console.ResetColor();
            }
        }

        if (currentNode is LeafNode leaf)
        {
            if (verbose)
            {
                Console.Write("Reached leaf node: ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(leaf.Outcome.Name);
                Console.ResetColor();
            }
            return leaf.Outcome;
        }

        throw new Exception("Invalid tree node.");
    }

    public static void Predict(
        Tree tree,
        DataPool dataPool,
        bool verbose = false,
        bool showConfusionMatrix = false,
        string? exportConfusionMatrixFile = null
    )
    {
        List<Element> elements = dataPool.TrainingData;

        List<Outcome> labels = elements
            .Select(e => e.Label)
            .Distinct()
            .OrderBy(label => label)
            .Select(label => new Outcome(label))
            .ToList();

        Dictionary<string, int> labelIndices = labels
            .Select((label, index) => (label.Name, index))
            .ToDictionary(x => x.Name, x => x.index);

        ConfusionMatrix confusionMatrix = new(labels, labels.Count);

        for (int i = 0; i < elements.Count; i++)
        {
            Element el = elements[i];
            Outcome prediction = Predict(tree, el, verbose);

            Console.WriteLine(
                $"Prediction: {prediction.Name}" +
                (verbose && i < elements.Count - 1 ? "\n" : "")
            );

            if (showConfusionMatrix)
            {
                if (!labelIndices.TryGetValue(el.Label, out int actualIndex))
                    throw new InvalidOperationException(
                        $"Unknown actual label: {el.Label}");

                if (!labelIndices.TryGetValue(
                    prediction.Name, out int predictedIndex))
                {
                    throw new InvalidOperationException(
                        $"Unknown predicted label: {prediction.Name}");
                }

                confusionMatrix.matrix[actualIndex, predictedIndex]++;
            }
        }

        if (showConfusionMatrix)
        {
            Console.WriteLine("\nConfusion Matrix:");
            confusionMatrix.Print();
            if (exportConfusionMatrixFile is not null)
            {
                confusionMatrix.Export(exportConfusionMatrixFile);
                Console.WriteLine($"Exported confusion matrix to {exportConfusionMatrixFile}");
            }
        }
    }
}