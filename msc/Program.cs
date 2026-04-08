using System.Data;
using System.Text;
using WeCantSpell.Hunspell;
using System.Text.RegularExpressions;
using System.Diagnostics;

string fileToProcess = string.Empty;
string data = string.Empty;
string resultsFile = string.Empty;

if(Environment.GetEnvironmentVariable("LAMBDA_TASK_ROOT") == null)
{
    try
    {
        Console.WriteLine("msc - Medical Spell Checker");

        if (Console.IsInputRedirected)
        {
            fileToProcess = args[0];
            using var stdin = Console.OpenStandardInput();
            using var sr = new StreamReader(stdin, Console.InputEncoding ?? Encoding.UTF8);
            data = await sr.ReadToEndAsync();
        }
        else
        {
            fileToProcess = args[0];
            resultsFile = fileToProcess + ".json";
            data = File.ReadAllText(fileToProcess);
        }

        long startTime = Stopwatch.GetTimestamp();
        data = checkSpellings(data, fileToProcess);
        TimeSpan elapsedTime = Stopwatch.GetElapsedTime(startTime);
       
        if (Console.IsInputRedirected)
        {
            using var stdout = Console.OpenStandardOutput();
            using var sw = new StreamWriter(stdout, Console.OutputEncoding ?? Encoding.UTF8) { AutoFlush = true };
            await sw.WriteAsync(data);
            Environment.Exit(0);
        }
        else
        {
            File.WriteAllText(resultsFile, data);
            Console.WriteLine($"Results written to : {resultsFile}");
        }

        Console.WriteLine($"Spellchecker duration : {elapsedTime}");
        Environment.Exit(0);
    }
    catch (Exception Ex)
    {
        Console.Error.WriteLine(Ex.Message);
        Environment.Exit(1);
    }
}
else
{
    try
    {
        // when invoked from lambda function ALWAYS use
        // STDIN as the source for the data to spellcheck
        if (Console.IsInputRedirected)
        {
            fileToProcess = args[0];
            using var stdin = Console.OpenStandardInput();
            using var sr = new StreamReader(stdin, Console.InputEncoding ?? Encoding.UTF8);
            data = await sr.ReadToEndAsync();

            data = checkSpellings(data, fileToProcess);

            using var stdout = Console.OpenStandardOutput();
            using var sw = new StreamWriter(stdout, Console.OutputEncoding ?? Encoding.UTF8) { AutoFlush = true };
            await sw.WriteAsync(data);
            Environment.Exit(0);
        }
        Environment.Exit(1);
    }
    catch (Exception Ex)
    {
        Console.Error.WriteLine(Ex.Message);
        Environment.Exit(1);
    }
}

static string checkSpellings (string input, string transaction)
{
    try
    {
        DataTable spellings = new();
        spellings.Clear();
        spellings.Columns.Add("Word");
        spellings.Columns.Add("Suggestions");

        input = Regex.Replace(input, @"(?<=[a-zA-Z])\.(?=[a-zA-Z])", ". ");
        input = Regex.Replace(input, @"(?<=[a-zA-Z])/(?=[a-zA-Z])", " / ");
        input = input.Replace("\n", " ");
        input = input.Replace("\r", " ");

        var dictionary = WordList.CreateFromFiles("./msc.dic");

        string[] words = input.Split(' ');

        foreach (string word in words)
        {
            string check = word;

            if (check.Length > 1)
            {
                check = check.Replace(",", "");
                check = check.Replace(".", "");
                check = check.Replace("(", "");
                check = check.Replace(")", "");

                if(check.StartsWith('\'') || check.EndsWith('\''))
                {
                    check = check.Replace("'", "");
                }
                check = check.Trim();

                if (!dictionary.Check(check))
                {
                    string suggestion = "";
                    IEnumerable<string> suggestions = dictionary.Suggest(check);
                    foreach (var correction in suggestions)
                    {
                        suggestion = suggestion + correction + "^";
                    }

                    DataRow wordRow = spellings.NewRow();
                    wordRow["Word"] = word;
                    wordRow["Suggestions"] = suggestion;
                    spellings.Rows.Add(wordRow);
                }
            }
        }

        StringBuilder jsonResult = new("{");
        jsonResult.Append("\"msc\": {");
        jsonResult.Append($"\"transactionID\": \"{transaction}\",");

        if(spellings.Rows.Count > 0)
        {
            jsonResult.Append("\"miss-spellings-found\": true,");
        }
        else
        {
            jsonResult.Append("\"miss-spellings-found\" : false");
        }

        if(spellings.Rows.Count > 0) {
            jsonResult.Append("\t\t\"miss-spellings\": [");
            int rowNumber = 1;
            foreach (DataRow row in spellings.Rows)
            {
                string word = row["Word"]?.ToString() ?? "";
                string suggestions = row["Suggestions"]?.ToString() ?? "";
                
                if(rowNumber == spellings.Rows.Count)
                {
                    jsonResult.Append("\t\t\t{\"" + word + "\": \"" + suggestions + "\"}");
                }
                else
                {
                    jsonResult.Append("\t\t\t{\"" + word + "\": \"" + suggestions + "\"}, ");
                }
                rowNumber++;
            }

            jsonResult.Append("],");
            jsonResult.Append($"\t\t\"miss-spellings-count\": \"{spellings.Rows.Count}\" ");
        }

        jsonResult.Append("} ");
        jsonResult.Append("} ");
        return jsonResult.ToString();
    }
    catch (Exception Ex)
    {
        Console.WriteLine(Ex.Message);
        return string.Empty;
    }
}
