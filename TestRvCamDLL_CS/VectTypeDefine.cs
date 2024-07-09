using M2dTypeDefine;
using System;
using System.Runtime.InteropServices;

namespace VectTypeDefine
{
    using static System.Runtime.InteropServices.JavaScript.JSType;
    using TFloat = double;


    #region Enum Type Define =========================================
    public enum TVectSide : int
    {
        vsNone = 0, vsComp, vsSold, vsBoth
    }

    public enum TViewMode : int
    {
        vmHome = 0, vmZoomIn, vmZoomOut, vmViewAtXY, vmViewLeft, vmViewUp,
        vmViewRight, vmViewDown, vmCanvasSizeChanged, vmDegreeMirrorX,
        vmViewAtMmPerPixel
    }

    public enum TVectPaintMode : int
    {
        pmSolid_Normal = 0, pmHollow_Normal, pmSkeleton_Normal,
        pmSolid_CompReality, pmHollow_CompReality, pmSkeleton_CompReality,
        pmSolid_SoldRelaity, pmHollow_SoldReality, pmSkeleton_SoldReality
    }

    public enum TVPRenderColor : int
    {
        vcByTVectSymbol = 0, vcByTVectObject, vcByTVectLayer, vcByTVectStep,
        vcByTFillRec, vcByTCode, vcByGCode
    }

    public enum TVectFileType : int
    {
        vtUnknown = 0, vtNewCreated, vtRaster, vtMVI, vtGerber274X,
        vtOdb, vtTGZOdb, vtExcellon, vtIPC356, vtSiebMeyer, vtSVG, vtDXF, vtDPF,
        vtAI, vtPostScript, vtEPS, vtRAR, vtZIP, vtEastekCar, vtTxt,
        vtTestFile, vtErrorLog, vtRasVectorCam, vtLdiBin, vtGDS, vtSimpleShapeFile,
        vtGIH
    }

    public enum TOdbStepListType : int
    {
        osAllSteps = 0, osInheritedStepsOnly, osTopStepsOnly,
        osBottomStepsOnly, osIndependentStepsOnly
    }

    public enum TOdbTgzType : int
    {
        otOdbFolder = 0, otTgzFile = 1
    }

    public enum TActionTarget : int
    {
        smTVectSymbol = 0, smTVectObject, smContinueLines,
        smTFPoints, smTestPoint, sm4wire
    }

    public enum TSelectAction : int
    {
        saSelect = 0, saUnSelect, saDelete, saUnDelete,
        saFreeze, saUnFreeze
    }

    public enum TVectSymbolType : int
    {
        stNone = 0, stText, stPoint, stSqrLine, stExtendSqrLine,
        stRndLine, stRectLine, stArc, stNSidePolygon, stNSideStar, stDiamond,
        stRect, stRndRect, stSqrDonut, stRndDonut, stEllipse, stOval, stFreeHand,
        stPoints, stPolyLine, stPolygon, stBSpline, stBSPolyline,
        stBSPolygon, stBzCurve, stBezierPolyLine, stBezierPolygon, stMixPolyLine,
        stMixPolygon, stIslandHoleSurface, stSymbolGroup,

        stDummy1, // 線段集合 -----

        // 保留形狀，供後續擴充用 -------------------------------------
        stOutSymbolLayer, stBarCode, stNurbPolyLine, stNurbPolygon, stBitmap,
        stReservedStart, stReserved07, stReserved08, stReserved09, stReserved10,
        stReserved11, stReserved12, stReserved13, stReserved14, stReserved15,
        stReserved16, stReserved17, stReserved18, stReserved19, stReservedFinal, 

        stOdbRndRect, stOdbChamfRect, stOdbOctagon, // 不等邊長八邊形 
        stOdbHHexagon,
        //水平不等邊長六邊形
        stOdbVHexagon, // 垂直  
        stOdbButterfly, stOdbSqrButterfly, stOdbTriangle,
        stOdbHalfOval, stOdbRndThermalRnd, stOdbRndThermalSqr, //Gerber274X AM7  ,
        stOdbSqrThermal, stOdbSqrThermalOpen, stOdbSqrRndThermal, stOdbRectThermal,
        stOdbRectThermalOpen, stOdbMoire, //Gerber274X AM6 ,
        stOdbDrillHole,
        // 2010'09'30 新增 -------------------------------------------------------------------
        stOdbSqrRndDonut, stOdbRndSqrDonut, stOdbRectDonut, stOdbRndRectDonut,
        stOdbOvalDonut, stOdbRndSqrThermal, stOdbRndSqrThermalOpen,
        stOdbRndRectThermal, stOdbRndRectThermalOpen, stOdbOvalThermal,
        stOdbOvalThermalOpen
    }       

