using App1_Chat.Services;
using System.Collections.ObjectModel;

namespace App1_Chat;

// donde unimos toda la lógica de la aplicación, la UI y los servicios, es el corazón de la app
public partial class MainPage : ContentPage
{
    private RabbitService _rabbitService;
    private LlmService _llmService;
    private bool _isConnected = false;

    public MainPage()
    {
        InitializeComponent();
        _rabbitService = new RabbitService();
        _llmService = new LlmService();
    }

    // Botón Conectar a RabbitMQ con preferencias guardadas
    private async void OnConnectClicked(object sender, EventArgs e)
    {
        if (_isConnected) return;

        string ip = Preferences.Get("BrokerIp", "localhost");
        string qPub = Preferences.Get("QueuePub", "cola1");
        string qSub = Preferences.Get("QueueSub", "cola2");

        try
        {
            // Suscribirse al evento de recibir mensaje, cuando llegue un mensaje de la otra app, se ejecutará el método HandleMessageReceived
            _rabbitService.OnMessageReceived += HandleMessageReceived;

            await _rabbitService.InitializeAsync(ip, qPub, qSub);

            _isConnected = true;

            // Actualizar UI cuando conecta exitosamente
            LblStatus.Text = "En línea";
            LblStatus.TextColor = Colors.White;

            BtnConnect.IsEnabled = false;
            BtnConnect.Opacity = 0.5;

            BtnDisconnect.IsEnabled = true;
            BtnDisconnect.Opacity = 1.0;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error Conexión", ex.Message, "OK");
        }
    }

    // Evento: Cuando llega un mensaje de la OTRA App
    private void HandleMessageReceived(string message)
    {
        // RabbitMQ corre en otro hilo, volvemos al hilo principal para tocar la UI
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // 1. Mostrar mensaje recibido (Izquierda - Blanco)
            AddChatBubble(message, false);

            // --- NUEVO: TEXTO A VOZ (Ahora lee lo que RECIBIMOS) ---
            if (SwVoz.IsToggled)
            {
                try
                {
                    // Leemos el mensaje del "enemigo" antes de ponernos a pensar
                    await TextToSpeech.Default.SpeakAsync(message, new SpeechOptions
                    {
                        Volume = 1.0f,
                        Pitch = 1.0f // Voz neutra para leer lo que dice el otro (o pon 0.8f si quieres que suene grave como el perro)
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error TextToSpeech: " + ex.Message);
                }
            }
            // --------------------------------------------------------

            // 2. Pensar respuesta con LLM
            await ProcessLlmResponse(message);
        });
    }

    // Lógica LLM: Recibe texto -> Consulta LM Studio -> Publica respuesta
    private async Task ProcessLlmResponse(string inputMessage)
    {
        // Mostrar indicador de "Escribiendo..."
        Title = "Pensando...";

        // Obtener config
        string url = Preferences.Get("LlmUrl", "http://localhost:1234/v1/chat/completions");
        string model = Preferences.Get("LlmModel", "llama-3.2-1b-instruct");
        string sysPrompt = Preferences.Get("SysPrompt", "Eres un asistente.");
        double temp = Convert.ToDouble(Preferences.Get("LlmTemp", "0.7"));
        int tokens = Convert.ToInt32(Preferences.Get("LlmTokens", "150"));

        // Llamar al LLM
        string response = await _llmService.GetResponseFromLlmAsync(inputMessage, sysPrompt, model, url, temp, tokens);

        Title = "Chat 1 - Equipo GATO 🐱"; // Restaurar título

        // 3. Mostrar mi respuesta (Derecha - Verde)
        AddChatBubble(response, true);

        // (Aquí hemos quitado el TextToSpeech porque ya no queremos leer nuestra propia respuesta)

        // 4. Enviar a RabbitMQ para que la otra app responda
        if (_isConnected)
        {
            await _rabbitService.SendMessageAsync(response);
        }
    }

    // Botón Enviar Manual para iniciar la conversación desde esta app
    private async void OnSendClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EntMessage.Text)) return;

        string msg = EntMessage.Text;
        AddChatBubble(msg, true); // Mostrar como mío
        EntMessage.Text = "";

        if (_isConnected)
        {
            await _rabbitService.SendMessageAsync(msg);
        }
        else
        {
            await DisplayAlert("Ojo", "Conéctate primero a RabbitMQ", "Vale");
        }
    }

    // Navegar a Configuración
    private async void OnConfigClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SettingsPage());
    }

    // Helper visual: Añade burbujas al chat estilo WhatsApp
    private void AddChatBubble(string text, bool isMe)
    {
        var frame = new Frame
        {
            CornerRadius = 10,
            Padding = new Thickness(12, 8),
            BackgroundColor = isMe ? Color.FromArgb("#DCF8C6") : Colors.White,
            HorizontalOptions = isMe ? LayoutOptions.End : LayoutOptions.Start,
            HasShadow = true,
            BorderColor = Colors.Transparent,
            MaximumWidthRequest = 280
        };

        var labelMsg = new Label
        {
            Text = text,
            TextColor = Colors.Black,
            FontSize = 15
        };

        var labelTime = new Label
        {
            Text = DateTime.Now.ToString("HH:mm"),
            TextColor = Color.FromArgb("#999999"),
            FontSize = 10,
            HorizontalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 4, 0, 0)
        };

        var stack = new VerticalStackLayout();
        stack.Children.Add(labelMsg);
        stack.Children.Add(labelTime);

        frame.Content = stack;

        ChatContainer.Children.Add(frame);
        ChatScroll.ScrollToAsync(ChatContainer, ScrollToPosition.End, true);
    }

    // Botón Desconectar/Parar cierra la conexión a RabbitMQ y limpia eventos para evitar fugas de memoria
    private async void OnDisconnectClicked(object sender, EventArgs e)
    {
        if (!_isConnected) return;

        try
        {

            await _rabbitService.DisposeAsync();
            // cerramos la conexión y dejamos de escuchar mensajes, es importante quitar el evento para evitar que la UI intente actualizarse con mensajes entrantes después de desconectar, lo que causaría errores
            _rabbitService.OnMessageReceived -= HandleMessageReceived;

            _isConnected = false;
            LblStatus.Text = "Desconectado";
            LblStatus.TextColor = Color.FromArgb("#FFCDD2");

            BtnConnect.IsEnabled = true;
            BtnConnect.Opacity = 1.0;

            BtnDisconnect.IsEnabled = false;
            BtnDisconnect.Opacity = 0.5;

            await DisplayAlert("Info", "Conversación detenida.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Error al desconectar: " + ex.Message, "OK");
        }
    }
}
