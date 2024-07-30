using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using Corinth;
using Corinth.Tags;

namespace JSONto2ALights
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

        public static string SanitisePath(string input, string type)
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

            // Check file is in H2AMPEK
            if (type == "tag")
            {
                if (!sanitisedPath.Contains("H2AMPEK\\tags"))
                {
                    Console.WriteLine("Input file is not in the HREK tags folder.");
                    return "";
                }
            }

            // Check correct extension
            if (type == "tag")
            {
                if (Path.GetExtension(sanitisedPath) != ".scenario_structure_lighting_info")
                {
                    Console.WriteLine("Input file is not a .scenario_structure_lighting_info tag.");
                    return "";
                }
            }
            else if (type == "json")
            {
                if (Path.GetExtension(sanitisedPath) != ".json")
                {
                    Console.WriteLine("Input file is not a .scenario_structure_lighting_info tag.");
                    return "";
                }
            }
            
            return sanitisedPath;
        }
    }

    internal class JSONto2ALights
    {
        static void Main(string[] args)
        {
            // Get H2AMP tag input from user
            string userTagPath;
            while (true)
            {
                Console.WriteLine("Enter full path to H2AMP .scenario_structure_lighting_info tag:\n");
                string userInput = Console.ReadLine();
                userTagPath = FilePathSanitiser.SanitisePath(userInput, "tag");
                if (userTagPath != "")
                {
                    Console.WriteLine("Valid path entered");
                    break;
                }
            }

            // Get H2AMPEK path
            int h2ampekIndex = userTagPath.IndexOf("H2AMPEK");
            string h2ampek = userTagPath.Substring(0, h2ampekIndex + 7);

            // Get relative tag path
            string relativePath = Path.ChangeExtension(userTagPath.Substring(h2ampekIndex + 13), null);

            // Get JSON path from user
            string jsonPath;
            while (true)
            {
                Console.WriteLine("Enter full path to .json file:\n");
                string userInput = Console.ReadLine();
                jsonPath = FilePathSanitiser.SanitisePath(userInput, "json");
                if (jsonPath != "")
                {
                    Console.WriteLine("Valid path entered");
                    break;
                }
            }

            // Read JSON from file
            string json = File.ReadAllText(jsonPath);

            // Deserialize JSON to object
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            LightDataContainer lightDataContainer = JsonSerializer.Deserialize<LightDataContainer>(json, options);

            // ManagedBlam Initialisation
            void callback(ManagedBlamCrashInfo LambdaExpression) { }
            ManagedBlamStartupParameters startupParams = new ManagedBlamStartupParameters
            {
                InitializationLevel = InitializationType.TagsOnly
            };
            ManagedBlamSystem.Start(h2ampek, callback, startupParams);
            var tagFile = new TagFile();
            var tagPath = TagPath.FromPathAndExtension(relativePath, "scenario_structure_lighting_info");

            try
            {
                tagFile.Load(tagPath);
                Console.WriteLine("Tagfile opened");

                // Output deserialized data
                Console.WriteLine("--- LIGHT DEFINITIONS ---");
                int i = 0;
                foreach (var def in lightDataContainer.LightDefinitions)
                {
                    Console.WriteLine($"Light Definition {i}: \n\tType: {def.Type}, \n\tFlags: {def.Flags}, \n\tColour: [{string.Join(", ", def.Colour)}], \n\tIntensity: {def.Intensity}, \n\tAttenBounds: [{string.Join(", ", def.AttenBounds)}]\n");
                    ((TagFieldBlock)tagFile.SelectField("Block:generic light definitions")).AddElement();

                    // Set light definition type
                    ((TagFieldEnum)tagFile.SelectField($"Block:generic light definitions[{i}]/Struct:Midnight_Light_Parameters/LongEnum:Light Type")).Value = def.Type;

                    // Set light definition flags
                    ((TagFieldFlags)tagFile.SelectField($"Block:generic light definitions[{i}]/Flags:flags")).RawValue = 10;

                    // Set light definition colour
                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:generic light definitions[{i}]/Struct:Midnight_Light_Parameters/RealRgbColor:Light Color")).Data = def.Colour;

                    // Set light definition intensity
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Block:generic light definitions[{i}]/Struct:Midnight_Light_Parameters/Struct:Intensity/Custom:Mapping")).Value.ClampRangeMin = def.Intensity;

                    // Set light definition falloff
                    ((TagFieldElementSingle)tagFile.SelectField($"Block:generic light definitions[{i}]/Struct:Midnight_Light_Parameters/Real:Distance Attenuation Start")).Data = def.AttenBounds[0] / 100;
                    ((TagFieldCustomFunctionEditor)tagFile.SelectField($"Block:generic light definitions[{i}]/Struct:Midnight_Light_Parameters/Struct:Distance Attenuation End/Custom:Mapping")).Value.ClampRangeMin = def.AttenBounds[1] / 100;
                    
                    i++;
                }

                Console.WriteLine("\n--- LIGHT INSTANCES ---");
                int j = 0;
                foreach (var inst in lightDataContainer.LightInstances)
                {
                    Console.WriteLine($"Light Instance {j}: \n\tDefIndex: {inst.DefIndex}, \n\tOrigin: [{string.Join(", ", inst.Origin)}], \n\tForward: [{string.Join(", ", inst.Forward)}], \n\tUp: [{string.Join(", ", inst.Up)}]\n");
                    ((TagFieldBlock)tagFile.SelectField("Block:generic light instances")).AddElement();

                    // Set light definition index
                    ((TagFieldElementInteger)tagFile.SelectField($"Block:generic light instances[{j}]/LongInteger:Light Definition Index")).Data = inst.DefIndex;

                    // Set light to static
                    ((TagFieldEnum)tagFile.SelectField($"Block:generic light instances[{j}]/LongEnum:light mode")).Value = 1;

                    // Set light definition origin
                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:generic light instances[{j}]/RealPoint3d:origin")).Data = inst.Origin;

                    // Set light definition forward
                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:generic light instances[{j}]/RealVector3d:forward")).Data = inst.Forward;

                    // Set light definition up
                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:generic light instances[{j}]/RealVector3d:up")).Data = inst.Up;

                    j++;
                }

            }
            catch
            {
                Console.WriteLine("Unknown managedblam error");
            }
            finally
            {
                tagFile.Save();
                tagFile.Dispose();
                ManagedBlamSystem.Stop();
                Console.WriteLine("\nSuccessfully written tag data! Press enter to exit.\n");
                Console.ReadLine();
            }
        }
    }
}