    public enum TVectSimpleShapeType : int
    {
        vstNone = 0, vstArc, vstCircle, vstLine,
        vstRect, vstPolygon, vstPolyLine,
        vstSegments, vstIslandHoleShape
    }

    public enum TIslandHole : int
    {
        ihIsland=0, 
        ihHole
    }

    public enum TIslandHoleShapeMode : int
    {    
        ihsTransparentIslandHole=0,
        ihsOpaqueIslandHole,
        ihsShapesGroup
    }
    #endregion


    #region Structure Type Define ====================================
    [StructLayout(LayoutKind.Explicit, Pack = 1, CharSet = CharSet.Ansi)]
    public unsafe struct TVectSimpleShape
    {
        [FieldOffset(0)]  public TFRect vstMinMax;
        [FieldOffset(32)] public TIslandHole vstIslandHole;
        [FieldOffset(36)] public TVectSimpleShapeType vstType;
        [FieldOffset(40)] public IntPtr vstPObj;
        [FieldOffset(48)] public TFloat vstRad;
        [FieldOffset(56)] public TVectSymbolType vstRefSymbolTp;
        [FieldOffset(60)] public IntPtr vstPFill;
        [FieldOffset(68)] public int vstDummyInt;
        [FieldOffset(72)] public int vstDummyint1;
        [FieldOffset(76)] public int vstDummyInt2;
        [FieldOffset(80)] public TFloat vstDummyFloat;
        [FieldOffset(88)] public IntPtr vstDummyPointer;
        [FieldOffset(96)] public IntPtr vstDummyPointer1;

        [FieldOffset(104)] public TFPoint arStart;
        [FieldOffset(120)] public TFPoint arEnd;
        [FieldOffset(136)] public TFPoint arCenter;
        [FieldOffset(152)] public TRotateOrient arOrient;
        [FieldOffset(156)] public TLineEndType arEndType;

        [FieldOffset(104)] public TFPoint cirCXY;

        [FieldOffset(104)] public TFPoint lneSXY;
        [FieldOffset(120)] public TFPoint lneEXY;

        [FieldOffset(104)] public TFPoint rcCXY;
        [FieldOffset(120)] public TFloat rcRadY;

        [FieldOffset(104)] public IntPtr PPolygon;  //不能用 TFPoint[] PPolygon 會當掉
        [FieldOffset(112)] public int plgAryLength; 

        [FieldOffset(104)] public IntPtr PPolyLine;//IntPtr 
        [FieldOffset(112)] public int plnAryLength; 

        [FieldOffset(104)] public IntPtr PSegments;//IntPtr 
        [FieldOffset(112)] public int segAryLength; 

        [FieldOffset(104)] public TIslandHoleShapeMode vstIhsMode;
        [FieldOffset(108)] public IntPtr ihShapeList;   //IntPtr 
        [FieldOffset(116)] public int shpCount;


