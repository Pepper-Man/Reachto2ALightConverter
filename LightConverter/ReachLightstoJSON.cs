using Bungie;
using Bungie.Tags;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LightConverter
{
    class LightDefData
    {
        public string Type { get; set; }
        public uint Flags { get; set; }
        public float[] Colour { get; set; }
        public float Intensity { get; set; }
        public float[] AttenBounds { get; set; }
    }

    class LightInstData
    {
        public long DefIndex { get; set; }
        public float[] Origin { get; set; }
        public float[] Forward { get; set; }
        public float[] Up { get; set; }
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

            var tagFile = new TagFile();
            var tagPath = TagPath.FromPathAndExtension(@"levels\dlc\cex_headlong\cex_headlong", "scenario_structure_lighting_info");
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
                    int lightType = ((TagFieldEnum)tagFile.SelectField($"Block:generic light definitions[{i}]/ShortEnum:type")).Value;
                    switch (lightType)
                    {
                        case 0:
                            lightInfo.Type = "omni";
                            break;
                        case 1:
                            lightInfo.Type = "spot";
                            break;
                        case 2:
                            lightInfo.Type = "directional";
                            break;
                    }

                    // Get light definition flags
                    lightInfo.Flags = ((TagFieldFlags)tagFile.SelectField($"Block:generic light definitions[{i}]/WordFlags:flags")).RawValue;

                    // Get light definition colour
                    lightInfo.Colour = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:generic light definitions[{i}]/RealRgbColor:color")).Data;

                    // Get light definition intensity
                    lightInfo.Intensity = ((TagFieldElementSingle)tagFile.SelectField($"Block:generic light definitions[{i}]/Real:intensity")).Data;

                    // Get light definition falloff
                    lightInfo.AttenBounds = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:generic light definitions[{i}]/Realbounds:far attenuation bounds")).Data;

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

                    Console.WriteLine($"Obtained light instance {j} info");
                }
            }
            finally
            {
                // Gracefully close tag file
                tagFile.Dispose();
                Console.WriteLine("Tagfile closed");
                Console.ReadLine();
            }
        }
    }
}
