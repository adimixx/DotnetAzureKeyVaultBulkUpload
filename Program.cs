using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

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
        stringBuilder.AppendLine(secretProp.Name);
    }
    Console.WriteLine($"{secretProps.Count()} secrets have been exported from Azure Key Vault. Writing to the file.");

    var outputDir = Path.Combine(AppContext.BaseDirectory, "Outputs");
    if (!Directory.Exists(outputDir))
    {
        Directory.CreateDirectory(outputDir);
    }

    // Export file to csv
    File.WriteAllText(Path.Combine(outputDir, "SecretKeys.csv"), stringBuilder.ToString());
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
        var splits = streamReader.ReadLine().Split(';');
        if (splits.Length >= 2 && !string.IsNullOrWhiteSpace(splits[0].Trim()) && !string.IsNullOrWhiteSpace(splits[1].Trim()))
        {
            keyVals.Add(new KeyValuePair<string, string>(splits[0].Trim(), splits[1].Trim()));
            Console.WriteLine($"Secret Added To List : {splits[0].Trim()} - {splits[1].Trim()}");
        }
    }

    Console.WriteLine($"No of secrets: {keyVals.Count}");

    if (!keyVals.Any())
    {
        Console.WriteLine("No of secrets have been imported.");
        return;
    }

    foreach (var kv in keyVals)
    {
        await secretClient.SetSecretAsync(kv.Key, kv.Value);
        Console.WriteLine($"Secret synced to KV : {kv.Key}");
    }
}

