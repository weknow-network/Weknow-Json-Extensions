# Weknow Json Extensions  [![NuGet](https://img.shields.io/nuget/v/Weknow.Text.Json.Extensions.svg)](https://www.nuget.org/packages/Weknow.Text.Json.Extensions/) [![.NET Build & Publish NuGet V2](https://github.com/weknow-network/Weknow-Json-Extensions/actions/workflows/build-publish.yml/badge.svg)](https://github.com/weknow-network/Weknow-Json-Extensions/actions/workflows/build-publish.yml)
Extensions for System.Text.Json 

Functionality of this library includes:

- [YieldWhen](#YieldWhen)
- [Serialization](#Serialization)
  - [Convert Object into Json Element](#ToJson)
  - [Convert Json Element to string](#AsString)
  - [Convert Json Element to Stream](#ToStream)
  - [ImmutableDictionary converter](#ImmutableDictionary-converter)

## YieldWhen 

Enumerate over json elements.

### With Path

Rely on path convention:

json.YieldWhen(/path convention/);

``` json
{
  "friends": [
    {
      "name": "Yaron",    
      "id": 1
    },
    {
      "name": "Aviad",   
      "id": 2
    }
  ]
}
```

- "friends.[].name") or "friends.*.name" 
  will result with ["Yaron", "Aviad"] 
- "friends.[0].name") or "friends.*.name" 
  will result with ["Yaron"] 
- "friends.[0].*") or "friends.*.name" 
  will result with ["Yaron",1] 


``` json
{ "role": [ "architect", "cto" ], "level": 3 }
```

See: YieldWhen_Path_Test

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

### ToJson

Convert .NET object into JsonElement.

``` cs
var entity = new Entity(12, new BEntity("Z"));
JsonElement json = entity.ToJson();
```

``` cs
var arr = new []{ 1, 2, 3 };
JsonElement json = arr.ToJson();
```

### AsString

Convert JsonElement to string

``` cs
JsonElement json = ...;
string compact = json.AsString();
string indented = json.AsIndentString();
string raw = json.GetRawText(); // same as json.AsIndentString();
```

### ToStream

Convert JsonElement to stream 

``` cs
JsonElement json = ...;
Stream srm = json.ToStream();
```

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
