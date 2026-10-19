from pathlib import Path
root = Path(__file__).resolve().parents[2]
s = (root/'CommonCls/CDL.cs').read_text(encoding='utf-8-sig')
methods = s[s.index('        public bool det_infer_img_ByteGray('):s.index('        //销毁深度学习模型')]
conversion = s[s.index('        public byte[] ConvertGrayToBGR(') if '        public byte[] ConvertGrayToBGR(' in s else s.index('        private byte[] ConvertGrayToBGR('):s.index('        //byte型灰度图像直接推理')]
text = '''using System;
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
''' + conversion + methods + '''
    static int checks;
    static void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
    public static void Main()
    {
        var instance = new InferenceChecks();
        foreach (bool color in new[] { false, true })
        {
            var result = new DLResult { AL_Rect = new List<CogRectangleAffine>(), AL_score = new List<double>(), AL_Type = new List<int>() };
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
'''
# DLResult uses ArrayList in this project; derive its initialization from the actual declarations.
text = text.replace('AL_score = new List<double>()', 'AL_score = new List<float>()')
(Path(__file__).parent/'InferenceChecks.cs').write_text(text, encoding='utf-8-sig')
