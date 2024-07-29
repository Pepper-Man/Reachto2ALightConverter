using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using Corinth;
using Corinth.Tags;
using System.Xml.Linq;

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

    internal class JSONto2ALights
    {
        static void Main(string[] args)
        {
            string jsonPath = "I:\\SteamLibrary\\steamapps\\common\\HREK\\cex_headlong_lightdata.json";

            // Read JSON from file
            string json = File.ReadAllText(jsonPath);

            // Deserialize JSON to object
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            LightDataContainer lightDataContainer = JsonSerializer.Deserialize<LightDataContainer>(json, options);

            // ManagedBlam Initialisation
            string h2ampek = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\H2AMPEK";
            void callback(ManagedBlamCrashInfo LambdaExpression) { }
            ManagedBlamStartupParameters startupParams = new ManagedBlamStartupParameters
            {
                InitializationLevel = InitializationType.TagsOnly
            };
            ManagedBlamSystem.Start(h2ampek, callback, startupParams);
            var tagFile = new TagFile();
            var pathString = @"levels\pepper\cex_headlong\cex_headlong_headlong";
            var tagPath = TagPath.FromPathAndExtension(pathString, "scenario_structure_lighting_info");

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
