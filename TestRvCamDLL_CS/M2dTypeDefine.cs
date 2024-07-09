


using RvCamDLL;
using System;
using System.Runtime.InteropServices;

namespace M2dTypeDefine
{

    using TFloat = double;

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
        mLastSaveDir = mLastDir;
    }


    #region Generic Array Add/Delete
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

    #region Delphi Equivalent====================
    public static class DelphiphiEq
    {
        public static bool In<T>(this T obj, IEnumerable<T> arr)
        {
            return arr.Contains(obj);
        }
    }
    #endregion


    public enum TEditShapeType : int //: UInt16
    {
        esNone = 0, esPoint, esLine, esArc, esCircle,
        esRectangle, esPolygon, esObject, esDummyType
    }

    public enum TValueUnit : int
    {
        uInch = 0, uMil = 1, uCM = 2, uMM = 3, uUM = 4
    }

    public enum TRotateOrient : int
    {
        oCCW = 0, oCW
    }

    public enum TLineEndType : int 
    {
        leRound=0, leSqrExtend, leSqrFlat, leShape  // 圓頭, 無延伸平頭, 延伸平頭 線段,  形狀頭
    }   


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

        //public TVectSimpleShape()
        //{
        //}
    }
}