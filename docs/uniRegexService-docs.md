# UniRegexService

## Purpose

`UniRegexService.cs` aggregates the regular expressions used to parse Canvas event titles and identify course and section information for by-class customization.

Each university configuration must define the following dictionary keys:

- `SectionInfo` - Extracts the section/course information from the assignment summary.
- `AssignmentName` - Extracts the assignment name from the assignment summary.
- `ClassName` - Extracts the course/class identifier from `SectionInfo`.
- `SectionName` - Extracts the section identifier from `SectionInfo`.

## Parsing Flow

Parsing occurs in two stages:

```text
Canvas Event Summary
├── AssignmentName
└── SectionInfo
    ├── ClassName
    └── SectionName
```

`SectionInfo` and `AssignmentName` receive the original Canvas event summary.

`ClassName` and `SectionName` receive the value returned by `SectionInfo`.

For example:

```text
"Homework 1 [CSE 101 AB1234]"
    │
    ├── AssignmentName → "Homework 1"
    │
    └── SectionInfo → "CSE 101 AB1234"
                        │
                        ├── ClassName   → "CSE 101"
                        └── SectionName → "AB1234"
```

## How Do I Add My School?

Before adding a new regular expression, check whether one of the existing universal or generalized definitions supports your school's naming format.

If a university requires its own parsing rules:

1. Add university-specific parsing functions for the definitions that differ from the existing ones.
2. Keep each definition as a `Regex.Match`, even if its current behavior could be implemented another way. University naming conventions may contain edge cases that require the expression to diverge later.
3. Add the university to `_uniParseFunctions`.
4. Define all four required dictionary keys. Reuse universal or generalized functions where applicable.

A university configuration should follow this structure:

```csharp
["University Name"] = new Dictionary<string, Func<string, string>>
{
    ["SectionInfo"] = SectionInfoUniversal,
    ["AssignmentName"] = AssignmentNameUniversal,
    ["ClassName"] = GetUniversityCN,
    ["SectionName"] = GetUniversitySN
}
```

Parsing functions should return an empty string when their expected value cannot be matched.

## Current Regex

This section describes the naming formats currently recognized by each parser. It is intended to describe the supported input format rather than the implementation details of the regular expression.

### Universal

#### `SectionInfoUniversal`

Matches content enclosed in square brackets.

```text
[<section information>]
```

Example:

```text
[CSE 101 AB1234]
```

#### `AssignmentNameUniversal`

Matches all text before the first opening square bracket.

```text
<assignment name> [<section information>]
```

### General

#### `GetGeneralCN`

Recognizes course names consisting of:

```text
<3-5 uppercase letters> [optional space] <3 digits> <0-3 letters>
```

Examples:

```text
CSE101
CSE 101
MATH 101
CSE 101A
```

### Arizona State University

#### `GetAzStUniCN`

Recognizes:

```text
<3 uppercase letters><3 digits>
```

Example:

```text
CSE101
```

#### `GetAzStUniSN`

Recognizes:

```text
<4 digits><additional section information><A, B, or C>
```

### Ohio State University

#### `GetOhioStUniCN`

Recognizes:

```text
<3+ uppercase letters> <4 digits>[optional decimal][optional uppercase letter]
```

#### `GetOhioStUniSN`

Recognizes an Ohio State section identifier in the format expected within its section information.

## Testing

The xUnit test project is located at `modtests/UniRegexService.Tests`. Run it from the repository root with:

```powershell
dotnet test modtests/UniRegexService.Tests/UniRegexService.Tests.csproj
```

Tests are organized into `Universal`, `ArizonaStateUniversity`, and `OhioStateUniversity` folders. Each fixture creates its own `UniRegexService` with a registered university name; the universal fixture uses Arizona State University's configuration because the universal parsers are shared by each university configuration. These folders currently contain setup only and no behavior-specific test cases.

Use the example in `Template/README.md` as a starting point when adding a test class and concrete cases for another university.