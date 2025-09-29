using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.Diagnostics;
using System.Net.Sockets;
namespace PhotoProcess;

public partial class WirelessPage : ContentPage
{
    private ImageProcessingMode? _currentMode = ImageProcessingMode.None;
    private bool _isProcessing = false;
    private bool _isWaveformVisible = false; // 跟踪波形图是否可见

    ImageProcess imageProcess = new ImageProcess();// 实例化图像处理类

    TcpHostService TcpHostService = new TcpHostService(12345);

    enum ImageProcessingMode
    {
        None,
        Grayscale,
        Binary,
        Gaussian,
        MaxRegion,
        Rotated
    }

    [Obsolete]
    public WirelessPage()
    {
        InitializeComponent();
        waveformCanvas.PaintSurface += OnWaveformPaintSurface;

        TcpHostService = new TcpHostService(12345);
        GetPicture.IsEnabled = false;
        AutoModeButton.IsEnabled = false;
        SaveWaveformButton.IsEnabled = false;
        // 订阅事件，当图片接收完成时触发
        TcpHostService.ImageReceived += OnImageReceived;
        // 订阅客户端连接事件
        TcpHostService.ClientConnected += OnClientConnected;
        // 订阅客户端断开事件
        TcpHostService.ClientDisconnected += OnClientDisconnected;
        TcpHostService.Start();
        ThresholdSlider.Value = 10;

        WaitForClientConnectionAsync();

    }
    private void OnClientDisconnected(TcpClient client)
    {
        //禁用所有按钮
        MainThread.BeginInvokeOnMainThread(() =>
        {
            GetPicture.IsEnabled = false;
            AutoModeButton.IsEnabled = false;
            SaveWaveformButton.IsEnabled = false;
        });
    }
    private void OnClientConnected(TcpClient client)
    {
        //启用所有按钮
        MainThread.BeginInvokeOnMainThread(() =>
        {
            GetPicture.IsEnabled = true;
            AutoModeButton.IsEnabled = true;
            SaveWaveformButton.IsEnabled = true;
        });

    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        TcpHostService.Stop();
    }

    // 按钮点击发送 'P' 指令，让树莓派拍照上传
    private async void Button_Clicked(object sender, EventArgs e)
    {
        if (_isProcessing) return;

        _isProcessing = true;
        ProcessingIndicator.IsVisible = true;
        ProcessingIndicator.IsRunning = true;

        try
        {
            await TcpHostService.SendCaptureCommandAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", $"发送失败: {ex.Message}", "确定");
        }
        finally
        {
            _isProcessing = false;
            ProcessingIndicator.IsRunning = false;
            ProcessingIndicator.IsVisible = false;
        }
    }

    private async void WaitForClientConnectionAsync()
    {
        // 显示弹窗
        var resultTask = MainThread.InvokeOnMainThreadAsync(async () =>
        {
             await DisplayAlert("等待连接", "正在等待服务器连接...", "确定");
        });

    }

    // 图片接收事件
    private void OnImageReceived(string filePath)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _isProcessing = true;
            ProcessingIndicator.IsVisible = true;
            ProcessingIndicator.IsRunning = true;

