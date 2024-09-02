
# MO.Mapper NuGet Package

MO.Mapper is a utility library for .NET applications that facilitates mapping one type of object to another. It is especially useful for transforming data transfer objects (DTOs) and business logic objects. MO.Mapper uses reflection mechanisms to copy data from a source object to a target object.

## Features
- `Map<TSource, TTarget>(TSource source)`: Converts an object of type TSource to an object of type TTarget.
- `Map<TSource, TTarget>(IEnumerable<TSource> source)`: Converts a collection of TSource objects to a collection of TTarget objects.

## How to Use

### Map<TSource, TTarget>(TSource source)
This method transforms a source object into a target type. If the target type has a parameterless constructor, the target object is created, and the readable properties from the source object are copied to the writable properties of the target object.

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

// Usage
Source source = new Source { Name = "John", Age = 30 };
Target target = Mapper.Map<Source, Target>(source);
```

### Map<TSource, TTarget>(IEnumerable<TSource> source)
This method converts all objects in a source collection to objects of the target type.

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

// Usage
List<Source> sources = new List<Source>
{
    new Source { Name = "John", Age = 30 },
    new Source { Name = "Jane", Age = 25 }
};

// Usage
List<Target> targets = Mapper.Map<Source, Target>(sources);
```

## Class to Record and Record to Class Conversions
MO.Mapper not only supports conversions between class types but also works with record types. Introduced in .NET 5, record types provide immutability with less boilerplate code for data objects. MO.Mapper supports these conversions as well.

**Class to Record**
You can convert a class type object to a record type object. This conversion is typically used in transitions between data transfer objects (DTOs) and business logic objects.

```csharp
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public record PersonRecord(string Name, int Age);

// Usage
Person person = new Person { Name = "Alice", Age = 28 };
PersonRecord personRecord = Mapper.Map<Person, PersonRecord>(person);
```

**Record to Class**
You can convert a record type object to a class type object. This conversion can be useful for processing data or meeting specific business logic requirements.

```csharp
public record EmployeeRecord(string Name, int Age);

public class Employee
{
    public string Name { get; set; }
    public int Age { get; set; }
}

// Usage
EmployeeRecord employeeRecord = new EmployeeRecord("Bob", 35);
Employee employee = Mapper.Map<EmployeeRecord, Employee>(employeeRecord);
```

## Notes
**Map<TSource, TTarget>(TSource source):**

- If the target type has a parameterless constructor, a target object is created, and properties from the source object are copied to the target object.
- If the target type's constructor requires parameters, appropriate parameters are taken from the source object to create the target object.

**Map<TSource, TTarget>(IEnumerable<TSource> source):**

- Each object in the provided source collection is transformed using the `Map<TSource, TTarget>(TSource source)` method, and the results are returned as a list.

## Error Handling
- **Missing Public Constructor**: An `InvalidOperationException` is thrown if there is no suitable constructor in the target type.
- **Parameter Mismatches**: Default values are assigned if parameters do not match.