        /// <summary>
        /// 建立圓形物件
        /// </summary>
        /// <param name="cirCxyMm">中心XYmm</param>
        /// <param name="cirRadMm">圓形半徑mm</param>
        public TVectSimpleShape(TFPoint cirCxyMm, TFloat cirRadMm)
        {
            this.vstType = TVectSimpleShapeType.vstCircle;
            this.vstRad = cirRadMm;

            this.cirCXY = cirCxyMm;
        }
        /// <summary>
        /// 建立有寬度線段
        /// </summary>
        /// <param name="lineSxyMm">起點XYmm</param>
        /// <param name="lineExyMm">終點XYmm</param>
        /// <param name="lineRadMm">線寬半徑mm</param>
        public TVectSimpleShape(TFPoint lineSxyMm, TFPoint lineExyMm, TFloat lineRadMm)
        {
            this.vstType = TVectSimpleShapeType.vstLine;
            this.vstRad = lineRadMm;
            
            this.lneSXY = lineSxyMm;
            this.lneEXY = lineExyMm;  
        }
        /// <summary>
        /// 建立長方形物件
        /// </summary>
        /// <param name="rectCxyMm">長方形中心CXYmm</param>
        /// <param name="rectRadXMm">長方形X半徑mm</param>
        /// <param name="rectRadYMm">長方形Y半徑mm</param>
        public TVectSimpleShape(TFPoint rectCxyMm, TFloat rectRadXMm, TFloat rectRadYMm)
        {
            this.vstType = TVectSimpleShapeType.vstRect;
            this.vstRad = rectRadXMm;

            this.rcCXY = rectCxyMm;
            this.rcRadY = rectRadYMm;
        }
        /// <summary>
        /// 建立Arc物件
        /// </summary>
        /// <param name="arcSxyMm">Arc起點XYmm</param>
        /// <param name="arcExyMm">Arc終點XYmm</param>
        /// <param name="arcCxyMm">Arc圓中心XYmm</param>
        /// <param name="arcLineRadMm">Arc線寬半徑mm</param>
        /// <param name="arcOrient">Arc方向 CW/CCW</param>
        /// <param name="lneEndTp">Arc端點類型</param>
        public TVectSimpleShape(TFPoint arcSxyMm, TFPoint arcExyMm, TFPoint arcCxyMm, TFloat arcLineRadMm,
            TRotateOrient arcOrient, TLineEndType lneEndTp = TLineEndType.leRound)
        {
            this.vstType = TVectSimpleShapeType.vstArc;
            this.vstRad = arcLineRadMm;

            this.arStart = arcSxyMm;
            this.arEnd = arcExyMm;
            this.arCenter = arcCxyMm;
            this.arOrient = arcOrient;
            this.arEndType = lneEndTp;
        }
        /// <summary>
        /// 建立Polygon or PolyLine
        /// </summary>
        /// <param name="polyXYsMm">傳入所有點XYmm</param>
        public TVectSimpleShape(TVectSimpleShapeType polyType, TFPoint[] polyXYsMm, TFloat lineRadMm)
        {
            if (!DelphiphiEq.In(polyType, [TVectSimpleShapeType.vstPolyLine, TVectSimpleShapeType.vstPolygon]))
                return;

            this.vstType = polyType;
            this.vstRad = lineRadMm;

            int szArray = polyXYsMm.Length * sizeof(TFPoint);

            IntPtr pAllocate = Marshal.AllocCoTaskMem(szArray);
            fixed (TFPoint* pShps0 = &(polyXYsMm[0]))
            {
                Buffer.MemoryCopy(pShps0, (void*)pAllocate, szArray, szArray);
            }

            this.plgAryLength = polyXYsMm.Length;

            this.PPolygon = pAllocate;
        }
        /// <summary>
        /// 線段集合
        /// </summary>
        /// <param name="linesMm">所有線段資料</param>
        /// <param name="lineRadMm">線段寬度半徑Mm</param>
        public TVectSimpleShape(TFLine[] linesMm, TFloat lineRadMm)
        {
            this.vstType = TVectSimpleShapeType.vstSegments;
            this.vstRad = lineRadMm;

            int szArray = linesMm.Length * sizeof(TFLine);

            IntPtr pAllocate = Marshal.AllocCoTaskMem(szArray);
            fixed (TFLine* pShps0 = &(linesMm[0]))
            {
                Buffer.MemoryCopy(pShps0, (void*)pAllocate, szArray, szArray);
            }

            this.segAryLength = linesMm.Length;

            this.PSegments = pAllocate;
        }
        public TVectSimpleShape(TVectSimpleShape fromShp)
        {
            this = fromShp;

            int szArray = 0;
            IntPtr pAllocate = IntPtr.Zero; 

            switch (this.vstType)
            {
                case TVectSimpleShapeType.vstPolygon:
                case TVectSimpleShapeType.vstPolyLine:
                    szArray = fromShp.plgAryLength * sizeof(TFPoint);
                    pAllocate = Marshal.AllocCoTaskMem(szArray);
                    Buffer.MemoryCopy((void*)fromShp.PPolygon, (void*)pAllocate, szArray, szArray);
                    this.PPolygon = pAllocate;
                    break;
                case TVectSimpleShapeType.vstSegments:
                    szArray = fromShp.segAryLength * sizeof(TFLine);
                    pAllocate = Marshal.AllocCoTaskMem(szArray);
                    Buffer.MemoryCopy((void*)fromShp.PSegments, (void*)pAllocate, szArray, szArray);
                    this.PSegments = pAllocate;
                    break;
                case TVectSimpleShapeType.vstIslandHoleShape:
                    szArray = fromShp.shpCount * sizeof(TVectSimpleShape);
                    pAllocate = Marshal.AllocCoTaskMem(szArray);
                    Buffer.MemoryCopy((void*)fromShp.ihShapeList, (void*)pAllocate, szArray, szArray);

                    TVectSimpleShape* pFr = (TVectSimpleShape*)fromShp.ihShapeList, pTo = (TVectSimpleShape*)pAllocate;                    
                    for (int i = 0; i < fromShp.shpCount; i++)
                    {
                        (*pTo) = new TVectSimpleShape((*pFr));
                        pFr += 1; //sizeOf(TVectSimpleShape);
                        pTo += 1; //sizeOf(TVectSimpleShape);
                    }

                    this.ihShapeList = pAllocate;
                    break;
            }
        }
        public TVectSimpleShape(TVectSimpleShape[] vectSimpleShapes)
        {

            this.vstType = TVectSimpleShapeType.vstIslandHoleShape;
            this.vstRad = 0.0f;

            int szArray = vectSimpleShapes.Length * sizeof(TVectSimpleShape);

            IntPtr pAllocate = Marshal.AllocCoTaskMem(szArray);
            fixed (TVectSimpleShape* pShps0 = &(vectSimpleShapes[0]))
            {
                //Buffer.MemoryCopy(pShps0, (void*)pAllocate, szArray, szArray);
                TVectSimpleShape* pFr = pShps0, pTo = (TVectSimpleShape*)pAllocate;
                for (int i = 0; i < vectSimpleShapes.Length; i++)
                {
                    (*pTo) = new TVectSimpleShape((*pFr));
                    pFr += 1; //sizeOf(TVectSimpleShape);
                    pTo += 1; //sizeOf(TVectSimpleShape);
                }
            }

            this.shpCount= vectSimpleShapes.Length;

            this.ihShapeList = pAllocate;
        }
    }
    #endregion


