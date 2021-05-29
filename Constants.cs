using System;
using System.Collections.Generic;
using System.Text;

namespace MMOG
{
    class Constants
    {
        public const int TICKS_PER_SEC = 30;
        public const float MS_PER_TICK = 1000f / TICKS_PER_SEC;
        public static int TIME_IN_SEC_TO_RESPOND_BEFORE_KICK = 5;
    }
}
