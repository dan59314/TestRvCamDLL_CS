
#define AutoLoadPlugins  //自動讀取 ./PlugIns/ 下的所有 DLL.


using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualBasic;


#region RvLib --------------------------------------- 
using RvLib.M2dTypeDefine;
using RvLib.VectTypeDefine;
using System.Security.Cryptography;

#endregion


#if AutoLoadPlugins  //自動讀取 ./PlugIns/ 下的所有 DLL.
using RvLib.RvCamDLL_PlugIn_FileIO;
//using System.Runtime.InteropServices; // 用 DllImport 需用此 命名空间
//using System.Reflection; // 使用 Assembly 类需用此 命名空间
//using System.Reflection.Emit; // 使用 ILGenerator 需用此 命名空间
#endif


namespace RvCamDLL
{
    using static System.Windows.Forms.VisualStyles.VisualStyleElement;
    using TFloat = double;


#pragma warning disable CS8618 // Members Initial in Constructure Function。
#pragma warning disable CS8601 // 可能有 Null 參考指派。
#pragma warning disable CS8604 // 取消 warnning 
#pragma warning disable CS8602 // 取消 warnning 
#pragma warning disable CS0642 // 取消 warnning 
#pragma warning disable CS0219 // 取消 warnning 
#pragma warning disable CS8600 // 取消 warnning 
#pragma warning disable CS8622 // 取消 warnning 




    public static class ClsRvCamDLL
    {
        private const string AssemblyName = "RvCamDLL.dll";
        //  exe 程式所在的資料夾下必須包含  RvCamDLL.dll, borlndmm.dll 和 [EXEs] 資料夾

        private const CallingConvention TargetCallingConvention = CallingConvention.StdCall;
        private const CharSet TargetCharSet = CharSet.Ansi;
        private const int cMaxAllocateStringSize = 8192;  //因應某些料號 Steps 過多，由 2048 -> 8192

        public const string cSupportedCadFileExtensions =
            "All files (*.*)|*.*" +
            "|RasVector Cam Project (*.rvc)|*.rvc" +
            "|CAD files (*.gbx,*.gbr,*.dxf,*.nc,*.dpf,*.gds,*.gih,*.txt)|*.gbx;*.gbr;*.dxf;*.nc;*.txt" + //"*.dpf;*.gds;*.gih;"
            "|IPC/356 (*.ipc,*.356)|*.ipc;*.356" +
            "|CAR (*.car)|*.car" ;

        #region Type Define =======================================================
        public delegate void TUpdateInterface(int aStep, int aLayer);
        #endregion

        #region Members Declation =====================================================
        public static TUpdateInterface? FUpdateInterface = default;
        public static string[] FLayerNames = [], FStepNames = [];
        public static TFloat cQuerySearchTolMm = 1.0;
        public static int FActStep = 0;
        public static int FActLayer = 0;
        private static Bitmap PaintDummyBmp;
        private static Color[] FStepColors = [
                Color.Blue, Color.Green, Color.Red, Color.DarkBlue, Color.LightGreen, Color.OrangeRed,
                Color.LightBlue, Color.LightGreen, Color.IndianRed, Color.SkyBlue, Color.GreenYellow, Color.IndianRed];
        private static Color[] FLayerColors = [
                Color.LightBlue, Color.LightGreen, Color.IndianRed, Color.SkyBlue, Color.GreenYellow, Color.IndianRed,
                Color.Blue, Color.Green, Color.Red, Color.DarkBlue, Color.LightGreen, Color.OrangeRed];
        private static Color FBackgroundColor = Color.Black;
        #endregion


        #region property =================================================================
        private static TSelectAction FSelectAction = TSelectAction.saSelect;
        public static TSelectAction SelectAction
        {
            get { return FSelectAction; }
            set { FSelectAction = value; }
        }

        private static TActionTarget FActionTarget = TActionTarget.smTVectObject;
        public static TActionTarget ActionTarget
        {
            get { return FActionTarget; }
            set { FActionTarget = value; }
        }
               
        private static TQueryObject FQueryTarget;
        public static TQueryObject QueryTarget
        {
            get { return FQueryTarget; }
            set { FQueryTarget = value; }
        }

        public static TValueUnit FDisplayUnit = TValueUnit.uMM;
        public static int FRulerPixelWidth = 30;
        public static Color FRulerColor = Color.Blue;
        public static byte FRulerAlpha = 150;
        public static bool FRulerVisible = true;
        #endregion


        #region CallBack Function-----------------------------------------------
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void TOnRunningProgress(ref byte aMainPercent, ref byte aChildPercent,
                IntPtr sMainTask, IntPtr sSubTask);
        public delegate void TOnLogInfo(IntPtr sLogInfo);

        private static string sCallback =
          string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n{7}\n{8}\n{9}\n{10}\n{11}\n{12}\n{13}\n{14}\n{15}\n{16}\n" +
              "{17}\n{18}\n{19}\n{20}\n{21}\n{22}\n{23}\n{24}\n{25}\n{26}\n{27}\n{28}\n{29}\n{30}\n",
            "/*",
            "  //在 MainForm1() 加入 Assign CallBack functions",
            "  //然後在 FormOnRunningProgress()內實作收到進度值的動作",
            " ",
            "   public void FormOnRunningProgress(ref byte aMainPercent, ref byte aChildPercent,",
            "       IntPtr sMainTask, IntPtr sSubTask)",
            "   {",
            "       slMain.Text = Marshal.PtrToStringUni(sMainTask);",
            "       slSub.Text = Marshal.PtrToStringUni(sSubTask);",
            "",
            "       prgMain.Value = aMainPercent;",
            "       prgSub.Value = aChildPercent;",
            "   }",
            "",
            "   public void FormOnLogInfo(IntPtr sLogInfo)",
            "   {",
            "      slLogInfo.Text = Marshal.PtrToStringUni(sLogInfo);",
            "   }",
            "",
            "   public MainForm()",
            "   {",
            "       InitializeComponent();",
            "   ",
            "       TOnRunningProgress CallBackFun = new TOnRunningProgress(FormOnRunningProgress);",
            "       ClsRvCamDLL.CS_RvCam_AssignCallBackFunc(CallBackFun);       ",
            " ",
            "       TOnLogInfo callBackLogInfo = new TOnLogInfo(FormOnLogInfo);",
            "       ClsRvCamDLL.CS_RvCam_AssignLogInfoFunc(callBackLogInfo);",
            "   }",
            "",
            "*/ "
            );
        #endregion



        static ClsRvCamDLL()
        {
            TFPoint FViewCXY = new TFPoint();
            TFRect FViewRect = new TFRect();

            PaintDummyBmp = new Bitmap(
                        10, 10,
                        PixelFormat.Format32bppArgb //PixelFormat.Format8bppIndexed // .Format32bppArgb
                       );

            if (!(Directory.Exists(".\\Exes\\") && File.Exists(".\\RvCamDLL.dll") && File.Exists(".\\borlndmm.dll")))
                MessageBox.Show("主程式所在的資料夾內必須包含  RvCamDLL.dll, borlndmm.dll 和 [EXEs] 資料夾");

#if DEBUG
            //MessageBox.Show(sCallback);
#endif


#if AutoLoadPlugins
            ClsRvCamDLL_PlugInDLL_FileIO.Get_PlugInNames(ref ClsRvCamDLL_PlugInDLL_FileIO.FPlugInDLLFileNames);
#endif

        }

        //~  ClsRvCamDLL()
        //{
        //    NativeMethods.Un_LoadPlugIns();

        //    if (PaintDummyBmp != null) PaintDummyBmp.Dispose();
        //}


        // Common DLL Functions ========================================================


        #region Common DLL Functions ===========================================================
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_IsAuthorized", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_IsAuthorized();
        /// <summary>檢查軟體是否有授權</summary>
        public static bool CS_RvCam_IsAuthorized()
        {
            TReturnCode ret = RvCam_IsAuthorized();

            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_GetDLLInfo", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_GetDLLInfo(
            ref IntPtr sDLLInfo
        );
        /// <summary>取得 DLL 訊息</summary>
        public static bool CS_RvCam_GetDLLInfo(
            ref string str_sDLLInfo
            )
        {
            IntPtr sDLLInfo = IntPtr.Zero; // Marshal.AllocHGlobal(cMaxAllocateStringSize);

            // DLL 內配置記憶體，在此 DLLIfo 不需配置
            TReturnCode ret = RvCam_GetDLLInfo(
                ref sDLLInfo
            );

            if (ret == TReturnCode.rcSuccess)
            {
                if (IntPtr.Zero != sDLLInfo)
                { str_sDLLInfo = Marshal.PtrToStringUni(sDLLInfo); }
            }
            else if (ret == TReturnCode.rcFail)
            {
            }
            else
            {
            }

            //Marshal.FreeHGlobal(sDLLInfo);

            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_AssignRunningProgressCallBackFunc", CallingConvention = TargetCallingConvention)]
        private static extern void RvCam_AssignRunningProgressCallBackFunc(
            [MarshalAs(UnmanagedType.FunctionPtr)] TOnRunningProgress pCallBackFunc
        );
        /// <summary>設定 CallBack 函數 (執行進度)</summary>
        public static void CS_RvCam_AssignRunningProgressCallBackFunc(
            [MarshalAs(UnmanagedType.FunctionPtr)] TOnRunningProgress pCallBackFunc
            )
        {
            RvCam_AssignRunningProgressCallBackFunc(
                pCallBackFunc
            );

        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_AssignLogInfoFunc", CallingConvention = TargetCallingConvention)]
        private static extern void RvCam_AssignLogInfoFunc(
             [MarshalAs(UnmanagedType.FunctionPtr)] TOnLogInfo pLogInfoFunc
        );
        /// <summary>設定 CallBack 函數 (Log 訊息)</summary>
        public static void CS_RvCam_AssignLogInfoFunc(
             [MarshalAs(UnmanagedType.FunctionPtr)] TOnLogInfo pLogInfoFunc
            )
        {
            RvCam_AssignLogInfoFunc(
                pLogInfoFunc
            );

        }
        #endregion

        // Add Functions =================================================
        #region "Add Functions ------------------------------------------"
        // 將圖形資料 新增到 新 Layer --------------------------------
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Add_LayerData", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Add_LayerData(
            int toStep,
            ref int getAddLayer,
            IntPtr pShapeDataArray0,
            int shapeDataLength,
            IntPtr pAddToNewLayer_SetName = default
        );
        /// <summary>
        /// 加入Layer 圖形資料
        /// </summary>
        /// <param name="toStep">要加入的Step</param>
        /// <param name="getAddLayer">要加入的Layer</param>
        /// <param name="addShapeData">EditShape Array</param>
        /// <param name="sAddToNewLayer_SetName">是否加入到新層</param>
        /// <returns></returns>
        public static bool CS_RvCam_Add_LayerData(
            int toStep,
            ref int getAddLayer,
            TVectSimpleShape[] addShapData,
            string sAddToNewLayer_SetName = ""
            )
        {
            TReturnCode ret = TReturnCode.rcFail;

            IntPtr pAddToNewLayer_SetName = Marshal.StringToBSTR(sAddToNewLayer_SetName);

            unsafe
            {
                fixed (TVectSimpleShape* pShps0 = &(addShapData[0]))
                {
                    ret = RvCam_Add_LayerData(
                        toStep,
                        ref getAddLayer,
                        (IntPtr)pShps0,
                        addShapData.Length,
                        pAddToNewLayer_SetName);
                }
            }

            return (ret == TReturnCode.rcSuccess);
        }
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Add_LayerData_Image", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Add_LayerData_Image(
        int toStep,
        ref int getAddLayer,
        IntPtr pImageStart0,
        int imageStride_BytesPerRow, int imagePixelWidth, int imagePixelHeight,
        TFloat imgageResolution_MmPerPixel,
        Byte imageBitPerPixel = 8,
        bool imageDibUpward = true,
        IntPtr pImageRealMinMaxMm = default(IntPtr),
        IntPtr pAddToNewLayer_SetName = default(IntPtr)
        );
        public static bool CS_RvCam_Add_LayerData_Image(
            int toStep,
            ref int getAddLayer,
            IntPtr pImageStart0,
            int imageStride_BytesPerRow, int imagePixelWidth, int imagePixelHeight,
            TFloat imgageResolution_MmPerPixel,
            Byte imageBitPerPixel = 8,
            bool imageDibUpward = true,
            IntPtr pImageRealMinMaxMm = default(IntPtr),
            string str_pAddToNewLayer_SetName = ""
            )
        {
            // 非 ref string，做轉換------------
            IntPtr pstr_pAddToNewLayer_SetName = Marshal.StringToBSTR(str_pAddToNewLayer_SetName);

            // ref string, DLL 內配置記憶體，在此不需配置------------
            //IntPtr pRef = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);

           
            TReturnCode ret = RvCam_Add_LayerData_Image(
                toStep,
                ref getAddLayer,
                pImageStart0,
                imageStride_BytesPerRow, imagePixelWidth, imagePixelHeight,
                imgageResolution_MmPerPixel,
                imageBitPerPixel,
                imageDibUpward,
                pImageRealMinMaxMm,
                pstr_pAddToNewLayer_SetName
            );

            //Marshal.FreeHGlobal(sXXX);

            return (TReturnCode.rcSuccess == ret);
        }
        #endregion


