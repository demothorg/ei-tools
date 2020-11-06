using System;
using System.IO;
using System.Collections.Generic;

namespace LiaMprFix
{
    class Program
    {
        static void Main(string[] args)
        {
            var mprFiles = Directory.GetFiles(".", "*.mpr");
            if (mprFiles.Length == 0)
            {
                Console.WriteLine("No .mpr files found in the current directory. Nothing to do");
                Console.WriteLine("Press ENTER to exit");
                Console.ReadLine();
                return;
            }

            foreach (var mprName in mprFiles)
            {
                var mpr = new EILib.MprFile();
                try
                {
                    mpr.Load(mprName);
                }
                catch (InvalidDataException e)
                {
                    Console.WriteLine("ERROR: Failed to read {0}: {1}", mprName, e.Message);
                    continue;
                }

                var badTiles = new HashSet<int>();
                for (var i = 0; i < mpr.TileTypes.Length; i++)
                {
                    if (mpr.TileTypes[i] >= EILib.ETileType.Last)
                    {
                        mpr.TileTypes[i] = EILib.ETileType.Road;
                        badTiles.Add(i);
                    }
                }
                if (badTiles.Count > 0)
                {
                    bool usesBadTiles = false;
                    for (var i = 0; i < mpr.LandTiles.GetLength(0); i++)
                    {
                        for (var j = 0; j < mpr.LandTiles.GetLength(1); j++)
                        {
                            if (badTiles.Contains(mpr.LandTiles[i, j].Index))
                            {
                                usesBadTiles = true;
                                break;
                            }

                        }
                    }

                    mpr.Save(mprName);

                    if (usesBadTiles)
                        Console.WriteLine("{0} has bad tiles and was fixed!", mprName);
                    else
                        Console.WriteLine("{0} has bad tiles but doesn't use them... Fixed anyway!", mprName);
                }
                else
                    Console.WriteLine("{0} is already good", mprName);
            }

            Console.WriteLine("All .mpr files were successfuly checked!");
            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }
    }
}
