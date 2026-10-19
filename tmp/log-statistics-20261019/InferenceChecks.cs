using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Cognex.VisionPro;
using CommonCls;
public class InferenceChecks
{
    private bool has_model_init = true;
    private List<string> _classNames = new List<string>();
    private int nativeCode, nativeBoxes;
    private bool throwNative;
    private int Det_ModelPredict(byte[] bytes, int w, int h, int channels, IntPtr result, int[] boxes, ref byte label)
    {
        if (throwNative) throw new Exception("simulated native failure");
        boxes[0] = nativeBoxes;
        label = (byte)'x';
        float[] values = new float[600];
        values[1] = .1f;
        Marshal.Copy(values, 0, result, values.Length);
        return nativeCode;
    }
        public byte[] ConvertGrayToBGR(byte[] grayData, int width, int height)
        {
            int totalPixels = width * height;
            byte[] bgrData = new byte[totalPixels * 3];

            // 将灰度数据同时复制到三个位置
            for (int channel = 0; channel < 3; channel++)
            {
                Buffer.BlockCopy(
                    grayData,                    // 源数组
                    0,                           // 源起始位置
                    bgrData,                     // 目标数组
                    totalPixels * channel,       // 目标起始位置（每个通道偏移totalPixels）
                    totalPixels                  // 复制的字节数
                );
            }

            // 注意：这种方法得到的是[BBB...GGG...RRR...]的布局
            // 需要重新排列为[BGR BGR BGR...]的布局
            return ReorderBGRChannels(bgrData, totalPixels);
        }

        private byte[] ReorderBGRChannels(byte[] planarBGR, int totalPixels)
        {
            byte[] interleavedBGR = new byte[totalPixels * 3];

            for (int i = 0; i < totalPixels; i++)
            {
                interleavedBGR[i * 3] = planarBGR[i];                    // B
                interleavedBGR[i * 3 + 1] = planarBGR[i + totalPixels];  // G
                interleavedBGR[i * 3 + 2] = planarBGR[i + totalPixels * 2]; // R
            }

            return interleavedBGR;
        }

        public bool det_infer_img_ByteGray(byte[] grayData, int width, int height, float fThreshold, float fThresholdPS, ref DLResult dlR)
        {
            bool inferenceSucceeded;
            return det_infer_img_ByteGray(grayData, width, height, fThreshold, fThresholdPS, ref dlR, out inferenceSucceeded);
        }

        public bool det_infer_img_ByteGray(byte[] grayData, int width, int height, float fThreshold, float fThresholdPS, ref DLResult dlR, out bool inferenceSucceeded)
        {
            inferenceSucceeded = false;
            float[] resultlist = new float[600];
            GCHandle resultHandle = GCHandle.Alloc(resultlist, GCHandleType.Pinned);
            IntPtr results = resultHandle.AddrOfPinnedObject();
            int[] boxesInfo = new int[1];
            byte[] labellist = new byte[1000];
            bool bSure = false;

            try
            {
                if (!has_model_init || grayData == null || width <= 0 || height <= 0 ||
                    grayData.Length < checked(width * height))
                    throw new InvalidOperationException("推理模型或本轮图像无效");
                byte[] bgrData = ConvertGrayToBGR(grayData, width, height);
                int returnCode = Det_ModelPredict(bgrData, width, height, 3, results, boxesInfo, ref labellist[0]);
                if (returnCode != 0) throw new InvalidOperationException("Det_ModelPredict returnCode=" + returnCode);
                if (boxesInfo[0] < 0 || boxesInfo[0] > resultlist.Length / 6)
                    throw new InvalidOperationException("推理框数量无效: " + boxesInfo[0]);

                string strGet = Encoding.Default.GetString(labellist, 0, labellist.Length);
                string[] predict_Label_List = strGet.Split(' ');

                for (int i = 0; i < boxesInfo[0]; i++)
                {
                    float score = resultlist[i * 6 + 1];
                    if (score > fThreshold)
                    {
                        CogRectangleAffine cogRect = new CogRectangleAffine();
                        cogRect.CenterX = resultlist[i * 6 + 2] + resultlist[i * 6 + 4] / 2;
                        cogRect.CenterY = resultlist[i * 6 + 3] + resultlist[i * 6 + 5] / 2;
                        cogRect.SideXLength = resultlist[i * 6 + 4];
                        cogRect.SideYLength = resultlist[i * 6 + 5];

                        string labelName = predict_Label_List[i].Trim();
                        int classIndex = _classNames.IndexOf(labelName);
                        if (classIndex >= 0)
                        {
                            // 类型 ID 从 1 开始（便于区分）
                            int typeId = classIndex + 1;
                            dlR.AL_Type.Add(typeId);
                            dlR.AL_Rect.Add(cogRect);
                            dlR.AL_score.Add(score);
                        }
                        else
                        {
                            // 未知类别：可记录日志或跳过
                            Debug.WriteLine($"未识别的类别名称: {labelName}");
                        }
                    }
                    bSure = true;
                }
                inferenceSucceeded = true;
            }
            catch (Exception e)
            {
                RuntimeLog.Model("[Infer] failed: " + e);
                return false;
            }
            finally
            {
                if (resultHandle.IsAllocated) resultHandle.Free();
            }
            return bSure;
        }

        int[] bInfering = new int[2];
        // 彩色图推理
        public bool det_infer_img_ByteColor(byte[] bgrData, int width, int height, float fThreshold, float fThresholdPS, ref DLResult dlR)
        {
            bool inferenceSucceeded;
            return det_infer_img_ByteColor(bgrData, width, height, fThreshold, fThresholdPS, ref dlR, out inferenceSucceeded);
        }

