# Weknow Json Extensions  [![NuGet](https://img.shields.io/nuget/v/Weknow.Text.Json.Extensions.svg)](https://www.nuget.org/packages/Weknow.Text.Json.Extensions/) [![.NET Build & Publish NuGet V2](https://github.com/weknow-network/Weknow-Json-Extensions/actions/workflows/build-publish.yml/badge.svg)](https://github.com/weknow-network/Weknow-Json-Extensions/actions/workflows/build-publish.yml)
# Extensions for System.Text.Json 

Functionality of this library includes:

- [YieldWhen](#YieldWhen)
- [Serialization](#Serialization)
  - [ImmutableDictionary converter](#ImmutableDictionary-converter)

## YieldWhen 

Enumerate over json elements.

### With Path

``` cs
[Theory]
[InlineData("friends.[].name", "Yaron,Aviad,Eyal")]
[InlineData("friends.*.name", "Yaron,Aviad,Eyal")]
[InlineData("*.[].name", "Yaron,Aviad,Eyal")]
[InlineData("friends.[1].name", "Aviad")]
[InlineData("skills.*.Role.[]", "architect,cto")]
[InlineData("skills.*.level", "3")]
[InlineData("skills.[3].role.[]", "architect,cto")]
[InlineData("skills.[3]", @"{""role"":[""architect"",""cto""],""level"":3}")]
public async Task YieldWhen_Path_Test(string path, string expectedJoined)
{
    using var srm = File.OpenRead("deep-filter-data.json");
    var source = await JsonDocument.ParseAsync(srm);

    var items = source.YieldWhen(path);
    // ...
}
```

### With Filter

``` cs
using static System.Text.Json.TraverseFlowInstruction;

var items = source.YieldWhen((json, deep, breadcrumbs) =>
{
    string last = breadcrumbs[^1];
    if(deep == 0 && last == "skills")
        return Drill;

    return deep switch
    {
        < 3 => Drill,
        _ => Do(TraverseFlow.SkipToParent),
    };
});
```

## Serialization

### ImmutableDictionary converter.

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
