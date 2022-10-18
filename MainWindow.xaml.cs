using FluentFTP;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Input;

namespace Downloader
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        public List<string> files_path = new List<string>();
        public string login = "";
        public string pass = "";
        public string remote_path = "";
        public string text_file_address_path = "";
        public List<string> addresses = new List<string>();
        CancellationTokenSource cancelTokenSource = new CancellationTokenSource();


        void Choose_files(object sender, RoutedEventArgs e)
        {

            files_path.Clear();
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "All files (*.*)|*.*";
            fileDialog.Multiselect = true;
            fileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //ileDialog.Filter = "Text files (*.txt)|*.txt";
            if (fileDialog.ShowDialog() == true)
            {
                foreach (string filename in fileDialog.FileNames)
                {
                    files_path.Add(filename);
                }
                //for (int i = 0; i < files_path.Count; i++)
                //{
                //    MessageBox.Show(files_path[i]);
                //}
            }
            Remove_files_button.IsEnabled = true;
        }

        void Remote_path_changed(object sender, TextChangedEventArgs e)
        {
            try
            {
                remote_path = remote_path_field.Text;
            }
            catch
            {
                MessageBox.Show("Введены некорректные данные!");
            }

        }

        void Choose_addresses_file(object sender, RoutedEventArgs e)
        {
            addresses.Clear();
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            fileDialog.Filter = "Text files (*.txt)|*.txt";
            if (fileDialog.ShowDialog() == true)
            {
                string result = File.ReadAllText(fileDialog.FileName);
                addresses_field.Text = result;
                string[] result_array = result.Split(',');
                addresses = new List<string>(result_array);
            }
            MessageBox.Show("Файл с IP-адресами выбран!");
        }

        public bool Checker()
        {
            if (files_path.Count == 0)
            {
                MessageBox.Show("Не выбраны файлы для загрузки!", "Ошибка");
                Choose_file_button.BorderBrush = new SolidColorBrush(Colors.Red);
                Choose_file_button.BorderThickness = new Thickness(3);
                return false;
            }
            if (addresses_field.Text == "127.0.0.1" || addresses_field.Text == "" || addresses.Count == 0)
            {
                MessageBox.Show("Неверно введены IP-адреса в поле ввода", "Ошибка");
                addresses_field.BorderBrush = new SolidColorBrush(Colors.Red);
                addresses_field.BorderThickness = new Thickness(3);
                return false;
            }
            if (login_field.Text == "" || pass_field.Text == "")
            {
                MessageBox.Show("Неверно введены данные для входа!", "Ошибка");
                login_field.BorderBrush = new SolidColorBrush(Colors.Red);
                login_field.BorderThickness = new Thickness(2);
                pass_field.BorderBrush = new SolidColorBrush(Colors.Red);
                pass_field.BorderThickness = new Thickness(2);
                return false;
            }
            return true;
        }


        void Change_color(object sender, MouseEventArgs e)
        {
            Control current_source = sender as Control;
            current_source.BorderThickness = new Thickness(1);
            current_source.BorderBrush = new SolidColorBrush(Colors.Gray);
        }

        void Start_Download_click(object sender, RoutedEventArgs e)
        {
            stop_download_button.IsEnabled = true;
            start_download_button.IsEnabled = false;
            //for (int i = 0; i < addresses.Count; i++)
            //{
            //    MessageBox.Show(addresses[i]);
            //}
            Start_Download();

        }

        void addresses_field_changed(object sender, TextChangedEventArgs e)
        {
            //TextBox textBox = sender as TextBox;
            string[] prepare_addresses_text = addresses_field.Text.Split(',');
            addresses = new List<string>(prepare_addresses_text);
        }


        private void Clear_addresses()
        {
            for (int i = 0; i < addresses.Count; i++)
            {
                addresses[i] = addresses[i].Replace(Environment.NewLine,"");
            }
        }

        async void Start_Download()
        {
            current_status_bar.Value = 0;
            current_status.Content = "Статус загрузки:";
            download_log.Text = "";
            download_log.Text += "Проверяю входные данные,ожидайте...\r\n";
            if (Checker())
            {
                Clear_addresses();
                download_log.Text += "Входные данные успешно проверены!\r\n";
                download_log.Text += "Начинаю загрузку файлов...\r\n";
                var user_data = new NetworkCredential(login, pass);
                var files_count = files_path.Count * addresses.Count;
                try
                {
                    for (int i = 0; i < addresses.Count; i++)
                    {
                        var client = new AsyncFtpClient(addresses[i], user_data);
                        for (int j = 0; j < files_path.Count; j++)
                        {
                            current_count_files.Content = "Осталось загрузить файлов: " + Convert.ToString(files_count);
                            string file_name = files_path[j].Substring(files_path[j].LastIndexOf("\\")).Remove(0, 1);
                            current_status.Content = "Статус загрузки: загружаю " + file_name + " на " + addresses[i];
                            download_log.Text += "Загружаю файл " + file_name + "\r\n";
                            CancellationToken cancel_token = cancelTokenSource.Token;
                            await UploadFileAsync(client, @"" + files_path[j], remote_path + file_name,cancel_token);
                            download_log.Text += "Файл " + file_name + " загружен! " + "\r\n";
                            files_count--;
                        }
                        await client.Disconnect();
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
                finally
                {
                    download_log.Text += "Работа скрипта завершена!\r\n";
                    current_status_bar.Value = 0;
                    start_download_button.IsEnabled = true;
                    stop_download_button.IsEnabled = false;
                    current_status.Content = "Статус загрузки:";
                    current_count_files.Content = "Осталось загрузить файлов: ";
                    speed_transfer.Content = "Скорость загрузки: ";
                }
            }
            else
            {
                download_log.Text += "Обнаружена ошибка во время проверки входных данных!";
                start_download_button.IsEnabled = true;
                stop_download_button.IsEnabled = false;
                return;
            }
        }

        public async Task UploadFileAsync(AsyncFtpClient ftpClient, string local_path,string remote_path, CancellationToken cancel_token)
        {
            current_status_bar.Value = 0;
            var token = cancel_token;
            if (token.IsCancellationRequested)
            {
                download_log.Text += "Операция прервана пользователем\r\n";
                return;
            }
            var ftp = ftpClient;
            await ftp.Connect(token);
                Progress<FtpProgress> progress = new Progress<FtpProgress>(p => {
                    if (p.Progress == 1)
                    {
                        current_status_bar.Value = 100;
                    }
                });
                // upload a file with progress tracking
                progress.ProgressChanged += Progress_ProgressChanged; 
                await ftp.UploadFile(local_path, remote_path, FtpRemoteExists.Overwrite, false, FtpVerify.None, progress, token);
        }

        private void Progress_ProgressChanged(object sender, FtpProgress e)
        {
            speed_transfer.Content = "Скорость загрузки: " + e.TransferSpeedToString();
            current_status_bar.Value = (int)e.Progress;
        }

        private void stop_download_button_Click(object sender, RoutedEventArgs e)
        {
            cancelTokenSource.Cancel();
            current_status.Content = "Статус загрузки:";
            current_status_bar.Value = 0;
        }

        private void login_field_changed(object sender, TextChangedEventArgs e)
        {
            login = login_field.Text;
        }

        private void pass_field_changed(object sender, TextChangedEventArgs e)
        {
            pass = pass_field.Text;
        }

        private void Remove_files_click(object sender, RoutedEventArgs e)
        {
            Remove_files_button.IsEnabled = false;
            files_path = new List<string>();
            MessageBox.Show("Выбранные файлы удалены!");
        }
    }
}




