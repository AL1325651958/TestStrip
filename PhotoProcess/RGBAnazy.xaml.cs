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
        // ����Picker��ItemsSource
        analysisModePicker.ItemsSource = analysisModes;

        // ������ʾ����
        analysisModePicker.ItemDisplayBinding = new Binding("Name");

        // ����Ĭ��ѡ��
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
                await DisplayAlert("Ȩ�ޱ��ܾ�", "��Ҫ�洢Ȩ�޲��ܷ���ͼƬ", "ȷ��");
                return;
            }
            var options = new PickOptions
            {
                PickerTitle = "ѡ��ͼƬ",
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

            // ����λͼ����
            imageProcess._originalBitmap?.Dispose();
            imageProcess._processedBitmap?.Dispose();
            imageProcess._originalBitmap = original.Copy();
            imageProcess._processedBitmap = imageProcess._originalBitmap.Copy(); // ����ԭʼ��ɫͼ��
            await UpdateImageDisplayAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("����", $"��ͼƬʧ��: {ex.Message}", "ȷ��");
        }
        finally
        {
            // ���ؼ���ָʾ��
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
            imageProcess._processedBitmap = imageProcess._originalBitmap.Copy(); ; // ����ͼ�����������
                                                                                   // ��ȡ��ǰ��ֵ
            byte threshold = 10;

            await Task.Run(async () =>
            {
                try
                {
                    // ����1: �ҶȻ�
                    Device.BeginInvokeOnMainThread(() => ProcessingIndicator.IsRunning = true);
                    imageProcess.ApplyGrayscale(imageProcess._processedBitmap);
                    await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());

                    await imageProcess.FanApplyBinary(imageProcess._processedBitmap, 80);
                    // ������ʾ
                    await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());
                    // ����3: ���������ȡ
                    using var binaryBitmap = imageProcess._processedBitmap.Copy();
                    SKRectI boundingBox = imageProcess.FindLargestConnectedComponent(binaryBitmap);
                    // ��ԭʼ��ɫͼ���ϲü�������
                    imageProcess.CropToRegion(imageProcess._originalBitmap, boundingBox, ref imageProcess._processedBitmap);
                    // ������ʾ
                    await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());
                    // ����4: ����ߴ��ڿ�����ת90��
                    if (imageProcess._processedBitmap.Height > imageProcess._processedBitmap.Width)
                    {
                        imageProcess.Rotate90(ref imageProcess._processedBitmap);
                        // ������ʾ
                        await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());
                    }

                    imageProcess._saveBitmap = imageProcess._processedBitmap.Copy();
                    //imageProcess.ApplyGrayscale(imageProcess._processedBitmap);
                    //await Device.InvokeOnMainThreadAsync(async () => await UpdateImageDisplayAsync());
                    //Device.BeginInvokeOnMainThread(() => {
                    //    waveformCanvas.InvalidateSurface();
                    //});

                    // ����5: Ӧ��RGB�˲���
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
            // ����洢Ȩ��
            var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Ȩ�ޱ��ܾ�", "��Ҫ�洢Ȩ�޲��ܱ���ͼƬ", "ȷ��");
                return;
            }

            // ����Ƿ��вü���Ĳ�ɫͼ��
            if (imageProcess._saveBitmap == null)
            {
                await DisplayAlert("����", "û�п��õĲü�ͼ��", "ȷ��");
                return;
            }

            // ��ȡ����ͼ�ߴ�
            int waveformWidth = (int)waveformCanvas.CanvasSize.Width;
            int waveformHeight = (int)waveformCanvas.CanvasSize.Height;

            // ȷ���ϲ�ͼ��Ŀ�ȣ�ȡ�����нϴ�Ŀ�ȣ�
            int combinedWidth = Math.Max(imageProcess._saveBitmap.Width, waveformWidth);

            // ��������ͼ��ʹ�úϲ�ͼ��Ŀ�ȣ�
            using (var waveformSurface = SKSurface.Create(new SKImageInfo(combinedWidth, waveformHeight)))
            {
                var waveformCanvasSurface = waveformSurface.Canvas;

                // ���»��Ʋ���ͼ����Ӧ�¿��
                DrawWaveform(waveformCanvasSurface, combinedWidth, waveformHeight);

                using var waveformImage = waveformSurface.Snapshot();

                // ����ϲ�ͼ��߶�
                int combinedHeight = imageProcess._saveBitmap.Height + waveformHeight + 100; // ��ӱ���ռ�

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
                    //combinedCanvas.DrawText("CT��ֽ�������", combinedWidth / 2, 30, titlePaint);

                    // ���Ʋü����ԭʼͼ�񣨾��У�
                    int croppedX = (combinedWidth - imageProcess._saveBitmap.Width) / 2;
                    combinedCanvas.DrawBitmap(imageProcess._saveBitmap, croppedX, 50); // �����·������ռ�

                    // ���Ʋ���ͼ�����У�
                    int waveformX = (combinedWidth - combinedWidth) / 2; // ����ͼ�����ϲ�ͼ����ͬ
                    int waveformY = imageProcess._saveBitmap.Height + 50; // �����·������ռ�
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

    private void OnWaveformPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var surface = e.Surface;
        var canvas = surface.Canvas;
        var info = e.Info;
        DrawWaveform(canvas, info.Width, info.Height);
    }
    private void OnChannelCheckedChanged(object sender, EventArgs e)
    {
        // ����ͨ������״̬
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
        baseline = baseline * 1.2; // ���20%
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
                if(i + j < 0 || i + j >= segmentWidth)
                    continue; // �����߽��������
                int index = i + j;
                if (index >= 0 && index < segmentWidth)
                {
                    sum += imageProcess._DataValues[index];
                    count++;
                }
            }

            filteredValues[i] = (byte)(sum / count);
        }

        // �м���������Ϊ����
        int centerIndex = segmentWidth / 2;
        int startIndex = Math.Max(0, centerIndex - 2);
        int endIndex = Math.Min(segmentWidth - 1, centerIndex + 2);
        byte baselineValue = (byte)Math.Max(0, Math.Min(255, baseline - 2)); // �������ֵ��2����ȷ����0~255֮��

        for (int i = startIndex; i <= endIndex; i++)
        {
            filteredValues[i] = baselineValue;
        }

        // �����������Σ�ʹ���˲�������ݣ�
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
        int[] grayIntValues = new int[filteredValues.Length];
        for (int i = 0; i < filteredValues.Length; i++)
        {
            grayIntValues[i] = filteredValues[i];
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
                // ȷ����ֵλ�������鷶Χ��
                int safePosition = Math.Clamp(peak.Position, 0, grayIntValues.Length - 1);
                float x = safePosition * scaleX;
                float y = height - grayIntValues[safePosition] * scaleY;

                // ��ǲ�������
                canvas.DrawCircle(x, y, 5, highlightPaint);

                // ȷ����ֵ��Χ�����鷶Χ��
                int safeStart = Math.Clamp(peak.Start, 0, grayIntValues.Length - 1);
                int safeEnd = Math.Clamp(peak.End, 0, grayIntValues.Length - 1);

                // ȷ����ʼλ�ò����ڽ���λ��
                if (safeStart > safeEnd)
                {
                    (safeStart, safeEnd) = (safeEnd, safeStart);
                }

                float startX = safeStart * scaleX;
                float endX = safeEnd * scaleX;

                // ����ֻ�������ڻ��߲��ֵ�·��
                using (var peakPath = new SKPath())
                {
                    peakPath.MoveTo(startX, baselineY);

                    // ����㵽�������
                    for (int i = safeStart; i <= safeEnd; i++)
                    {
                        // ȷ�����������鷶Χ��
                        if (i < 0 || i >= grayIntValues.Length) continue;

                        float xi = i * scaleX;
                        // ʹ���˲����ֵ��ȷ��ֵ�ں���Χ��
                        float dataValue = Math.Clamp(grayIntValues[i], 0, 255);
                        float yi = height - Math.Max((float)baseline, dataValue) * scaleY;
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

    // ��ӷ���ģʽ�ı��¼�����
    private void OnAnalysisModeChanged(object sender, EventArgs e)
    {
        if (analysisModePicker.SelectedIndex == -1) return;

        // ��ȡѡ�е���
        var selectedItem = analysisModePicker.SelectedItem as AnalysisModeItem;

        if (selectedItem != null)
        {
            if (modeMapping.TryGetValue(selectedItem.Name, out var mode))
            {
                // ���µ�ǰ����ģʽ
                imageProcess.CurrentAnalysisMode = mode;
            }
        }
    }

}