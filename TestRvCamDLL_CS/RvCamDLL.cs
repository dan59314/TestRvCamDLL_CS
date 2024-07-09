
#define _CopyStride
#define YFlip_Bitmap  // 解決 Cam 圖形 上下顛倒的問題
#define AutoLoadPlugins  //自動讀取 ./PlugIns/ 下的所有 DLL.


//#define PaintOnlyOneLayer


using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using M2dTypeDefine;
using VectTypeDefine;
using RvCamDLL_PlugIn_FileIO;
using Microsoft.VisualBasic;


#if AutoLoadPlugins  //自動讀取 ./PlugIns/ 下的所有 DLL.
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
#pragma warning disable CS8604
#pragma warning disable CS8602
#pragma warning disable CS0642
#pragma warning disable CS0219
#pragma warning disable CS8600
#pragma warning disable CS8622

    #region Type Defione ====================================================
    public enum TReturnCode : int
    {
        rcFail = 0, rcSuccess, rcUnAuthorized,
        rcFinal
    }

    public enum TEditMode : int
    {
        emNone=0, emSelect, emQuery
    }

    public static class sReturnCode
    {
        public static string StrValue(TReturnCode aReturnCode)
        {
            string ss = "";
            switch (aReturnCode)
            {
                case TReturnCode.rcFail: ss = "Fail"; break;
                case TReturnCode.rcSuccess: ss = "Successs"; break;
                case TReturnCode.rcUnAuthorized: ss = "UnAuthorized"; break;
                default: ss = ""; break;
            }
            return ss;
        }

        public static int IntValue(TReturnCode aReturnCode)
        {

            return (int)aReturnCode;
        }
    }
    #endregion



    public static class ClsRvCamDLL
    {
        private const string AssemblyName = "RvCamDLL.dll";
        //  exe 程式所在的資料夾下必須包含  RvCamDLL.dll, borlndmm.dll 和 [EXEs] 資料夾

        private const CallingConvention TargetCallingConvention = CallingConvention.StdCall;
        private const CharSet TargetCharSet = CharSet.Ansi;
        private const int cMaxAllocateStringSize = 8192;  //因應某些料號 Steps 過多，由 2048 -> 8192

        #region Type Define =======================================================
        public delegate void TUpdateInterface(int aStep, int aLayer);
        #endregion

        #region Members Declation =====================================================
        public static TUpdateInterface? FUpdateInterface = default;
        public static string[] FLayerNames = [], FStepNames=[];
        public static int FActStep = 0;
        public static int FActLayer = 0;
        private static Bitmap PaintDummyBmp;
        private static Color[] FStepColors = [
                Color.Blue, Color.Green, Color.Red, Color.DarkBlue, Color.LightGreen, Color.OrangeRed,
                Color.LightBlue, Color.LightGreen, Color.IndianRed, Color.SkyBlue, Color.GreenYellow, Color.IndianRed];
        private static Color[] FLayerColors = [
                Color.LightBlue, Color.LightGreen, Color.IndianRed, Color.SkyBlue, Color.GreenYellow, Color.IndianRed,
                Color.Blue, Color.Green, Color.Red, Color.DarkBlue, Color.LightGreen, Color.OrangeRed];
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

        private static TEditMode FEditAction = TEditMode.emNone;
        public static TEditMode EditMode
        {
            get { return FEditAction; }
            set { FEditAction = value; }
        }
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
            "       slMain.Text = Marshal.PtrToStringAnsi(sMainTask);",
            "       slSub.Text = Marshal.PtrToStringAnsi(sSubTask);",
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
#if DEBUG
            MessageBox.Show("主程式所在的資料夾內必須包含  RvCamDLL.dll, borlndmm.dll 和 [EXEs] 資料夾");

            MessageBox.Show(sCallback);
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
                { str_sDLLInfo = Marshal.PtrToStringAnsi(sDLLInfo); }
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
            string sAddToNewLayer_SetName =""
            )
        {
            TReturnCode ret = TReturnCode.rcFail;

            IntPtr pAddToNewLayer_SetName = Marshal.StringToBSTR(sAddToNewLayer_SetName );

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
            string str_loadLayers,
            string str_setSaveCadFileDirectory,
            TVectFileType saveCadFileType,
            ref string str_getSavedFileNames
            )
        {
            // 非 ref string，做轉換------------
            IntPtr pstr_loadOdbDir_TGZFile = Marshal.StringToBSTR(str_loadOdbDir_TGZFile);
            IntPtr pstr_loadStep = Marshal.StringToBSTR(str_loadStep);
            IntPtr pstr_loadLayers = Marshal.StringToBSTR(str_loadLayers);
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
                str_getSavedFileNames = Marshal.PtrToStringUni(pRef_getSavedFileNames);
            }

            //Marshal.FreeHGlobal(sXXX);

            return (ret==TReturnCode.rcSuccess);
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
            string str_loadCadFiles,
            string str_setSaveCadFileDirectory,
            TVectFileType setSaveCadFileType,
            ref string str_getSavedFileNames
            )
        {
            // 非 ref string，做轉換------------
            IntPtr pstr_loadCadFiles = Marshal.StringToBSTR(str_loadCadFiles);
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
                str_getSavedFileNames= Marshal.PtrToStringUni(pRefstr_getSavedFileNames);
            }

            //Marshal.FreeHGlobal(sXXX);


            return (ret==TReturnCode.rcSuccess);
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

            return ( ret == TReturnCode.rcSuccess);
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

            return (TReturnCode.rcSuccess == ret);
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
            Convert_StringArray_To_String(str_sPrompts, ",", ref sPrompts);

            // 非 ref string，做轉換------------
            IntPtr pstr_sTitle = Marshal.StringToBSTR(str_sTitle);
            IntPtr pstr_sPrompts = Marshal.StringToBSTR(sPrompts);

            string sValues = "";
            // ref string, DLL 內配置記憶體，在此不需配置------------
            IntPtr pRefgetValues = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);
            if (str_getValues.Length>0)
            {
                Convert_StringArray_To_String(str_getValues, ",", ref sValues);
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

                for (int i = 0; i<sSelected.Length; i++)
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


        // Get Functions =================================================
        #region Get Functions  ===================================================
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Get_StepsLayers_ODB", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Get_StepsLayers_ODB(
            IntPtr sOdbDir_TGZFile,
            ref IntPtr getSteps,
            ref IntPtr getLayers,
            TOdbStepListType stepsListTp = TOdbStepListType.osAllSteps,
            IntPtr atTopStepName = default(IntPtr)
        );
        /// <summary>取得 ODB++/TGZ 的 Steps, Layers 名稱</summary>
        public static bool CS_RvCam_Get_StepsLayers_ODB(
            string sOdbDir_TGZFile,
            ref string getSteps,
            ref string getLayers,
            TOdbStepListType stepsListTp = TOdbStepListType.osAllSteps,
            string atTopStepName = ""
            )
        {

            getSteps = "";
            getLayers = "";

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
                { getSteps = Marshal.PtrToStringAuto(pStps); }

                if (IntPtr.Zero != pLyrs)
                { getLayers = Marshal.PtrToStringAuto(pLyrs); }
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
            ref string getSteps,
            ref string getLayers,
            TOdbTgzType loadOdbTgzTp = TOdbTgzType.otOdbFolder,
            TOdbStepListType stepsListTp = TOdbStepListType.osAllSteps
            )
        {
            getODB_TGZFile = "";
            getSteps = "";
            getLayers = "";

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
                { getSteps = Marshal.PtrToStringAuto(pStps); }

                if (IntPtr.Zero != pLyrs)
                { getLayers = Marshal.PtrToStringAuto(pLyrs); }
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

            if (ret==TReturnCode.rcSuccess)
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
                ClsRvCamDLL_PlugInDLL_FileIO.Copy_TVectSimpleShapes(pGetShps0, shpsLength,
                   ref ClsRvCamDLL_PlugInDLL_FileIO.FVectShapeData);
                ret = TReturnCode.rcSuccess;
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
            ref IntPtr getLoadToLayerName,
            ref TVectFileType getFileType,
            IntPtr setLoadToStepNameOrNull = default(IntPtr),
            bool blClearCurrentData = true
        );
        /// <summary>
        /// 無顯示介面,背景 讀取 CAD 檔案(*.GBX, *.DXF, *.NC, *.DWG...)，可多選
        /// </summary>
        /// <param name="str_sCadFileNames">讀入多個檔案並以 ',' 分開， eg. 'c:\a.gbx,d:\b.dxf'</param>
        /// <param name="str_getLoadToStepName">傳回讀入到哪個Step</param>
        /// <param name="str_getLoadToLayerName">傳回讀入到哪個Layer</param>
        /// <param name="getFileType">傳回讀入的檔案類型</param>
        /// <param name="str_setLoadToStepNameOrNull">可設定要讀入的目的 Step，或者Null</param>
        /// <param name="blClearCurrentData">讀入前是否先清除目前CAM資料</param>
        /// <returns></returns>
        public static bool CS_RvCam_Load_CAD(
            string str_sCadFileNames, // 讀入多個檔案並以 ',' 分開， eg. 'c:\a.gbx,d:\b.dxf'
            ref string str_getLoadToStepName,
            ref string str_getLoadToLayerName,
            ref TVectFileType getFileType,
            string str_setLoadToStepNameOrNull = "",
            bool blClearCurrentData = true
            )
        {
            // 非 ref string，做轉換------------
            IntPtr pstr_sCadFileNames = Marshal.StringToBSTR(str_sCadFileNames);
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
                str_getLoadToLayerName = Marshal.PtrToStringUni(pRefstr_getLoadToLayerName);
            }

            //Marshal.FreeHGlobal(sXXX);

            return (ret == TReturnCode.rcSuccess);
        }
        /// <summary>
        /// 以開檔視窗開啟CAD檔案，可多選
        /// </summary>
        /// <param name="getCadFileNames">傳回開啟的檔案名稱</param>
        /// <param name="getLoadToStepName">傳回讀入到哪個Step</param>
        /// <param name="getLoadToLayerName">傳回讀入到哪個Layer</param>
        /// <param name="getFileType">傳回讀入的檔案類型</param>
        /// <param name="setLoadToStepNameOrNull">可設定要讀入的目的 Step，或者Null</param>
        /// <param name="blClearCurrentData">讀入前是否先清除目前CAM資料</param>
        /// <returns></returns>
        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Load_CAD_Dialog", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Load_CAD_Dialog(
            ref IntPtr getCadFileNames, // 讀入多個檔案並以 ',' 分開， eg. 'c:\a.gbx,d:\b.dxf'
            ref IntPtr getLoadToStepName,
            ref IntPtr getLoadToLayerName,
            ref TVectFileType getFileType,
            IntPtr setLoadToStepNameOrNull = default(IntPtr),
            bool blClearCurrentData = true
        );
        /// <summary></summary>
        public static bool CS_RvCam_Load_CAD_Dialog(
            ref string str_getCadFileNames, // 讀入多個檔案並以 ',' 分開， eg. 'c:\a.gbx,d:\b.dxf'
            ref string str_getLoadToStepName,
            ref string str_getLoadToLayerName,
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
            IntPtr pRefstr_getLoadToLayerName = IntPtr.Zero;

            TReturnCode ret = RvCam_Load_CAD_Dialog(
                ref pRefstr_getCadFileNames, // 讀入多個檔案並以 ',' 分開， eg. 'c:\a.gbx,d:\b.dxf'
                ref pRefstr_getLoadToStepName,
                ref pRefstr_getLoadToLayerName,
                ref getFileType,
                pstr_setLoadToStepName,
                blClearCurrentData
            );

            if (ret == TReturnCode.rcSuccess)
            {
                str_getCadFileNames = Marshal.PtrToStringUni(pRefstr_getCadFileNames);
                str_getLoadToStepName = Marshal.PtrToStringUni(pRefstr_getLoadToStepName);
                str_getLoadToLayerName = Marshal.PtrToStringUni(pRefstr_getLoadToLayerName);
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
            ref string str_getSteps,
            ref string str_getLayers,
            bool showImportOdbDialog = false,
            TOdbStepListType stepsListTp = TOdbStepListType.osAllSteps,
            string loadOnlySteps = "", //指定讀入的 Steps, 和 Layers Name
            string loadOnlyLayers = "" // Eg.  'pcb,array,panel', 'comp, l2, l3'
            )
        {
            // 非 ref string，做轉換------------
            IntPtr pstr_sOdbDir_TGZFile = Marshal.StringToBSTR(str_sOdbDir_TGZFile);
            IntPtr ploadOnlySteps = Marshal.StringToBSTR(loadOnlySteps);
            IntPtr ploadOnlyLayers = Marshal.StringToBSTR(loadOnlyLayers);

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
                str_getSteps = Marshal.PtrToStringUni(pRefstr_getSteps);
                str_getLayers = Marshal.PtrToStringUni(pRefstr_getLayers);
            }

            //Marshal.FreeHGlobal(sXXX);

            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Load_ODB_Dialog", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Load_ODB_Dialog(
            ref IntPtr getOdbDir_TGZFile,
            ref IntPtr getSteps,
            ref IntPtr getLayers,
            TOdbTgzType loadOdbTgzTp = TOdbTgzType.otOdbFolder,
            TOdbStepListType stepsListTp = TOdbStepListType.osAllSteps,
            IntPtr loadOnlySteps = default, //指定讀入的 Steps, 和 Layers Name
            IntPtr loadOnlyLayers = default // Eg.  'pcb,array,panel', 'comp, l2, l3'
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
            TOdbTgzType loadOdbTgzTp = TOdbTgzType.otOdbFolder,
            TOdbStepListType stepsListTp = TOdbStepListType.osAllSteps,
            string loadOnlySteps = "", //指定讀入的 Steps, 和 Layers Name
            string loadOnlyLayers = "" // Eg.  'pcb,array,panel', 'comp, l2, l3'
            )
        {
            // 非 ref string，做轉換------------
            IntPtr ploadOnlySteps = Marshal.StringToBSTR(loadOnlySteps);
            IntPtr ploadOnlyLayers = Marshal.StringToBSTR(loadOnlyLayers);

            // ref string, DLL 內配置記憶體，在此不需配置-----------
            IntPtr pRefstr_getOdbDir_TGZFile = IntPtr.Zero;
            IntPtr pRefstr_getSteps = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);
            IntPtr pRefstr_getLayers = IntPtr.Zero;



            TReturnCode ret = RvCam_Load_ODB_Dialog(
                ref pRefstr_getOdbDir_TGZFile,
                ref pRefstr_getSteps,
                ref pRefstr_getLayers,
                loadOdbTgzTp,
                stepsListTp,
                ploadOnlySteps,
                ploadOnlyLayers
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
            TVectPaintMode paintMode = TVectPaintMode.pmSolid_Normal
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
            TVectPaintMode paintMode = TVectPaintMode.pmSolid_Normal
            )
        {
            TReturnCode ret = RvCam_Paint_Canvas(
                paintStep, paintLayer,
                Convert_ColorToIntAARRGGBB(paintColor),
                canvasID,
                ref CnvScan0,
                CnvRowBytes, CnvWidth, CnvHeight, CnvBitsPerPixel,
                cnvDIBUpWard,
                paintMode
            );

            return (ret == TReturnCode.rcSuccess);
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
            int qryStep, int qryLayer,
            ref bool getIsEmpty
            )
        {
            TReturnCode ret = RvCam_Query_IsEmpty_StepLayer(
                qryStep, qryLayer,
                ref getIsEmpty
            );

            return (ret == TReturnCode.rcSuccess);
        }

        [DllImport(AssemblyName, CharSet = TargetCharSet, EntryPoint = "RvCam_Query_ObjectInfo", CallingConvention = TargetCallingConvention)]
        private static extern TReturnCode RvCam_Query_ObjectInfo(
        int qryStep, int qryLayer,
        TFloat qryXmm, TFloat qryYmm, TFloat qryTolmm,
        ref IntPtr getObjectInfo
    );
        public static bool CS_RvCam_Query_ObjectInfo(
            int qryStep, int qryLayer,
            TFloat qryXmm, TFloat qryYmm, TFloat qryTolmm,
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
                ref pRefstr_getObjectInfo
            );

            if (ret == TReturnCode.rcSuccess)
            {

                str_getObjectInfo = Marshal.PtrToStringUni(pRefstr_getObjectInfo);
            }

            //Marshal.FreeHGlobal(sXXX);


            return (ret == TReturnCode.rcSuccess);
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

            return (ret==TReturnCode.rcSuccess);
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
            IntPtr pstr_sSaveCadFileName = IntPtr.Zero; //Marshal.AllocHGlobal(cMaxAllocateStringSize);

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


            return ( ret == TReturnCode.rcSuccess);
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
                fixed (TVectSimpleShape* pShps0 = &(shpsData[0]) )
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
            if (ClsRvCamDLL_PlugInDLL_FileIO.LoadLibraryMethod_And_Run(dllId, methodID, ref loadSaveFileName))
            {
                switch (methodID)
                {
                    case 0: //Is_RvCamDLL
                        break;

                    case 1: // Load_File
                        #region Load_File
                        if (ClsRvCamDLL.CS_RvCam_Add_LayerData(FActStep, ref FActLayer,
                                ClsRvCamDLL_PlugInDLL_FileIO.FVectShapeData,
                                ClsRvCamDLL_PlugInDLL_FileIO.FVectShapeDataFileName))
                        {

                            if (null != FUpdateInterface)
                                FUpdateInterface(FActStep, FActLayer);

                            MessageBox.Show(
                                    string.Format( "File '{0}' loaded.", loadSaveFileName));
                        }   
                        #endregion
                        break;

                    case 2: // Process_TVectSimpleShapes
                        #region Process_TVectSimpleShapes
                        #endregion
                        break;

                    case 3: //Save_File
                        #region Save_File
                        MessageBox.Show(
                                string.Format("File '{0}' saved.", loadSaveFileName));
                        #endregion
                        break;

                    default:
                        break;
                }
                
            }
            else
            {
                MessageBox.Show(
                        string.Format("Failed '{0}'", loadSaveFileName));

            }
        }

        public static void Add_PlugIn_Method_Items(ref ToolStripMenuItem hostMenuItem)
        {
            if (hostMenuItem == null) return;

            string hostDLLName = hostMenuItem.Name;

            int methodNum = ClsRvCamDLL_PlugInDLL_FileIO.FPlugInMethodNames.Length;

            ToolStripMenuItem[] items = new ToolStripMenuItem[methodNum]; // You would obviously calculate this value at runtime
            for (int i=0; i<methodNum; i++)
            {
                string
                    sMethodName = ClsRvCamDLL_PlugInDLL_FileIO.FPlugInMethodNames[i],
                    sItemName = hostDLLName + sMethodName;

                items[i] = new ToolStripMenuItem();
                items[i].Name = hostDLLName + i.ToString();
                items[i].Tag = i;
                items[i].Text = sMethodName;

                //hostMenuItem.DropDownItems.Add(items[i]);

                items[i].Click += new EventHandler(PlugInMethodMenuItemClickHandler);
            }
            hostMenuItem.DropDownItems.AddRange(items);
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
                //items[i].Click += new EventHandler(PlugInMenuItemClickHandler);

                //hostMenuItem.DropDownItems.Add(items[i]);

                Add_PlugIn_Method_Items(ref items[i]);  
            }

            hostMenuItem.DropDownItems.AddRange(items);
