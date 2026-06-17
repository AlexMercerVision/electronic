using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

namespace BJJCZ_BaseDevelop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAP
    {
        public int bmType;
        public int bmWidth;
        public int bmHeight;
        public int bmWidthBytes;
        public ushort bmPlanes;
        public ushort bmBitsPixel;
        public IntPtr bmBits;
    }

    public class JczLmc
    {
        public static string GetErrorText(int nErr)
        {
            switch (nErr)
            {
                case 0: return "成功";
                case 1: return "发现EZCAD已经在运行";
                case 2: return "找不到EZCAD.CFG";
                case 3: return "打开LMC失败";
                case 4: return "没有LMC设备";
                case 5: return "lmc设备版本错误";
                case 6: return "找不到设备配置文件MarkCfg";
                case 7: return "有报警信号";
                case 8: return "用户中断";
                case 9: return "不明错误";
                case 10: return "超时";
                case 11: return "未初始化";
                case 12: return "读文件失败";
                case 13: return "窗口为空";
                case 14: return "找不到字体";
                case 15: return "笔号错误";
                case 16: return "指定对象不是文本对象";
                case 17: return "保存文件失败";
                case 18: return "保存文件失败找不到指定对象";
                case 19: return "当前状态不能执行此操作";
                case 20: return "参数输入错误";
                default:
                    break;
            }
            return "参数输入错误";
        }

        public static string GetErrorText(int nErr, bool English)
        {
            if (English)
            {
                switch (nErr)
                {
                    case 0: return "Success";
                    case 1: return " Now have a working EACAD";
                    case 2: return "No found EZCAD.CFG";
                    case 3: return "Open LMC faild";
                    case 4: return "No LMC Board";
                    case 5: return "Lmc vision Error";
                    case 6: return " No found MarkCfg in Plug ";
                    case 7: return "Error Signal";
                    case 8: return "User Stop";
                    case 9: return "unknown error";
                    case 10: return "out time";
                    case 11: return "No Initialization";
                    case 12: return "Read File Error";
                    case 13: return "full Windows";
                    case 14: return "No found font";
                    case 15: return "Pen error";
                    case 16: return "object is not text";
                    case 17: return "save file fail";
                    case 18: return "save file fail becouse same object is no found";
                    case 19: return "Now state can not work as command";
                    case 20: return "error Parameter";
                    default:
                        break;
                }
                return "error Parameter";
            }
            else
            {
                switch (nErr)
                {
                    case 0: return "成功";
                    case 1: return "发现EZCAD已经在运行";
                    case 2: return "找不到EZCAD.CFG";
                    case 3: return "打开LMC失败";
                    case 4: return "没有LMC设备";
                    case 5: return "lmc设备版本错误";
                    case 6: return "找不到设备配置文件MarkCfg";
                    case 7: return "有报警信号";
                    case 8: return "用户中断";
                    case 9: return "不明错误";
                    case 10: return "超时";
                    case 11: return "未初始化";
                    case 12: return "读文件失败";
                    case 13: return "窗口为空";
                    case 14: return "找不到字体";
                    case 15: return "笔号错误";
                    case 16: return "指定对象不是文本对象";
                    case 17: return "保存文件失败";
                    case 18: return "保存文件失败找不到指定对象";
                    case 19: return "当前状态不能执行此操作";
                    case 20: return "参数输入错误";
                    default:
                        break;
                }
                return "参数输入错误";
            }
        }


        #region   系统函数
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
        [DllImport("gdi32.dll")]
        private static extern int GetObject(IntPtr hgdiobj, int cbBuffer, ref BITMAP lpvObject);
        #endregion

        #region 设备相关 (MultiMarkEzd.dll 函数声明)

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_Initial", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_Initial(string strEzCadPath, bool bTestMode, IntPtr hOwenWnd);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_Close", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_Close();

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GetAllCardSN", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_GetAllCardSN([MarshalAs(UnmanagedType.LPArray, SizeConst = 8)] int[] SN);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_SetDevCfg", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_SetDevCfg(int cardSN);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_SetDevCfg2", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_SetDevCfg2(int cardSN, bool bAxisShow0, bool bAxisShow1);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_SetRotateMoveParam", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_SetRotateMoveParam(int cardSN, double dMoveX, double dMoveY, double dCenterX, double dCenterY, double dRotateAng);
        #endregion

        #region 加工相关

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_Mark", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_Mark(int cardSN, bool bFlyMark);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_MarkEntity", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_MarkEntity(int cardSN, string strEntName);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_MarkEntityFly", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_MarkEntityFly(int cardSN, string strEntName);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_MarkLine", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_MarkLine(int cardSN, double X1, double Y1, double X2, double Y2, int Pen);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_MarkPoint", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_MarkPoint(int cardSN, double X, double Y, double delay, int pen);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_StopMark", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool lmc1_StopMarkNative(int cardSN);

        public static int lmc1_StopMark(int cardSN)
        {
            return lmc1_StopMarkNative(cardSN) ? 0 : 9;
        }

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_RedLightMark", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_RedLightMark(int cardSN);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_RedLightMarkContourByEnt", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_RedLightMarkContourByEnt(int cardSN, string strEntName);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GotoPos", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_GotoPos(int cardSN, double x, double y);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GetCurCoor", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_GetCurCoor(int cardSN, ref double x, ref double y);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_IsMarkFinish", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool lmc1_IsMarkFinishNative(int cardSN);

        public static int lmc1_IsMarkFinish(int cardSN)
        {
            return lmc1_IsMarkFinishNative(cardSN) ? 1 : 0;
        }

        #endregion

        #region 文件相关

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_LoadEzdFile", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_LoadEzdFile(int cardSN, string strFileName);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_SaveEntLibToFile", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_SaveEntLibToFile(int cardSN, string strFileName);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GetPrevBitmap2", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        // [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GetPrevBitmap", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lmc1_GetPrevBitmap(int cardSN, IntPtr hwnd, int nBmpWidth, int nBmpHeight);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GetEzdFilePrevBitmap", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lmc1_GetEzdFilePrevBitmap(int cardSN, string strEntName);

        #endregion

        #region 对象操作

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GetEntSize", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_GetEntSize(int cardSN, string pEntName, ref double dMinx, ref double dMiny, ref double dMaxx, ref double dMaxy, ref double dZ);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_MoveEnt", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_MoveEnt(int cardSN, string pEntName, ref double dMovex, ref double dMovey);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_ScaleEnt", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_ScaleEnt(int cardSN, string strEntName, double dCenx, double dCeny, double dScaleX, double dScaleY);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_MirrorEnt", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_MirrorEnt(int cardSN, string strEntName, double dCenx, double dCeny, bool bMirrorX, bool bMirrorY);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_RotateEnt", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_RotateEnt(int cardSN, string pEntName, double dCenv, double dCeny, double dAngle);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_CopyEnt", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_CopyEnt(int cardSN, string strEntName, string strNewEntName);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GetEntityCount", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_GetEntityCount(int cardSN);   // 返回对象个数

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GetEntityName", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_GetEntityName(int cardSN, int nEntityIndex, StringBuilder szEntName);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_SetEntityName", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_SetEntityName(int cardSN, int nEntIndex, string strEntName);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_ChangeEntName", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_ChangeEntName(int cardSN, string strEntName, string strNewName);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_ChangeEntPenNo", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_ChangeEntPenNo(int cardSN, string strEntName, int nPenNo);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GroupEnt", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_GroupEnt(int cardSN, string pEntName1, string pEntName2, string pEntNameNew, int pen);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GroupEnt2", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_GroupEnt2(int cardSN, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] strEntName, int nEntCount, string strGroupName, int nGroupPen);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_UnGroupEnt", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_UnGroupEnt(int cardSN, string strGroupEntName);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_SetEntAllChildPen", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lmc1_SetEntAllChildPen(int cardSN, string strEntName, int nPenNo);
        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GetPenNumberFromEnt", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_GetPenNumberFromEnt(int cardSN, string strEntName);

        #endregion

        #region 文本相关

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_ChangeTextByName", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_ChangeTextByName(int cardSN, string strTextName, string strTextNew);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GetTextByName", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_GetTextByName(int cardSN, string strEntName, StringBuilder Text);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_TextResetSn", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_TextResetSn(int cardSN, string strEntName);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GetTextEntParam3", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_GetTextEntParam3(int cardSN, string EntName,
            StringBuilder FontName, ref int TextSpaceMode, ref double TextSpace,
            ref double CharHeight, ref double CharWidthRatio, ref double CharAngle,
            ref double CharSpace, ref double LineSpace, ref double spaceWidthRatio, ref bool EqualCharWidth);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_SetTextEntParam3", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_SetTextEntParam3(int cardSN, string EntName, string fontname,
            int TextSpaceMode, double TextSpace, double CharHeight, double CharWidthRatio,
            double CharAngle, double CharSpace, double LineSpace, double spaceWidthRatio,
            bool EqualCharWidth);

        #endregion

        #region 端口相关

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_ReadPort", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_ReadPort(int cardSN, ref ushort data);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_WritePort", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_WritePort(int cardSN, ushort data);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GetOutPort", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_GetOutPort(int cardSN, ref ushort data);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_LaserOn", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_LaserOn(int cardSN, bool bOpen);

        #endregion

        #region 笔号相关

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GetPenParam", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_GetPenParam(int nCard, int nPenNo,
            ref int nMarkLoop, ref double dMarkSpeed, ref double dPowerRatio, ref double dCurrent,
            ref int nFreq, ref double dQPulseWidth, ref int nStartTC, ref int nLaserOffTC,
            ref int nEndTC, ref int nPolyTC, ref double dJumpSpeed, ref int nJumpPosTC,
            ref int nJumpDistTC, ref double dEndComp, ref double dAccDist, ref double dPointTime,
            ref bool bPulsePointMode, ref int nPulseNum, ref double dFlySpeed);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_SetPenParam", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_SetPenParam(int nCard, int nPenNo,
            int nMarkLoop, double dMarkSpeed, double dPowerRatio, double dCurrent,
            int nFreq, double dQPulseWidth, int nStartTC, int nLaserOffTC,
            int nEndTC, int nPolyTC, double dJumpSpeed, int nJumpPosTC,
            int nJumpDistTC, double dEndComp, double dAccDist, double dPointTime,
            bool bPulsePointMode, int nPulseNum, double dFlySpeed);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_SetPenDisableState", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_SetPenDisableState(int nCard, int nPenNo, bool bDisableMark);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GetPenDisableState", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_GetPenDisableState(int nCard, int nPenNo, ref bool bDisableMark);

        #endregion

        #region 填充相关

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_SetHatchParam", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_SetHatchParam(int nCard, bool bEnableContour,
            int bEnableHatch1, int nPenNo1, int nHatchAttrib1, double dHatchEdgeDist1,
            double dHatchLineDist1, double dHatchStartOffset1, double dHatchEndOffset1, double dHatchAngle1,
            int bEnableHatch2, int nPenNo2, int nHatchAttrib2, double dHatchEdgeDist2,
            double dHatchLineDist2, double dHatchStartOffset2, double dHatchEndOffset2, double dHatchAngle2);


        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_SetHatchParam2", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_SetHatchParam2(int nCard, bool bEnableContour,
            int nParamIndex, int bEnableHatch, int nPenNo, int nHatchType,
            bool bHatchAllCalc, bool bHatchEdge, bool bHatchAverageLine,
            double dHatchAngle, double dHatchLineDist, double dHatchEdgeDist,
            double dHatchStartOffset, double dHatchEndOffset, double dHatchLineReduction,
            double dHatchLoopDist, int nEdgeLoop, bool nHatchLoopRev,
            bool bHatchAutoRotate, double dHatchRotateAngle);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_SetHatchParam3", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_SetHatchParam3(int nCard, bool bEnableContour,
            int nParamIndex, int bEnableHatch, int nPenNo, int nHatchType,
            bool bHatchAllCalc, bool bHatchEdge, bool bHatchAverageLine,
            double dHatchAngle, double dHatchLineDist, double dHatchEdgeDist,
            double dHatchStartOffset, double dHatchEndOffset, double dHatchLineReduction,
            double dHatchLoopDist, int nEdgeLoop, bool nHatchLoopRev,
            bool bHatchAutoRotate, double dHatchRotateAngle, bool bHatchCross);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_SetHatchEntParam2", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_SetHatchEntParam2(int nCard, string pHatchName,
            bool bEnableContour, int nParamIndex, int bEnableHatch, bool bContourFirst,
            int nPenNo, int nHatchType, bool bHatchAllCalc, bool bHatchEdge, bool bHatchAverageLine,
            double dHatchAngle, double dHatchLineDist, double dHatchEdgeDist,
            double dHatchStartOffset, double dHatchEndOffset, double dHatchLineReduction,
            double dHatchLoopDist, int nEdgeLoop, bool nHatchLoopRev, bool bHatchAutoRotate,
            double dHatchRotateAngle, bool bHatchCrossMode, int dCycCount);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GetHatchEntParam2", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_GetHatchEntParam2(int nCard, string pHatchName,
            ref bool bEnableContour, int nParamIndex, ref int bEnableHatch, ref bool bContourFirst,
            ref int nPenNo, ref int nHatchType, ref bool bHatchAllCalc, ref bool bHatchEdge,
            ref bool bHatchAverageLine, ref double dHatchAngle, ref double dHatchLineDist,
            ref double dHatchEdgeDist, ref double dHatchStartOffset, ref double dHatchEndOffset,
            ref double dHatchLineReduction, ref double dHatchLoopDist, ref int nEdgeLoop,
            ref bool nHatchLoopRev, ref bool bHatchAutoRotate, ref double dHatchRotateAngle,
            ref bool bHatchCrossMode, ref int dCycCount);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_HatchEnt", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_HatchEnt(int nCard, string strEntName, string strHatchEntName);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_UnHatchEnt", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_UnHatchEnt(int nCard, string strHatchEntName);

        #endregion

        #region 添加删除对象

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_ClearEntLib", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_ClearEntLib(int cardSN);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_DeleteEnt", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_DeleteEnt(int cardSN, string strEntName);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_AddTextToLib", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_AddTextToLib(int cardSN, string pStr, string pEntName,
            double dPosX, double dPosY, double dPosZ, int nAlign, double dTextRotateAngle,
            int nPenNo, bool bHatchText);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_AddCurveToLib", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_AddCurveToLib(int cardSN, [MarshalAs(UnmanagedType.LPArray)] double[,] ptBuf, int ptNum, string pEntName, int nPenNo, int bHatch);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_AddCircleToLib", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_AddCircleToLib(int cardSN, double ptCenX, double ptCenY, double dRadius, string pEntName, int nPenNo);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_AddFileToLib", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_AddFileToLib(int cardSN, string pFileName, string pEntName,
            double dPosX, double dPosY, double dPosZ, int nAlign, double dRatio, int nPenNo, bool bHatchFile);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_AddBarCodeToLib", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_AddBarCodeToLib(int cardSN, string pStr, string pEntName,
            double dPosX, double dPosY, double dPosZ, int nAlign, int nPenNo, bool bHatchText,
            int nBarcodeType, ushort wBarCodeAttrib, double dHeight, double dNarrowWidth,
            [MarshalAs(UnmanagedType.LPArray)] double[] dBarWidthScale,
            [MarshalAs(UnmanagedType.LPArray)] double[] dSpaceWidthScale,
            double dMidCharSpaceScale, double dQuietLeftScale, double dQuietMidScale,
            double dQuietRightScale, double dQuietTopScale, double dQuietBottomScale,
            int nRow, int nCol, int nCheckLevel, int nSizeMode,
            double dTextHeight, double dTextWidth, double dTextOffsetX, double dTextOffsetY,
            double dTextSpace, double dDiameter, string pTextFontName);

        #endregion

        #region 扩展轴相关

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_Reset", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_ResetAxis(int cardSN, bool bEnAxis0, bool bEnAxis1);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_AxisCorrectOrigin", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_AxisCorrectOrigin(int cardSN, int axis);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_AxisMoveTo", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lmc1_AxisMoveTo(int cardSN, int axis, double GoalPos);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GetAxisCoor", CallingConvention = CallingConvention.Cdecl)]
        public static extern double lmc1_GetAxisCoor(int cardSN, int axis);

        #endregion

        // ========== 公共方法（多卡支持） ==========

        /// <summary>获取所有有效卡序号，返回卡数量，SN数组长度为8，无效为-1</summary>
        public static int GetAllCardSN(out int[] snArray)
        {
            snArray = new int[8];
            int ret = lmc1_GetAllCardSN(snArray);
            return ret;
        }

        /// <summary>初始化指定卡（建议先调用 GetAllCardSN 获取有效卡号）</summary>
        public static int InitializeCard(string ezcadPath, bool bTestMode, IntPtr hWnd)
        {
            return lmc1_Initial(ezcadPath, bTestMode, hWnd);
        }

        // ==================== 预览图辅助方法（基于多头库） ====================
        /// <summary>
        /// 获取指定卡当前数据库的预览图像（支持逻辑坐标，但多头库仅返回原始大小图像）
        /// </summary>
        [DllImport("gdi32.dll")]
        private static extern int GetBitmapBits(IntPtr hbitmap, int cbBuffer, byte[] lpvBits);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel, byte[] lpvBits);

        /* public static Image lmc1_GetCurPreviewImage3(int cardSN, IntPtr hwnd, int bmpwidth, int bmpheight)
         {
             IntPtr hBmp = lmc1_GetPrevBitmap(cardSN, hwnd, bmpwidth, bmpheight);
             if (hBmp == IntPtr.Zero) return null;

             try
             {
                 // 获取位图信息
                 BITMAP bmpInfo = new BITMAP();
                 GetObject(hBmp, Marshal.SizeOf(typeof(BITMAP)), ref bmpInfo);

                 if (bmpInfo.bmWidth == 0 || bmpInfo.bmHeight == 0)
                     return null;

                 // 获取位图数据
                 int bitsSize = bmpInfo.bmWidthBytes * bmpInfo.bmHeight;
                 byte[] bits = new byte[bitsSize];
                 GetBitmapBits(hBmp, bitsSize, bits);

                 // 重新创建位图
                 IntPtr newHBmp = CreateBitmap(bmpInfo.bmWidth, bmpInfo.bmHeight,
                                                bmpInfo.bmPlanes, bmpInfo.bmBitsPixel, bits);

                 if (newHBmp == IntPtr.Zero)
                     return null;

                 Image img = Image.FromHbitmap(newHBmp);
                 DeleteObject(newHBmp);
                 return img;
             }
             catch (Exception ex)
             {
              //   Debug.WriteLine($"创建图像失败: {ex.Message}");
                 return null;
             }
             finally
             {
                 DeleteObject(hBmp);
             }
         }*/
        public static Image lmc1_GetCurPreviewImage3(int cardSN, IntPtr hwnd, int bmpwidth, int bmpheight)
        {
            IntPtr hBmp = lmc1_GetPrevBitmap(cardSN, hwnd, bmpwidth, bmpheight);
            if (hBmp == IntPtr.Zero) return null;

            try
            {
                // 先创建 Bitmap，如果成功则复制一份
                using (Bitmap tempBmp = Bitmap.FromHbitmap(hBmp))
                {
                    return new Bitmap(tempBmp);
                }
            }
            catch (Exception ex)
            {
                // WriteLine($"FromHbitmap 失败: {ex.Message}");
                return null;
            }
            finally
            {
                // 删除原始 HBITMAP
                DeleteObject(hBmp);
            }
        }

        /// <summary>
        /// 获取指定卡指定对象的预览图像
        /// </summary>
        public static Image lmc1_GetEzdFileEntityPrevBitmap(int cardSN, string entName)
        {
            try
            {
                IntPtr hBmp = lmc1_GetEzdFilePrevBitmap(cardSN, entName);
                if (hBmp == IntPtr.Zero) return null;
                Image img = Image.FromHbitmap(hBmp);
                DeleteObject(hBmp);
                return img;
            }
            catch
            {
                return null;
            }
        }

        // ==================== 对象名称与数量辅助方法 ====================
        /* public static int lmc1_GetEntityCount(int cardSN)
         {
             return lmc1_GetEntityCount(cardSN);
         }*/

        public static string lmc1_GetEntityNameByIndex(int cardSN, int index)
        {
            StringBuilder sb = new StringBuilder(256);
            int ret = lmc1_GetEntityName(cardSN, index, sb);
            return ret == 0 ? sb.ToString() : "";
        }

        /* public static int lmc1_GetPenNumberFromEnt(int cardSN, string entName)
         {
             return lmc1_GetPenNumberFromEnt(cardSN, entName);
         }*/

        // ==================== 笔号参数辅助方法（带卡号） ====================
        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GetPenParam4", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int lmc1_GetPenParam4Native(int cardSN, int nPenNo, StringBuilder pStrName,
            ref int clr, ref bool bDisableMark, ref bool bUseDefParam, ref int nMarkLoop,
            ref double dMarkSpeed, ref double dPowerRatio, ref double dCurrent, ref int nFreq,
            ref double dQPulseWidth, ref int nStartTC, ref int nLaserOffTC, ref int nEndTC,
            ref int nPolyTC, ref double dJumpSpeed, ref int nMinJumpDelayTCUs, ref int nMaxJumpDelayTCUs,
            ref double dJumpLengthLimit, ref double dPointTimeMs, ref bool nSpiSpiContinueMode,
            ref int nSpiWave, ref int nYagMarkMode, ref bool bPulsePointMode, ref int nPulseNum,
            ref bool bEnableACCMode, ref double dEndComp, ref double dAccDist, ref double dBreakAngle,
            ref bool bWobbleMode, ref double bWobbleDiameter, ref double bWobbleDist);

        [DllImport("MultiMarkEzd", EntryPoint = "lmc1_SetPenParam4", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int lmc1_SetPenParam4Native(int cardSN, int nPenNo, string pStrName, int clr,
            bool bDisableMark, bool bUseDefParam, int nMarkLoop, double dMarkSpeed, double dPowerRatio,
            double dCurrent, int nFreq, double dQPulseWidth, int nStartTC, int nLaserOffTC,
            int nEndTC, int nPolyTC, double dJumpSpeed, int nMinJumpDelayTCUs, int nMaxJumpDelayTCUs,
            double dJumpLengthLimit, double dPointTimeMs, bool nSpiSpiContinueMode, int nSpiWave,
            int nYagMarkMode, bool bPulsePointMode, int nPulseNum, bool bEnableACCMode,
            double dEndComp, double dAccDist, double dBreakAngle, bool bWobbleMode,
            double bWobbleDiameter, double bWobbleDist);
        public static int lmc1_GetPenParam4(int cardSN, int nPenNo, StringBuilder pStrName,
            ref int clr, ref bool bDisableMark, ref bool bUseDefParam, ref int nMarkLoop,
            ref double dMarkSpeed, ref double dPowerRatio, ref double dCurrent, ref int nFreq,
            ref double dQPulseWidth, ref int nStartTC, ref int nLaserOffTC, ref int nEndTC,
            ref int nPolyTC, ref double dJumpSpeed, ref int nMinJumpDelayTCUs, ref int nMaxJumpDelayTCUs,
            ref double dJumpLengthLimit, ref double dPointTimeMs, ref bool nSpiSpiContinueMode,
            ref int nSpiWave, ref int nYagMarkMode, ref bool bPulsePointMode, ref int nPulseNum,
            ref bool bEnableACCMode, ref double dEndComp, ref double dAccDist, ref double dBreakAngle,
            ref bool bWobbleMode, ref double bWobbleDiameter, ref double bWobbleDist)
        {
            return lmc1_GetPenParam4Native(cardSN, nPenNo, pStrName, ref clr, ref bDisableMark,
                ref bUseDefParam, ref nMarkLoop, ref dMarkSpeed, ref dPowerRatio, ref dCurrent,
                ref nFreq, ref dQPulseWidth, ref nStartTC, ref nLaserOffTC, ref nEndTC, ref nPolyTC,
                ref dJumpSpeed, ref nMinJumpDelayTCUs, ref nMaxJumpDelayTCUs, ref dJumpLengthLimit,
                ref dPointTimeMs, ref nSpiSpiContinueMode, ref nSpiWave, ref nYagMarkMode,
                ref bPulsePointMode, ref nPulseNum, ref bEnableACCMode, ref dEndComp, ref dAccDist,
                ref dBreakAngle, ref bWobbleMode, ref bWobbleDiameter, ref bWobbleDist);
        }

        public static int lmc1_SetPenParam4(int cardSN, int nPenNo, string pStrName, int clr,
            bool bDisableMark, bool bUseDefParam, int nMarkLoop, double dMarkSpeed, double dPowerRatio,
            double dCurrent, int nFreq, double dQPulseWidth, int nStartTC, int nLaserOffTC,
            int nEndTC, int nPolyTC, double dJumpSpeed, int nMinJumpDelayTCUs, int nMaxJumpDelayTCUs,
            double dJumpLengthLimit, double dPointTimeMs, bool nSpiSpiContinueMode, int nSpiWave,
            int nYagMarkMode, bool bPulsePointMode, int nPulseNum, bool bEnableACCMode,
            double dEndComp, double dAccDist, double dBreakAngle, bool bWobbleMode,
            double bWobbleDiameter, double bWobbleDist)
        {
            return lmc1_SetPenParam4Native(cardSN, nPenNo, pStrName, clr, bDisableMark, bUseDefParam,
                nMarkLoop, dMarkSpeed, dPowerRatio, dCurrent, nFreq, dQPulseWidth, nStartTC,
                nLaserOffTC, nEndTC, nPolyTC, dJumpSpeed, nMinJumpDelayTCUs, nMaxJumpDelayTCUs,
                dJumpLengthLimit, dPointTimeMs, nSpiSpiContinueMode, nSpiWave, nYagMarkMode,
                bPulsePointMode, nPulseNum, bEnableACCMode, dEndComp, dAccDist, dBreakAngle,
                bWobbleMode, bWobbleDiameter, bWobbleDist);
        }
        public static int CloseLibrary() => lmc1_Close();

        #region 旧版兼容方法（Obsolete，内部使用 cardSN=0）
        [Obsolete("请使用带卡序号的重载")]
        public static int Initialize(string path, bool bTest) => InitializeCard(path, bTest, IntPtr.Zero);
        [Obsolete("请使用带卡序号的重载")]
        public static int Mark(bool Fly) => lmc1_Mark(0, Fly);
        [Obsolete("请使用带卡序号的重载")]
        public static int MarkEntity(string EntName) => lmc1_MarkEntity(0, EntName);
        [Obsolete("请使用带卡序号的重载")]
        public static int MarkEntityFly(string EntName) => lmc1_MarkEntityFly(0, EntName);
        [Obsolete("请使用带卡序号的重载")]
        public static int MarkLine(double X1, double Y1, double X2, double Y2, int Pen) => lmc1_MarkLine(0, X1, Y1, X2, Y2, Pen);
        [Obsolete("请使用带卡序号的重载")]
        public static int MarkPoint(double X, double Y, double Delay, int Pen) => lmc1_MarkPoint(0, X, Y, Delay, Pen);
        [Obsolete("请使用带卡序号的重载")]
        public static int StopMark() => lmc1_StopMark(0);
        [Obsolete("请使用带卡序号的重载")]
        public static int RedMark() => lmc1_RedLightMark(0);
        [Obsolete("请使用带卡序号的重载")]
        public static int RedMarkContour() => lmc1_RedLightMark(0);
        [Obsolete("请使用带卡序号的重载")]
        public static int RedLightMarkByEnt(string EntName, bool bContour) => lmc1_RedLightMarkContourByEnt(0, EntName);
        [Obsolete("请使用带卡序号的重载")]
        public static int LoadEzdFile(string FileName) => lmc1_LoadEzdFile(0, FileName);
        [Obsolete("请使用带卡序号的重载")]
        public static int SaveEntLibToFile(string strFileName) => lmc1_SaveEntLibToFile(0, strFileName);
        [Obsolete("请使用带卡序号的重载")]
        public static ushort GetEntityCount() => (ushort)lmc1_GetEntityCount(0);
        [Obsolete("请使用带卡序号的重载")]
        public static string GetEntityNameByIndex(int nEntityIndex)
        {
            StringBuilder sb = new StringBuilder(256);
            lmc1_GetEntityName(0, nEntityIndex, sb);
            return sb.ToString();
        }
        [Obsolete("请使用带卡序号的重载")]
        public static int SetEntityNameByIndex(int nEntityIndex, string entname) => lmc1_SetEntityName(0, nEntityIndex, entname);
        [Obsolete("请使用带卡序号的重载")]
        public static int ChangeEntName(string strEntName, string strNewEntName) => lmc1_ChangeEntName(0, strEntName, strNewEntName);
        [Obsolete("请使用带卡序号的重载")]
        public static int MoveEnt(string strEntName, double dMovex, double dMovey) => lmc1_MoveEnt(0, strEntName, ref dMovex, ref dMovey);
        [Obsolete("请使用带卡序号的重载")]
        public static int ScaleEnt(string strEntName, double dCenx, double dCeny, double dScaleX, double dScaleY) => lmc1_ScaleEnt(0, strEntName, dCenx, dCeny, dScaleX, dScaleY);
        [Obsolete("请使用带卡序号的重载")]
        public static int RotateEnt(string strEntName, double dCenx, double dCeny, double dAngle) => lmc1_RotateEnt(0, strEntName, dCenx, dCeny, dAngle);
        [Obsolete("请使用带卡序号的重载")]
        public static int CopyEnt(string strEntName, string strNewEntName) => lmc1_CopyEnt(0, strEntName, strNewEntName);
        [Obsolete("请使用带卡序号的重载")]
        public static int GroupEnt(string strEntName1, string strEntName2, string strGroupName, int nGroupPen) => lmc1_GroupEnt(0, strEntName1, strEntName2, strGroupName, nGroupPen);
        [Obsolete("请使用带卡序号的重载")]
        public static int lmc1_GroupEnt2(string[] strEntName, int nEntCount, string strGroupName, int nGroupPen) => lmc1_GroupEnt2(0, strEntName, nEntCount, strGroupName, nGroupPen);
        [Obsolete("请使用带卡序号的重载")]
        public static int UnGroupEnt(string strGroupName) => lmc1_UnGroupEnt(0, strGroupName);
        [Obsolete("请使用带卡序号的重载")]
        public static int GetPenNumberFromEnt(string strEntName) => lmc1_GetPenNumberFromEnt(0, strEntName);
        [Obsolete("请使用带卡序号的重载")]
        public static int ChangeTextByName(string EntName, string NewText) => lmc1_ChangeTextByName(0, EntName, NewText);
        [Obsolete("请使用带卡序号的重载")]
        public static int GetTextByName(string strEntName, StringBuilder Text) => lmc1_GetTextByName(0, strEntName, Text);
        [Obsolete("请使用带卡序号的重载")]
        public static int TextResetSn(string TextName) => lmc1_TextResetSn(0, TextName);
        [Obsolete("请使用带卡序号的重载")]
        public static int ReadPort(ref ushort data) => lmc1_ReadPort(0, ref data);
        [Obsolete("请使用带卡序号的重载")]
        public static int WritePort(ushort data) => lmc1_WritePort(0, data);
        [Obsolete("请使用带卡序号的重载")]
        public static int GetOutPort(ref ushort data) => lmc1_GetOutPort(0, ref data);
        [Obsolete("请使用带卡序号的重载")]
        public static int LaserOn(bool bOpen) => lmc1_LaserOn(0, bOpen);
        [Obsolete("请使用带卡序号的重载")]
        public static int SetDevCfg() => lmc1_SetDevCfg(0);
        [Obsolete("请使用带卡序号的重载")]
        public static int SetDevCfg2(bool bAxisShow0, bool bAxisShow1) => lmc1_SetDevCfg2(0, bAxisShow0, bAxisShow1);
        [Obsolete("请使用带卡序号的重载")]
        public static int SetRotateMoveParam(double dMoveX, double dMoveY, double dCenterX, double dCenterY, double dRotateAng) => lmc1_SetRotateMoveParam(0, dMoveX, dMoveY, dCenterX, dCenterY, dRotateAng);
        [Obsolete("请使用带卡序号的重载")]
        public static int GetEntSize(string strEntName, ref double dMinx, ref double dMiny, ref double dMaxx, ref double dMaxy, ref double dz) => lmc1_GetEntSize(0, strEntName, ref dMinx, ref dMiny, ref dMaxx, ref dMaxy, ref dz);
        [Obsolete("请使用带卡序号的重载")]
        public static int MirrorEnt(string strEntName, double dCenx, double dCeny, bool bMirrorX, bool bMirrorY) => lmc1_MirrorEnt(0, strEntName, dCenx, dCeny, bMirrorX, bMirrorY);
        [Obsolete("请使用带卡序号的重载")]
        public static int GotoPos(double x, double y) => lmc1_GotoPos(0, x, y);
        [Obsolete("请使用带卡序号的重载")]
        public static int GetCurCoor(ref double x, ref double y) => lmc1_GetCurCoor(0, ref x, ref y);
        [Obsolete("请使用带卡序号的重载")]
        public static int GetPenParam(int nPenNo, ref int nMarkLoop, ref double dMarkSpeed, ref double dPowerRatio, ref double dCurrent, ref int nFreq, ref double dQPulseWidth, ref int nStartTC, ref int nLaserOffTC, ref int nEndTC, ref int nPolyTC, ref double dJumpSpeed, ref int nJumpPosTC, ref int nJumpDistTC, ref double dEndComp, ref double dAccDist, ref double dPointTime, ref bool bPulsePointMode, ref int nPulseNum, ref double dFlySpeed) => lmc1_GetPenParam(0, nPenNo, ref nMarkLoop, ref dMarkSpeed, ref dPowerRatio, ref dCurrent, ref nFreq, ref dQPulseWidth, ref nStartTC, ref nLaserOffTC, ref nEndTC, ref nPolyTC, ref dJumpSpeed, ref nJumpPosTC, ref nJumpDistTC, ref dEndComp, ref dAccDist, ref dPointTime, ref bPulsePointMode, ref nPulseNum, ref dFlySpeed);
        [Obsolete("请使用带卡序号的重载")]
        public static int SetPenParam(int nPenNo, int nMarkLoop, double dMarkSpeed, double dPowerRatio, double dCurrent, int nFreq, double dQPulseWidth, int nStartTC, int nLaserOffTC, int nEndTC, int nPolyTC, double dJumpSpeed, int nJumpPosTC, int nJumpDistTC, double dEndComp, double dAccDist, double dPointTime, bool bPulsePointMode, int nPulseNum, double dFlySpeed) => lmc1_SetPenParam(0, nPenNo, nMarkLoop, dMarkSpeed, dPowerRatio, dCurrent, nFreq, dQPulseWidth, nStartTC, nLaserOffTC, nEndTC, nPolyTC, dJumpSpeed, nJumpPosTC, nJumpDistTC, dEndComp, dAccDist, dPointTime, bPulsePointMode, nPulseNum, dFlySpeed);
        [Obsolete("请使用带卡序号的重载")]
        public static int SetPenDisableState(int nPenNo, bool bDisableMark) => lmc1_SetPenDisableState(0, nPenNo, bDisableMark);
        [Obsolete("请使用带卡序号的重载")]
        public static int GetPenDisableState(int nPenNo, ref bool bDisableMark) => lmc1_GetPenDisableState(0, nPenNo, ref bDisableMark);
        [Obsolete("请使用带卡序号的重载")]
        public static int SetHatchParam(bool bEnableContour, int bEnableHatch1, int nPenNo1, int nHatchAttrib1, double dHatchEdgeDist1, double dHatchLineDist1, double dHatchStartOffset1, double dHatchEndOffset1, double dHatchAngle1, int bEnableHatch2, int nPenNo2, int nHatchAttrib2, double dHatchEdgeDist2, double dHatchLineDist2, double dHatchStartOffset2, double dHatchEndOffset2, double dHatchAngle2) => lmc1_SetHatchParam(0, bEnableContour, bEnableHatch1, nPenNo1, nHatchAttrib1, dHatchEdgeDist1, dHatchLineDist1, dHatchStartOffset1, dHatchEndOffset1, dHatchAngle1, bEnableHatch2, nPenNo2, nHatchAttrib2, dHatchEdgeDist2, dHatchLineDist2, dHatchStartOffset2, dHatchEndOffset2, dHatchAngle2);
        [Obsolete("请使用带卡序号的重载")]
        public static int SetHatchEntParam2(string HatchName, bool bEnableContour, int nParamIndex, int bEnableHatch, bool bContourFirst, int nPenNo, int nHatchType, bool bHatchAllCalc, bool bHatchEdge, bool bHatchAverageLine, double dHatchAngle, double dHatchLineDist, double dHatchEdgeDist, double dHatchStartOffset, double dHatchEndOffset, double dHatchLineReduction, double dHatchLoopDist, int nEdgeLoop, bool nHatchLoopRev, bool bHatchAutoRotate, double dHatchRotateAngle, bool bHatchCrossMode, int dCycCount) => lmc1_SetHatchEntParam2(0, HatchName, bEnableContour, nParamIndex, bEnableHatch, bContourFirst, nPenNo, nHatchType, bHatchAllCalc, bHatchEdge, bHatchAverageLine, dHatchAngle, dHatchLineDist, dHatchEdgeDist, dHatchStartOffset, dHatchEndOffset, dHatchLineReduction, dHatchLoopDist, nEdgeLoop, nHatchLoopRev, bHatchAutoRotate, dHatchRotateAngle, bHatchCrossMode, dCycCount);
        [Obsolete("请使用带卡序号的重载")]
        public static int GetHatchEntParam2(string HatchName, ref bool bEnableContour, int nParamIndex, ref int bEnableHatch, ref bool bContourFirst, ref int nPenNo, ref int nHatchType, ref bool bHatchAllCalc, ref bool bHatchEdge, ref bool bHatchAverageLine, ref double dHatchAngle, ref double dHatchLineDist, ref double dHatchEdgeDist, ref double dHatchStartOffset, ref double dHatchEndOffset, ref double dHatchLineReduction, ref double dHatchLoopDist, ref int nEdgeLoop, ref bool nHatchLoopRev, ref bool bHatchAutoRotate, ref double dHatchRotateAngle, ref bool bHatchCrossMode, ref int dCycCount) => lmc1_GetHatchEntParam2(0, HatchName, ref bEnableContour, nParamIndex, ref bEnableHatch, ref bContourFirst, ref nPenNo, ref nHatchType, ref bHatchAllCalc, ref bHatchEdge, ref bHatchAverageLine, ref dHatchAngle, ref dHatchLineDist, ref dHatchEdgeDist, ref dHatchStartOffset, ref dHatchEndOffset, ref dHatchLineReduction, ref dHatchLoopDist, ref nEdgeLoop, ref nHatchLoopRev, ref bHatchAutoRotate, ref dHatchRotateAngle, ref bHatchCrossMode, ref dCycCount);
        [Obsolete("请使用带卡序号的重载")]
        public static int HatchEnt(string strEntName, string strHatchEntName) => lmc1_HatchEnt(0, strEntName, strHatchEntName);
        [Obsolete("请使用带卡序号的重载")]
        public static int UnHatchEnt(string strHatchEntName) => lmc1_UnHatchEnt(0, strHatchEntName);
        [Obsolete("请使用带卡序号的重载")]
        public static int lmc1_AddCircleToLib(double ptCenX, double ptCenY, double dRadius, string pEntName, int nPenNo) => lmc1_AddCircleToLib(0, ptCenX, ptCenY, dRadius, pEntName, nPenNo);
        [Obsolete("请使用带卡序号的重载")]
        public static int ResetAxis(bool bEnAxis1, bool bEnAxis2) => lmc1_ResetAxis(0, bEnAxis1, bEnAxis2);
        [Obsolete("请使用带卡序号的重载")]
        public static int AxisMoveTo(int axis, double GoalPos) => lmc1_AxisMoveTo(0, axis, GoalPos);
        [Obsolete("请使用带卡序号的重载")]
        public static int AxisGoHome(int axis) => lmc1_AxisCorrectOrigin(0, axis);
        [Obsolete("请使用带卡序号的重载")]
        public static double GetAxisCoor(int axis) => lmc1_GetAxisCoor(0, axis);
        #endregion

    }

}

