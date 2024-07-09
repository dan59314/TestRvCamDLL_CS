{
  //**********************************************************************
  編譯好的 plugin DLL，放到程式下的 [PlugIn] 目錄內，程式會自動讀取

  //**********************************************************************
}

unit Export_PlugIn_FileIO_Delphi;

interface

  uses
    System.Classes,

    VCL.Dialogs  //如果外部引用此 DLL 時會當掉，則不要使用 VCL.Dialog

    ;


type
  TReturnCode=(rcFail=0, rcSuccess, rcUnAuthorized, rcFinal);
  TIslandHole = (ihIsland, ihHole); // 如果是 hiHole, 則下方必須是透空的
  TIslandHoles = Set of TIslandHole;
  TLineEndType = (leRound=0, leSqrExtend, leSqrFlat, leShape);  // 圓頭, 無延伸平頭, 延伸平頭 線段,  形狀頭

  TRotateOrient=(oCCW=0, oCW);  //真實座標系統, 非螢幕座標系統

  TVectSimpleShapeType=(
    vstNone=0,  vstArc, vstCircle, vstLine,
    vstRect, vstPolygon, vstPolyLine,
    vstSegments, vstIslandHoleShape );
  TVectSimpleShapeTypes = Set of TVectSimpleShapeType;

  TIslandHoleShapeMode = (
    ihsTransparentIslandHole=0,
    ihsOpaqueIslandHole,
    ihsShapesGroup);

type
  TFloat = double;

  TFPoint= packed record
    X,Y:TFloat;
  end;
  pDynFPoints =^TDynFPoints;
  TDynFPoints = Array of TFPoint;

  PFLine=^TFLine;
  TFLine=record
    lnW:TFloat;
    dummyFloat:TFloat;
    case integer of
    0:(x1,y1,x2,y2:TFloat);
    1:(sx,sy,ex,ey:TFloat);
    2:(sXY, eXY:TFPoint);
  end;
  PDynFLines = ^TDynFLines;
  TDynFLines = Array of TFLine;

  PFRect=^TFRect;
//  {$IFDEF UsingFMX}
//  TFRect = TRectF;
//  {$ELSE}
  TFRect=packed record
    case integer of
    0:(Left,Top,Right,Bottom:TFloat;);
    1:(LeftTop:TFPoint; RightBottom:TFPoint);
  end;


  PVectSimpleShape=^TVectSimpleShape;
  TVectSimpleShape = packed record
    vstMinMax:TFRect;
    vstIslandHole:TIslandHole;
    vstType:TVectSimpleShapeType;
    vstPObj:Pointer; // 用來追蹤所屬 Object
    vstRad:TFloat;
    vstRefSymbolTp:integer; //TVectSymbolType;
    {$IFNDEF DisableGDI_EFFECT}
    vstPFill:Pointer; //PFillRec;
    {$ENDIF}


    vstDummyInt, //
    vstDummyint1,
    vstDummyInt2:Integer; //Variant;
    vstDummyFloat:TFloat;
    vstDummyPointer,
    vstDummyPointer1:Pointer;

    case  TVectSimpleShapeType of
    vstNone:();
    vstArc:(arStart, arEnd, arCenter: TFPoint; arOrient: TRotateOrient;
          arEndType:TLineEndType);
    vstCircle:  (cirCXY:TFPoint);        // 一個 TFPoint
    vstLine:    (lneSXY,lneEXY:TFPoint);          // 一個 TLine
    vstRect:     (rcCXY:TFPoint; rcRadY:TFloat);
    vstPolygon:  (PPolygon:PDynFPoints; plgAryLength:integer); // 一個 TDynFPonits
    vstPolyLine: (PPolyLine:PDynFPoints; plnAryLength:integer); // 一個 TDynFPonits
    vstSegments: (PSegments:PDynFLines; segAryLength:integer); // 一個  線段群組 PLIne
    vstIslandHoleShape: (vstIhsMode:TIslandHoleShapeMode; // 也可參考 TVectSimpleShape.vstRefSymbolTp
       ihShapeList:{$IFDEF windows}TVectSimpleShapeList{$ELSE}Pointer{$ENDIF};
       shpCount:integer);
  end;
  PDynVectSimpleShapes = ^TDynVectSimpleShapes;
  TDynVectSimpleShapes = Array of TVectSimpleShape;



const
  cPlugInDescription = 'SimpleShapeFile IO';


function Is_RvCamDLL(
  var sDLLDescription:PWideChar
  ):TReturnCode; stdcall;

function Load_File(
  const setLoadFileName:PWideChar;
  var pShapeDataArray0:Pointer;
  var shapeDataLength:integer
  ):TReturnCode; stdcall;

function Process_TVectSimpleShapes(
  var pShapeDataArray0:Pointer;
  var shapeDataLength:integer
  ):TReturnCode; stdcall;

function Save_File(
  var setgetSaveFileName:PWideChar;
  pShapeDataArray0:Pointer;
  shapeDataLength:integer
  ):TReturnCode; stdcall;

implementation


function Is_RvCamDLL(
  var sDLLDescription:PWideChar
  ):TReturnCode; stdcall;
begin
{$IFDEF Debug}
  ShowMessage( 'Is_RvCamDLL( ) called.');
{$ENDIF}

  result := TReturnCode.rcFail;


  result := TReturnCode.rcSuccess;
end;

function Load_File(
  const setLoadFileName:PWideChar;
  var pShapeDataArray0:Pointer;
  var shapeDataLength:integer
  ):TReturnCode; stdcall;
var
  fn:String;
begin
{$IFDEF Debug}
  ShowMessage( 'Load_File( ) called.');
{$ENDIF}

  result := TReturnCode.rcFail;

  pShapeDataArray0 := nil;
  shapeDataLength := 0;

  if (nil=setLoadFileName) then exit;


  // Create your


  if (pShapeDataArray0<>nil) and (shapeDataLength>0) then
    result := TReturnCode.rcSuccess;
end;

function Process_TVectSimpleShapes(
  var pShapeDataArray0:Pointer;
  var shapeDataLength:integer
  ):TReturnCode; stdcall;
begin
{$IFDEF Debug}
  ShowMessage( 'Process_TVectSimpleShapes( ) called.');
{$ENDIF}

  result := TReturnCode.rcFail;


  result := TReturnCode.rcSuccess;
end;

function Save_File(
  var setgetSaveFileName:PWideChar;
  pShapeDataArray0:Pointer;
  shapeDataLength:integer
  ):TReturnCode; stdcall;
begin
{$IFDEF Debug}
  ShowMessage( 'Save_File( ) called.');
{$ENDIF}

  result := TReturnCode.rcFail;


  result := TReturnCode.rcSuccess;
end;


end.
