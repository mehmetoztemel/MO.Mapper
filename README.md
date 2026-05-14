# MO.Mapper NuGet Package

MO.Mapper is a lightweight, high-performance object mapping library for .NET applications. It simplifies transforming data between types such as DTOs and business logic objects, using reflection with built-in caching for optimal performance.

## Features

- `Map<TSource, TTarget>(TSource source)` — Maps a single source object to a target type.
- `Map<TSource, TTarget>(IEnumerable<TSource> source)` — Maps a collection of source objects to a list of target objects.
- **Reflection cache** — Property and constructor metadata is resolved once per type and reused across all subsequent calls. No repeated `GetProperties` overhead.
- **Record support** — Works with both `class` and `record` types, including parameterized constructors.
- **Thread-safe** — Cache uses `ConcurrentDictionary`, safe for multi-threaded environments such as ASP.NET Core.

---

## Installation

```bash
dotnet add package MO.Mapper
```

---

## Usage

### Single object mapping

```csharp
public class Source
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public class Target
{
    public string Name { get; set; }
    public int Age { get; set; }
}

Source source = new Source { Name = "John", Age = 30 };
Target target = Mapper.Map<Source, Target>(source);
```

### Collection mapping

```csharp
List<Source> sources = new List<Source>
{
    new Source { Name = "John", Age = 30 },
    new Source { Name = "Jane", Age = 25 }
};

List<Target> targets = Mapper.Map<Source, Target>(sources);
```

### Mapping into an existing object

An existing target instance can be passed as the second parameter. In this case no new object is created — only the matching properties are overwritten.

```csharp
Target existing = GetFromDatabase();
Mapper.Map<Source, Target>(source, existing);
```

### Updating an existing collection

```csharp
List<Target> existingTargets = GetFromDatabase();
List<Target> updated = Mapper.Map<Source, Target>(sources, existingTargets);
```

---

## Class and Record conversions

MO.Mapper supports all combinations of `class` and `record` types.

**Class → Record**

```csharp
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public record PersonRecord(string Name, int Age);

Person person = new Person { Name = "Alice", Age = 28 };
PersonRecord personRecord = Mapper.Map<Person, PersonRecord>(person);
```

**Record → Class**

```csharp
public record EmployeeRecord(string Name, int Age);

public class Employee
{
    public string Name { get; set; }
    public int Age { get; set; }
}

EmployeeRecord employeeRecord = new EmployeeRecord("Bob", 35);
Employee employee = Mapper.Map<EmployeeRecord, Employee>(employeeRecord);
```

---

## How the cache works

Reflection metadata (property lists and constructors) is expensive to resolve repeatedly. MO.Mapper caches this metadata the first time a type is encountered and reuses it for all subsequent calls.

```
First call:  Book  → BookDto   resolves and caches typeof(Book), typeof(BookDto)
Second call: Book  → BookDto   reads directly from cache, no reflection overhead
Third call:  BookDto → Book    typeof(Book) and typeof(BookDto) are already cached, nothing new is resolved
```

Cache entries are scoped to the application lifetime. Because entries are keyed by `Type` and only metadata is stored (not object instances), memory usage is negligible even in large projects — typically a few hundred KB across hundreds of mapped types.

---

## Notes

**Constructor resolution priority**

1. Parameterless constructor — object is created and properties are copied.
2. Fewest-parameter constructor — parameters are matched to source properties by name (case-insensitive). Unmatched parameters receive their default value (`0`, `false`, `null`, etc.).

**Property matching rules**

- Matching is done by property name (case-sensitive equality).
- Type compatibility is checked via `IsAssignableFrom`. Incompatible types are skipped.
- Only readable (`CanRead`) source properties and writable (`CanWrite`) target properties are considered.

---

## Error Handling

| Situation | Behavior |
|---|---|
| No public constructor on target type | `InvalidOperationException` is thrown |
| Constructor parameter has no matching source property | Default value is used (`0`, `false`, `null`) |
| Source property type is incompatible with target property | Property is skipped |
| `source` argument is `null` | `ArgumentNullException` is thrown |