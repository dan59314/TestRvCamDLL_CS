
#define RvCamDLL
//#define _EnableCallBackFunctions  // 偶爾會導致程式當掉


using Microsoft.VisualBasic;
using RvCamDLL;
using System;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using static RvCamDLL.ClsRvCamDLL;

using M2dTypeDefine;
using VectTypeDefine;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using RvCamDLL_PlugIn_FileIO;


namespace TestRvCamDLL_CS
{
     using TFloat = double;


#pragma warning disable CS8602 // 取消 warnning  可能 null 參考的取值 。
#pragma warning disable CS8604 // 取消 warnning  可能 null 參考的取值 。
#pragma warning disable CS0642 // 取消 warnning 
#pragma warning disable CS8600 // 取消 warnning 
#pragma warning disable CS8618 // 取消 warnning 
#pragma warning disable CS8622 // 取消 warnning 
    public partial class MainForm : Form
    {

        #region Const ======================================================================
        private static int cCanvasID_pbMain = 0, cCanvasID_pbSub = 1;
        #endregion


        #region Variables ==================================================================
        int FMouseDownX, FMouseDownY;
        private int[] FCheckedLayers = [];
        private Color[] FCheckedLayerColors = [];
        private bool FVisible_ChildSteps = true;
        private bool FVisible_Pads = true;
        private bool FVisible_Lines = true;
        private bool FVisible_Polygons = true;
        #endregion


        #region property =====================================================================
        public int[] CheckedLayers
        {
            get
            {
                FCheckedLayers = [];

                if (ckLstBxLayers.SelectedIndex < 0) return [];

                for (int i = 0; i < ClsRvCamDLL.FLayerNames.Length; i++)
                {
                    if (i != ckLstBxLayers.SelectedIndex && i < ckLstBxLayers.Items.Count && ckLstBxLayers.GetItemChecked(i))
                    {
                        ArrayExtension.Add(ref FCheckedLayers, i);
                    }
                }
                // ActLayer 加到最後面----------------------------
                ArrayExtension.Add(ref FCheckedLayers, ckLstBxLayers.SelectedIndex);
                return FCheckedLayers;
            }
        }

        public Color[] CheckedLayerColors
        {
            get
            {
                FCheckedLayerColors = [];
                if (ckLstBxLayers.SelectedIndex < 0) return [];

                FCheckedLayers = [];
                for (int i = 0; i < FLayerNames.Length; i++)
                {
                    if (i != ckLstBxLayers.SelectedIndex && i < ckLstBxLayers.Items.Count && ckLstBxLayers.GetItemChecked(i))
                    {
                        ArrayExtension.Add(ref FCheckedLayerColors,
                           ClsRvCamDLL.Get_LayerColor(i));
                    }
                }
                // ActLayer 加到最後面----------------------------
                ArrayExtension.Add(ref FCheckedLayerColors,
                     ClsRvCamDLL.Get_LayerColor(ckLstBxLayers.SelectedIndex));
                return FCheckedLayerColors;
            }
        }

        private TVectPaintMode FVectPaintMode = TVectPaintMode.pmSolid_Normal;
        public TVectPaintMode VectPaintMode
        {
            get { return FVectPaintMode; }
            set
            {
                FVectPaintMode = value;
                // 繪圖------------------------------------------------------------
                Update_Paint_PictureBoxA(cCanvasID_pbMain, pbMain, FVectPaintMode);
            }
        }


        public int ActLayer   // property
        {
            get { return ClsRvCamDLL.FActLayer; }   // get method
            set
            {
                FActLayer = value;
                if (FActLayer >= 0 && FActLayer < ckLstBxLayers.Items.Count)
                {
                    ckLstBxLayers.SelectedIndex = FActLayer;
                    tbLayers.SelectedIndex = FActLayer;
                }
                slLogInfo.Text = string.Format("Stp({0}), Lyr({1})", FActStep, FActLayer);

                // Home View --------------------------------------------------------------

                // 繪圖------------------------------------------------------------
                Update_Paint_PictureBoxA(cCanvasID_pbMain, pbMain, FVectPaintMode);

                ClsRvCamDLL.CS_RvCam_View_Update(cCanvasID_pbSub,
                    pbSub.Width, pbSub.Height, ActStep, FActLayer, TViewMode.vmHome);
                Update_Paint_PictureBoxA(cCanvasID_pbSub, pbSub, TVectPaintMode.pmSolid_Normal, FActLayer);
            }  // set method
        }

        public int ActStep   // property
        {
            get { return FActStep; }   // get method
            set
            {
                FActStep = value;
                slLogInfo.Text = string.Format("Stp({0}), Lyr({1})", FActStep, FActLayer);

                // Home View --------------------------------------------------------------

                // 繪圖------------------------------------------------------------
                Update_Paint_PictureBoxA(cCanvasID_pbMain, pbMain, FVectPaintMode);

                ClsRvCamDLL.CS_RvCam_View_Update(cCanvasID_pbSub,
                    pbSub.Width, pbSub.Height, ActStep, FActLayer, TViewMode.vmHome);
                Update_Paint_PictureBoxA(cCanvasID_pbSub, pbSub, TVectPaintMode.pmSolid_Normal, FActLayer);
            }
        }
        #endregion


        #region Misc. Functions ======================================================================
        public void FormOnRunningProgress(
            ref byte aMainPercent, ref byte aChildPercent,
            IntPtr sMainTask, IntPtr sSubTask)
        {
            if (IntPtr.Zero == sMainTask || IntPtr.Zero == sSubTask) return;


            slMain.Text = Marshal.PtrToStringAnsi(sMainTask);
            slSub.Text = Marshal.PtrToStringAnsi(sSubTask);

            prgMain.Value = aMainPercent;
            prgSub.Value = aChildPercent;

            stsbar0.Refresh(); //加上此行才能更新

        }
        public void FormOnLogInfo(
            IntPtr sLogInfo)
        {
            string sInfo = Marshal.PtrToStringAnsi(sLogInfo);
            string[] sInfos = sInfo.Split("@");

            //if (sInfos.Length == 4)
            //{
            //    slMain.Text = sInfos[0];
            //    slSub.Text = sInfos[2];

            //    prgMain.Value = Convert.ToInt32(sInfos[1]);
            //    prgSub.Value = Convert.ToInt32(sInfos[3]);

            //}
            //else
            {
                slLogInfo.Text = sInfo;
            }

            stsbar0.Refresh();
        }

        public void DoUpdateInterface(
            int aStep, int aLayer)
        {
            Initial_Interfaces_StepLayers();

            FActStep = aStep;
            ActLayer = aLayer;
        }

        private bool Get_OdbStepListType(
            ref TOdbStepListType odbStpTp)
        {
            bool ret = false;

            odbStpTp = TOdbStepListType.osAllSteps;

            string inputStr = "0";

            if (Get_InputString(ref inputStr,
                    string.Format("Input Steps List Type : 0:{0}, 1:{1}, 2:{2}, 3:{3}, 4:{4}, ",
                        TOdbStepListType.osAllSteps.ToString(),
                        TOdbStepListType.osInheritedStepsOnly.ToString(),
                        TOdbStepListType.osBottomStepsOnly.ToString(),
                        TOdbStepListType.osIndependentStepsOnly.ToString()), "Input Odb Steps List Type"))
            {
                int iVal = Convert.ToInt32(inputStr);

                if (Enum.IsDefined(typeof(TOdbStepListType), iVal))
                {
                    odbStpTp = (TOdbStepListType)iVal;
                    ret = true;
                }
            }

            return ret;
        }

        private void Initial_Componet_Properties()
        {
            this.KeyPreview = true;
        }
        private void Initial_Interfaces_StepLayers(
            string getStps = "", string getLyrs = "")
        {
            //tbSteps.TabPages.Insert(0, steps);
            //tbSteps.TabPages.Remove()

            if ("" == getStps || "" == getLyrs)
                ClsRvCamDLL.CS_RvCam_Get_StepsLayers_CurrentData(ref getStps, ref getLyrs);

            getStps.Trim();
            getLyrs.Trim();

            if ("" == getStps)
                FStepNames = [];
            else
                FStepNames = getStps.Split(',');
            if ("" == getLyrs)
                FLayerNames = [];
            else
                FLayerNames = getLyrs.Split(",");

            int oStp = FActStep, oLyr = FActLayer;

            tbSteps.TabPages.Clear();
            tbLayers.TabPages.Clear();
            ckLstBxLayers.Items.Clear();

            try
            {
                foreach (string s in FStepNames)
                {
                    tbSteps.TabPages.Add(s);
                }

                foreach (string s in FLayerNames)
                {
                    tbLayers.TabPages.Add(s);
                    ckLstBxLayers.Items.Add(s);
                }


                this.tbSteps.Update();
                this.tbLayers.Update();

                ClsRvCamDLL.CS_RvCam_View_Update(cCanvasID_pbMain, pbMain.Width, pbMain.Height, 0, 0, TViewMode.vmHome);

                ActStep = Math.Max(0, Math.Min(FStepNames.Length - 1, oStp)); ;
                ActLayer = Math.Max(0, Math.Min(FLayerNames.Length - 1, oLyr));

            }
            finally
            {
            }

        }

