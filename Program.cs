// See https://aka.ms/new-console-template for more information

using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;



var azureKeyVaultUri = "";
var secretClient = new SecretClient(new Uri(azureKeyVaultUri), new DefaultAzureCredential());

// Get all Secret keys from KV
var secretProps = secretClient.GetPropertiesOfSecrets();
var stringBuilder = new StringBuilder();
foreach (var secretProp in secretProps)
{
    stringBuilder.AppendLine(secretProp.Name);
}

// Export file to csv
var outputDir = Path.Combine(AppContext.BaseDirectory, "Outputs");

if (!Directory.Exists(outputDir))
{
    Directory.CreateDirectory(outputDir);
}

File.WriteAllText(Path.Combine(outputDir, "SecretKeys.csv"), stringBuilder.ToString());

//
// var streamReader = new StreamReader("file.csv");
// var keyVals = new List<KeyValuePair<string, string>>();
//
// while (!streamReader.EndOfStream)
// {
//     var splits = streamReader.ReadLine().Split(';');
//     keyVals.Add(new KeyValuePair<string, string>(splits[0].Trim(), splits[1].Trim()));
// }
//
// Console.WriteLine($"No of secrets: {keyVals.Count}");
//
//
//
//
// foreach (var kv in keyVals)
// {
//     await secretClient.SetSecretAsync(kv.Key, kv.Value);
//     Console.WriteLine($"Secret sync to KV : {kv.Key}");
// }
//
