# MediaRipper

A .NET Standard library to crawl a website in a user-defined way and download all the pictures or other files.

## Usage

### Initialization
First of all initialize the ripper with its fluent builder. We specify the domain and the root folder where we want our file structure to be saved. We also additionally provide one or more session cookies (in case you need to be logged in before accessing the requested resources) and/or headers. We also specify the timeout for the HTTP requests in seconds and the amount of concurrent threads to be used when fetching and downloading files at the same time in a parallelized way.

```csharp
Ripper ripper = new Ripper("https://example.com", "Example")
    .WithSessionCookie("PHPSESSID", "...")
    .WithHeader("Authorization", "Bearer ey...")
    .WithTimeout(60)
    .WithThreads(10);
```

### Fetching pages
This library makes use of two main data structures: `FetchPattern` and `FetchedItem`.
`FetchedPattern` contains the regular expression that will be matched in order to retrieve the `uri` and the `name` of a certain resource.

Let's assume our pages have this uri. `/products/page/1`.

We can then define one for our pages like this.

```csharp
var pattern = new FetchPattern(@"/products/page/([0-9]+)");
```
It's possible to specify custom formats for the `uri` and the `name` by using matching groups, for example if we specify a uri format like this `[0]` it will match the captured group with index 0 (which is the full match) while for the name we can define `[1]` and it will be replaced with the value of the first captured group, which is the page number.

After that we can fetch a list of pages that we can access. We will use the async methods as synchronous because we're in a test console application, but this library is built to work with any graphical interface too. We provide the page where we can find all the other pages we want to fetch, and we do not provide a folder name so that the default one will be used.

```csharp
var pages = ripper.FetchAsync("/products.html", "", pattern).Result.Distinct();
```

### Fetching files
Now we can feed the array of `FetchedItem`s to a new method which will fetch more resources from each of the items. We can type something like this.

```csharp
pattern = new FetchPattern("/products/images/([a-zA-Z0-9_-]+.jpg)");
var images = ripper.FetchAllAsync(pages, pattern, "Products").Result;
```

### Downloading files
After grabbing all the files we need, we can start downloading them using the `RipAllAsync` method. We can also specify an `IProgress<ProgressReport>` object that should be a delegate that informs the user about the current progress.

```csharp
ripper.RipAllAsync(pics, true, new Progress<ProgressReport>(report =>
{
    Console.Title = $"Demo Ripper | Ripping | {report.percent:0.00}%";
    Console.WriteLine(report.message);
})).Wait();
```

And we're done!