        public bool det_infer_img_ByteColor(byte[] bgrData, int width, int height, float fThreshold, float fThresholdPS, ref DLResult dlR, out bool inferenceSucceeded)
        {
            inferenceSucceeded = false;
            float[] resultlist = new float[600];
            GCHandle resultHandle = GCHandle.Alloc(resultlist, GCHandleType.Pinned);
            IntPtr results = resultHandle.AddrOfPinnedObject();
            int[] boxesInfo = new int[1];
            byte[] labellist = new byte[1000];
            bool bSure = false;

            try
            {
                if (!has_model_init || bgrData == null || width <= 0 || height <= 0 ||
                    bgrData.Length < checked(width * height * 3))
                    throw new InvalidOperationException("推理模型或本轮图像无效");
                lock (bInfering)
                {
                    int returnCode = Det_ModelPredict(bgrData, width, height, 3, results, boxesInfo, ref labellist[0]);
                    if (returnCode != 0) throw new InvalidOperationException("Det_ModelPredict returnCode=" + returnCode);
                    if (boxesInfo[0] < 0 || boxesInfo[0] > resultlist.Length / 6)
                        throw new InvalidOperationException("推理框数量无效: " + boxesInfo[0]);
                }


                string strGet = Encoding.Default.GetString(labellist, 0, labellist.Length);
                string[] predict_Label_List = strGet.Split(' ');

                for (int i = 0; i < boxesInfo[0]; i++)
                {
                    float score = resultlist[i * 6 + 1];
                    if (score > fThreshold)
                    {
                        CogRectangleAffine cogRect = new CogRectangleAffine();
                        cogRect.CenterX = resultlist[i * 6 + 2] + resultlist[i * 6 + 4] / 2;
                        cogRect.CenterY = resultlist[i * 6 + 3] + resultlist[i * 6 + 5] / 2;
                        cogRect.SideXLength = resultlist[i * 6 + 4];
                        cogRect.SideYLength = resultlist[i * 6 + 5];

                        string labelName = predict_Label_List[i].Trim();
                        int classIndex = _classNames.IndexOf(labelName);
                        if (classIndex >= 0 && labelName != "true")
                        {
                            dlR.AL_Type.Add(classIndex + 1);
                            dlR.AL_Rect.Add(cogRect);
                            dlR.AL_score.Add(score);
                        }
                    }
                    bSure = true;
                }
                inferenceSucceeded = true;
            }
            catch (Exception e)
            {
                RuntimeLog.Model("[Infer] failed: " + e);
                return false;
            }
            finally
            {
                if (resultHandle.IsAllocated) resultHandle.Free();
            }
            return bSure;
        }

    static int checks;
    static void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
    public static void Main()
    {
        var instance = new InferenceChecks();
        foreach (bool color in new[] { false, true })
        {
            var result = new DLResult { AL_Rect = new List<CogRectangleAffine>(), AL_score = new List<float>(), AL_Type = new List<int>() };
            byte[] bytes = new byte[color ? 12 : 4];
            bool success;
            instance.nativeCode = 0; instance.nativeBoxes = 0; instance.throwNative = false;
            bool old = color ? instance.det_infer_img_ByteColor(bytes, 2, 2, .5f, .5f, ref result, out success) : instance.det_infer_img_ByteGray(bytes, 2, 2, .5f, .5f, ref result, out success);
            Check(success && !old && result.AL_Rect.Count == 0, "zero boxes is successful, old bool preserved");
            instance.nativeBoxes = 1;
            old = color ? instance.det_infer_img_ByteColor(bytes, 2, 2, .5f, .5f, ref result, out success) : instance.det_infer_img_ByteGray(bytes, 2, 2, .5f, .5f, ref result, out success);
            Check(success && old && result.AL_Rect.Count == 0, "below-threshold boxes remain valid");
            instance.nativeCode = -7;
            old = color ? instance.det_infer_img_ByteColor(bytes, 2, 2, .5f, .5f, ref result, out success) : instance.det_infer_img_ByteGray(bytes, 2, 2, .5f, .5f, ref result, out success);
            Check(!success && !old, "nonzero native return rejected");
            instance.nativeCode = 0; instance.nativeBoxes = 101;
            old = color ? instance.det_infer_img_ByteColor(bytes, 2, 2, .5f, .5f, ref result, out success) : instance.det_infer_img_ByteGray(bytes, 2, 2, .5f, .5f, ref result, out success);
            Check(!success && !old, "invalid box count rejected");
            instance.throwNative = true;
            old = color ? instance.det_infer_img_ByteColor(bytes, 2, 2, .5f, .5f, ref result, out success) : instance.det_infer_img_ByteGray(bytes, 2, 2, .5f, .5f, ref result, out success);
            Check(!success && !old, "native exception rejected");
            instance.throwNative = false; instance.has_model_init = false;
            old = color ? instance.det_infer_img_ByteColor(bytes, 2, 2, .5f, .5f, ref result, out success) : instance.det_infer_img_ByteGray(bytes, 2, 2, .5f, .5f, ref result, out success);
            Check(!success && !old, "uninitialized model rejected");
            instance.has_model_init = true;
            old = color ? instance.det_infer_img_ByteColor(new byte[1], 2, 2, .5f, .5f, ref result, out success) : instance.det_infer_img_ByteGray(new byte[1], 2, 2, .5f, .5f, ref result, out success);
            Check(!success && !old, "short frame rejected");
        }
        Console.WriteLine("Actual extracted inference methods: " + checks + " checks passed");
    }
}
