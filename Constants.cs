using System;
using System.Collections.Generic;
using System.Text;

namespace MMOG
{
    class Constants
    {
        public const int TICKS_PER_SEC = 30;
        public const float MS_PER_TICK = 1000f / TICKS_PER_SEC;
        //public const string MAP_DATA_FILE_PATH = @"D:\MMOG-SERVER\MAPDATA.txt";
        public const string MAP_DATA_FILE_PATH = @"D:\Programowanie\Unity\MMOG-Client\PC_Build\OBSTACLE_MAPDATA_SERVER.txt";
        public const string GROUND_MAP_DATA_FILE_PATH = @"D:\Programowanie\Unity\MMOG-Client\PC_Build\GROUND_MAPDATA_SERVER.txt";
        
        public static int TIME_IN_SEC_TO_RESPOND_BEFORE_KICK = 10;
    }
}
