
#define _FixDelphiMissingCallDLL // 如果 PlugInDLL 不和 EXE 同一路徑，必須加上這個定義



using System;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;


#region RvLib --------------------------------------- 
using RvLib.M2dTypeDefine;
using RvLib.VectTypeDefine;
using System.Windows.Forms;
#endregion



namespace RvLib.RvCamDLL_PlugIn_FileIO
{
#pragma warning disable CS8605 // 取消 warnning 
#pragma warning disable CS8600 // 取消 warnning 
#pragma warning disable CS8620 // 取消 warnning 
#pragma warning disable CS8601 // 取消 warnning 

    #region Windows Native API ================================= 
    public static class ClsRvCamDLL_PlugInDLL_FileIO
    {
        /// <summary>
        /// 原型是 :HMODULE LoadLibrary(LPCTSTR lpFileName);
        /// </summary>
        /// <param name="lpFileName">DLL 文件名 </param>
        /// <returns> 函数库模块的句柄 </returns>
        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        /// <summary>
        /// 原型是 : FARPROC GetProcAddress(HMODULE hModule, LPCWSTR lpProcName);
        /// </summary>
        /// <param name="hModule"> 包含需调用函数的函数库模块的句柄 </param>
        /// <param name="lpProcName"> 调用函数的名称 </param>
        /// <returns> 函数指针 </returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        /// <summary>
        /// 原型是 : BOOL FreeLibrary(HMODULE hModule);
        /// </summary>
        /// <param name="hModule"> 需释放的函数库模块的句柄 </param>
        /// <returns> 是否已释放指定的 Dll</returns>
        [DllImport("kernel32", EntryPoint = "FreeLibrary", SetLastError = true)]
        static extern bool FreeLibrary(IntPtr hModule);

        #region DLL Method TypeDefine =================================
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]  // Cdecl
        public delegate TReturnCode TIs_RvCamDLL(ref IntPtr pGetDLlDescription);
        public delegate TReturnCode TLoad_File(IntPtr pLoadFileName, ref IntPtr pGetShapeDataArray0, ref int getShapeDataLength);
        public delegate TReturnCode TSave_File(IntPtr pSaveFileName, IntPtr pSetShapeDataArray0, int setShapeDataLength);
        public delegate TReturnCode TProcess_TVectSimpleShape(ref IntPtr pEditShapeDataArray0, ref int editShapeDataLength);
        #endregion




#if _FixDelphiMissingCallDLL
        public static string FullExeName = System.Reflection.Assembly.GetExecutingAssembly().Location;
        private static string cPlugInRoot = System.IO.Path.GetDirectoryName(FullExeName) + "\\PlugIns\\"; //  ".\\PlugIns\\";
#else
        private static string cPlugInRoot = ".\\PlugIns\\";
#endif
        public static string[] FPlugInDLLFileNames = [], FPlugInDLLDescriptions = [];
        public static string[] FPlugInMethodNames = ["Is_RvCamDLL", "Load_File", "Process_TVectSimpleShapes", "Save_File"];
        public static TVectSimpleShape[] FVectShapeData = [];
        public static string FVectShapeDataFileName = "";

        public static string GetLibraryPathname(string filename)
        {
            // If 64-bit process, load 64-bit DLL
            bool is64bit = System.Environment.Is64BitProcess;

            string prefix = "Win32";

            if (is64bit)
            {
                prefix = "x64";
            }

            var lib1 = prefix + @"\" + filename;

            return lib1;
        }


        public static void Copy_TVectSimpleShapes(IntPtr pFrShpsArray0, int shpsArrayLength, ref TVectSimpleShape[] toVectShpsData)
        {
            toVectShpsData = new TVectSimpleShape[shpsArrayLength];

            //string sShps = "";
            int szShp = Marshal.SizeOf(typeof(TVectSimpleShape));
            //szShp = 149; //
            IntPtr pCurShp = pFrShpsArray0;
            for (int i = 0; i < shpsArrayLength; i++)
            {
                try
                {
                    // Point in unmanaged memory.
                    TVectSimpleShape bShp = (TVectSimpleShape)Marshal.PtrToStructure(pCurShp, typeof(TVectSimpleShape));
                    //string shpName = Marshal.PtrToStringUni(bShp.esName);
                    //sShps += string.Format("\t{0}\n", shpName);

                    toVectShpsData[i] = new TVectSimpleShape(bShp); // bShp;
                }
                finally
                {
                }
                pCurShp += szShp;  // IntPtr 每次加上 TVectSimpleShape大小
            }
        }