#endif
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

#if _CopyStride
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
#else
                byte[] rgbValues = new byte[imgBytes];

                // 將 pImageStart0 指向的資料拷貝到 rgbValues[]-------------
                Marshal.Copy(
                   pImgStart0,
                   rgbValues, 0, imgBytes
                    );

                // 將 rgbValues[] 拷貝到 bmp.scan0-----------------------
                Marshal.Copy(rgbValues, 0, pNative, imgBytes);
#endif

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
            RvCam_Get_CamToImageXY(canvasID, Math.Min(camMinXmm,camMaxXmm), Math.Min(camMinYmm,camMaxYmm),
                ref imgMinX, ref imgMaxY);
            RvCam_Get_CamToImageXY(canvasID, Math.Max(camMinXmm, camMaxXmm), Math.Max(camMinYmm, camMaxYmm),
                ref imgMaxX, ref imgMinY);
        }
        public static void Convert_Image_To_CamMinMax(int canvasID,
            int imgMinX, int imgMinY, int imgMaxX, int imgMaxY,
            ref double camMinXmm, ref double camMinYmm, ref double camMaxXmm, ref double camMaxYmm)
        {
            RvCam_Get_ImageToCamXY(canvasID, Math.Min(imgMinX,imgMaxX), Math.Max(imgMinY, imgMaxY),
                ref camMinXmm, ref camMinYmm);
            RvCam_Get_ImageToCamXY(canvasID, Math.Max(imgMinX, imgMaxX), Math.Min(imgMinY, imgMaxY),
                ref camMaxXmm, ref camMaxYmm);
        }
        public static void Convert_ImageMinMax_To_CamCxyWidthHeight(int canvasID,
            int imgMinX, int imgMinY, int imgMaxX, int imgMaxY,
            ref double camCXmm, ref double camCYmm, ref double camWidthmm, ref double camHeightmm)
        {
            double minXmm = 0.0, minYmm = 0.0, maxXmm = 0.0, maxYmm = 0.0;            

            ClsRvCamDLL.Convert_Image_To_CamMinMax(
                    canvasID,
                    imgMinX,imgMinY,imgMaxX,imgMaxY,
                    ref minXmm, ref minYmm, ref maxXmm, ref maxYmm);

            camCXmm = (minXmm + maxXmm) / 2;
            camCYmm = (minYmm + maxYmm) / 2;
            camWidthmm = Math.Abs(maxXmm - minXmm);
            camHeightmm = Math.Abs(maxYmm - minYmm);
        }
        public static bool Convert_StringArray_To_String(
            string[] strAry, string sDivdeChar, ref string getStr)
        {
            getStr = "";
            if (strAry.Length<=0) return false;

            for (int i = 0; i<strAry.Length; i++)
            {
                if (i == 0) getStr = getStr + strAry[i];
                else getStr = getStr + sDivdeChar + strAry[i];
            }

            return (getStr != "");
        }
        public static bool Convert_String_To_StringArray(string str, string sDivideChar, 
            ref string[] getStrAry)
        {
            getStrAry = str.Split(sDivideChar);

            return (getStrAry.Length > 0);
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
        public static bool Get_ImageToCamMinMax(
            int canvasID, int imgMinX, int imgMinY, int imgMaxX, int imgMaxY,
            ref TFloat camMinX, ref TFloat camMinY, ref TFloat camMaxX, ref TFloat camMaxY
            )
        {
            RvCam_Get_ImageToCamXY(
                canvasID,
                Math.Min(imgMinX,imgMaxX), Math.Max(imgMinY,imgMaxY),
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
        public static bool Get_ViewCXY(int canvasID, ref double viewCXmm, ref double viewCYmm,
            bool byStoredView = false)
        {
            double viewWmm = 0.0, viewHmm = 0.0, viewMmPerPxl = 0.0, viewDeg = 0.0;
            bool viewMrX = false;

            return (CS_RvCam_Get_ViewInfo(canvasID, ref viewCXmm, ref viewCYmm,
                ref viewWmm, ref viewHmm, ref viewMmPerPxl, ref viewDeg, ref viewMrX, 
                byStoredView));
            
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
            ref double viewMinX, ref double viewMinY, ref double viewMaxX, ref double viewMaxY,
            bool byStoredView = false)
        {
            bool ret = false;

            double viewCXmm=0.0, viewCYmm=0.0, viewWmm = 0.0, viewHmm = 0.0, viewMmPerPxl = 0.0, viewDeg = 0.0;
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
            string sTitle = "Select ActionTarget";
            bool[] blSelected = [];
            int actID = (int)ClsRvCamDLL.ActionTarget;
            if (ClsRvCamDLL.CS_RvCam_Dialog_ItemSelect(sTitle, sFileTps, ref blSelected, ref actID, false))
            {
                ret = (TVectFileType)actID;

                //ClsRvCamDLL.EditMode = TEditMode.emSelect;
            }

            return ret;
        }
        public static bool Get_InputString(ref string sInput, string sTitle, string sPrompt)
        {
            sInput = Interaction.InputBox(
                sTitle,
                sPrompt,
                sInput,
                -1, -1);

            return ("" != sInput);
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
        public static void Paint_BitmapToPictureBox(Bitmap bmp, ref PictureBox pbx, bool strechDraw = false)
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
            Color imgBackgroudClr, ref Bitmap bmp, int imgAlpha = 125)
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
            if (imgBytePerPxl !=  bmpBytePerPxl ||
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
                byte[]  frRgbValues = new byte[bmpStrides],
                        toRgbValues = new byte[bmpStrides];

                IntPtr  pFr = pImgStart0,
#if YFlip_Bitmap
                        pTo = pBmpScan0 + (imgHeight-1)*imgStrideBytes,
#else
                        pTo = pBmpScan0,
#endif
                        pCurFr = pImgStart0, 
                        pCurTo = pBmpScan0;

                int bmpAlpha = 255 - imgAlpha;


                for (int iy = 0; iy < imgHeight-1; iy++)
                {
                    // 拷貝一整列資料---------------------------------
                    Marshal.Copy(pFr, frRgbValues, 0, imgStrideBytes );
                    Marshal.Copy(pTo, toRgbValues, 0, imgStrideBytes);

                    for(int ix = 0; ix < toRgbValues.Length; ix+=4) // 一次跳 4 byte
                    {
                        int idAA = ix+3, idRR=ix+2, idGG=ix+1, idBB=ix;

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
#if YFlip_Bitmap
                    pTo -= bmpStrides;
#else
                    pTo += bmpStrides;
#endif
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
            Color[] paintClrs,  int canvasID, 
            ref Bitmap cnvBmp, int cnvWidth, int cnvHeight, 
            PixelFormat cnvPF = PixelFormat.Format32bppArgb,
            TVectPaintMode paintMode = TVectPaintMode.pmSolid_Normal)
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

            Update_Bitmap( ref cnvBmp, cnvWidth, cnvHeight,
                cnvPF, Color.Black  );

            Graphics g = Graphics.FromImage(cnvBmp);
            g.Clear(Color.Black);

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
                    paintMode))
            {
                //將 cnvScan0 的資料 拷貝到 cnvBmp-------------------------------------------
                Convert_IntPtrToBitmap(cnvScan0, cnvWidth, cnvHeight, imgStrideBytes, cnvBitsPerPixel, ref cnvBmp);     

                ret = true;                
            }
#else
            byte imgAlpha = 125;

            for (int i = 0; i<aLyrs.Length; i++)
            {
                if ( ClsRvCamDLL.CS_RvCam_Paint_Canvas(
                   aStep, aLyrs[i], paintClrs[i],
                   canvasID,
                   ref cnvScan0, imgStrideBytes,
                   cnvWidth, cnvHeight, cnvBitsPerPixel,
                   true,
                   paintMode))
                {
                    if (i == aLyrs.Length - 1) imgAlpha = 255;
                    else imgAlpha = 100;

                    //將 cnvScan0 的資料 Alpha Blend 到 cnvBmp-------------------------------------
                    Paint_IntPtrToBitmap_AlphaBlend(cnvScan0, cnvWidth, cnvHeight, imgStrideBytes, cnvBitsPerPixel, 
                        Color.Black, ref cnvBmp, imgAlpha);
                    //cnvBmp.Save("d:\\temp\\cnv.bmp");
                    ret = true;
                }
            }
#endif


#if DEBUG
            //cnvBmp.Save("d:\\temp\\cnv.bmp");
#endif

            return ret;
        }        
        /// <summary>
        /// 在PictureBox 上畫 Rectangle
        /// </summary>
        /// <param name="pbx"></param>
        /// <param name="aLeft"></param>
        /// <param name="aTop"></param>
        /// <param name="aRight"></param>
        /// <param name="aBottom"></param>
        /// <param name="paintClr"></param>
        /// <param name="penThickness"></param>
        public static void Paint_Rectangle(ref PictureBox pbx, 
            int aLeft, int aTop, int aRight, int aBottom, 
            Color paintClr, int penThickness = 2
            )
        {
            //pbx.Refresh();
            Pen drwaPen = new Pen(paintClr, penThickness);
            int width = aRight - aLeft, height = aBottom-aTop;

            Rectangle rect = new Rectangle(
                Math.Min(aLeft, aRight),
                Math.Min(aTop, aBottom),
                width * Math.Sign(width),
                height * Math.Sign(height));

            Graphics g = pbx.CreateGraphics();
            g.DrawRectangle(drwaPen, rect);
        }
        public static void Paint_Cam_PictureBox(int canvasID,
            PictureBox pbx, int paintStep, int[] paintLayers,
            Color[] paintLayerColors,
            TVectPaintMode paintMode = TVectPaintMode.pmSolid_Normal,
            TViewMode viewMode = TViewMode.vmCanvasSizeChanged)
        {
            ClsRvCamDLL.UpdateView_And_PaintToPictureBox(
                    canvasID, pbx,
                    viewMode,
                    paintStep,
                    paintLayers, //[FActLayer],
                    paintLayerColors, //[ClsRvCamDLL.Get_LayerColor(FActLayer)],
                    paintMode
                    );
        }

        public static void Set_StepColor(int stepID,  Color stepColor)
        {
            if (stepID < 0 || stepID >= FStepColors.Length) return;

            FStepColors[stepID % FStepColors.Length] = stepColor;
        }
        public static void Set_LayerColor(int layerID, Color layerColor)
        {
            if (layerID < 0 || layerID >= FLayerColors.Length) return;

            FLayerColors[layerID % FLayerColors.Length] = layerColor;
        }


        /// <summary>
        /// 更新 Bitmap
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="bmpWidth"></param>
        /// <param name="bmpHeight"></param>
        /// <param name="bmpPF"></param>
        /// <param name="bkClr"></param>
        public static void Update_Bitmap(ref Bitmap bmp, int bmpWidth, int bmpHeight, PixelFormat bmpPF,
            Color bkClr)
        {
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
        private static bool UpdateView_And_PaintToBitmap(
            int canvasID, int canvasWidth, int canvasHeight,
            TViewMode atViewMode,
            int viewStep, int[] viewLayers, Color[] paintLayerColors,
            ref Bitmap paintBmp, PixelFormat pf
            )
        {
            if (viewLayers.Length<=0  || viewLayers.Length!=paintLayerColors.Length) { return false;}

            ClsRvCamDLL.CS_RvCam_View_Update(canvasID,
                  canvasWidth,canvasHeight,
                  viewStep, viewLayers[0], atViewMode);

            return  ClsRvCamDLL.Paint_CamToBitmap(
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
        public static bool UpdateView_And_PaintToPictureBox(
            int canvasID, PictureBox pbx,
            TViewMode paintViewMode,
            int viewStep, int[] viewLayers, Color[] paintLayerColors,
            TVectPaintMode paintMode = TVectPaintMode.pmSolid_Normal,
            bool stretchDraw = false
            )
        {

            bool ret = false;

            if (viewLayers.Length<=0  || viewLayers.Length!=paintLayerColors.Length) { return false;}

            ClsRvCamDLL.CS_RvCam_View_Update(canvasID,
                  pbx.Width,pbx.Height,
                  viewStep, viewLayers[0], paintViewMode);

            
            if (true ==  ClsRvCamDLL.Paint_CamToBitmap(
                    viewStep, viewLayers,
                    paintLayerColors, canvasID,
                    ref PaintDummyBmp, pbx.Width, pbx.Height,
                    PixelFormat.Format32bppArgb,
                    paintMode
                    ) )
            {

               Paint_BitmapToPictureBox(PaintDummyBmp, ref pbx, stretchDraw);

               ret = true;
            }
            else
            {
                Update_Bitmap(ref PaintDummyBmp, pbx.Width, pbx.Height,
                    PixelFormat.Format32bppArgb,
                    Color.Black);

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
        public static void UpdateView_Pan(
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
        public static bool UpdateView_ViewMinMax(
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


#endregion

    }

#pragma warning restore CS8601 // 可能有 Null 參考指派。
#pragma warning restore CS8618 // 可能有 Null 參考指派。
#pragma warning restore CS8604
#pragma warning restore CS8602
#pragma warning restore CS0642
#pragma warning restore CS0219
#pragma warning restore CS8600
#pragma warning restore CS8622


}