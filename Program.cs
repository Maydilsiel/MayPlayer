using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TagLib;

namespace MayPlayer
{
    internal static class Program
    {
        // Создаем список для хранения путей обработанных файлов
        private static System.Collections.Generic.List<string> processedFiles = new System.Collections.Generic.List<string>();

        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0)
            {
                // Если есть аргументы командной строки, создаем форму Form1 и передаем ее в метод ProcessFiles
                Form1 form1 = new Form1();
                ProcessFiles(form1, args);
            }
            else
            {
                // Создаем форму Form1
                Form1 form1 = new Form1();

                // Загружаем скины в форму Form3
                Form3 form3 = new Form3();
                string skinsFolder = "skins";
                if (Directory.Exists(skinsFolder))
                {
                    string[] skinFiles = Directory.GetFiles(skinsFolder, "*.ini");
                    form3.LoadSkins(skinFiles);
                }

                // Запускаем главную форму Form1 и форму Form3 в режиме диалогового окна
                Application.Run(form1);
            }
        }

        private static void ProcessFiles(Form1 form1, string[] files)
        {
            foreach (string filePath in files)
            {
                // Проверяем, есть ли путь в списке обработанных файлов
                if (processedFiles.Contains(filePath))
                {
                    continue; // Пропускаем файл, если он уже был обработан
                }

                if (Directory.Exists(filePath))
                {
                    ProcessDirectory(form1, filePath);
                }
                else
                {
                    ProcessFile(form1, filePath);
                }

                // Добавляем путь в список обработанных файлов
                processedFiles.Add(filePath);
            }

            // Проверяем, есть ли загруженные файлы в списке listBox1 и если нет, выходим из метода
            if (form1.listBox1.Items.Count == 0)
            {
                form1.StopPlayback();
                return;
            }

            // Останавливаем воспроизведение перед запуском приложения
            form1.StopPlayback();

            // Устанавливаем выбранный индекс в listBox1 и listBox2, если есть элементы в списках
            form1.listBox2.SelectedIndex = 0;

            // Начинаем воспроизведение первого трека

            Application.Run(form1);
        }

        private static void ProcessFile(Form1 form1, string filePath)
        {
            string fileExtension = Path.GetExtension(filePath).ToLower();
            if (IsMusicExtension(fileExtension))
            {
                if (form1.listBox1.Items.Count > 0 && !form1.IsMusicPlaying())
                {
                    // Если есть уже загруженные файлы и музыка не воспроизводится в данный момент, добавляем новый файл в список и начинаем воспроизведение
                    form1.AddFileToListBox(filePath);
                    form1.Select();
                }
                else if (form1.listBox1.Items.Count == 0)
                {
                    // Если список пуст, просто добавляем файл в список без начала воспроизведения
                    form1.StopPlayback();
                    form1.AddFileToListBox(filePath);
                }
            }
        }

        private static void ProcessDirectory(Form1 form1, string directoryPath)
        {
            string[] files = Directory.GetFiles(directoryPath);
            foreach (string file in files)
            {
                ProcessFile(form1, file);
            }

            string[] subdirectories = Directory.GetDirectories(directoryPath);
            foreach (string subdirectory in subdirectories)
            {
                ProcessDirectory(form1, subdirectory);
            }
        }

        private static bool IsMusicExtension(string fileExtension)
        {
            string[] musicExtensions = { ".mp3", ".wav", ".flac", ".ogg", ".m4a", ".wma", ".aac" };
            return musicExtensions.Contains(fileExtension);
        }
    }
}
