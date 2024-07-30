# Reachto2ALights
A pair of C# programs designed to transfer all static and dynamic light data from a Reach `.scenario_structure_lighting_info` tag to a H2AMP one, for the purpose of matching lighting when porting BSPs.
Uses two separate executables as I can't run both Reach and 2AMP ManagedBlam from the same program

# Requirements
* Requires [.NET 4.8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48)

# Usage
- Download the latest zip from the [releases tab](https://github.com/Pepper-Man/Reachto2ALightConverter/releases)
- The folder structure in the zip shows you where to place each .exe file - one in `HREK\bin` and the other in `H2AMPEK\bin`.
- Run the executable in `HREK\bin`. Provide the full filepath for the Reach `.scenario_structure_lighting_info` tag you want to copy the data from.
- The program will run and produce a `.json` file in the same folder.
- Run the second executable in `H2AMPEK\bin`. First provide the full filepath for the H2AMP `.scenario_structure_lighting_info` tag you want to copy the data into. Then provide the full filepath to the `.json` file the previous executable made.
- The program should now successfully put the data into the tag. That's it! Light the BSP to see the results.

# Credits
- Thanks to [ILRepack](https://github.com/gluck/il-repack) useful library for merging .dlls into the executable for easier portability.
- The code uses ManagedBlam.dll (not distributed or kept in this repo in any form) which is property of Bungie/Microsoft/343 Industries.