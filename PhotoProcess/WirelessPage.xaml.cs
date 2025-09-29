using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.Diagnostics;
using System.Net.Sockets;
namespace PhotoProcess;

public partial class WirelessPage : ContentPage
{
    private ImageProcessingMode? _currentMode = ImageProcessingMode.None;
    private bool _isProcessing = false;
    private bool _isWaveformVisible = false; // ���ٲ���ͼ�Ƿ�ɼ�

    ImageProcess imageProcess = new ImageProcess();// ʵ����ͼ������

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
        // �����¼�����ͼƬ�������ʱ����
        TcpHostService.ImageReceived += OnImageReceived;
        // ���Ŀͻ��������¼�
        TcpHostService.ClientConnected += OnClientConnected;
        // ���Ŀͻ��˶Ͽ��¼�
        TcpHostService.ClientDisconnected += OnClientDisconnected;
        TcpHostService.Start();
        ThresholdSlider.Value = 10;

        WaitForClientConnectionAsync();

    }
    private void OnClientDisconnected(TcpClient client)
    {
        //�������а�ť
        MainThread.BeginInvokeOnMainThread(() =>
        {
            GetPicture.IsEnabled = false;
            AutoModeButton.IsEnabled = false;
            SaveWaveformButton.IsEnabled = false;
        });
    }
    private void OnClientConnected(TcpClient client)
    {
        //�������а�ť
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

    // ��ť������� 'P' ָ�����ݮ�������ϴ�
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
            await DisplayAlert("����", $"����ʧ��: {ex.Message}", "ȷ��");
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
        // ��ʾ����
        var resultTask = MainThread.InvokeOnMainThreadAsync(async () =>
        {
             await DisplayAlert("�ȴ�����", "���ڵȴ�����������...", "ȷ��");
        });

    }

    // ͼƬ�����¼�
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

                // ���Ź���ͼƬ
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

                // ���� imageProcess λͼ
                imageProcess._originalBitmap?.Dispose();
                imageProcess._processedBitmap?.Dispose();
                imageProcess._originalBitmap = original;
                imageProcess._processedBitmap = original.Copy();

                // ���ô���״̬
                _currentMode = ImageProcessingMode.None;
                ThresholdSlider.IsVisible = true;
                AutoModeButton.IsEnabled = true;
                SaveWaveformButton.IsEnabled = true;
                imageProcess._DataValues = null;
                _isWaveformVisible = false;
                waveformCanvas.IsVisible = false;

                // ���� UI
                await UpdateImageDisplayAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("����", $"����ͼƬʧ��: {ex.Message}", "ȷ��");
            }
            finally
            {
                _isProcessing = false;
                ProcessingIndicator.IsRunning = false;
                ProcessingIndicator.IsVisible = false;
            }
        });
    }

    // ����ͼƬ��ʾ
    [Obsolete]
    private async Task UpdateImageDisplayAsync()
    {
        if (imageProcess._processedBitmap != null)
        {
            using var stream = new MemoryStream();
            imageProcess._processedBitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
            stream.Seek(0, SeekOrigin.Begin);

            var imageData = stream.ToArray(); // �����ӳ�������
            await Device.InvokeOnMainThreadAsync(() =>
            {
                selectedImage.Source = ImageSource.FromStream(() => new MemoryStream(imageData));
                selectedImage.IsVisible = true; // ȷ���ؼ��ɼ�
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
            int idx = i * 4;  // ÿ������4�ֽ� (RGBA)
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
            imageProcess._processedBitmap = imageProcess._originalBitmap.Copy(); ; // ����ͼ�����������
                                                                                   // ��ȡ��ǰ��ֵ
            byte threshold = (byte)ThresholdSlider.Value;

            await Task.Run(async () =>
            {
                try
                {
                    // ����1: �ҶȻ�
                    Device.BeginInvokeOnMainThread(() => ProcessingIndicator.IsRunning = true);
                    imageProcess.ApplyGrayscale(imageProcess._processedBitmap);
                    _currentMode = ImageProcessingMode.Grayscale;
                    // ������ʾ
                    await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());

                    // ����2: ��ֵ����ʹ�õ�ǰ��ֵ��
                    Device.BeginInvokeOnMainThread(() => ThresholdSlider.IsVisible = true);
                    await imageProcess.ApplyBinary(imageProcess._processedBitmap, threshold);
                    _currentMode = ImageProcessingMode.Binary;

                    // ������ʾ
                    await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());

                    // ����3: ���������ȡ
                    using var binaryBitmap = imageProcess._processedBitmap.Copy();
                    SKRectI boundingBox = imageProcess.FindLargestConnectedComponent(binaryBitmap);

                    // ��ԭʼ��ɫͼ���ϲü�������
                    imageProcess.CropToRegion(imageProcess._originalBitmap, boundingBox, ref imageProcess._processedBitmap);
                    _currentMode = ImageProcessingMode.MaxRegion;

                    // ������ʾ
                    await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());

                    // ����4: ����ߴ��ڿ�����ת90��
                    if (imageProcess._processedBitmap.Height > imageProcess._processedBitmap.Width)
                    {
                        imageProcess.Rotate90(ref imageProcess._processedBitmap);
                        _currentMode = ImageProcessingMode.Rotated;

                        // ������ʾ
                        await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());
                    }
                    imageProcess._saveBitmap = imageProcess._processedBitmap;
                    // ����5: �ҶȻ�����ת��ͼ����ܱ�Ϊ��ɫ��
                    imageProcess.ApplyGrayscale(imageProcess._processedBitmap);
                    _currentMode = ImageProcessingMode.Grayscale;

                    // ������ʾ
                    await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());

                    // ����6: �Ҷȷ�������ʾ����ͼ��
                    Device.BeginInvokeOnMainThread(() => {
                        _isWaveformVisible = true;
                        waveformCanvas.IsVisible = true;
                        waveformCanvas.InvalidateSurface();
                    });
                }

                catch (Exception ex)
                {
                    Debug.WriteLine($"�Զ�ģʽʧ��: {ex}");
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

    // �� SaveWaveform_Clicked �����еľֲ����� waveformCanvas ������Ϊ waveformSkCanvas���������ֶ� waveformCanvas ��ͻ
    [Obsolete]
    private async void SaveWaveform_Clicked(object sender, EventArgs e)
    {
        if (!_isWaveformVisible || imageProcess._DataValues == null)
        {
            await DisplayAlert("��ʾ", "���Ƚ��лҶȷ�������ʾ����ͼ", "ȷ��");
            return;
        }

        try
        {
            // ����洢Ȩ��
            var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Ȩ�ޱ��ܾ�", "��Ҫ�洢Ȩ�޲��ܱ���ͼƬ", "ȷ��");
                return;
            }

            // ����Ƿ��вü���Ĳ�ɫͼ��
            if (imageProcess._croppedColorBitmap == null)
            {
                await DisplayAlert("����", "û�п��õĲü�ͼ��", "ȷ��");
                return;
            }

            // ��ȡ����ͼ�ߴ�
            int waveformWidth = (int)waveformCanvas.CanvasSize.Width;
            int waveformHeight = (int)waveformCanvas.CanvasSize.Height;

            // ȷ���ϲ�ͼ��Ŀ�ȣ�ȡ�����нϴ�Ŀ�ȣ�
            int combinedWidth = Math.Max(imageProcess._croppedColorBitmap.Width, waveformWidth);

            // ��������ͼ��ʹ�úϲ�ͼ��Ŀ�ȣ�
            using (var waveformSurface = SKSurface.Create(new SKImageInfo(combinedWidth, waveformHeight)))
            {
                var waveformCanvasSurface = waveformSurface.Canvas;

                // ���»��Ʋ���ͼ����Ӧ�¿��
                DrawWaveform(waveformCanvasSurface, combinedWidth, waveformHeight);

                using var waveformImage = waveformSurface.Snapshot();

                // ����ϲ�ͼ��߶�
                int combinedHeight = imageProcess._croppedColorBitmap.Height + waveformHeight + 100; // ��ӱ���ռ�

                // �����ϲ�ͼ�񣨲ü�ͼ�����ϣ�����ͼ���£�
                using (var combinedSurface = SKSurface.Create(new SKImageInfo(combinedWidth, combinedHeight)))
                {
                    var combinedCanvas = combinedSurface.Canvas;
                    combinedCanvas.Clear(SKColors.White);

                    // ��ӱ���
                    using var titlePaint = new SKPaint
                    {
                        Color = SKColors.Black,
                        TextSize = 24,
                        IsAntialias = true,
                        TextAlign = SKTextAlign.Center
                    };
                    combinedCanvas.DrawText("CT��ֽ�������", combinedWidth / 2, 30, titlePaint);

                    // ���Ʋü����ԭʼͼ�񣨾��У�
                    int croppedX = (combinedWidth - imageProcess._croppedColorBitmap.Width) / 2;
                    combinedCanvas.DrawBitmap(imageProcess._croppedColorBitmap, croppedX, 50); // �����·������ռ�

                    // ���Ʋ���ͼ�����У�
                    int waveformX = (combinedWidth - combinedWidth) / 2; // ����ͼ�����ϲ�ͼ����ͬ
                    int waveformY = imageProcess._croppedColorBitmap.Height + 50; // �����·������ռ�
                    combinedCanvas.DrawImage(waveformImage, waveformX, waveformY);

                    // ����ϲ�ͼ��
                    using var combinedImage = combinedSurface.Snapshot();
                    using var data = combinedImage.Encode(SKEncodedImageFormat.Png, 100);
                    string fileName = $"CT_Analysis_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                    string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                    // �����ļ�������
                    using (var stream = File.OpenWrite(filePath))
                    {
                        data.SaveTo(stream);
                    }

                    // ʹ�ù��������û�ѡ�񱣴�λ�ã�������ᣩ
                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = "CT��ֽ�������",
                        File = new ShareFile(filePath)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("����", $"����������ʧ��: {ex.Message}", "ȷ��");
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

        // �����������ε�ƽ��ֵ
        double average = 0;
        foreach (byte value in imageProcess._DataValues)
        {
            average += value;
        }
        average /= imageProcess._DataValues.Length;
        float averageY = (float)(height - average * scaleY);

        // ������ߣ�ʹ���������ݵ���λ�������ʵ���ߣ�
        var allValues = new List<byte>(imageProcess._DataValues);
        allValues.Sort();
        double baseline = allValues[allValues.Count / 2]; // ��λ��
        baseline = baseline * 1.1; // ���5%
        baseline = Math.Min(baseline, 255); // ȷ��������255
        float baselineY = (float)(height - baseline * scaleY);

        /*
         * �˲��㷨 - ʹ���ƶ�ƽ���˲���
         */
        int windowSize = 5; // �˲����ڴ�С��������
        int halfWindow = windowSize / 2;
        byte[] filteredValues = new byte[segmentWidth];

        for (int i = 0; i < segmentWidth; i++)
        {
            int sum = 0;
            int count = 0;

            // ���㴰���ڵ�ƽ��ֵ
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

        // ����ԭʼ���Σ�ʹ���˲�������ݣ�
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

        // ����ƽ��ֵ��
        using var avgPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Gray,
            StrokeWidth = 1,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new float[] { 3, 3 }, 0)
        };
        canvas.DrawLine(0, averageY, width, averageY, avgPaint);

        // ���ƻ����ߣ���ɫ���ߣ�
        using var baselinePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Green,
            StrokeWidth = 1,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0)
        };
        canvas.DrawLine(0, baselineY, width, baselineY, baselinePaint);

        // ��ӻ��߱�ǩ
        using var baselineTextPaint = new SKPaint
        {
            Color = SKColors.Green,
            TextSize = 20,
            IsAntialias = true
        };
        canvas.DrawText($"Base: {baseline:F1}", 10, baselineY - 5, baselineTextPaint);

        // ====== �޸Ĳ��֣�ʹ�û���ƽ���ߵĲ������㷨������ƽ��5%�� ======
        // �� _grayValues (byte[]) ת��Ϊ int[]
        int[] grayIntValues = new int[imageProcess._DataValues.Length];
        for (int i = 0; i < imageProcess._DataValues.Length; i++)
        {
            grayIntValues[i] = imageProcess._DataValues[i];
        }

        // �ڷ����ڲ���Ⲩ��
        var peaks = imageProcess.FindPeaksBasedOnAverage(grayIntValues);

        // �޸ģ����ݹ̶�λ��ʶ��T�ߺ�C�ߣ����T�ߣ��ұ�C�ߣ�
        imageProcess.IdentifyTCPeaks(peaks, grayIntValues.Length);

        // ��ȡT�ߺ�C��
        var tPeak = peaks.FirstOrDefault(p => p.IsT);
        var cPeak = peaks.FirstOrDefault(p => p.IsC);

        // ����T/C��ֵ�����������
        double ratio = 0;
        if (tPeak != null && cPeak != null && cPeak.Area > 0)
        {
            ratio = tPeak.Area / cPeak.Area;
        }
        else if (tPeak == null && cPeak != null && cPeak.Area > 0)
        {
            // δ�ҵ�T�ߣ���C�ߴ��� - T/C��ֵΪ0
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
                Color = new SKColor(0, 200, 0, 100), // ��͸����ɫ
                IsAntialias = true
            };

            // ���������ǩ�����С
            using var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 20,
                IsAntialias = true
            };

            // ����CT��ֵ�����С
            using var ratioPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 40,
                IsAntialias = true
            };

            // �������м�⵽�Ĳ���
            foreach (var peak in peaks)
            {
                float x = peak.Position * scaleX;
                float y = height - imageProcess._DataValues[peak.Position] * scaleY;

                // ��ǲ�������
                canvas.DrawCircle(x, y, 5, highlightPaint);

                // ���Ʋ�������ֻ���Ƹ��ڻ��ߵĲ��֣�
                float startX = peak.Start * scaleX;
                float endX = peak.End * scaleX;

                // ����ֻ�������ڻ��߲��ֵ�·��
                using (var peakPath = new SKPath())
                {
                    peakPath.MoveTo(startX, baselineY);

                    // ����㵽�������
                    for (int i = peak.Start; i <= peak.End; i++)
                    {
                        float xi = i * scaleX;
                        // ��ʽת��Ϊfloat
                        float yi = height - (float)Math.Max(baseline, imageProcess._DataValues[i]) * scaleY;
                        peakPath.LineTo(xi, yi);
                    }

                    // �Ӳ����յ�ص����
                    peakPath.LineTo(endX, baselineY);
                    peakPath.Close();

                    // ��䲨������
                    canvas.DrawPath(peakPath, fillPaint);
                }

                // ��ӱ�ǩ
                string label = peak.IsT ? "T" : peak.IsC ? "C" : "P";
                canvas.DrawText($"{label}:{peak.Area:F0}", x, y - 10, textPaint);
            }

            // ��ʾ״̬��Ϣ
            if (tPeak == null)
            {
                ratio = 0;
            }
            // ��ʾT/C��ֵ�����������
            string ratioText = $"T/C: {ratio:F4}";
            // �����ı�����Ծ�����ʾ
            float textWidth = ratioPaint.MeasureText(ratioText);
            // ����ˮƽ����λ��
            float centerX = (width - textWidth) / 2;
            // ��ֱλ�ñ���ԭ����40���أ�
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