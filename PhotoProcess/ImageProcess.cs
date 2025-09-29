using SkiaSharp;
namespace PhotoProcess
{
    internal class ImageProcess
    {
        public SKBitmap? _saveBitmap;
        public SKBitmap? _originalBitmap;//原始图像
        public SKBitmap? _processedBitmap;//处理后的图像
        public byte[]? _DataValues; // 存储中间行的数据值
        public int _startX = 0; // 数据起始X坐标
        public int _endX = 0; // 数据结束X坐标
        public SKBitmap? _croppedColorBitmap;//裁剪后的彩色图像
        public double TCrate; // T线比率
        public ImageProcess()
        {
            _originalBitmap = null;
            _processedBitmap = null;
        }

        //构造函数，传入SKBitmap对象
        public ImageProcess(SKBitmap sKBitmap)
        {
            _originalBitmap = sKBitmap.Copy();
            _processedBitmap = _originalBitmap.Copy();
        }

        //加载图像
        public void LoadImage(SKBitmap sKBitmap)
        {
            _originalBitmap = sKBitmap;
            _processedBitmap = null;
        }



        //灰度处理
        public void ApplyGrayscale(SKBitmap bitmap)
        {
            using var pixmap = bitmap.PeekPixels();
            var pixels = pixmap.GetPixelSpan<byte>();
            int width = bitmap.Width;
            int height = bitmap.Height;
            int pixelCount = width * height;
            int middleY = height / 2;
            _startX = (int)(width * 0.1);
            _endX = (int)(width * 0.9);
            int segmentWidth = _endX - _startX;
            _DataValues = new byte[segmentWidth];
#if WINDOWS
        for (int i = 0; i < pixelCount; i++)
            {
                int idx = i * 4;  // 每个像素4字节 (RGBA)
                int x = i % width;
                int y = i / width;
                byte b = pixels[idx];
                byte g = pixels[idx + 1];
                byte r = pixels[idx + 2];
                byte gray = (byte)(r * 0.299f + g * 0.587f + b * 0.114f);
                pixels[idx] = gray;     // R
                pixels[idx + 1] = gray; // G
                pixels[idx + 2] = gray; // B
                if (y == middleY && x >= _startX && x < _endX)
                {
                    _DataValues[x - _startX] = gray;
                }
            }
#endif
#if ANDROID
        for (int i = 0; i < pixelCount; i++)
            {
                int idx = i * 4;  // 每个像素4字节 (RGBA)
                int x = i % width;
                int y = i / width;
                byte r = pixels[idx];
                byte g = pixels[idx + 1];
                byte b = pixels[idx + 2];
                byte gray = (byte)(r * 0.299f + g * 0.587f + b * 0.114f);
                pixels[idx] = gray;     // R
                pixels[idx + 1] = gray; // G
                pixels[idx + 2] = gray; // B
                if (y == middleY && x >= _startX && x < _endX)
                {
                    _DataValues[x - _startX] = gray;
                }
            }
#endif
        }

        //二值化处理
        public Task ApplyBinary(SKBitmap bitmap, byte threshold)
        {
            return Task.Run(() =>
            {
                using var pixmap = bitmap.PeekPixels();
                var pixels = pixmap.GetPixelSpan<byte>();
                int width = bitmap.Width;
                int height = bitmap.Height;
                int pixelCount = width * height;
#if ANDROID
            for (int i = 0; i < pixelCount; i++)
                {
                    int idx = i * 4;
                    byte gray = pixels[idx];
                    byte binary = gray > threshold ? (byte)255 : (byte)0;

                    pixels[idx] = binary;     // R
                    pixels[idx + 1] = binary; // G
                    pixels[idx + 2] = binary; // B
                }
#endif
#if WINDOWS
                for (int i = 0; i < pixelCount; i++)
                {
                    int idx = i * 4;
                    byte gray = pixels[idx];
                    byte binary = gray > threshold ? (byte)255 : (byte)0;

                    pixels[idx + 2] = binary;     // R
                    pixels[idx + 1] = binary; // G
                    pixels[idx + 0] = binary; // B
                }
#endif

            });
        }