        private void Show_StepLayers(string fn, string
            getSteps, string getLayers)
        {
            if (("" == getSteps) || ("" == getLayers))
                return;

            string[] steps = getSteps.Split(",");
            string[] lyrs = getLayers.Split(",");

#if _SplitStrings

            string s1 = string.Format("OdbTgz :  \n\t{0}\n", fn);

            s1 = s1 + string.Format("Stesps ({0}) :  \n",  steps.Length );
            foreach (var sStp in steps)
            {
                s1 = s1 + string.Format("\t{0}\n",  sStp);
            }

            s1 = s1 + string.Format("Layers ({0}) :  \n", lyrs.Length);
            foreach (var sLyr in lyrs)
            {
                s1 = s1 + string.Format("\t{0}\n", sLyr);
            }

            MessageBox.Show(s1);
#else
            MessageBox.Show(string.Format("OdbTgz : \n\t{0}\n\nSteps({3}) : \n\t{1}\n\nLayers({4}) : \n\t{2}\n",
                   fn, getSteps, getLayers, steps.Length, lyrs.Length));
#endif
        }

        private void Update_Paint_PictureBoxA(int canvasID, PictureBox pbx,
            TVectPaintMode paintMode, int paintLayer = -1
            )
        {
            // 繪圖------------------------------------------------------------
            int[] paintLyrs = [];
            Color[] paintClrs = [];

            if (paintLayer >= 0) { paintLyrs = [paintLayer]; paintClrs = [Get_LayerColor(paintLayer)]; }
            else { paintLyrs = CheckedLayers; paintClrs = CheckedLayerColors; }

            ClsRvCamDLL.Paint_Cam_PictureBox(
                canvasID, pbx,
                ActStep,
                paintLyrs, //CheckedLayers, 
                paintClrs,
                paintMode);
            pbx.Refresh(); //Update(); 
        }

        private void Reset_To_Default()
        {
            ClsRvCamDLL.EditMode = TEditMode.emNone;
            ClsRvCamDLL.SelectAction = TSelectAction.saSelect;
            ClsRvCamDLL.ActionTarget = TActionTarget.smTVectObject;

            VectPaintMode = TVectPaintMode.pmSolid_Normal;

            ClsRvCamDLL.UpdateView_And_PaintToPictureBox(
                cCanvasID_pbMain, pbMain,
                TViewMode.vmHome,
                FActStep,
                CheckedLayers, //[FActLayer],
                CheckedLayerColors, //[ClsRvCamDLL.Get_LayerColor(FActLayer)],
                FVectPaintMode
                );

            VectPaintMode = TVectPaintMode.pmSolid_Normal;
        }

        private void Create_Members()
        {
        }
        private void Initial_Members()
        {

#if _EnableCallBackFunctions
            MessageBox.Show(
                    "如果程式會當掉，請取消  ''#define _ _EnableCallBackFunctions'' ");

            TOnRunningProgress callBackRunPrg = new TOnRunningProgress(FormOnRunningProgress);
            ClsRvCamDLL.CS_RvCam_AssignRunningProgressCallBackFunc(callBackRunPrg);


            TOnLogInfo callBackLogInfo = new TOnLogInfo(FormOnLogInfo);
            ClsRvCamDLL.CS_RvCam_AssignLogInfoFunc(callBackLogInfo);
#endif
            ClsRvCamDLL.FUpdateInterface = DoUpdateInterface; // (int aStep, int aLayer)

            FStepNames = [];
            FLayerNames = [];

            FCheckedLayers = [];
            FCheckedLayerColors = [];

        }
        private void Release_Members()
        {

        }
        #endregion




        public MainForm()
        {
            Initial_Componet_Properties();

            InitializeComponent();

            Create_Members();

            Initial_Members();  // members 初始化放在此地

            Initial_Interfaces_StepLayers(); // "pcb,array,panel", "csm,csk,comp,l2,l3,l4,sold,ssk,ssm,pth");

            ClsRvCamDLL.Add_PlugInItems(ref this.miPlugIns);
        }

        ~MainForm()
        {
            Release_Members();
        }

        private void pbMain_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                this.ViewModeClick(miViewZoomIn, null);
            }
            else
            {
                this.ViewModeClick(miViewZoomOut, null);
            }


