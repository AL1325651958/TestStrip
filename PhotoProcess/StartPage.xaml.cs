namespace PhotoProcess;

public partial class StartPage : ContentPage
{
    public StartPage()
    {
        InitializeComponent();
    }

    private async void OnGrayscaleAnalysisClicked(object sender, EventArgs e)
    {
        try
        {
            // 确保使用 await 调用异步导航方法
            await Navigation.PushAsync(new MainPage(), animated: true);
        }
        catch (Exception ex)
        {
            // 捕获并显示异常信息
            await DisplayAlert("错误", $"导航失败: {ex.Message}", "确定");
        }
    }

    private async void OnRGBAnalysisClicked(object sender, EventArgs e)
    {
        // 处理RGB分析按钮点击
        await Navigation.PushAsync(new RGBAnazy(), animated: true);
    }

    private async void OnPendingClicked(object sender, EventArgs e)
    {
        // 处理待定按钮点击
        await Navigation.PushAsync(new WirelessPage(), animated: true);
    }
}