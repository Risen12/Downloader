using FluentFTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;
using Control = System.Windows.Controls.Control;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using System.Net.NetworkInformation;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Uploader
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

        List<AsyncFtpClient> ftpClients = new List<AsyncFtpClient>();
        public int parallel_check = 0;
        string current_log_file = "";
        public string problem_block = "";
        string async_block = "";
        public bool standart_upload = true;
        public List<string> files_path = new List<string>();
        public string directory_path = "";
        string directory_name = "";
        public string login = "admin";
        public string pass = "iZpa16ukrOaFbI91IYRa";
        public int files_count = 0;
        public bool need_file_log = true;
        public NetworkCredential user_data;
        string file_name = "";
        public string remote_path = "/NAND_flash/Icons/Download/start/";
        public bool directory = false;
        public string log_file_path;
        public List<string> addresses = new List<string>();
        public string version = "0";
        private List<string> results = new List<string>();
        CancellationTokenSource cancelTokenSource = new CancellationTokenSource();



        


        void Choose_files(object sender, RoutedEventArgs e)
        {
            directory = false;
            directory_path = "";
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
                Remove_files_button.IsEnabled = true;
            }
            else
            {
                return;
            }

        }

        private void Choose_directory_button_Click(object sender, RoutedEventArgs e)
        {
            directory = true;
            files_path.Clear();
            directory_path = "";
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Выберите директорию, которую вы хотите загрузить на устройство.";

                folderDialog.RootFolder = Environment.SpecialFolder.Desktop;
                DialogResult result = folderDialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    Remove_files_button.IsEnabled = true;
                    directory_path = folderDialog.SelectedPath;
                    files_count = Directory.GetFiles(directory_path,"*",SearchOption.AllDirectories).Length;
                }
            }

        }


        void Remote_path_changed(object sender, TextChangedEventArgs e)
        {
            try
            {
                remote_path = remote_path_field.Text;
            }
            catch
            {
                MessageBox.Show("Введены некорректные данные!", "Ошибка");
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
            else
            {
                return;
            }

        }

        private void Problem_logger(string address,string reason = "")
        {
            problem_block += address + " причина: " + reason;
        }


        public void Logger(string text, string reason = "default", bool to_file = false, bool break_line_file = false)
        {
            Span new_info = new Span();
            switch (reason)
            {
                case "Regular":
                    new_info.Foreground = Brushes.Gray;
                    break;
                case "Error":
                    new_info.Foreground = Brushes.Red;
                    break;
                case "Success":
                    new_info.Foreground = Brushes.Green;
                    break;
                case "Info":
                    new_info.Foreground = Brushes.Orange;
                    break;
                default:
                    new_info.Foreground = Brushes.Black;
                    break;
            }
            string time = DateTime.Now.ToShortTimeString();
            new_info.Inlines.Add(time + ": " + text + "\r\n");
            download_log.Inlines.Add(new_info);

            if (to_file)
            {
                if (need_file_log == false) return;
                if (string.IsNullOrEmpty(log_file_path))
                    log_file_path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\uploader_log"+ current_log_file + ".txt";
                if (!File.Exists(log_file_path))
                {
                    using (FileStream file = new FileStream(log_file_path, FileMode.Create)) { };
                }
                using (StreamWriter sw = new StreamWriter(log_file_path, true))
                {
                    time = DateTime.Now.ToShortTimeString();
                    sw.Write(time + ": " + text + "\r\n");
                    if (break_line_file)
                    {
                        if (standart_upload == false)
                        {
                            return;
                        }
                        sw.Write("==========================================\r\n");
                    }
                };   
            }
        }


        private  void Async_Logger(string next_note, string address = "", bool last_note = false)
        {
            if (need_file_log == false) return;
            if (string.IsNullOrEmpty(log_file_path))
                log_file_path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\downloader_log.txt";
            if (!File.Exists(log_file_path))
            {
                using (FileStream file = new FileStream(log_file_path, FileMode.Create)) { };
            }
            if (last_note)
            {
                using (StreamWriter sw = new StreamWriter(log_file_path, true))
                {
                    if (address == "")
                    {
                        string time = DateTime.Now.ToShortTimeString();
                        async_block += time + ": " + next_note + "\r\n";
                        sw.Write(async_block + "\r\n");
                        //sw.Write("==========================================\r\n");
                    }
                    else
                    {
                        string time = DateTime.Now.ToShortTimeString();
                        async_block += time + ": " + next_note + " (" + address + ")\r\n";
                        sw.Write(async_block + "\r\n");
                        //sw.Write("==========================================\r\n");
                    }

                }
            }
            else
            {
                if (address == "")
                {
                    string time = DateTime.Now.ToShortTimeString();
                    async_block += time + ": " + next_note + "\r\n";
                }
                else
                {
                    string time = DateTime.Now.ToShortTimeString();
                    async_block += time + ": " + next_note + " (" + address + ")\r\n";
                }

            }
        }

        public bool Checker()
        {
            if (files_path.Count == 0 && string.IsNullOrEmpty(directory_path))
            {
                MessageBox.Show("Не выбраны файлы или директория для загрузки!", "Ошибка");
                Choose_file_button.BorderBrush = new SolidColorBrush(Colors.Red);
                Choose_file_button.BorderThickness = new Thickness(3);
                Choose_directory_button.BorderBrush = new SolidColorBrush(Colors.Red);
                Choose_directory_button.BorderThickness = new Thickness(3);
                return false;
            }
            if (addresses_field.Text == "127.0.0.1" || addresses_field.Text == "" || addresses.Count == 0)
            {
                MessageBox.Show("Неверно введены IP-адреса в поле ввода", "Ошибка");
                addresses_field.BorderBrush = new SolidColorBrush(Colors.Red);
                addresses_field.BorderThickness = new Thickness(3);
                return false;
            }
            //if (login_field.Text == "" || pass_field.Password == "")
            //{
            //    MessageBox.Show("Неверно введены данные для входа!", "Ошибка");
            //    login_field.BorderBrush = new SolidColorBrush(Colors.Red);
            //    login_field.BorderThickness = new Thickness(2);
            //    pass_field.BorderBrush = new SolidColorBrush(Colors.Red);
            //    pass_field.BorderThickness = new Thickness(2);
            //    return false;
            //}
            return true;
        }


        void Change_color(object sender, MouseEventArgs e)
        {
            Control current_source = sender as Control;
            current_source.BorderThickness = new Thickness(2);
            current_source.BorderBrush = new SolidColorBrush(Colors.White);
        }

        void Start_Download_click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(log_file_path))
            {
                var same_window = Process.GetProcessesByName("Uploader");
                if (same_window.Length - 1 >= 1)
                {
                    current_log_file = same_window.Length.ToString();
                }
                else current_log_file = "";
            }
            cancelTokenSource = new CancellationTokenSource();
            if (need_log_file_box.IsChecked == false)
                need_file_log = false;
            start_download_button.IsEnabled = false;
            stop_download_button.IsEnabled = true;
            if (standart_upload)
                Standart_upload();
            else
                Parallel_Upload();
        }

        void addresses_field_changed(object sender, TextChangedEventArgs e)
        {
            string[] prepare_addresses_text = addresses_field.Text.Split(',');
            addresses = new List<string>(prepare_addresses_text);
        }


        private void Clear_addresses()
        {
            Regex check = new Regex(@"[0-9]{1,3}['.']{1}[0-9]{1,3}['.']{1}[0-9]{1,3}['.']{1}[0-9]{1,3}");
            for (int i = 0; i < addresses.Count; i++)
            {
                addresses[i] = addresses[i].Replace(Environment.NewLine, "");
                addresses[i] = addresses[i].Replace(@"\r\n", "");
                addresses[i] = addresses[i].Replace(" ", "");
                if (string.IsNullOrEmpty(addresses[i]) || string.IsNullOrWhiteSpace(addresses[i]) || check.IsMatch(addresses[i]) == false)
                {
                    addresses.Remove(addresses[i]);
                    i--;
                }
            }
        }

        bool Check_connection(string current_address)
        {
            Ping pinger = null;
            bool result = false;
            try
            {
                pinger = new Ping();
                PingReply pingReply = pinger.Send(current_address);
                result = pingReply.Status == IPStatus.Success;
            }
            catch (PingException e)
            {
                Logger("Адрес " + current_address + " недоступен!","Error");
            }
            finally
            { 
                if(pinger != null)
                    pinger.Dispose();
            }
            return result;
        }



        async Task<string[]> Check_FTP_pass(string current_address)
        {
            string version = "0";
            var result = new string[3]; 
            try
            {
                login = "admin";
                pass = "iZpa16ukrOaFbI91IYRa";
                user_data = new NetworkCredential(login, pass);
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + current_address + "/NAND_Flash");
                request.Timeout = 3000;
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Credentials = user_data;
                request.KeepAlive = false;
                var response = (FtpWebResponse)await request.GetResponseAsync();
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string files = reader.ReadToEnd();
                        if (files.Contains("Media") && !files.Contains("Autostart"))
                        {
                            version = "8";
                        }
                        else
                        {
                            version = "6";
                        }
                    }
                }
                response.Close();
                response.Dispose();
                return result = new string[] {version ,"admin", "iZpa16ukrOaFbI91IYRa" };
            }
            catch (WebException e)
            {
                //Logger("Произошла ошибка при подключении!","Error",true);
                if (e.Status == WebExceptionStatus.Timeout)
                {
                    Logger("Адрес " + current_address + " не ответил на FTP-запрос в течении 5-ти секунд, необходимо загрузить файлы вручную!","Error",true);
                    return new string[] { "0", "", "" };
                }
                if (e.Status == WebExceptionStatus.ConnectFailure)
                {
                    Logger("Подключение к " + current_address + " не удалось, необходимо загрузить файлы вручную!", "Error", true);
                    return new string[] { "0", "", "" };
                }
                Logger("Пробую другой пароль ...", "Info", true);
                try
                {
                    login = "admin";
                    pass = "habhat";
                    user_data = new NetworkCredential(login,pass);
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + current_address + "/NAND_Flash");
                    request.Method = WebRequestMethods.Ftp.ListDirectory;
                    request.KeepAlive = false;
                    request.Credentials = new NetworkCredential(login, pass);
                    var response = (FtpWebResponse)request.GetResponse();
                    using (Stream stream = response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string files = reader.ReadToEnd();
                            if (files.Contains("Media") && !files.Contains("Autostart"))
                            {
                                version = "8";
                            }
                            else
                            {
                                version = "6";
                            }
                        }
                    }
                    response.Close();
                    response.Dispose();
                    return result = new string[] { version, "admin", "habhat" };
                }
                catch (Exception r)
                {
                    Logger("Второй пароль не подошёл, загрузить файлы необходимо вручную!","Error", true);
                    return new string[] { "0", "", "" };
                }
            }
        }


        private void Init_data(bool isDirectory = false)
        {

            user_data = new NetworkCredential("admin", "iZpa16ukrOaFbI91IYRa");
            login = "admin";
            pass = "iZpa16ukrOaFbI91IYRa";
            problem_block = "";
            async_block = "";
            Clear_addresses();
            if (isDirectory)
                files_count = addresses.Count * Directory.GetFiles(directory_path, "*.*",SearchOption.AllDirectories).Length;
            else
                files_count = addresses.Count * files_path.Count;
        }

        string Set_version(string version)
        {
            if (version == "6")
            {
                remote_path = "/NAND_flash/Icons/Download/start/";
                return "/NAND_flash/Icons/Download/start/";
            }
            else
            {
                remote_path = "/NAND_flash/Media/Download/start/";
                return "/NAND_flash/Media/Download/start/";
            }

        }

        //async void Download_directory()
        //{
        //    files_count = Directory.GetFiles(directory_path, "*", SearchOption.AllDirectories).Length;
        //    AsyncFtpClient client = null;
        //    current_status_bar.Value = 0;
        //    current_status.Content = "Статус загрузки:";
        //    download_log.Inlines.Clear();
        //    Logger("Проверяю входные данные, ожидайте ...", "Info");
        //    if (Checker())
        //    {
        //        Init_data(true);
        //        Logger("Входные данные успешно проверены!", "Success");
        //        Logger("Начинаю загрузку директории ...", "Info");

        //        try
        //        {
        //            for (int i = 0; i < addresses.Count; i++)
        //            {
        //                try
        //                {
        //                    Logger("Работаю с адресом " + addresses[i], "Info", true);
        //                    Logger("Проверяю доступность адреса " + addresses[i] + " ...", "Info");
        //                    if (!Check_connection(addresses[i]))
        //                    {
        //                        Logger("Адрес " + addresses[i] + " недоступен!", "Error", true);
        //                        Logger("Перехожу к следующему адресу.", "Info");
        //                        continue;
        //                    }
        //                    Logger("Адрес " + addresses[i] + " доступен!", "Success");
        //                    Logger("Проверяю доступ по порту FTP ...", "Info");
        //                    if (Check_FTP_pass(addresses[i]) == 0)
        //                    {
        //                        Logger("Адрес " + addresses[i] + " недоступен по порту FTP!", "Error", true);
        //                        Logger("Перехожу к следующему адресу.", "Info");
        //                        continue;
        //                    }
        //                    Logger("Адрес " + addresses[i] + " доступен по FTP!", "Success", true);
        //                    client = new AsyncFtpClient(addresses[i], user_data);
        //                    directory_name = directory_path.Substring(directory_path.LastIndexOf("\\")).Remove(0, 1);
        //                    Progress_loger(addresses[i]);
        //                    Logger("Загружаю директорию " + directory_name + " ...", "Info");
        //                    CancellationToken cancel_token = cancelTokenSource.Token;
        //                    await UploadDirectoryAsync(client, directory_path, remote_path, cancel_token);
        //                    Logger("Директория " + directory_name + " загружена!", "Success", true);
        //                    await client.Disconnect();
        //                }
        //                catch (Exception e)
        //                {
        //                    Logger("Произошла ошибка во время загрузки файлов на адрес " + addresses[i] + "!", "Error", true);
        //                    Logger(e.Message, "Error", true);
        //                    Logger("Перехожу к следующему адресу ...", "Info", true);
        //                    continue;
        //                }
        //                Logger("Работа с адресом " + addresses[i] + " завершена!", "Info", true);
        //                current_count_files.Visibility = Visibility.Visible;
        //            }
        //            await client.Disconnect();
        //            client.Dispose();
        //        }
        //        catch (Exception e)
        //        {
        //            MessageBox.Show(e.Message);
        //        }
        //        finally
        //        {
        //            Logger("Работа скрипта завершена!", "Info");
        //            Clear_data();
        //        }
        //    }
        //    else
        //    {
        //        Logger("Обнаружена ошибка во время проверки входных данных!", "Error");
        //        start_download_button.IsEnabled = true;
        //        stop_download_button.IsEnabled = false;
        //        return;
        //    }
        //}


        void Clear_data()
        {
            current_status_bar.Value = 0;
            start_download_button.IsEnabled = true;
            stop_download_button.IsEnabled = false;
            current_status.Content = "Статус загрузки:";
            current_count_files.Content = "Осталось загрузить файлов: ";
            speed_transfer.Content = "Скорость загрузки: ";
            //time_transfer.Content = "Осталось времени: ";
        }

        //async void Download_Files()
        //{
        //    AsyncFtpClient client = null;
        //    current_status_bar.Value = 0;
        //    current_status.Content = "Статус загрузки:";
        //    download_log.Inlines.Clear();
        //    Logger("Проверяю входные данные, ожидайте ...", "Info");
        //    if (Checker())
        //    {
        //        Init_data();
        //        Logger("Входные данные успешно проверены!", "Success", true);
        //        Logger("Начинаю загрузку файлов ...", "Info");

        //        try
        //        {
        //            for (int i = 0; i < addresses.Count; i++)
        //            {
        //                try 
        //                {
        //                    Logger("Работаю с адресом " + addresses[i], "Info", true);
        //                    Logger("Проверяю доступность адреса " + addresses[i] + " ...", "Info");
        //                    if (!Check_connection(addresses[i]))
        //                    {
        //                        Logger("Адрес " + addresses[i] + " недоступен!", "Error", true);
        //                        Logger("Перехожу к следующему адресу.", "Info");
        //                        continue;
        //                    }
        //                    Logger("Адрес " + addresses[i] + " доступен!", "Success");
        //                    Logger("Проверяю доступ по порту FTP ...", "Info");
        //                    if (Check_FTP_pass(addresses[i]) == 0)
        //                    {
        //                        Logger("Адрес " + addresses[i] + " недоступен по порту FTP!", "Error", true);
        //                        Logger("Перехожу к следующему адресу.", "Info");
        //                        continue;
        //                    }
        //                    Logger("Адрес " + addresses[i] + " доступен по FTP!", "Success", true);
        //                    client = new AsyncFtpClient(addresses[i], user_data);
        //                    for (int j = 0; j < files_path.Count; j++)
        //                    {
        //                        file_name = files_path[j].Substring(files_path[j].LastIndexOf("\\")).Remove(0, 1);
        //                        Progress_loger(addresses[i]);
        //                        Logger("Загружаю файл " + file_name, "Info");
        //                        CancellationToken cancel_token = cancelTokenSource.Token;
        //                        await UploadFileAsync(client, @"" + files_path[j], remote_path + file_name, cancel_token);
        //                        Logger("Файл " + file_name + " загружен!", "Success", true);
        //                        files_count--;
        //                    }
        //                }
        //                catch (Exception e)
        //                {
        //                    Logger("Произошла ошибка во время загрузки файлов на адрес " + addresses[i] + "!", "Error", true);
        //                    Logger(e.Message, "Error", true);
        //                    Logger("Перехожу к следующему адресу ...", "Info", true);
        //                    continue;
        //                }
        //                Logger("Работа с адресом " + addresses[i] + " завершена!", "Info", true);
        //            }
        //            await client.Disconnect();
        //            client.Dispose();
        //        }
        //        catch (Exception e)
        //        {
        //            Logger("Произошла ошибка " + e.Message,"Error", true);
        //        }
        //        finally
        //        {
        //            Logger("Работа скрипта завершена!", "Info");
        //            Clear_data();
        //        }
        //    }
        //    else
        //    {
        //        Logger("Обнаружена ошибка во время проверки входных данных!", "Error");
        //        start_download_button.IsEnabled = true;
        //        stop_download_button.IsEnabled = false;
        //        return;
        //    }
        //}

        async Task UploadDirectoryAsync(AsyncFtpClient ftpClient, string local_path,string remote_path, CancellationToken cancel_token)
        {
            var token = cancel_token;
            if (token.IsCancellationRequested)
            {
                await CancelRequest(token);
                return;
            }
            using (var ftp = ftpClient)
            {
                await ftp.Connect(token);
                Progress<FtpProgress> progress = new Progress<FtpProgress>(p => {
                    if (p.Progress == 1)
                    {
                        current_status_bar.Value = 100;
                    }
                });
                progress.ProgressChanged += Progress_ProgressChanged;
                //remote_path = remote_path + directory_name + "/";
                await ftp.UploadDirectory(local_path,remote_path + directory_name + "/", FtpFolderSyncMode.Update, FtpRemoteExists.Overwrite, FtpVerify.None, null, progress, token);
                try
                {
                    await ftp.DeleteFile(remote_path + directory_name + "/" + "desktop.ini");
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }

        private void Progress_loger(string current_address)
        {
            if (directory)
            {
                current_count_files.Visibility = Visibility.Hidden;
                current_status.Content = "Статус загрузки: загружаю директорию " + directory_name + " на " + current_address;
            }
            else
            {
                current_count_files.Content = "Осталось загрузить файлов: " + Convert.ToString(files_count);
                current_status.Content = "Статус загрузки: загружаю " + file_name + " на " + current_address;
            }
        }


        public async Task UploadFileAsync(AsyncFtpClient ftpClient, string local_path,string remote_path, CancellationToken cancel_token)
        {
            current_status_bar.Value = 0;
            var token = cancel_token;
            if (token.IsCancellationRequested)
            {
                await CancelRequest(token);
                return;
            }
            using (var ftp = ftpClient)
            {
                await ftp.Connect(token);
                Progress<FtpProgress> progress = new Progress<FtpProgress>(p => {
                    if (p.Progress == 1)
                    {
                        current_status_bar.Value = 100;
                    }
                });
                progress.ProgressChanged += Progress_ProgressChanged;
                await ftp.UploadFile(local_path, remote_path, FtpRemoteExists.Overwrite, false, FtpVerify.None, progress, token);
            }
        }


        private async Task CancelRequest(CancellationToken token)
        {
            Logger("Операция прервана пользователем!", "Error");
            Clear_data();
            foreach (var item in ftpClients)
            {
                await item.Disconnect();
            }
            current_status.Content = "Статус загрузки:";
            current_status_bar.Value = 0;
            stop_download_button.IsEnabled = false;
            start_download_button.IsEnabled = true;
            return;
        }


        private async Task Check_directory(AsyncFtpClient client, CancellationToken token,string local_remote_path)
        {
            if (cancelTokenSource.IsCancellationRequested)
            {
                await CancelRequest(token);
                return;
            }
            else
                await client.CreateDirectory(local_remote_path,true, token);
        }

        private void Progress_ProgressChanged(object sender, FtpProgress e)
        {
            //time_transfer.Content = "Осталось времени: " + new TimeSpan(e.ETA.Hours, (int)e.ETA.TotalMinutes, (int)e.ETA.TotalSeconds).ToString();
            speed_transfer.Content = "Скорость загрузки: " + e.TransferSpeedToString();
            if (directory)
            {
                current_status_bar.Value = (int)e.Progress;
                current_count_files.Content = "Осталось загрузить файлов: " + (files_count - (e.FileIndex + 1)).ToString();
            }
            else
            {
                current_status_bar.Value = (int)e.Progress;
            }

        }

        private void stop_download_button_Click(object sender, RoutedEventArgs e)
        {
            cancelTokenSource.Cancel();
            stop_download_button.IsEnabled = false;
            start_download_button.IsEnabled = true;
        }


        private void Remove_files_click(object sender, RoutedEventArgs e)
        {
            Remove_files_button.IsEnabled = false;
            files_path = new List<string>();
            directory_path = "";
            MessageBox.Show("Выбранные файлы и директории удалены!");
        }
        

        private void manual_remote_path_Checked(object sender, RoutedEventArgs e)
        {
            remote_path = remote_path_field.Text;
            remote_path_field.Text = remote_path;
            remote_path_field.IsEnabled = true;
        }

        private void manual_remote_path_Unchecked(object sender, RoutedEventArgs e)
        {
            remote_path_field.IsEnabled = false;
        }

        void Hide_Data()
        {
            current_count_files.Content = "Осталось загрузить файлов: -";
            current_status.Content = "Статус загрузки: -";
            //time_transfer.Content = "Осталось времени: -";

        }

        async Task Upload_addresses(int position, int count = 10)
        {
            ftpClients = new List<AsyncFtpClient>();
            parallel_check = 0;
            CancellationToken cancel_token = cancelTokenSource.Token;
            List<string> temp_addresses = addresses.GetRange(position, count);
            //Parallel.ForEach(temp_addresses,(item) => Dispatcher.InvokeAsync(() => Upload_parallel_address(item,cancel_token)));
            foreach (var item in temp_addresses)
            {
                Parallel.Invoke(() => Upload_parallel_address(item, cancel_token));
                await Task.Delay(500);
            }
            while (parallel_check != count)
            {
                if (cancelTokenSource.IsCancellationRequested)
                {
                    Logger("Загрузка отменена пользователем!","Error",true);
                    return;
                }
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        async Task Upload_address(string address, CancellationToken token)
        {
            ftpClients = new List<AsyncFtpClient>();
            string local_remote_path = "";
            try
            {
                Logger("Работаю с адресом " + address, "Info", true);
                Logger("Проверяю доступность адреса " + address + " ...", "Info");
                if (!Check_connection(address))
                {
                    Logger("Адрес " + address + " недоступен!", "Error", true);
                    Logger("Перехожу к следующему адресу.", "Info", true, true);
                    Problem_logger(address, "Нет пинга.\r\n");
                    return;
                }
                Logger("Адрес " + address + " доступен!", "Success");
                Logger("Проверяю доступ по порту FTP ...", "Info");
                var result = await Check_FTP_pass(address);
                if (result[0] == "0")
                {
                    Logger("Адрес " + address + " недоступен по порту FTP!", "Error", true);
                    Logger("Перехожу к следующему адресу.", "Info", true,true);
                    Problem_logger(address, "Нет доступа по FTP.\r\n");
                    return;
                }
                if (manual_remote_path.IsChecked == true)
                {
                    remote_path = remote_path_field.Text;
                    local_remote_path = remote_path_field.Text;
                    if (local_remote_path[remote_path.Length - 1] != '/') local_remote_path += '/';
                }
                else
                    local_remote_path = Set_version(result[0]);
                Logger("Адрес " + address + " доступен по FTP!", "Success", true);
                Progress_loger(address);
                if (directory)
                {
                    if (cancelTokenSource.IsCancellationRequested)
                    {
                        await CancelRequest(token);
                        return;
                    }
                    var ftpClient = new AsyncFtpClient(address, result[1], result[2]);
                    await Clear_directory(local_remote_path, address, new NetworkCredential(result[1], result[2]));
                    await Check_directory(ftpClient, token, local_remote_path);
                    ftpClient = new AsyncFtpClient(address, result[1], result[2]);
                    ftpClients.Add(ftpClient);
                    directory_name = directory_path.Substring(directory_path.LastIndexOf("\\")).Remove(0, 1);
                    Logger("Загружаю директорию " + directory_name + " ...", "Info");
                    await UploadDirectoryAsync(ftpClient, directory_path, local_remote_path, token);
                    Logger("Директория " + directory_name + " загружена!", "Success", true);
                    await ftpClient.Disconnect();
                    ftpClient.Dispose();
                }
                else
                {
                    var ftpClient = new AsyncFtpClient(address, result[1], result[2]);
                    await Clear_directory(local_remote_path, address, new NetworkCredential(result[1], result[2]));
                    await Check_directory(ftpClient, token, local_remote_path);
                    for (int j = 0; j < files_path.Count; j++)
                    {
                        if (cancelTokenSource.IsCancellationRequested)
                        {
                            await CancelRequest(token);
                            return;
                        }
                        ftpClient = new AsyncFtpClient(address, result[1], result[2]);
                        ftpClients.Add(ftpClient);
                        file_name = files_path[j].Substring(files_path[j].LastIndexOf("\\")).Remove(0, 1);
                        Progress_loger(address);
                        Logger("Загружаю файл " + file_name, "Info");
                        await UploadFileAsync(ftpClient,files_path[j], local_remote_path + file_name, token);
                        Logger("Файл " + file_name + " загружен!", "Success", true);
                        files_count--;
                    }
                    await ftpClient.Disconnect();
                    ftpClient.Dispose();
                }
                Logger("Загрузка файлов на адрес " + address + " завершена успешно!", "Success", true, true);
            }
            catch (Exception e)
            {
                Logger("Произошла ошибка во время загрузки файлов на адрес " + address + "!", "Error", true, true);
                Problem_logger(address, "Ошибка во время загрузки файлов.\r\n");
                Logger(e.Message, "Error", true);
                files_count -= files_path.Count;
            }
       }

        async Task Upload_parallel_address(string address, CancellationToken token)
        {

            string local_remote_path = "";
            try
            {
                Async_Logger("Работаю с адресом " + address);
                Logger("Работаю с адресом " + address , "Info", true);
                Logger("Проверяю доступность адреса " + address + " ...", "Info");
                if (!Check_connection(address))
                {
                    Async_Logger("Адрес " + address + " недоступен!");
                    Async_Logger("Перехожу к следующему адресу.",address, true); 
                    Logger("Адрес " + address + " недоступен!", "Error", true);
                    Logger("Перехожу к следующему адресу.", "Info", true, true);
                    Problem_logger(address, "Нет пинга.\r\n");
                    parallel_check++;
                    return;
                }
                Async_Logger("Адрес " + address + " доступен!", address);
                Logger("Адрес " + address + " доступен!", "Success");
                Logger("Проверяю доступ по порту FTP ...", "Info");
                var result = await Check_FTP_pass(address);
                if (result[0] == "0")
                {
                    Async_Logger("Адрес " + address + " недоступен по порту FTP!", address);
                    Async_Logger("Перехожу к следующему адресу.", address, true);
                    Logger("Адрес " + address + " недоступен по порту FTP!", "Error", true);
                    Logger("Перехожу к следующему адресу.", "Info", true, true);
                    Problem_logger(address, "Нет доступа по FTP.\r\n");
                    parallel_check++;
                    return;
                }
                if (manual_remote_path.IsChecked == true)
                {
                    remote_path = remote_path_field.Text;
                    local_remote_path = remote_path_field.Text;
                }
                else
                    local_remote_path = Set_version(result[0]);
                Async_Logger("Адрес " + address + " доступен по FTP!", address);
                Logger("Адрес " + address + " доступен по FTP!", "Success", true);
                var ftpClient = new AsyncFtpClient(address, result[1], result[2]);
                if (directory)
                {
                    await Clear_directory(local_remote_path, address, new NetworkCredential(result[1], result[2]));
                    await Check_directory(ftpClient, token, local_remote_path);
                    ftpClient = new AsyncFtpClient(address, result[1], result[2]);
                    ftpClients.Add(ftpClient);
                    directory_name = directory_path.Substring(directory_path.LastIndexOf("\\")).Remove(0, 1);
                    Logger("Загружаю директорию " + directory_name + " ...", "Info");
                    await UploadDirectoryAsync(ftpClient, directory_path, local_remote_path, token);
                    Async_Logger("Директория " + directory_name + " загружена!" + " (" + address + ")");
                    Logger("Директория " + directory_name + " загружена!", "Success", true);
                    await ftpClient.Disconnect();
                    ftpClient.Dispose();
                    parallel_check++;
                    files_count -= (files_count / addresses.Count);
                }
                else
                {
                    await Clear_directory(local_remote_path, address, new NetworkCredential(result[1], result[2]));
                    await Check_directory(ftpClient, token,local_remote_path);
                    for (int j = 0; j < files_path.Count; j++)
                    {
                        ftpClient = new AsyncFtpClient(address, result[1], result[2]);
                        ftpClients.Add(ftpClient);
                        file_name = files_path[j].Substring(files_path[j].LastIndexOf("\\")).Remove(0, 1);
                        Logger("Загружаю файл " + file_name, "Info");
                        CancellationToken cancel_token = cancelTokenSource.Token;
                        await UploadFileAsync(ftpClient, files_path[j], local_remote_path + file_name, cancel_token);
                        Async_Logger("Файл " + file_name + " загружен!" + " (" + address +  ")");
                        Logger("Файл " + file_name + " загружен!" + " (" + address + ")", "Success", true);
                        files_count--;
                    }
                    await ftpClient.Disconnect();
                    ftpClient.Dispose();
                    parallel_check++;
                }
                Async_Logger("Загрузка файлов на адрес " + address + " завершена успешно!", "", true);
                Logger("Загрузка файлов на адрес " + address + " завершена успешно!", "Success", true, true);
            }
            catch (Exception e)
            {
                Async_Logger(e.Message, address);
                Async_Logger("Произошла ошибка во время загрузки файлов на адрес " + address + "!","", true);
                Logger("Произошла ошибка во время загрузки файлов на адрес " + address + "!", "Error", true, true);
                Logger(e.Message, "Error", true);
                Problem_logger(address, "Ошибка во время загрузки файлов.\r\n");
                files_count -= files_path.Count;
                parallel_check++;
            }
        }


        private async Task Clear_directory(string remote_path,string address, NetworkCredential credential)
        {
            const string path_v6 = "/NAND_flash/Icons/Download/start/";
            const string path_v8 = "/NAND_flash/Media/Download/start/";
            if (remote_path == path_v6 || remote_path == path_v8)
            {
                using (var ftp = new AsyncFtpClient(address, credential))
                {
                    if (await ftp.DirectoryExists(remote_path.Substring(0, remote_path.Length - 1)) == true)
                    {
                        await ftp.DeleteDirectory(remote_path.Substring(0, remote_path.Length - 1));
                    }
                    else return;
                }
            }
            else
                return;
        }


        private async Task Parallel_Upload()
        {
            try
            {
                await Task.Delay(500);
                Logger("Выбрана параллельная загрузка файлов.", "Info", true);
                current_status_bar.Value = 0;
                current_status.Content = "Статус загрузки:";
                download_log.Inlines.Clear();
                Logger("Проверяю входные данные, ожидайте ...", "Info");
                if (Checker())
                {
                    await Task.Delay(500);
                    Init_data();
                    Hide_Data();
                    Logger("Входные данные успешно проверены!", "Success", true);
                    Logger("Начинаю загрузку файлов ...", "Info");
                    await Task.Delay(500);
                    if (addresses.Count <= 10)
                    {
                        await Upload_addresses(0, addresses.Count);
                    }
                    else
                    {
                        int current_count = addresses.Count;
                        int position = 0;
                        while (current_count != 0)
                        {
                            if (current_count - 10 <= 0)
                            {
                                await Upload_addresses(position,current_count);
                                current_count = 0;
                            }
                            else
                            {
                                await Upload_addresses(position);
                                position += 10;
                                current_count -= 10;
                            }
                        }
                    }
                    Logger("Загрузка файлов завершена!", "Success", true);
                    Clear_data();
                    if (!string.IsNullOrEmpty(problem_block) || !string.IsNullOrWhiteSpace(problem_block))
                        Logger("Список IP-адресов, на которые не получилось загрузить файлы:\r\n" + problem_block,"Error",true);
                }
                else
                {
                    Logger("Обнаружена ошибка во время проверки входных данных!", "Error");
                    start_download_button.IsEnabled = true;
                    stop_download_button.IsEnabled = false;
                }
            }
            catch (Exception e)
            {
                Logger("Произошла ошибка во время параллельной загрузки файлов!", "Error", true);
                Logger(e.Message,"Error",true);
            }
        }

        private async void Standart_upload()
        {
            try
            {
                await Task.Delay(500);
                CancellationToken cancel_token = cancelTokenSource.Token;
                Logger("Выбрана последовательная загрузка файлов.", "Info", true);
                current_status_bar.Value = 0;
                current_status.Content = "Статус загрузки:";
                download_log.Inlines.Clear();
                Logger("Проверяю входные данные, ожидайте ...", "Info");
                if (Checker())
                {
                    await Task.Delay(500);
                    Init_data();
                    Logger("Входные данные успешно проверены!", "Success", true);
                    Logger("Начинаю загрузку файлов ...", "Info");
                    for (int i = 0; i < addresses.Count; i++)
                    {
                        try
                        {
                            await Upload_address(addresses[i], cancel_token);

                        }
                        catch (Exception e)
                        {
                            Logger("Произошла ошибка во время загрузки файлов на адрес " + addresses[i] + "!", "Error", true);
                            Logger(e.Message, "Error", true);
                            Logger("Перехожу к следующему адресу.", "Info", true,true);
                            continue;
                        }
                    }
                    Logger("Работа скрипта завершена!", "Success", true);
                    Clear_data();
                    if (!string.IsNullOrEmpty(problem_block) || !string.IsNullOrWhiteSpace(problem_block))
                        Logger("Список IP-адресов, на которые не получилось загрузить файлы:\r\n" + problem_block, "Error", true);
                }
            
                else
                {
                    Logger("Обнаружена ошибка во время проверки входных данных!", "Error");
                    start_download_button.IsEnabled = true;
                    stop_download_button.IsEnabled = false;
                    return;
                }
            }
            catch (Exception e)
            {
                Logger("Произошла ошибка во время последовательной загрузки файлов!", "Error", true);
                Logger(e.Message, "Error", true);
            }
        }

        private void standart_upload_button_Checked(object sender, RoutedEventArgs e)
        {
            standart_upload = true;
        }

        private void parallel_upload_button_Checked(object sender, RoutedEventArgs e)
        {
            standart_upload = false;
        }

        private void close_app_button_Click(object sender, RoutedEventArgs e)
        {
            cancelTokenSource.Cancel();
            Close();
        }

        private void hide_app_button_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Create_final_file(string problem_box = "")
        {
            if (!need_file_log) return;
            var same_window = Process.GetProcessesByName("Uploader");
        }

    }
}