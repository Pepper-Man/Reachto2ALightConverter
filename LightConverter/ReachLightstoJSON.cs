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

    internal class ReachLightstoJSON
    {
        static void Main(string[] args)
        {
            // ManagedBlam Initialisation
            string hrek = "I:\\SteamLibrary\\steamapps\\common\\HREK";
            void callback(ManagedBlamCrashInfo LambdaExpression) { }
            ManagedBlamStartupParameters startupParams = new ManagedBlamStartupParameters
            {
                InitializationLevel = InitializationType.TagsOnly
            };
            ManagedBlamSystem.Start(hrek, callback, startupParams);

            List<LightDefData> lightDefData = new List<LightDefData>();
            List<LightInstData> lightInstData = new List<LightInstData>();

            var tagFile = new TagFile();
            var pathString = @"levels\dlc\cex_headlong\cex_headlong";
            var tagPath = TagPath.FromPathAndExtension(pathString, "scenario_structure_lighting_info");
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
                string filePath = Path.Combine(hrek, $"{Path.GetFileName(pathString)}_lightdata.json");
                File.WriteAllText(filePath, json);

                Console.WriteLine($"JSON data written to {filePath}");
            }
            Console.ReadLine();
            ManagedBlamSystem.Stop();
        }
    }
}
