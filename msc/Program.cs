using System.Data;
using System.Text;
using WeCantSpell.Hunspell;
using System.Text.RegularExpressions;

string transactionID = "";

if(args.Length == 1)
{
    transactionID = validateArg(args[0]);
}

if(args.Length == 2)
{
    Console.WriteLine("ARG1 = " + args[1]);
    transactionID = args[1];
}

if(transactionID == "TESTING")
{
    string input = "Patient was given paracetamool on Thurrsday";
    // string input = "Patient was given a drink and feels fine";

    input = checkSpellings(input, transactionID);
    // input = returnFormattedJSON(input);
    Console.WriteLine(input);
} else
{
    Console.WriteLine("TRANSACTION ID : " + transactionID);

    try
    {
        string input = File.ReadAllText("/tmp/" + transactionID + ".txt");
        input = checkSpellings(input, transactionID);
        // input = returnFormattedJSON(input);
        File.WriteAllText("/tmp/" + transactionID + ".json", input);
    }
    catch (Exception Ex)
    {
        Console.WriteLine(Ex.Message);
    }
}

Console.WriteLine("Bye from MSC");


static string validateArg(string arg)
{
    Console.WriteLine("validateArg" + arg);

    var parts = arg.Split(['='], 2);
    if (parts.Length == 2 && parts[0] == "--transactionID")
    {
        return parts[1];
    }
    else
    {
        Environment.Exit(1);
        return "";
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
