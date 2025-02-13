using System.Diagnostics;
using System.Text;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            await ExecuteAzSignedInAsync();
            Console.WriteLine("Azure login successful.");

            var azureKeyVaultUri = "";
            var operation = 0;

            while (string.IsNullOrWhiteSpace(azureKeyVaultUri))
            {
                Console.Write("Enter your Azure Key Vault URI: ");
                azureKeyVaultUri = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(azureKeyVaultUri))
                {
                    Console.Write("Invalid input given. Please try again");
                }
            }

            var secretClient = new SecretClient(new Uri(azureKeyVaultUri), new DefaultAzureCredential());

            while (operation != 1 && operation != 2)
            {
                Console.WriteLine("1 : Export Secrets");
                Console.WriteLine("2 : Bulk Import Secrets");

                Console.Write("Please Enter Operation : ");
                var input = Console.ReadLine();

                int.TryParse(input, out operation);
            }

            if (operation == 1)
            {
                // Get all Secret keys from KV
                var secretProps = secretClient.GetPropertiesOfSecrets();
                var stringBuilder = new StringBuilder();
                foreach (var secretProp in secretProps)
                {
                    var secret = await secretClient.GetSecretAsync(secretProp.Name);
                    stringBuilder.AppendLine(ConvertToCsvCell(secret.Value.Name) + "," + ConvertToCsvCell(secret.Value.Value));
                }

                Console.WriteLine($"{secretProps.Count()} secrets have been exported from Azure Key Vault. Writing to the file.");

                var outputDir = Path.Combine(AppContext.BaseDirectory, "Outputs");
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // Export file to csv
                var str = stringBuilder.ToString();
                File.WriteAllText(Path.Combine(outputDir, "SecretKeys.csv"), str );
                Console.WriteLine("Success!");
            }

            else
            {
                var inputDir = Path.Combine(AppContext.BaseDirectory, "Inputs");
                var inputFile = Path.Combine(inputDir, "SecretsToUpload.csv");
                if (!File.Exists(inputFile))
                {
                    throw new FileNotFoundException("File not found", inputFile);
                }

                var streamReader = new StreamReader(inputFile);
                var keyVals = new List<KeyValuePair<string, string>>();

                while (!streamReader.EndOfStream)
                {
                    var line = streamReader.ReadLine();
                    var splits = line.Split(',');
                    if (splits.Length >= 2 && !string.IsNullOrWhiteSpace(splits[0].Trim()) && !string.IsNullOrWhiteSpace(splits[1]))
                    {
                        keyVals.Add(new KeyValuePair<string, string>(splits[0].Trim(), splits[1]));
                        Console.WriteLine($"Secret Added To List : {splits[0].Trim()} - {splits[1]}");
                    }
                }

                Console.WriteLine($"No of secrets: {keyVals.Count}");

                if (!keyVals.Any())
                {
                    Console.WriteLine("No secrets imported.");
                    return;
                }

                foreach (var kv in keyVals)
                {
                    var notAdded = false;
                    try
                    {
                        var existingVal = await secretClient.GetSecretAsync(kv.Key);
                        if (existingVal.HasValue && existingVal.Value?.Value == kv.Value)
                        {
                            notAdded = true;
                        }
                    }
                    catch (RequestFailedException e)
                    {
                        if (e.ErrorCode == "SecretNotFound")
                        {
                            Console.WriteLine(e.Message.Split(".")[0]);
                        }
                        else
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                    finally
                    {
                        if (notAdded)
                        {
                            Console.WriteLine($"Secret NOT synced to KV, Similar value already set : {kv.Key}");
                        }
                        else
                        {
                            Console.WriteLine($"Start syncing to KV : {kv.Key}");
                            await secretClient.SetSecretAsync(kv.Key, kv.Value);
                            Console.WriteLine($"Secret synced to KV : {kv.Key}");  
                        }
                       
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
    
    static async Task ExecuteAzSignedInAsync()
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "az",
            Arguments = "ad signed-in-user show",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();
        
        Console.WriteLine(output);

        if (process.ExitCode != 0)
        {
            await ExecuteAzLoginAsync();
        }
    }

    static async Task ExecuteAzLoginAsync()
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "az",
            Arguments = "login",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Azure CLI login failed. Error: {error}");
        }

        Console.WriteLine(output);
    }
    
    private static string ConvertToCsvCell(string value)
    {
        var mustQuote = value.Any(x => x == ',' || x == '\"' || x == '\r' || x == '\n');

        if (!mustQuote)
        {
            return value;
        }

        value = value.Replace("\"", "\"\"");

        return string.Format("\"{0}\"", value);
    }
}