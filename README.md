# Weknow Json Extensions  [![NuGet](https://img.shields.io/nuget/v/Weknow.Text.Json.Extensions.svg)](https://www.nuget.org/packages/Weknow.Text.Json.Extensions/)
Extensions for System.Text.Json 

for example ImmutableDictionary converter.

``` csharp
JsonStringEnumConverter EnumConvertor = new JsonStringEnumConverter(JsonNamingPolicy.CamelCase);
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    // Add the converter
    Converters = { EnumConvertor, JsonImmutableDictionaryConverter.Default }
};

var source = new Dictionary<ConsoleColor, string> 
{
    [ConsoleColor.Blue] = nameof(ConsoleColor.Blue),
    [ConsoleColor.White] = nameof(ConsoleColor.White)
};

string json = JsonSerializer.Serialize(source, options);
T deserialized = JsonSerializer.Deserialize<T>(json, options);

```
