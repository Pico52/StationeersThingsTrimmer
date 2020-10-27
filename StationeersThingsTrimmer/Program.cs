using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace StationeersThingsTrimmer
{
    internal class Program
    {
        #region Declarations

        private static readonly string FileLocation = Environment.CurrentDirectory;
        private static readonly string FileName = "world.xml";
        private static readonly bool DebugIsOn = false;

        private static List<XElement> pipeNetworkIds,
                                      cableNetworkIds,
                                      chuteNetworkIds,
                                      thingSaveData,
                                      savedPipeNetworkIds,
                                      savedCableNetworkIds,
                                      savedChuteNetworkIds,
                                      savedThings;

        //savedRooms,
        //savedAtmospheres;
        private static IEnumerable<XElement> worldData;

        //atmosphereSaveData,
        //roomSaveData;
        private static XDocument xmlDoc;

        private static float minX,
                             maxX,
                             minZ,
                             maxZ;

        #endregion Declarations

        private static void Main()
        {
            #region Initializations

            try
            {
                xmlDoc = XDocument.Load(Path.Combine(FileLocation, FileName));
            }
            catch (System.IO.FileNotFoundException)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("ERROR:");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($"Could not find ");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write($"\'{FileName}\'");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($" within current directory: \n");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write($"\"{FileLocation}\"\n");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($"\nPlease place the ");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write($"\'{FileName}\'");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($" save file within the same directory as this .exe file.\n" +
                              $"\n" +
                              $"Press any key to exit..");
                Console.ReadKey();
                return;
            }

            // Lists of objects and IDs that will need to be saved.
            savedPipeNetworkIds = new List<XElement>();
            savedCableNetworkIds = new List<XElement>();
            savedChuteNetworkIds = new List<XElement>();
            savedThings = new List<XElement>();

            // Load the descendant data from the world.xml document.
            worldData = xmlDoc.Descendants("WorldData");
            thingSaveData = worldData.Elements("Things").Elements("ThingSaveData").ToList();

            // Load element data for counting what will be removed.
            pipeNetworkIds = worldData.Elements("PipeNetworks").Elements("NetworkId").ToList();
            cableNetworkIds = worldData.Elements("CableNetworks").Elements("NetworkId").ToList();
            chuteNetworkIds = worldData.Elements("ChuteNetworks").Elements("NetworkId").ToList();
            //atmosphereSaveData = worldData.Elements("Atmospheres").Elements("AtmosphereSaveData");
            //roomSaveData = worldData.Elements("Rooms").Elements("Room");

            // Bounds for all Things to be saved.  This is where the user's base is.
            Console.Write("Enter world position coordinates that you want to be saved:\n");
            bool isNumber;
            do
            {
                Console.Write("Minimum X: ");
                string input = Console.ReadLine();
                isNumber = float.TryParse(input, out minX);
                if (!isNumber)
                {
                    Console.WriteLine("Minimum X position must be a number.");
                }
            } while (!isNumber);

            do
            {
                Console.Write("Maximum X: ");
                string input = Console.ReadLine();
                isNumber = float.TryParse(input, out maxX);
                if (!isNumber)
                {
                    Console.WriteLine("Maximum X position must be a number.");
                }
            } while (!isNumber);

            do
            {
                Console.Write("Minimum Z: ");
                string input = Console.ReadLine();
                isNumber = float.TryParse(input, out minZ);
                if (!isNumber)
                {
                    Console.WriteLine("Minimum Z position must be a number.");
                }
            } while (!isNumber);

            do
            {
                Console.Write("Maximum Z: ");
                string input = Console.ReadLine();
                isNumber = float.TryParse(input, out maxZ);
                if (!isNumber)
                {
                    Console.WriteLine("Maximum Z position must be a number.");
                }
            } while (!isNumber);

            #endregion Initializations

            Run();
        }

        private static void Run()
        {
            #region Determine What To Save

            // These lists of integers will be used for calculations and conversions before being put into XML format.
            List<int> tempPipes = new List<int>();
            List<int> tempCables = new List<int>();
            List<int> tempChutes = new List<int>();

            foreach (XElement thing in thingSaveData)
            {
                // Determine the world position of this Thing.
                XElement worldPos = thing.Element("WorldPosition");
                float x = float.Parse(worldPos.Element("x").Value);
                float z = float.Parse(worldPos.Element("z").Value);

                // Check if this Thing is outside the user-defined bounds to be saved.
                if (x < minX || x > maxX || z < minZ || z > maxZ) continue;

                // Save this Thing.
                savedThings.Add(thing);

                // Check if Thing is a Pipe, Cable, or Chute. Pull its info if it is, otherwise remain 0.
                XElement pipeElement = thing.Element("PipeNetworkId");
                XElement cableElement = thing.Element("CableNetworkId");
                XElement chuteElement = thing.Element("ChuteNetworkId");
                string pipeIdValue = pipeElement?.Value ?? "0";
                string cableIdValue = cableElement?.Value ?? "0";
                string chuteIdValue = chuteElement?.Value ?? "0";

                // Convert to integer from string.
                int pipeId = Int32.Parse(pipeIdValue);
                int cableId = Int32.Parse(cableIdValue);
                int chuteId = Int32.Parse(chuteIdValue);

                // Add IDs of networks to be saved.  If it is 0, it wasn't one of those types.
                if (pipeId != 0) tempPipes.Add(pipeId);
                if (cableId != 0) tempCables.Add(cableId);
                if (chuteId != 0) tempChutes.Add(chuteId);
            }

            // Shrinks list to only unique sets of networks.
            tempPipes = tempPipes.Distinct().ToList();
            tempCables = tempCables.Distinct().ToList();
            tempChutes = tempChutes.Distinct().ToList();

            // Turn the integers of Ids into properly formatted XML elements.
            foreach (int pipe in tempPipes)
            {
                savedPipeNetworkIds.Add(new XElement("NetworkId", pipe));
            }
            foreach (int cable in tempCables)
            {
                savedCableNetworkIds.Add(new XElement("NetworkId", cable));
            }
            foreach (int chute in tempChutes)
            {
                savedChuteNetworkIds.Add(new XElement("NetworkId", chute));
            }

            #endregion Determine What To Save

            #region Debug

            // Let the user know which pipe, cable, and chute networks are recognized to be saved.
            if (DebugIsOn)
            {
                Console.WriteLine("<=====DEBUG=====>\nPipes:");
                foreach (XElement pipe in savedPipeNetworkIds)
                {
                    Console.Write($"{pipe.Value} ");
                }
                Console.WriteLine("\n\nCables:");
                foreach (XElement cable in savedCableNetworkIds)
                {
                    Console.Write($"{cable.Value} ");
                }
                Console.WriteLine("\n\nChutes:");
                foreach (XElement chute in savedChuteNetworkIds)
                {
                    Console.Write($"{chute.Value} ");
                }
                Console.WriteLine("<===============>\n");
            }

            #endregion Debug

            #region Update

            // Removing everything.
            xmlDoc.Descendants("WorldData").Elements("Things").Elements("ThingSaveData").Remove();
            xmlDoc.Descendants("WorldData").Elements("PipeNetworks").Elements("NetworkId").Remove();
            xmlDoc.Descendants("WorldData").Elements("CableNetworks").Elements("NetworkId").Remove();
            xmlDoc.Descendants("WorldData").Elements("ChuteNetworks").Elements("NetworkId").Remove();

            // To-do: Remove atmospheres and rooms?

            // Adding saved objects back in.
            xmlDoc.Element("WorldData").Element("PipeNetworks").Add(savedPipeNetworkIds);
            xmlDoc.Element("WorldData").Element("CableNetworks").Add(savedCableNetworkIds);
            xmlDoc.Element("WorldData").Element("ChuteNetworks").Add(savedChuteNetworkIds);
            xmlDoc.Element("WorldData").Element("Things").Add(savedThings);

            xmlDoc.Save(Path.Combine(FileLocation, FileName));

            #endregion Update

            #region Display Results

            // Count up everything that's been removed.
            int thingsRemoved = thingSaveData.Count - savedThings.Count;
            int pipesRemoved = pipeNetworkIds.Count - savedPipeNetworkIds.Count;
            int cablesRemoved = cableNetworkIds.Count - savedCableNetworkIds.Count;
            int chutesRemoved = chuteNetworkIds.Count - savedChuteNetworkIds.Count;
            //int atmospheresRemoved = atmospheres.Count - savedAtmospheres.Count;
            //int roomsRemoved = rooms.Count - savedRooms.Count;

            // Display it all to the console.
            Console.WriteLine($"\n<========ORIGINAL========>\n" +
                              $"Things:         {thingSaveData.Count}\n" +
                              $"Pipe Networks:  {pipeNetworkIds.Count}\n" +
                              $"Cable Networks: {cableNetworkIds.Count}\n" +
                              $"Chute Networks: {chuteNetworkIds.Count}\n" +
                              //$"Atmospheres:  {atmospheres.Count}\n" +
                              //$"Rooms:        {rooms.Count}\n\n" +
                              $"<=========SAVED=========>\n" +
                              $"Things:         {savedThings.Count}\n" +
                              $"Pipe Networks:  {savedPipeNetworkIds.Count}\n" +
                              $"Cable Networks: {savedCableNetworkIds.Count}\n" +
                              $"Chute Networks: {savedChuteNetworkIds.Count}\n" +
                              //$"Atmospheres:  {savedAtmospheres.Count}\n" +
                              //$"Rooms:        {savedRooms.Count}\n\n" +
                              $"<========REMOVED========>\n" +
                              $"Things:         {thingsRemoved}\n" +
                              $"Pipe Networks:  {pipesRemoved}\n" +
                              $"Cable Networks: {cablesRemoved}\n" +
                              $"Chute Networks: {chutesRemoved}\n" +
                              //$"Atmospheres:  {atmospheresRemoved}\n" +
                              //$"Rooms:        {roomsRemoved}\n" +
                              $"<=======================>\n");

            // Give the user a chance to see the output.
            Console.Write("Press any key to exit..");
            Console.ReadKey();

            #endregion Display Results
        }
    }
}