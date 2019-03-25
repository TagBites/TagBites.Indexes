# TagBites.Indexes

Wrapper for Lucene.Net full-text search index to simplify creation, searching and to store it as a single file.

## Creating Index Example

```csharp
using (var searchIndex = new SearchIndexBuilder("index.sidx"))
{
    searchIndex.Index("/docs/readme.md", "Readme File Title", "This is an example of readme text file content.");
    searchIndex.Index("/docs/start.md", "Start File Title", "This is an example of start text file content.");
}
```

## Search Example

```csharp
using (var searchIndex = new SearchIndex("index.sidx"))
{
    var result = searchIndex.Search("file content");

    for (var i = 0; i < result.Items.Count; i++)
    {
        var item = result.Items[i];

        Console.WriteLine("{0}. {1}", i + 1, item.Title);
        Console.WriteLine(item.Preview);
        Console.WriteLine("Url: {0}", item.Url);
    }
}

```

Output:

```
1. Readme File Title
This is an example of readme text <mark>file</mark> <mark>content</mark>.
Url: /docs/readme.md

2. Start File Title
This is an example of start text <mark>file</mark> <mark>content</mark>.
Url: /docs/start.md
```