        //Calibrate Funtions =============================================
        #region "Calibrate Functions ----------------------------------"*/
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Calibrate_BulidTable", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Calibrate_BulidTable(
            IntPtr pImgXYArray0Pixel, IntPtr pCadXYArray0Mm,  //PFPoint
            int ptXNum, int ptYNum
        );
        public static bool CS_RvCam_Calibrate_BulidTable(
            IntPtr pImgXYArray0Pixel, IntPtr pCadXYArray0Mm,
            int ptXNum, int ptYNum
            )
        {
            TReturnCode ret = RvCam_Calibrate_BulidTable(
                pImgXYArray0Pixel, pCadXYArray0Mm,
                ptXNum, ptYNum
            );

            return (TReturnCode.rcSuccess==ret);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Calibrate_CamToImageXY", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Calibrate_CamToImageXY(
            int aStep, int imageLayer,
            IntPtr pFrXYArray0Mm, IntPtr pToXYArray0Pixel,
            int ptNum
        );
        public static bool CS_RvCam_Calibrate_CamToImageXY(
            int aStep, int imageLayer,
            IntPtr pFrXYArray0Mm, IntPtr pToXYArray0Pixel,
            int ptNum
            )
        {
            TReturnCode ret = RvCam_Calibrate_CamToImageXY(
                aStep, imageLayer,
                pFrXYArray0Mm, pToXYArray0Pixel,
                ptNum
            );

            return (TReturnCode.rcSuccess == ret);
        }
        #endregion


        // Clear Functions =================================================
        #region Clear Functions ===================================================
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Clear_UnZipBuffer", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Clear_UnZipBuffer();
        /// <summary>清除解壓縮路徑檔案</summary>
        public static bool CS_RvCam_Clear_UnZipBuffer()
        {
            TReturnCode ret = RvCam_Clear_UnZipBuffer();

            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Clear_CurrentData", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Clear_CurrentData();
        /// <summary>清除目前的 CAM 資料</summary>
        public static bool CS_RvCam_Clear_CurrentData()
        {
            TReturnCode ret = RvCam_Clear_CurrentData();

            return (ret == TReturnCode.rcSuccess);
        }

        //清除某一層資料
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Clear_LayerData", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Clear_LayerData(
            int atStep, int clearLayer
        );
        public static bool CS_RvCam_Clear_LayerData(
            int atStep, int clearLayer
            )
        {
            TReturnCode ret = RvCam_Clear_LayerData(
                atStep, clearLayer
            );

            return (TReturnCode.rcSuccess == ret);
        }
        #endregion


        // Convert Functions ===============================================
        #region Convert Functions------------------------------------*/
        //轉換檔案格式
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "Rvcam_Convert_File_OdbTGZ_To_CAD", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode Rvcam_Convert_File_OdbTGZ_To_CAD(
            IntPtr loadOdbDir_TGZFile,
            IntPtr loadStep,
            IntPtr loadLayers,
            IntPtr setSaveCadFileDirectory,
            TVectFileType saveCadFileType,
            ref IntPtr getSavedFileNames
        );
        /// <summary>
        /// 將Odb/TGZ 轉換成 CAD 檔案(s)
        /// </summary>
        /// <param name="str_loadOdbDir_TGZFile">讀取的 Odb目錄 或 Tgz檔案</param>
        /// <param name="str_loadStep">指定讀取的StepName，必須指定</param>
        /// <param name="str_loadLayers">指定讀取的 LayerNames，必須指定</param>
        /// <param name="str_setSaveCadFileDirectory"></param>
        /// <param name="saveCadFileType"></param>
        /// <param name="str_getSavedFileNames"></param>
        /// <returns></returns>
        public static bool CS_Rvcam_Convert_File_OdbTGZ_To_CAD(
            string str_loadOdbDir_TGZFile,
            string str_loadStep,
            string[] str_loadLayers,
            string str_setSaveCadFileDirectory,
            TVectFileType saveCadFileType,
            ref string[] str_getSavedFileNames
            )
        {

            string sLoadLyrs = Convert_StringArray_To_String(str_loadLayers, ",");
            // 非 ref string，做轉換------------
            IntPtr pstr_loadOdbDir_TGZFile = Marshal.StringToBSTR(str_loadOdbDir_TGZFile);
            IntPtr pstr_loadStep = Marshal.StringToBSTR(str_loadStep);
            IntPtr pstr_loadLayers = Marshal.StringToBSTR(sLoadLyrs);
            IntPtr pstr_setSaveCadFileDirectory = Marshal.StringToBSTR(str_setSaveCadFileDirectory);

            // ref string, DLL 內配置記憶體，在此不需配置------------
            IntPtr pRef_getSavedFileNames = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);



            TReturnCode ret = Rvcam_Convert_File_OdbTGZ_To_CAD(
                pstr_loadOdbDir_TGZFile,
                pstr_loadStep,
                pstr_loadLayers,
                pstr_setSaveCadFileDirectory,
                saveCadFileType,
                ref pRef_getSavedFileNames
            );

            if (ret == TReturnCode.rcSuccess)
            {
                str_getSavedFileNames = Convert_String_To_StringArray(Marshal.PtrToStringUni(pRef_getSavedFileNames), ",");
            }

            //Marshal.FreeHGlobal(sXXX);

            return (ret == TReturnCode.rcSuccess);
        }

        //轉換檔案格式 CAD => CAD 檔案(*.GBX, *.DXF, *.NC, *.DWG...)
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "Rvcam_Convert_File_CAD_To_CAD", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode Rvcam_Convert_File_CAD_To_CAD(
            IntPtr loadCadFiles,
            IntPtr setSaveCadFileDirectory,
            TVectFileType setSaveCadFileType,
            ref IntPtr getSavedFileNames
        );
        /// <summary>
        /// 轉換檔案格式 CAD => CAD 檔案(*.GBX, *.DXF, *.NC, *.DWG...)
        /// </summary>
        /// <param name="str_loadCadFiles">讀入的Cad檔名，以','隔開。  eg. 'c:\a.gbx,d:\b.dxf'</param>
        /// <param name="str_setSaveCadFileDirectory">指定輸出路徑</param>
        /// <param name="setSaveCadFileType">設定輸出檔案格式</param>
        /// <param name="str_getSavedFileNames">傳回輸出的所有檔案</param>
        /// <returns></returns>
        public static bool CS_Rvcam_Convert_File_CAD_To_CAD(
            string[] str_loadCadFiles,
            string str_setSaveCadFileDirectory,
            TVectFileType setSaveCadFileType,
            ref string str_getSavedFileNames
            )
        {
            string sLoadCadFiles = "";
            for (int i = 0; i < str_loadCadFiles.Length; i++) sLoadCadFiles = sLoadCadFiles + ",";

            // 非 ref string，做轉換------------
            IntPtr pstr_loadCadFiles = Marshal.StringToBSTR(sLoadCadFiles);
            IntPtr pstr_setSaveCadFileDirectory = Marshal.StringToBSTR(str_setSaveCadFileDirectory);

            // ref string, DLL 內配置記憶體，在此不需配置------------
            IntPtr pRefstr_getSavedFileNames = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);

            TReturnCode ret = Rvcam_Convert_File_CAD_To_CAD(
                pstr_loadCadFiles,
                pstr_setSaveCadFileDirectory,
                setSaveCadFileType,
                ref pRefstr_getSavedFileNames
            );

            if (ret == TReturnCode.rcSuccess)
            {
                str_getSavedFileNames = Marshal.PtrToStringUni(pRefstr_getSavedFileNames);
            }

            //Marshal.FreeHGlobal(sXXX);


            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Convert_Resolution_MmPerPixel_To_DPI", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Convert_Resolution_MmPerPixel_To_DPI(
            Double mmPerPxl,
            ref Double DPI
        );
        /// <summary>轉換解析度單位 Mm/Pixel -> DPI</summary>
        public static bool CS_RvCam_Convert_Resolution_MmPerPixel_To_DPI(
            Double mmPerPxl,
            ref Double DPI
            )
        {
            TReturnCode ret = RvCam_Convert_Resolution_MmPerPixel_To_DPI(
                mmPerPxl,
                ref DPI
            );

            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Convert_Resolution_DPI_To_MmPerPixel", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Convert_Resolution_DPI_To_MmPerPixel(
            Double DPI,
            ref Double mmPerPxl
        );
        /// <summary>轉換解析度單位 DPI -> Mm/Pixel</summary>
        public static bool CS_RvCam_Convert_Resolution_DPI_To_MmPerPixel(
            Double DPI,
            ref Double mmPerPxl
            )
        {
            TReturnCode ret = RvCam_Convert_Resolution_DPI_To_MmPerPixel(
                DPI,
                ref mmPerPxl
            );

            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Convert_Unit_Value", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Convert_Unit_Value(
            TValueUnit frUnit,
            TFloat frValue,
            TValueUnit toUnit,
            ref TFloat toValue
        );
        /// <summary>轉換數值單位 Inch, mil, cm, mm, um...</summary>
        public static bool CS_RvCam_Convert_Unit_Value(
            TValueUnit frUnit,
            TFloat frValue,
            TValueUnit toUnit,
            ref TFloat toValue
            )
        {
            TReturnCode ret = RvCam_Convert_Unit_Value(
                frUnit,
                frValue,
                toUnit,
                ref toValue
            );

            return (ret == TReturnCode.rcSuccess);
        }

        //轉換檢視中心(viewXYmm)+畫布範圍(canvasWidthPixel,canvasHeightPixel => 檢視範圍(mm)
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Convert_ViewXYmmWHpixel_To_ViewMinMaxMm", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Convert_ViewXYmmWHpixel_To_ViewMinMaxMm(
            TFloat viewCXmm, TFloat viewCYmm,
            int canvasWidthPixel, int canvasHeightPixel,
            TFloat viewResolution_MmPerPixel,
            ref TFloat viewMinXmm, ref TFloat viewMinYmm, ref TFloat viewMaxXmm, ref TFloat viewMaxYmm
        );
        /// <summary>
        /// 轉換檢視中心(viewXYmm)+畫布範圍(canvasWidthPixel,canvasHeightPixel => 檢視範圍(mm)
        /// </summary>
        /// <param name="viewCXmm">中心Xmm</param>
        /// <param name="viewCYmm">中心Ymm</param>
        /// <param name="canvasWidthPixel">影像寬度Pixel</param>
        /// <param name="canvasHeightPixel">影像高度Pixel</param>
        /// <param name="viewResolution_MmPerPixel">影像解析度</param>
        /// <param name="viewMinXmm">回傳圖形範圍 Left(mm)</param>
        /// <param name="viewMinYmm">回傳圖形範圍 Bottom(mm)</param>
        /// <param name="viewMaxXmm">回傳圖形範圍 Right(mm)</param>
        /// <param name="viewMaxYmm">回傳圖形範圍 Top(mm)</param>
        /// <returns></returns>
        public static bool CS_RvCam_Convert_ViewXYmmWHpixel_To_ViewMinMaxMm(
            TFloat viewCXmm, TFloat viewCYmm,
            int canvasWidthPixel, int canvasHeightPixel,
            TFloat viewResolution_MmPerPixel,
            ref TFloat viewMinXmm, ref TFloat viewMinYmm, ref TFloat viewMaxXmm, ref TFloat viewMaxYmm
            )
        {
            TReturnCode ret = RvCam_Convert_ViewXYmmWHpixel_To_ViewMinMaxMm(
                viewCXmm, viewCYmm,
                canvasWidthPixel, canvasHeightPixel,
                viewResolution_MmPerPixel,
                ref viewMinXmm, ref viewMinYmm, ref viewMaxXmm, ref viewMaxYmm
            );

            return (TReturnCode.rcSuccess == ret);
        }
        #endregion


        // Delete Functions ==============================================
        #region "Delete Functions -------------------------------"
        //刪除某一層資料
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Delete_LayerData", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Delete_LayerData(
            int delLayer
        );
        /// <summary>
        /// 清除並刪除某一層資料
        /// </summary>
        /// <param name="delLayer">刪除的 Layer</param>
        /// <returns></returns>
        public static bool CS_RvCam_Delete_LayerData(
            int delLayer
            )
        {
            TReturnCode ret = RvCam_Delete_LayerData(
                delLayer
            );

            return true; // (TReturnCode.rcSuccess == ret);
        }
        #endregion


        // Dialog Functions ==================================================
        #region "Dialog Functions-------------------------------------'*/
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Dialog_MultiInputBox", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Dialog_MultiInputBox(
            IntPtr sTitle,
            IntPtr sPrompts,
            ref IntPtr getValues
        );
        /// <summary>
        /// 輸入多個項目並回傳
        /// </summary>
        /// <param name="str_sTitle">標題</param>
        /// <param name="str_sPrompts">項目名稱(s)，以 ',' 隔開</param>
        /// <param name="str_getValues">回傳項目的值(s)，以 ',' 隔開</param>
        /// <returns></returns>
        public static bool CS_RvCam_Dialog_MultiInputBox(
            string str_sTitle,
            string[] str_sPrompts,
            ref string[] str_getValues
            )
        {
            string sPrompts = "";
            Convert_StringArray_To_String(str_sPrompts, "@", ref sPrompts);

            // 非 ref string，做轉換------------
            IntPtr pstr_sTitle = Marshal.StringToBSTR(str_sTitle);
            IntPtr pstr_sPrompts = Marshal.StringToBSTR(sPrompts);

            string sValues = "";
            // ref string, DLL 內配置記憶體，在此不需配置------------
            IntPtr pRefgetValues = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);
            if (str_getValues.Length > 0)
            {
                Convert_StringArray_To_String(str_getValues, "@", ref sValues);
                pRefgetValues = Marshal.StringToBSTR(sValues);
            }

            TReturnCode ret = RvCam_Dialog_MultiInputBox(
                pstr_sTitle,
                pstr_sPrompts,
                ref pRefgetValues
            );

            if (ret == TReturnCode.rcSuccess)
            {
                sValues = Marshal.PtrToStringUni(pRefgetValues);
                Convert_String_To_StringArray(sValues, "@", ref str_getValues);
            }

            //Marshal.FreeHGlobal(sXXX);


            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Dialog_ItemSelect", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Dialog_ItemSelect(
        IntPtr sTitle,
        IntPtr sItems,
        ref IntPtr getItemsSelections,
        ref int selectedIndex,
        bool multiSelection = false
    );
        public static bool CS_RvCam_Dialog_ItemSelect(
            string str_sTitle,
            string[] str_sItems,
            ref bool[] itemsSelected,
            ref int selectedIndex,
            bool multiSelection = false
            )
        {
            // 非 ref string，做轉換------------
            IntPtr pstr_sTitle = Marshal.StringToBSTR(str_sTitle);

            string sItems = "";
            if (!Convert_StringArray_To_String(str_sItems, ",", ref sItems)) return false;
            IntPtr psItems = Marshal.StringToBSTR(sItems);

            string sSelects = "";
            // ref string, DLL 內配置記憶體，在此不需配置------------
            IntPtr pRefgetSelect = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);            

            TReturnCode ret = RvCam_Dialog_ItemSelect(
                pstr_sTitle,
                psItems,
                ref pRefgetSelect,
                ref selectedIndex,
                multiSelection
            );


            if (ret == TReturnCode.rcSuccess)
            {
                sItems = Marshal.PtrToStringUni(pRefgetSelect);
                string[] sSelected = sItems.Split(",");

                for (int i = 0; i < sSelected.Length; i++)
                {
                    if ("0" == sSelected[i])
                        ArrayExtension.Add(ref itemsSelected, false); // itemsSelected.Join(false);
                    else
                        ArrayExtension.Add(ref itemsSelected, true);
                }
            }

            //Marshal.FreeHGlobal(sXXX);


            return (TReturnCode.rcSuccess == ret);
        }
        #endregion


        // Edit Functions =======================================================
        #region "Edit Functions ------------------------------------------"*/
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Edit_Step", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Edit_Step(
        IntPtr editStepName,
        TStepEditMode editMode,
        TFloat shiftXmm_scaleX_rotateDegree,
        TFloat shiftYmm_scaleY,
        ref IntPtr setGetNewStepName
        );
        /// <summary>
        /// Step 資料編輯 (複製、旋轉、位移、鏡射...)
        /// </summary>
        /// <param name="str_editStepName">編輯Step名稱</param>
        /// <param name="editMode">編輯模式 TStepEditMode</param>
        /// <param name="shiftXmm_scaleX_rotateDegree">位移X 或 縮放比例X 或 旋轉角度</param>
        /// <param name="shiftYmm_scaleY">位移Y 或 縮放比例Y</param>
        /// <param name="str_setGetNewStepName">設定新Step名稱(傳入字串)或傳回新Step名稱(傳入null)</param>
        /// <returns></returns>
        public static bool CS_RvCam_Edit_Step(
            string str_editStepName,
            TStepEditMode editMode,
            TFloat shiftXmm_scaleX_rotateDegree,
            TFloat shiftYmm_scaleY,
            ref string str_setGetNewStepName
            )
        {
            // 非 ref string，做轉換------------
            IntPtr pstr_editStepName = Marshal.StringToBSTR(str_editStepName);

            // ref string, DLL 內配置記憶體，在此不需配置------------
            IntPtr pRefstr_setGetNewStepName = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);


            TReturnCode ret = RvCam_Edit_Step(
                pstr_editStepName,
                editMode,
                shiftXmm_scaleX_rotateDegree,
                shiftYmm_scaleY,
                ref pRefstr_setGetNewStepName
            );

            if (ret == TReturnCode.rcSuccess)
            {

               str_setGetNewStepName = Marshal.PtrToStringUni(pRefstr_setGetNewStepName);
            }

            //Marshal.FreeHGlobal(sXXX);
            return (TReturnCode.rcSuccess == ret);
        }
        /// <summary>
        /// 新增Step 排版複製
        /// </summary>
        /// <param name="editStepName">編輯參考Step名稱</param>
        /// <param name="shiftXmm">新Step位移Xmm</param>
        /// <param name="shiftYmm">新Step位移Y</param>
        /// <param name="rotateDegree">新Step旋轉角度</param>
        /// <param name="mirrorX">新Step是否MirrorX (MirrorY = rotateDeg=180, mirrorX=true)</param>
        /// <param name="stpRptNumX">X方向排版片數</param>
        /// <param name="stpRptNumY">Y方向排版片數</param>
        /// <param name="stpRptDXmm">X方向排版間隔mm</param>
        /// <param name="stpRptDYmm">Y方向排版間隔mm</param>
        /// <param name="setGetNewStepName">設定新Step名稱(傳入字串)或傳回新Step名稱(傳入null)</param>
        /// <returns></returns>
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Edit_Step_StepRepeat", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Edit_Step_StepRepeat(
            IntPtr editStepName,
            TFloat shiftXmm, TFloat shiftYmm,
            TFloat rotateDegree,
            bool mirrorX,
            int stpRptNumX, int stpRptNumY,
            TFloat stpRptDXmm, TFloat stpRptDYmm,
            ref IntPtr setGetNewStepName
        );
        public static bool CS_RvCam_Edit_Step_StepRepeat(
            string str_editStepName,
            TFloat shiftXmm, TFloat shiftYmm,
            TFloat rotateDegree,
            bool mirrorX,
            int stpRptNumX, int stpRptNumY,
            TFloat stpRptDXmm, TFloat stpRptDYmm,
            ref string str_setGetNewStepName
            )
        {
            // 非 ref string，做轉換------------
            IntPtr pstr_editStepName = Marshal.StringToBSTR(str_editStepName);

            // ref string, DLL 內配置記憶體，在此不需配置------------
            IntPtr pRefstr_setGetNewStepName = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);


            TReturnCode ret = RvCam_Edit_Step_StepRepeat(
                pstr_editStepName,
                shiftXmm, shiftYmm,
                rotateDegree,
                mirrorX,
                stpRptNumX, stpRptNumY,
                stpRptDXmm, stpRptDYmm,
                ref pRefstr_setGetNewStepName
            );

            if (ret == TReturnCode.rcSuccess)
            {

                str_setGetNewStepName= Marshal.PtrToStringUni(pRefstr_setGetNewStepName);
            }

            //Marshal.FreeHGlobal(sXXX);


            return (TReturnCode.rcSuccess == ret);
        }
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Edit_Layer", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Edit_Layer(
            IntPtr editStepName,
            IntPtr editLayerNames,
            TLayerEditMode editMode,
            TFloat shiftXmm_scaleX_rotateDegree,
            TFloat shiftYmm_scaleY,
            ref IntPtr getNewLayerNames
        );
        /// <summary>
        /// Layer編輯 ( 複製、新增、多層合併、旋轉、位移、鏡射、選取拷貝...)
        /// </summary>
        /// <param name="str_editStepName">編輯的Step名稱</param>
        /// <param name="str_editLayerNames">編輯的Layers名稱(可多層，以","隔開，eg. "l1,l2,l3"</param>
        /// <param name="editMode">層編輯模式 TLayerEditMode</param>
        /// <param name="shiftXmm_scaleX_rotateDegree">位移Xmm 或 縮放比例X 或 旋轉角度</param>
        /// <param name="shiftYmm_scaleY">位移Ymm 或 縮放比例Y</param>
        /// <param name="str_getNewLayerNames">傳回新層名稱</param>
        /// <returns></returns>
        public static bool CS_RvCam_Edit_Layer(
            string str_editStepName,
            string[] str_editLayerNames,
            TLayerEditMode editMode,
            TFloat shiftXmm_scaleX_rotateDegree,
            TFloat shiftYmm_scaleY,
            ref string[] str_getNewLayerNames
            )
        {
            string sLyrs = Convert_StringArray_To_String(str_editLayerNames, ",");

            // 非 ref string，做轉換------------
            IntPtr pstr_editStepName = Marshal.StringToBSTR(str_editStepName);
            IntPtr psLyrs = Marshal.StringToBSTR(sLyrs);

            // ref string, DLL 內配置記憶體，在此不需配置------------
            IntPtr pRefstr_getNewLayerNames = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);


            TReturnCode ret = RvCam_Edit_Layer(
                pstr_editStepName,
                psLyrs,
                editMode,
                shiftXmm_scaleX_rotateDegree,
                shiftYmm_scaleY,
                ref pRefstr_getNewLayerNames
            );

            if (ret == TReturnCode.rcSuccess)
            {
                str_getNewLayerNames = Convert_String_To_StringArray(Marshal.PtrToStringUni(pRefstr_getNewLayerNames), ",");
            }

            //Marshal.FreeHGlobal(sXXX);


            return (TReturnCode.rcSuccess == ret);
        }
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Edit_Layer_Align", CallingConvention = TargetCallingConvention)]
            private static extern TReturnCode RvCam_Edit_Layer_Align(
            int editStepID,
            int fromLayerID,
            TFPoint fromLayerMark1mm, TFPoint fromLayerMark2mm,
            int toLayerID,
            TFPoint toLayerMark1mm, TFPoint toLayerMark2mm,
            ref TFloat getRotateDegree,
            ref TFPoint getShiftXYmm,
            ref TFloat getScale
        );
        /// <summary>
        /// 兩層 CAD/Bmp 的旋轉位移對位校正
        /// </summary>
        /// <param name="editStepID">編輯的StepID</param>
        /// <param name="fromLayerID">來源Layer( eg. Bmp 圖形層)</param>
        /// <param name="fromLayerMark1mm">來源Layer的第一個對位點XYmm</param>
        /// <param name="fromLayerMark2mm">來源Layer的第二個對位點XYmm</param>
        /// <param name="toLayerID">參考Layer (eg. CAD 層)</param>
        /// <param name="toLayerMark1mm">參考Layer的第一個對位點XYmm</param>
        /// <param name="toLayerMark2mm">參考Layer的第二個對位點XYmm</param>
        /// <param name="getRotateDegree">傳回校正的角度</param>
        /// <param name="getShiftXYmm">傳回校正位移值</param>
        /// <param name="getScale">傳回校正縮放比例</param>
        /// <returns></returns>
        public static bool CS_RvCam_Edit_Layer_Align(
            int editStepID,
            int fromLayerID,
            TFPoint fromLayerMark1mm, TFPoint fromLayerMark2mm,
            int toLayerID,
            TFPoint toLayerMark1mm, TFPoint toLayerMark2mm,
            ref TFloat getRotateDegree,
            ref TFPoint getShiftXYmm,
            ref TFloat getScale
            )
        {
            TReturnCode ret = RvCam_Edit_Layer_Align(
                editStepID,
                fromLayerID,
                fromLayerMark1mm, fromLayerMark2mm,
                toLayerID,
                toLayerMark1mm, toLayerMark2mm,
                ref getRotateDegree,
                ref getShiftXYmm,
                ref getScale
            );

            return (TReturnCode.rcSuccess == ret);
        }
        #endregion


        // Get Functions =================================================
        #region Get Functions  ===================================================
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Get_StepsLayers_ODB", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Get_StepsLayers_ODB(
            IntPtr sOdbDir_TGZFile,
            ref IntPtr getSteps,
            ref IntPtr getLayers,
            TOdbStepListType stepsListTp = TOdbStepListType.osAllSteps,
            IntPtr atTopStepName = default
        );
        /// <summary>取得 ODB++/TGZ 的 Steps, Layers 名稱</summary>
        public static bool CS_RvCam_Get_StepsLayers_ODB(
            string sOdbDir_TGZFile,
            ref string[] getSteps,
            ref string[] getLayers,
            TOdbStepListType stepsListTp = TOdbStepListType.osAllSteps,
            string atTopStepName = ""
            )
        {

            getSteps = [];
            getLayers = [];

            IntPtr pFn = Marshal.StringToBSTR(sOdbDir_TGZFile); // Marshal.StringToHGlobalUni(sOdbDir_TGZFile);
            IntPtr pAtTopStp = Marshal.StringToBSTR(atTopStepName); // Marshal.StringToHGlobalUni(atTopStepName);
            IntPtr pStps = default, pLyrs = default;

            TReturnCode ret = RvCam_Get_StepsLayers_ODB(
                pFn,
                ref pStps,
                ref pLyrs,
                stepsListTp,
                pAtTopStp
            );

            if (ret == TReturnCode.rcSuccess)
            {
                if (IntPtr.Zero != pStps)
                { getSteps = Marshal.PtrToStringAuto(pStps).Split(","); }

                if (IntPtr.Zero != pLyrs)
                { getLayers = Marshal.PtrToStringAuto(pLyrs).Split(","); }
            }

            //Marshal.FreeHGlobal(pFn);
            //Marshal.FreeHGlobal(pAtTopStp);

            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Get_StepsLayers_ODB_Dialog", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Get_StepsLayers_ODB_Dialog(
            ref IntPtr getODB_TGZFile,
            ref IntPtr getSteps,
            ref IntPtr getLayers,
            TOdbTgzType loadOdbTgzTp = TOdbTgzType.otOdbFolder,
            TOdbStepListType stepsListTp = TOdbStepListType.osAllSteps
        );
        /// <summary>取得 ODB++/TGZ 的 Steps, Layers 名稱 ( 開檔介面 )</summary>
        public static bool CS_RvCam_Get_StepsLayers_ODB_Dialog(
            ref string getODB_TGZFile,
            ref string[] getSteps,
            ref string[] getLayers,
            TOdbTgzType loadOdbTgzTp = TOdbTgzType.otOdbFolder,
            TOdbStepListType stepsListTp = TOdbStepListType.osAllSteps
            )
        {
            getODB_TGZFile = "";
            getSteps = [];
            getLayers = [];

            IntPtr pFn = default;
            IntPtr pStps = default, pLyrs = default;

            TReturnCode ret = RvCam_Get_StepsLayers_ODB_Dialog(
                ref pFn,
                ref pStps,
                ref pLyrs,
                loadOdbTgzTp,
                stepsListTp
            );


            if (ret == TReturnCode.rcSuccess)
            {
                if (IntPtr.Zero != pFn)
                { getODB_TGZFile = Marshal.PtrToStringAuto(pFn); }

                if (IntPtr.Zero != pStps)
                { getSteps = Marshal.PtrToStringAuto(pStps).Split(","); }

                if (IntPtr.Zero != pLyrs)
                { getLayers = Marshal.PtrToStringAuto(pLyrs).Split(","); }
            }

            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Get_StepsLayers_CurrentData", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Get_StepsLayers_CurrentData(
            ref IntPtr getSteps,
            ref IntPtr getLayers
        );
        /// <summary>取得目前 CAM 資料的 Steps,Layers 名稱</summary>
        public static bool CS_RvCam_Get_StepsLayers_CurrentData(
            ref string getSteps,
            ref string getLayers
            )
        {
            getSteps = "";
            getLayers = "";

            IntPtr pStps = default, pLyrs = default;

            TReturnCode ret = RvCam_Get_StepsLayers_CurrentData(
                ref pStps,
                ref pLyrs
            );

            if (ret == TReturnCode.rcSuccess)
            {
                if (IntPtr.Zero != pStps)
                { getSteps = Marshal.PtrToStringAuto(pStps); }

                if (IntPtr.Zero != pLyrs)
                { getLayers = Marshal.PtrToStringAuto(pLyrs); }
            }

            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Get_ImageToCamXY", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Get_ImageToCamXY(
            int canvasID,
            int imageX, int imageY,
            ref TFloat camXmm, ref TFloat camYmm,
            bool byStoredView = false
        );
        /// <summary>取得 ImageXY -> CamXY</summary>
        public static bool CS_RvCam_Get_ImageToCamXY(
            int canvasID,
            int imageX, int imageY,
            ref TFloat camXmm, ref TFloat camYmm,
            bool byStoredView = false
            )
        {
            TReturnCode ret = RvCam_Get_ImageToCamXY(
                canvasID,
                imageX, imageY,
                ref camXmm, ref camYmm,
                byStoredView
            );

            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Get_CamToImageXY", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Get_CamToImageXY(
            int canvasID,
            TFloat camXmm, TFloat camYmm,
            ref int imageX, ref int imageY
        );
        /// <summary>取得 CamXY -> ImageXY</summary>
        public static bool CS_RvCam_Get_CamToImageXY(
            int canvasID,
            TFloat camXmm, TFloat camYmm,
            ref int imageX, ref int imageY
            )
        {
            TReturnCode ret = RvCam_Get_CamToImageXY(
                canvasID,
                camXmm, camYmm,
                ref imageX, ref imageY
            );

            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Get_ViewInfo", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Get_ViewInfo(
            int canvasID,
            ref TFloat viewCXMm, ref TFloat viewCYmm,
            ref TFloat viewWidthMm, ref TFloat viewHeightMm,
            ref TFloat viewResolutionMmPerPixel,
            ref TFloat viewDegree,
            ref bool viewMirrorX,
            bool byStoredView = false
        );
        /// <summary>取得View的資訊</summary>
        public static bool CS_RvCam_Get_ViewInfo(
            int canvasID,
            ref TFloat viewCXMm, ref TFloat viewCYmm,
            ref TFloat viewWidthMm, ref TFloat viewHeightMm,
            ref TFloat viewResolutionMmPerPixel,
            ref TFloat viewDegree,
            ref bool viewMirrorX,
            bool byStoredView = false
            )
        {
            TReturnCode ret = RvCam_Get_ViewInfo(
                canvasID,
                ref viewCXMm, ref viewCYmm,
                ref viewWidthMm, ref viewHeightMm,
                ref viewResolutionMmPerPixel,
                ref viewDegree,
                ref viewMirrorX,
                byStoredView
            );

            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Get_ObjectsCount", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Get_ObjectsCount(
            int qryStep, int qryLayer,
            ref int padCount, ref int lineCount, ref int arcCount
        );
        /// <summary>取得 Step/Layer 的物件數量</summary>
        public static bool CS_RvCam_Get_ObjectsCount(
            int qryStep, int qryLayer,
            ref int padCount, ref int lineCount, ref int arcCount
            )
        {
            TReturnCode ret = RvCam_Get_ObjectsCount(
                qryStep, qryLayer,
                ref padCount, ref lineCount, ref arcCount
            );

            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Get_Color", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Get_Color(
            ref int getColor
                    );
        /// <summary>
        /// 選擇顏色
        /// </summary>
        /// <param name="getColor">傳回選取的顏色</param>
        /// <returns></returns>
        public static bool CS_RvCam_Get_Color(
            ref Color getColor
            )
        {
            int intGetColor = Convert_ColorToIntAARRGGBB(getColor);
            TReturnCode ret = RvCam_Get_Color(
                ref intGetColor
            );

            if (ret == TReturnCode.rcSuccess)
                getColor = Convert_IntAARRGGBBToColor(intGetColor);

            return (ret == TReturnCode.rcSuccess);
        }

        // 將圖形資料 新增到 新 Layer --------------------------------
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Get_LayerData", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Get_LayerData(
            int getStep,
            int getLayer,
            ref IntPtr pShapeDataArray0,
            ref int shapeDataLength
        );
        /// <summary>
        /// 取的Layer Data
        /// </summary>
        /// <param name="getStep">Step</param>
        /// <param name="getLayer">Layer</param>
        /// <param name="getShapeData">Array Length</param>
        /// <returns></returns>
        public static bool CS_RvCam_Get_LayerData(
            int getStep,
            int getLayer,
            ref TVectSimpleShape[] getShapeData
            )
        {

            TReturnCode ret = TReturnCode.rcFail;

            IntPtr pGetShps0 = IntPtr.Zero;
            int shpsLength = 0;

            if (TReturnCode.rcSuccess == RvCam_Get_LayerData(
                    getStep,
                    getLayer,
                    ref pGetShps0,
                    ref shpsLength))
            {
#if AutoLoadPlugins  //自動讀取 ./PlugIns/ 下的所有 DLL.
                ClsRvCamDLL_PlugInDLL_FileIO.Copy_TVectSimpleShapes(pGetShps0, shpsLength,
                   ref ClsRvCamDLL_PlugInDLL_FileIO.FVectShapeData);
                ret = TReturnCode.rcSuccess;
#endif
            }

            return (ret == TReturnCode.rcSuccess);
        }
        #endregion


        // Load Functions =================================================
        #region Load Functions  ===================================================
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Load_CAD", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Load_CAD(
            IntPtr sCadFileNames,  // 讀入多個檔案並以 ',' 分開， eg. 'c:\a.gbx,d:\b.dxf'
            ref IntPtr getLoadToStepName,
            ref IntPtr getLoadToLayerNames,
            ref TVectFileType getFileType,
            IntPtr setLoadToStepNameOrNull = default,
            bool blClearCurrentData = true
        );
        /// <summary>
        /// 無顯示介面,背景 讀取 CAD 檔案(*.GBX, *.DXF, *.NC, *.DWG...)，可多選
        /// </summary>
        /// <param name="str_sCadFileNames">讀入多個檔案並以 ',' 分開， eg. 'c:\a.gbx,d:\b.dxf'</param>
        /// <param name="str_getLoadToStepName">傳回讀入到哪個Step</param>
        /// <param name="str_getLoadToLayerNames">傳回讀入到哪個Layer</param>
        /// <param name="getFileType">傳回讀入的檔案類型</param>
        /// <param name="str_setLoadToStepNameOrNull">可設定要讀入的目的 Step，或者Null</param>
        /// <param name="blClearCurrentData">讀入前是否先清除目前CAM資料</param>
        /// <returns></returns>
        public static bool CS_RvCam_Load_CAD(
            string[] str_sCadFileNames, // 讀入多個檔案並以 ',' 分開， eg. 'c:\a.gbx,d:\b.dxf'
            ref string str_getLoadToStepName,
            ref string[] str_getLoadToLayerNames,
            ref TVectFileType getFileType,
            string str_setLoadToStepNameOrNull = "",
            bool blClearCurrentData = true
            )
        {
            string sCadFileNames = "";
            sCadFileNames = Convert_StringArray_To_String(str_sCadFileNames, ",");
            //for (int iy = 0; iy < str_sCadFileNames.Length; iy++)
            //{
            //    sCadFileNames = sCadFileNames + str_sCadFileNames[iy] + ",";
            //}
            // 非 ref string，做轉換------------
            IntPtr pstr_sCadFileNames = Marshal.StringToBSTR(sCadFileNames);
            IntPtr pstr_setLoadToStepName = Marshal.StringToBSTR(str_setLoadToStepNameOrNull);

            // ref string, DLL 內配置記憶體，在此不需配置------------
            IntPtr pRefstr_getLoadToStepName = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);
            IntPtr pRefstr_getLoadToLayerName = IntPtr.Zero;

            TReturnCode ret = RvCam_Load_CAD(
                pstr_sCadFileNames, // 讀入多個檔案並以 ',' 分開， eg. 'c:\a.gbx,d:\b.dxf'
                ref pRefstr_getLoadToStepName,
                ref pRefstr_getLoadToLayerName,
                ref getFileType,
                pstr_setLoadToStepName,
                blClearCurrentData
            );

            if (ret == TReturnCode.rcSuccess)
            {
                str_getLoadToLayerNames = 
                    Convert_String_To_StringArray( Marshal.PtrToStringUni(pRefstr_getLoadToLayerName), ",");
            }

            //Marshal.FreeHGlobal(sXXX);

            return (ret == TReturnCode.rcSuccess);
        }
        /// <summary>
        /// 以開檔視窗開啟CAD檔案，可多選
        /// </summary>
        /// <param name="getCadFileNames">傳回開啟的檔案名稱</param>
        /// <param name="getLoadToStepName">傳回讀入到哪個Step</param>
        /// <param name="getLoadToLayerNames">傳回讀入到哪個Layer</param>
        /// <param name="getFileType">傳回讀入的檔案類型</param>
        /// <param name="setLoadToStepNameOrNull">可設定要讀入的目的 Step，或者Null</param>
        /// <param name="blClearCurrentData">讀入前是否先清除目前CAM資料</param>
        /// <returns></returns>
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Load_CAD_Dialog", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Load_CAD_Dialog(
            ref IntPtr getCadFileNames, // 讀入多個檔案並以 ',' 分開， eg. 'c:\a.gbx,d:\b.dxf'
            ref IntPtr getLoadToStepName,
            ref IntPtr getLoadToLayerNames,
            ref TVectFileType getFileType,
            IntPtr setLoadToStepNameOrNull = default,
            bool blClearCurrentData = true
        );
        /// <summary>
        /// 以DLL內開檔視窗 讀入CAD檔案
        /// </summary>
        /// <param name="str_getCadFileNames">傳回開啟的檔案名稱</param>
        /// <param name="str_getLoadToStepName">傳回目標StepName</param>
        /// <param name="str_getLoadToLayerNames">傳回新增的LayerNames</param>
        /// <param name="getFileType">傳回讀入的檔案格式</param>
        /// <param name="str_setLoadToStepNameOrNull">指定目標StepName</param>
        /// <param name="blClearCurrentData">是否先清除現有所有資烙</param>
        /// <returns></returns>
        public static bool CS_RvCam_Load_CAD_Dialog(
            ref string[] str_getCadFileNames, // 讀入多個檔案並以 ',' 分開， eg. 'c:\a.gbx,d:\b.dxf'
            ref string str_getLoadToStepName,
            ref string[]  str_getLoadToLayerNames,
            ref TVectFileType getFileType,
            string str_setLoadToStepNameOrNull = "",
            bool blClearCurrentData = true
            )
        {
            // 非 ref string，做轉換------------
            IntPtr pstr_setLoadToStepName = Marshal.StringToBSTR(str_setLoadToStepNameOrNull);

            // ref string, DLL 內配置記憶體，在此不需配置------------
            IntPtr pRefstr_getCadFileNames = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);
            IntPtr pRefstr_getLoadToStepName = IntPtr.Zero;
            IntPtr pRefstr_getLoadToLayerNames = IntPtr.Zero;

            TReturnCode ret = RvCam_Load_CAD_Dialog(
                ref pRefstr_getCadFileNames, // 讀入多個檔案並以 ',' 分開， eg. 'c:\a.gbx,d:\b.dxf'
                ref pRefstr_getLoadToStepName,
                ref pRefstr_getLoadToLayerNames,
                ref getFileType,
                pstr_setLoadToStepName,
                blClearCurrentData
            );

            if (ret == TReturnCode.rcSuccess)
            {
                str_getCadFileNames = Convert_String_To_StringArray(
                        Marshal.PtrToStringUni(pRefstr_getCadFileNames), ",");
                str_getLoadToStepName = Marshal.PtrToStringUni(pRefstr_getLoadToStepName);
                str_getLoadToLayerNames = Convert_String_To_StringArray(
                        Marshal.PtrToStringUni(pRefstr_getLoadToLayerNames), ",");
            }

            //Marshal.FreeHGlobal(sXXX);

            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Load_ODB", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Load_ODB(
            IntPtr sOdbDir_TGZFile,
            ref IntPtr getSteps,
            ref IntPtr getLayers,
            bool showImportOdbDialog = false,
            TOdbStepListType stepsListTp = TOdbStepListType.osAllSteps,
            IntPtr loadOnlySteps = default, //指定讀入的 Steps, 和 Layers Name
            IntPtr loadOnlyLayers = default // Eg.  'pcb,array,panel', 'comp, l2, l3'
        );
        /// <summary>
        /// 無顯示介面,背景 讀取 'Cam ODB++目錄' 或 'TGZ檔案'
        /// </summary>
        /// <param name="str_sOdbDir_TGZFile">設定讀入的  'Cam ODB++目錄' 或 'TGZ檔案</param>
        /// <param name="str_getSteps">傳回所有Steps 名稱，以 ',' 隔開。eg. "pcb,array,panel" </param>
        /// <param name="str_getLayers">傳回所有Layers 名稱，以 ',' 隔開。eg. "ssk,l1,l2,sold,pth" </param>
        /// <param name="showImportOdbDialog">是否顯示使用者自訂 ODB Import 視窗</param>
        /// <param name="stepsListTp">指定讀入Steps的方式(繼承模式、獨立Steps....)</param>
        /// <param name="loadOnlySteps">設定只讀入 Steps</param>
        /// <param name="loadOnlyLayers">設定只讀入 Layers</param>
        /// <returns></returns>
        public static bool CS_RvCam_Load_ODB(
            string str_sOdbDir_TGZFile,
            ref string[] str_getSteps,
            ref string[] str_getLayers,
            bool showImportOdbDialog = false,
            TOdbStepListType stepsListTp = TOdbStepListType.osAllSteps,
            string[]? loadOnlySteps = null, //指定讀入的 Steps, 和 Layers Name
            string[]? loadOnlyLayers = null // Eg.  'pcb,array,panel', 'comp, l2, l3'
            )
        {
            string sLoadOnlySteps = Convert_StringArray_To_String(loadOnlySteps, ","),
                   sLoadOnlyLayers = Convert_StringArray_To_String(loadOnlyLayers, ",");

            // 非 ref string，做轉換------------
            IntPtr pstr_sOdbDir_TGZFile = Marshal.StringToBSTR(str_sOdbDir_TGZFile);
            IntPtr ploadOnlySteps = Marshal.StringToBSTR(sLoadOnlySteps);
            IntPtr ploadOnlyLayers = Marshal.StringToBSTR(sLoadOnlyLayers);

            // ref string, DLL 內配置記憶體，在此不需配置------------
            IntPtr pRefstr_getSteps = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);
            IntPtr pRefstr_getLayers = IntPtr.Zero;

            TReturnCode ret = RvCam_Load_ODB(
                pstr_sOdbDir_TGZFile,
                ref pRefstr_getSteps,
                ref pRefstr_getLayers,
                showImportOdbDialog,
                stepsListTp,
                ploadOnlySteps,
                ploadOnlyLayers
            );

            if (ret == TReturnCode.rcSuccess)
            {
                str_getSteps = Marshal.PtrToStringUni(pRefstr_getSteps).Split(",");
                str_getLayers = Marshal.PtrToStringUni(pRefstr_getLayers).Split(",");
            }

            //Marshal.FreeHGlobal(sXXX);

            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Load_ODB_Dialog", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Load_ODB_Dialog(
            ref IntPtr getOdbDir_TGZFile,
            ref IntPtr getSteps,
            ref IntPtr getLayers,
            TOdbTgzType loadOdbTgzTp = TOdbTgzType.otOdbFolder
        );

        /// <summary>
        /// 以開檔視窗讀取 'Cam ODB++目錄' 或 'TGZ檔案'
        /// </summary>
        /// <param name="str_getOdbDir_TGZFile">傳回選取讀入的  'Cam ODB++目錄' 或 'TGZ檔案</param>
        /// <param name="str_getSteps">傳回所有Steps 名稱，以 ',' 隔開。eg. "pcb,array,panel" </param>
        /// <param name="str_getLayers">傳回所有Layers 名稱，以 ',' 隔開。eg. "ssk,l1,l2,sold,pth" </param>
        /// <param name="loadOdbTgzTp">設定要讀入 Odb目錄 或 TGZ檔案</param>
        /// <param name="stepsListTp">指定讀入Steps的方式(繼承模式、獨立Steps....)</param>
        /// <param name="loadOnlySteps">設定只讀入 Steps</param>
        /// <param name="loadOnlyLayers">設定只讀入 Layers</param>
        public static bool CS_RvCam_Load_ODB_Dialog(
            ref string str_getOdbDir_TGZFile,
            ref string str_getSteps,
            ref string str_getLayers,
            TOdbTgzType loadOdbTgzTp = TOdbTgzType.otOdbFolder
            )
        {
            // 非 ref string，做轉換------------
            //IntPtr ploadOnlySteps = Marshal.StringToBSTR(selectedSteps);
            //IntPtr ploadOnlyLayers = Marshal.StringToBSTR(selectedLayers);

            // ref string, DLL 內配置記憶體，在此不需配置-----------
            IntPtr pRefstr_getOdbDir_TGZFile = IntPtr.Zero;
            IntPtr pRefstr_getSteps = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);
            IntPtr pRefstr_getLayers = IntPtr.Zero;



            TReturnCode ret = RvCam_Load_ODB_Dialog(
                ref pRefstr_getOdbDir_TGZFile,
                ref pRefstr_getSteps,
                ref pRefstr_getLayers,
                loadOdbTgzTp
            );

            if (ret == TReturnCode.rcSuccess)
            {
                str_getOdbDir_TGZFile = Marshal.PtrToStringUni(pRefstr_getOdbDir_TGZFile);
                str_getSteps = Marshal.PtrToStringUni(pRefstr_getSteps);
                str_getLayers = Marshal.PtrToStringUni(pRefstr_getLayers);
            }

            //Marshal.FreeHGlobal(sXXX);

            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Load_CalibrationData", CallingConvention = TargetCallingConvention)]
            private static extern TReturnCode RvCam_Load_CalibrationData(
            IntPtr sLoadFileName
        );
        public static bool CS_RvCam_Load_CalibrationData(
            string str_sLoadFileName
            )
        {
            // 非 ref string，做轉換------------
            IntPtr pstr_sLoadFileName = Marshal.StringToBSTR(str_sLoadFileName);

            // ref string, DLL 內配置記憶體，在此不需配置------------
            //IntPtr pRef = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);


            TReturnCode ret = RvCam_Load_CalibrationData(
                pstr_sLoadFileName
            );

            //Marshal.FreeHGlobal(sXXX);

            return (TReturnCode.rcSuccess ==  ret);
        }
        #endregion


        // Paint Functions =================================================
        #region Paint Functions ===========================================
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Paint_Canvas", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Paint_Canvas(
            int paintStep, int paintLayer,
            int paintColor_AARRGGBB,
            int canvasID,
            ref IntPtr CnvScan0,
            int CnvRowBytes, int CnvWidth, int CnvHeight, int CnvBitsPerPixel,
            bool cnvDIBUpWard = true,
            TVectPaintMode paintMode = TVectPaintMode.pmSolid_Normal,
            TCompensateMode paintCompensateMode = TCompensateMode.cmNonCompensate
        );
        /// <summary>繪圖函數</summary>
        /// <param name="paintStep">Step ID</param>
        /// <param name="paintLayer">Layer ID</param>
        /// <param name="paintColor">繪圖顏色</param>
        /// <param name="canvasID">所屬畫布(eg.PictureBox) ID</param>
        /// <param name="CnvScan0">圖形位址指標</param>
        /// <param name="CnvRowBytes">圖形每列byte數</param>
        /// <param name="CnvWidth">圖形寬度 Pixel</param>
        /// <param name="CnvHeight">圖形高度pixel</param>
        /// <param name="CnvBitsPerPixel">圖形每個像素多少bit</param>
        /// <param name="cnvDIBUpWard">圖形資料是否由下到上儲存</param>
        /// <param name="paintMode">繪圖模式</param>
        public static bool CS_RvCam_Paint_Canvas(
            int paintStep, int paintLayer,
            Color paintColor,
            int canvasID,
            ref IntPtr CnvScan0,
            int CnvRowBytes, int CnvWidth, int CnvHeight, int CnvBitsPerPixel,
            bool cnvDIBUpWard = true,
            TVectPaintMode paintMode = TVectPaintMode.pmSolid_Normal,
            TCompensateMode paintCompensateMode = TCompensateMode.cmNonCompensate
            )
        {
            TReturnCode ret = RvCam_Paint_Canvas(
                paintStep, paintLayer,
                Convert_ColorToIntAARRGGBB(paintColor),
                canvasID,
                ref CnvScan0,
                CnvRowBytes, CnvWidth, CnvHeight, CnvBitsPerPixel,
                cnvDIBUpWard,
                paintMode,
                paintCompensateMode
            );

            return (ret == TReturnCode.rcSuccess);
        }
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Paint_Canvas_Ruler", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Paint_Canvas_Ruler(
        int canvasID,
        int paintColor_AARRGGBB,
        TValueUnit setDisplayUnit,
        ref IntPtr CnvScan0,
        int CnvRowBytes, int CnvWidth, int CnvHeight, int CnvBitsPerPixel,
        bool cnvDIBUpWard = true,
        int rulerPixelWidth = 50
    );
        public static bool CS_RvCam_Paint_Canvas_Ruler(
            int canvasID,
            Color paintColor,
            TValueUnit setDisplayUnit,
            ref IntPtr CnvScan0,
            int CnvRowBytes, int CnvWidth, int CnvHeight, int CnvBitsPerPixel,
            bool cnvDIBUpWard = true,
            int rulerPixelWidth = 50
            )
        {
            TReturnCode ret = RvCam_Paint_Canvas_Ruler(
                canvasID,
                Convert_ColorToIntAARRGGBB(paintColor),
                setDisplayUnit,
                ref CnvScan0,
                CnvRowBytes, CnvWidth, CnvHeight, CnvBitsPerPixel,
                cnvDIBUpWard,
                rulerPixelWidth
            );

            return (TReturnCode.rcSuccess == ret);
        }
        #endregion


        // Query Functions ====================================================
        #region Query Functions -----------------------------------------
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Query_IsEmpty_StepLayer", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Query_IsEmpty_StepLayer(
           int qryStep, int qryLayer,
           ref bool getIsEmpty
       );
        /// <summary>檢查 Step/Layer 是否有資料</summary>
        public static bool CS_RvCam_Query_IsEmpty_StepLayer(
            int qryStep, int qryLayer
            )
        {
            bool getIsEmpty = true;

            TReturnCode ret = RvCam_Query_IsEmpty_StepLayer(
                qryStep, qryLayer,
                ref getIsEmpty
            );

            return getIsEmpty;
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Query_ObjectInfo", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Query_ObjectInfo(
        int qryStep, int qryLayer,
        TFloat qryXmm, TFloat qryYmm, TFloat qryTolmm,
        ref TFloat getObjCXmm, ref TFloat getObjCYmm, ref TFloat getObjWmm, ref TFloat getObjHmm,
        ref IntPtr getObjectInfo
    );
        public static bool CS_RvCam_Query_ObjectInfo(
            int qryStep, int qryLayer,
            TFloat qryXmm, TFloat qryYmm, TFloat qryTolmm,
            ref TFloat getObjCXmm, ref TFloat getObjCYmm, ref TFloat getObjWmm, ref TFloat getObjHmm,
            ref string getSymbolName,
            ref string getSymbTp,
            ref string getObjStepName,
            ref string str_getObjectInfo
            )
        {
            // 非 ref string，做轉換------------
            //IntPtr p = Marshal.StringToBSTR();

            // ref string, DLL 內配置記憶體，在此不需配置------------
            IntPtr pRefstr_getObjectInfo = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);

            TReturnCode ret = RvCam_Query_ObjectInfo(
                qryStep, qryLayer,
                qryXmm, qryYmm, qryTolmm,
                ref getObjCXmm, ref getObjCYmm, ref getObjWmm, ref getObjHmm,
                ref pRefstr_getObjectInfo
            );

            if (ret == TReturnCode.rcSuccess)
            {
                str_getObjectInfo = Marshal.PtrToStringUni(pRefstr_getObjectInfo);


                //S(symbTp@symbName):wh(wMm,hMm):cxy(Xmm,Ymm):StepLayer(stepName@lyrName)

                string[] sInfos = str_getObjectInfo.Split(['(', ')', ':']);
                //symbTp@symbName
                string[] symbs = sInfos[1].Split("@");
                getSymbTp = symbs[0];
                getSymbolName = symbs[1];

                string[] stpLyrs = sInfos[10].Split("@");
                getObjStepName = stpLyrs[0];
            }

            //Marshal.FreeHGlobal(sXXX);


            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Query_MinMax", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Query_MinMax(
            int qryStep, int qryLayer,
            TCamTarget qryTarget,
            ref TFloat getMinXmm, ref TFloat getMinYmm, ref TFloat getMaxXmm, ref TFloat getMaxYmm
        );
        public static bool CS_RvCam_Query_MinMax(
            int qryStep, int qryLayer,
            TCamTarget qryTarget,
            ref TFloat getMinXmm, ref TFloat getMinYmm, ref TFloat getMaxXmm, ref TFloat getMaxYmm
            )
        {
            TReturnCode ret = RvCam_Query_MinMax(
                qryStep, qryLayer,
                qryTarget,
                ref getMinXmm, ref getMinYmm, ref getMaxXmm, ref getMaxYmm
            );

            return (TReturnCode.rcSuccess == ret);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Query_BlobCXY_Scan0", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Query_BlobCXY_Scan0(
        TFloat inImgX, TFloat inImgY,
        ref TFloat getBlobCX, ref TFloat getBlobCY,
        ref TFloat getBlobW, ref TFloat getBlobH,
        IntPtr imgScan0,
        int imgRowBytes, int imgWidth, int imgHeight, int imgBitsPerPixel,
        bool imgDIBUpWard = true
        );
        /// <summary>
        /// 取的影像資料 Scan0 Blob 中心 (pixel)
        /// </summary>
        /// <param name="inImgX"></param>
        /// <param name="inImgY"></param>
        /// <param name="getBlobCX"></param>
        /// <param name="getBlobCY"></param>
        /// <param name="imgScan0"></param>
        /// <param name="imgRowBytes"></param>
        /// <param name="imgWidth"></param>
        /// <param name="imgHeight"></param>
        /// <param name="imgBitsPerPixel"></param>
        /// <param name="imgDIBUpWard"></param>
        /// <returns></returns>
        public static bool CS_RvCam_Query_BlobCXY_Scan0(
            TFloat inImgX, TFloat inImgY,
            ref TFloat getBlobCX, ref TFloat getBlobCY,
            ref TFloat getBlobW, ref TFloat getBlobH,
            IntPtr imgScan0,
            int imgRowBytes, int imgWidth, int imgHeight, int imgBitsPerPixel,
            bool imgDIBUpWard = true
            )
        {
            TReturnCode ret = RvCam_Query_BlobCXY_Scan0(
                inImgX, inImgY,
                ref getBlobCX, ref getBlobCY,
                ref getBlobW, ref getBlobH,
                imgScan0,
                imgRowBytes, imgWidth, imgHeight, imgBitsPerPixel,
                imgDIBUpWard
            );

            return (TReturnCode.rcSuccess == ret);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Query_BlobCXY_Layer", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Query_BlobCXY_Layer(
            int atStep, int atLayer,
            TFloat inCXmm, TFloat inCYmm,
            ref TFloat getBlobCXmm, ref TFloat getBlobCYmm,
            ref TFloat getBlobCXpixel, ref TFloat getBlobCYpixel,
            ref TFloat getBlobWmm, ref TFloat getBlobHmm,
            ref IntPtr getObjectInfo
        );
        /// <summary>
        /// 取的Layer影像資料上的 Blob中心 (mm)
        /// </summary>
        /// <param name="inCXmm"></param>
        /// <param name="inCYmm"></param>
        /// <param name="getBlobCXmm"></param>
        /// <param name="getBlobCYmm"></param>
        /// <param name="qryStep"></param>
        /// <param name="qryLayer"></param>
        /// <returns></returns>
        public static bool CS_RvCam_Query_BlobCXY_Layer(
            int qryStep, int qryLayer,
            TFloat inCXmm, TFloat inCYmm,
            ref TFloat getBlobCXmm, ref TFloat getBlobCYmm,
            ref TFloat getBlobCXpixel, ref TFloat getBlobCYpixel,
            ref TFloat getBlobWmm, ref TFloat getBlobHmm,
            ref string sSymbolName,
            ref string sSymbTp,
            ref string getObjStepName,
            ref string sObjectInfo
            )
        {
            // ref string, DLL 內配置記憶體，在此不需配置------------
            IntPtr pRefsObjectInfo = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);

            TReturnCode ret = RvCam_Query_BlobCXY_Layer(
                qryStep, qryLayer,
                inCXmm, inCYmm,
                ref getBlobCXmm, ref getBlobCYmm,
                ref getBlobCXpixel, ref getBlobCYpixel,
                ref getBlobWmm, ref getBlobHmm,
                ref pRefsObjectInfo
            );
            
            if (ret == TReturnCode.rcSuccess)
            {
                sObjectInfo = Marshal.PtrToStringUni(pRefsObjectInfo);

                //S(symbTp@symbName):wh(wMm,hMm):cxy(Xmm,Ymm):StepLayer(stepName@lyrName)
                string[] sInfos = sObjectInfo.Split(['(', ')', ':']);
                //symbTp@symbName
                string[] symbs = sInfos[1].Split("@");
                sSymbolName = symbs[1];
                sSymbTp = symbs[0];

                string[] stpLyrs = sInfos[10].Split("@");
                getObjStepName = stpLyrs[0];
            }
            return (TReturnCode.rcSuccess == ret);
        }
        #endregion


        // Set Functions ===================================================
        #region Set Functions----------------------------------------"*/
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Set_Visible_PaintObjects", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Set_Visible_PaintObjects(
            bool childSteps = true,
            bool pads = true,
            bool lines = true,
            bool polygons = true
        );
        /// <summary>設定顯示/隱藏 物件</summary>
        public static TReturnCode CS_RvCam_Set_Visible_PaintObjects(
            bool childSteps = true,
            bool pads = true,
            bool lines = true,
            bool polygons = true
            )
        {
            TReturnCode ret = RvCam_Set_Visible_PaintObjects(
                childSteps,
                pads,
                lines,
                polygons
            );

            return ret;
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Set_RenderColor", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Set_RenderColor(
                    TVPRenderColor renderClr
                );
        /// <summary>
        /// 設定顏色顯示模式
        /// </summary>
        /// <param name="renderClr">層顏色模式 或 物件別顯示 或 ......</param>
        /// <returns></returns>
        public static bool CS_RvCam_Set_RenderColor(
            TVPRenderColor renderClr
            )
        {
            TReturnCode ret = RvCam_Set_RenderColor(
                renderClr
            );

            return (ret == TReturnCode.rcSuccess);
        }
        #endregion


        // Render Functions ===================================================
        #region "Render Functions----------------------------------------"*/
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Render_Image_CAD", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Render_Image_CAD(
            IntPtr cadFn,
            TFloat renderResolution_MmPerPixel,
            ref IntPtr pGetImageStart0,
            ref int getImageSizeTotalMB, ref int getStride_BytesPerRow, ref int getImagePixelWidth, ref int getImagePixelHeight,
            Byte atBitPerPixel = 8,
            IntPtr pAssignRenderMinMaxMm = default,
            IntPtr pDoSaveToBmpFile = default,
            TVectPaintMode paintMode = TVectPaintMode.pmSolid_Normal
        );
        /// <summary>
        /// 背景讀入一個CAD檔案，算圖傳回圖形資料，可指定同時輸出Bmp檔名。
        /// </summary>
        /// <param name="str_cadFn">CAD檔名</param>
        /// <param name="renderResolution_MmPerPixel">算圖解析度 Mm/Pixel</param>
        /// <param name="pGetImageStart0">傳回圖形資料 pScan0</param>
        /// <param name="getImageSizeTotalMB">傳回圖形大小 MB</param>
        /// <param name="getStride_BytesPerRow">傳回圖形每列Byte數</param>
        /// <param name="getImagePixelWidth">傳回圖形寬度 Pixel</param>
        /// <param name="getImagePixelHeight">傳回圖形高度 Pixel</param>
        /// <param name="atBitPerPixel">圖形像素Bits數</param>
        /// <param name="pAssignRenderMinMaxMm">是否指定輸出範圍(TRect)mm</param>
        /// <param name="str_pDoSaveToBmpFile">是否同時輸出 到指定 Bmp檔案</param>
        /// <param name="paintMode">繪圖模式 TVectPaintMode</param>
        /// <returns></returns>
        public static bool CS_RvCam_Render_Image_CAD(
            string str_cadFn,
            TFloat renderResolution_MmPerPixel,
            ref IntPtr pGetImageStart0,
            ref int getImageSizeTotalMB, ref int getStride_BytesPerRow, ref int getImagePixelWidth, ref int getImagePixelHeight,
            Byte atBitPerPixel = 8,
            IntPtr pAssignRenderMinMaxMm = default,
            string str_pDoSaveToBmpFile = "",
            TVectPaintMode paintMode = TVectPaintMode.pmSolid_Normal
            )
        {
            // 非 ref string，做轉換------------
            IntPtr pstr_cadFn = Marshal.StringToBSTR(str_cadFn);
            IntPtr pstr_pDoSaveToBmpFile = Marshal.StringToBSTR(str_pDoSaveToBmpFile);

            // ref string, DLL 內配置記憶體，在此不需配置------------
            //IntPtr pRef = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);


            TReturnCode ret = RvCam_Render_Image_CAD(
                pstr_cadFn,
                renderResolution_MmPerPixel,
                ref pGetImageStart0,
                ref getImageSizeTotalMB, ref getStride_BytesPerRow, ref getImagePixelWidth, ref getImagePixelHeight,
                atBitPerPixel,
                pAssignRenderMinMaxMm,
                pstr_pDoSaveToBmpFile,
                paintMode
            );

            return (TReturnCode.rcSuccess == ret);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Render_Image_ODB", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Render_Image_ODB(
            IntPtr odbTgzFn,
            IntPtr renderStepName, IntPtr renderLayerNames,
            TFloat renderResolution_MmPerPixel,
            ref IntPtr pGetImageStart0,
            ref int getImageSizeTotalMB, ref int getStride_BytesPerRow,
            ref int getImagePixelWidth, ref int getImagePixelHeight,
            Byte atBitPerPixel = 8,
            IntPtr pAssignRenderMinMaxMm = default,
            IntPtr pDoSaveToDirectory = default,
            TVectPaintMode paintMode = TVectPaintMode.pmSolid_Normal
        );
        /// <summary>
        /// 背景讀入CAD檔案，算圖傳回圖形資料，可指定同時輸出Bmp檔名。
        /// </summary>
        /// <param name="str_odbTgzFn">背景讀入 ODB目錄 或 TGZ檔名</param>
        /// <param name="str_renderStepName">指定算圖Step名稱</param>
        /// <param name="str_renderLayerNames">指定算圖Layer名稱，可多筆，eg. "comp,l2,l3"</param>
        /// <param name="renderResolution_MmPerPixel">算圖解析度 Mm/Pixel</param>
        /// <param name="pGetImageStart0">傳回圖形資料 pScan0</param>
        /// <param name="getImageSizeTotalMB">傳回圖形大小 MB</param>
        /// <param name="getStride_BytesPerRow">傳回圖形每列Byte數</param>
        /// <param name="getImagePixelWidth">傳回圖形寬度 Pixel</param>
        /// <param name="getImagePixelHeight">傳回圖形高度 Pixel</param>
        /// <param name="atBitPerPixel">圖形像素Bits數</param>
        /// <param name="pAssignRenderMinMaxMm">是否指定輸出範圍(TRect)mm</param>
        /// <param name="str_pDoSaveToDirectory">是否同時輸出Bmp檔案(s) 到指定目錄內</param>
        /// <param name="paintMode">繪圖模式 TVectPaintMode</param>
        /// <returns></returns>
        public static bool CS_RvCam_Render_Image_ODB(
            string str_odbTgzFn,
            string str_renderStepName, string[] str_renderLayerNames,
            TFloat renderResolution_MmPerPixel,
            ref IntPtr pGetImageStart0,
            ref int getImageSizeTotalMB, ref int getStride_BytesPerRow,
            ref int getImagePixelWidth, ref int getImagePixelHeight,
            Byte atBitPerPixel = 8,
            IntPtr pAssignRenderMinMaxMm = default,
            string str_pDoSaveToDirectory = "",
            TVectPaintMode paintMode = TVectPaintMode.pmSolid_Normal
            )
        {
            string sRenderLayers = Convert_StringArray_To_String(str_renderLayerNames, ",");
            // 非 ref string，做轉換------------
            IntPtr pstr_odbTgzFn = Marshal.StringToBSTR(str_odbTgzFn);
            IntPtr pstr_renderStepName = Marshal.StringToBSTR(str_renderStepName);
            IntPtr pstr_renderLayerNames = Marshal.StringToBSTR(sRenderLayers);
            IntPtr pstr_pDoSaveToDirectory = Marshal.StringToBSTR(str_pDoSaveToDirectory);

            // ref string, DLL 內配置記憶體，在此不需配置------------
            //IntPtr pRef = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);


            TReturnCode ret = RvCam_Render_Image_ODB(
                pstr_odbTgzFn,
                pstr_renderStepName, pstr_renderLayerNames,
                renderResolution_MmPerPixel,
                ref pGetImageStart0,
                ref getImageSizeTotalMB, ref getStride_BytesPerRow, ref getImagePixelWidth, ref getImagePixelHeight,
                atBitPerPixel,
                pAssignRenderMinMaxMm,
                pstr_pDoSaveToDirectory,
                paintMode
            );

            return (TReturnCode.rcSuccess == ret);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Render_Image_StepLayer", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Render_Image_StepLayer(
            int renderStepID, int renderLayerID,
            TFloat renderResolution_MmPerPixel,
            ref IntPtr pGetImageStart0,
            ref int getImageSizeTotalMB, ref int getStride_BytesPerRow, ref int getImagePixelWidth, ref int getImagePixelHeight,
            Byte atBitPerPixel = 8,
            IntPtr pAssignRenderMinMaxMm = default,
            IntPtr pDoSaveToBmpFile = default,
            TVectPaintMode paintMode = TVectPaintMode.pmSolid_Normal
        );
        /// <summary>
        /// 輸入StepID / LayerID，算圖傳回圖形資料，可指定同時輸出Bmp檔名。
        /// </summary>
        /// <param name="renderStepID">Step ID</param>
        /// <param name="renderLayerID">Layer ID</param>
        /// <param name="renderResolution_MmPerPixel">算圖解析度 Mm/Pixel</param>
        /// <param name="pGetImageStart0">傳回圖形資料 pScan0</param>
        /// <param name="getImageSizeTotalMB">傳回圖形大小 MB</param>
        /// <param name="getStride_BytesPerRow">傳回圖形每列Byte數</param>
        /// <param name="getImagePixelWidth">傳回圖形寬度 Pixel</param>
        /// <param name="getImagePixelHeight">傳回圖形高度 Pixel</param>
        /// <param name="atBitPerPixel">圖形像素Bits數</param>
        /// <param name="pAssignRenderMinMaxMm">是否指定輸出範圍(TRect)mm</param>
        /// <param name="str_pDoSaveToBmpFile">是否同時輸出 到指定 Bmp檔案</param>
        /// <param name="paintMode">繪圖模式 TVectPaintMode</param>
        /// <returns></returns>
        public static bool CS_RvCam_Render_Image_StepLayer(
            int renderStepID, int renderLayerID,
            TFloat renderResolution_MmPerPixel,
            ref IntPtr pGetImageStart0,
            ref int getImageSizeTotalMB, ref int getStride_BytesPerRow, ref int getImagePixelWidth, ref int getImagePixelHeight,
            Byte atBitPerPixel = 8,
            IntPtr pAssignRenderMinMaxMm = default,
            string str_pDoSaveToBmpFile = "",
            TVectPaintMode paintMode = TVectPaintMode.pmSolid_Normal
            )
        {
            // 非 ref string，做轉換------------
            IntPtr pstr_pDoSaveToBmpFile = Marshal.StringToBSTR(str_pDoSaveToBmpFile);

            // ref string, DLL 內配置記憶體，在此不需配置------------
            //IntPtr pRef = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);


            TReturnCode ret = RvCam_Render_Image_StepLayer(
                renderStepID, renderLayerID,
                renderResolution_MmPerPixel,
                ref pGetImageStart0,
                ref getImageSizeTotalMB, ref getStride_BytesPerRow, ref getImagePixelWidth, ref getImagePixelHeight,
                atBitPerPixel,
                pAssignRenderMinMaxMm,
                pstr_pDoSaveToBmpFile,
                paintMode
            );

            return (TReturnCode.rcSuccess == ret);
        }
        #endregion


        // Select Functions =================================================
        #region "Select Functions---------------------------------------"
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Select_Objects", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Select_Objects(
            int selectStep, int selectLayer,
            TFloat selectCXmm, TFloat selectCYmm, TFloat selectWidthXmm, TFloat selectHeightmm,
            ref int getSelectedObjectsCount,
            TSelectAction selectAction = TSelectAction.saSelect,
            TActionTarget selectTarget = TActionTarget.smTVectObject
        );
        /// <summary>
        /// Select/Delete/Freeze Objects
        /// </summary>
        /// <param name="selectStep">stepbID</param>
        /// <param name="selectLayer">layer ID</param>
        /// <param name="selectCXmm">選取中心座標X(mm)</param>
        /// <param name="selectCYmm">選取中心座標Y(mm)</param>
        /// <param name="selectWidthXmm">選取寬度W(mm)，如果選取全部則=0.0</param>
        /// <param name="selectHeightmm">選取高度H(mm)，如果選取全部則=0.0</param>
        /// <param name="getSelectedObjectsCount">傳回選取的物件數量</param>
        /// <param name="selectAction">選取後的動作</param>
        /// <param name="selectTarget">編輯的目標物件類型</param>
        /// <returns></returns>
        public static bool CS_RvCam_Select_Objects(
            int selectStep, int selectLayer,
            TFloat selectCXmm, TFloat selectCYmm, TFloat selectWidthXmm, TFloat selectHeightmm,
            ref int getSelectedObjectsCount,
            TSelectAction selectAction = TSelectAction.saSelect,
            TActionTarget selectTarget = TActionTarget.smTVectObject
            )
        {
            TReturnCode ret = RvCam_Select_Objects(
                selectStep, selectLayer,
                selectCXmm, selectCYmm, selectWidthXmm, selectHeightmm,
                ref getSelectedObjectsCount,
                selectAction,
                selectTarget
            );

            return (TReturnCode.rcSuccess == ret);
        }
        //以SymbolName選取
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Select_Objects_BySymbolName", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Select_Objects_BySymbolName(
            int selectStep, int selectLayer,
            IntPtr selectSymbolName,
            ref int getSelectedObjectsCount,
            TSelectAction selectAction = TSelectAction.saSelect
        );
        /// <summary>
        /// 傳入Symbol內，選取所有屬於該Symbol的物件
        /// </summary>
        /// <param name="selectStep">選取的StepID</param>
        /// <param name="selectLayer">選取的LayerID</param>
        /// <param name="str_selectSymbolName">選取的SymbolName</param>
        /// <param name="getSelectedObjectsCount">傳回選取的數量</param>
        /// <param name="selectAction">選取動作</param>
        /// <returns></returns>
        public static bool CS_RvCam_Select_Objects_BySymbolName(
            int selectStep, int selectLayer,
            string str_selectSymbolName,
            ref int getSelectedObjectsCount,
            TSelectAction selectAction = TSelectAction.saSelect
            )
        {
            // 非 ref string，做轉換------------
            IntPtr pstr_selectSymbolName = Marshal.StringToBSTR(str_selectSymbolName);

            // ref string, DLL 內配置記憶體，在此不需配置------------
            //IntPtr pRef = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);


            TReturnCode ret = RvCam_Select_Objects_BySymbolName(
                selectStep, selectLayer,
                pstr_selectSymbolName,
                ref getSelectedObjectsCount,
                selectAction
            );

            //Marshal.FreeHGlobal(sXXX);


            return (TReturnCode.rcSuccess == ret);
        }
        #endregion


        // Save Functions ===================================================
        #region Save Functions----------------------------------------"*/
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Save_CAD", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Save_CAD(
            ref IntPtr sSaveCadFileName,
            IntPtr saveStepName, IntPtr saveLayerName,
            TVectFileType saveCadFileType = TVectFileType.vtGerber274X
        );
        /// <summary>
        /// 無顯示介面,背景 儲存 CAD檔案 (*.GBX, *.DXF, *.NC, *.DWG...)
        /// </summary>
        /// <param name="str_sSaveCadFileName">設定儲存檔案</param>
        /// <param name="str_saveStepName">設定儲存 Step</param>
        /// <param name="str_saveLayerName">設定儲存 Layers</param>
        /// <param name="saveCadFileType">設定儲存的檔案類型</param>
        /// <returns></returns>
        public static bool CS_RvCam_Save_CAD(
            ref string str_sSaveCadFileName,
            string str_saveStepName, string str_saveLayerName,
            TVectFileType saveCadFileType = TVectFileType.vtGerber274X
            )
        {
            // 非 ref string，做轉換------------
            IntPtr pstr_saveStepName = Marshal.StringToBSTR(str_saveStepName);
            IntPtr pstr_saveLayerName = Marshal.StringToBSTR(str_saveLayerName);

            // ref string, DLL 內配置記憶體，在此不需配置------------
            IntPtr pstr_sSaveCadFileName = Marshal.StringToBSTR(str_sSaveCadFileName); //Marshal.AllocHGlobal(cMaxAllocateStringSize);

            TReturnCode ret = RvCam_Save_CAD(
                ref pstr_sSaveCadFileName,
                pstr_saveStepName, pstr_saveLayerName,
                saveCadFileType
            );

            if (ret == TReturnCode.rcSuccess)
            {
                str_sSaveCadFileName = Marshal.PtrToStringUni(pstr_sSaveCadFileName);
            }

            //Marshal.FreeHGlobal(sXXX);


            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Save_CAD_Dialog", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Save_CAD_Dialog(
            ref IntPtr getSavedCadFileName,
            IntPtr saveStepName, IntPtr saveLayerName,
            TVectFileType saveFileType = TVectFileType.vtGerber274X
        );
        /// <summary>儲存 CAD檔案 (*.GBX, *.DXF, *.NC, *.DWG...) (存檔介面)</summary>
        public static bool CS_RvCam_Save_CAD_Dialog(
            ref string str_getSavedCadFileName,
            string str_saveStepName, string str_saveLayerName,
            TVectFileType saveFileType = TVectFileType.vtGerber274X
            )
        {
            // 非 ref string，做轉換------------
            IntPtr pstr_saveStepName = Marshal.StringToBSTR(str_saveStepName);
            IntPtr pstr_saveLayerName = Marshal.StringToBSTR(str_saveLayerName);

            // ref string, DLL 內配置記憶體，在此不需配置------------
            IntPtr pstr_getSavedCadFileName = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);

            TReturnCode ret = RvCam_Save_CAD_Dialog(
                ref pstr_getSavedCadFileName,
                pstr_saveStepName, pstr_saveLayerName,
                saveFileType
            );

            if (ret == TReturnCode.rcSuccess)
            {
                str_getSavedCadFileName = Marshal.PtrToStringUni(pstr_getSavedCadFileName);
            }

            //Marshal.FreeHGlobal(sXXX);


            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Save_Image_FromScan0", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Save_Image_FromScan0(
        IntPtr saveImageFileName,
        IntPtr pSaveImageScan0,
        int imageSizeTotalMB,
        int stride_BytesPerRow, int imagePixelWidth, int imagePixelHeight,
        Byte atBitPerPixel
    );
        public static bool CS_RvCam_Save_Image_FromScan0(
            string str_saveImageFileName,
            IntPtr pSaveImageScan0,
            int imageSizeTotalMB,
            int stride_BytesPerRow, int imagePixelWidth, int imagePixelHeight,
            Byte atBitPerPixel
            )
        {
            // 非 ref string，做轉換------------
            IntPtr pstr_saveImageFileName = Marshal.StringToBSTR(str_saveImageFileName);

            // ref string, DLL 內配置記憶體，在此不需配置------------
            //IntPtr pRef = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);

            //Marshal.FreeHGlobal(sXXX);

            TReturnCode ret = RvCam_Save_Image_FromScan0(
                pstr_saveImageFileName,
                pSaveImageScan0,
                imageSizeTotalMB,
                stride_BytesPerRow, imagePixelWidth, imagePixelHeight,
                atBitPerPixel
            );

            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Save_CalibrationData", CallingConvention = TargetCallingConvention)]
            private static extern TReturnCode RvCam_Save_CalibrationData(
            IntPtr sSaveFileName
        );
        public static bool CS_RvCam_Save_CalibrationData(
            string str_sSaveFileName
            )
        {
            // 非 ref string，做轉換------------
            IntPtr pstr_sSaveFileName = Marshal.StringToBSTR(str_sSaveFileName);

            // ref string, DLL 內配置記憶體，在此不需配置------------
            //IntPtr pRef = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);


            //Marshal.FreeHGlobal(sXXX);

            TReturnCode ret = RvCam_Save_CalibrationData(
                pstr_sSaveFileName
            );

            return (TReturnCode.rcSuccess == ret);
        }
        #endregion


        // Update Functions =========================================================
        #region "Update Functions ------------------------------------------------}
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Update_LayerData", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Update_LayerData(
            int atStep,
            int updateLayer,
            IntPtr pShapeDataArray0,
            int shapeDataLength
        );
        /// <summary>
        /// 取代並更新 Layer Data
        /// </summary>
        /// <param name="atStep">Step</param>
        /// <param name="updateLayer">取代並更新的Layer</param>
        /// <param name="pShapeDataArray0">&TVectSimpleShape[0]</param>
        /// <param name="shapeDataLength">Array Length</param>
        /// <returns></returns>
        public static bool CS_RvCam_Update_LayerData(
            int atStep,
            int updateLayer,
            TVectSimpleShape[] shpsData
            )
        {
            TReturnCode ret = TReturnCode.rcFail;

            if (shpsData.Length <= 0) return false;

            unsafe
            {
                fixed (TVectSimpleShape* pShps0 = &(shpsData[0]))
                {
                    ret = RvCam_Update_LayerData(
                        atStep,
                        updateLayer,
                        (IntPtr)pShps0,
                        shpsData.Length
                        );
                }
            }

            return (ret == TReturnCode.rcSuccess);
        }
        #endregion


        // View Functions =================================================
        #region View Functions ===========================================

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_View_Store", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_View_Store(
            int canvasID
        );
        /// <summary>儲存目前的View</summary>
        public static bool CS_RvCam_View_Store(
            int canvasID
            )
        {
            TReturnCode ret = RvCam_View_Store(
                canvasID
            );

            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_View_Update", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_View_Update(
            int canvasID,
            TFloat cnvW_viewX_viewDeg_atMmPerPxl, TFloat cnvH_viewY_viewMrX,
            int viewStep, int viewLayer,
            TViewMode atViewMode = TViewMode.vmHome
                     );
        /// <summary>
        /// 更新 繪圖的View 資料
        /// </summary>
        /// <param name="canvasID">畫布 ID</param>
        /// <param name="cnvW_viewX_viewDeg_atMmPerPxl">canvasW or viewCX or viewDegree or ViewResolution</param>
        /// <param name="cnvH_viewY_viewMrX">canvasH or viewCY or viewMirrorX</param>
        /// <param name="viewStep">Step ID</param>
        /// <param name="viewLayer">Layer ID</param>
        /// <param name="atViewMode">View 模式</param>
        /// <returns></returns>
        public static bool CS_RvCam_View_Update(
            int canvasID,
            TFloat cnvW_viewX_viewDeg_atMmPerPxl, TFloat cnvH_viewY_viewMrX,
            int viewStep, int viewLayer,
            TViewMode atViewMode = TViewMode.vmHome
            )
        {
            TReturnCode ret = RvCam_View_Update(
                canvasID,
                cnvW_viewX_viewDeg_atMmPerPxl,
                cnvH_viewY_viewMrX,
                viewStep, viewLayer,
                atViewMode
            );

            return (ret == TReturnCode.rcSuccess);
        }


        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_View_Update_ViewMinMax", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_View_Update_ViewMinMax(
            int canvasID,
            TFloat viewMinXmm, TFloat ViewMinYmm, TFloat ViewMaxXmm, TFloat viewMaxYmm,
            int viewStep, int viewLayer
        );
        /// <summary>以檢視範圍更新目前的View </summary>
        public static bool CS_RvCam_View_Update_ViewMinMax(
            int canvasID,
            TFloat viewMinXmm, TFloat ViewMinYmm, TFloat ViewMaxXmm, TFloat viewMaxYmm,
            int viewStep, int viewLayer
            )
        {
            TReturnCode ret = RvCam_View_Update_ViewMinMax(
                canvasID,
                viewMinXmm, ViewMinYmm, ViewMaxXmm, viewMaxYmm,
                viewStep, viewLayer
            );

            return (ret == TReturnCode.rcSuccess);
        }
        #endregion


        // NonDLL Functions =================================================
        #region NonDLL Functions ===========================================
        private static void PlugInMenuItemClickHandler(object sender, EventArgs e)
        {
            ToolStripMenuItem clickedItem = (ToolStripMenuItem)sender;
            // Take some action based on the data in clickedItem

            int dllId = int.Parse((sender as ToolStripMenuItem).Tag.ToString());

#if DEBUG
            //MessageBox.Show(string.Format("{0} called.",
            //    ClsRvCamDLL_PlugInDLL_FileIO.FPlugInDLLFileNames[dllId]));
#endif
        }
        private static void PlugInMethodMenuItemClickHandler(object sender, EventArgs e)
        {
            ToolStripMenuItem senderItem = (ToolStripMenuItem)sender;
            ToolStripMenuItem ownerItem = (ToolStripMenuItem)(senderItem.GetCurrentParent() as ToolStripDropDown).OwnerItem;

            int dllId = int.Parse(ownerItem.Tag.ToString());
            int methodID = int.Parse(senderItem.Tag.ToString());

            string loadSaveFileName = "";

#if DEBUG
            //MessageBox.Show(string.Format(" \"{0}.{1}( )\"  called.",
            //    Path.GetFileName(ClsRvCamDLL_PlugInDLL_FileIO.FPlugInDLLFileNames[dllId]),
            //    ClsRvCamDLL_PlugInDLL_FileIO.FPlugInMethodNames[methodID]));
#endif

#if AutoLoadPlugins  //自動讀取 ./PlugIns/ 下的所有 DLL.

            switch (methodID)
            {
                case 0: //Is_RvCamDLL
                    break;

                case 1: // Load_File
                    #region Load_File
                    if (ClsRvCamDLL_PlugInDLL_FileIO.LoadLibraryMethod_And_Run(dllId, methodID, ref loadSaveFileName,
                            ref ClsRvCamDLL_PlugInDLL_FileIO.FVectShapeData))
                    {
                        if (ClsRvCamDLL.CS_RvCam_Add_LayerData(FActStep, ref FActLayer,
                            ClsRvCamDLL_PlugInDLL_FileIO.FVectShapeData,
                            ClsRvCamDLL_PlugInDLL_FileIO.FVectShapeDataFileName))
                        {

                            if (null != FUpdateInterface)
                                FUpdateInterface(FActStep, FActLayer);

                            //MessageBox.Show( string.Format("File '{0}' loaded.", loadSaveFileName));
                        }
                    }
                    else
                        MessageBox.Show(
                            string.Format("Failed '{0}'", loadSaveFileName));
                    #endregion
                    break;

                case 2: // Process_TVectSimpleShapes
                    #region Process_TVectSimpleShapes
                    #endregion
                    break;

                case 3: //Save_File
                    #region Save_File
                    //MessageBox.Show( string.Format("File '{0}' saved.", loadSaveFileName));
                    if (ClsRvCamDLL.CS_RvCam_Get_LayerData(FActStep, FActLayer,
                            ref ClsRvCamDLL_PlugInDLL_FileIO.FVectShapeData))
                    {
                        if (ClsRvCamDLL_PlugInDLL_FileIO.LoadLibraryMethod_And_Run(dllId, methodID, ref loadSaveFileName,
                            ref ClsRvCamDLL_PlugInDLL_FileIO.FVectShapeData))
                        {
                            MessageBox.Show(
                                string.Format("File saved.\n\t'{0}'", loadSaveFileName));
                        }
                        else
                            MessageBox.Show(
                                string.Format("Failed. '{0}'", loadSaveFileName));
                    }
                    #endregion
                    break;

                default:
                    break;
            }
#endif
        }

        private static void Add_PlugIn_Method_Items(ref ToolStripMenuItem hostMenuItem)
        {
            if (hostMenuItem == null) return;

            string hostDLLName = hostMenuItem.Name;

#if AutoLoadPlugins  //自動讀取 ./PlugIns/ 下的所有 DLL.
            int methodNum = ClsRvCamDLL_PlugInDLL_FileIO.FPlugInMethodNames.Length;

            ToolStripMenuItem[] items = new ToolStripMenuItem[methodNum]; // You would obviously calculate this value at runtime
            for (int i = 0; i < methodNum; i++)
            {
                string
                    sMethodName = ClsRvCamDLL_PlugInDLL_FileIO.FPlugInMethodNames[i],
                    sItemName = hostDLLName + sMethodName;

                items[i] = new ToolStripMenuItem();
                items[i].Name = hostDLLName + i.ToString();
                items[i].Tag = i;
                items[i].Text = sMethodName;

                //hostMenuItem.DropDownItems.Add(items[iy]);

                items[i].Click += new EventHandler(PlugInMethodMenuItemClickHandler);
            }
            hostMenuItem.DropDownItems.AddRange(items);
#endif
        }
        public static void Add_PlugInItems(ref ToolStripMenuItem hostMenuItem)
        {
#if AutoLoadPlugins  //自動讀取 ./PlugIns/ 下的所有 DLL.

            if (hostMenuItem == null) return;
            if (ClsRvCamDLL_PlugInDLL_FileIO.FPlugInDLLFileNames.Length <= 0) return;

            int dllNum = ClsRvCamDLL_PlugInDLL_FileIO.FPlugInDLLFileNames.Length;

            ToolStripMenuItem[] items = new ToolStripMenuItem[dllNum]; // You would obviously calculate this value at runtime
            for (int i = 0; i < items.Length; i++)
            {
                string
                    sDllName =
                        Path.GetFileName(ClsRvCamDLL_PlugInDLL_FileIO.FPlugInDLLFileNames[i]),
                    sDllDescript = ClsRvCamDLL_PlugInDLL_FileIO.FPlugInDLLDescriptions[i];

                items[i] = new ToolStripMenuItem();
                items[i].Name = sDllName + i.ToString();
                items[i].Tag = i;
                items[i].Text = sDllDescript;
                //items[iy].Click += new EventHandler(PlugInMenuItemClickHandler);

                //hostMenuItem.DropDownItems.Add(items[iy]);

                Add_PlugIn_Method_Items(ref items[i]);
            }

            hostMenuItem.DropDownItems.AddRange(items);
#endif
        }

        public static bool Calibrate_BuildTable(int canvasID, int atStep, int cadLyr, int imgLyr,
             TEditShape[][] roiShps, ref TEditShape[][] cadShps, ref TEditShape[][] imgShps)
        {
            bool ret = false, blAllQueried=true;

            if (atStep < 0 || atStep >= FStepNames.Length) return false;
            if (cadLyr < 0 || cadLyr >= FLayerNames.Length) return false;
            if (imgLyr < 0 || imgLyr >= FLayerNames.Length) return false;

            if (roiShps.Length < 2 && roiShps[0].Length < 2) return false; //至少四點


            ClsM2dTypeDefineFunctions.Clear_TEditShapes2D(ref cadShps);
            ClsM2dTypeDefineFunctions.Clear_TEditShapes2D(ref imgShps);

            for (int iy = 0; iy < roiShps.Length; iy++)
            {
                ArrayExtension.Add(ref cadShps, []);
                ArrayExtension.Add(ref imgShps, []);

                for (int ix = 0; ix < roiShps[iy].Length; ix++)
                {
                    TFPoint cXYmm = roiShps[iy][ix].cirCXY;
                    TFloat getBlobCXpxl = 0.0, getBlobCYpxl = 0.0;

                    TFloat getObjCXmm = 0.0, getObjCYmm = 0.0, getObjWmm = 0.0, getObjHmm = 0.0;
                    string symbName = "", sSymbTp = "", getObjStepName = "", sGetObjInfo = "";

                    //先 Query CAD Layer 的 Grids----------------------------------------------------
                    if (ClsRvCamDLL.Query_LayerObject(canvasID, atStep, cadLyr, cXYmm.X, cXYmm.Y, cQuerySearchTolMm,
                            ref getObjCXmm, ref getObjCYmm,
                            ref getBlobCXpxl, ref getBlobCYpxl,
                            ref getObjWmm, ref getObjHmm,
                            ref symbName, ref sSymbTp, ref getObjStepName, ref sGetObjInfo))
                    {
                        TEditShape circle = new TEditShape(new TFPoint(getObjCXmm, getObjCYmm), getObjWmm / 2.0,
                            "", //string.Format("({0},{1})", getObjCXmm.ToString("#0.###"), getObjCYmm.ToString("#0.###")),
                            atStep, cadLyr); // ActLayer);
                        ArrayExtension.Add(ref cadShps[iy], circle);
                    }
                    else
                    {
                        blAllQueried = false;
                    }

                    //再 Query Image Layer 的 Grids----------------------------------------------------
                    if (ClsRvCamDLL.Query_LayerObject(canvasID, atStep, imgLyr, cXYmm.X, cXYmm.Y, cQuerySearchTolMm,
                            ref getObjCXmm, ref getObjCYmm,
                            ref getBlobCXpxl, ref getBlobCYpxl,
                            ref getObjWmm, ref getObjHmm,
                            ref symbName, ref sSymbTp, ref getObjStepName, ref sGetObjInfo))
                    {
                        TEditShape circle = new TEditShape(new TFPoint(getObjCXmm, getObjCYmm), getObjWmm / 2.0,
                            "", //string.Format("({0},{1})", getObjCXmm.ToString("#0.###"), getObjCYmm.ToString("#0.###")),
                            atStep, imgLyr); // ActLayer);
                        ArrayExtension.Add(ref imgShps[iy], circle);

                    }
                    else
                    {
                        blAllQueried = false;
                    }
                }
            }

            if (!blAllQueried) return false;

            if (roiShps.Length != cadShps.Length || roiShps.Length != imgShps.Length) return false;

            TFPoint[] cadLyrPts = new TFPoint[cadShps.Length*cadShps[0].Length],
                      imgLyrPts = new TFPoint[imgShps.Length*imgShps[0].Length],
                      imgLyrPxls = new TFPoint[imgShps.Length*imgShps[0].Length];
            int incPt = 0, nY = imgShps.Length, nX = imgShps[0].Length;
            for (int iy=0; iy<imgShps.Length; iy++)
            {
                for (int ix = 0; ix < imgShps[iy].Length; ix++)
                {
                    cadLyrPts[incPt] = cadShps[iy][ix].cirCXY;
                    imgLyrPts[incPt] = imgShps[iy][ix].cirCXY;
                    imgLyrPxls[incPt] = new TFPoint(0.0, 0.0);

                    incPt++;
                }
            }
            

            unsafe
            {
                fixed(TFPoint* pCadXYs0 = &(cadLyrPts[0]))
                {
                    fixed (TFPoint* pPts0 = &(imgLyrPts[0]))
                    {
                        fixed (TFPoint* pPxls0 = &(imgLyrPxls[0]))
                        {
                            //將imgLyrPts[] -> imgLyrPxls[]----------------------------------
                            if (CS_RvCam_Calibrate_CamToImageXY(
                                atStep, imgLyr,
                                (IntPtr)pPts0, (IntPtr)pPxls0, imgLyrPts.Length))
                            {
                                // 輸入 cadLyrPts[]  imgLyrPts[] 校正
                                ret = CS_RvCam_Calibrate_BulidTable(
                                    (IntPtr)pPxls0, (IntPtr)pCadXYs0, 
                                    nX,nY);
                            }
                        }
                    }
                }
            }

            return ret;
               
        }

        /// <summary>
        /// 將圖形位址 IntPtr 轉換成 Bitmap
        /// </summary>
        /// <param name="pImgStart0"></param>
        /// <param name="imgWidth"></param>
        /// <param name="imgHeight"></param>
        /// <param name="imgStrideBytes"></param>
        /// <param name="imgBitPerPxl"></param>
        /// <param name="bmp"></param>
        /// <returns></returns>
        private static bool Convert_IntPtrToBitmap(IntPtr pImgStart0,
            int imgWidth, int imgHeight, int imgStrideBytes, int imgBitPerPxl,
            ref Bitmap bmp)
        {

            if (IntPtr.Zero == pImgStart0) return false;

            if (bmp != null) bmp.Dispose();


            PixelFormat pf = PixelFormat.Format32bppArgb; // .Format8bppIndexed;

            switch (imgBitPerPxl)
            {
                case 1:
                    pf = PixelFormat.Format1bppIndexed;
                    break;

                case 8:
                    pf = PixelFormat.Format8bppIndexed;
                    break;
                case 24:
                    pf = PixelFormat.Format24bppRgb;
                    break;
                case 32:
                    pf = PixelFormat.Format32bppArgb;
                    break;
                default:
                    pf = PixelFormat.Format8bppIndexed;
                    break;
            }

#if _UseCSharpAPI
            bmp = new Bitmap(imgWidth, imgHeight, imgStrideBytes, pf, pImgStart0);
            return (bmp.Width > 0);
#else
            // pixelformat Align
            int toImgStride_bytesPerRow =
                    (imgStrideBytes % imgBitPerPxl != 0) ?
                    (imgStrideBytes / imgBitPerPxl + 1) * imgBitPerPxl :
                    imgStrideBytes;

            bmp = new Bitmap(
                       toImgStride_bytesPerRow / imgBitPerPxl * 8, imgHeight,
                       pf //PixelFormat.Format8bppIndexed // .Format32bppArgb
                       );

            {
                BitmapData bmpData = bmp.LockBits(
                    new Rectangle(0, 0,
                            bmp.Width,
                            bmp.Height),
                            ImageLockMode.WriteOnly,
                            bmp.PixelFormat);
                IntPtr pBmpScan0 = bmpData.Scan0;

                int imgBytes = imgStrideBytes * imgHeight;

                byte[] rgbValues = new byte[toImgStride_bytesPerRow];

                // 將 pImageStart0 指向的資料拷貝到 rgbValues[]-------------

                IntPtr pFr = pImgStart0;
                for (int i = 0; i < imgHeight; i++)
                {
                    Marshal.Copy(pFr,
                       rgbValues, 0, imgStrideBytes
                        );

                    // 將 rgbValues[] 拷貝到 bmp.scan0-----------------------
                    Marshal.Copy(rgbValues, 0, pBmpScan0,
                        imgStrideBytes);

                    pFr += imgStrideBytes;
                    pBmpScan0 += toImgStride_bytesPerRow;
                }

                bmp.UnlockBits(bmpData);
            }
#endif

            return true;
        }
        /// <summary>
        /// 將 Color 轉換成 integer
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private static int Convert_ColorToIntAARRGGBB(Color color)
        {
            int intClr = color.ToArgb();
            //  color.R<<16 + color.G << 8 + color.B + color.A << 24;

            return intClr;
        }
        private static Color Convert_IntAARRGGBBToColor(int intClr)
        {
            Color color = Color.White;

            UInt32 iABGR = (UInt32)intClr;

            UInt32
                alpha = 255,
                red = (UInt32)(iABGR & 0xFF),
                green = (UInt32)(iABGR & 0x00FF00) >> 8,
                blue = (UInt32)(iABGR & 0xFF0000) >> 16;

            color = Color.FromArgb((int)alpha, (int)red, (int)green, (int)blue);

            //color.R = intClr >> 16 & 0xFF;
            //color.G = intClr >> 8 & 0xFF;
            //color.B = intClr &0xFF;

            return color;
        }
        /// <summary>
        /// 將 cam 的 minmax 轉換成 image minmax
        /// </summary>
        /// <param name="canvasID"></param>
        /// <param name="camMinXmm"></param>
        /// <param name="camMinYmm"></param>
        /// <param name="camMaxXmm"></param>
        /// <param name="camMaxYmm"></param>
        /// <param name="imgMinX"></param>
        /// <param name="imgMinY"></param>
        /// <param name="imgMaxX"></param>
        /// <param name="imgMaxY"></param>
        public static void Convert_Cam_To_ImageMinMax(int canvasID,
            double camMinXmm, double camMinYmm, double camMaxXmm, double camMaxYmm,
            ref int imgMinX, ref int imgMinY, ref int imgMaxX, ref int imgMaxY)
        {
            RvCam_Get_CamToImageXY(canvasID, Math.Min(camMinXmm, camMaxXmm), Math.Min(camMinYmm, camMaxYmm),
                ref imgMinX, ref imgMaxY);
            RvCam_Get_CamToImageXY(canvasID, Math.Max(camMinXmm, camMaxXmm), Math.Max(camMinYmm, camMaxYmm),
                ref imgMaxX, ref imgMinY);
        }
        /// <summary>
        /// 將繪圖區的 Pixel MinMax -> mm MinMax 區域
        /// </summary>
        /// <param name="canvasID"></param>
        /// <param name="imgMinX"></param>
        /// <param name="imgMinY"></param>
        /// <param name="imgMaxX"></param>
        /// <param name="imgMaxY"></param>
        /// <param name="camMinXmm"></param>
        /// <param name="camMinYmm"></param>
        /// <param name="camMaxXmm"></param>
        /// <param name="camMaxYmm"></param>
        public static void Convert_Image_To_CamMinMax(int canvasID,
            int imgMinX, int imgMinY, int imgMaxX, int imgMaxY,
            ref double camMinXmm, ref double camMinYmm, 
            ref double camMaxXmm, ref double camMaxYmm)
        {
            RvCam_Get_ImageToCamXY(canvasID, Math.Min(imgMinX, imgMaxX), Math.Max(imgMinY, imgMaxY),
                ref camMinXmm, ref camMinYmm);
            RvCam_Get_ImageToCamXY(canvasID, Math.Max(imgMinX, imgMaxX), Math.Min(imgMinY, imgMaxY),
                ref camMaxXmm, ref camMaxYmm);
        }
        /// <summary>
        /// 將繪圖區的範圍(pixel)轉換成 Mm 中心和長寬 (mm)
        /// </summary>
        /// <param name="canvasID"></param>
        /// <param name="imgMinX"></param>
        /// <param name="imgMinY"></param>
        /// <param name="imgMaxX"></param>
        /// <param name="imgMaxY"></param>
        /// <param name="camCXmm"></param>
        /// <param name="camCYmm"></param>
        /// <param name="camWidthmm"></param>
        /// <param name="camHeightmm"></param>
        public static void Convert_ImageMinMax_To_CamCxyWidthHeight(int canvasID,
            int imgMinX, int imgMinY, int imgMaxX, int imgMaxY,
            ref double camCXmm, ref double camCYmm, 
            ref double camWidthmm, ref double camHeightmm)
        {
            double minXmm = 0.0, minYmm = 0.0, maxXmm = 0.0, maxYmm = 0.0;

            ClsRvCamDLL.Convert_Image_To_CamMinMax(
                    canvasID,
                    imgMinX, imgMinY, imgMaxX, imgMaxY,
                    ref minXmm, ref minYmm, ref maxXmm, ref maxYmm);

            camCXmm = (minXmm + maxXmm) / 2;
            camCYmm = (minYmm + maxYmm) / 2;
            camWidthmm = Math.Abs(maxXmm - minXmm);
            camHeightmm = Math.Abs(maxYmm - minYmm);
        }
        /// <summary>
        /// string[] -> string
        /// </summary>
        /// <param name="strAry"></param>
        /// <param name="sDivdeChar"></param>
        /// <param name="getStr"></param>
        /// <returns></returns>
        public static bool Convert_StringArray_To_String(
            string[] strAry, string sDivdeChar, ref string getStr)
        {
            getStr = "";
            if (strAry.Length <= 0) return false;

            for (int i = 0; i < strAry.Length; i++)
            {
                if (i == 0) getStr = getStr + strAry[i];
                else getStr = getStr + sDivdeChar + strAry[i];
            }

            return (getStr != "");
        }
        /// <summary>
        /// string -> string[]
        /// </summary>
        /// <param name="str"></param>
        /// <param name="sDivideChar"></param>
        /// <param name="getStrAry"></param>
        /// <returns></returns>
        public static bool Convert_String_To_StringArray(
            string str, string sDivideChar,
            ref string[] getStrAry)
        {
            getStrAry = str.Split(sDivideChar);

            return (getStrAry.Length > 0);
        }
        /// <summary>
        /// string[] -> string
        /// </summary>
        /// <param name="strAry"></param>
        /// <param name="sDivdeChar"></param>
        /// <returns></returns>
        public static string Convert_StringArray_To_String(
            string[] strAry, string sDivdeChar)
        {
            string sRet = "";
            Convert_StringArray_To_String(strAry, sDivdeChar, ref sRet);
            return sRet;
        }
        /// <summary>
        /// string -> string[]
        /// </summary>
        /// <param name="str"></param>
        /// <param name="sDivideChar"></param>
        /// <returns></returns>
        public static string[] Convert_String_To_StringArray(
            string str, string sDivideChar)
        {
            string[] sRets = [];
            Convert_String_To_StringArray(str, sDivideChar, ref sRets);
            return sRets;
        }


        /// <summary>
        /// 取得 step 顏色
        /// </summary>
        /// <param name="stepID"></param>
        /// <returns></returns>
        public static Color Get_StepColor(int stepID)
        {
            if (stepID < 0) return Color.White;

            return FStepColors[stepID % FStepColors.Length];
        }
        /// <summary>
        /// 取得 Layer 顏色
        /// </summary>
        /// <param name="layerID"></param>
        /// <returns></returns>
        public static Color Get_LayerColor(int layerID)
        {
            if (layerID < 0) return Color.White;

            return FLayerColors[layerID % FLayerColors.Length];
        }
        public static int Get_LayerIndex(string lyrName)
        {
            return Array.FindIndex(FLayerNames, m => m == lyrName);
        }
        public static int Get_StringIndex(string[] strAry, string str)
        {
            return Array.FindIndex(strAry, m => m == str);
        }
        public static bool Get_ImageToCamMinMax(
            int canvasID, int imgMinX, int imgMinY, 
            int imgMaxX, int imgMaxY,
            ref TFloat camMinX, ref TFloat camMinY, 
            ref TFloat camMaxX, ref TFloat camMaxY
            )
        {
            RvCam_Get_ImageToCamXY(
                canvasID,
                Math.Min(imgMinX, imgMaxX), Math.Max(imgMinY, imgMaxY),
                ref camMinX, ref camMinY);

            TReturnCode ret = RvCam_Get_ImageToCamXY(
                canvasID,
                Math.Max(imgMinX, imgMaxX), Math.Min(imgMinY, imgMaxY),
                ref camMaxX, ref camMaxY);


            return (ret == TReturnCode.rcSuccess);
        }
        /// <summary>
        /// 取得目前 View的中心座標 CXY mm
        /// </summary>
        /// <param name="canvasID"></param>
        /// <param name="viewCXmm"></param>
        /// <param name="viewCYmm"></param>
        /// <param name="byStoredView"></param>
        /// <returns></returns>
        public static bool Get_ViewCXY(int canvasID, 
            ref double viewCXmm, ref double viewCYmm,
            bool byStoredView = false)
        {
            double viewWmm = 0.0, viewHmm = 0.0, viewMmPerPxl = 0.0, viewDeg = 0.0;
            bool viewMrX = false;

            return (CS_RvCam_Get_ViewInfo(canvasID, ref viewCXmm, ref viewCYmm,
                ref viewWmm, ref viewHmm, ref viewMmPerPxl, ref viewDeg, ref viewMrX,
                byStoredView));

        }
        public static bool Get_ViewResolution(int canvasID, 
            ref double viewMmPerPixel)
        {
            double viewWmm = 0.0, viewHmm = 0.0, viewDeg = 0.0, viewCXmm = 0.0, viewCYmm = 0.0;
            bool viewMrX = false;

            return (CS_RvCam_Get_ViewInfo(canvasID, ref viewCXmm, ref viewCYmm,
                ref viewWmm, ref viewHmm, ref viewMmPerPixel, ref viewDeg, ref viewMrX,
                false));

        }
        /// <summary>
        /// 取得目前View檢視範圍 單位 mm
        /// </summary>
        /// <param name="canvasID"></param>
        /// <param name="viewMinX"></param>
        /// <param name="viewMinY"></param>
        /// <param name="viewMaxX"></param>
        /// <param name="viewMaxY"></param>
        /// <param name="byStoredView"></param>
        /// <returns></returns>
        public static bool Get_ViewMinMax(int canvasID,
            ref double viewMinX, ref double viewMinY, 
            ref double viewMaxX, ref double viewMaxY,
            bool byStoredView = false)
        {
            bool ret = false;

            double viewCXmm = 0.0, viewCYmm = 0.0, viewWmm = 0.0, viewHmm = 0.0, viewMmPerPxl = 0.0, viewDeg = 0.0;
            bool viewMrX = false;

            if (CS_RvCam_Get_ViewInfo(canvasID, ref viewCXmm, ref viewCYmm,
                ref viewWmm, ref viewHmm, ref viewMmPerPxl, ref viewDeg, ref viewMrX,
                byStoredView))
            {
                double vW2 = viewWmm / 2.0, vH2 = viewHmm / 2.0;
                viewMinX = viewCXmm - vW2;
                viewMinY = viewCYmm - vH2;
                viewMaxX = viewCXmm + vW2;
                viewMaxY = viewCYmm + vH2;
                ret = true;
            }

            return ret;
        }
        /// <summary>
        /// 選取檔案類型
        /// </summary>
        /// <returns></returns>
        public static TVectFileType Get_TVectFileType()
        {
            TVectFileType ret = TVectFileType.vtUnknown;

            ret = TVectFileType.vtGerber274X;

            string[] sFileTps = [];
            foreach (string item in Enum.GetNames(typeof(TVectFileType)))
            {
                ArrayExtension.Add(ref sFileTps, item);
            }
            string sTitle = "Select File Type";
            bool[] blSelected = [];
            int actID = (int)ClsRvCamDLL.ActionTarget;
            if (ClsRvCamDLL.CS_RvCam_Dialog_ItemSelect(sTitle, sFileTps, ref blSelected, ref actID, false))
            {
                ret = (TVectFileType)actID;

                //ClsRvCamDLL.EditMode = TEditMode.emSelect;
            }

            return ret;
        }
        /// <summary>
        /// 選取ODB Steps 清單模式
        /// </summary>
        /// <returns></returns>
        public static TOdbStepListType Get_TVOdbStepListType()
        {
            TOdbStepListType ret = TOdbStepListType.osAllSteps;

            string[] stpLstTp = [];
            foreach (string item in Enum.GetNames(typeof(TOdbStepListType)))
            {
                ArrayExtension.Add(ref stpLstTp, item);
            }
            string sTitle = "Select Odb Steps Archive Type";
            bool[] blSelected = [];
            int actID = (int)ret;
            if (ClsRvCamDLL.CS_RvCam_Dialog_ItemSelect(sTitle, stpLstTp, ref blSelected, ref actID, false))
            {
                ret = (TOdbStepListType)actID;

                //ClsRvCamDLL.EditMode = TEditMode.emSelect;
            }

            return ret;
        }
        /// <summary>
        /// 字串輸入對話框
        /// </summary>
        /// <param name="sInput"></param>
        /// <param name="sTitle"></param>
        /// <param name="sPrompt"></param>
        /// <returns></returns>
        public static bool Get_InputString(ref string sInput, 
            string sTitle, string sPrompt)
        {
            sInput = Interaction.InputBox(
                sPrompt,
                sTitle,
                sInput,
                -1, -1);

            return ("" != sInput);
        }
        /// <summary>
        /// 取得高解析算圖參數
        /// </summary>
        /// <param name="rrenderResolution_MmPerPixel"></param>
        /// <param name="viewMinMax"></param>
        /// <param name="pAssignRenderMinMaxMm"></param>
        /// <param name="fnSaveBmp"></param>
        /// <returns></returns>
        public static bool Get_RenderImage_Params(ref TFloat rrenderResolution_MmPerPixel,
            TFRect viewMinMax, ref IntPtr pAssignRenderMinMaxMm,
            ref string fnSaveBmp)
        {

            #region// 輸入解析度-----------------------------------------
            string sInput = "";
            sInput = rrenderResolution_MmPerPixel.ToString("#0.###");
            if (ClsRvCamDLL.Get_InputString(ref sInput, "Input Resolution (mm/pxl)", "Resolution (mm/pxl)"))
                rrenderResolution_MmPerPixel = Convert.ToDouble(sInput);
            else
                return false;
            #endregion


            #region// 如果知道輸出範圍，則在此設定，否則略過-------------------------------------
            string[] promptViewMinMax = ["ViewMinX(mm)", "ViewMinY(mm)", "ViewMaxX(mm)", "ViewMaxY(mm)"],
                     sViewMinMax = [viewMinMax.Left.ToString("#0.###"), viewMinMax.Bottom.ToString("#0.###"),
                                       viewMinMax.Right.ToString("#0.###"), viewMinMax.Top.ToString("#0.###")];
            if (ClsRvCamDLL.CS_RvCam_Dialog_MultiInputBox("Input View MinMax (mm)", promptViewMinMax, ref sViewMinMax))
            {
                viewMinMax.Left = Convert.ToDouble(sViewMinMax[0]);
                viewMinMax.Bottom = Convert.ToDouble(sViewMinMax[1]);
                viewMinMax.Right = Convert.ToDouble(sViewMinMax[2]);
                viewMinMax.Top = Convert.ToDouble(sViewMinMax[3]);
                unsafe
                {
                    pAssignRenderMinMaxMm = (IntPtr)(&viewMinMax);
                }
            }
            else
            {
                pAssignRenderMinMaxMm = IntPtr.Zero;
            }
            #endregion

            #region //選取輸出檔案 -------------------------------
            if (!ClsRvCamDLL.Select_File_Save(ref fnSaveBmp, "Bmp File(*.bmp)|*.bmp"))
            {
                fnSaveBmp = "";
            }
            else
            {
                Path.ChangeExtension(fnSaveBmp, ".bmp");
            }

            #endregion

            return true;
        }
        /// <summary>
        /// 取得 Steps / Layers 清單，並選取
        /// </summary>
        /// <param name="setTgzOdbDirFn"></param>
        /// <param name="allSteps"></param>
        /// <param name="allLayers"></param>
        /// <param name="selectedSteps"></param>
        /// <param name="selectedLayers"></param>
        /// <param name="odbTgzTp"></param>
        /// <returns></returns>
        public static bool Get_StepLayersAll_And_Select(string setTgzOdbDirFn,
            ref string[] allSteps, ref string[] allLayers,
            ref string[] selectedSteps, ref string[] selectedLayers,
            ref TOdbTgzType odbTgzTp)
        {
            allSteps = [];
            allLayers = [];
            selectedSteps = [];
            selectedLayers = [];

            if (File.Exists(setTgzOdbDirFn)) odbTgzTp = TOdbTgzType.otTgzFile;
            else if (Directory.Exists(setTgzOdbDirFn)) odbTgzTp = TOdbTgzType.otOdbFolder;
            else return false;

            #region 取得ODB料號的所有Steps/Layers，並指定只要讀入的 Steps /Layers 名稱-------------------
            //取得ODB的所有 Steps/Layers 名稱--------------------------------------
            if (ClsRvCamDLL.CS_RvCam_Get_StepsLayers_ODB(setTgzOdbDirFn, ref allSteps, ref allLayers, TOdbStepListType.osAllSteps))
                ;

            //輸入要讀入的 Steps/Layers，不指定，則 設為 "" ----------------------------------
            string[] loadOnlyStepLyrs = [Convert_StringArray_To_String(allSteps, ","), Convert_StringArray_To_String(allLayers, ",")];
            if (ClsRvCamDLL.CS_RvCam_Dialog_MultiInputBox("Enter Selected Steps / Layers Names", ["Input Select Steps", "Input Select Layers"],
                ref loadOnlyStepLyrs)) ;
            selectedSteps = ClsRvCamDLL.Convert_String_To_StringArray(loadOnlyStepLyrs[0], ",");
            selectedLayers = ClsRvCamDLL.Convert_String_To_StringArray(loadOnlyStepLyrs[1], ",");
            #endregion

            return true; // ( selectedSteps.Length > 0 && selectedLayers.Length > 0);
        }


        /// <summary>
        /// 將 Bitmap 畫到 Graphic
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="g"></param>
        /// <param name="pbxWidth"></param>
        /// <param name="pbxHeight"></param>
        /// <param name="bkClr"></param>
        /// <param name="strechDraw"></param>
        public static void Paint_BitmapToGraphic(Bitmap bmp, ref Graphics g,
            int pbxWidth, int pbxHeight, Color bkClr, bool strechDraw = false)
        {
            if (bmp == null || bmp.Width <= 0) return;
            if (g == null) return;


            // 將 bmp 畫到 pixtureBox1-------------------------
            //Graphics g = pbx.CreateGraphics();

            // Create solid brush.
            SolidBrush blueBrush = new SolidBrush(bkClr); //. Color.GreenYellow);
            // Create rectangle.
            Rectangle rect = new Rectangle(0, 0,
                pbxWidth - 1, pbxHeight - 1);
            // Fill rectangle to screen.
            g.FillRectangle(blueBrush, rect);

            //bmp.SetResolution(300f, 300f); //可以縮放在pictureBox1上的大小
            int sidePxl = 5;
            if (strechDraw)
            {
                float scale = (float)bmp.Height / bmp.Width;
                int pbxDrawW = pbxWidth - sidePxl * 2;
                g.DrawImage(bmp,
                    new Rectangle(sidePxl, sidePxl,
                        pbxDrawW,
                        //pictureBox1.Height - 1 - sidePxl * 2
                        (int)Math.Round(pbxDrawW * scale)
                        ),
                    new Rectangle(0, 0,
                            bmp.Width,
                            bmp.Height),
                    GraphicsUnit.Pixel);
            }
            else
                g.DrawImage(bmp, new Point(sidePxl, sidePxl));

            //g.Dispose();
        }
        /// <summary>
        /// 將 Bitmap 畫到 PictureBox
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="pbx"></param>
        /// <param name="strechDraw"></param>
        public static void Paint_BitmapToPictureBox(Bitmap bmp, 
            ref PictureBox pbx, bool strechDraw = false)
        {
            if (bmp == null || bmp.Width <= 0) return;
            if (pbx == null) return;

            if (pbx.Image != null) pbx.Image.Dispose();
            pbx.Image = bmp.Clone(
            new Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.PixelFormat.DontCare);

            //bmp.Save("d:\\temp\\aa.bmp");

            // 將 bmp 畫到 pixtureBox1-------------------------
            //Graphics g =  pbx.CreateGraphics();
            //Paint_BitmapToGraphic(bmp, ref g, pbx.Width, pbx.Height, pbx.BackColor, strechDraw);
            //g.Dispose();
        }
        /// <summary>
        /// 將圖形(IntPtr) 透明繪圖到 Bitmap
        /// </summary>
        /// <param name="pImgStart0"></param>
        /// <param name="imgWidth"></param>
        /// <param name="imgHeight"></param>
        /// <param name="imgStrideBytes"></param>
        /// <param name="imgBitPerPxl"></param>
        /// <param name="imgBackgroudClr"></param>
        /// <param name="bmp"></param>
        /// <param name="imgAlpha"></param>
        /// <returns></returns>
        private static bool Paint_IntPtrToBitmap_AlphaBlend(IntPtr pImgStart0,
            int imgWidth, int imgHeight, int imgStrideBytes, int imgBitPerPxl,
            Color imgBackgroudClr, ref Bitmap bmp, int imgAlpha = 125,
            bool doFlipY = false)
        {
            bool ret = false;

            if (IntPtr.Zero == pImgStart0) return false;
            if (bmp == null) return ret;

            if (imgWidth != bmp.Width || imgHeight != bmp.Height) return false;


            int imgBytePerPxl = imgBitPerPxl / 8,
                bmpBytePerPxl = 4;

            switch (bmp.PixelFormat)
            {
                case PixelFormat.Format32bppArgb:
                    bmpBytePerPxl = 4;
                    break;
                default:
                    return false;
            }

            // 確保是 32 bit 圖形------------------------------------------
            if (imgBytePerPxl != bmpBytePerPxl ||
                bmpBytePerPxl != 4) return false;

            // pixelformat Align
            int bmpStrides =
                    (imgStrideBytes % imgBitPerPxl != 0) ?
                    (imgStrideBytes / imgBitPerPxl + 1) * imgBitPerPxl :
                    imgStrideBytes;

            {

                BitmapData bmpData = bmp.LockBits(
                    new Rectangle(0, 0,
                            bmp.Width,
                            bmp.Height),
                            ImageLockMode.WriteOnly,
                            bmp.PixelFormat);

                bmpStrides = bmpData.Stride;
                IntPtr pBmpScan0 = bmpData.Scan0;

                // 將 pImageStart0 指向的資料拷貝到 rgbValues[]-------------
                //int imgBytes = imgStrideBytes * imgHeight;
                byte[] frRgbValues = new byte[bmpStrides],
                        toRgbValues = new byte[bmpStrides];

                int bmpAlpha = 255 - imgAlpha;

                IntPtr pFr = pImgStart0,
                       pTo = pBmpScan0,
                       pCurFr = pImgStart0,
                       pCurTo = pBmpScan0;

                if (true == doFlipY)
                {
                    // 解決 Cam 圖形 上下顛倒的問題
                    pTo = pBmpScan0 + (imgHeight - 1) * imgStrideBytes;

                    for (int iy = 0; iy < imgHeight - 1; iy++)
                    {
                        // 拷貝一整列資料---------------------------------
                        Marshal.Copy(pFr, frRgbValues, 0, imgStrideBytes);
                        Marshal.Copy(pTo, toRgbValues, 0, imgStrideBytes);

                        for (int ix = 0; ix < toRgbValues.Length; ix += 4) // 一次跳 4 byte
                        {
                            int idAA = ix + 3, idRR = ix + 2, idGG = ix + 1, idBB = ix;

                            //RgbValues[idAA] = (byte)bmpAlpha;

                            if (frRgbValues[idRR] == imgBackgroudClr.R &&
                                frRgbValues[idGG] == imgBackgroudClr.G &&
                                frRgbValues[idBB] == imgBackgroudClr.B
                                )
                            // 是被景色，不處理---------------------------------------
                            {
                                // toRgbValues[idAA] = (byte)bmpAlpha;
                                //idAA = idAA;
                            }
                            else
                            {
                                //AA 
                                //toRgbValues[idAA] = (byte)imgAlpha; // frRgbValues[ix+0];//RR

                                toRgbValues[idRR] = (byte)((frRgbValues[idRR] * imgAlpha + toRgbValues[idRR] * bmpAlpha) / 255);

                                //GG
                                toRgbValues[idGG] = (byte)((frRgbValues[idGG] * imgAlpha + toRgbValues[idGG] * bmpAlpha) / 255);

                                //BB
                                toRgbValues[idBB] = (byte)((frRgbValues[idBB] * imgAlpha + toRgbValues[idBB] * bmpAlpha) / 255);

                            }

                        }

                        // 將 rgbValues[] 拷貝到 bmp.scan0-----------------------
                        Marshal.Copy(toRgbValues, 0, pTo, imgStrideBytes);

                        pFr += imgStrideBytes;
                        pTo -= bmpStrides;
                    }
                }
                else
                {
                    pTo = pBmpScan0;
                    for (int iy = 0; iy < imgHeight - 1; iy++)
                    {
                        // 拷貝一整列資料---------------------------------
                        Marshal.Copy(pFr, frRgbValues, 0, imgStrideBytes);
                        Marshal.Copy(pTo, toRgbValues, 0, imgStrideBytes);

                        for (int ix = 0; ix < toRgbValues.Length; ix += 4) // 一次跳 4 byte
                        {
                            int idAA = ix + 3, idRR = ix + 2, idGG = ix + 1, idBB = ix;

                            //RgbValues[idAA] = (byte)bmpAlpha;

                            if (frRgbValues[idRR] == imgBackgroudClr.R &&
                                frRgbValues[idGG] == imgBackgroudClr.G &&
                                frRgbValues[idBB] == imgBackgroudClr.B
                                )
                            // 是被景色，不處理---------------------------------------
                            {
                                // toRgbValues[idAA] = (byte)bmpAlpha;
                                //idAA = idAA;
                            }
                            else
                            {
                                //AA 
                                //toRgbValues[idAA] = (byte)imgAlpha; // frRgbValues[ix+0];//RR

                                toRgbValues[idRR] = (byte)((frRgbValues[idRR] * imgAlpha + toRgbValues[idRR] * bmpAlpha) / 255);

                                //GG
                                toRgbValues[idGG] = (byte)((frRgbValues[idGG] * imgAlpha + toRgbValues[idGG] * bmpAlpha) / 255);

                                //BB
                                toRgbValues[idBB] = (byte)((frRgbValues[idBB] * imgAlpha + toRgbValues[idBB] * bmpAlpha) / 255);

                            }

                        }

                        // 將 rgbValues[] 拷貝到 bmp.scan0-----------------------
                        Marshal.Copy(toRgbValues, 0, pTo, imgStrideBytes);

                        pFr += imgStrideBytes;
                        pTo += bmpStrides;
                    }
                }


                bmp.UnlockBits(bmpData);
            }

#if DEBUG
            //bmp.Save("d:\\temp\\a.bmp");
#endif

            return ret;
        }
        /// <summary>
        /// 將 CAM 圖形畫到 Bitmap
        /// </summary>
        /// <param name="aStep"></param>
        /// <param name="aLyrs"></param>
        /// <param name="paintClrs"></param>
        /// <param name="canvasID"></param>
        /// <param name="cnvBmp"></param>
        /// <param name="cnvWidth"></param>
        /// <param name="cnvHeight"></param>
        /// <param name="cnvPF"></param>
        /// <param name="paintMode"></param>
        /// <returns></returns>
        public static bool Paint_CamToBitmap(int aStep, int[] aLyrs,
            Color[] paintClrs, int canvasID,
            ref Bitmap cnvBmp, int cnvWidth, int cnvHeight,
            PixelFormat cnvPF = PixelFormat.Format32bppArgb,
            TVectPaintMode paintMode = TVectPaintMode.pmSolid_Normal,
            bool aRulerVisible = false,
            TCompensateMode compenMode = TCompensateMode.cmNonCompensate)
        {
            bool ret = false;

            if (aLyrs.Length <= 0) return false;

            // force cnvPF = 32 bit----------------------------------
            cnvPF = PixelFormat.Format32bppArgb;

            int imgStrideBytes = cnvWidth * 4;
            int cnvBitsPerPixel = 32;

            switch (cnvPF)
            {
                case PixelFormat.Format32bppArgb:
                    cnvBitsPerPixel = 4 * 8; ;
                    imgStrideBytes = 4 * cnvWidth;
                    break;
                case PixelFormat.Format8bppIndexed:
                    cnvBitsPerPixel = 8; ;
                    imgStrideBytes = cnvWidth;
                    break;
                case PixelFormat.Format24bppRgb:
                    cnvBitsPerPixel = 3 * 8; ;
                    imgStrideBytes = 3 * cnvWidth;
                    break;
                default:
                    return ret;
            }

            Update_Bitmap(ref cnvBmp, cnvWidth, cnvHeight,
                cnvPF, FBackgroundColor);

            Graphics g = Graphics.FromImage(cnvBmp);
            g.Clear(FBackgroundColor);

            IntPtr cnvScan0 = default;

            int imgBytes = imgStrideBytes * cnvHeight;

#if PaintOnlyOneLayer
            // 將 CamData 畫到 cnvBmp.Scan0------------------------------------
            if (TReturnCode.rcSuccess == ClsRvCamDLL.CS_RvCam_Paint_Canvas(
                    aStep, aLyrs[0], paintClrs[0],
                    canvasID, 
                    ref cnvScan0, imgStrideBytes, 
                    cnvWidth, cnvHeight, cnvBitsPerPixel,
                    true,
                    paintMode,
                    compenMode))
            {
                //將 cnvScan0 的資料 拷貝到 cnvBmp-------------------------------------------
                Convert_IntPtrToBitmap(cnvScan0, cnvWidth, cnvHeight, imgStrideBytes, cnvBitsPerPixel, ref cnvBmp);     

                ret = true;                
            }
#else
            byte imgAlpha = 125;
            int actLyr = aLyrs[aLyrs.Length-1];

            for (int i = 0; i < aLyrs.Length; i++)
            {


                if (ClsRvCamDLL.CS_RvCam_Paint_Canvas(
                   aStep, aLyrs[i], paintClrs[i],
                   canvasID,
                   ref cnvScan0, imgStrideBytes,
                   cnvWidth, cnvHeight, cnvBitsPerPixel,
                   true,
                   paintMode,
                   (aLyrs[i]==actLyr)?compenMode: TCompensateMode.cmNonCompensate //actlayer 才作補償顯示
                   ))
                {
                    if (i == aLyrs.Length - 1)
                    {
                        imgAlpha = 255;
                    }
                    else imgAlpha = 100;

                    //將 cnvScan0 的資料 Alpha Blend 到 cnvBmp-------------------------------------
                    Paint_IntPtrToBitmap_AlphaBlend(cnvScan0, cnvWidth, cnvHeight, imgStrideBytes, cnvBitsPerPixel,
                         FBackgroundColor, ref cnvBmp, imgAlpha,
#if DoYFlip_Bitmap
                         true
 #else
                         false
#endif
                            );
                    //cnvBmp.Save("d:\\temp\\cnv.bmp");
                    ret = true;
                }
            }

            if (aRulerVisible == true)
            {        
                // 劃出尺規-------------------------------------------
                ClsRvCamDLL.CS_RvCam_Paint_Canvas_Ruler(
                       canvasID, FRulerColor, FDisplayUnit,
                       ref cnvScan0, imgStrideBytes,
                       cnvWidth, cnvHeight, cnvBitsPerPixel,
                       true,
                       FRulerPixelWidth
                    );
                //將 cnvScan0 的資料 Alpha Blend 到 cnvBmp-------------------------------------
                Paint_IntPtrToBitmap_AlphaBlend(cnvScan0, cnvWidth, cnvHeight, imgStrideBytes, cnvBitsPerPixel,
                     FBackgroundColor, ref cnvBmp, FRulerAlpha,
    #if DoYFlip_Bitmap
                             true
    #else
                             false
    #endif
                     );

            }
        
#endif


#if DEBUG
            //cnvBmp.Save("d:\\temp\\cnv.bmp");
#endif

            return ret;
        }
        /// <summary>
        /// 將 CAM 圖形畫到 PictureBox
        /// </summary>
        /// <param name="canvasID"></param>
        /// <param name="pbx"></param>
        /// <param name="paintStep"></param>
        /// <param name="paintLayers"></param>
        /// <param name="paintLayerColors"></param>
        /// <param name="paintMode"></param>
        /// <param name="viewMode"></param>
        /// <param name="aRulerVisible"></param>
        public static void Paint_Cam_PictureBox(int canvasID,
            PictureBox pbx, int paintStep, int[] paintLayers,
            Color[] paintLayerColors,
            TVectPaintMode paintMode = TVectPaintMode.pmSolid_Normal,
            TViewMode viewMode = TViewMode.vmCanvasSizeChanged,
            bool aRulerVisible = false,
            TCompensateMode compenMode = TCompensateMode.cmNonCompensate)
        {
            ClsRvCamDLL.Update_View_And_PaintToPictureBox(
                    canvasID, pbx,
                    viewMode,
                    paintStep,
                    paintLayers, //[FActLayer],
                    paintLayerColors, //[ClsRvCamDLL.Get_LayerColor(FActLayer)],
                    paintMode,
                    false,
                    aRulerVisible,
                    compenMode
                    );
        }


        /// <summary>
        /// 詢問Layer物件
        /// </summary>
        /// <param name="canvasID"></param>
        /// <param name="qryStep"></param>
        /// <param name="qryLayer"></param>
        /// <param name="qryXmm"></param>
        /// <param name="qryYmm"></param>
        /// <param name="qryTolmm"></param>
        /// <param name="getObjCXmm"></param>
        /// <param name="getObjCYmm"></param>
        /// <param name="getObjWmm"></param>
        /// <param name="getObjHmm"></param>
        /// <param name="symbName"></param>
        /// <param name="sGetObjInfo"></param>
        /// <returns></returns>
        public static bool Query_LayerObject(int canvasID, 
            int qryStep, int qryLayer, 
            TFloat qryXmm, TFloat qryYmm, TFloat qryTolmm,
            ref TFloat getObjCXmm, ref TFloat getObjCYmm,
            ref TFloat getBlobCXpixel, ref TFloat getBlobCYpixel,
            ref TFloat getObjWmm, ref TFloat getObjHmm,
            ref string symbName, ref string sSymbTp,
            ref string getObjStepName,
            ref string sGetObjInfo)
        {
            bool ret = false;

            symbName = "";
            sGetObjInfo = "";
            getObjCXmm = getObjCYmm = getBlobCXpixel = getBlobCYpixel = getObjWmm = getObjHmm = 0.0;

            if (ClsRvCamDLL.CS_RvCam_Query_ObjectInfo(qryStep, qryLayer, qryXmm, qryYmm, qryTolmm,
                    ref getObjCXmm, ref getObjCYmm, ref getObjWmm, ref getObjHmm,
                    ref symbName, ref sSymbTp, ref getObjStepName, ref sGetObjInfo))
            {
                getBlobCXpixel = -1; 
                getBlobCYpixel = -1;

                ret = true;
            }
            else if ( ClsRvCamDLL.CS_RvCam_Query_BlobCXY_Layer(qryStep, qryLayer, qryXmm, qryYmm, 
                        ref getObjCXmm, ref getObjCYmm, ref getBlobCXpixel, ref getBlobCYpixel, ref getObjWmm, ref getObjHmm,
                        ref symbName, ref sSymbTp, ref getObjStepName, ref sGetObjInfo
                    ))
            {
                ret = true;
            }

            return ret;
        }


        /// <summary>
        /// 設定Step顏色
        /// </summary>
        /// <param name="stepID"></param>
        /// <param name="stepColor"></param>
        public static void Set_StepColor(int stepID, Color stepColor)
        {
            if (stepID < 0 || stepID >= FStepColors.Length) return;

            FStepColors[stepID % FStepColors.Length] = stepColor;
        }
        /// <summary>
        /// 設定Layer顏色
        /// </summary>
        /// <param name="layerID"></param>
        /// <param name="layerColor"></param>
        public static void Set_LayerColor(int layerID, Color layerColor)
        {
            if (layerID < 0 || layerID >= FLayerColors.Length) return;

            FLayerColors[layerID % FLayerColors.Length] = layerColor;
        }


        /// <summary>
        /// CAD檔案讀檔視窗
        /// </summary>
        /// <param name="fnCADs"></param>
        /// <param name="multiSelectFiles"></param>
        /// <returns></returns>
        public static bool Select_CADFile_Open(ref string[] fnCADs, 
            bool multiSelectFiles = false)
        {
            string fnCAD = "";
            if (fnCADs.Length > 0) fnCAD = fnCADs[0];
            else fnCAD = ClsM2dTypeDefineVabiable.mLastFile;

            bool ret = false;
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Multiselect = multiSelectFiles;
                openFileDialog.InitialDirectory =
                   // Directory.GetCurrentDirectory(); // "c:\\";
                   Path.GetDirectoryName(fnCAD);
                openFileDialog.FileName = fnCAD;

                openFileDialog.Filter = cSupportedCadFileExtensions;
                //"All files (*.*)|*.*" +
                //"|CAD files (*.gbx,*.gbr,*.dxf,*.nc,*.dpf,*.gds,*.gih,*.txt)|*.gbx;*.gbr;*.dxf;*.nc;*.dpf;*.gds;*.gih;*.txt" +
                //"|IPC/356 (*.ipc,*.356)|*.ipc;*.356" +
                //"|CAR (*.car)|*.car" +
                //"|RasVector Cam (*.rvc)|*.rvc"
                ;
                openFileDialog.FilterIndex = 0;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    fnCAD = openFileDialog.FileName;
                    fnCADs = openFileDialog.FileNames;
                    if (fnCADs.Length <= 0) fnCADs = [fnCAD];
                    ret = (fnCADs.Length > 0);
                }
            }

            return ret;
        }
        /// <summary>
        /// CAD檔案存檔視窗
        /// </summary>
        /// <param name="fnCAD"></param>
        /// <returns></returns>
        public static bool Select_CADFile_Save(ref string fnCAD)
        {
            bool ret = false;

            string fn = ("" == fnCAD) ? ClsM2dTypeDefineVabiable.mLastFile : fnCAD;


            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = Path.GetDirectoryName(fn);
                saveFileDialog.FileName = fn;

                saveFileDialog.Filter = cSupportedCadFileExtensions;
                ;
                saveFileDialog.FilterIndex = 0;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    fnCAD = saveFileDialog.FileName;
                    ret = true;
                }
            }

            return ret;
        }

        public static bool Select_File_Open(ref string aFn, string sFilter = "*.*")
        {
            //string aFn = ClsM2dTypeDefineVabiable.mLastFile;

            bool ret = false;
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Multiselect = false;
                openFileDialog.InitialDirectory =
                   // Directory.GetCurrentDirectory(); // "c:\\";
                   Path.GetDirectoryName(aFn);
                openFileDialog.FileName = aFn;

                openFileDialog.Filter = "All files (*.*)|*.*";// cSupportedCadFileExtensions;
                //"All files (*.*)|*.*" +
                //"|CAD files (*.gbx,*.gbr,*.dxf,*.nc,*.dpf,*.gds,*.gih,*.txt)|*.gbx;*.gbr;*.dxf;*.nc;*.dpf;*.gds;*.gih;*.txt" +
                //"|IPC/356 (*.ipc,*.356)|*.ipc;*.356" +
                //"|CAR (*.car)|*.car" +
                //"|RasVector Cam (*.rvc)|*.rvc"
                ;
                openFileDialog.FilterIndex = 0;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    aFn = openFileDialog.FileName;
                    ret = File.Exists(aFn);
                }
            }

            return ret;
        }
        /// <summary>
        /// 存檔視窗
        /// </summary>
        /// <param name="aFn"></param>
        /// <param name="sFilter"></param>
        /// <returns></returns>
        public static bool Select_File_Save(ref string aFn, string sFilter = "*.*")
        {
            bool ret = false;

            string fn = ("" == aFn) ? ClsM2dTypeDefineVabiable.mLastFile : aFn;


            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = Path.GetDirectoryName(fn);
                saveFileDialog.FileName = fn;

                saveFileDialog.Filter = sFilter;
                ;
                saveFileDialog.FilterIndex = 0;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    aFn = saveFileDialog.FileName;
                    ret = true;
                }
            }

            return ret;
        }
        /// <summary>
        /// TGZ檔案開檔視窗
        /// </summary>
        /// <param name="fnTGZ"></param>
        /// <returns></returns>
        public static bool Select_TGZFile_Open(ref string fnTGZ)
        {
            bool ret = false;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory =
                   // Directory.GetCurrentDirectory(); // "c:\\";
                   Path.GetDirectoryName(fnTGZ);
                openFileDialog.FileName = fnTGZ;

                openFileDialog.Filter = "tgz files (*.tgz)|*.tgz|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    fnTGZ = openFileDialog.FileName;
                    ret = File.Exists(fnTGZ);
                }
            }

            return ret;
        }
        /// <summary>
        /// 目錄資料夾選取視窗
        /// </summary>
        /// <param name="sDir"></param>
        /// <returns></returns>
        public static bool Select_Directory(ref string sDir)
        {
            bool ret = false;

            using (var fbd = new FolderBrowserDialog())
            {
                fbd.InitialDirectory = sDir;
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    sDir = fbd.SelectedPath;

                    ret = Directory.Exists(sDir);
                }
            }

            return ret;
        }


        /// <summary>
        /// 更新 Bitmap
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="bmpWidth"></param>
        /// <param name="bmpHeight"></param>
        /// <param name="bmpPF"></param>
        /// <param name="bkClr"></param>
        public static void Update_Bitmap(ref Bitmap bmp, int bmpWidth, 
            int bmpHeight, PixelFormat bmpPF,
            Color bkClr)
        {
            if (bmpWidth <= 0 || bmpHeight <= 0) return;

            if (bmp != null) bmp.Dispose();

            bmp = new Bitmap(
                       bmpWidth, bmpHeight,
                       bmpPF //PixelFormat.Format8bppIndexed // .Format32bppArgb
                       );

            Graphics g = Graphics.FromImage(bmp);
            g.Clear(bkClr);
        }
        /// <summary>
        /// 更新畫布(PictureBox)的View，並繪圖到Bitmap
        /// </summary>
        /// <param name="canvasID"></param>
        /// <param name="canvasWidth"></param>
        /// <param name="canvasHeight"></param>
        /// <param name="atViewMode"></param>
        /// <param name="viewStep"></param>
        /// <param name="viewLayers"></param>
        /// <param name="paintLayerColors"></param>
        /// <param name="paintBmp"></param>
        /// <param name="pf"></param>
        /// <returns></returns>
        private static bool Update_View_And_PaintToBitmap(
            int canvasID, int canvasWidth, int canvasHeight,
            TViewMode atViewMode,
            int viewStep, int[] viewLayers, Color[] paintLayerColors,
            ref Bitmap paintBmp, PixelFormat pf
            )
        {
            if (viewLayers.Length <= 0 || viewLayers.Length != paintLayerColors.Length) { return false; }

            ClsRvCamDLL.CS_RvCam_View_Update(canvasID,
                  canvasWidth, canvasHeight,
                  viewStep, viewLayers[0], atViewMode);

            return ClsRvCamDLL.Paint_CamToBitmap(
                    viewStep, viewLayers,
                    paintLayerColors, canvasID,
                    ref paintBmp, canvasWidth, canvasHeight,
                    pf);


        }
        /// <summary>
        /// 更新畫布(PictureBox)的View，並繪圖到 PictureBox
        /// </summary>
        /// <param name="canvasID">畫布ID</param>
        /// <param name="pbx">畫布PictureBox</param>
        /// <param name="paintViewMode">View模式</param>
        /// <param name="viewStep">Step ID</param>
        /// <param name="viewLayers">[Layer IDs]</param>
        /// <param name="paintLayerColors">[Layer Colors]</param>
        /// <param name="paintMode">繪圖模式</param>
        /// <param name="stretchDraw">是否StretchDraw</param>
        /// <returns></returns>
        public static bool Update_View_And_PaintToPictureBox(
            int canvasID, PictureBox pbx,
            TViewMode paintViewMode,
            int viewStep, int[] viewLayers, Color[] paintLayerColors,
            TVectPaintMode paintMode = TVectPaintMode.pmSolid_Normal,
            bool stretchDraw = false,
            bool aRulerVisible = false,
            TCompensateMode compenMode = TCompensateMode.cmNonCompensate
            )
        {

            bool ret = false;



            if (viewLayers.Length <= 0 || viewLayers.Length != paintLayerColors.Length || FLayerNames.Length <= 0)
            {
                //Graphics g = pbx.CreateGraphics();
                //SolidBrush bBrush = new SolidBrush(FBackgroundColor); //. Color.GreenYellow);
                //Rectangle rect = new Rectangle(0, 0,
                //    pbx.Width - 1, pbx.Height - 1);
                //// Fill rectangle to screen.
                //g.FillRectangle(bBrush, rect);
                //pbx.Update();
                //g.Dispose();

                Update_Bitmap(ref PaintDummyBmp, pbx.Width, pbx.Height,
                    PixelFormat.Format32bppArgb, FBackgroundColor); 
                Paint_BitmapToPictureBox(PaintDummyBmp, ref pbx, stretchDraw);
                return false;
            }

            ClsRvCamDLL.CS_RvCam_View_Update(canvasID,
                  pbx.Width,pbx.Height,
                  viewStep, viewLayers[0], paintViewMode);

            
            if (true ==  ClsRvCamDLL.Paint_CamToBitmap(
                    viewStep, viewLayers,
                    paintLayerColors, canvasID,
                    ref PaintDummyBmp, pbx.Width, pbx.Height,
                    PixelFormat.Format32bppArgb,
                    paintMode,
                    aRulerVisible,
                    compenMode
                    ) )
            {

               Paint_BitmapToPictureBox(PaintDummyBmp, ref pbx, stretchDraw);

               ret = true;
            }
            else
            {
                Update_Bitmap(ref PaintDummyBmp, pbx.Width, pbx.Height,
                    PixelFormat.Format32bppArgb,
                     FBackgroundColor);

                Paint_BitmapToPictureBox(PaintDummyBmp, ref pbx, stretchDraw);
            }

            return ret;
        }
        /// <summary>
        /// 按下滑鼠右鍵，在PictureBox上MouseMove時更新View，
        /// 需在 MouseDown() 內先作 CS_RvCam_View_Store()
        /// </summary>
        /// <param name="viewStep">Step ID</param>
        /// <param name="viewLayer">Layer ID</param>
        /// <param name="canvasID">畫布ID</param>
        /// <param name="canvasDownX">MouseDown()時的e.X</param>
        /// <param name="canvasDownY">MouseDown()時的e.Y</param>
        /// <param name="canvasMoveX">MouseMove()時的e.X</param>
        /// <param name="canvasMoveY">MouseMove()時的e.Y</param>
        public static void Update_View_Pan(
            int viewStep, int viewLayer,
            int canvasID, 
            int canvasDownX, int canvasDownY, int canvasMoveX, int canvasMoveY)
        {
            double oCamX = 0.0, oCamY = 0.0;
            ClsRvCamDLL.CS_RvCam_Get_ImageToCamXY(canvasID, canvasMoveX, canvasMoveY, ref oCamX, ref oCamY, true);

            double oCamDnX = 0.0, oCamDnY = 0.0;
            ClsRvCamDLL.CS_RvCam_Get_ImageToCamXY(canvasID, canvasDownX, canvasDownY, ref oCamDnX, ref oCamDnY, true);
            double dx = oCamX - oCamDnX, dy = oCamY - oCamDnY;

            double oCamCX = 0.0, oCamCY = 0.0;
            Get_ViewCXY(canvasID, ref oCamCX, ref oCamCY, true);
            double vX = oCamCX - dx, vY = oCamCY - dy;
            ClsRvCamDLL.CS_RvCam_View_Update(canvasID, vX, vY, viewStep, viewLayer, TViewMode.vmViewAtXY);
        }
        /// <summary>
        /// 以檢視範圍 MinMax (mm) 更新 View
        /// </summary>
        /// <param name="viewStep">Step ID</param>
        /// <param name="viewLayer">Layer ID</param>
        /// <param name="canvasID">畫布ID</param>
        /// <param name="updateCanvasID">要更新View的畫布 ID</param>
        /// <param name="canvasDownX">MouseDown()時的e.X</param>
        /// <param name="canvasDownY">MouseDown()時的e.Y</param>
        /// <param name="canvasUpX">MouseUp()時的eX</param>
        /// <param name="canvasUpY">MouseUp()時的e.Y</param>
        /// <returns></returns>
        public static bool Update_View_ViewMinMax(
            int viewStep, int viewLayer,
            int canvasID, int updateCanvasID,
            int canvasDownX, int canvasDownY, int canvasUpX, int canvasUpY)
        {
            bool ret = false;

            double viewMinXmm = 0, viewMinYmm = 0, viewMaxXmm = 0, viewMaxYmm = 0;

            if (ClsRvCamDLL.Get_ImageToCamMinMax(
                canvasID, canvasDownX, canvasDownY, canvasUpX, canvasUpY,
                ref viewMinXmm, ref viewMinYmm, ref viewMaxXmm, ref viewMaxYmm))
            {
                if (ClsRvCamDLL.CS_RvCam_View_Update_ViewMinMax(
                    updateCanvasID, viewMinXmm, viewMinYmm, viewMaxXmm, viewMaxYmm,
                    viewStep, viewLayer))
                {
                    ret = true;
                }
            }

            return ret;
        }
        /// <summary>
        /// 更新string[] Steps / Layers Array
        /// </summary>
        /// <param name="sStepNames"></param>
        /// <param name="sLayerNames"></param>
        /// <param name="stepNamesAry"></param>
        /// <param name="layerNamesAry"></param>
        public static void Update_StepLayerNames(string sStepNames, string sLayerNames, 
            ref string[] stepNamesAry, ref string[] layerNamesAry)
        {
            if ("" == sStepNames)
                stepNamesAry = [];
            else
                stepNamesAry = sStepNames.Split(',');
            if ("" == sLayerNames)
            {
                layerNamesAry = [];
            }
            else
            {
                layerNamesAry = sLayerNames.Split(",");

            }
        }

#endregion

    }

#pragma warning restore CS8601 
#pragma warning restore CS8618 
#pragma warning restore CS8604
#pragma warning restore CS8602
#pragma warning restore CS0642
#pragma warning restore CS0219
#pragma warning restore CS8600
#pragma warning restore CS8622


}