            pbSub.Refresh(); // 更新 pbSub viewMinMax 框框繪圖
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            MouseWheel += new MouseEventHandler(pbMain_MouseWheel);
        }

        private void btnDLLInfo_Click(object sender, EventArgs e)
        {
            string strDLLInfo = "";

            ClsRvCamDLL.CS_RvCam_GetDLLInfo(ref strDLLInfo);

            MessageBox.Show(strDLLInfo);
        }

        private void btnIsAuthorized_Click(object sender, EventArgs e)
        {
            if (ClsRvCamDLL.CS_RvCam_IsAuthorized())
            {
                MessageBox.Show("Authorized.");
            }
            else
            {
                MessageBox.Show("UnAuthorized!");
            }

        }


        private void miGet_StepLayers_ODB0_Click(object sender, EventArgs e)
        {
            TOdbStepListType odbStpTp = TOdbStepListType.osAllSteps;
            if (!Get_OdbStepListType(ref odbStpTp))
                return;


            string getOdbDirTgzFn = " ", getSteps = " ", getLyrs = " ";

            if (ClsRvCamDLL.CS_RvCam_Get_StepsLayers_ODB_Dialog(
                ref getOdbDirTgzFn, ref getSteps, ref getLyrs, TOdbTgzType.otTgzFile, odbStpTp))
            {
                //MessageBox.Show(string.Format("OdbTgz : \n\t{0}\n\nSteps : \n\t{1}\n\nLayers : \n\t{2}\n",
                //       getOdbDirTgzFn, getSteps, getLyrs));
                Show_StepLayers(getOdbDirTgzFn, getSteps, getLyrs);
            }
            else
            {
                MessageBox.Show(string.Format(" Fail! '{0}'", getOdbDirTgzFn));
            }
        }

        private void miGet_StepsLayers_ODB_Dialog_Click(object sender, EventArgs e)
        {
            TOdbStepListType odbStpTp = TOdbStepListType.osAllSteps;
            if (!Get_OdbStepListType(ref odbStpTp))
                return;


            string getOdbDirTgzFn = " ", getSteps = " ", getLyrs = " ";

            if (ClsRvCamDLL.CS_RvCam_Get_StepsLayers_ODB_Dialog(
                ref getOdbDirTgzFn, ref getSteps, ref getLyrs, TOdbTgzType.otOdbFolder, odbStpTp))
            {
                //MessageBox.Show(string.Format("OdbTgz : \n\t{0}\n\nSteps : \n\t{1}\n\nLayers : \n\t{2}\n",
                //       getOdbDirTgzFn, getSteps, getLyrs));
                Show_StepLayers(getOdbDirTgzFn, getSteps, getLyrs);
            }
            else
            {
                MessageBox.Show(string.Format(" Fail! '{0}'", getOdbDirTgzFn));
            }
        }

        private void miGetStepLayers_Tgz_Click(object sender, EventArgs e)
        {
            TOdbStepListType odbStpTp = TOdbStepListType.osAllSteps;
            if (!Get_OdbStepListType(ref odbStpTp))
                return;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory =
                   // Directory.GetCurrentDirectory(); // "c:\\";
                   Path.GetDirectoryName(ClsM2dTypeDefineVabiable.mLastFile);
                openFileDialog.FileName = ClsM2dTypeDefineVabiable.mLastFile;

                openFileDialog.Filter = "tgz files (*.tgz)|*.tgz|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    ClsM2dTypeDefineVabiable.mLastFile = openFileDialog.FileName;

                    //Get the path of specified file
                    string fnTgz = openFileDialog.FileName;
                    string getSteps = " ", getLyrs = " ";

                    if (ClsRvCamDLL.CS_RvCam_Get_StepsLayers_ODB(
                        fnTgz, ref getSteps, ref getLyrs, odbStpTp))
                    {
                        //MessageBox.Show(string.Format("OdbTgz : \n\t{0}\n\nSteps : \n\t{1}\n\nLayers : \n\t{2}\n",
                        //       fnTGZ, getSteps, getLyrs));
                        Show_StepLayers(fnTgz, getSteps, getLyrs);
                    }
                    else
                    {
                        MessageBox.Show(string.Format(" Fail! '{0}'", fnTgz));
                    }
                }
            }
        }

        private void miGetStepLayersODB_Click(object sender, EventArgs e)
        {
            TOdbStepListType odbStpTp = TOdbStepListType.osAllSteps;
            if (!Get_OdbStepListType(ref odbStpTp))
                return;

            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                fbd.InitialDirectory = ClsM2dTypeDefineVabiable.mLastDir;

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    ClsM2dTypeDefineVabiable.mLastDir = fbd.SelectedPath;

                    //string[] files = Directory.GetFiles(fbd.SelectedPath);
                    string filePath = fbd.SelectedPath;
                    string getSteps = " ", getLyrs = " ";

                    if (ClsRvCamDLL.CS_RvCam_Get_StepsLayers_ODB(
                        filePath, ref getSteps, ref getLyrs, odbStpTp))
                    {
                        //MessageBox.Show(string.Format("OdbTgz : \n\t{0}\n\nSteps : \n\t{1}\n\nLayers : \n\t{2}\n",
                        //       fnTGZ, getSteps, getLyrs));
                        Show_StepLayers(filePath, getSteps, getLyrs);
                    }
                    else
                    {
                        MessageBox.Show(string.Format(" Fail! '{0}'", filePath));
                    }

                    //System.Windows.Forms.MessageBox.Show("Files found: " + files.Length.ToString(), "Message");
                }
            }
        }

        private void miGetStepLayersODBDialog_Click(object sender, EventArgs e)
        {

            string getSteps = " ", getLyrs = " ";

            if (ClsRvCamDLL.CS_RvCam_Get_StepsLayers_CurrentData(
                  ref getSteps, ref getLyrs))
            {
                //MessageBox.Show(string.Format("Current : \n\t{0}\n\nSteps : \n\t{1}\n\nLayers : \n\t{2}\n",
                //       "", getSteps, getLyrs));
                Show_StepLayers("", getSteps, getLyrs);
            }
            else
            {
                MessageBox.Show(string.Format(" Fail! '{0}'", ""));
            }
        }

        private void miClearCurrentData_Click(object sender, EventArgs e)
        {
            if (ClsRvCamDLL.CS_RvCam_Clear_CurrentData())
            {
                Initial_Interfaces_StepLayers();

                // 更新介面
                MessageBox.Show(string.Format("Success. '{0}'", ""));
            }
            else
            {
                MessageBox.Show(string.Format(" Fail! '{0}'", ""));
            }
        }

        private void miCearUnZipBuffer_Click(object sender, EventArgs e)
        {
            if (ClsRvCamDLL.CS_RvCam_Clear_UnZipBuffer())
            {
                //MessageBox.Show(string.Format("Success. '{0}'", ""));
            }
            else
            {
                MessageBox.Show(string.Format(" Fail! '{0}'", ""));
            }
        }

        private void LoadOdbTGZDialogClick(object sender, EventArgs e)
        {

            string sLoadOnlyStps = "", sLoadOnlyLyrs = "";


            #region 詢問是否輸入 指定的 Steps 和 Layers 就好 =========================================   
#if _CSharpInputBox
            string sInput = "";
            Get_InputString(ref sInput, string.Format("Enter Load Only Step Names. eg '{0}' ",
                    "'pcb,array,panel"),
                "Input Load Only Steps");

            sInput = "";
            Get_InputString(
                string.Format(ref sInput, "Enter Load Only Layer Names. eg '{0}' ",
                        "'comp, l2, pth"),
                    "Input Load Only Layers");
#else
            string str_sTitle = "Input Step/Layer Names";
            string[] str_sPrompts = ["Step Names", "Layer Names"],
                     str_getValues = ["", ""];

            if (ClsRvCamDLL.CS_RvCam_Dialog_MultiInputBox(str_sTitle, str_sPrompts, ref str_getValues))
            {
                sLoadOnlyStps = str_getValues.Length > 0 ? str_getValues[0] : "";
                sLoadOnlyLyrs = str_getValues.Length > 1 ? str_getValues[1] : "";
            }
#endif
            #endregion

            // 讀入有排版繼承關係的 Steps就好---------------------------- 
            TOdbStepListType odbStpTp = TOdbStepListType.osInheritedStepsOnly;

            string str_getOdbDir_TGZFile = "";
            string str_getSteps = "";
            string str_getLayers = "";
            TOdbTgzType loadOdbTgzTp = TOdbTgzType.otOdbFolder;

            // 指定輸入 ODB++ 資料夾 或者 TGZ 檔案 ---------------------------------
            loadOdbTgzTp = (TOdbTgzType)int.Parse((sender as ToolStripMenuItem).Tag.ToString());

            //C lsRvCamDLL.CS_RvCam_Load_ODB() 無介面讀檔函數

            if (ClsRvCamDLL.CS_RvCam_Load_ODB_Dialog(
                ref str_getOdbDir_TGZFile,
                ref str_getSteps,
                ref str_getLayers,
                loadOdbTgzTp,
                odbStpTp,
                sLoadOnlyStps,
                sLoadOnlyLyrs)
                )
            {
                Initial_Interfaces_StepLayers();

                //MessageBox.Show(string.Format("Success. '{0}'", ""));
                Show_StepLayers(str_getOdbDir_TGZFile, str_getSteps, str_getLayers);
            }
            else
            {
                MessageBox.Show(string.Format(" Fail! '{0}'", ""));
            }
        }

        private void tbLayers_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActLayer = ((TabControl)sender).SelectedIndex;
        }

        private void ckLstBxLayers_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActLayer = ((CheckedListBox)sender).SelectedIndex;
        }

        private void tbSteps_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActStep = ((TabControl)sender).SelectedIndex;
        }

        private void pbMain_Paint(object sender, PaintEventArgs e)
        {

            // 將 bmp 畫到 pixtureBox1-------------------------
            //e.Graphics.DrawImage(
            //    PaintDummyBmp,
            //    0, 0, PaintDummyBmp.Width, PaintDummyBmp.Height);
        }

        private void miPaintCamData_Click(object sender, EventArgs e)
        {
            Bitmap bmp = new Bitmap(pbMain.Width, pbMain.Height);

            try
            {
                if (true == ClsRvCamDLL.Paint_CamToBitmap(ActStep, [ActLayer],
                            [ClsRvCamDLL.Get_LayerColor(FActLayer)],
                            cCanvasID_pbMain,
                            ref bmp,
                            pbMain.Width, pbMain.Height,
                            PixelFormat.Format32bppArgb))
                {
                    //ClsRvCamDLL.PaintDummyBmp.Save(....)
                }
            }
            finally
            {
                bmp.Dispose();
            }


        }

        private void tbLayers_DrawItem(object sender, DrawItemEventArgs e)
        {
            //將  tbLayers.DrawMode = OwnerDrawFixed;

            e.Graphics.FillRectangle(new SolidBrush(
                    ClsRvCamDLL.Get_LayerColor(e.Index)), e.Bounds);

            // Then draw the current tab button text 
            Rectangle paddedBounds = e.Bounds;
            paddedBounds.Inflate(-2, -2);
            e.Graphics.DrawString(tbLayers.TabPages[e.Index].Text, this.Font, SystemBrushes.HighlightText, paddedBounds);

        }

        private void tbSteps_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.Graphics.FillRectangle(new SolidBrush(
                    ClsRvCamDLL.Get_StepColor(e.Index)), e.Bounds);

            // Then draw the current tab button text 
            Rectangle paddedBounds = e.Bounds;
            paddedBounds.Inflate(-2, -2);
            e.Graphics.DrawString(tbSteps.TabPages[e.Index].Text, this.Font, SystemBrushes.HighlightText, paddedBounds);
        }

        private void pbSub_Paint(object sender, PaintEventArgs e)
        {
            // 在 pbSub 上劃出 View MinMax 的框框----------------------------
            double vMinX = 0.0, vMinY = 0.0, vMaxX = 0.0, vMaxY = 0.0;
            if (ClsRvCamDLL.Get_ViewMinMax(cCanvasID_pbMain, ref vMinX, ref vMinY, ref vMaxX, ref vMaxY))
            {
                int aLeft = 0, aTop = 0, aRight = 0, aBottom = 0;
                ClsRvCamDLL.Convert_Cam_To_ImageMinMax(cCanvasID_pbSub, vMinX, vMinY, vMaxX, vMaxY,
                    ref aLeft, ref aTop, ref aRight, ref aBottom);

                ClsRvCamDLL.Paint_Rectangle(ref pbSub, aLeft, aTop, aRight, aBottom,
                   Color.DarkRed, 5);
            }
        }

        private void ViewModeClick(object sender, EventArgs? e)
        {

            TViewMode viewMode = (TViewMode)int.Parse((sender as ToolStripMenuItem).Tag.ToString());

            ClsRvCamDLL.UpdateView_And_PaintToPictureBox(
                cCanvasID_pbMain, pbMain,
                viewMode,
                FActStep,
                CheckedLayers, //[FActLayer],
                CheckedLayerColors, //[ClsRvCamDLL.Get_LayerColor(FActLayer)],
                FVectPaintMode
                );

            pbMain.Refresh();
        }

        private void tbSteps_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            int stepID = ((TabControl)sender).SelectedIndex;

            ClsRvCamDLL.CS_RvCam_View_Update(cCanvasID_pbMain, pbMain.Width, pbMain.Height, stepID, ActLayer, TViewMode.vmHome);

            ActStep = stepID;
        }

        private void pbSub_SizeChanged(object sender, EventArgs e)
        {
            ClsRvCamDLL.UpdateView_And_PaintToPictureBox(
                cCanvasID_pbSub, pbSub,
                TViewMode.vmCanvasSizeChanged,
                FActStep,
                [FActLayer],
                [ClsRvCamDLL.Get_LayerColor(FActLayer)],
                TVectPaintMode.pmSolid_Normal
                );
            pbSub.Refresh();
        }

        private void pbMain_SizeChanged(object sender, EventArgs e)
        {
            ClsRvCamDLL.UpdateView_And_PaintToPictureBox(
                cCanvasID_pbMain, pbMain,
                TViewMode.vmCanvasSizeChanged,
                FActStep,
                CheckedLayers, //[FActLayer],
                CheckedLayerColors, //[ClsRvCamDLL.Get_LayerColor(FActLayer)],
                FVectPaintMode
                );

            pbMain.Refresh();
        }

        private void panel5_Paint(object sender, PaintEventArgs e)
        {

        }

        private void ckbxSelLyrs_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void ckbxSelLyrs_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < ckLstBxLayers.Items.Count; i++)
            {
                ckLstBxLayers.SetItemChecked(i, ckbxSelLyrs.Checked);

            }

            ActLayer = FActLayer;

        }

        private void pbMain_MouseDown(object sender, MouseEventArgs e)
        {
            FMouseDownX = e.X;
            FMouseDownY = e.Y;


            if (e.Button == MouseButtons.Right) // Pan View
            {
                ClsRvCamDLL.CS_RvCam_View_Store(cCanvasID_pbMain);
            }
        }

        private void pbMain_MouseMove(object sender, MouseEventArgs e)
        {
            int imgX = Convert.ToInt32(e.X),
                imgY = Convert.ToInt32(e.Y);

            double camX = 0.0,
                   camY = 0.0;

            ClsRvCamDLL.CS_RvCam_Get_ImageToCamXY(cCanvasID_pbMain,
                    imgX, imgY, ref camX, ref camY);

            tslInfo0.Text =
                string.Format("Img({0},{1}), Cam({2},{3}) mm",
                    //e.X.ToString("#0.0"), e.Y.ToString("#0.0"),
                    imgX, imgY,
                    camX.ToString("#0.###"),
                    camY.ToString("#0.###"));


            if (e.Button == MouseButtons.Left)  // Frame View (Draw Rectangle)
            {
                pbMain.Refresh();
                ClsRvCamDLL.Paint_Rectangle(ref pbMain, e.X, e.Y, FMouseDownX, FMouseDownY, Color.DarkRed);
            }
            else if (e.Button == MouseButtons.Right) // Pan View
            {
                ClsRvCamDLL.UpdateView_Pan(ActStep, ActLayer, cCanvasID_pbMain, FMouseDownX, FMouseDownY, e.X, e.Y);


                Update_Paint_PictureBoxA(cCanvasID_pbMain, pbMain, FVectPaintMode);

                pbSub.Refresh(); // 更新 pbSub viewMinMax 框框繪圖
            }
        }

        private void pbMain_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)  // Frame View Update/Paint
            {
                int dnImgX = Convert.ToInt32(FMouseDownX),
                    dnImgY = Convert.ToInt32(FMouseDownY),
                    upImgX = Convert.ToInt32(e.X),
                    upImgY = Convert.ToInt32(e.Y);
                double atCXmm = 0.0, atCYmm = 0.0, selWmm = 0.0, selHmm = 0.0;

                switch (EditMode)
                {
                    case TEditMode.emNone:
                        if (ClsRvCamDLL.UpdateView_ViewMinMax(
                            ActStep, ActLayer,
                            cCanvasID_pbMain, cCanvasID_pbMain,
                            dnImgX, dnImgY, upImgX, upImgY))
                        {

                            Update_Paint_PictureBoxA(cCanvasID_pbMain, pbMain, FVectPaintMode);

                            pbSub.Refresh(); // 更新 pbSub viewMinMax 框框繪圖
                        }
                        break;
                    case TEditMode.emSelect:

                        ClsRvCamDLL.Convert_ImageMinMax_To_CamCxyWidthHeight(cCanvasID_pbMain,
                                dnImgX, dnImgY, upImgX, upImgY,
                                ref atCXmm, ref atCYmm, ref selWmm, ref selHmm);

                        int selObjCount = 0;

                        if (ClsRvCamDLL.CS_RvCam_Select_Objects(ActStep, ActLayer, atCXmm, atCYmm, selWmm, selHmm,
                                ref selObjCount,
                                ClsRvCamDLL.SelectAction, ActionTarget))
                            ;

                        slLogInfo.Text = string.Format("Sel. Objs: {0}", selObjCount);

                        Update_Paint_PictureBoxA(cCanvasID_pbMain, pbMain, FVectPaintMode);

                        break;
                    case TEditMode.emQuery:
                        if (ClsRvCamDLL.CS_RvCam_Get_ImageToCamXY(cCanvasID_pbMain,
                                upImgX, upImgY,
                                ref atCXmm, ref atCYmm))
                        {
                            double searchTolmm = 1.0;

                            string sGetObjInfo = "";
                            if (ClsRvCamDLL.CS_RvCam_Query_ObjectInfo(ActStep, ActLayer, atCXmm, atCYmm, searchTolmm,
                                    ref sGetObjInfo))
                            {
                                Update_Paint_PictureBoxA(cCanvasID_pbMain, pbMain, FVectPaintMode);

                                slLogInfo.Text = sGetObjInfo;
                                string sInfo = sGetObjInfo.Replace(':', '\n');
                                MessageBox.Show(sInfo);
                            }
                        }
                        break;
                }

            }

            (sender as PictureBox).Refresh();
        }

        private void pbSub_MouseMove(object sender, MouseEventArgs e)
        {
            int imgX = Convert.ToInt32(e.X),
                imgY = Convert.ToInt32(e.Y);

            double camX = 0.0,
                   camY = 0.0;


            if (ClsRvCamDLL.CS_RvCam_Get_ImageToCamXY(cCanvasID_pbSub,
                    imgX, imgY, ref camX, ref camY))
            {
                tslInfo0.Text =
                    string.Format("Img({0},{1}), Cam({2},{3}) mm",
                        //e.X.ToString("#0.0"), e.Y.ToString("#0.0"),
                        imgX, imgY,
                        camX.ToString("#0.###"),
                        camY.ToString("#0.###"));
            }

            if (e.Button == MouseButtons.Left)
            {
                pbSub.Refresh();
                ClsRvCamDLL.Paint_Rectangle(ref pbSub, FMouseDownX, FMouseDownY, e.X, e.Y, Color.DarkRed);
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (ClsRvCamDLL.CS_RvCam_View_Update(cCanvasID_pbMain,
                     camX, camY, ActStep, ActLayer, TViewMode.vmViewAtXY))
                {
                    Update_Paint_PictureBoxA(cCanvasID_pbMain, pbMain, FVectPaintMode);

                    pbSub.Refresh(); // 更新 pbSub viewMinMax 框框繪圖
                }
            }

        }

        private void pbSub_MouseDown(object sender, MouseEventArgs e)
        {

            FMouseDownX = e.X;
            FMouseDownY = e.Y;
        }

        private void pbSub_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int dnImgX = Convert.ToInt32(FMouseDownX),
                    dnImgY = Convert.ToInt32(FMouseDownY),
                    upImgX = Convert.ToInt32(e.X),
                    upImgY = Convert.ToInt32(e.Y);

                if (ClsRvCamDLL.UpdateView_ViewMinMax(
                        ActStep, ActLayer,
                        cCanvasID_pbSub, cCanvasID_pbMain,
                        dnImgX, dnImgY, upImgX, upImgY))
                {
                    Update_Paint_PictureBoxA(cCanvasID_pbMain, pbMain, FVectPaintMode);
                }
            }

            pbSub.Refresh();
        }

        private void pbMain_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                ClsRvCamDLL.CS_RvCam_View_Update(cCanvasID_pbMain, pbMain.Width, pbMain.Height, ActStep, ActLayer, TViewMode.vmHome);
                Update_Paint_PictureBoxA(cCanvasID_pbMain, pbMain, FVectPaintMode);

            }
        }

        private void miLoadCadDialog_Click(object sender, EventArgs e)
        {
            string getCadFn = "",
                   getLoadToStpName = "",
                   getLoadToLyrName = "",
                   sSetLoadStep = (ActStep >= 0 && ActStep < FStepNames.Length) ? FStepNames[ActStep] : "";
            bool blClearCurData = false;


            //C lsRvCamDLL.CS_RvCam_Load_CAD() 無介面讀檔函數

            TVectFileType getFileTp = TVectFileType.vtUnknown;

            if (ClsRvCamDLL.CS_RvCam_Load_CAD_Dialog(ref getCadFn,
                    ref getLoadToStpName, ref getLoadToLyrName,
                    ref getFileTp,
                    sSetLoadStep, blClearCurData))
            {

                this.Initial_Interfaces_StepLayers();

                ActLayer = FLayerNames.Length - 1;
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void miCamToImageXY_Click(object sender, EventArgs e)
        {

            string sCamX = "1.0", sCamY = "1.0";

            if (Get_InputString(ref sCamX, string.Format("Enter CamX ({0}) ", "Mm"), "Input Cam X(mm)") &&
                Get_InputString(ref sCamY, string.Format("Enter CamY ({0}) ", "Mm"), "Input Cam Y(mm)"))
            {
                int imgX = 0, imgY = 0;
                double camX = Convert.ToDouble(sCamX), camY = Convert.ToDouble(sCamY);
                if (ClsRvCamDLL.CS_RvCam_Get_CamToImageXY(cCanvasID_pbMain,
                     camX, camY, ref imgX, ref imgY))
                {
                    MessageBox.Show(
                        string.Format(" CamXY({0},{1}) ==> ImageXY({2},{3})",
                           camX, camY, imgX, imgY)
                        );
                }
            }
        }

        private void miImageToCamXY_Click(object sender, EventArgs e)
        {

            string sImgX = "10", sImgY = "10";

            if (Get_InputString(ref sImgX, string.Format("Enter ImageX ({0}) ", "Pixel"), "Input ImageX(Pixel)") &&
                Get_InputString(ref sImgY, string.Format("Enter ImageY ({0}) ", "Pixel"), "Input ImageY(Pixel)"))
            {
                int imgX = Convert.ToInt32(sImgX), imgY = Convert.ToInt32(sImgY);
                double camX = 0.0, camY = 0.0;
                if (ClsRvCamDLL.CS_RvCam_Get_ImageToCamXY(cCanvasID_pbMain,
                     imgX, imgY, ref camX, ref camY))
                {
                    MessageBox.Show(
                        string.Format(" ImageXY({0},{1}) ==> CamXY({2},{3})",
                           imgX, imgY, camX, camY)
                        );
                }
            }
        }

        private void miGetViewInfo_Click(object sender, EventArgs e)
        {

            double viewCXmm = 0.0, viewCYmm = 0.0, viewWmm = 0.0, viewHmm = 0.0, viewMmPerPxl = 0.0, viewDeg = 0.0;
            bool viewMrX = false;

            if (CS_RvCam_Get_ViewInfo(cCanvasID_pbMain, ref viewCXmm, ref viewCYmm,
                ref viewWmm, ref viewHmm, ref viewMmPerPxl, ref viewDeg, ref viewMrX,
                false))
            {
                MessageBox.Show(
                    string.Format("Unit (mm)\nViewCXY : ({0}, {1})\nViewWH : ({2}, {3})\nViewResolution : {4} mm/pxl\n" +
                        "viewDegree : {5}\nViewMirrorX : {6}\n",
                        viewCXmm.ToString("#0.###"), viewCYmm.ToString("#0.###"),
                        viewWmm.ToString("#0.###"), viewHmm.ToString("#0.###"), viewMmPerPxl.ToString("#0.###"),
                        viewDeg.ToString("#0.#"), viewMrX)
                    );
            }
        }

        private void vmDegreeMirrorXToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double viewDeg = 0.0;
            double mrX = 0.0;

            string sDeg = "0";

            if (Get_InputString(ref sDeg, string.Format("Enter View Degree ({0}) ", "0~359"), "Input View Degree"))
                viewDeg = Convert.ToDouble(sDeg);

            const string message = "View MirrorX?";
            const string caption = "View MirrorX";
            var result = MessageBox.Show(message, caption,
                                         MessageBoxButtons.YesNo,
                                         MessageBoxIcon.Question);

            if (result == DialogResult.Yes) mrX = 1.0;


            ClsRvCamDLL.CS_RvCam_View_Update(cCanvasID_pbMain, viewDeg, mrX, ActStep, ActLayer, TViewMode.vmDegreeMirrorX);


            ClsRvCamDLL.UpdateView_And_PaintToPictureBox(
                cCanvasID_pbMain, pbMain,
                TViewMode.vmCanvasSizeChanged,
                FActStep,
                CheckedLayers, //[FActLayer],
                CheckedLayerColors, //[ClsRvCamDLL.Get_LayerColor(FActLayer)],
                FVectPaintMode
                );

            pbMain.Refresh();
            pbSub.Refresh();
        }

        private void vmAtResolutionMmPerPxlToolStripMenuItem_Click(object sender, EventArgs e)
        {

            double viewCXmm = 0.0, viewCYmm = 0.0, viewWmm = 0.0, viewHmm = 0.0, viewMmPerPxl = 0.0, viewDeg = 0.0;
            bool viewMrX = false;

            if (CS_RvCam_Get_ViewInfo(cCanvasID_pbMain, ref viewCXmm, ref viewCYmm,
                ref viewWmm, ref viewHmm, ref viewMmPerPxl, ref viewDeg, ref viewMrX,
                false))
                ;

            string sMmPerPxl = viewMmPerPxl.ToString();


            if (Get_InputString(ref sMmPerPxl,
                    string.Format("Enter View Resolution ({0}) ", "mm/pixel"),
                 "Input View Resolution"))
            {
                double mmPerPxl = Convert.ToDouble(sMmPerPxl);

                if (ClsRvCamDLL.CS_RvCam_View_Update(cCanvasID_pbMain, mmPerPxl, 0.0,
                    ActStep, ActLayer, TViewMode.vmViewAtMmPerPixel))
                {
                    Update_Paint_PictureBoxA(cCanvasID_pbMain, pbMain, FVectPaintMode);
                }
            }
        }

        private void miSaveCADFileDialg_Click(object sender, EventArgs e)
        {
            string getSaveCadFn = "",
                   saveStpName = (ActStep >= 0 && ActStep < FStepNames.Length) ? FStepNames[ActStep] : "",
                   saveLyrName = (ActLayer >= 0 && ActLayer < FLayerNames.Length) ? FLayerNames[ActLayer] : "";

            //C lsRvCamDLL.CS_RvCam_Save_CAD_Auto() 無介面存檔函數

            if (ClsRvCamDLL.CS_RvCam_Save_CAD_Dialog(
                    ref getSaveCadFn,
                    saveStpName, saveLyrName,
                    Get_TVectFileType()))
            {
                MessageBox.Show(
                    string.Format("'{0}' Saved Succes.", getSaveCadFn));
            }
            else
            {
                MessageBox.Show("Save File Failed!");
            }
        }

        private void TVectPaintMode_Click(object sender, EventArgs e)
        {
            VectPaintMode = (TVectPaintMode)int.Parse((sender as ToolStripMenuItem).Tag.ToString());
        }

        private void miAllObjectsVisible_Click(object sender, EventArgs e)
        {
            FVisible_ChildSteps = true;
            FVisible_Pads = true;
            FVisible_Lines = true;
            FVisible_Polygons = true;

            ClsRvCamDLL.CS_RvCam_Set_Visible_PaintObjects(
                FVisible_ChildSteps, FVisible_Pads, FVisible_Lines, FVisible_Polygons);
            // 繪圖------------------------------------------------------------
            Update_Paint_PictureBoxA(cCanvasID_pbMain, pbMain, FVectPaintMode);
        }

        private void miPaintObjectsVisible_Click(object sender, EventArgs e)
        {
            int tagID = int.Parse((sender as ToolStripMenuItem).Tag.ToString());

            switch (tagID)
            {
                case 0: FVisible_ChildSteps = !FVisible_ChildSteps; break;
                case 1: FVisible_Pads = !FVisible_Pads; break;
                case 2: FVisible_Lines = !FVisible_Lines; break;
                case 3: FVisible_Polygons = !FVisible_Polygons; break;
                default: break;
            }

            ClsRvCamDLL.CS_RvCam_Set_Visible_PaintObjects(
                FVisible_ChildSteps, FVisible_Pads, FVisible_Lines, FVisible_Polygons);
            // 繪圖------------------------------------------------------------
            Update_Paint_PictureBoxA(cCanvasID_pbMain, pbMain, FVectPaintMode);
        }

        private void miRvCam_IsEmpty_Layer_Click(object sender, EventArgs e)
        {
            bool isEmpty = true;
            ClsRvCamDLL.CS_RvCam_Query_IsEmpty_StepLayer(ActStep, ActLayer, ref isEmpty);

            if (isEmpty)
                MessageBox.Show(
                        string.Format("Step({0}) / Layer({1}) is Empty.", ActStep, ActLayer));
            else
                MessageBox.Show(
                        string.Format("Step({0}) / Layer({1}) is not Empty.", ActStep, ActLayer));
        }

        private void miGetObjCount_Click(object sender, EventArgs e)
        {
            int padCount = 0, lineCount = 0, arcCount = 0;

            ClsRvCamDLL.CS_RvCam_Get_ObjectsCount(ActStep, ActLayer, ref padCount, ref lineCount, ref arcCount);

            MessageBox.Show(
                string.Format("Pads : {0}\nLines : {1}\nArc : {2}\n",
                    padCount, lineCount, arcCount)
                );
        }

        private void TVPRenderColor_Click(object sender, EventArgs e)
        {
            int tagID = int.Parse((sender as ToolStripMenuItem).Tag.ToString());

            ClsRvCamDLL.CS_RvCam_Set_RenderColor((TVPRenderColor)tagID);
            // 繪圖------------------------------------------------------------
            Update_Paint_PictureBoxA(cCanvasID_pbMain, pbMain, FVectPaintMode);
        }

        private void miSetLayerColor_Click(object sender, EventArgs e)
        {
            Color getColor = Get_LayerColor(ActLayer);
            if (ClsRvCamDLL.CS_RvCam_Get_Color(ref getColor))
            {
                ClsRvCamDLL.Set_LayerColor(ActLayer, getColor);

                ActLayer = ActLayer;

                tbLayers.Refresh(); // .TabPages[ActLayer].Refresh();

            }
        }

        private void miSetStepColor_Click(object sender, EventArgs e)
        {
            Color getColor = Get_LayerColor(ActLayer);
            if (ClsRvCamDLL.CS_RvCam_Get_Color(ref getColor))
            {
                ClsRvCamDLL.Set_StepColor(ActLayer, getColor);

                ActLayer = ActLayer;

                tbSteps.Refresh(); // .TabPages[ActLayer].Refresh();
            }
        }

        private void miConvertOdbToCadFile_Click(object sender, EventArgs e)
        {
            string loadOdbDir = "";

            #region 選取輸入的 ODB dir
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.InitialDirectory = ClsM2dTypeDefineVabiable.mLastDir;
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    ClsM2dTypeDefineVabiable.mLastDir = fbd.SelectedPath;
                    loadOdbDir = fbd.SelectedPath;
                    //string[] files = Directory.GetFiles(fbd.SelectedPath);
                    //System.Windows.Forms.MessageBox.Show("Files found: " + files.Length.ToString(), "Message");
                }
            }
            #endregion


            #region 詢問是否輸入 指定的 Steps 和 Layers 就好 =========================================  

            string sLoadOnlyStps = "", sLoadOnlyLyrs = "";

            sLoadOnlyStps = "pcb";
            sLoadOnlyLyrs = "top,layer_2";

            Get_InputString(ref sLoadOnlyStps, string.Format("Enter Load Only Step Names. eg '{0}' ",
                    "'pcb,array,panel"),
                "Input Load Only Steps");
            Get_InputString(ref sLoadOnlyLyrs, string.Format("Enter Load Only Layer Names. eg '{0}' ",
                        "'comp, l2, pth"),
                    "Input Load Only Layers");
            #endregion

            if ("" == sLoadOnlyStps || "" == sLoadOnlyLyrs) return;

            string sOutputDir = ClsM2dTypeDefineVabiable.mLastSaveDir;

            #region 選取輸出路徑
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.InitialDirectory = ClsM2dTypeDefineVabiable.mLastSaveDir;
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    sOutputDir = fbd.SelectedPath;
                    ClsM2dTypeDefineVabiable.mLastSaveDir = sOutputDir;

                    //string[] files = Directory.GetFiles(fbd.SelectedPath);
                    //System.Windows.Forms.MessageBox.Show("Files found: " + files.Length.ToString(), "Message");
                }
            }
            #endregion
            if (!Directory.Exists(sOutputDir)) return;

            string getSavedFileNames = "";

            if (ClsRvCamDLL.CS_Rvcam_Convert_File_OdbTGZ_To_CAD(loadOdbDir, sLoadOnlyStps, sLoadOnlyLyrs,
                sOutputDir, Get_TVectFileType(), ref getSavedFileNames))
            {
                MessageBox.Show(string.Format(" File Saved.\n\n\tOutput Dir: {0}\n\n\tOuput Files: '{1}'",
                    sOutputDir, getSavedFileNames));
            }
            else
            {
                MessageBox.Show(string.Format(" Fail! '{0}'", "Convert File"));
            }
        }

        private void miConvertTGZToCadFiles_Click(object sender, EventArgs e)
        {
            //This will give us the full name path of the executable file:
            //i.e. C:\Program Files\MyApplication\MyApplication.exe
            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            //This will strip just the working path name:
            //C:\Program Files\MyApplication
            string strWorkPath = System.IO.Path.GetDirectoryName(strExeFilePath),
                    fnTgz = ClsM2dTypeDefineVabiable.mLastFile;

#if DEBUG
            string strSampleTgzDir = strWorkPath + Path.DirectorySeparatorChar
                    + "SampleFiles" + Path.DirectorySeparatorChar;

            fnTgz = strSampleTgzDir + "84014776001_p1_odbjob.tgz";

#else
            
#endif
            //if (!File.Exists(strSampleTgzFn)) return;


            #region 詢問是否輸入 指定的 Steps 和 Layers 就好 =========================================  

            string sLoadOnlyStps = "", sLoadOnlyLyrs = "";

            sLoadOnlyStps = "pcb";
            if (Get_InputString(ref sLoadOnlyStps,
                    string.Format("Enter Load Only Step Names. eg '{0}' ",
                    "'pcb,array,panel"),
                    "Input Load Only Steps"))
            {
                sLoadOnlyLyrs = "top,layer_2";
                if (Get_InputString(ref sLoadOnlyLyrs,
                    string.Format("Enter Load Only Layer Names. eg '{0}' ", "'comp, l2, pth"),
                    "Input Load Only Layers"))
                    ;
            }
            #endregion

            if ("" == sLoadOnlyStps || "" == sLoadOnlyLyrs) return;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory =
                   // Directory.GetCurrentDirectory(); // "c:\\";
                   Path.GetDirectoryName(fnTgz);
                openFileDialog.FileName = fnTgz;

                openFileDialog.Filter = "tgz files (*.tgz)|*.tgz|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    ClsM2dTypeDefineVabiable.mLastFile = openFileDialog.FileName;

                    fnTgz = openFileDialog.FileName;
                    string getSavedFileNames = "";
                    string sOutputDir =
                         Path.GetDirectoryName(fnTgz) + Path.DirectorySeparatorChar +
                         "OutputCadFiles" + Path.DirectorySeparatorChar;

                    Directory.CreateDirectory(sOutputDir);

                    #region 選取輸出路徑
                    using (var fbd = new FolderBrowserDialog())
                    {
                        fbd.InitialDirectory = sOutputDir;
                        DialogResult result = fbd.ShowDialog();

                        if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                        {
                            sOutputDir = fbd.SelectedPath;

                            //string[] files = Directory.GetFiles(fbd.SelectedPath);
                            //System.Windows.Forms.MessageBox.Show("Files found: " + files.Length.ToString(), "Message");
                        }
                    }
                    #endregion
                    if (!Directory.Exists(sOutputDir)) return;


                    if (ClsRvCamDLL.CS_Rvcam_Convert_File_OdbTGZ_To_CAD(fnTgz, sLoadOnlyStps, sLoadOnlyLyrs,
                        sOutputDir, Get_TVectFileType(), ref getSavedFileNames))
                    {
                        ClsM2dTypeDefineVabiable.mLastSaveDir = sOutputDir;

                        MessageBox.Show(string.Format(" File Saved.\n\n\tOutput Dir: {0}\n\n\tOuput Files: '{1}'",
                            sOutputDir, getSavedFileNames));
                    }
                    else
                    {
                        MessageBox.Show(string.Format(" Fail! '{0}'", "Convert File"));
                    }
                }
            }
        }

        private void miCADToCADFiles_Click(object sender, EventArgs e)
        {

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Multiselect = true;
                openFileDialog.InitialDirectory =
                   // Directory.GetCurrentDirectory(); // "c:\\";
                   Path.GetDirectoryName(ClsM2dTypeDefineVabiable.mLastFile);
                openFileDialog.FileName = ClsM2dTypeDefineVabiable.mLastFile;

                openFileDialog.Filter = "All files (*.*)|*.*|Gerber274X (*.gb*)|*.gb*|AutoCAD DXF (*.dxf)|*.dxf;*.dxf";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string[] loadFnsAry = openFileDialog.FileNames; // .FileName;

                    if (loadFnsAry.Length <= 0) return;

                    ClsM2dTypeDefineVabiable.mLastFile = loadFnsAry[0];

                    string loadFns = "";
                    for (int i = 0; i < loadFnsAry.Length; i++)
                    {
                        loadFns = loadFns + loadFnsAry[i] + ',';
                    }

                    string loadFn = ClsM2dTypeDefineVabiable.mLastFile; // saveFileDialog.FileName;

                    string getSavedFileNames = "";
                    string sOutputDir = ClsM2dTypeDefineVabiable.mLastSaveDir;
                    //Path.GetDirectoryName(loadFn) + Path.DirectorySeparatorChar +
                    //"OutputCadFiles" + Path.DirectorySeparatorChar;

                    Directory.CreateDirectory(sOutputDir);

                    #region 選取輸出路徑
                    using (var fbd = new FolderBrowserDialog())
                    {
                        fbd.InitialDirectory = sOutputDir;
                        DialogResult result = fbd.ShowDialog();

                        if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                        {
                            sOutputDir = fbd.SelectedPath;

                            //string[] files = Directory.GetFiles(fbd.SelectedPath);
                            //System.Windows.Forms.MessageBox.Show("Files found: " + files.Length.ToString(), "Message");
                        }
                    }
                    #endregion
                    if (!Directory.Exists(sOutputDir)) return;

                    if (ClsRvCamDLL.CS_Rvcam_Convert_File_CAD_To_CAD(loadFns,
                        sOutputDir, Get_TVectFileType(), ref getSavedFileNames))
                    {
                        ClsM2dTypeDefineVabiable.mLastSaveDir = sOutputDir;
                        MessageBox.Show(string.Format(" File Saved.\n\n\tOutput Dir: {0}\n\n\tOuput Files: '{1}'",
                            sOutputDir, getSavedFileNames));
                    }
                    else
                    {
                        MessageBox.Show(string.Format(" Fail! '{0}'", "Convert File"));
                    }
                }
            }
        }

        private void TSelectActionClick(object sender, EventArgs e)
        {
            ClsRvCamDLL.SelectAction = (TSelectAction)int.Parse((sender as ToolStripMenuItem).Tag.ToString());

            ClsRvCamDLL.EditMode = TEditMode.emSelect;

            //string[] sActTarget = [];
            //foreach (string item in Enum.GetNames(typeof(TActionTarget)))
            //{
            //    ArrayExtension.Add(ref sActTarget, item);
            //}

            //string sTitle = "Select ActionTarget";
            //bool[] blSelected = [];
            //int actID = (int)ClsRvCamDLL.ActionTarget;
            //// 選取物件類型--------------------------
            //if (ClsRvCamDLL.CS_RvCam_Dialog_ItemSelect(sTitle, sActTarget, ref blSelected, ref actID, false))
            //{
            //    ClsRvCamDLL.ActionTarget = (TActionTarget)actID;

            //}
        }

        private void miEditNone_Click(object sender, EventArgs e)
        {
            EditMode = TEditMode.emNone;
        }

        private void MainForm_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                //MessageBox.Show("Escape key pressed");

                miEditNone_Click(sender, e);

                Reset_To_Default();

                // prevent child controls from handling this event as well
                e.SuppressKeyPress = true;
            }
        }

        private void miUnDeleteAll_Click(object sender, EventArgs e)
        {

            int selObjCnt = 0;
            // 針對所有物件編輯，則將 selectWidthXmm = 0.0, selectHeightmm = 0.0
            ClsRvCamDLL.CS_RvCam_Select_Objects(ActStep, ActLayer,
                0.0, 0.0, 0.0, 0.0,
                ref selObjCnt, TSelectAction.saUnDelete,
                TActionTarget.smTVectObject);
            // 繪圖------------------------------------------------------------
            Update_Paint_PictureBoxA(cCanvasID_pbMain, pbMain, FVectPaintMode);
        }


        private void miUnFreezeAllObjs_Click(object sender, EventArgs e)
        {

            int selObjCnt = 0;
            // 針對所有物件編輯，則將 selectWidthXmm = 0.0, selectHeightmm = 0.0
            ClsRvCamDLL.CS_RvCam_Select_Objects(ActStep, ActLayer,
                0.0, 0.0, 0.0, 0.0,
                ref selObjCnt, TSelectAction.saUnFreeze,
                TActionTarget.smTVectObject);
            // 繪圖------------------------------------------------------------
            Update_Paint_PictureBoxA(cCanvasID_pbMain, pbMain, FVectPaintMode);
        }

        private void miUnSelectAllClick(object sender, EventArgs e)
        {
            int selObjCnt = 0;
            // 針對所有物件編輯，則將 selectWidthXmm = 0.0, selectHeightmm = 0.0
            ClsRvCamDLL.CS_RvCam_Select_Objects(ActStep, ActLayer,
                0.0, 0.0, 0.0, 0.0,
                ref selObjCnt, TSelectAction.saUnSelect,
                TActionTarget.smTVectObject);
            // 繪圖------------------------------------------------------------
            Update_Paint_PictureBoxA(cCanvasID_pbMain, pbMain, FVectPaintMode);

        }


        private void ActionTargetClick(object sender, EventArgs e)
        {
            ClsRvCamDLL.ActionTarget = (TActionTarget)int.Parse((sender as ToolStripMenuItem).Tag.ToString());

            //string[] sActTarget = [];
            //foreach (string item in Enum.GetNames(typeof(TActionTarget)))
            //{
            //    ArrayExtension.Add(ref sActTarget, item);
            //}
            //string sTitle = "Select ActionTarget";
            //bool[] blSelected = [];
            //int actID = (int)ClsRvCamDLL.ActionTarget;
            //// 選取物件類型--------------------------
            //if (ClsRvCamDLL.CS_RvCam_Dialog_ItemSelect(sTitle, sActTarget, ref blSelected, ref actID, false))
            //{
            //    ClsRvCamDLL.ActionTarget = (TActionTarget)actID;

            //    //ClsRvCamDLL.EditMode = TEditMode.emSelect;
            //}
        }

        private void miEditQuery_Click(object sender, EventArgs e)
        {
            ClsRvCamDLL.EditMode = TEditMode.emQuery;
        }

        private void miRvCamLoadCad1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory =
                   // Directory.GetCurrentDirectory(); // "c:\\";
                   Path.GetDirectoryName(ClsM2dTypeDefineVabiable.mLastFile);
                openFileDialog.FileName = ClsM2dTypeDefineVabiable.mLastFile;

                openFileDialog.Filter = "All files (*.*)|*.*" +
                    "|CAD files (*.gbx,*.gbr,*.dxf,*.nc,*.dpf)|*.gbx;*.gbr;*.dxf;*.nc;*.dpf" +
                    "IPC/356 (*.ipc,*.356)|*.ipc;*.356" +
                    "RasVector Cam (*.rvc)|*.rvc"
                    ;
                openFileDialog.FilterIndex = 0;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    ClsM2dTypeDefineVabiable.mLastFile = openFileDialog.FileName;

                    //Get the path of specified file
                    string fnCAD = openFileDialog.FileName;
                    string sLoadToStepName = " ", sLoadToLyrName = " ",
                            setStepName = (ActStep >= 0 && ActStep < FStepNames.Length) ? FStepNames[ActStep] : "";
                    TVectFileType fileTp = TVectFileType.vtUnknown;


                    if (ClsRvCamDLL.CS_RvCam_Load_CAD(fnCAD, ref sLoadToStepName, ref sLoadToLyrName,
                            ref fileTp, setStepName, false))
                    {

                        this.Initial_Interfaces_StepLayers();

                        ActLayer = FLayerNames.Length - 1;
                    }
                    else
                    {
                        MessageBox.Show(string.Format(" Fail! '{0}'", fnCAD));
                    }
                }
            }
        }

        private void miSaveCadFile_Click(object sender, EventArgs e)
        {

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory =
                   // Directory.GetCurrentDirectory(); // "c:\\";
                   Path.GetDirectoryName(ClsM2dTypeDefineVabiable.mLastFile);
                saveFileDialog.FileName = ClsM2dTypeDefineVabiable.mLastFile;

                saveFileDialog.Filter = "All files (*.*)|*.*" +
                    "|CAD files (*.gbx,*.gbr,*.dxf,*.nc,*.dpf)|*.gbx;*.gbr;*.dxf;*.nc;*.dpf" +
                    "IPC/356 (*.ipc,*.356)|*.ipc;*.356" +
                    "RasVector Cam (*.rvc)|*.rvc"
                    ;
                saveFileDialog.FilterIndex = 0;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    ClsM2dTypeDefineVabiable.mLastFile = saveFileDialog.FileName;

                    //Get the path of specified file
                    string fnCAD = saveFileDialog.FileName;
                    string sSaveToStepName = (ActStep >= 0 && ActStep < FStepNames.Length) ? FStepNames[ActStep] : "",
                        sSaveToLyrName = (ActLayer >= 0 && ActLayer < FLayerNames.Length) ? FLayerNames[ActLayer] : "";
                    TVectFileType fileTp = TVectFileType.vtUnknown;

                    fileTp = ClsRvCamDLL.Get_TVectFileType();

                    if (ClsRvCamDLL.CS_RvCam_Save_CAD(ref fnCAD, sSaveToStepName, sSaveToLyrName,
                            fileTp))
                    {
                        this.Initial_Interfaces_StepLayers();

                        ActLayer = FLayerNames.Length - 1;
                    }
                    else
                    {
                        MessageBox.Show(string.Format(" Fail! '{0}'", fnCAD));
                    }
                }
            }
        }

        private void miAddLayerDataClick(object sender, EventArgs e)
        {
            /*TVectSimpleShapeType
                vstNone = 0, vstArc, vstCircle, vstLine,
                vstRect, vstPolygon, vstPolyLine,
                vstSegments, vstIslandHoleShape */

            #region Create TSimpleShapes----------------------------------------
            int enumItemCount = Enum.GetNames(typeof(TVectSimpleShapeType)).Length - 1;
            TVectSimpleShape[] eShps = new TVectSimpleShape[enumItemCount];

            TFloat aRad = 5.0f, lineRad = 0.5f, shiftL = 12.0f;
            TFPoint curCXY = new TFPoint(20.0f, 20.0f);

            TVectSimpleShape nShp;

            // ArcSXY, ArcEXY, arcCXY, arcLineRad, arcOrient, lineEndType
            eShps[0] = new TVectSimpleShape(
                new TFPoint(curCXY.X, curCXY.Y - aRad), new TFPoint(curCXY.X, curCXY.Y + aRad), curCXY,
                lineRad, TRotateOrient.oCW, TLineEndType.leRound);
            curCXY.X += shiftL;

            // CircleCXY, CircleRad
            eShps[1] = new TVectSimpleShape(new TFPoint(curCXY.X, curCXY.Y), aRad);
            curCXY.X += shiftL;

            // lineSXY, lineEXY
            eShps[2] = new TVectSimpleShape(new TFPoint(curCXY.X - aRad, curCXY.Y - aRad),
                new TFPoint(curCXY.X + aRad, curCXY.Y + aRad), lineRad);
            curCXY.X += shiftL;

            // rectCXY, rectRadX, rectRadY
            eShps[3] = new TVectSimpleShape(curCXY, aRad, aRad / 2.0);
            curCXY.Y += shiftL;


            // Polygon
            eShps[4] = new TVectSimpleShape(TVectSimpleShapeType.vstPolygon,
                [new TFPoint(curCXY.X-aRad, curCXY.Y-aRad), new TFPoint(curCXY.X+aRad, curCXY.Y-aRad),
                 new TFPoint(curCXY.X+aRad, curCXY.Y+aRad), new TFPoint(curCXY.X,curCXY.Y+aRad*1.5),
                 new TFPoint(curCXY.X-aRad, curCXY.Y+aRad)], lineRad);
            curCXY.X -= shiftL;

            // PolyLine 
            eShps[5] = new TVectSimpleShape(TVectSimpleShapeType.vstPolyLine,
                [new TFPoint(curCXY.X-aRad, curCXY.Y-aRad), new TFPoint(curCXY.X+aRad, curCXY.Y-aRad),
                 new TFPoint(curCXY.X+aRad,curCXY.Y+aRad), new TFPoint(curCXY.X,curCXY.Y+aRad*1.5),
                 new TFPoint(curCXY.X-aRad,curCXY.Y+aRad)], lineRad);
            curCXY.X -= shiftL;

            // Segments
            eShps[6] = new TVectSimpleShape([
                new TFLine(curCXY.X-aRad,curCXY.Y-aRad,curCXY.X-aRad,curCXY.Y+aRad),
                new TFLine(curCXY.X+aRad,curCXY.Y-aRad,curCXY.X+aRad,curCXY.Y+aRad),
                new TFLine(curCXY.X-aRad,curCXY.Y,curCXY.X+aRad,curCXY.Y)], lineRad);
            curCXY.Y += shiftL * 2;

            // ihShapeList 左上 Shapes Collection
            //TVectSimpleShape shp = new TVectSimpleShape(eShps);
            //ClsVectManager.Shift_TVectSimpleShape(ref shp, new TFPoint(0.0f, shiftL * 2));
            //ArrayExtension.Add(ref eShps, shp);
            eShps[7] = new TVectSimpleShape(eShps);
            ClsVectManager.Shift_TVectSimpleShape(ref eShps[7], new TFPoint(0.0f, shiftL * 3.0));

            // 右上 Shapes Collection
            nShp = new TVectSimpleShape(eShps[7]);
            ArrayExtension.Add(ref eShps, nShp);
            ClsVectManager.Shift_TVectSimpleShape(ref eShps[eShps.Length - 1], new TFPoint(shiftL * 4.5f, 0.0f));

            // 右下 Shapes Collection
            nShp = new TVectSimpleShape(eShps[7]);
            ArrayExtension.Add(ref eShps, nShp);
            ClsVectManager.Shift_TVectSimpleShape(ref eShps[eShps.Length - 1], new TFPoint(shiftL * 4.5f, -shiftL * 3.0f));

            #endregion



            string sNewLayerName = "LayerNameXXXX";
            Get_InputString(ref sNewLayerName,
                string.Format("Enter New Layer Name.  ", ""),
                "Input Layer Name");

            int getToLyr = ActLayer;

            if (ClsRvCamDLL.CS_RvCam_Add_LayerData(ActStep, ref getToLyr, eShps, sNewLayerName))
            {
                this.Initial_Interfaces_StepLayers();

                ActLayer = getToLyr;
            }
        }

        private void loadTGZToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory =
                   // Directory.GetCurrentDirectory(); // "c:\\";
                   Path.GetDirectoryName(ClsM2dTypeDefineVabiable.mLastFile);
                openFileDialog.FileName = ClsM2dTypeDefineVabiable.mLastFile;

                openFileDialog.Filter = "TGZ (*.tgz)|*.tgz"
                    ;
                openFileDialog.FilterIndex = 0;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    ClsM2dTypeDefineVabiable.mLastFile = openFileDialog.FileName;

                    //Get the path of specified file
                    string fnTGZ = openFileDialog.FileName;
                    string sLoadToStepName = " ", sLoadToLyrName = " ";

                    if (ClsRvCamDLL.CS_RvCam_Load_ODB(fnTGZ, ref sLoadToStepName, ref sLoadToLyrName))
                    {
                        this.Initial_Interfaces_StepLayers();

                        ActLayer = FLayerNames.Length - 1;
                    }
                    else
                    {
                        MessageBox.Show(string.Format(" Fail! '{0}'", fnTGZ));
                    }
                }
            }
        }

        private void loadODBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                fbd.InitialDirectory = ClsM2dTypeDefineVabiable.mLastDir;

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    ClsM2dTypeDefineVabiable.mLastDir = fbd.SelectedPath;

                    //string[] files = Directory.GetFiles(fbd.SelectedPath);
                    string filePath = fbd.SelectedPath;
                    string getSteps = " ", getLyrs = " ";

                    if (ClsRvCamDLL.CS_RvCam_Load_ODB(
                        filePath, ref getSteps, ref getLyrs, false))
                    {
                        Initial_Interfaces_StepLayers();

                        Show_StepLayers(filePath, getSteps, getLyrs);
                    }
                    else
                    {
                        MessageBox.Show(string.Format(" Fail! '{0}'", filePath));
                    }

                    //System.Windows.Forms.MessageBox.Show("Files found: " + files.Length.ToString(), "Message");
                }
            }
        }

        private void miResolutionMmPerPxl2DPI_Click(object sender, EventArgs e)
        {

            TFloat frValue = 0.0f, toValue = 0.0f;

            string sInput = "1.0";
            if (Get_InputString(ref sInput,
                string.Format("Resolution Unit Convertion"),
                "Input Resolution (Mm/Pixel)"))
            {
                frValue = Convert.ToInt32(sInput);

                if (ClsRvCamDLL.CS_RvCam_Convert_Resolution_MmPerPixel_To_DPI(frValue, ref toValue))
                {
                    MessageBox.Show(string.Format(
                        "{0} Mm/Pixel = {1} DPI", frValue.ToString("#0.###"), toValue.ToString("#0.###")));
                }
            }
        }

        private void miClearLayerData0_Click(object sender, EventArgs e)
        {
            if (ClsRvCamDLL.CS_RvCam_Clear_LayerData(ActStep, ActLayer))
            {
                ActLayer = ActLayer;
            }
        }

        private void miUnitValueConvert_Click(object sender, EventArgs e)
        {
            TFloat frValue = 0.0f, toValue = 0.0f;

            string sInput = "1.0";
            if (Get_InputString(ref sInput,
                string.Format("Value Unit Convertion"),
                "Input Vaue (mm)"))
            {
                frValue = Convert.ToInt32(sInput);

                if (ClsRvCamDLL.CS_RvCam_Convert_Unit_Value(
                        TValueUnit.uMM, frValue, TValueUnit.uInch, ref toValue))
                {
                    MessageBox.Show(string.Format(
                        "{0} mm = {1} inch", frValue.ToString("#0.###"), toValue.ToString("#0.###")));
                }

            }
        }

        private void miResolutionDPI2MmPerPxl_Click(object sender, EventArgs e)
        {

            TFloat frValue = 0.0f, toValue = 0.0f;

            string sInput = "1.0";
            if (Get_InputString(ref sInput,
                string.Format("Resolution Unit Convertion"),
                "Input Resolution (DPI)"))
            {
                frValue = Convert.ToInt32(sInput);

                if (ClsRvCamDLL.CS_RvCam_Convert_Resolution_DPI_To_MmPerPixel(frValue, ref toValue))
                {
                    MessageBox.Show(string.Format(
                        "{0} DPI = {1} Mm/Pixel", frValue.ToString("#0.###"), toValue.ToString("#0.###")));
                }

            }
        }

        private void miDleteLayerData0_Click(object sender, EventArgs e)
        {
            if (ClsRvCamDLL.CS_RvCam_Delete_LayerData(ActLayer))
            {
                Initial_Interfaces_StepLayers();
            }
        }

        private void miRvCamGetLayerData0_Click(object sender, EventArgs e)
        {
            TVectSimpleShape[] getLyrData = [];

            if (ClsRvCamDLL.CS_RvCam_Get_LayerData(ActStep, ActLayer, ref getLyrData))
            {
                // 自訂處理 Layer Data---------------------------
                MessageBox.Show("RvCam_Get_LayerData( ) called.");
            }
        }

        private void miUpdateLayerData0_Click(object sender, EventArgs e)
        {
            if (ClsRvCamDLL.CS_RvCam_Update_LayerData(ActStep, ActLayer,
                  ClsRvCamDLL_PlugInDLL_FileIO.FVectShapeData))
            {
                ActLayer = ActLayer;
            }
        }

        private void miTEditMode_Click(object sender, EventArgs e)
        {

            ClsRvCamDLL.EditMode = (TEditMode)int.Parse((sender as ToolStripMenuItem).Tag.ToString());

        }

    }

#pragma warning restore CS8602 // 取消 warnning  可能 null 參考的取值 。
#pragma warning restore CS8604 // 取消 warnning  可能 null 參考的取值 。
#pragma warning restore CS0642 // 取消 warnning 
#pragma warning restore CS8600 // 取消 warnning 
#pragma warning restore CS8618 // 取消 warnning 
#pragma warning restore CS8622 // 取消 warnning 
}
