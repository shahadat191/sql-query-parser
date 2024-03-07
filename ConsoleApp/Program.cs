using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenAI_API;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Reflection.Metadata;
using ConsoleApp.Expressions;
using System.Text;
using ConsoleApp.Tokenizer2;
using static ConsoleApp.Expressions.AndExpression;
using System.Linq.Expressions;

namespace ConsoleApp
{
    public class MyModel
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }


    public class NestedValueConfigurationConverter : JsonConverter<NestedValueConfiguration>
    {
        public override NestedValueConfiguration ReadJson(JsonReader reader, Type objectType, NestedValueConfiguration existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                JObject jsonObject = JObject.Load(reader);
                if (jsonObject.TryGetValue("$type", out JToken typeToken))
                {
                    string typeName = typeToken.ToString();
                    return jsonObject.ToObject(Type.GetType(typeName)) as NestedValueConfiguration;
                }
                var configuration = jsonObject.ToObject<NestedValueConfiguration>();
                configuration.NestedAttributeType = NestedAttributeType.Constant;
                return configuration;
            }
            catch (Exception ex)
            {
                throw new JsonException("Error while deserializing NestedValueConfiguration", ex);
            }

        }

        public override void WriteJson(JsonWriter writer, NestedValueConfiguration value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanWrite => false; // Indicate that writing is supported

    }


    internal class Program
    {
        static void Main(string[] args)
        {
            var tokens = Tokenizer2.Tokenizer.Tokenize("(T1 >= 35 AND (T1 <= 45 And T1 = 67)) OR (T3 = 'Intern' AND (T2 = 45 AND T2 = 67)) ");

            foreach (var token in tokens)
            {
                Console.Write($"Type: {token.Type}, ");
                if(token is PredicateToken predicate)
                {
                    Console.WriteLine($"       : {predicate}");
                }
                else if(token is Tokenizer2.Token normalToken)
                {
                    Console.WriteLine($" : {normalToken.Value}");
                }

            }

            var expression = Tokenizer2.ExpressionParser.ParseTokens(tokens);
            Expressions.ExpressionParser.PrintExpressionTree(expression);


            var (isValid, queriesDict) = CheckQueries(expression);

            Console.WriteLine(isValid);
            foreach (var item in queriesDict)
            {
                Console.WriteLine(item.Key);
                Expressions.ExpressionParser.PrintExpressionTree(item.Value);
                Console.WriteLine("");

            }

            /*            var tokenizer = new Tokenizer();
                        var inputs = new List<string>{
                             "((T1 >= 35 AND T1 <= 45) OR (T2 < 35 AND T2 >= 10)) AND T3 == 'Intern'"
                             };

                        foreach (var input in inputs)
                        {
                            var tokens = tokenizer.Tokenize(input);

                            var parseTokens = ExpressionParser.ParseTokens(tokens);
                            ExpressionParser.PrintExpressionTree(parseTokens);
                            Console.WriteLine("");

                        }*/

            //string filePath = @"D:\IQVIA\enhanced-view\src\temp-snowflake.sql";
            //string content = File.ReadAllText(filePath);

            //// Normalize newlines to Unix style
            //string modifiedContent = content.Replace("\r\n", "\n");

            //// Convert the modified content to bytes
            //byte[] contentBytes = Encoding.UTF8.GetBytes(modifiedContent);

            //// Write the bytes to the file, bypassing newline normalization
            //using (var fileStream = new FileStream(@"D:\IQVIA\enhanced-view\src\temp-snowflake2.sql", FileMode.Create))
            //{
            //    fileStream.Write(contentBytes, 0, contentBytes.Length);
            //}
        }

        private static (bool isValid, Dictionary<string, IExpression> queriesDict) CheckQueries(IExpression expression)
        {
            switch (expression)
            {
                case PredicateExpression predicateExpression:
                    return (true, new Dictionary<string, IExpression> { { predicateExpression.PropertyName, expression } });

                case OrExpression orExpression:
                    return MergeQueries(orExpression, (left, right) => new OrExpression(left, right));

                case AndExpression andExpression:
                    return MergeQueries(andExpression, (left, right) => new AndExpression(left, right));

                default:
                    // Handling of unsupported expressions can be decided here.
                    return (false, new Dictionary<string, IExpression>());
            }
        }

        private static (bool isValid, Dictionary<string, IExpression> queriesDict) MergeQueries(LogicalExpression expression, Func<IExpression, IExpression, IExpression> mergeExpression)
        {
            var (leftValid, leftQueryDict) = CheckQueries(expression.Expr1);
            var (rightValid, rightQueryDict) = CheckQueries(expression.Expr2);

            var queryDict = new Dictionary<string, IExpression>();
            var unionKeys = leftQueryDict.Keys.Union(rightQueryDict.Keys);

            foreach (var key in unionKeys)
            {
                if (leftQueryDict.TryGetValue(key, out var leftValue) && rightQueryDict.TryGetValue(key, out var rightValue))
                {
                    queryDict.Add(key, mergeExpression(leftValue, rightValue));
                }
                else
                {
                    queryDict.Add(key, leftQueryDict.ContainsKey(key) ? leftQueryDict[key] : rightQueryDict[key]);
                }
            }

            bool isValid = leftValid && rightValid && (unionKeys.Count() == 1 || unionKeys.Count() == leftQueryDict.Count + rightQueryDict.Count);
            return (isValid, queryDict);
        }

        private static async Task<IEnumerable<string>> GetXmlDocuments(string selectedText)
        {
            var apiKey = "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";

            OpenAIAPI api = new OpenAIAPI(apiKey); // shorthand

            var chat = api.Chat.CreateConversation();

            var prompt = $"Generate XML documentation for this {selectedText}";

            chat.AppendUserInput(prompt);
            string response = await chat.GetResponseFromChatbotAsync();

            // Extract and print the XML comments
            var xmlComments = ExtractXMLComments(response);
            return xmlComments;
        }

        static IEnumerable<string> ExtractXMLComments(string input)
        {
            string pattern = @"///(.*?)\n";

            // Match XML comments in the input string
            MatchCollection matches = Regex.Matches(input, pattern, RegexOptions.Singleline);

            // Extract and return the XML comments
            return matches.Select(match => match.Groups[0].Value.Trim());
        }

        static void ExtractAndWriteColumns(string inputFile, string outputFile, List<string> columnsToExtract)
        {
            string[] inputCsvLines = File.ReadAllLines(inputFile);
            List<string[]> extractedData = new List<string[]>();

            var columnNames = inputCsvLines.First().Split(',').Select(field => field.Trim('\"')).ToList();
            var extractedColumnIndices = columnsToExtract.Select(q => columnNames.IndexOf(q));

            foreach (var line in inputCsvLines.Skip(1))
            {
                string[] row = line.Split(',').ToArray();
                if (row.Length >= columnsToExtract.Count)
                {
                    string[] extractedRow = extractedColumnIndices.Select(col => row[col]).Select(field => field.Trim('\"')).ToArray();
                    extractedData.Add(extractedRow);
                }
            }

            WriteExtractedDataToCsv(outputFile, columnsToExtract, extractedData);
        }

        static void WriteExtractedDataToCsv(string outputFile, List<string> columnsToExtract, List<string[]> extractedData)
        {
            using (var writer = new StreamWriter(outputFile))
            {
                writer.WriteLine(string.Join(",", columnsToExtract));
                foreach (var row in extractedData)
                {
                    writer.WriteLine(string.Join(",", row));
                }
            }
        }

        public static void ReadFile(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                string content = reader.ReadToEnd();
                Console.WriteLine(content);
            }
        }
    }




    public enum AttributeType
    {
        Direct,
        Nested
    }

    public class ExtendedAttributeConfiguration
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ConfigurationId { get; set; }
        public AttributeType Type { get; protected set; }
    }

    public class DirectAttributeConfiguration : ExtendedAttributeConfiguration
    {
        public DirectAttributeConfiguration()
        {
            Type = AttributeType.Direct;
        }

        public string ColumnName { get; set; }
    }

    public class NestedAttributeConfiguration : ExtendedAttributeConfiguration
    {
        public NestedAttributeConfiguration()
        {
            Type = AttributeType.Nested;
        }

        public ICollection<NestedValueConfiguration> NestedAttributeValues { get; set; } = new List<NestedValueConfiguration>();
    }

    public class NestedValueConfiguration
    {
        public string SubAttributeName { get; set; }
        public string DefaultValue { get; set; }
        public AttributeType DataType { get; set; }
        public NestedAttributeType NestedAttributeType { get; set; }
        public bool IsRequired { get; set; }
    }

    public class ConstantNestedValueConfiguration : NestedValueConfiguration
    {
        public ConstantNestedValueConfiguration()
        {
            NestedAttributeType = NestedAttributeType.Constant;
        }
    }

    public class LookupNestedValueConfiguration : NestedValueConfiguration
    {
        public LookupNestedValueConfiguration()
        {
            NestedAttributeType = NestedAttributeType.Lookup;
        }

        public string ColumnName { get; set; }
        public List<string> DropdownValue { get; set; } = new List<string>();
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum NestedAttributeType
    {
        /// <summary>
        /// Constant.
        /// </summary>
        [EnumMember(Value = "Constant")]
        Constant = 0,

        /// <summary>
        /// Lookup.
        /// </summary>
        [EnumMember(Value = "Lookup")]
        Lookup = 1,
    }
}
