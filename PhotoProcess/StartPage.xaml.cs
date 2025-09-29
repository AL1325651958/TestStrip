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
            // ȷ��ʹ�� await �����첽��������
            await Navigation.PushAsync(new MainPage(), animated: true);
        }
        catch (Exception ex)
        {
            // ������ʾ�쳣��Ϣ
            await DisplayAlert("����", $"����ʧ��: {ex.Message}", "ȷ��");
        }
    }

    private async void OnRGBAnalysisClicked(object sender, EventArgs e)
    {
        // ����RGB������ť���
        await Navigation.PushAsync(new RGBAnazy(), animated: true);
    }

    private async void OnPendingClicked(object sender, EventArgs e)
    {
        // ���������ť���
        await Navigation.PushAsync(new WirelessPage(), animated: true);
    }
}