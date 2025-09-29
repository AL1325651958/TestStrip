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
        // 设置Picker的ItemsSource
        analysisModePicker.ItemsSource = analysisModes;

        // 设置显示属性
        analysisModePicker.ItemDisplayBinding = new Binding("Name");

        // 设置默认选择
        if (analysisModes.Count > 0)
        {
            analysisModePicker.SelectedIndex = 0;
        }
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

            // 更新位图引用
            imageProcess._originalBitmap?.Dispose();
            imageProcess._processedBitmap?.Dispose();
            imageProcess._originalBitmap = original.Copy();
            imageProcess._processedBitmap = imageProcess._originalBitmap.Copy(); // 保留原始彩色图像
            await UpdateImageDisplayAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", $"打开图片失败: {ex.Message}", "确定");
        }
        finally
        {
            // 隐藏加载指示器
            ProcessingIndicator.IsVisible = false;
            ProcessingIndicator.IsRunning = false;
        }
    }

    private async void AutoModeButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            ProcessingIndicator.IsVisible = true;
            ProcessingIndicator.IsRunning = true;
            AutoModeButton.IsEnabled = false;
            imageProcess._processedBitmap = imageProcess._originalBitmap.Copy(); ; // 更新图像处理类的引用
                                                                                   // 获取当前阈值
            byte threshold = 10;

            await Task.Run(async () =>
            {
                try
                {
                    // 步骤1: 灰度化
                    Device.BeginInvokeOnMainThread(() => ProcessingIndicator.IsRunning = true);
                    imageProcess.ApplyGrayscale(imageProcess._processedBitmap);
                    await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());

                    await imageProcess.FanApplyBinary(imageProcess._processedBitmap, 80);
                    // 更新显示
                    await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());
                    // 步骤3: 最大区域提取
                    using var binaryBitmap = imageProcess._processedBitmap.Copy();
                    SKRectI boundingBox = imageProcess.FindLargestConnectedComponent(binaryBitmap);
                    // 在原始彩色图像上裁剪该区域
                    imageProcess.CropToRegion(imageProcess._originalBitmap, boundingBox, ref imageProcess._processedBitmap);
                    // 更新显示
                    await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());
                    // 步骤4: 如果高大于宽则旋转90度
                    if (imageProcess._processedBitmap.Height > imageProcess._processedBitmap.Width)
                    {
                        imageProcess.Rotate90(ref imageProcess._processedBitmap);
                        // 更新显示
                        await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());
                    }

                    imageProcess._saveBitmap = imageProcess._processedBitmap.Copy();
                    //imageProcess.ApplyGrayscale(imageProcess._processedBitmap);
                    //await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());
                    //Device.BeginInvokeOnMainThread(() => {
                    //    waveformCanvas.InvalidateSurface();
                    //});

                    // 步骤5: 应用RGB滤波器
                    imageProcess.ApplyRGBFilter(imageProcess._processedBitmap);
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
            // 请求存储权限
            var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("权限被拒绝", "需要存储权限才能保存图片", "确定");
                return;
            }

            // 检查是否有裁剪后的彩色图像
            if (imageProcess._saveBitmap == null)
            {
                await DisplayAlert("错误", "没有可用的裁剪图像", "确定");
                return;
            }

            // 获取波形图尺寸
            int waveformWidth = (int)waveformCanvas.CanvasSize.Width;
            int waveformHeight = (int)waveformCanvas.CanvasSize.Height;

            // 确定合并图像的宽度（取两者中较大的宽度）
            int combinedWidth = Math.Max(imageProcess._saveBitmap.Width, waveformWidth);

            // 创建波形图（使用合并图像的宽度）
            using (var waveformSurface = SKSurface.Create(new SKImageInfo(combinedWidth, waveformHeight)))
            {
                var waveformCanvasSurface = waveformSurface.Canvas;

                // 重新绘制波形图以适应新宽度
                DrawWaveform(waveformCanvasSurface, combinedWidth, waveformHeight);

                using var waveformImage = waveformSurface.Snapshot();

                // 计算合并图像高度
                int combinedHeight = imageProcess._saveBitmap.Height + waveformHeight + 100; // 添加标题空间

                // 创建合并图像（裁剪图像在上，波形图在下）
                using (var combinedSurface = SKSurface.Create(new SKImageInfo(combinedWidth, combinedHeight)))
                {
                    var combinedCanvas = combinedSurface.Canvas;
                    combinedCanvas.Clear(SKColors.White);

                    // 添加标题
                    using var titlePaint = new SKPaint
                    {
                        Color = SKColors.Black,
                        TextSize = 24,
                        IsAntialias = true,
                        TextAlign = SKTextAlign.Center
                    };
                    //combinedCanvas.DrawText("CT试纸分析结果", combinedWidth / 2, 30, titlePaint);

                    // 绘制裁剪后的原始图像（居中）
                    int croppedX = (combinedWidth - imageProcess._saveBitmap.Width) / 2;
                    combinedCanvas.DrawBitmap(imageProcess._saveBitmap, croppedX, 50); // 标题下方留出空间

                    // 绘制波形图（居中）
                    int waveformX = (combinedWidth - combinedWidth) / 2; // 波形图宽度与合并图像相同
                    int waveformY = imageProcess._saveBitmap.Height + 50; // 标题下方留出空间
                    combinedCanvas.DrawImage(waveformImage, waveformX, waveformY);

                    // 保存合并图像
                    using var combinedImage = combinedSurface.Snapshot();
                    using var data = combinedImage.Encode(SKEncodedImageFormat.Png, 100);
                    string fileName = $"CT_Analysis_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                    string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                    // 保存文件到缓存
                    using (var stream = File.OpenWrite(filePath))
                    {
                        data.SaveTo(stream);
                    }

                    // 使用共享功能让用户选择保存位置（包括相册）
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
    private void OnChannelCheckedChanged(object sender, EventArgs e)
    {
        // 更新通道启用状态
        imageProcess._R_enabled = RedCheckBox.IsChecked;
        imageProcess._G_enabled = GreenCheckBox.IsChecked;
        imageProcess._B_enabled = BlueCheckBox.IsChecked;
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

        // 计算整个波形的平均值
        double average = 0;
        foreach (byte value in imageProcess._DataValues)
        {
            average += value;
        }
        average /= imageProcess._DataValues.Length;
        float averageY = (float)(height - average * scaleY);

        // 计算基线（使用整体数据的中位数，并适当提高）
        var allValues = new List<byte>(imageProcess._DataValues);
        allValues.Sort();
        double baseline = allValues[allValues.Count / 2]; // 中位数
        baseline = baseline * 1.2; // 提高20%
        baseline = Math.Min(baseline, 255); // 确保不超过255
        float baselineY = (float)(height - baseline * scaleY);

        /*
         * 滤波算法 - 使用移动平均滤波器
         */
        int windowSize = 5; // 滤波窗口大小（奇数）
        int halfWindow = windowSize / 2;
        byte[] filteredValues = new byte[segmentWidth];
        for (int i = 0; i < segmentWidth; i++)
        {
            int sum = 0;
            int count = 0;

            // 计算窗口内的平均值
            for (int j = -halfWindow; j <= halfWindow; j++)
            {
                if(i + j < 0 || i + j >= segmentWidth)
                    continue; // 跳过边界外的索引
                int index = i + j;
                if (index >= 0 && index < segmentWidth)
                {
                    sum += imageProcess._DataValues[index];
                    count++;
                }
            }

            filteredValues[i] = (byte)(sum / count);
        }

        // 中间数据设置为基线
        int centerIndex = segmentWidth / 2;
        int startIndex = Math.Max(0, centerIndex - 2);
        int endIndex = Math.Min(segmentWidth - 1, centerIndex + 2);
        byte baselineValue = (byte)Math.Max(0, Math.Min(255, baseline - 2)); // 计算基线值减2，并确保在0~255之间

        for (int i = startIndex; i <= endIndex; i++)
        {
            filteredValues[i] = baselineValue;
        }

        // 绘制整个波形（使用滤波后的数据）
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

        // 绘制基底线（绿色虚线）
        using var baselinePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Green,
            StrokeWidth = 1,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0)
        };
        canvas.DrawLine(0, baselineY, width, baselineY, baselinePaint);

        // 添加基线标签
        using var baselineTextPaint = new SKPaint
        {
            Color = SKColors.Green,
            TextSize = 20,
            IsAntialias = true
        };
        canvas.DrawText($"Base: {baseline:F1}", 10, baselineY - 5, baselineTextPaint);

        // ====== 修改部分：使用基于平均线的波峰检测算法（大于平均5%） ======
        // 将 _grayValues (byte[]) 转换为 int[]
        int[] grayIntValues = new int[filteredValues.Length];
        for (int i = 0; i < filteredValues.Length; i++)
        {
            grayIntValues[i] = filteredValues[i];
        }

        // 在方法内部检测波峰
        var peaks = imageProcess.FindPeaksBasedOnAverage(grayIntValues);

        // 修改：根据固定位置识别T线和C线（左边T线，右边C线）
        imageProcess.IdentifyTCPeaks(peaks, grayIntValues.Length);

        // 获取T线和C线
        var tPeak = peaks.FirstOrDefault(p => p.IsT);
        var cPeak = peaks.FirstOrDefault(p => p.IsC);

        // 计算T/C比值（基于面积）
        double ratio = 0;
        if (tPeak != null && cPeak != null && cPeak.Area > 0)
        {
            ratio = tPeak.Area / cPeak.Area;
        }
        else if (tPeak == null && cPeak != null && cPeak.Area > 0)
        {
            // 未找到T线，但C线存在 - T/C比值为0
            ratio = 0;
        }

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
                Color = new SKColor(0, 200, 0, 100), // 半透明绿色
                IsAntialias = true
            };

            // 调整波峰标签字体大小
            using var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 20,
                IsAntialias = true
            };

            // 调整CT比值字体大小
            using var ratioPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 40,
                IsAntialias = true
            };

            // 绘制所有检测到的波峰
            foreach (var peak in peaks)
            {
                // 确保峰值位置在数组范围内
                int safePosition = Math.Clamp(peak.Position, 0, grayIntValues.Length - 1);
                float x = safePosition * scaleX;
                float y = height - grayIntValues[safePosition] * scaleY;

                // 标记波峰中心
                canvas.DrawCircle(x, y, 5, highlightPaint);

                // 确保峰值范围在数组范围内
                int safeStart = Math.Clamp(peak.Start, 0, grayIntValues.Length - 1);
                int safeEnd = Math.Clamp(peak.End, 0, grayIntValues.Length - 1);

                // 确保开始位置不大于结束位置
                if (safeStart > safeEnd)
                {
                    (safeStart, safeEnd) = (safeEnd, safeStart);
                }

                float startX = safeStart * scaleX;
                float endX = safeEnd * scaleX;

                // 创建只包含高于基线部分的路径
                using (var peakPath = new SKPath())
                {
                    peakPath.MoveTo(startX, baselineY);

                    // 从起点到波峰起点
                    for (int i = safeStart; i <= safeEnd; i++)
                    {
                        // 确保索引在数组范围内
                        if (i < 0 || i >= grayIntValues.Length) continue;

                        float xi = i * scaleX;
                        // 使用滤波后的值，确保值在合理范围内
                        float dataValue = Math.Clamp(grayIntValues[i], 0, 255);
                        float yi = height - Math.Max((float)baseline, dataValue) * scaleY;
                        peakPath.LineTo(xi, yi);
                    }

                    // 从波峰终点回到起点
                    peakPath.LineTo(endX, baselineY);
                    peakPath.Close();

                    // 填充波峰区域
                    canvas.DrawPath(peakPath, fillPaint);
                }

                // 添加标签
                string label = peak.IsT ? "T" : peak.IsC ? "C" : "P";
                canvas.DrawText($"{label}:{peak.Area:F0}", x, y - 10, textPaint);
            }

            // 显示状态信息
            if (tPeak == null)
            {
                ratio = 0;
            }
            // 显示T/C比值（基于面积）
            string ratioText = $"T/C: {ratio:F4}";
            // 测量文本宽度以居中显示
            float textWidth = ratioPaint.MeasureText(ratioText);
            // 计算水平居中位置
            float centerX = (width - textWidth) / 2;
            // 垂直位置保持原样（40像素）
            canvas.DrawText(ratioText, centerX, 40, ratioPaint);
        }
    }

    private ObservableCollection<AnalysisModeItem> analysisModes = new ObservableCollection<AnalysisModeItem>
{
    new AnalysisModeItem("Gray Fusion   Standard RGB to Gray (0.3R+0.6G+0.1B)", "Standard RGB to Gray formula (0.3R + 0.6G + 0.1B)"),
    new AnalysisModeItem("Standard Difference   Highlight Red vs Background (R-(G+B)/2)", "Highlight difference between red and background (R - (G+B)/2)"),
    new AnalysisModeItem("Enhanced Red   Boost Red Channel (1.6R-G-B)", "Boost red channel (1.6R - G - B)"),
    new AnalysisModeItem("Enhanced Green-Blue   Boost Green & Blue Channels (1.5*(G+B)-0.8*R)", "Boost green and blue channels (1.5*(G+B) - 0.8*R)"),
    new AnalysisModeItem("Green-Blue Difference   Highlight Green vs Blue (|G-B|)", "Highlight difference between green and blue (|G - B|)"),
    new AnalysisModeItem("Target Color   Enhance Specific Target Color", "Enhance specific target color (default: red)"),
    new AnalysisModeItem("Max Difference   Calculate Max Channel Diff (Max(|R-G|,|G-B|,|B-R|))", "Calculate maximum channel difference (Max(|R-G|, |G-B|, |B-R|))"),
    new AnalysisModeItem("PCA Fusion   Principal Component Analysis Fusion", "Fuse channels using PCA (direction of max information)")
};

    private Dictionary<string, ImageProcess.ChannelDiffMode> modeMapping = new Dictionary<string, ImageProcess.ChannelDiffMode>
{
    {"Gray Fusion   Standard RGB to Gray (0.3R+0.6G+0.1B)", ImageProcess.ChannelDiffMode.Gray},
    {"Standard Difference   Highlight Red vs Background (R-(G+B)/2)", ImageProcess.ChannelDiffMode.Standard},
    {"Enhanced Red   Boost Red Channel (1.6R-G-B)", ImageProcess.ChannelDiffMode.EnhancedRed},
    {"Enhanced Green-Blue   Boost Green & Blue Channels (1.5*(G+B)-0.8*R)", ImageProcess.ChannelDiffMode.EnhancedGreenBlue},
    {"Green-Blue Difference   Highlight Green vs Blue (|G-B|)", ImageProcess.ChannelDiffMode.GreenBlueDiff},
    {"Target Color   Enhance Specific Target Color", ImageProcess.ChannelDiffMode.TargetColor},
    {"Max Difference   Calculate Max Channel Diff (Max(|R-G|,|G-B|,|B-R|))", ImageProcess.ChannelDiffMode.MaxDifference},
    {"PCA Fusion   Principal Component Analysis Fusion", ImageProcess.ChannelDiffMode.PCA}
};

    // 添加分析模式改变事件处理
    private void OnAnalysisModeChanged(object sender, EventArgs e)
    {
        if (analysisModePicker.SelectedIndex == -1) return;

        // 获取选中的项
        var selectedItem = analysisModePicker.SelectedItem as AnalysisModeItem;

        if (selectedItem != null)
        {
            if (modeMapping.TryGetValue(selectedItem.Name, out var mode))
            {
                // 更新当前分析模式
                imageProcess.CurrentAnalysisMode = mode;
            }
        }
    }

}