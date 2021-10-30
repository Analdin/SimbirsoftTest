using System;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

namespace Simbirsoft_parsing
{
    //Debug
    public class CommonData
    {
        public static bool Debug;
    }

    public static class DataBaseJob
    {
        public static string Server { get; set; } = "37.140.192.191";
        public static string DatabaseName { get; set; } = "u1486803_simbirsoftbase";
        public static string UserName { get; set; } = "u1486803_adminsi";
        public static string Password { get; set; } = "simbirsoft";

        private static int Timeout = Timeout;

        private static MySqlConnection Connection { get; set; }

        public static bool IsConnect()
        {
            bool connected = true;

            if (Connection == null)
            {
                try
                {
                    //string connstring = string.Format("Server={0}; database={1}; UID={2}; password={3};CharSet=UTF8;", Server, DatabaseName, UserName, Password);
                    string connstring = $"Server{Server};database{DatabaseName};UID{UserName};password{Password};CharSet=UTF8";

                    Connection = new MySqlConnection(connstring);
                    Connection.Open();
                    Console.WriteLine("Подключение успешно");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка соединения" + ex);
                    connected = false;
                }
            }
            else
            {
                try
                {
                    connected = (Connection.State == System.Data.ConnectionState.Open) ? true : false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка: " + ex);
                    connected = false;
                }
            }

            return connected;
        }
        public static void Close()
        {
            Connection.Close();
        }
        public static void AddWordItems(KeyValuePair<string, int> element)
        {
            try
            {
                string query = $"INSERT INTO DataStorage(word, count)  VALUES('{element.Key}', { element.Value})";

                using (MySqlCommand cmd = new MySqlCommand(query, Connection))
                {
                    cmd.CommandTimeout = Timeout;
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex);
            }
        }
    }

    class Program
    {
        //Работаем со списоком, возвращаем список с введенными сайтами
        public static List<Uri> SitesSplit()
        {
            Console.ForegroundColor = ConsoleColor.Green;

            string enterSites = string.Empty;
            List<Uri> result = new List<Uri>();

            CommonData.Debug = File.Exists(Directory.GetCurrentDirectory() + @"\fixxx.txt");

            Console.WriteLine("Введите домен...");

            if (CommonData.Debug)
                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));
            enterSites = Console.ReadLine();

            Console.WriteLine($"Вы ввели следующие сайты: {enterSites}");
            if (CommonData.Debug)
                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1500));

            //Разбираем сайты по Split

            string[] sites = enterSites.Split(',');

            foreach (var url in sites)
            {
                result.Add(new Uri(url));
                Console.WriteLine("Добавили в список сайт - " + url);
            }

            return result;
        }

        //Получаем Dom Html страницы сайта
        public static string GetDomModel(string FinalText)
        {
            Console.ForegroundColor = ConsoleColor.Green;

            List<Uri> ALlSites = SitesSplit();

            string LoneSite = ALlSites[0].ToString();
            Console.WriteLine("Получен сайт - " + LoneSite);

            if (String.IsNullOrWhiteSpace(LoneSite))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Не удается получить сайт из списка..");
                throw new Exception();
            }

            Console.ForegroundColor = ConsoleColor.Green;

            var html = LoneSite;

            HtmlWeb web = new HtmlWeb();
            var HtmlDoc = web.Load(html);

            HtmlNode node = HtmlDoc.DocumentNode.SelectSingleNode("//html");

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(node.InnerText);
            FinalText = node.InnerText;

            Console.WriteLine(node.InnerText);

            if (CommonData.Debug)
                System.Threading.Thread.Sleep(1000);

            return FinalText;
        }

        public static void ClearOldReports()
        {
            string ReportPath = Directory.GetCurrentDirectory() + @"\Report\report.txt";
            File.WriteAllText(ReportPath, null);
        }

        private static Dictionary<string, int> Parse(string text)
        {
            string ReportPath = Directory.GetCurrentDirectory() + @"\Report\report.txt";

            var result = new Dictionary<string, int>();

            string[] chunks = text.Split(' ', ',', ';', '<', '>');

            foreach (var chunk in chunks)
            {
                if (!result.ContainsKey(chunk))
                    result.Add(chunk, 0);

                result[chunk]++;
            }

            foreach (var element in result)
            {
                File.AppendAllText(ReportPath, element.ToString() + "\n");
                Console.WriteLine("Элемент: " + element);
            }

            foreach (var element in result)
            {
                DataBaseJob.AddWordItems(element);
            }

            return result;
        }

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;

            DataBaseJob.IsConnect();

            string FinalText = string.Empty;
            string ErrFilePath = Directory.GetCurrentDirectory() + @"\Errors\ErrLog.txt";
            string ReportPath = Directory.GetCurrentDirectory() + @"\Report\report.txt";

            if (!File.Exists(ErrFilePath) && File.Exists(ReportPath))
            {
                File.Create(ErrFilePath);
                File.Create(ReportPath);
            }

            ClearOldReports();

            CommonData.Debug = File.Exists(Directory.GetCurrentDirectory() + @"\fixxx.txt");

            //Перекидываем все что получили методом в список для работы
            List<Uri> SplitedSites = SitesSplit();

            //Скидываем текст с Html страницы в переменную для работы
            string ParsedText = GetDomModel(FinalText);

            if (CommonData.Debug)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Полученный текст со страницы: - " + ParsedText);
            }

            //Убираем лишние символы с текста и разбираем на отдельные слова регуляркой
            Regex regex = new Regex(@"[а-яёА-ЯЁ]+");
            MatchCollection matches = regex.Matches(ParsedText.ToLower());

            Parse(ParsedText);
        }
    }
}
