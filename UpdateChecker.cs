using MMOG;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;

namespace MMOG
{
    public class UpdateChecker
    {
        public static int ElementsCount = 0;
        public static UPDATE_NOTES SERVER_UPDATE_VERSIONS;
        public static string serverJsonFile;
        public static bool isReady = false;

        //public static void IniciateData_TEST() {
        //    if (isReady) return;
        //    isReady = true;

        //    SERVER_UPDATE_VERSIONS = new UPDATE_NOTES();
        //    SERVER_UPDATE_VERSIONS._Data =
        //        new DATA() {
        //            _Locations = new List<Locations>()
        //            {
        //                new Locations() {
        //                    _Id = 0,
        //                    _Name = "Start_First_Floor",
        //                    _Coordinates = new Vector3_json(7, -2, 14),
        //                    _Type = new List<Maptypes>()
        //                {
        //                        new Maptypes() {
        //                            _Type = "Ground_MAP",
        //                            _Version = 1001
        //                        },
        //                        new Maptypes() {
        //                            _Type = "Obstacle_MAP",
        //                            _Version = 1001
        //                        }
        //                    }
        //                },
        //                new Locations() {
        //                    _Id = 1,
        //                    _Name = "Start_Second_Floor",
        //                    _Coordinates = new Vector3_json(7, -2, 14),

        //                    _Type = new List<Maptypes>()
        //                {
        //                        new Maptypes() {
        //                            _Type = "Ground_MAP",
        //                            _Version = 1001
        //                        },
        //                        new Maptypes() {
        //                            _Type = "Obstacle_MAP",
        //                            _Version = 1001
        //                        }
        //                    }
        //                }
        //            },
        //            _Items = new List<Items>() {
        //                new Items(
        //                    id: (int)ITEMS.Armor,
        //                    name: ITEMS.Armor.ToString(),
        //                    type: "Wearable"
        //                ),
        //                new Items(
        //                    id: (int)ITEMS.Stone,
        //                    name: ITEMS.Stone.ToString(),
        //                    type: "Trash"
        //                ),
        //                new Items(
        //                    id : (int)ITEMS.Health_Potion,
        //                    name : ITEMS.Health_Potion.ToString(),
        //                    type :"Consumable"
        //                )
        //            }
        //        };
        //    SaveChangesToFile();
        //}

        public static void SaveChangesToFile() {
            serverJsonFile = (JsonSerializer.Serialize(SERVER_UPDATE_VERSIONS)).ToString();

            using (FileStream fs = new FileStream(Constants.PATH_NOTES_SERVER, FileMode.Create)) {
                using (TextWriter tw = new StreamWriter(fs)) {
                    tw.WriteAsync(serverJsonFile);
                }
            }
        }
        public static void ReadDataFromFile() {
            serverJsonFile = File.ReadAllText(Constants.PATH_NOTES_SERVER);
           
            SERVER_UPDATE_VERSIONS = JsonSerializer.Deserialize<UPDATE_NOTES>(serverJsonFile);
            
        }

        public static void ChangeRecord(ITEMS _item, Items updatedItem) {
            updatedItem.UpdateVersionNumber();
            UpdateChecker.SaveChangesToFile();
        }
        public static void ChangeRecord(LOCATIONS _location, MAPTYPE _maptype, Maptypes Updatedmaptype ) {
            Updatedmaptype.UpdateVersionNumber();
            UpdateChecker.SaveChangesToFile();
        }
        public static int GetVersionOf(LOCATIONS _location, MAPTYPE _maptype, DATATYPE _datatype = DATATYPE.Locations) => SERVER_UPDATE_VERSIONS._Data[_location][_maptype]._Version;
        public static int GetVersionOf(ITEMS _item, DATATYPE _datatype = DATATYPE.Items) => SERVER_UPDATE_VERSIONS._Data[_item]._Version;
    }
}