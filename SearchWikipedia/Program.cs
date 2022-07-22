using SearchWikipedia;

Console.WriteLine("Wiki inicial: ");
string initialWiki = Console.ReadLine();

Console.WriteLine("Wiki final: ");
string finalWiki = Console.ReadLine();

RecursiveSearch recursiveSearch = new RecursiveSearch();

var a = await recursiveSearch.StartSearch(initialWiki, finalWiki, 20);

Console.Beep();
Console.WriteLine("\n");
Console.WriteLine("Profundidade: " + a.DeepLevel);
Console.WriteLine("Tempo: " + a.Time);
Console.WriteLine("Caminho: " + a.Path);
Console.WriteLine("\n\n");
Console.ReadKey();