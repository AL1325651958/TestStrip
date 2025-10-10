using Microsoft.Maui.Platform;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using System.Collections.ObjectModel;

namespace PhotoProcess;

public partial class RGBAnazy : ContentPage
{
    ImageProcess imageProcess = new ImageProcess();
    public RGBAnazy()
	{
		InitializeComponent();
        waveformCanvas.PaintSurface += OnWaveformPaintSurface;
        InitializeAnalysisModePicker();
    }

    private void InitializeAnalysisModePicker()
    {

    }
    private async void Button_Clicked(object sender, EventArgs e)
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.StorageRead>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("权限被拒绝", "需要存储权限才能访问图片", "确定");
                return;
            }
            var options = new PickOptions
            {
                PickerTitle = "选择图片",
                FileTypes = FilePickerFileType.Images
            };
            var result = await FilePicker.Default.PickAsync(options);
            if (result == null) return;
            using var stream = await result.OpenReadAsync();
            var original = SKBitmap.Decode(stream);
            if (original.Width * original.Height > 1000000)
            {
                float scale = (float)Math.Sqrt(1000000f / (original.Width * original.Height));
                var newWidth = (int)(original.Width * scale);
                var newHeight = (int)(original.Height * scale);

                var resized = new SKBitmap(newWidth, newHeight);
                original.ScalePixels(resized, SKFilterQuality.Medium);
                original.Dispose();
                original = resized;
            }
            imageProcess._originalBitmap?.Dispose();
            imageProcess._processedBitmap?.Dispose();
            imageProcess._originalBitmap = original.Copy();
            imageProcess._processedBitmap = imageProcess._originalBitmap.Copy(); 
            await UpdateImageDisplayAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", $"打开图片失败: {ex.Message}", "确定");
        }
        finally
        {
            ProcessingIndicator.IsVisible = false;
            ProcessingIndicator.IsRunning = false;
        }
    }

    private async void AutoModeButton_Clicked(object sender, EventArgs e)
    {
        if(imageProcess._processedBitmap  == null)
        {
            await DisplayAlert("错误", "请先加载图片", "确定");
            return;
        }
        try
        {
            ProcessingIndicator.IsVisible = true;
            ProcessingIndicator.IsRunning = true;
            AutoModeButton.IsEnabled = false;
            imageProcess._processedBitmap = imageProcess._originalBitmap.Copy(); ; 
                                                                                  
            byte threshold = 10;

            await Task.Run(async () =>
            {
                try
                {
                    Device.BeginInvokeOnMainThread(() => ProcessingIndicator.IsRunning = true);
                    imageProcess.ApplyGrayscale(imageProcess._processedBitmap);
                    await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());

                    await imageProcess.ApplyBinary(imageProcess._processedBitmap, 150);
                    await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());


                    Device.BeginInvokeOnMainThread(() =>
                    {
                        waveformCanvas.InvalidateSurface();
                    });
                    using var binaryBitmap = imageProcess._processedBitmap.Copy();
                    SKRectI boundingBox = imageProcess.FindLargestConnectedComponent(binaryBitmap);
                    imageProcess.CropToRegion(imageProcess._originalBitmap, boundingBox, ref imageProcess._processedBitmap);
                    await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());
                    if (imageProcess._processedBitmap.Height > imageProcess._processedBitmap.Width)
                    {
                        imageProcess.Rotate90(ref imageProcess._processedBitmap);
                        await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());
                    }

                    imageProcess._saveBitmap = imageProcess._processedBitmap.Copy();
                    //imageProcess.ApplyGrayscale(imageProcess._processedBitmap);
                    //await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        waveformCanvas.InvalidateSurface();
                    });
                    //imageProcess.ApplyInvertSmart(imageProcess._processedBitmap);
                    //await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());
                    //imageProcess.ApplyRGBFilter(imageProcess._processedBitmap);
                    await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());

                    imageProcess.ApplyChannelDiff(imageProcess._processedBitmap, imageProcess.CurrentAnalysisMode);
                    await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        waveformCanvas.InvalidateSurface();
                    });
                }

                catch (Exception ex)
                {
                    
                }
            });
        }
        finally
        {
            ProcessingIndicator.IsRunning = false;
            ProcessingIndicator.IsVisible = false;
            AutoModeButton.IsEnabled = true;
        }
    }

    private async void SaveWaveformButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("权限被拒绝", "需要存储权限才能保存图片", "确定");
                return;
            }
            if (imageProcess._saveBitmap == null)
            {
                await DisplayAlert("错误", "没有可用的裁剪图像", "确定");
                return;
            }
            int waveformWidth = (int)waveformCanvas.CanvasSize.Width;
            int waveformHeight = (int)waveformCanvas.CanvasSize.Height;
            int combinedWidth = Math.Max(imageProcess._saveBitmap.Width, waveformWidth);
            using (var waveformSurface = SKSurface.Create(new SKImageInfo(combinedWidth, waveformHeight)))
            {
                var waveformCanvasSurface = waveformSurface.Canvas;
                DrawWaveform(waveformCanvasSurface, combinedWidth, waveformHeight);
                using var waveformImage = waveformSurface.Snapshot();
                int combinedHeight = imageProcess._saveBitmap.Height + waveformHeight + 100;
                using (var combinedSurface = SKSurface.Create(new SKImageInfo(combinedWidth, combinedHeight)))
                {
                    var combinedCanvas = combinedSurface.Canvas;
                    combinedCanvas.Clear(SKColors.White);
                    using var titlePaint = new SKPaint
                    {
                        Color = SKColors.Black,
                        TextSize = 24,
                        IsAntialias = true,
                        TextAlign = SKTextAlign.Center
                    };
                    int croppedX = (combinedWidth - imageProcess._saveBitmap.Width) / 2;
                    combinedCanvas.DrawBitmap(imageProcess._saveBitmap, croppedX, 50);
                    int waveformX = (combinedWidth - combinedWidth) / 2;
                    int waveformY = imageProcess._saveBitmap.Height + 50; 
                    combinedCanvas.DrawImage(waveformImage, waveformX, waveformY);
                    using var combinedImage = combinedSurface.Snapshot();
                    using var data = combinedImage.Encode(SKEncodedImageFormat.Png, 100);
                    string fileName = $"CT_Analysis_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                    string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
                    using (var stream = File.OpenWrite(filePath))
                    {
                        data.SaveTo(stream);
                    }
                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = "CT试纸分析结果",
                        File = new ShareFile(filePath)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", $"保存分析结果失败: {ex.Message}", "确定");
        }
    }
    private void OnWaveformPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var surface = e.Surface;
        var canvas = surface.Canvas;
        var info = e.Info;
        DrawWaveform(canvas, info.Width, info.Height);
    }
    private async Task UpdateImageDisplayAsync()
    {
        if (imageProcess._processedBitmap != null)
        {
            using var stream = new MemoryStream();
            imageProcess._processedBitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
            stream.Seek(0, SeekOrigin.Begin);
            await Device.InvokeOnMainThreadAsync(() =>
            selectedImage.Source = ImageSource.FromStream(() => new MemoryStream(stream.ToArray())));
            waveformCanvas.InvalidateSurface();
        }
    }
    private void DrawWaveform(SKCanvas canvas, int width, int height)
    {
        if (imageProcess._DataValues == null || imageProcess._DataValues.Length == 0)
            return;

        canvas.Clear(SKColors.White);
        int segmentWidth = imageProcess._DataValues.Length;
        float scaleX = (float)width / segmentWidth;
        float scaleY = (float)height / 255;

        // 保存原始数据用于面积计算
        int[] originalIntValues = new int[imageProcess._DataValues.Length];
        for (int i = 0; i < imageProcess._DataValues.Length; i++)
        {
            originalIntValues[i] = imageProcess._DataValues[i];
        }

        // 应用Savitzky-Golay滤波器保留边界特征（用于峰检测）
        byte[] filteredValues = ApplySavitzkyGolayFilter(imageProcess._DataValues, 5, 2);

        // 计算全局平均值（基于滤波后数据）
        double average = 0;
        foreach (byte value in filteredValues)
        {
            average += value;
        }
        average /= filteredValues.Length;
        float averageY = (float)(height - average * scaleY);

        // 使用滤波后数据进行波峰检测
        int[] filteredIntValues = new int[filteredValues.Length];
        for (int i = 0; i < filteredValues.Length; i++)
        {
            filteredIntValues[i] = filteredValues[i];
        }

        // 使用您完善的波峰检测算法（基于滤波后数据）
        var peaks = imageProcess.FindPeaksBasedOnAverage(filteredIntValues);

        // 将数据分为左右两部分（基于峰的位置分布）
        int midPoint = FindOptimalSplitPoint(peaks, segmentWidth);
        byte[] leftData = new byte[midPoint];
        byte[] rightData = new byte[segmentWidth - midPoint];
        Array.Copy(filteredValues, 0, leftData, 0, midPoint);
        Array.Copy(filteredValues, midPoint, rightData, 0, segmentWidth - midPoint);

        // 计算左侧基线（基于滤波后数据）
        double leftBaseline = CalculateBaseline(leftData);
        float leftBaselineY = (float)(height - leftBaseline * scaleY);

        // 计算右侧基线（基于滤波后数据）
        double rightBaseline = CalculateBaseline(rightData);
        float rightBaselineY = (float)(height - rightBaseline * scaleY);

        // 使用您完善的T/C峰识别算法
        imageProcess.IdentifyTCPeaks(peaks, filteredIntValues.Length);

        // 为每个峰分配对应的基线并计算面积（使用原始数据）
        foreach (var peak in peaks)
        {
            // 根据峰位置选择对应的基线
            double baseline = peak.Position < midPoint ? leftBaseline : rightBaseline;

            // 使用滤波后数据确定边界
            var boundaries = FindPeakBoundaries(filteredIntValues, peak.Position, baseline);
            peak.Start = boundaries.start;
            peak.End = boundaries.end;

            // 梯形法计算面积（使用原始数据和滤波后确定的基线）
            double area = 0;
            for (int i = peak.Start; i < peak.End; i++)
            {
                // 使用原始数据和对应的基线
                double height1 = Math.Max(0, originalIntValues[i] - baseline);
                double height2 = Math.Max(0, originalIntValues[i + 1] - baseline);
                area += (height1 + height2) * 0.5;
            }
            peak.Area = area;
        }

        // 绘制波形（使用滤波后数据）
        using var path = new SKPath();
        path.MoveTo(0, height - filteredValues[0] * scaleY);
        for (int i = 1; i < segmentWidth; i++)
        {
            float y = height - filteredValues[i] * scaleY;
            path.LineTo(i * scaleX, y);
        }

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Blue,
            StrokeWidth = 2,
            IsAntialias = true
        };
        canvas.DrawPath(path, paint);

        // 绘制平均值线
        using var avgPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Gray,
            StrokeWidth = 1,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new float[] { 3, 3 }, 0)
        };
        canvas.DrawLine(0, averageY, width, averageY, avgPaint);

        // 绘制左侧基线
        using var leftBaselinePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Green,
            StrokeWidth = 1,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0)
        };
        canvas.DrawLine(0, leftBaselineY, midPoint * scaleX, leftBaselineY, leftBaselinePaint);

        // 绘制右侧基线
        using var rightBaselinePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.DarkGreen,
            StrokeWidth = 1,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0)
        };
        canvas.DrawLine(midPoint * scaleX, rightBaselineY, width, rightBaselineY, rightBaselinePaint);

        // 绘制基线文本
        using var baselineTextPaint = new SKPaint
        {
            Color = SKColors.Green,
            TextSize = 20,
            IsAntialias = true
        };
        canvas.DrawText($"L-Base: {leftBaseline:F1}", 10, leftBaselineY - 5, baselineTextPaint);
        canvas.DrawText($"R-Base: {rightBaseline:F1}", width - 150, rightBaselineY - 5, baselineTextPaint);

        // 计算T/C比率
        var tPeak = peaks.FirstOrDefault(p => p.IsT);
        var cPeak = peaks.FirstOrDefault(p => p.IsC);
        double ratio = 0;
        if (tPeak != null && cPeak != null && cPeak.Area > 0)
        {
            ratio = tPeak.Area / cPeak.Area;
        }
        else if (tPeak == null && cPeak != null && cPeak.Area > 0)
        {
            ratio = 0;
        }

        // 只绘制T峰和C峰
        if (peaks.Count > 0)
        {
            using var highlightPaint = new SKPaint
            {
                Color = SKColors.Red,
                StrokeWidth = 3,
                IsAntialias = true
            };

            using var fillPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = new SKColor(0, 200, 0, 100),
                IsAntialias = true
            };

            using var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 20,
                IsAntialias = true
            };

            using var ratioPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 40,
                IsAntialias = true
            };

            // 只绘制T峰和C峰
            foreach (var peak in peaks.Where(p => p.IsT || p.IsC))
            {
                int safePosition = Math.Clamp(peak.Position, 0, filteredIntValues.Length - 1);
                float x = safePosition * scaleX;
                float y = height - filteredIntValues[safePosition] * scaleY;

                // 绘制峰顶标记
                canvas.DrawCircle(x, y, 5, highlightPaint);

                int safeStart = Math.Clamp(peak.Start, 0, filteredIntValues.Length - 1);
                int safeEnd = Math.Clamp(peak.End, 0, filteredIntValues.Length - 1);

                if (safeStart > safeEnd)
                {
                    (safeStart, safeEnd) = (safeEnd, safeStart);
                }

                float startX = safeStart * scaleX;
                float endX = safeEnd * scaleX;

                // 确定峰区域对应的基线
                float baselineY = peak.Position < midPoint ? leftBaselineY : rightBaselineY;

                // 绘制峰区域填充
                using (var peakPath = new SKPath())
                {
                    peakPath.MoveTo(startX, baselineY);
                    for (int i = safeStart; i <= safeEnd; i++)
                    {
                        if (i < 0 || i >= filteredIntValues.Length) continue;

                        float xi = i * scaleX;
                        float dataValue = Math.Clamp(filteredIntValues[i], 0, 255);

                        // 使用峰对应的基线计算高度
                        double baseline = peak.Position < midPoint ? leftBaseline : rightBaseline;
                        float yi = height - Math.Max((float)baseline, dataValue) * scaleY;

                        peakPath.LineTo(xi, yi);
                    }
                    peakPath.LineTo(endX, baselineY);
                    peakPath.Close();
                    canvas.DrawPath(peakPath, fillPaint);
                }

                // 绘制峰标签
                string label = peak.IsT ? "T" : "C";
                canvas.DrawText($"{label}:{peak.Area:F0}", x, y - 10, textPaint);
            }

            // 绘制T/C比率
            string ratioText = $"T/C: {ratio:F4}";
            imageProcess.TCrate = ratio;
            float textWidth = ratioPaint.MeasureText(ratioText);
            float centerX = (width - textWidth) / 2;
            canvas.DrawText(ratioText, centerX, 40, ratioPaint);
        }
    }

    // Savitzky-Golay滤波器
    private byte[] ApplySavitzkyGolayFilter(byte[] data, int windowSize, int polynomialOrder)
    {
        if (windowSize % 2 == 0) windowSize++; // 确保窗口大小为奇数
        int halfWindow = windowSize / 2;
        byte[] filtered = new byte[data.Length];

        // 预计算系数
        double[] coefficients = CalculateSavitzkyGolayCoefficients(windowSize, polynomialOrder);

        for (int i = 0; i < data.Length; i++)
        {
            double sum = 0;
            for (int j = -halfWindow; j <= halfWindow; j++)
            {
                int index = i + j;
                if (index < 0) index = 0;
                if (index >= data.Length) index = data.Length - 1;

                sum += data[index] * coefficients[j + halfWindow];
            }
            filtered[i] = (byte)Math.Round(Math.Clamp(sum, 0, 255));
        }
        return filtered;
    }

    // 计算Savitzky-Golay系数
    private double[] CalculateSavitzkyGolayCoefficients(int windowSize, int polynomialOrder)
    {
        int halfWindow = windowSize / 2;
        double[] coefficients = new double[windowSize];

        // Vandermonde矩阵
        double[,] A = new double[windowSize, polynomialOrder + 1];
        for (int i = -halfWindow; i <= halfWindow; i++)
        {
            for (int j = 0; j <= polynomialOrder; j++)
            {
                A[i + halfWindow, j] = Math.Pow(i, j);
            }
        }

        // 计算伪逆矩阵 (A^T * A)^-1 * A^T
        // 这里简化处理，实际应用中应使用矩阵运算库
        double[] c = new double[windowSize];
        for (int i = 0; i < windowSize; i++)
        {
            c[i] = A[i, 0]; // 取第一列（常数项系数）
        }

        // 归一化
        double sum = c.Sum();
        for (int i = 0; i < windowSize; i++)
        {
            coefficients[i] = c[i] / sum;
        }

        return coefficients;
    }

    // 寻找最佳分割点（基于峰的位置分布）
    private int FindOptimalSplitPoint(List<ImageProcess.PeakInfo> peaks, int segmentWidth)
    {
        if (peaks.Count == 0) return segmentWidth / 2;

        // 如果有T峰和C峰，使用它们之间的中点作为分割点
        var tPeak = peaks.FirstOrDefault(p => p.IsT);
        var cPeak = peaks.FirstOrDefault(p => p.IsC);

        if (tPeak != null && cPeak != null)
        {
            return (tPeak.Position + cPeak.Position) / 2;
        }

        // 如果没有明确的T/C极峰，使用所有峰的位置中值
        if (peaks.Count > 0)
        {
            var positions = peaks.Select(p => p.Position).OrderBy(p => p).ToList();
            return positions[positions.Count / 2];
        }

        // 默认使用中点
        return segmentWidth / 2;
    }

    // 计算基线方法（优化版）
    private double CalculateBaseline(byte[] data)
    {
        if (data == null || data.Length == 0) return 0;

        // 计算中值
        var sortedData = new List<byte>(data);
        sortedData.Sort();
        double median = sortedData[sortedData.Count / 2];

        // 计算低值区域的平均值（排除高值）
        double sum = 0;
        int count = 0;
        foreach (byte value in data)
        {
            if (value <= median * 1.5)
            {
                sum += value;
                count++;
            }
        }

        if (count == 0) return median;
        return sum / count;
    }

    // 动态边界检测（如果需要重新计算边界）
    private (int start, int end) FindPeakBoundaries(int[] data, int peakIndex, double baseline)
    {
        // 参数校验
        if (data == null || data.Length == 0 || peakIndex < 0 || peakIndex >= data.Length)
            return (-1, -1);

        // 动态阈值 = 基线 + (峰值-基线)*比例因子
        double dynamicThreshold = baseline + (data[peakIndex] - baseline) * 0.1;

        // 向左搜索起点
        int start = peakIndex;
        while (start > 0)
        {
            // 低于动态阈值
            if (data[start] < dynamicThreshold)
                break;

            // 检测局部最小值点（导数由负变正）
            if (start > 1 &&
                data[start-1] < data[start] &&
                data[start-1] < data[start-2])
            {
                break;
            }
            start--;
        }

        // 向右搜索终点
        int end = peakIndex;
        while (end < data.Length - 1)
        {
            // 低于动态阈值
            if (data[end] < dynamicThreshold)
                break;

            // 检测局部最小值点
            if (end < data.Length - 2 &&
                data[end+1] < data[end] &&
                data[end+1] < data[end+2])
            {
                break;
            }
            end++;
        }

        // 边界平滑处理（防止噪声干扰）
        start = Math.Max(0, start - 2);
        end = Math.Min(data.Length - 1, end + 2);

        // 确保最小峰宽
        int minPeakWidth = 5;
        if (end - start < minPeakWidth)
        {
            int center = (start + end) / 2;
            start = Math.Max(0, center - minPeakWidth/2);
            end = Math.Min(data.Length - 1, center + minPeakWidth/2);
        }

        return (start, end);
    }

    private ObservableCollection<AnalysisModeItem> analysisModes = new ObservableCollection<AnalysisModeItem>
{
    new AnalysisModeItem("Max Difference   Calculate Max Channel Diff (Max(|R-G|,|G-B|,|B-R|))", "Calculate maximum channel difference (Max(|R-G|, |G-B|, |B-R|))"),
    new AnalysisModeItem("Gray Fusion   Standard RGB to Gray (0.3R+0.6G+0.1B)", "Standard RGB to Gray formula (0.3R + 0.6G + 0.1B)"),
    new AnalysisModeItem("Standard Difference   Highlight Red vs Background (R-(G+B)/2)", "Highlight difference between red and background (R - (G+B)/2)"),
    new AnalysisModeItem("Enhanced Red   Boost Red Channel (1.6R-G-B)", "Boost red channel (1.6R - G - B)"),
    new AnalysisModeItem("Enhanced Green-Blue   Boost Green & Blue Channels (1.5*(G+B)-0.8*R)", "Boost green and blue channels (1.5*(G+B) - 0.8*R)"),
    new AnalysisModeItem("Green-Blue Difference   Highlight Green vs Blue (|G-B|)", "Highlight difference between green and blue (|G - B|)"),
    new AnalysisModeItem("Target Color   Enhance Specific Target Color", "Enhance specific target color (default: red)"),
   };

    private Dictionary<string, ImageProcess.ChannelDiffMode> modeMapping = new Dictionary<string, ImageProcess.ChannelDiffMode>
{
    {"Max Difference   Calculate Max Channel Diff (Max(|R-G|,|G-B|,|B-R|))", ImageProcess.ChannelDiffMode.MaxDifference},
    {"Gray Fusion   Standard RGB to Gray (0.3R+0.6G+0.1B)", ImageProcess.ChannelDiffMode.Gray},
    {"Standard Difference   Highlight Red vs Background (R-(G+B)/2)", ImageProcess.ChannelDiffMode.Standard},
    {"Enhanced Red   Boost Red Channel (1.6R-G-B)", ImageProcess.ChannelDiffMode.EnhancedRed},
    {"Enhanced Green-Blue   Boost Green & Blue Channels (1.5*(G+B)-0.8*R)", ImageProcess.ChannelDiffMode.EnhancedGreenBlue},
    {"Green-Blue Difference   Highlight Green vs Blue (|G-B|)", ImageProcess.ChannelDiffMode.GreenBlueDiff},
    {"Target Color   Enhance Specific Target Color", ImageProcess.ChannelDiffMode.TargetColor},
    
};
    int touchnum;
    private async void waveformCanvas_Touch(object sender, SKTouchEventArgs e)
    {
#if ANDROID
        touchnum++;
        if (touchnum % 2 == 0)
        {
            if (imageProcess._DataValues == null) return;

            try
            {
                string userInput = null;
                bool isValidInput = false;
                while (!isValidInput)
                {
                    userInput = await DisplayPromptAsync(
                        "仪器实测数据",
                        "请输入仪器实测的数据：",
                        "确认",
                        "不添加发送",
                        "0", 
                        maxLength: 20,
                        keyboard: Keyboard.Numeric);
                    if (userInput == null) break;
                    if (double.TryParse(userInput, out _))
                    {
                        isValidInput = true;
                    }
                    else
                    {
                        await DisplayAlert("输入错误", "请输入有效的数字", "确定");
                    }
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"DataValues_{timestamp}.txt";
                string folderPath = FileSystem.Current.AppDataDirectory;
                string filePath = Path.Combine(folderPath, fileName);

                var lines = new List<string>();
                lines.Add($"# T/C: {imageProcess.TCrate}");
                lines.Add($"# Time: {timestamp}");
                foreach (byte b in imageProcess._DataValues)
                {
                    lines.Add(b.ToString());
                }
                lines.Add(imageProcess.TCrate.ToString());
                if (isValidInput)
                {
                    lines.Add(userInput);
                }

                await File.WriteAllLinesAsync(filePath, lines);

                await Share.RequestAsync(new ShareFileRequest
                {
                    Title = "分享数据文件",
                    File = new ShareFile(filePath)
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误", $"保存/分享文件失败: {ex.Message}", "确定");
            }
        }
#endif
    }

}