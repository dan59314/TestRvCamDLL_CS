


using RvCamDLL;
using RvLib.VectTypeDefine;
using System;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;



namespace RvLib.M2dTypeDefine
{
    using static System.Net.Mime.MediaTypeNames;
    using TFloat = double;

#pragma warning disable CS8600 // 取消 warnning 

    #region ClsM2dTypeDefineVariable =========================================
    public static class ClsM2dTypeDefineVabiable
    {
        public static string
            mLastDir =
#if DEBUG
        mLastDir = "D:\\檔案料號\\RV測試料號\\TGZ\\",
        mLastFile = "*.*",
#else
        mLastDir = Directory.GetCurrentDirectory(),
        mLastFile = ClsM2dTypeDefineVabiable.mLastDir,            
#endif
        mLastSaveFile = mLastFile,
        mLastSaveDir = mLastDir;
    }
    #endregion


    #region Generic Array Add/Delete ===================================
    public static class ArrayExtension
    {
        /*使用範例----------------------------------------------------
         * int[] array = {3, 4, 5};
           ArrayExtension.Add(ref array, 1);

           int[] array2 = { 6, 7, 8 };
           ArrayExtension.AddRange(ref array, array2);
        */

        public static T[] Add<T>(ref T[] originArray, T element)
        {
            Array.Resize(ref originArray, originArray.Length + 1);
            originArray[originArray.Length - 1] = element;
            return originArray;
        }

        public static T[] AddRange<T>(ref T[] originArray, IEnumerable<T> anotherSet)
        {
            T[] anotherArray = anotherSet.ToArray<T>();
            int originLength = originArray.Length;
            Array.Resize(ref originArray, originArray.Length + anotherArray.Length);
            anotherArray.CopyTo(originArray, originLength);
            return originArray;
        }
    }
    #endregion


    #region Delphi Equivalent ======================================
    public static class ClsDelphiEquivalent
    {
        public static bool In<T>(this T obj, IEnumerable<T> arr)
        {
            return arr.Contains(obj);
        }

        public static bool SameText(string strA, string strB)
        {
           return (0 == string.Compare(strA,strB, true));
        }
    }
    #endregion


    #region Enum Type Defione ====================================================
    public enum TReturnCode : int
    {
        rcFail = 0, rcSuccess, rcUnAuthorized,
        rcFinal
    }

    public enum TEditMode : int
    {
        emNone = 0, emSelect, emQuery, emAlignLayer, emGridCalibrate
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


    public enum TEditShapeType : int //: UInt16
    {
        esNone = 0, esPoint, esLine, esArc, esCircle,
        esRectangle, esPolygon, esObject, esDummyType
    }

    public enum TValueUnit : int
    {
        uInch = 0, uMil = 1, uCM = 2, uMM = 3, uUM = 4

    }
    
    public static class ClsValueUnit
    {
        public static string ToString(TValueUnit valUnit)
        {
            string ss = "";
            switch (valUnit)
            {
                case TValueUnit.uInch: ss = "Inch";  break;
                case TValueUnit.uMil: ss = "Mil"; break;
                case TValueUnit.uCM: ss = "CM"; break;
                case TValueUnit.uMM: ss = "MM"; break;
                case TValueUnit.uUM: ss = "UM"; break;
                default: ss = ""; break;
            }
            return ss;
        }
    }


    public enum TRotateOrient : int
    {
        oCCW = 0, oCW
    }

    public enum TLineEndType : int 
    {
        leRound=0, leSqrExtend, leSqrFlat, leShape  // 圓頭, 無延伸平頭, 延伸平頭 線段,  形狀頭
    }   
    #endregion


