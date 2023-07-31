using IniParser;
using IniParser.Model;
using MayPlayer.Properties;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MayPlayer
{
    public partial class Form3 : Form
    {
        private string selectedSkinPath;
        private FileIniDataParser iniParser;
        private IniData settingsData;
        private Form1 settings; // Добавляем параметр для ссылки на Form1
        public Form3(Form1 form1) // Добавляем Form1 как аргумент в конструктор
        {
            InitializeComponent();
            iniParser = new FileIniDataParser();
            this.settings = form1; // Сохраняем ссылку на Form1 в поле класса
        }
        public void LoadSkins()
        {
            string skinsFolder = "skins";
            if (Directory.Exists(skinsFolder))
            {
                // Получаем пути ко всем файлам с расширением .ini в папке "skins"
                string[] skinFiles = Directory.GetFiles(skinsFolder, "*.ini");

                // Очищаем listBox1 перед загрузкой новых скинов
                listBox1.Items.Clear();

                // Добавляем имена файлов с расширением .ini в listBox1
                foreach (string skinFile in skinFiles)
                {
                    listBox1.Items.Add(Path.GetFileName(skinFile));
                }
            }
        }

        private string[] loadedSkins;

        public Form3()
        {
            InitializeComponent();
        }

        // Метод для загрузки скинов в ListBox1
        public void LoadSkins(string[] skins)
        {
            loadedSkins = skins; // Сохраняем загруженные скины
            listBox1.Items.Clear(); // Очищаем ListBox1 перед загрузкой новых скинов
            listBox1.Items.AddRange(loadedSkins); // Заполняем ListBox1 загруженными скинами
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            // Загрузка настроек из файла settings.ini
            LoadSettings();

            // Применение настроек к форме
            ApplySettings();
            settings = (Form1)this.Owner;
            LoadSkins();
        }

        private void LoadSettings()
        {
            FileIniDataParser iniParser = new FileIniDataParser(); // Создаем экземпляр FileIniDataParser
                                                                   // Проверяем наличие файла settings.ini
            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MayPlayer");
            string settingsFile = Path.Combine(appDataFolder, "settings.ini");

            if (File.Exists(settingsFile))
            {
                // Чтение файла настроек
                settingsData = iniParser.ReadFile(settingsFile);
                selectedSkinPath = settingsData["App"]["Skin"];
            }
            else
            {
                // Если файла нет, создаем новый INI-файл и устанавливаем путь к скинам по умолчанию
                settingsData = new IniData();
                settingsData.Sections.AddSection("App");
                settingsData["App"]["Skin"] = "";
                selectedSkinPath = "";
                iniParser.WriteFile(settingsFile, settingsData);
            }
        }

        private void ApplySettings()
        {
            // Применение настроек к форме или другим элементам управления
            // ...
        }

        public void SaveSettings()
        {
            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MayPlayer");
            string settingsFile = Path.Combine(appDataFolder, "settings.ini");

            // Записываем настройки обратно в файл settings.ini
            iniParser.WriteFile(settingsFile, settingsData);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Выбор папки кастомных скинов с помощью folderBrowserDialog1
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                selectedSkinPath = folderBrowserDialog1.SelectedPath;

                // Сохраняем выбранный путь в INI-файле
                settingsData["App"]["Skin"] = selectedSkinPath;
                SaveSettings();

                // Применяем настройки к форме (или обновляем список скинов в Form1)
                ApplySettings();

                // Выводим ini файлы в listBox1
                PopulateListBoxWithIniFiles(selectedSkinPath);
            }
        }

        private void PopulateListBoxWithIniFiles(string folderPath)
        {
            // Очищаем listBox1 перед добавлением новых файлов
            listBox1.Items.Clear();

            // Получаем список всех ini файлов из указанной папки
            string[] iniFiles = Directory.GetFiles(folderPath, "*.ini");

            // Добавляем имена ini файлов в listBox1
            listBox1.Items.AddRange(iniFiles.Select(Path.GetFileName).ToArray());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            LoadSkins();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string skinsDirectory = Path.Combine(Application.StartupPath, "skins");

            if (listBox1.SelectedItem != null)
            {
                // Получаем выбранный путь к скину из listBox1
                string selectedFileName = listBox1.SelectedItem.ToString();
                selectedSkinPath = Path.Combine(skinsDirectory, selectedFileName);

                // Применяем выбранный скин к Form1
                ApplySelectedSkin();

                // Сохраняем путь к выбранному скину в настройках (settings.ini)
                settingsData["App"]["Skin"] = selectedSkinPath;
                SaveSettings();

                // Убедитесь, что Form1 (mainForm) была инициализирована перед использованием
                if (settings != null)
                {
                    settings.ApplySkin(selectedSkinPath);
                }
            }
        }

        private void ApplySelectedSkin()
        {
            // Здесь реализуйте логику применения выбранного скина из selectedSkinPath к Form1
            // Например, загрузите изображение скина и установите его фоном формы, цветами элементов и т. д.
            // Предполагается, что вы умеете применять скин к форме и её элементам.
            settings.ApplySkin(selectedSkinPath); // Предполагается, что у Form1 есть метод ApplySkin для применения скина.
            settings = (Form1)this.Owner;
        }

        private void ApplySelectedLanguange()
        {
            // Здесь реализуйте логику применения выбранного скина из selectedSkinPath к Form1
            // Например, загрузите изображение скина и установите его фоном формы, цветами элементов и т. д.
            // Предполагается, что вы умеете применять скин к форме и её элементам.
            settings.ApplySkin(selectedSkinPath); // Предполагается, что у Form1 есть метод ApplySkin для применения скина.
            settings = (Form1)this.Owner;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
            {
                settings = (Form1)this.Owner;
                settings.panel1.Visible = true;
                settings.panel2.Visible = true;
                settings.button8.Visible = false;
            }
            else
            {
                settings = (Form1)this.Owner;
                settings.button8.Visible = true;
                settings.button8.Text = "︾";
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                settings = (Form1)this.Owner;
                settings.pictureBox2.Visible = false;
            }
            else
            {
                settings = (Form1)this.Owner;
                settings.pictureBox2.Visible = true;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // Открываем диалог выбора шрифта
            if (fontDialog1.ShowDialog() == DialogResult.OK)
            {
                // Сохраняем текущий текст textBox1
                string currentText = textBox1.Text;

                // Отображаем название выбранного шрифта в textBox1
                string newFontText = fontDialog1.Font.Name + ", " + fontDialog1.Font.Size + ", " + fontDialog1.Font.Style.ToString();

                // Получаем текущую позицию курсора
                int cursorPosition = textBox1.SelectionStart;

                // Вставляем новый текст в положение курсора
                textBox1.Text = currentText.Insert(cursorPosition, newFontText);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            // Открываем диалог выбора цвета
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                // Сохраняем текущий текст textBox1
                string currentText = textBox1.Text;

                // Отображаем выбранный цвет в textBox1 в формате "text=#RRGGBB"
                string newColorText = "#" + colorDialog1.Color.R.ToString("X2") + colorDialog1.Color.G.ToString("X2") + colorDialog1.Color.B.ToString("X2");

                // Получаем текущую позицию курсора
                int cursorPosition = textBox1.SelectionStart;

                // Вставляем новый текст в положение курсора
                textBox1.Text = currentText.Insert(cursorPosition, newColorText);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            // Формируем сообщение с подсказкой по настройкам
            StringBuilder message = new StringBuilder();
            message.AppendLine("Пример взят из темы hacker.ini");
            message.AppendLine();
            message.AppendLine("FormSettings:");
            message.AppendLine("BackgroundColor=#000000 (устанавливает черный фон)");
            message.AppendLine("ForeColor=#FFFFFF (устанавливает белый цвет для текста и элементов управления)");
            message.AppendLine();
            message.AppendLine("LabelSettings:");
            message.AppendLine("ForeColor=#08ff45 (устанавливает зеленый цвет для текста Label)");
            message.AppendLine("BackColor=#000000 (устанавливает черный фон для элементов Label)");
            message.AppendLine();
            message.AppendLine("ButtonSettings:");
            message.AppendLine("ForeColor=#08ff45 (устанавливает зеленый цвет для текста на кнопках)");
            message.AppendLine("BackColor=#000000 (устанавливает черный фон для кнопок)");
            message.AppendLine();
            message.AppendLine("MenuSettings:");
            message.AppendLine("ForeColor=#000000 (устанавливает черный цвет для текста в меню)");
            message.AppendLine();
            message.AppendLine("WindowSettings:");
            message.AppendLine("WindowFont=Consolas, 8, Regular (устанавливает шрифт 'Consolas' с размером 8 и стилем 'Regular' для текста в окне формы)");
            message.AppendLine();
            message.AppendLine("TextSettings:");
            message.AppendLine("WindowFont=Consolas, 20, Bold (устанавливает шрифт 'Consolas' с размером 20 и стилем 'Bold' для текстовых элементов)");
            message.AppendLine();
            message.AppendLine("OtherTextSettings:");
            message.AppendLine("WindowFont=Consolas, 11, Regular (устанавливает шрифт 'Consolas' с размером 11 и стилем 'Regular' для других текстовых элементов)");
            message.AppendLine();
            message.AppendLine("ItalicTextSettings:");
            message.AppendLine("WindowFont=Consolas, 11, Italic (устанавливает шрифт 'Consolas' с размером 11 и стилем 'Italic' для текстовых элементов с наклоном)");
            message.AppendLine();
            message.AppendLine("Важно! При установки шрифта, необходиммо убрать (8,25), чтобы выглядело вот так (8), иначе возникнут ошибки с интерфейсом");

            // Отображаем сообщение с подсказкой в MessageBox
            MessageBox.Show(message.ToString(), "Подсказка по настройкам", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                // Получаем выбранный элемент из ComboBox1
                string selectedItem = comboBox1.SelectedItem.ToString();

                // Добавляем выбранный элемент в ListBox2
                listBox2.Items.Add(selectedItem);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            listBox2.Items.Add(textBox1.Text);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndex != -1)
            {
                // Удаляем выбранный элемент из listBox1
                listBox2.Items.RemoveAt(listBox2.SelectedIndex);
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            // Открываем диалог сохранения файла с фильтром на расширение .ini
            saveFileDialog1.Filter = "INI Файл|*.ini";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                // Создаем новый файл и открываем его для записи
                using (StreamWriter writer = new StreamWriter(saveFileDialog1.FileName))
                {
                    // Записываем каждый элемент из listBox1 в файл
                    foreach (var item in listBox2.Items)
                    {
                        writer.WriteLine(item.ToString());
                    }
                }

                MessageBox.Show("Содержимое конструктора успешно сохранено в файл .ini.");
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
        }
    }
}
