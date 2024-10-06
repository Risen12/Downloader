using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Uploader;

namespace Uploader
{
    /// <summary>
    /// Логика взаимодействия для Loading_window.xaml
    /// </summary>
    public partial class Loading_window : Window
    {
        public Loading_window()
        {
            InitializeComponent();
        }


        async void Load()
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
            MainWindow main = new MainWindow();
            Close();
            main.Show();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Load();
        }
    }
}
