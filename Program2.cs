using System.Net;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var hardCodedPath = Path.Combine(builder.Environment.ContentRootPath, "data", "sample.txt");

app.MapGet("/", async (HttpRequest request, IConfiguration configuration) =>
{
	var source = request.Query["source"].ToString().Equals("config", StringComparison.OrdinalIgnoreCase)
		? "config"
		: "hardcoded";
	var configuredPath = configuration["FileReader:Path"];
	var filePath = source == "config" && !string.IsNullOrWhiteSpace(configuredPath)
		? Path.IsPathRooted(configuredPath)
			? configuredPath
			: Path.Combine(builder.Environment.ContentRootPath, configuredPath)
		: hardCodedPath;

	string fileContents;
	string? error = null;

	try
	{
		fileContents = await File.ReadAllTextAsync(filePath);
	}
	catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
	{
		fileContents = string.Empty;
		error = exception.Message;
	}

	var status = error is null ? "File loaded" : "Unable to read file";
	var details = error is null
		? $"<pre>{WebUtility.HtmlEncode(fileContents)}</pre>"
		: $"<p class=\"error\">{WebUtility.HtmlEncode(error)}</p>";

	return Results.Content($$"""
		<!doctype html>
		<html lang="en">
		<head>
			<meta charset="utf-8">
			<meta name="viewport" content="width=device-width, initial-scale=1">
			<title>File Browser</title>
			<style>
				body { font-family: system-ui, sans-serif; margin: 2rem auto; max-width: 60rem; padding: 0 1rem; color: #202124; }
				nav { display: flex; gap: .75rem; margin: 1rem 0 1.5rem; }
				a { background: #1f5eff; color: white; padding: .6rem 1rem; text-decoration: none; border-radius: .35rem; }
				a.secondary { background: #e8eefc; color: #17429b; }
				code, pre { background: #f3f5f7; border-radius: .35rem; }
				code { padding: .15rem .3rem; }
				pre { min-height: 10rem; padding: 1rem; white-space: pre-wrap; }
				.error { color: #a51d2d; }
			</style>
		</head>
		<body>
			<h1>File Browser</h1>
			<p>{{WebUtility.HtmlEncode(status)}} from <code>{{WebUtility.HtmlEncode(filePath)}}</code></p>
			<nav>
				<a href="/?source=hardcoded">Use hard-coded C# path</a>
				<a class="secondary" href="/?source=config">Use config path</a>
			</nav>
			{{details}}
		</body>
		</html>
		""", "text/html");
});

app.Run();
