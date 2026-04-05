using System.Data;
using System.Text;
using WeCantSpell.Hunspell;
using System.Text.RegularExpressions;

string fileToProcess = args[0];

if(Environment.GetEnvironmentVariable("LAMBDA_TASK_ROOT") == null)
{
    try
    {
        Console.WriteLine("msc - Medical Spell Checker");

        string resultsFile = fileToProcess + ".json";
        string data = File.ReadAllText(fileToProcess);
        data = checkSpellings(data, fileToProcess);
        File.WriteAllText(resultsFile, data);
        Console.WriteLine($"Results written to : {resultsFile}");
    }
    catch (Exception Ex)
    {
        Console.WriteLine(Ex.Message);
    }
}
else
{
    Console.WriteLine("msc - invoked from Lambda function");
    try
    {
        string input = File.ReadAllText("/tmp/" + fileToProcess + ".txt");
        input = checkSpellings(input, fileToProcess);
        File.WriteAllText("/tmp/" + fileToProcess + ".json", input);
    }
    catch (Exception Ex)
    {
        Console.WriteLine(Ex.Message);
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

        string spellCheckedResult = "";

        input = Regex.Replace(input, @"(?<=[a-zA-Z])\.!p!(?=[a-zA-Z])", ". ");
        input = Regex.Replace(input, @"(?<=[a-zA-Z])!p!(?=[a-zA-Z])", " ");
        input = Regex.Replace(input, @"(?<=[a-zA-Z])\.(?=[a-zA-Z])", ". ");
        input = Regex.Replace(input, @"(?<=[a-zA-Z])/(?=[a-zA-Z])", " / ");
        input = input.Replace("\n", " ");
        input = input.Replace("\r", " ");

        var dictionary = WordList.CreateFromFiles("./msc.dic");

        string[] words = input.Split(' ');

        foreach (string word in words)
        {
            string check = word;
            check = check.Trim();

            if (check.Length == 0 || check == "/")
            {
                spellCheckedResult += $"{word}";
            }
            else
            {
                string preRemoved = check;
                check = check.Replace("!p!", "");
                check = check.Replace(",", "");
                check = check.Replace(".", "");
                check = check.Replace("(", "");
                check = check.Replace(")", "");

                if(check.StartsWith('\'') || check.EndsWith('\''))
                {
                    check = check.Replace("'", "");
                }
                check = check.Trim();

                if(word != "!p!") {
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
                    else
                    {
                        spellCheckedResult += $"{word} ";
                    }
                }
                else
                {
                    spellCheckedResult += $"{word} ";
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
