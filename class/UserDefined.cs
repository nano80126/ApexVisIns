using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApexVisIns
{
    public partial class MainWindow : System.Windows.Window
    {
#if BASLER
        public static int CAM_WIDTH => 2040;
        public static int CAM_HEIGHT => 2040;
        public static double CAM_FPS => 12;
        public static double CAM_EXPOSURE => 10000;
#elif UVC
#if APEX    // Apex 倒角
        public static int CAM_WIDTH => 1600;
        public static int CAM_HEIGHT => 1200;
        public static double CAM_FPS => 12;
        public static double CAM_EXPOSURE => -8;

        //private Apex.ChamferDirection ChamferDirection = Apex.ChamferDirection.Left;
        //private bool ApexAnalyzeRequest = false;

#elif COIL    // 鈹銅線圈
        public static int CAM_WIDTH => 1920;
        public static int CAM_HEIGHT => 1080;
        public static double CAM_FPS => 30;
        public static double CAM_EXPOSURE => -6;
#endif
#endif
    }
}
