using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PureBattleGame.Games.CockroachPet;

public static class ChromaKeyProcessor
{
    /// <summary>
    /// 智能双模自动扣图 (Adaptive Chroma Key & Border Color Matting)
    /// 1. 动态采样图像边缘背景颜色 (支持橄榄绿、浅绿、暗绿、灰色等各种 AI 背景)
    /// 2. 结合绿色主导度与背景色彩距离，实现高精度背景去除与绿边平滑抑制 (Green Spill Suppression)
    /// </summary>
    public static Bitmap RemoveGreenScreen(Bitmap source, float sensitivity = 0.45f)
    {
        int width = source.Width;
        int height = source.Height;

        Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(result))
        {
            g.DrawImage(source, 0, 0, width, height);
        }

        BitmapData data = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

        int bytesCount = Math.Abs(data.Stride) * height;
        byte[] pixelBuffer = new byte[bytesCount];
        Marshal.Copy(data.Scan0, pixelBuffer, 0, bytesCount);

        int stride = data.Stride;

        // 1. 动态取样四周边缘像素，计算真实背景平均色 (R, G, B)
        long sumR = 0, sumG = 0, sumB = 0;
        int sampleCount = 0;

        int step = Math.Max(1, width / 40);
        for (int x = 0; x < width; x += step)
        {
            AddSample(pixelBuffer, 0 * stride + x * 4, ref sumR, ref sumG, ref sumB, ref sampleCount);
            AddSample(pixelBuffer, (height - 1) * stride + x * 4, ref sumR, ref sumG, ref sumB, ref sampleCount);
        }
        for (int y = 0; y < height; y += step)
        {
            AddSample(pixelBuffer, y * stride + 0 * 4, ref sumR, ref sumG, ref sumB, ref sampleCount);
            AddSample(pixelBuffer, y * stride + (width - 1) * 4, ref sumR, ref sumG, ref sumB, ref sampleCount);
        }

        float avgR = sampleCount > 0 ? sumR / (float)sampleCount : 100;
        float avgG = sampleCount > 0 ? sumG / (float)sampleCount : 180;
        float avgB = sampleCount > 0 ? sumB / (float)sampleCount : 100;

        // 2. 双指标抠图遍历
        for (int y = 0; y < height; y++)
        {
            int rowOffset = y * stride;
            for (int x = 0; x < width; x++)
            {
                int index = rowOffset + (x * 4);
                int b = pixelBuffer[index + 0];
                int gVal = pixelBuffer[index + 1];
                int r = pixelBuffer[index + 2];
                int a = pixelBuffer[index + 3];

                if (a == 0) continue;

                // 指标 1: 绿色分量主导度 (Green Dominance)
                float maxOther = Math.Max(r, b);
                float greenDiff = gVal - maxOther;

                // 指标 2: 欧式色彩空间距离 (Color Distance to Detected Background)
                float dr = r - avgR;
                float dg = gVal - avgG;
                float db = b - avgB;
                float colorDist = (float)Math.Sqrt(dr * dr + dg * dg + db * db);

                bool isGreenMatch = (gVal > 45 && greenDiff > 16);
                bool isBgColorMatch = (colorDist < 55);

                if (isGreenMatch || isBgColorMatch)
                {
                    if (greenDiff > 30 || colorDist < 36)
                    {
                        // 100% 完全透明
                        pixelBuffer[index + 3] = 0;
                    }
                    else
                    {
                        // 边缘渐变羽化与绿溢消除 (Green Spill Suppression)
                        float alphaFactor = Math.Min(1.0f, colorDist / 55.0f);
                        pixelBuffer[index + 3] = (byte)(a * alphaFactor);
                        if (gVal > maxOther)
                        {
                            pixelBuffer[index + 1] = (byte)maxOther; // 压制绿边
                        }
                    }
                }
            }
        }

        Marshal.Copy(pixelBuffer, 0, data.Scan0, bytesCount);
        result.UnlockBits(data);
        return result;
    }

    private static void AddSample(byte[] buffer, int offset, ref long sumR, ref long sumG, ref long sumB, ref int count)
    {
        if (offset < 0 || offset + 3 >= buffer.Length) return;
        sumB += buffer[offset + 0];
        sumG += buffer[offset + 1];
        sumR += buffer[offset + 2];
        count++;
    }
}