    #region Structure Define ============================================
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TFPoint // TSingleXY
    {
        public TFloat X;
        public TFloat Y;

        //public static implicit operator TFPoint(int initialValue)
        //{
        //    return new TFPoint() { X = initialValue, Y = initialValue };
        //}

        public TFPoint(TFloat ax, TFloat ay) { X = ax; Y = ay; }

    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct TFLine // TSingleXY
    {
        [FieldOffset(0)] public TFloat LnW;
        [FieldOffset(8)] public TFloat dummyFloat;

        [FieldOffset(16)] public TFloat SX;
        [FieldOffset(24)] public TFloat SY;
        [FieldOffset(32)] public TFloat EX;
        [FieldOffset(40)] public TFloat EY;

        [FieldOffset(16)] public TFPoint SXY;
        [FieldOffset(32)] public TFPoint EXY;

        public TFLine(TFloat sx, TFloat sy, TFloat ex, TFloat ey)
        {
            this.SX = sx;
            this.SY = sy;
            this.EX = ex;
            this.EY = ey;   
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct TFRect
    {
        //  case integer of
        //  0:(Left,Top,Right,Bottom:TFloat;);
        [FieldOffset(0)] public TFloat Left;
        [FieldOffset(8)] public TFloat Top;
        [FieldOffset(16)] public TFloat Right;
        [FieldOffset(24)] public TFloat Bottom;

        //  1:(LeftTop:TFPoint; RightBottom:TFPoint);
        [FieldOffset(0)] public TFPoint LeftTop;
        [FieldOffset(16)] public TFPoint RightBottom;

        public TFRect(TFloat aLeft, TFloat aTop, TFloat aRight, TFloat aBottom)
        {
            this.Left = aLeft;
            this.Top = aTop;
            this.Right = aRight;   
            this.Bottom = aBottom;
        }
    }


    [StructLayout(LayoutKind.Explicit, Pack =1, CharSet = CharSet.Ansi)]
    public unsafe struct TEditShape
    {
        //TVectSimpleShapeType = (esPoint=0, esLine, esArc, esCircle, esRectangle, esPolygon);
        //TVectSimpleShape :
        [FieldOffset(0)] public IntPtr esName;
        [FieldOffset(8)] public TEditShapeType esShapeTp;  //cardinal
        [FieldOffset(12)] public TFRect esMinMax;
        [FieldOffset(44)] public byte esExtraID;
        [FieldOffset(45)] public int esDummyInt;
        [FieldOffset(49)] public TFloat esDummyFloat;
        [FieldOffset(57)] public IntPtr esDummyPointer;

        //case TVectSimpleShapeType of // ROI Shape
        //esPoint:(ptXY,lineEXY:TFPoint;ptRad:TFloat);
        [FieldOffset(65)] public TFPoint ptXY;
        [FieldOffset(81)] public TFloat ptRad;

        //esLine:(lineSXY,lineEXY:TFPoint;lineRad:TFloat);
        [FieldOffset(65)] public TFPoint lineSXY;
        [FieldOffset(81)] public TFPoint lineEXY;
        [FieldOffset(97)] public TFloat lineRad;

        //esArc:(arcSXY,arcEXY,arcCXY:TFPoint;arcOrnt:TRotateOrient;arcLineRad:TFloat);
        [FieldOffset(65)] public TFPoint arcSXY;
        [FieldOffset(81)] public TFPoint arcEXY;
        [FieldOffset(97)] public TFPoint arcCXY;
        [FieldOffset(113)] public TRotateOrient arcOrnt;  //word
        [FieldOffset(117)] public TFloat arcLineRad;

        //esCircle:(cirCXY:TFPoint; cirRad:TFloat);
        [FieldOffset(65)] public TFPoint cirCXY;
        [FieldOffset(81)] public TFloat cirRad;

        //esRectangle:(rectCXY:TFPoint; rectRadX, rectRadY:TFloat);
        [FieldOffset(65)] public TFPoint rectCXY;
        [FieldOffset(81)] public TFloat rectRadX;
        [FieldOffset(89)] public TFloat rectRadY;

        //esPolygon:(fPointLst:{$IFDEF Android}Pointer{$ELSE}TFPointList{$ENDIF});
        [FieldOffset(65)] public IntPtr fPointLst;

        //esDummyType: (dummyIntegers : Array[0..20] of Integer);
        [FieldOffset(65)] public fixed int gDummyIntegers[21]; // 固定長度 array[20] + 1 byte delphi Array Head
        //[FieldOffset(65)] public IntPtr gDummyIntegers ;

        public TEditShape(TFPoint cirCxyMm, TFloat cirRadMm, string name="",
            int extraID=0, int dummyInt=0)
        {
            if ("" != name)
                this.esName = Marshal.StringToHGlobalAuto(name);

            this.esExtraID = (byte)extraID;
            this.esDummyInt = dummyInt;


            this.esShapeTp = TEditShapeType.esCircle;
            this.cirRad = cirRadMm;

            this.cirCXY = cirCxyMm;
        }
        public TEditShape(TFPoint lineSxyMm, TFPoint lineExyMm, TFloat lineRadMm, string name = "",
            int extraID = 0, int dummyInt = 0)
        {
            if ("" != name)
                this.esName = Marshal.StringToHGlobalAuto(name);

            this.esExtraID = (byte)extraID;
            this.esDummyInt = dummyInt;


            this.esShapeTp = TEditShapeType.esLine;
            this.lineRad = lineRadMm;

            this.lineSXY = lineSxyMm;
            this.lineEXY = lineExyMm;
        }
        public TEditShape(TFPoint rectCxyMm, TFloat rectRadXMm, TFloat rectRadYMm, string name = "",
            int extraID = 0, int dummyInt = 0)
        {
            if ("" != name)
                this.esName = Marshal.StringToHGlobalAuto(name);

            this.esExtraID = (byte)extraID;
            this.esDummyInt = dummyInt;


            this.esShapeTp = TEditShapeType.esRectangle;
            this.rectRadX = rectRadXMm;

            this.rectCXY = rectCxyMm;
            this.rectRadY = rectRadYMm;
        }
        public TEditShape(TFPoint arcSxyMm, TFPoint arcExyMm, TFPoint arcCxyMm, TFloat arcLineRadMm,
            TRotateOrient arcOrient, string name = "",
            int extraID = 0, int dummyInt = 0)
        {
            if ("" != name)
                this.esName = Marshal.StringToHGlobalAuto(name);

            this.esExtraID = (byte)extraID;
            this.esDummyInt = dummyInt;


            //Marshal.FreeHGlobal(this.esName);

            this.esShapeTp = TEditShapeType.esArc;
            this.arcLineRad = arcLineRadMm;

            this.arcSXY = arcSxyMm;
            this.arcEXY = arcExyMm;
            this.arcCXY = arcCxyMm;
            this.arcOrnt = arcOrient;
        }

    }
    #endregion


    #region ClsM2dTypeDefineFunctions =============================================
    public static class ClsM2dTypeDefineFunctions
    {
        public static int cAllLayerID = -1;

        public static void Clear_TEditShapes(ref TEditShape[] editShps)
        {
            if (null == editShps || editShps.Length<=0) return;

            for (int i = 0; i < editShps.Length; i++)
            {
                if (editShps[i].esName!=IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(editShps[i].esName);
                }
            }

            editShps = [];
        }
        public static void Clear_TEditShapes2D(ref TEditShape[][] edit2dShps)
        {
            if (null == edit2dShps || edit2dShps.Length <= 0) return;

            for (int iy = 0; iy < edit2dShps.Length; iy++)
            {
                for (int ix = 0; ix < edit2dShps[iy].Length; ix++)
                {
                    if (edit2dShps[iy][ix].esName != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(edit2dShps[iy][ix].esName);
                    }
                }

                edit2dShps[iy] = [];
            }

            edit2dShps = [];
        }


        public static TFloat Get_Distance(TFloat x1, TFloat y1, TFloat x2, TFloat y2)
        {
            return Math.Sqrt(Math.Pow((x2 - x1), 2) + Math.Pow((y2 - y1), 2));
        }

        public static TFPoint Get_Line_MiddlePoint(TFPoint lineSXY,  TFPoint lineEXY)
        {
            return new TFPoint((lineSXY.X + lineEXY.X) / 2.0, (lineSXY.Y + lineEXY.Y) / 2.0);
        }

        public static TFPoint Get_TEditShape_CXY(TEditShape editShp)
        {
            switch(editShp.esShapeTp)
            {
                case TEditShapeType.esLine:
                    return Get_Line_MiddlePoint(editShp.lineSXY, editShp.lineEXY);
                case TEditShapeType.esRectangle:
                    return editShp.rectCXY;
                case TEditShapeType.esPoint:
                    return editShp.ptXY;
                case TEditShapeType.esCircle:
                    return editShp.cirCXY;
                case TEditShapeType.esArc:
                    return editShp.arcCXY;
                default:
                    return new TFPoint(0.0, 0.0);
            }

        }
        public static void Paint_Ellipse(Graphics g,
            int aLeft, int aTop, int aRight, int aBottom,
            Color paintClr, int penThickness = 2)
        {
            //pbx.Refresh();
            Pen drwaPen = new Pen(paintClr, penThickness);
            int width = aRight - aLeft, height = aBottom - aTop;

            Rectangle rect = new Rectangle(
                Math.Min(aLeft, aRight),
                Math.Min(aTop, aBottom),
                width * Math.Sign(width),
                height * Math.Sign(height));

            g.DrawEllipse(drwaPen, rect);
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
        public static void Paint_Rectangle(Graphics g,
            int aLeft, int aTop, int aRight, int aBottom,
            Color paintClr, int penThickness = 2
            )
        {
            //pbx.Refresh();
            Pen drwaPen = new Pen(paintClr, penThickness);
            int width = aRight - aLeft, height = aBottom - aTop;

            Rectangle rect = new Rectangle(
                Math.Min(aLeft, aRight),
                Math.Min(aTop, aBottom),
                width * Math.Sign(width),
                height * Math.Sign(height));

            g.DrawRectangle(drwaPen, rect);
        }
        /// <summary>
        /// 畫Rectangle
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

            Graphics g = pbx.CreateGraphics();
            Paint_Rectangle(g,
                aLeft, aTop, aRight, aBottom,
                paintClr, penThickness
            );
        }

        public static void Paint_Line(Graphics g,
            int sX, int sY, int eX, int eY,
            Color paintClr, int penThickness = 2
            )
        {
            //pbx.Refresh();
            Pen drwaPen = new Pen(paintClr, penThickness);

            g.DrawLine(drwaPen, new Point(sX, sY), new Point(eX, eY));
        }

        public static void Paint_TEditShapes(int canvasID, TEditShape[] paintShps, Color paintClr, PaintEventArgs e,
            int atExtraID = 0, int atDummyInt = 0, int fontSz = 32)
        {
            if (paintShps.Length <= 0) return;

            int cPxlW2 = 3;
            int imgX1 = 0, imgY1 = 0, imgX2 = 0, imgY2 = 0;
            bool blDrawString = false, blDrawShape = false;
            int imgCX = 0, imgCY = 0;

            for (int i = 0; i < paintShps.Length; i++)
                if (paintShps[i].esExtraID == atExtraID)
                if (paintShps[i].esDummyInt == cAllLayerID || paintShps[i].esDummyInt == atDummyInt)
                {
                    blDrawString = (paintShps[i].esName != IntPtr.Zero);

                    switch (paintShps[i].esShapeTp)
                    {
                        case TEditShapeType.esLine:
                            ClsRvCamDLL.CS_RvCam_Get_CamToImageXY(canvasID,
                                    paintShps[i].lineSXY.X,
                                    paintShps[i].lineSXY.Y,
                                    ref imgX1, ref imgY1);
                            ClsRvCamDLL.CS_RvCam_Get_CamToImageXY(canvasID,
                                    paintShps[i].lineEXY.X,
                                    paintShps[i].lineEXY.Y,
                                    ref imgX2, ref imgY2);
                            blDrawShape = true;
                            imgCX = (imgX1 + imgX2) / 2;
                            imgCY = (imgY1 + imgY2) / 2;
                            break;
                        case TEditShapeType.esRectangle:
                            ClsRvCamDLL.Convert_Cam_To_ImageMinMax(
                                    canvasID,
                                    paintShps[i].rectCXY.X - paintShps[i].rectRadX,
                                    paintShps[i].rectCXY.Y - paintShps[i].rectRadY,
                                    paintShps[i].rectCXY.X + paintShps[i].rectRadX,
                                    paintShps[i].rectCXY.Y + paintShps[i].rectRadY,
                                    ref imgX1, ref imgY1, ref imgX2, ref imgY2
                                    );
                            blDrawShape = true;
                            ClsRvCamDLL.CS_RvCam_Get_CamToImageXY(canvasID, paintShps[i].rectCXY.X, paintShps[i].rectCXY.Y, ref imgCX, ref imgCY);
                            break;
                        case TEditShapeType.esPoint:
                            ClsRvCamDLL.Convert_Cam_To_ImageMinMax(
                                    canvasID,
                                    paintShps[i].ptXY.X - paintShps[i].ptRad,
                                    paintShps[i].ptXY.Y - paintShps[i].ptRad,
                                    paintShps[i].ptXY.X + paintShps[i].ptRad,
                                    paintShps[i].ptXY.Y + paintShps[i].ptRad,
                                    ref imgX1, ref imgY1, ref imgX2, ref imgY2
                                    );
                            ClsRvCamDLL.CS_RvCam_Get_CamToImageXY(canvasID, paintShps[i].ptXY.X, paintShps[i].ptXY.Y, ref imgCX, ref imgCY);
                            blDrawShape = true;
                            break;
                        case TEditShapeType.esCircle:
                            ClsRvCamDLL.Convert_Cam_To_ImageMinMax(
                                    canvasID,
                                    paintShps[i].cirCXY.X - paintShps[i].cirRad,
                                    paintShps[i].cirCXY.Y - paintShps[i].cirRad,
                                    paintShps[i].cirCXY.X + paintShps[i].cirRad,
                                    paintShps[i].cirCXY.Y + paintShps[i].cirRad,
                                    ref imgX1, ref imgY1, ref imgX2, ref imgY2
                                    );
                            blDrawShape = true;
                            ClsRvCamDLL.CS_RvCam_Get_CamToImageXY(canvasID, paintShps[i].cirCXY.X, paintShps[i].cirCXY.Y, ref imgCX, ref imgCY);
                            break;
                        case TEditShapeType.esArc:
                            TFloat arcRad = Get_Distance(paintShps[i].arcCXY.X, paintShps[i].arcCXY.Y, paintShps[i].arcSXY.X, paintShps[i].arcSXY.Y);
                            ClsRvCamDLL.Convert_Cam_To_ImageMinMax(
                                    canvasID,
                                    paintShps[i].arcCXY.X - arcRad,
                                    paintShps[i].arcCXY.Y - arcRad,
                                    paintShps[i].arcCXY.X + arcRad,
                                    paintShps[i].arcCXY.Y + arcRad,
                                    ref imgX1, ref imgY1, ref imgX2, ref imgY2
                                    );
                            blDrawShape = true;
                            ClsRvCamDLL.CS_RvCam_Get_CamToImageXY(canvasID, paintShps[i].arcSXY.X, paintShps[i].arcSXY.Y, ref imgCX, ref imgCY);
                            break;
                        default:
                            return;
                    }

                    if (blDrawShape == true)
                    {
                        if (ClsDelphiEquivalent.In(paintShps[i].esShapeTp, [TEditShapeType.esLine]))
                        {
                            Paint_Line(e.Graphics, imgX1, imgY1, imgX2, imgY2, paintClr);
                        }
                        else if (ClsDelphiEquivalent.In(paintShps[i].esShapeTp, [TEditShapeType.esCircle]))
                        {
                                imgX1 = (imgX1 == imgX2) ? imgX1 - cPxlW2 : imgX1;
                                imgX2 = (imgX1 == imgX2) ? imgX2 + cPxlW2 : imgX2;
                                imgY1 = (imgY1 == imgY2) ? imgY1 - cPxlW2 : imgY1;
                                imgY2 = (imgY1 == imgY2) ? imgY2 + cPxlW2 : imgY2;
                                Paint_Ellipse(e.Graphics, imgX1, imgY1, imgX2, imgY2, paintClr);
                            }
                        else
                        {
                            imgX1 = (imgX1 == imgX2) ? imgX1 - cPxlW2 : imgX1;
                            imgX2 = (imgX1 == imgX2) ? imgX2 + cPxlW2 : imgX2;
                            imgY1 = (imgY1 == imgY2) ? imgY1 - cPxlW2 : imgY1;
                            imgY2 = (imgY1 == imgY2) ? imgY2 + cPxlW2 : imgY2;
                            Paint_Rectangle(e.Graphics, imgX1, imgY1, imgX2, imgY2, paintClr);
                        }                            
                    }

                    if (blDrawString == true && IntPtr.Zero != paintShps[i].esName)
                    {
                        string sName = (IntPtr.Zero == paintShps[i].esName) ? "" : Marshal.PtrToStringAuto(paintShps[i].esName);
                        SizeF stringSize = new SizeF();
                        int szSXY = 0;
                        var aPaintFontFamily = new FontFamily("Times New Roman");
                        System.Drawing.Font aPaintFont = new System.Drawing.Font(aPaintFontFamily, fontSz, FontStyle.Bold, GraphicsUnit.Pixel);
                        stringSize = e.Graphics.MeasureString(sName, aPaintFont);
                        szSXY = (int)stringSize.Width;
                        var solidBrush = new SolidBrush(paintClr);

                        e.Graphics.DrawString(sName, aPaintFont,
                            solidBrush, new PointF(imgCX, imgCY));
                    }
                }
        }
        public static void Paint_TEditShapes2D(int canvasID, TEditShape[][] paint2dShps, Color paintClr, PaintEventArgs e,
            int atExtraID = 0, int atDummyInt = 0, int fontSz = 32)
        {
            for (int iy = 0; iy < paint2dShps.Length; iy++) 
                Paint_TEditShapes(canvasID, paint2dShps[iy], paintClr, e, atExtraID, atDummyInt, fontSz);
        }
    }


   

        #endregion
#pragma warning restore CS8600 // 取消 warnning 
    }