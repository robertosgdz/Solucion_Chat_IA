namespace App1_Chat
{
    public partial class App : Application
    {
        public App()
        {
            // Inicializa los componentes de la aplicación, como recursos y estilos definidos en App.xaml
            InitializeComponent();

            // Devuelve una navigation page para que sea navegable
            MainPage = new NavigationPage(new MainPage());
        }
    }
}