    public static class ClsVectManager
    {
        #region Functions =============================================
        public static void Shift_TFPoint(ref TFPoint fpt, TFPoint shiftXY)
        {
            fpt.X = fpt.X + shiftXY.X;
            fpt.Y = fpt.Y + shiftXY.Y;
        }


        public static void Shift_TVectSimpleShape(ref TVectSimpleShape vsShp, TFPoint shiftXY)
        {
            Shift_TFPoint(ref vsShp.vstMinMax.LeftTop, shiftXY);
            Shift_TFPoint(ref vsShp.vstMinMax.RightBottom, shiftXY);

            switch ( vsShp.vstType)
            {
                case TVectSimpleShapeType.vstArc:
                    Shift_TFPoint(ref vsShp.arStart, shiftXY);
                    Shift_TFPoint(ref vsShp.arEnd, shiftXY);
                    Shift_TFPoint(ref vsShp.arCenter, shiftXY);
                    break;
                case TVectSimpleShapeType.vstCircle:
                    Shift_TFPoint(ref vsShp.cirCXY, shiftXY);
                    break;
                case TVectSimpleShapeType.vstRect:
                    Shift_TFPoint(ref vsShp.rcCXY, shiftXY);
                    break;
                case TVectSimpleShapeType.vstLine:
                    Shift_TFPoint(ref vsShp.lneSXY, shiftXY);
                    Shift_TFPoint(ref vsShp.lneEXY, shiftXY);
                    break;

                case TVectSimpleShapeType.vstPolygon:
                case TVectSimpleShapeType.vstPolyLine:
                    unsafe
                    {      
                        TFPoint* pXY = (TFPoint*)vsShp.PPolygon;
                        for (int i = 0; i < vsShp.plgAryLength; i++)
                        {
                            Shift_TFPoint( ref(*pXY), shiftXY);
                            pXY += 1; // sizeof(TFPoint);
                        }
                    }                  
                    break;

                case TVectSimpleShapeType.vstSegments:
                    unsafe
                    {
                        TFLine* pLn = (TFLine*)vsShp.PSegments;
                        for (int i = 0; i < vsShp.segAryLength; i++)
                        {
                            Shift_TFPoint(ref(*pLn).SXY, shiftXY);
                            Shift_TFPoint(ref(*pLn).EXY, shiftXY);
                            pLn += 1; // sizeof(TFLine);
                        }
                    }
                    break;

                case TVectSimpleShapeType.vstIslandHoleShape:
                    unsafe
                    {
                        TVectSimpleShape* pShp = (TVectSimpleShape*)vsShp.ihShapeList;
                        for (int i = 0; i < vsShp.shpCount; i++)
                        {
                            Shift_TVectSimpleShape( ref (*pShp), shiftXY);
                            //pShp = (TVectSimpleShape*)((IntPtr)pShp + 160);
                            pShp += 1; //sizeOf(TVectSimpleShape);
                        }
                    }
                    break;
            }

        }
        #endregion
    }
    
}