        public Task FanApplyBinary(SKBitmap bitmap, byte threshold)
        {
            return Task.Run(() =>
            {
                using var pixmap = bitmap.PeekPixels();
                var pixels = pixmap.GetPixelSpan<byte>();
                int width = bitmap.Width;
                int height = bitmap.Height;
                int pixelCount = width * height;
#if ANDROID
            for (int i = 0; i < pixelCount; i++)
                {
                    int idx = i * 4;
                    byte gray = pixels[idx];
                    byte binary = gray > threshold ? (byte)0 : (byte)255;

                    pixels[idx] = binary;     // R
                    pixels[idx + 1] = binary; // G
                    pixels[idx + 2] = binary; // B
                }
#endif
#if WINDOWS
                for (int i = 0; i < pixelCount; i++)
                {
                    int idx = i * 4;
                    byte gray = pixels[idx];
                    byte binary = gray > threshold ? (byte)0 : (byte)255;

                    pixels[idx + 2] = binary;     // R
                    pixels[idx + 1] = binary; // G
                    pixels[idx + 0] = binary; // B
                }
#endif

            });
        }

        //高斯模糊处理
        public void ApplyGaussianBlur(SKBitmap bitmap, int kernelSize)
        {
            float[,] kernel = {
                {1f/16, 2f/16, 1f/16},
                {2f/16, 4f/16, 2f/16},
                {1f/16, 2f/16, 1f/16}
            };
            using var tempBitmap = new SKBitmap(bitmap.Width, bitmap.Height);
            using var pixmap = bitmap.PeekPixels();
            using var tempPixmap = tempBitmap.PeekPixels();

            var pixels = pixmap.GetPixelSpan<byte>();
            var tempPixels = tempPixmap.GetPixelSpan<byte>();

            int width = bitmap.Width;
            int height = bitmap.Height;
            int halfKernel = kernelSize / 2;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float r = 0, g = 0, b = 0;
                    for (int ky = -halfKernel; ky <= halfKernel; ky++)
                    {
                        for (int kx = -halfKernel; kx <= halfKernel; kx++)
                        {
                            int px = Math.Clamp(x + kx, 0, width - 1);
                            int py = Math.Clamp(y + ky, 0, height - 1);

                            int idx = (py * width + px) * 4;
                            float weight = kernel[ky + halfKernel, kx + halfKernel];

                            r += pixels[idx] * weight;
                            g += pixels[idx + 1] * weight;
                            b += pixels[idx + 2] * weight;
                        }
                    }
                    int outIdx = (y * width + x) * 4;
                    tempPixels[outIdx] = (byte)r;
                    tempPixels[outIdx + 1] = (byte)g;
                    tempPixels[outIdx + 2] = (byte)b;
                    tempPixels[outIdx + 3] = pixels[outIdx + 3]; // 保留Alpha
                }
            }
            tempPixels.CopyTo(pixels);
        }


        public void Rotate90(ref SKBitmap bitmap)
        {
            var rotatedBitmap = new SKBitmap(bitmap.Height, bitmap.Width);
            using (var canvas = new SKCanvas(rotatedBitmap))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.Translate(rotatedBitmap.Width, 0);
                canvas.RotateDegrees(90);
                canvas.DrawBitmap(bitmap, 0, 0);
            }
            bitmap.Dispose();
            bitmap = rotatedBitmap;
        }

        //最大联通区域
        public SKRectI FindLargestConnectedComponent(SKBitmap binaryBitmap)
        {
            int width = binaryBitmap.Width;
            int height = binaryBitmap.Height;
            using var pixmap = binaryBitmap.PeekPixels();
            var pixels = pixmap.GetPixelSpan<byte>();

            // 使用并查集数据结构处理等价关系
            var parents = new Dictionary<int, int>();
            var sizes = new Dictionary<int, int>();
            int[,] labels = new int[height, width];
            int currentLabel = 1;

            // 第一遍扫描：分配标签并记录等价关系
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = (y * width + x) * 4;

                    if (pixels[idx] == 255) // 白色像素
                    {
                        int left = (x > 0) ? labels[y, x - 1] : 0;
                        int top = (y > 0) ? labels[y - 1, x] : 0;

                        if (left == 0 && top == 0)
                        {
                            // 新标签
                            labels[y, x] = currentLabel;
                            parents[currentLabel] = currentLabel;
                            sizes[currentLabel] = 1;
                            currentLabel++;
                        }
                        else
                        {
                            // 获取最小邻居标签（忽略0）
                            int minNeighbor = 0;
                            if (left != 0 && top != 0)
                                minNeighbor = Math.Min(left, top);
                            else
                                minNeighbor = (left != 0) ? left : top;

                            labels[y, x] = minNeighbor;

                            // 建立等价关系（Union-Find）
                            if (left != 0 && left != minNeighbor)
                                Union(minNeighbor, left, parents, sizes);
                            if (top != 0 && top != minNeighbor)
                                Union(minNeighbor, top, parents, sizes);

                            // 更新当前标签的大小
                            sizes[Find(minNeighbor, parents)]++;
                        }
                    }
                }
            }

            // 第二遍扫描：合并等价标签到根标签
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int label = labels[y, x];
                    if (label > 0)
                    {
                        labels[y, x] = Find(label, parents);
                    }
                }
            }

            // 统计连通区域大小
            var regionSizes = new Dictionary<int, int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int label = labels[y, x];
                    if (label > 0)
                    {
                        if (!regionSizes.ContainsKey(label))
                            regionSizes[label] = 0;
                        regionSizes[label]++;
                    }
                }
            }

            // 找到最大连通区域
            int maxLabel = 0;
            int maxSize = 0;
            foreach (var kvp in regionSizes)
            {
                if (kvp.Value > maxSize)
                {
                    maxSize = kvp.Value;
                    maxLabel = kvp.Key;
                }
            }

            // 计算最大连通区域的边界矩形
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            bool found = false;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (labels[y, x] == maxLabel)
                    {
                        found = true;
                        minX = Math.Min(minX, x);
                        minY = Math.Min(minY, y);
                        maxX = Math.Max(maxX, x);
                        maxY = Math.Max(maxY, y);
                    }
                }
            }

            if (!found || minX > maxX || minY > maxY)
            {
                return new SKRectI(0, 0, width, height);
            }

            // 扩展边界以确保包含整个连通区域
            int padding = 5; // 添加5像素的填充
            minX = Math.Max(0, minX - padding);
            minY = Math.Max(0, minY - padding);
            maxX = Math.Min(width - 1, maxX + padding);
            maxY = Math.Min(height - 1, maxY + padding);

            return new SKRectI(minX, minY, maxX, maxY);
        }

        private int Find(int label, Dictionary<int, int> parents)
        {
            if (!parents.ContainsKey(label)) return label;
            if (parents[label] != label)
            {
                parents[label] = Find(parents[label], parents);
            }
            return parents[label];
        }

        private void Union(int root1, int root2, Dictionary<int, int> parents, Dictionary<int, int> sizes)
        {
            int x = Find(root1, parents);
            int y = Find(root2, parents);
            if (x == y) return;

            // 按大小合并：小树合并到大树
            if (!sizes.ContainsKey(x)) sizes[x] = 1;
            if (!sizes.ContainsKey(y)) sizes[y] = 1;

            if (sizes[x] < sizes[y])
            {
                parents[x] = y;
                sizes[y] += sizes[x];
            }
            else
            {
                parents[y] = x;
                sizes[x] += sizes[y];
            }
        }
        // 裁剪图像到最大连通区域
        public void CropToRegion(SKBitmap sourceBitmap, SKRectI boundingBox, ref SKBitmap targetBitmap)
        {
            int minX = boundingBox.Left;
            int minY = boundingBox.Top;
            int maxX = boundingBox.Right;
            int maxY = boundingBox.Bottom;
            int cropWidth = maxX - minX + 1;
            int cropHeight = maxY - minY + 1;

            // 释放之前的裁剪图像
            _croppedColorBitmap?.Dispose();

            // 创建新的裁剪图像
            _croppedColorBitmap = new SKBitmap(cropWidth, cropHeight, sourceBitmap.ColorType, sourceBitmap.AlphaType);

            using var sourcePixmap = sourceBitmap.PeekPixels();
            using var croppedPixmap = _croppedColorBitmap.PeekPixels();

            var sourcePixels = sourcePixmap.GetPixelSpan<byte>();
            var croppedPixels = croppedPixmap.GetPixelSpan<byte>();

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int srcIdx = (y * sourceBitmap.Width + x) * 4;
                    int dstY = y - minY;
                    int dstX = x - minX;
                    int dstIdx = (dstY * cropWidth + dstX) * 4;
                    croppedPixels[dstIdx] = sourcePixels[srcIdx];         // R
                    croppedPixels[dstIdx + 1] = sourcePixels[srcIdx + 1]; // G
                    croppedPixels[dstIdx + 2] = sourcePixels[srcIdx + 2]; // B
                    croppedPixels[dstIdx + 3] = sourcePixels[srcIdx + 3]; // A
                }
            }

            // 更新目标位图
            targetBitmap?.Dispose();
            targetBitmap = _croppedColorBitmap.Copy();
        }



        // 波峰信息类
        public class PeakInfo
        {
            public int Position { get; set; }  // 波峰位置
            public int Start { get; set; }      // 波峰起始位置
            public int End { get; set; }        // 波峰结束位置
            public double Area { get; set; }    // 波峰面积（只包含高于基线的部分）
            public bool IsT { get; set; }       // 是否为T线
            public bool IsC { get; set; }       // 是否为C线
        }



        public void IdentifyTCPeaks(List<PeakInfo> peaks, int arrayLength)
        {
            if (peaks.Count == 0) return;

            // 清除之前的标记
            foreach (var peak in peaks)
            {
                peak.IsT = false;
                peak.IsC = false;
            }

            // 固定规则：左边是T线，右边是C线

            // 1. 寻找C线（右边最高峰）
            PeakInfo? cPeak = null;
            double maxAreaRight = 0;

            for (int i = 0; i < peaks.Count; i++)
            {
                // 位置在右半部分（避免误判左侧噪声）
                if (peaks[i].Position > arrayLength * 0.5)
                {
                    // 选择右半部分面积最大的波峰作为C线
                    if (peaks[i].Area > maxAreaRight)
                    {
                        maxAreaRight = peaks[i].Area;
                        cPeak = peaks[i];
                    }
                }
            }

            if (cPeak != null)
            {
                cPeak.IsC = true;
            }

            // 2. 寻找T线（左边最高峰）
            PeakInfo? tPeak = null;
            double maxAreaLeft = 0;

            for (int i = 0; i < peaks.Count; i++)
            {
                // 位置在左半部分（避免误判右侧噪声）
                if (peaks[i].Position < arrayLength * 0.5)
                {
                    // 选择左半部分面积最大的波峰作为T线
                    if (peaks[i].Area > maxAreaLeft)
                    {
                        maxAreaLeft = peaks[i].Area;
                        tPeak = peaks[i];
                    }
                }
            }

            if (tPeak != null)
            {
                tPeak.IsT = true;
            }

            // 3. 确保C线和T线不连通
            if (cPeak != null && tPeak != null)
            {
                // 如果C线和T线位置相邻，可能是同一个连通区域
                if (cPeak.Start <= tPeak.End)
                {
                    // 选择面积较大的作为C线，另一个作为T线
                    if (cPeak.Area > tPeak.Area)
                    {
                        tPeak.IsT = false;
                    }
                    else
                    {
                        cPeak.IsC = false;
                    }
                }
            }
        }


        public List<PeakInfo> FindPeaksBasedOnAverage(int[] values)
        {
            var peaks = new List<PeakInfo>();
            if (values == null || values.Length < 3) return peaks;

            // 计算基线（使用整体数据的中位数，并适当提高）
            var allValues = new List<int>(values);
            allValues.Sort();
            double baseline = allValues[allValues.Count / 2]; // 中位数
            baseline = baseline * 1.05; // 提高5%

            // 设置阈值 - 大于基线的5%
            int threshold = (int)(baseline * 1.05);

            // 查找高于阈值的连续区域
            bool inPeak = false;
            int peakStart = 0;
            int peakMaxIndex = 0;
            int peakMaxValue = 0;

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] > threshold)
                {
                    if (!inPeak)
                    {
                        // 开始新的波峰区域
                        inPeak = true;
                        peakStart = i;
                        peakMaxIndex = i;
                        peakMaxValue = values[i];
                    }
                    else
                    {
                        // 更新波峰最大值
                        if (values[i] > peakMaxValue)
                        {
                            peakMaxValue = values[i];
                            peakMaxIndex = i;
                        }
                    }
                }
                else if (inPeak)
                {
                    // 结束当前波峰区域
                    inPeak = false;

                    // 确保波峰宽度足够（至少3个像素）
                    if (i - peakStart >= 3)
                    {
                        // 向左扩展波峰起点
                        int start = peakStart;
                        while (start > 0 && values[start] > values[start - 1])
                            start--;

                        // 向右扩展波峰终点
                        int end = i - 1;
                        while (end < values.Length - 1 && values[end] > values[end + 1])
                            end++;

                        // 计算波峰面积（相对于基线的积分）
                        double area = 0;
                        for (int j = start; j <= end; j++)
                        {
                            // 只计算高于基线的部分
                            area += Math.Max(0, values[j] - baseline);
                        }

                        peaks.Add(new PeakInfo
                        {
                            Position = peakMaxIndex,
                            Start = start,
                            End = end,
                            Area = area
                        });
                    }
                }
            }

            // 处理最后一个波峰
            if (inPeak && values.Length - peakStart >= 3)
            {
                int start = peakStart;
                while (start > 0 && values[start] > values[start - 1])
                    start--;

                int end = values.Length - 1;
                while (end < values.Length - 1 && values[end] > values[end + 1])
                    end++;

                double area = 0;
                for (int j = start; j <= end; j++)
                {
                    // 只计算高于基线的部分
                    area += Math.Max(0, values[j] - baseline);
                }

                peaks.Add(new PeakInfo
                {
                    Position = peakMaxIndex,
                    Start = start,
                    End = end,
                    Area = area
                });
            }

            return peaks;
        }


        //RGB分析专用
        public bool _R_enabled = true;
        public bool _G_enabled = true;
        public bool _B_enabled = true;
        public ChannelDiffMode CurrentAnalysisMode = ChannelDiffMode.Gray;
        // RGB 分析处理函数
        public void ApplyRGBFilter(SKBitmap bitmap)
        {
            using var pixmap = bitmap.PeekPixels();
            var pixels = pixmap.GetPixelSpan<byte>();
            int pixelCount = bitmap.Width * bitmap.Height;
            for (int i = 0; i < pixelCount; i++)
            {
                int idx = i * 4;
                // 假设像素存储顺序是BGR
                if (!_B_enabled) pixels[idx + 2] = 0;     // B 通道置零
                if (!_G_enabled) pixels[idx + 1] = 0; // G 通道置零
                if (!_R_enabled) pixels[idx + 0] = 0; // R 通道置零
                                                      // 注意：这里交换了R和B，索引0是B，索引2是R
            }

            //取中间行数据 10%~90%
            int width = bitmap.Width;
            int height = bitmap.Height;
            int middleY = height / 2;
            _startX = (int)(width * 0.1);
            _endX = (int)(width * 0.9);
            int segmentWidth = _endX - _startX;
            _DataValues = new byte[segmentWidth];
            byte gray = 0;
            byte r = 0,g = 0, b = 0;
            // 提取中间行的数据（灰度值）
            for (int x = _startX; x < _endX; x++)
            {
                int idx = (middleY * width + x) * 4;
#if ANDROID
                r = pixels[idx + 0]; // 注意：我们的像素顺序是RGB，所以R在索引0
                g = pixels[idx + 1]; // G在索引1
                b = pixels[idx + 2]; // B在索引2
#endif
#if WINDOWS
                r = pixels[idx + 2]; // 注意：我们的像素顺序是BGR，所以R在索引2
                g = pixels[idx + 1]; // G在索引1
                b = pixels[idx + 0]; // B在索引0
#endif
                gray = CalculateChannelDiff(r, g, b, CurrentAnalysisMode);
                _DataValues[x - _startX] = gray;

            }
        }

        //灰度融合法
        private static byte CalculateGray(byte r, byte g, byte b)
        {
            return (byte)(r * 0.299f + g * 0.587f + b * 0.114f);
        }
        public enum ChannelDiffMode
        {
            Gray,
            Standard,       // 标准通道差异 (R - (G+B)/2)
            EnhancedRed,    // 增强红色通道 (2*R - G - B)
            GreenBlueDiff,  // 绿蓝差异 (|G - B|)
            TargetColor,    // 针对特定颜色增强
            MaxDifference,  // 最大通道差异
            EnhancedGreenBlue,
            PCA             // 添加PCA融合方案

        }

        public static byte CalculateChannelDiff(byte r, byte g, byte b, ChannelDiffMode mode = ChannelDiffMode.Standard)
        {
            return mode switch
            {
                // 灰度值：直接计算灰度值
                ChannelDiffMode.Gray => CalculateGray(r, g, b),
                // 标准差异：突出红色与背景的差异
                ChannelDiffMode.Standard => (byte)Math.Clamp(0.5*(r - (g + b) / 2), 0, 255),
                // 增强红色：更强烈的红色对比
                ChannelDiffMode.EnhancedRed => (byte)Math.Clamp(0.5*( 2 * r - g - b), 0, 255),
                // 增强绿蓝：突出绿色与蓝色的差异
                ChannelDiffMode.EnhancedGreenBlue => (byte)Math.Clamp(0.5*((g + b)/2 - 0.3f * r), 0, 255),
                // 绿蓝差异：突出绿色与蓝色的差异
                ChannelDiffMode.GreenBlueDiff => (byte)Math.Abs(0.5*(g - b)),
                // 目标颜色增强：针对特定目标颜色增强
                ChannelDiffMode.TargetColor => CalculateTargetColorDiff(r, g, b),
                // 最大通道差异：计算最大通道差异
                ChannelDiffMode.MaxDifference => (byte)(0.5*(Math.Max(Math.Abs(r - g), Math.Max(Math.Abs(g - b), Math.Abs(b - r))))),
                // PCA融合：使用主成分分析融合通道
                ChannelDiffMode.PCA => CalculatePCADiff(r, g, b),
                _ => (byte)Math.Clamp(r - (g + b) / 2, 0, 200)
            };
        }

        // PCA融合方法
        private static byte CalculatePCADiff(byte r, byte g, byte b)
        {
            // 使用预训练的主成分向量（针对自然图像优化）
            const float pc1 = 0.577f; // 主成分1 - 亮度方向
            const float pc2 = 0.577f; // 主成分1 - 亮度方向
            const float pc3 = 0.577f; // 主成分1 - 亮度方向

            // 计算第一主成分投影值
            float projection = r * pc1 + g * pc2 + b * pc3;

            // 归一化到0-255范围
            // 自然图像中，投影值通常在0-255范围内
            return (byte)Math.Clamp(0.8*projection, 0, 255);
        }

        // 完整的PCA融合方法（可训练版本）
        public static byte[] CalculatePCADiffFull(byte[] rValues, byte[] gValues, byte[] bValues)
        {
            int length = rValues.Length;
            byte[] pcaValues = new byte[length];

            // 计算RGB通道的均值
            float meanR = CalculateMean(rValues);
            float meanG = CalculateMean(gValues);
            float meanB = CalculateMean(bValues);

            // 计算协方差矩阵
            float varR = 0, varG = 0, varB = 0;
            float covRG = 0, covRB = 0, covGB = 0;

            for (int i = 0; i < length; i++)
            {
                float dr = rValues[i] - meanR;
                float dg = gValues[i] - meanG;
                float db = bValues[i] - meanB;

                varR += dr * dr;
                varG += dg * dg;
                varB += db * db;
                covRG += dr * dg;
                covRB += dr * db;
                covGB += dg * db;
            }

            // 计算平均协方差
            varR /= length;
            varG /= length;
            varB /= length;
            covRG /= length;
            covRB /= length;
            covGB /= length;

            // 协方差矩阵
            float[,] covMatrix =
            {
                {varR, covRG, covRB},
                {covRG, varG, covGB},
                {covRB, covGB, varB}
            };

            // 使用幂迭代法计算主特征向量
            float[] eigenVector = PowerIteration(covMatrix);

            // 计算投影值
            float minValue = float.MaxValue;
            float maxValue = float.MinValue;
            float[] projections = new float[length];

            for (int i = 0; i < length; i++)
            {
                float projection =
                    eigenVector[0] * (rValues[i] - meanR) +
                    eigenVector[1] * (gValues[i] - meanG) +
                    eigenVector[2] * (bValues[i] - meanB);

                projections[i] = projection;

                if (projection < minValue) minValue = projection;
                if (projection > maxValue) maxValue = projection;
            }

            // 归一化到0-255范围
            float range = maxValue - minValue;
            if (range < 0.001f) range = 1f; // 避免除零

            for (int i = 0; i < length; i++)
            {
                float normalized = (projections[i] - minValue) / range * 255;
                pcaValues[i] = (byte)Math.Clamp(normalized, 0, 255);
            }

            return pcaValues;
        }

        // 幂迭代法计算主特征向量
        private static float[] PowerIteration(float[,] matrix, int maxIterations = 10, float tolerance = 1e-6f)
        {
            int n = matrix.GetLength(0);
            float[] v = new float[n];

            // 初始化随机向量
            Random rand = new Random();
            for (int i = 0; i < n; i++)
            {
                v[i] = (float)rand.NextDouble();
            }

            // 归一化
            float norm = VectorNorm(v);
            for (int i = 0; i < n; i++)
            {
                v[i] /= norm;
            }

            for (int iter = 0; iter < maxIterations; iter++)
            {
                // 计算 Av
                float[] av = new float[n];
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        av[i] += matrix[i, j] * v[j];
                    }
                }

                // 计算特征值估计
                float eigenvalue = VectorDot(v, av);

                // 更新向量
                float[] newV = new float[n];
                float newNorm = VectorNorm(av);

                if (newNorm < tolerance) break;

                for (int i = 0; i < n; i++)
                {
                    newV[i] = av[i] / newNorm;
                }

                // 检查收敛
                float diff = 0;
                for (int i = 0; i < n; i++)
                {
                    diff += Math.Abs(newV[i] - v[i]);
                }

                if (diff < tolerance) break;

                v = newV;
            }

            return v;
        }

        // 计算向量范数
        private static float VectorNorm(float[] v)
        {
            float sum = 0;
            foreach (float value in v)
            {
                sum += value * value;
            }
            return (float)Math.Sqrt(sum);
        }

        // 计算向量点积
        private static float VectorDot(float[] a, float[] b)
        {
            float sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                sum += a[i] * b[i];
            }
            return sum;
        }

        // 计算均值
        private static float CalculateMean(byte[] values)
        {
            float sum = 0;
            foreach (byte value in values)
            {
                sum += value;
            }
            return sum / values.Length;
        }

        // 针对特定目标颜色的差异计算（可自定义目标色）
        private static byte CalculateTargetColorDiff(byte r, byte g, byte b)
        {
            const byte targetR = 220;
            const byte targetG = 50;
            const byte targetB = 50;

            // 计算颜色相似度差异
            float diffR = Math.Abs(r - targetR) / 255f;
            float diffG = Math.Abs(g - targetG) / 255f;
            float diffB = Math.Abs(b - targetB) / 255f;

            // 综合差异（值越小表示越接近目标色）
            float similarity = 1.0f - (diffR + diffG + diffB) / 3.0f;

            // 转换为差异值（越接近目标色值越大）
            return (byte)(255 * similarity);
        }
    }
}
