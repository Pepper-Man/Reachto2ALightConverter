using Bungie;
using Bungie.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;

namespace LightConverter
{
    public class LightDefData
    {
        public int Type { get; set; }
        public uint Flags { get; set; }
        public float[] Colour { get; set; }
        public float Intensity { get; set; }
        public float[] AttenBounds { get; set; }
    }

    public class LightInstData
    {
        public long DefIndex { get; set; }
        public float[] Origin { get; set; }
        public float[] Forward { get; set; }
        public float[] Up { get; set; }
    }

    public class LightDataContainer
    {
        public List<LightDefData> LightDefinitions { get; set; }
        public List<LightInstData> LightInstances { get; set; }
    }

    public class FilePathSanitiser
    {
        // Define the invalid path characters
        private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();

        public static string SanitiseFilePath(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Input file path cannot be null or whitespace.");
                return "";
            }

            // Trim whitespace and quotes
            string sanitisedPath = input.Trim().Trim('"');

            // Check for invalid characters
            if (sanitisedPath.IndexOfAny(InvalidPathChars) >= 0)
            {
                Console.WriteLine("Input file path contains invalid characters.");
                return "";
            }

            // Get the absolute path to ensure it's well-formed
            try
            {
                sanitisedPath = Path.GetFullPath(sanitisedPath);
            }
            catch
            {
                Console.WriteLine("Input file path is not valid.");
                return "";
            }

            // Check file exists
            if (!File.Exists(sanitisedPath))
            {
                Console.WriteLine("Input file does not exist.");
                return "";
            }

            // Check file is in HREK
            if (!sanitisedPath.Contains("HREK\\tags"))
            {
                Console.WriteLine("Input file is not in the HREK tags folder.");
                return "";
            }
            
            // Check correct tag type
            if (Path.GetExtension(sanitisedPath) != ".scenario_structure_lighting_info")
            {
                Console.WriteLine("Input file is not a .scenario_structure_lighting_info tag.");
                return "";
            }

            return sanitisedPath;
        }
    }

    internal class ReachLightstoJSON
    {
        static void Main(string[] args)
        {
            string userTagPath;
            while (true)
            {
                Console.WriteLine("Enter full path to Reach .scenario_structure_lighting_info tag:\n");
                string userInput = Console.ReadLine();
                userTagPath = FilePathSanitiser.SanitiseFilePath(userInput);
                if (userTagPath != "")
                {
                    Console.WriteLine("Valid path entered");
                    break;
                }
            }

            // Get HREK path
            int hrekIndex = userTagPath.IndexOf("HREK");
            string hrek = userTagPath.Substring(0, hrekIndex + 4);

            // Get relative tag path
            string relativePath = Path.ChangeExtension(userTagPath.Substring(hrekIndex + 10), null);
            

            // ManagedBlam Initialisation
            void callback(ManagedBlamCrashInfo LambdaExpression) { }
            ManagedBlamStartupParameters startupParams = new ManagedBlamStartupParameters
            {
                InitializationLevel = InitializationType.TagsOnly
            };
            ManagedBlamSystem.Start(hrek, callback, startupParams);

            List<LightDefData> lightDefData = new List<LightDefData>();
            List<LightInstData> lightInstData = new List<LightInstData>();

            var tagFile = new TagFile();
            var tagPath = TagPath.FromPathAndExtension(relativePath, "scenario_structure_lighting_info");
            try
            {
                tagFile.Load(tagPath);
                Console.WriteLine("Tagfile opened");
                Console.WriteLine("\nReading light definition data\n");

                // Get total number of light definitions
                int lightDefCount = ((TagFieldBlock)tagFile.SelectField("Block:generic light definitions")).Elements.Count();

                for (int i = 0; i < lightDefCount; i++)
                {
                    LightDefData lightInfo = new LightDefData();

                    // Get light definition type
                    lightInfo.Type = ((TagFieldEnum)tagFile.SelectField($"Block:generic light definitions[{i}]/ShortEnum:type")).Value;

                    // Get light definition flags
                    lightInfo.Flags = ((TagFieldFlags)tagFile.SelectField($"Block:generic light definitions[{i}]/WordFlags:flags")).RawValue;

                    // Get light definition colour
                    lightInfo.Colour = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:generic light definitions[{i}]/RealRgbColor:color")).Data;

                    // Get light definition intensity
                    lightInfo.Intensity = ((TagFieldElementSingle)tagFile.SelectField($"Block:generic light definitions[{i}]/Real:intensity")).Data;

                    // Get light definition falloff
                    lightInfo.AttenBounds = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:generic light definitions[{i}]/Realbounds:far attenuation bounds")).Data;

                    lightDefData.Add(lightInfo);
                    Console.WriteLine($"Obtained light definition {i} info");
                }

                Console.WriteLine("\nReading light instance data\n");

                // Get total number of light instances
                int lightInstCount = ((TagFieldBlock)tagFile.SelectField("Block:generic light instances")).Elements.Count();

                for (int j = 0; j < lightInstCount; j++)
                {
                    LightInstData lightInstInfo= new LightInstData();

                    // Get light definition index
                    lightInstInfo.DefIndex = ((TagFieldElementInteger)tagFile.SelectField($"Block:generic light instances[{j}]/LongInteger:definition index")).Data;

                    // Get light definition origin
                    lightInstInfo.Origin = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:generic light instances[{j}]/RealPoint3d:origin")).Data;

                    // Get light definition forward
                    lightInstInfo.Forward = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:generic light instances[{j}]/RealVector3d:forward")).Data;

                    // Get light definition up
                    lightInstInfo.Up = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:generic light instances[{j}]/RealVector3d:up")).Data;

                    lightInstData.Add(lightInstInfo);
                    Console.WriteLine($"Obtained light instance {j} info");
                }
            }
            catch
            {
                Console.WriteLine("Unknown managedblam error");
            }
            finally
            {
                // Gracefully close tag file
                tagFile.Dispose();
                Console.WriteLine("\nTagfile closed\n\n");

                var lightDataContainer = new LightDataContainer
                {
                    LightDefinitions = lightDefData,
                    LightInstances = lightInstData
                };

                // Serialize to JSON
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                string json = JsonSerializer.Serialize(lightDataContainer, options);

                // Write JSON to file
                string filePath = Path.Combine(AppContext.BaseDirectory, $"{Path.GetFileName(relativePath)}_lightdata.json");
                File.WriteAllText(filePath, json);

                Console.WriteLine($"JSON data written to {filePath}");
            }
            Console.WriteLine("\nPress enter to exit");
            Console.ReadLine();
            ManagedBlamSystem.Stop();
        }
    }
}