        public static Delegate ProcGetMethod<TGetMethodName>(string dllPath, string functionName,
            ref IntPtr hModule)
        {
            hModule = LoadLibrary(dllPath);
            IntPtr functionAddress = GetProcAddress(hModule, functionName);
            return Marshal.GetDelegateForFunctionPointer(functionAddress, typeof(TGetMethodName));
        }

        #region  Process DLL Methods =================================
        public static bool Process_Method_Is_RvCamDLL(IntPtr procAddr)
        {
            if (procAddr == IntPtr.Zero) return false;

            TIs_RvCamDLL getMethodName =
                (TIs_RvCamDLL)Marshal.GetDelegateForFunctionPointer(procAddr, typeof(TIs_RvCamDLL));

            IntPtr pGetDLLDescript = IntPtr.Zero;
            if (TReturnCode.rcSuccess == getMethodName(ref pGetDLLDescript))
            {
                if (IntPtr.Zero != pGetDLLDescript)
                {
                    string sInfo = Marshal.PtrToStringUni(pGetDLLDescript);
                    MessageBox.Show(sInfo);
                }
            }

            return true;
        }
        public static bool Process_Method_Load_File(IntPtr procAddr, ref TVectSimpleShape[] shapeData,
                ref string loadFileName)
        {
            loadFileName = "";
            FVectShapeData = [];

            bool ret = false;

            if (procAddr == IntPtr.Zero) return false;

            TLoad_File getMethodName =
                (TLoad_File)Marshal.GetDelegateForFunctionPointer(procAddr, typeof(TLoad_File));

            
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory =
                   // Directory.GetCurrentDirectory(); // "c:\\";
                   Path.GetDirectoryName(ClsM2dTypeDefineVabiable.mLastFile);
                openFileDialog.FileName = ClsM2dTypeDefineVabiable.mLastFile;

                openFileDialog.Filter = "All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    ClsM2dTypeDefineVabiable.mLastFile = openFileDialog.FileName;

                    //Get the path of specified file
                    string fileName = openFileDialog.FileName;
                    IntPtr pLoadFn = Marshal.StringToBSTR(fileName);
                    IntPtr pGetShpAry0 = IntPtr.Zero;
                    int shpAryLength = 0;

                    if (TReturnCode.rcSuccess == getMethodName(pLoadFn, ref pGetShpAry0, ref shpAryLength))
                    {
                        loadFileName = openFileDialog.FileName;
                        FVectShapeDataFileName = Path.GetFileName( Marshal.PtrToStringUni(pLoadFn) );
                        Copy_TVectSimpleShapes(pGetShpAry0, shpAryLength, ref FVectShapeData);

                        ret = true;
                        //MessageBox.Show( string.Format("ShpAry Length : {0}\n\n{1} ",shpAryLength, sShps) );
                    }
                    
                }
            }
            return ret;
        }
        public static bool Process_Method_Process_TVectSimpleShapes(IntPtr procAddr, ref TVectSimpleShape[] shapeData, ref string saveFileName)
        {
            saveFileName = "";

            bool ret = false;

            if (procAddr == IntPtr.Zero) return false;

            TProcess_TVectSimpleShape getMethodName =
                (TProcess_TVectSimpleShape)Marshal.GetDelegateForFunctionPointer(procAddr, typeof(TProcess_TVectSimpleShape));

            string fileName = "";
            IntPtr pLoadFn = Marshal.StringToBSTR(fileName);
            IntPtr pGetShpAry0 = IntPtr.Zero;
            int shpAryLength = 0;

            if (TReturnCode.rcSuccess == getMethodName(ref pGetShpAry0, ref shpAryLength))
            {
                FVectShapeDataFileName = Path.GetFileName(Marshal.PtrToStringUni(pLoadFn));
                Copy_TVectSimpleShapes(pGetShpAry0, shpAryLength, ref FVectShapeData);

                ret = true;
                //MessageBox.Show( string.Format("ShpAry Length : {0}\n\n{1} ",shpAryLength, sShps) );
            }

            return ret;
        }
        public static bool Process_Method_Save_File(IntPtr procAddr, ref TVectSimpleShape[] shapeData, ref string saveFileName)
        {
            saveFileName = "";

            if (procAddr == IntPtr.Zero) return false;


            TSave_File getMethodName =
                (TSave_File)Marshal.GetDelegateForFunctionPointer(procAddr, typeof(TSave_File));


            using (SaveFileDialog SaveFileDialog = new SaveFileDialog())
            {
                SaveFileDialog.InitialDirectory =
                   // Directory.GetCurrentDirectory(); // "c:\\";
                   Path.GetDirectoryName(ClsM2dTypeDefineVabiable.mLastFile);
                SaveFileDialog.FileName = "*.*"; // ClsM2dTypeDefineVabiable.mLastFile;

                SaveFileDialog.Filter = "All files (*.*)|*.*";
                SaveFileDialog.FilterIndex = 1;
                SaveFileDialog.RestoreDirectory = true;

                if (SaveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    ClsM2dTypeDefineVabiable.mLastFile = SaveFileDialog.FileName;

                    //Get the path of specified file
                    string fileName = SaveFileDialog.FileName;
                    IntPtr pSavedFn = Marshal.StringToBSTR(fileName);

                    unsafe
                    {
                        fixed (TVectSimpleShape* pSetShpAry0 = &(shapeData[0]))
                        {
                            int shpAryLength = shapeData.Length;

                            if (TReturnCode.rcSuccess == getMethodName(pSavedFn,  (IntPtr)pSetShpAry0, shpAryLength))
                            {
                                saveFileName = SaveFileDialog.FileName;
                                return true;
                            }

                        }
                    }
                    

                }
            }

            return true;
        }
        #endregion

        public static bool LoadLibraryMethod_And_Run(int dllFnID, int methodId, ref string loadSaveFileName, ref TVectSimpleShape[] shpData)
        {
            bool ret = false;
            loadSaveFileName = "";


            if (dllFnID < 0 || dllFnID >= FPlugInDLLFileNames.Length) return false;
            if (methodId < 0 || methodId >= FPlugInMethodNames.Length) return false;

            string
                fn = FPlugInDLLFileNames[dllFnID],
                methodName = FPlugInMethodNames[methodId];


            if (!File.Exists(fn)) return false;

            // 取得函数库模块的句柄
            var hModule = LoadLibrary(fn); // FPlugInDLLFileNames[i]);

            try
            {
                // 若函数库模块的句柄为空，则抛出异常
                if (hModule != IntPtr.Zero)
                {

                    // 取得函数指针
                    IntPtr farProc = GetProcAddress(hModule, methodName);
                    // 若函數存在---------------------------
                    if (farProc != IntPtr.Zero)
                    {
                        // 有 "Get_Method() 的 DlL 才加入
                        //if (methodName == "Is_RvCamDLL") 
                        switch (methodId)
                        {
                            case 0: //"Is_RvCamDLL"
                                ret = Process_Method_Is_RvCamDLL(farProc);
                                break;

                            case 1: //"Load_File"
                                shpData = [];
                                if (Process_Method_Load_File(farProc, ref shpData, ref loadSaveFileName))
                                {
#if DEBUG
                                    //string sShps = "";
                                    //for (int i = 0; i < shpData.Length; i++)
                                    //{
                                    //    string shpName = string.Format("{0}({1})", shpData[i].ToString(), i);
                                    //    sShps += string.Format("\t{0}\n", shpName);

                                    //}
                                    //MessageBox.Show(string.Format("ShpAry Length : {0}\n\n{1} ", shpData.Length, sShps));
#endif
                                    ret = (shpData.Length>0);
                                }
                                break;

                            case 2:// "Process_TVectSimpleShapes"
                                ret = Process_Method_Process_TVectSimpleShapes(farProc, ref shpData, ref loadSaveFileName);
                                break;

                            case 3:// "Save_File"
                                ret = Process_Method_Save_File(farProc, ref shpData, ref loadSaveFileName);
                                break;

                            default:
                                break;
                        }

                    }
                }
                return ret;
            }
            finally
            {
                if (IntPtr.Zero != hModule)
                    FreeLibrary(hModule);
            }
        }

        public static void Get_PlugInNames(ref string[] plugInDllNames)
        {
            if (!Directory.Exists(cPlugInRoot)) return;

            string[] dllFns = Directory.GetFiles(cPlugInRoot);

#if DEBUG
            //string sFns = "";
            //for (int i = 0; i < dllFns.Length; i++) { sFns = sFns + "\n\t" + dllFns[i]; }
            //MessageBox.Show(string.Format("DLL Names ({0}) :\n\n\t{1}",
            //    dllFns.Length, sFns));
#endif

            for (int i = 0; i < dllFns.Length; i++)
            {
                string fn = dllFns[i]; // Path.GetFullPath(FPlugInDLLFileNames[i]);

                if (!File.Exists(fn)) return;

                string sExt = Path.GetExtension(fn);

                //if (0 == string.Compare(sExt, ".dll", true) )
                if (ClsDelphiEquivalent.SameText(sExt, ".dll"))
                {        // 取得函数库模块的句柄
                    var hModule = LoadLibrary(fn); // FPlugInDLLFileNames[i]);

                    try
                    {
                        // 若函数库模块的句柄为空，则抛出异常
                        if (hModule != IntPtr.Zero)
                        {

                            for (int iMethod = 0; iMethod < FPlugInMethodNames.Length; iMethod++)
                            {
                                // 取得函数指针
                                IntPtr farProc = GetProcAddress(hModule, FPlugInMethodNames[iMethod]);
                                // 若函數存在---------------------------
                                if (farProc != IntPtr.Zero)
                                {
                                    // 有 "Get_Method() 的 DlL 才加入
                                    if (FPlugInMethodNames[iMethod] == "Is_RvCamDLL") //只加入一次 dll name
                                    {
                                        ArrayExtension.Add(ref FPlugInDLLFileNames, fn);
                                        string sInfo = Path.GetFileName(fn);

                                        TIs_RvCamDLL getMethodName =
                                            (TIs_RvCamDLL)Marshal.GetDelegateForFunctionPointer(
                                                farProc,
                                                typeof(TIs_RvCamDLL));
                                        IntPtr pGetStr = IntPtr.Zero;
                                        if (TReturnCode.rcSuccess == getMethodName(ref pGetStr))
                                        {
                                            sInfo = Marshal.PtrToStringUni(pGetStr);
    #if DEBUG
                                            //MessageBox.Show(string.Format("Method({0}): \n{1}( ) : \n\"{2}\" ", 
                                            //    iMethod, 
                                            //    FPlugInMethodNames[iMethod],
                                            //    Marshal.PtrToStringUni(pGetStr) ));
    #endif
                                        }

                                        ArrayExtension.Add(ref FPlugInDLLDescriptions, sInfo);
                                    }
                                    else
                                    {
    #if DEBUG
                                        //MessageBox.Show(string.Format("Method({0}): \n{1}( ) :", iMethod, FPlugInMethodNames[iMethod]));
    #endif
                                    }
                                }
                                else
                                {
                                    // 非PlugIn Dll 沒有 Get_Method(); 所以不警告
                                    //throw (new Exception(" 没有找到 :" + cPlugInMethodName + " 这个函数的入口点 "));
                                }
                            }

                        }
    #if DEBUG
                        else
                        {
                            //throw (new Exception(" 没有找到 :" + fn + "."));
                        }
    #endif
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        MessageBox.Show(string.Format("{1}\n\nFailed to load '{0}'.", fn, e.Message));
                        throw;
                    }
                    finally
                    {
                        if (IntPtr.Zero != hModule)
                            FreeLibrary(hModule);
                    }
                }

            

            }

        }

        /// <summary>
        /// 卸载 Dll
        /// </summary>
        public static void Release_PlugIns()
        {
            FPlugInDLLFileNames = [];
        }
    }
    #endregion

#pragma warning restore CS8622 // 取消 warnning 
#pragma warning restore CS8600 // 取消 warnning 
#pragma warning restore CS8620 // 取消 warnning 
#pragma warning restore CS8601 // 取消 warnning 
}