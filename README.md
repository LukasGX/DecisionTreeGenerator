# DecisionTreeGenerator

[![.NET](https://github.com/LukasGX/DecisionTreeGenerator/actions/workflows/dotnet.yml/badge.svg)](https://github.com/LukasGX/DecisionTreeGenerator/actions/workflows/dotnet.yml)

A decision tree generator written in C#.

## Features

- Generates decision trees using entropy and information gain.
- Supports categorical attributes with predefined outcomes.
- Loads schemas and datasets from CSV and JSON files.
- Tests generated trees against test data.
- Command-line interface with automatic help and version information.

## Installation

Clone the repository and build the project:

```bash
git clone https://github.com/LukasGX/DecisionTreeGenerator.git
cd DecisionTreeGenerator
dotnet build -c Release
```

The application can also be published and installed as `dtg` for direct use in the terminal.

## Usage

```bash
dtg [options]
```

| Option                 | Description                    | Default      |
| ---------------------- | ------------------------------ | ------------ |
| `-s`, `--schema`       | Schema file or `autodetect`    | `autodetect` |
| `-e`, `--trainingdata` | Training data file or `manual` | `manual`     |
| `-t`, `--testdata`     | Test data file or `manual`     | `manual`     |
| `--help`, `-h`         | Show help                      |              |
| `--version`            | Show version                   |              |

Files can be loaded as CSV or JSON by specifying the format before the filename.

Example:

```bash
dtg -s json:schema.json -e csv:training.csv -t json:test.json
```

## Schema format

A schema defines the available attributes and their possible outcomes.

### CSV

Schema CSV files do not have a header. Each line contains an attribute name followed by its possible outcomes, separated by semicolons.

```csv
Color;Red;Green;Blue
Shape;Circle;Square;Rectangle
```

### JSON

```json
[
    {
        "name": "Color",
        "outcomes": ["Red", "Green", "Blue"]
    },
    {
        "name": "Shape",
        "outcomes": ["Circle", "Square", "Rectangle"]
    }
]
```

## Training and test data

Each data entry consists of a label and a value for every attribute defined in the schema.

### CSV

CSV data files use a header row. The `Label` column is required. All other columns must match the schema attributes.

```csv
Label;Color;Shape
A;Red;Circle
A;Green;Square
B;Red;Rectangle
```

Semicolons are used as delimiters.

### JSON

JSON data files contain an array of objects. Each object has a `Label` property and one property for every schema attribute.

```json
[
    {
        "Label": "A",
        "Color": "Red",
        "Shape": "Circle"
    },
    {
        "Label": "B",
        "Color": "Green",
        "Shape": "Rectangle"
    }
]
```

## How it works

The generator calculates the entropy of the training data and uses information gain to select attributes for splitting the dataset. It recursively builds the tree until the remaining data can no longer be usefully split.

The resulting tree can be printed and evaluated against the test dataset.

## Dependencies

- .NET
- [System.CommandLine](https://www.nuget.org/packages/System.CommandLine)
- [CsvHelper](https://www.nuget.org/packages/CsvHelper)

JSON processing uses `System.Text.Json`.

## Project structure

```text
Data/                  Data structures and schema handling
DecisionTree/          Tree representation and evaluation
Generator/             Decision tree generation algorithm
Nodes/                 Decision and leaf nodes
Program.cs             Command-line interface
```