            try
            {
                SKBitmap original;
                using (var stream = File.OpenRead(filePath))
                {
                    original = SKBitmap.Decode(stream);
                }

                // 缩放过大图片
                if (original.Width * original.Height > 1_000_000)
                {
                    float scale = (float)Math.Sqrt(1_000_000f / (original.Width * original.Height));
                    int newWidth = (int)(original.Width * scale);
                    int newHeight = (int)(original.Height * scale);

                    var resized = new SKBitmap(newWidth, newHeight);
                    original.ScalePixels(resized, SKFilterQuality.Medium);
                    original.Dispose();
                    original = resized;
                }

                // 更新 imageProcess 位图
                imageProcess._originalBitmap?.Dispose();
                imageProcess._processedBitmap?.Dispose();
                imageProcess._originalBitmap = original;
                imageProcess._processedBitmap = original.Copy();

                // 重置处理状态
                _currentMode = ImageProcessingMode.None;
                ThresholdSlider.IsVisible = true;
                AutoModeButton.IsEnabled = true;
                SaveWaveformButton.IsEnabled = true;
                imageProcess._DataValues = null;
                _isWaveformVisible = false;
                waveformCanvas.IsVisible = false;

                // 更新 UI
                await UpdateImageDisplayAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误", $"处理图片失败: {ex.Message}", "确定");
            }
            finally
            {
                _isProcessing = false;
                ProcessingIndicator.IsRunning = false;
                ProcessingIndicator.IsVisible = false;
            }
        });
    }

    // 更新图片显示
    [Obsolete]
    private async Task UpdateImageDisplayAsync()
    {
        if (imageProcess._processedBitmap != null)
        {
            using var stream = new MemoryStream();
            imageProcess._processedBitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
            stream.Seek(0, SeekOrigin.Begin);

            var imageData = stream.ToArray(); // 避免延迟流问题
            await Device.InvokeOnMainThreadAsync(() =>
            {
                selectedImage.Source = ImageSource.FromStream(() => new MemoryStream(imageData));
                selectedImage.IsVisible = true; // 确保控件可见
                waveformCanvas.InvalidateSurface();
            });
        }
    }

    [Obsolete]
    private void ApplyGrayscale(SKBitmap bitmap)
    {
        using var pixmap = bitmap.PeekPixels();
        var pixels = pixmap.GetPixelSpan<byte>();
        int width = bitmap.Width;
        int height = bitmap.Height;
        int pixelCount = width * height;
        int middleY = height / 2;
        imageProcess._startX = (int)(width * 0.1);
        imageProcess._endX = (int)(width * 0.9);
        int segmentWidth = imageProcess._endX - imageProcess._startX;
        imageProcess._DataValues = new byte[segmentWidth];

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
            if (y == middleY && x >= imageProcess._startX && x < imageProcess._endX)
            {
                imageProcess._DataValues[x - imageProcess._startX] = gray;
            }
        }
    }
    private Task ApplyBinary(SKBitmap bitmap, byte threshold)
    {
        return Task.Run(() =>
        {
            using var pixmap = bitmap.PeekPixels();
            var pixels = pixmap.GetPixelSpan<byte>();
            int width = bitmap.Width;
            int height = bitmap.Height;
            int pixelCount = width * height;

            for (int i = 0; i < pixelCount; i++)
            {
                int idx = i * 4;
                byte gray = pixels[idx];
                byte binary = gray > threshold ? (byte)255 : (byte)0;

                pixels[idx] = binary;     // R
                pixels[idx + 1] = binary; // G
                pixels[idx + 2] = binary; // B
            }
        });
    }


    [Obsolete]
    private async void ThresholdSlider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (_isProcessing ||
            _currentMode != ImageProcessingMode.Binary ||
            imageProcess._processedBitmap == null ||
            imageProcess._originalBitmap == null)
            return;

        try
        {
            _isProcessing = true;
            ProcessingIndicator.IsVisible = true;
            ProcessingIndicator.IsRunning = true;
            var temp = imageProcess._originalBitmap.Copy();
            await Task.Run(() => ApplyGrayscale(temp));
            await ApplyBinary(temp, (byte)e.NewValue);
            if (imageProcess._processedBitmap != temp)
            {
                imageProcess._processedBitmap?.Dispose();
                imageProcess._processedBitmap = temp;
            }
            await UpdateImageDisplayAsync();
        }
        finally
        {
            _isProcessing = false;
            ProcessingIndicator.IsRunning = false;
            ProcessingIndicator.IsVisible = false;
        }
    }
    [Obsolete]
    private async void AutoMode_Clicked(object sender, EventArgs e)
    {
        if (_isProcessing || imageProcess._processedBitmap == null || imageProcess._originalBitmap == null) return;

        try
        {
            _isProcessing = true;
            ProcessingIndicator.IsVisible = true;
            ProcessingIndicator.IsRunning = true;
            AutoModeButton.IsEnabled = false;
            imageProcess._processedBitmap = imageProcess._originalBitmap.Copy(); ; // 更新图像处理类的引用
                                                                                   // 获取当前阈值
            byte threshold = (byte)ThresholdSlider.Value;

            await Task.Run(async () =>
            {
                try
                {
                    // 步骤1: 灰度化
                    Device.BeginInvokeOnMainThread(() => ProcessingIndicator.IsRunning = true);
                    imageProcess.ApplyGrayscale(imageProcess._processedBitmap);
                    _currentMode = ImageProcessingMode.Grayscale;
                    // 更新显示
                    await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());

                    // 步骤2: 二值化（使用当前阈值）
                    Device.BeginInvokeOnMainThread(() => ThresholdSlider.IsVisible = true);
                    await imageProcess.ApplyBinary(imageProcess._processedBitmap, threshold);
                    _currentMode = ImageProcessingMode.Binary;

                    // 更新显示
                    await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());

                    // 步骤3: 最大区域提取
                    using var binaryBitmap = imageProcess._processedBitmap.Copy();
                    SKRectI boundingBox = imageProcess.FindLargestConnectedComponent(binaryBitmap);

                    // 在原始彩色图像上裁剪该区域
                    imageProcess.CropToRegion(imageProcess._originalBitmap, boundingBox, ref imageProcess._processedBitmap);
                    _currentMode = ImageProcessingMode.MaxRegion;

                    // 更新显示
                    await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());

                    // 步骤4: 如果高大于宽则旋转90度
                    if (imageProcess._processedBitmap.Height > imageProcess._processedBitmap.Width)
                    {
                        imageProcess.Rotate90(ref imageProcess._processedBitmap);
                        _currentMode = ImageProcessingMode.Rotated;

                        // 更新显示
                        await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());
                    }
                    imageProcess._saveBitmap = imageProcess._processedBitmap;
                    // 步骤5: 灰度化（旋转后图像可能变为彩色）
                    imageProcess.ApplyGrayscale(imageProcess._processedBitmap);
                    _currentMode = ImageProcessingMode.Grayscale;

                    // 更新显示
                    await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());

                    // 步骤6: 灰度分析（显示波形图）
                    Device.BeginInvokeOnMainThread(() => {
                        _isWaveformVisible = true;
                        waveformCanvas.IsVisible = true;
                        waveformCanvas.InvalidateSurface();
                    });
                }

                catch (Exception ex)
                {
                    Debug.WriteLine($"自动模式失败: {ex}");
                }
            });
        }
        finally
        {
            _isProcessing = false;
            ProcessingIndicator.IsRunning = false;
            ProcessingIndicator.IsVisible = false;
            AutoModeButton.IsEnabled = true;
        }
    }

    // 将 SaveWaveform_Clicked 方法中的局部变量 waveformCanvas 重命名为 waveformSkCanvas，避免与字段 waveformCanvas 冲突
    [Obsolete]
    private async void SaveWaveform_Clicked(object sender, EventArgs e)
    {
        if (!_isWaveformVisible || imageProcess._DataValues == null)
        {
            await DisplayAlert("提示", "请先进行灰度分析并显示波形图", "确定");
            return;
        }

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
            if (imageProcess._croppedColorBitmap == null)
            {
                await DisplayAlert("错误", "没有可用的裁剪图像", "确定");
                return;
            }

            // 获取波形图尺寸
            int waveformWidth = (int)waveformCanvas.CanvasSize.Width;
            int waveformHeight = (int)waveformCanvas.CanvasSize.Height;

            // 确定合并图像的宽度（取两者中较大的宽度）
            int combinedWidth = Math.Max(imageProcess._croppedColorBitmap.Width, waveformWidth);

            // 创建波形图（使用合并图像的宽度）
            using (var waveformSurface = SKSurface.Create(new SKImageInfo(combinedWidth, waveformHeight)))
            {
                var waveformCanvasSurface = waveformSurface.Canvas;

                // 重新绘制波形图以适应新宽度
                DrawWaveform(waveformCanvasSurface, combinedWidth, waveformHeight);

                using var waveformImage = waveformSurface.Snapshot();

                // 计算合并图像高度
                int combinedHeight = imageProcess._croppedColorBitmap.Height + waveformHeight + 100; // 添加标题空间

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
                    combinedCanvas.DrawText("CT试纸分析结果", combinedWidth / 2, 30, titlePaint);

                    // 绘制裁剪后的原始图像（居中）
                    int croppedX = (combinedWidth - imageProcess._croppedColorBitmap.Width) / 2;
                    combinedCanvas.DrawBitmap(imageProcess._croppedColorBitmap, croppedX, 50); // 标题下方留出空间

                    // 绘制波形图（居中）
                    int waveformX = (combinedWidth - combinedWidth) / 2; // 波形图宽度与合并图像相同
                    int waveformY = imageProcess._croppedColorBitmap.Height + 50; // 标题下方留出空间
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

    [Obsolete]
    private void DrawWaveform(SKCanvas canvas, int width, int height)
    {
        if (imageProcess._DataValues == null) return;
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
        baseline = baseline * 1.1; // 提高5%
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
                int index = i + j;
                if (index >= 0 && index < segmentWidth)
                {
                    sum += imageProcess._DataValues[index];
                    count++;
                }
            }

            filteredValues[i] = (byte)(sum / count);
        }

        // 绘制原始波形（使用滤波后的数据）
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
        int[] grayIntValues = new int[imageProcess._DataValues.Length];
        for (int i = 0; i < imageProcess._DataValues.Length; i++)
        {
            grayIntValues[i] = imageProcess._DataValues[i];
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
                float x = peak.Position * scaleX;
                float y = height - imageProcess._DataValues[peak.Position] * scaleY;

                // 标记波峰中心
                canvas.DrawCircle(x, y, 5, highlightPaint);

                // 绘制波峰区域（只绘制高于基线的部分）
                float startX = peak.Start * scaleX;
                float endX = peak.End * scaleX;

                // 创建只包含高于基线部分的路径
                using (var peakPath = new SKPath())
                {
                    peakPath.MoveTo(startX, baselineY);

                    // 从起点到波峰起点
                    for (int i = peak.Start; i <= peak.End; i++)
                    {
                        float xi = i * scaleX;
                        // 显式转换为float
                        float yi = height - (float)Math.Max(baseline, imageProcess._DataValues[i]) * scaleY;
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
    [Obsolete]
    private void OnWaveformPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        if (!_isWaveformVisible) return;
        var surface = e.Surface;
        var canvas = surface.Canvas;
        var info = e.Info;
        DrawWaveform(canvas, info.Width, info.Height);
    }

}