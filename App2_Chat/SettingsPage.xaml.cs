namespace App2_Chat; //Cambiar a App2_Chat en el otro proyecto

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        // Cargar valores guardados o usar defaults
        EntIpBroker.Text = Preferences.Get("BrokerIp", "localhost");
        EntColaPublish.Text = Preferences.Get("QueuePub", "cola2");
        EntColaConsume.Text = Preferences.Get("QueueSub", "cola1");

        EntLlmUrl.Text = Preferences.Get("LlmUrl", "http://localhost:1234/v1/chat/completions");
        EntModel.Text = Preferences.Get("LlmModel", "llama-3.2-1b-instruct");
        EdSystemPrompt.Text = Preferences.Get("SysPrompt", "Eres un asistente útil.");
        EntTemp.Text = Preferences.Get("LlmTemp", "0.7");
        EntTokens.Text = Preferences.Get("LlmTokens", "150");
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Guardar valores
        Preferences.Set("BrokerIp", EntIpBroker.Text);
        Preferences.Set("QueuePub", EntColaPublish.Text);
        Preferences.Set("QueueSub", EntColaConsume.Text);

        Preferences.Set("LlmUrl", EntLlmUrl.Text);
        Preferences.Set("LlmModel", EntModel.Text);
        Preferences.Set("SysPrompt", EdSystemPrompt.Text);
        Preferences.Set("LlmTemp", EntTemp.Text);
        Preferences.Set("LlmTokens", EntTokens.Text);

        await DisplayAlert("Éxito", "Configuración guardada", "OK");

        // Volver atrás
        await Navigation.PopAsync();
    }
}
