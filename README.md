# Weknow Json Extensions  

[![Prepare](https://github.com/weknow-network/Weknow-Json-Extensions/actions/workflows/prepare-nuget.yml/badge.svg)](https://github.com/weknow-network/Weknow-Json-Extensions/actions/workflows/prepare-nuget.yml) [![Build & Deploy NuGet](https://github.com/weknow-network/Weknow-Json-Extensions/actions/workflows/Deploy.yml/badge.svg)](https://github.com/weknow-network/Weknow-Json-Extensions/actions/workflows/Deploy.yml)  
[![NuGet](https://img.shields.io/nuget/v/Weknow.Text.Json.Extensions.svg)](https://www.nuget.org/packages/Weknow.Text.Json.Extensions/)  

Functionality of this library includes:

- [YieldWhen](#YieldWhen)
- [Filter](#Filter)
  - [Path based Filter][#Path-based-Filter]
- [Exclude](#Exclude)
- [Merge](#Merge)
  - [Merge Into][#Merge-Into]
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

- **"friends.[].name"** or "friends.*.name" 
  will result with ["Yaron", "Aviad"] 
- **"friends.[0].name"** or "friends.*.name" 
  will result with ["Yaron"] 
- **"friends.[0].*"** or "friends.*.name" 
  will result with ["Yaron",1] 


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

## Filter

Filter operation, clean up the element according to a filter.  
It excludes whatever doesn't match the filter

``` json
{
  "A": 10,
  "B": [
    { "Val": 40 },
    { "Val": 20 },
    { "Factor": 20 }
  ],
  "C": [0, 25, 50, 100 ],
  "Note": "Re-shape json"
}
```

Filter:
``` cs
JsonElement source = ..;
var target = source.Filter((e, _, _) =>
            e.ValueKind != JsonValueKind.Number || e.GetInt32() > 30 
            ? TraverseFlowWrite.Drill 
            : TraverseFlowWrite.Skip);
```
Will result in:
``` cs
{
  "B": [ { "Val": 40 }],
  "C": [ 50, 100 ],
  "Note": "Re-shape json"
}
```
### Path based Filter

``` cs
var target = source.Filter("B.*.val");
// results: {"B":[{"Val":40},{"Val":20}]}
```

``` cs
var target = source.Filter("B.[]");
// results: {"B":[{"Val":40},{"Val":20},{"Factor":20}]}
```

``` cs
var target = source.Filter("B.[].Factor");
// results: {"B":[{"Factor":20}]}
```

``` cs
var target = source.Filter("B.[1].val");
// results: {"B":[{"Val":20}]}
```

## Exclude

Exclude is kind of opposite of Filter.
It instruct which elements to remove.

``` json
{
  "A": 10,
  "B": [
    { "Val": 40 },
    { "Val": 20 },
    { "Factor": 20 }
  ],
  "C": [0, 25, 50, 100 ],
  "Note": "Re-shape json"
}
```

``` cs
var target = source.Exclude("B.*.val");
// results: {"A":10,"B":[{"Factor":20}],"C":[0,25,50,100],"Note":"Re-shape json"}
```

``` cs
var target = source.Exclude("B.[]");
// results: {"A":10,"B":[],"C":[],"Note":"Re-shape json"}
```

``` cs
var target = source.Exclude("B.[1]");
// results: {"A":10,"B":[{"Val":40},{"Factor":20}],"C":[0,50,100],"Note":"Re-shape json"}
```

``` cs
var target = source.Exclude("B");
// results: {"A":10,"C":[0,25,50,100],"Note":"Re-shape json"}
```

## Merge

Merging 2 or more json.
The last json will override previous on conflicts

``` source json
{
  "A": 1
}
```

``` joined json 
{
  "B": 2
}
```

``` cs
var target = source.Merge(joined);
// results: {"A":1,"B":2}
```

``` cs
var target = source.Merge(new { B = 2}); // anonymous type
// results: {"A":1,"b":2}
```

More scenarios:
- {"A":1}.Merge({"B":2,"C":3}) = {"A":1, "B":2, "C"":3}
- {"A":1}.Merge({"B":2},{"C":3}) = {"A":1, "B":2, "C"":3}
- {"A":1}.Merge({"B":2},{"C":3}) = {"A":1, "B":2, "C"":3}

### Merge Into

Merging json into specific path of a source json.
The last json will override previous on conflicts

``` source json
{
  "A": 1,
  "B": {
    "B1":[1,2,3]
  }
}
```

``` cs
var joined = 5;
var target = source.MergeInto("B.B1.[1]", joined);
// results: { "A": 1, "B": { "B1":[1,5,3] }
}
```

``` cs
var joined = ... // {"New":"Object"};
var target = source.MergeInto("B.B1.[1]", joined);
// results: { "A": 1, "B": { "B1":[1,{"New":"Object"},3] }
}
```

``` cs
var joined = new {"New":"Object"}; // anonymous type
var target = source.MergeInto("B.B1.[1]", joined);
// results: { "A": 1, "B": { "B1":[1,{"new":"Object"},3] }
}
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
