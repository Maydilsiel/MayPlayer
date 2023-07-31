using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;
using NAudio.Wave;
using TagLib;
using NAudio.Wave.SampleProviders;
using System.IO;
using NAudio;
using NAudio.Gui;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing.Drawing2D;
using System.Threading;
using Un4seen.Bass;
using Un4seen.Bass.Misc;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using IniParser;
using IniParser.Model;
using System.Globalization;
using System.Drawing.Imaging;
using Microsoft.Build.Framework.XamlTypes;


namespace MayPlayer
{
    public partial class Form1 : Form
    {
        private AudioFileReader audioFile;
        private WaveOutEvent outputDevice = new WaveOutEvent();
        private bool isRepeatEnabled = false;
        private bool isRepeatPlaylistEnabled = false; // Повтор по списку
        private bool isShuffleEnabled = false; // Повтор в разброс
        private List<string> shuffledPlaylist; // Новый список для случайного порядка песен
        private int currentShuffledIndex = 0; // Текущий индекс в случайном списке
        private Random random = new Random();
        private List<int> playedIndexes = new List<int>();
        private int currentSongIndex = -1;
        private bool isSongEnded = false;
        private bool isRepeatSongEnabled = false;
        private string previousSelectedFile;
        private bool isPaused = false;
        private bool isTrackEnded = false;
        private bool isPlaying = false;
        private int currentTrackIndex = -1;
        private string currentFilePath = "";
        private bool isSwitchingTrack = false;
        private SoundPlayer player;
        private bool isTrackFullyPlayed = true;
        private string previousFilePath = string.Empty;
        private AudioFileReader loop;
        private WaveOutEvent repeatWaveOut;
        private bool isSeeking = false;
        private bool isReplayingSong = false;
        private bool isSongPlaying = false;
        private bool isRepeatCurrentTrackEnabled = false;
        private bool isFirstTrack = true; // Флаг, указывающий, что это первый трек после перемешивания
        private bool isFirstTrackPlayed = false;
        private bool isSwitchingToRandom = false; // Флаг для определения, происходит ли переключение на случайную песню
        private bool isReplayingRandom = false;
        private int randomTrackCounter = 0;
        private bool isRepeatCircularEnabled = false;
        private float previousVolume;
        bool isRandomPlaybackEnabled = false;
        private FileIniDataParser iniParser;
        private IniData settingsData;
        private string selectedSkinPath;
        private string languanges;
        private int blurAmount = 10;

        public Form1()
        {
            InitializeComponent();
            iniParser = new FileIniDataParser();
            this.AllowDrop = true;
            //--------------------------Tray---------------------------------//
        }

        public Form1(string[] files) : this()
        {
            foreach (string filePath in files)
            {
                if (Directory.Exists(filePath))
                {
                    ProcessDirectory(filePath);
                }
                else
                {
                    ProcessFile(filePath);
                }
            }
        }

        public void AddFileToListBox(string filePath)
        {
            listBox1.Items.Add(filePath);

            var file = TagLib.File.Create(filePath);
            string artist = file.Tag.FirstPerformer;
            string title = file.Tag.Title;
            string displayText = string.IsNullOrEmpty(artist) ? filePath : $"{artist} - {title}";

            listBox2.Items.Add(displayText);

            if (listBox1.Items.Count == 1)
            {
                listBox1.SelectedIndex = 0; // Устанавливаем выбранный индекс 0, если список был пуст
            }

            if (listBox2.Items.Count == 1)
            {
                listBox2.SelectedIndex = 0; // Устанавливаем выбранный индекс 0, если список был пуст
            }
        }

        private enum PlayerMode
        {
            Player,
            Tray
        }

        private PlayerMode currentMode = PlayerMode.Player; // По умолчанию устанавливаем режим плеера


        private NotifyIcon trayIcon;        
        private void Form1_Load(object sender, EventArgs e)
        {
            outputDevice = new WaveOutEvent();
            player = new SoundPlayer();
            groupBox1.Text = $"Громкость: {trackBar2.Value}%";

            // Инициализация outputDevice, если он не был инициализирован ранее
            if (outputDevice == null)
            {
                outputDevice = new WaveOutEvent();
            }

            outputDevice.Volume = trackBar2.Value / 100f;

            //---------------------------Skinsload--------------------------//
            LoadSettings();
            ApplySettings();
            //--------------------------Tray--------------------------------//
            this.Icon = Properties.Resources.mayplayer;

            // Добавляем обработчик для события FormClosing, чтобы скрывать окно при закрытии, если плеер находится в режиме трея
            this.FormClosing += Form1_FormClosing;
            //---------------------------------------------------------------//
            pictureBox2.Image = pictureBox1.Image;
            ApplyBackgroundBlur();
            //-----------------------------------------------------------------//
            if (listBox1.Items.Count > 0)
            {
                listBox1.SelectedIndex = 0;
            }

            if (listBox2.Items.Count > 0)
            {
                string selectedFile = listBox1.Items[listBox2.SelectedIndex].ToString();
                var file = TagLib.File.Create(selectedFile);
                string artist = file.Tag.FirstPerformer;
                string title = file.Tag.Title;
                // Обновление заголовка окна плеера
                string windowTitle;
                if (!string.IsNullOrEmpty(artist) && !string.IsNullOrEmpty(title))
                {
                    windowTitle = $"MayPlayer | {artist} - {title}";
                }
                else
                {
                    windowTitle = $"MayPlayer | {selectedFile}";
                }
                this.Text = windowTitle;
                listBox2.SelectedIndex = 0;
                PlayM();
            }

        }

        private void ApplyBackgroundBlur()
        {
            // Загружаем текущее изображение фона формы в объект Bitmap
            Bitmap backgroundImage = new Bitmap(pictureBox1.Image);

            // Масштабируем изображение до размеров pictureBox2
            backgroundImage = ResizeImage(backgroundImage, pictureBox2.Width, pictureBox2.Height);

            // Создаем новое изображение с тем же размером, что и pictureBox2
            Bitmap blurredImage = new Bitmap(pictureBox2.Width, pictureBox2.Height);

            // Создаем объект Graphics для работы с изображением
            using (Graphics graphics = Graphics.FromImage(blurredImage))
            {
                // Создаем матрицу размытия
                using (ImageAttributes imageAttributes = new ImageAttributes())
                {
                    float[][] matrix =
                    {
                new float[] { 1, 0, 0, 0, 0 }, // Красный цвет
                new float[] { 0, 1, 0, 0, 0 }, // Зеленый цвет
                new float[] { 0, 0, 1, 0, 0 }, // Синий цвет
                new float[] { 0, 0, 0, 0.2f, 0 }, // Прозрачность (здесь можно задать уровень размытия)
                new float[] { 0, 0, 5, 0, 10 } // Альфа-канал (непрозрачность)
            };

                    ColorMatrix colorMatrix = new ColorMatrix(matrix);
                    imageAttributes.SetColorMatrix(colorMatrix);

                    // Рисуем размытое изображение
                    graphics.DrawImage(backgroundImage, new Rectangle(0, 0, pictureBox2.Width, pictureBox2.Height), 0, 0, pictureBox2.Width, pictureBox2.Height, GraphicsUnit.Pixel, imageAttributes);
                }
            }

            // Устанавливаем размытое изображение как фон для pictureBox2
            pictureBox2.Image = blurredImage;
        }

        private Bitmap ResizeImage(Image image, int width, int height)
        {
            Bitmap resizedImage = new Bitmap(width, height);
            using (Graphics graphics = Graphics.FromImage(resizedImage))
            {
                graphics.DrawImage(image, 0, 0, width, height);
            }
            return resizedImage;
        }


        private void UpdateTrackBarPosition()
        {
            if (audioFile != null && outputDevice != null && outputDevice.PlaybackState == PlaybackState.Playing)
            {
                trackBar1.Value = (int)audioFile.CurrentTime.TotalSeconds;
            }
        }

        private void загрузитьПеснюToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Audio Files (*.wav;*.mp3;*.ogg;*.flac;*.m4a;*.wma;*.aac)|*.wav;*.mp3;*.ogg;*.flac;*.m4a;*.wma;*.aac|All Files (*.*)|*.*";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string selectedFile = openFileDialog1.FileName;
                player.SoundLocation = selectedFile;

                // Извлечение метаданных
                var file = TagLib.File.Create(selectedFile);
                string artist = file.Tag.FirstPerformer;
                string title = file.Tag.Title;

                // Добавление песни в ListBox1
                listBox1.Items.Add(selectedFile);

                // Добавление названия композиции в ListBox2
                string songInfo;
                if (!string.IsNullOrEmpty(title))
                {
                    songInfo = title;
                }
                else
                {
                    songInfo = selectedFile;
                }
                listBox2.Items.Add(artist + songInfo);

                // Автовыбор только что загруженной песни и композиции
                listBox1.SelectedItem = selectedFile;
                listBox2.SelectedItem = artist + songInfo;
            }
        }

        private void ShufflePlaylist()
        {
            if (listBox1.Items.Count == 0)
                return;

            shuffledPlaylist = new List<string>();

            foreach (var item in listBox1.Items)
            {
                shuffledPlaylist.Add(item.ToString());
            }

            // Тасуем список песен случайным образом
            int n = shuffledPlaylist.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                string value = shuffledPlaylist[k];
                shuffledPlaylist[k] = shuffledPlaylist[n];
                shuffledPlaylist[n] = value;
            }
        }

        public void PlayM()
        {
            if (listBox1.SelectedItem != null && listBox2.SelectedIndex == listBox1.SelectedIndex)
            {
                string selectedFile = listBox1.SelectedItem.ToString();

                if (currentFilePath != selectedFile)
                {
                    // Освобождаем ресурсы текущего трека и outputDevice, если они уже существуют
                    if (outputDevice != null)
                    {
                        outputDevice.Stop();
                        outputDevice.Dispose();
                    }

                    if (audioFile != null)
                    {
                        audioFile.Dispose();
                    }

                    // Создаем новый объект AudioFileReader с выбранным файлом
                    audioFile = new AudioFileReader(selectedFile);
                    outputDevice = new WaveOutEvent();
                    outputDevice.Init(audioFile);

                    // Обновляем trackBar1
                    trackBar1.Minimum = 0;
                    trackBar1.Maximum = (int)audioFile.TotalTime.TotalSeconds;

                    // Включаем воспроизведение
                    outputDevice.Play();

                    currentFilePath = selectedFile;
                    currentTrackIndex = listBox1.SelectedIndex; // Запоминаем индекс текущего трека
                    isPlaying = true;
                    label6.Text = "▶";

                    // Обновляем метку с временем окончания песни
                    label5.Text = audioFile.TotalTime.ToString(@"hh\:mm\:ss");

                    // Включаем таймер для обновления времени воспроизведения
                    timer1.Start();
                }
                else
                {
                    if (!isPlaying)
                    {
                        outputDevice.Play();
                        isPlaying = true;
                        label6.Text = "▶";
                        button1.Text = "▶";
                    }
                    else
                    {
                        outputDevice.Pause();
                        isPlaying = false;
                        label6.Text = "||";
                        button1.Text = "||";
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PlayM();
        }

        public bool IsMusicPlaying()
        {
            // Ваш код для проверки, воспроизводится ли музыка в данный момент
            // Например, можно использовать состояние плеера или другую логику проверки
            return false; // Здесь верните true, если музыка воспроизводится, и false в противном случае
        }

        internal delegate void Button1ClickEventHandler(object sender, EventArgs e);
        internal event Button1ClickEventHandler OnButton1Click;

        public void PerformButtonClick()
        {
            OnButton1Click?.Invoke(this, EventArgs.Empty);
        }


        private void audioFile_PositionChanged(object sender, EventArgs e)
        {
            if (audioFile != null && outputDevice.PlaybackState == PlaybackState.Playing)
            {
                trackBar1.Invoke((MethodInvoker)delegate
                {
                    trackBar1.Value = (int)audioFile.CurrentTime.TotalSeconds;
                });

                // Обновляем метку с текущим временем воспроизведения
                label4.Invoke((MethodInvoker)delegate
                {
                    label4.Text = audioFile.CurrentTime.ToString(@"hh\:mm\:ss");
                });

                // Обновляем метку с временем окончания песни
                label5.Invoke((MethodInvoker)delegate
                {
                    label5.Text = audioFile.TotalTime.ToString(@"hh\:mm\:ss");
                });
            }
        }


        private void OutputDevice_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            // Освобождаем ресурсы трека, так как воспроизведение закончено
            if (audioFile != null)
            {
                audioFile.Dispose();
                audioFile = null;
            }

            // Прекращаем обработку, если в данный момент идет переключение треков
            if (isSwitchingTrack)
            {
                return;
            }

            // Событие возникает, когда воспроизведение завершено
            // Здесь мы можем проиграть следующий трек
            int currentIndex = listBox1.SelectedIndex;
            int nextIndex;

            if (!isFirstTrackPlayed)
            {
                // Проигрываем следующий трек по порядку, так как это первый трек
                nextIndex = (currentIndex + 1) % listBox1.Items.Count;
                isFirstTrackPlayed = true; // Устанавливаем флаг начала проигрывания
            }

            // Проверяем, является ли следующая песня такой же, как предыдущая
            if (listBox1.SelectedItem.ToString() == currentFilePath)
            {
                if (isRepeatSongEnabled)
                {
                    // При повторе песни не выполняем переключение, просто проигрываем её заново
                    PlaySelectedFile(currentFilePath);
                }
                return;
            }

            // Задержка в 200 миллисекунд для избежания конфликтов при переключении
            //Thread.Sleep(200);

            // Устанавливаем флаг переключения трека перед вызовом PlaySelectedFile
            isSwitchingTrack = true;

            // Проигрываем выбранную песню
            PlaySelectedFile(listBox1.SelectedItem.ToString());

            // Сбрасываем флаг переключения трека после завершения переключения
            isSwitchingTrack = false;

            // Обновляем текущий файл и состояние воспроизведения
            currentFilePath = listBox1.SelectedItem.ToString();
            isPlaying = true;
            label6.Text = "▶";

            this.Text = $"MayPlayer | {label1.Text} - {label2.Text}";

            // При повторном воспроизведении активируем новый экземпляр repeatWaveOut
            if (isRepeatSongEnabled)
            {
                repeatWaveOut = new WaveOutEvent();
                repeatWaveOut.Init(loop);
                repeatWaveOut.Play();
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (audioFile != null)
            {
                if (audioFile.CanSeek && !isSeeking)
                {
                    // Устанавливаем флаг блокировки обновления времени
                    isSeeking = true;

                    // Если проигрывание приостановлено (песня завершена)
                    if (isPaused)
                    {
                        // Возобновляем воспроизведение
                        outputDevice.Play();

                        // Сбрасываем флаг приостановки
                        isPaused = false;

                        // Возобновляем таймер обновления времени
                        timer1.Start();
                    }

                    // Устанавливаем текущее время воспроизведения в соответствии с позицией трекбара
                    audioFile.CurrentTime = TimeSpan.FromSeconds(trackBar1.Value);

                    // Обновляем метку с текущим временем воспроизведения, соответствующим новому значению трекбара
                    label4.Text = audioFile.CurrentTime.ToString(@"hh\:mm\:ss");

                    // Снимаем флаг блокировки обновления времени
                    isSeeking = false;
                }
            }
        }

        private void trackBar1_MouseUp(object sender, MouseEventArgs e)
        {
            // При отпускании мыши после перемотки трекбара, выполняем перемотку аудиофайла
            if (audioFile != null && !isPaused)
            {
                int newValue = trackBar1.Value;
                audioFile.CurrentTime = TimeSpan.FromSeconds(newValue);

                // Возобновляем воспроизведение
                outputDevice.Play();
            }
        }

        public void StopPlayback()
        {
            if (outputDevice != null && outputDevice.PlaybackState == PlaybackState.Playing)
            {
                outputDevice.Stop();
                outputDevice.Dispose();
                outputDevice = null;
            }

            if (audioFile != null)
            {
                audioFile.Dispose();
                audioFile = null;
            }

            // Останавливаем таймер обновления времени воспроизведения
            timer1.Stop();

            // Обнуляем состояние переменных
            isPlaying = false;
            currentFilePath = string.Empty;
            label4.Text = "00:00:00";
            trackBar1.Value = 0;
            if (outputDevice != null)
            {
                if (isPlaying)
                {
                    outputDevice.Pause(); // Если воспроизведение активно, делаем паузу
                    label6.Text = "||";
                    button1.Text = "||";
                }
                else
                {
                    outputDevice.Stop(); // Если воспроизведение на паузе или проиграно до конца, останавливаем
                    label6.Text = "◼";
                    button1.Text = "▶";
                }

                isPlaying = false;
            }
            label6.Text = "◼";
            this.Text = "MayPlayer";
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (outputDevice != null && outputDevice.PlaybackState == PlaybackState.Playing)
            {
                if (audioFile != null)
                {
                    // Обновляем значение TrackBar в соответствии с текущим временем песни
                    int newValue = (int)Math.Min(audioFile.CurrentTime.TotalSeconds, audioFile.TotalTime.TotalSeconds);
                    trackBar1.Value = Math.Min(newValue, trackBar1.Maximum);

                    // Обновляем метку с текущим временем воспроизведения
                    if (!isSeeking) // Проверяем, идет ли перемотка
                    {
                        label4.Text = audioFile.CurrentTime.ToString(@"hh\:mm\:ss");
                    }
                }
            }
            else if (outputDevice != null && outputDevice.PlaybackState == PlaybackState.Stopped)
            {
                // Воспроизведение закончено, сбрасываем позицию трекбара в 0
                trackBar1.Value = 0;
                label4.Text = "00:00:00";
                label5.Text = "00:00:00";

                // Останавливаем таймер, чтобы он перестал обновлять значения
                timer1.Stop();

                // Если активен режим повтора одной песни, проигрываем текущий трек заново
                if (isRepeatSongEnabled)
                {
                    PlaySelectedFile(currentFilePath);
                    outputDevice.Play();
                }
                // Если активен режим повтора по списку, проигрываем текущий трек снова
                else if (isRepeatPlaylistEnabled)
                {
                    PlaySelectedFile(currentFilePath);
                    outputDevice.Play();
                }
                // Если активен режим повтора текущего трека, проигрываем текущий трек заново
                else if (isRepeatCurrentTrackEnabled)
                {
                    PlaySelectedFile(currentFilePath);
                    outputDevice.Play();
                }
                else if (isRepeatCircularEnabled)
                {
                    // Если активен режим повтора по кругу, переходим к следующему треку в списке
                    int nextIndex = (currentTrackIndex + 1) % listBox1.Items.Count;
                    listBox1.SelectedIndex = nextIndex;
                    string selectedFile = listBox1.SelectedItem.ToString();
                    PlaySelectedFile(selectedFile);
                    currentFilePath = selectedFile;
                    currentTrackIndex = nextIndex; // Обновляем индекс текущего трека

                    // Если достигли конца списка, переходим к началу и продолжаем воспроизведение
                    if (currentTrackIndex == 0)
                    {
                        listBox1.SelectedIndex = 0;
                        selectedFile = listBox1.SelectedItem.ToString();
                        PlaySelectedFile(selectedFile);
                        currentFilePath = selectedFile;
                        currentTrackIndex = 0; // Обновляем индекс текущего трека
                    }
                }
                else if (isRandomPlaybackEnabled && listBox1.Items.Count > 1)
                {
                    // Если активен режим в разброс, проигрываем следующий трек случайным образом
                    int nextIndex = currentTrackIndex;
                    while (nextIndex == currentTrackIndex)
                    {
                        nextIndex = new Random().Next(listBox1.Items.Count);
                    }
                    listBox1.SelectedIndex = nextIndex;

                    string selectedFile = listBox1.SelectedItem.ToString();
                    PlaySelectedFile(selectedFile);
                    currentFilePath = selectedFile;
                    currentTrackIndex = nextIndex; // Обновляем индекс текущего трека
                }
                else // Если ни один из режимов повтора не активен и режим в разброс не активен, переходим к следующему треку в списке
                {
                    int nextIndex = (currentTrackIndex + 1) % listBox1.Items.Count;

                    // Проверяем, достигли ли мы конца списка
                    if (nextIndex == 0 && !isRepeatCircularEnabled)
                    {
                        // Если не включен режим повтора по кругу, останавливаем воспроизведение
                        StopPlayback();
                    }
                    else
                    {
                        listBox1.SelectedIndex = nextIndex;
                        string selectedFile = listBox1.SelectedItem.ToString();
                        PlaySelectedFile(selectedFile);
                        currentFilePath = selectedFile;
                        currentTrackIndex = nextIndex; // Обновляем индекс текущего трека
                    }
                }
            }
        }


        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            groupBox1.Text = $"Громкость: {trackBar2.Value}%";

            // Изменение громкости воспроизведения
            if (outputDevice != null)
            {
                outputDevice.Volume = trackBar2.Value / 100f;
            }
        }



        private void button4_Click(object sender, EventArgs e)
        {
            if (listBox1.Items.Count > 1)
            {
                int currentIndex = listBox1.SelectedIndex;
                int previousIndex = (currentIndex - 1 + listBox1.Items.Count) % listBox1.Items.Count;

                // Обновляем текущий индекс выбранной песни
                listBox1.SelectedIndex = previousIndex;
                listBox2.SelectedIndex = previousIndex; // Обновляем выбранный индекс в listBox2

                string selectedFile = listBox1.SelectedItem.ToString();

                // Проверяем, является ли следующая песня такой же, как предыдущая
                if (selectedFile == currentFilePath)
                {
                    return; // Прекращаем дальнейшее перелистывание
                }

                // Проигрываем выбранную песню
                PlaySelectedFile(selectedFile);

                // Обновляем текущий файл и состояние воспроизведения
                currentFilePath = selectedFile;
                label6.Text = "▶";

                // Если воспроизведение было на паузе, возобновляем его
                if (!isPlaying)
                {
                    outputDevice.Play();
                    isPlaying = true;
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (listBox1.Items.Count > 1)
            {
                int currentIndex = listBox1.SelectedIndex;
                int nextIndex = (currentIndex + 1) % listBox1.Items.Count;

                // Обновляем текущий индекс выбранной песни
                listBox1.SelectedIndex = nextIndex;
                listBox2.SelectedIndex = nextIndex; // Обновляем выбранный индекс в listBox2

                string selectedFile = listBox1.SelectedItem.ToString();

                // Проверяем, является ли следующая песня такой же, как предыдущая
                if (selectedFile == currentFilePath)
                {
                    return; // Прекращаем дальнейшее перелистывание
                }

                // Проигрываем выбранную песню
                PlaySelectedFile(selectedFile);

                // Обновляем текущий файл и состояние воспроизведения
                currentFilePath = selectedFile;
                label6.Text = "▶▶";

                // Если воспроизведение было на паузе, возобновляем его
                if (!isPlaying)
                {
                    outputDevice.Play();
                    isPlaying = true;
                }
            }
        }

        private void PlaySelectedFile(string filePath)
        {
            // Проверяем, отличается ли выбранный файл от текущего
            if (currentFilePath != filePath)
            {
                // Освобождаем ресурсы текущего трека, если они есть
                if (audioFile != null)
                {
                    audioFile.Dispose();
                    audioFile = null;
                }

                // Создаем новый объект AudioFileReader с выбранным файлом
                audioFile = new AudioFileReader(filePath);
                outputDevice.Init(audioFile);

                if (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    outputDevice.Stop();
                }

                currentFilePath = filePath;

                // Обновляем trackBar1
                trackBar1.Minimum = 0;
                trackBar1.Maximum = (int)audioFile.TotalTime.TotalSeconds;

                // Включаем воспроизведение
                outputDevice.Play();

                // Обновляем метку с временем окончания песни
                label5.Text = audioFile.TotalTime.ToString(@"hh\:mm\:ss");

                // Включаем таймер для обновления времени воспроизведения
                timer1.Start();

                // Обновляем значение trackBar1
                UpdateTrackBarPosition();
            }
            else
            {
                // Если активен режим повтора одной песни или повтор текущего трека, проигрываем текущий трек заново
                if (isRepeatSongEnabled || isRepeatCurrentTrackEnabled)
                {
                    // Освобождаем ресурсы текущего трека, если они есть
                    if (audioFile != null)
                    {
                        audioFile.Dispose();
                        audioFile = null;
                    }

                    // Создаем новый объект AudioFileReader с выбранным файлом
                    audioFile = new AudioFileReader(filePath);
                    outputDevice.Init(audioFile);

                    // Включаем воспроизведение
                    outputDevice.Play();

                    // Обновляем метку с временем окончания песни
                    label5.Text = audioFile.TotalTime.ToString(@"hh\:mm\:ss");

                    // Включаем таймер для обновления времени воспроизведения
                    timer1.Start();

                    // Обновляем значение trackBar1
                    UpdateTrackBarPosition();
                }
                // Иначе, если активен режим повтора по списку, продолжаем воспроизведение текущего трека
                else if (isRepeatPlaylistEnabled)
                {
                    outputDevice.Play();
                }
            }
            // Если проигрывание активно и активирован режим повтора по рандомному списку, переключаем трек на случайный
            if (outputDevice != null && outputDevice.PlaybackState == PlaybackState.Playing && isShuffleEnabled)
            {
                outputDevice.PlaybackStopped -= OutputDevice_PlaybackStopped; // Удаляем обработчик события PlaybackStopped
                outputDevice.PlaybackStopped += OutputDevice_PlaybackStopped; // Добавляем обработчик события PlaybackStopped
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            button1.Enabled = true;
            outputDevice.Pause();
            label6.Text = "||";
            isPaused = true; // Устанавливаем флаг паузы
        }

        private void button3_Click(object sender, EventArgs e)
        {
            StopPlayback();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                int selectedIndex = listBox1.SelectedIndex;
                listBox1.Items.RemoveAt(selectedIndex);

                // Если удалена текущая песня, остановить и очистить воспроизведение
                if (outputDevice != null && selectedIndex == listBox1.SelectedIndex)
                {
                    outputDevice.Stop();
                    outputDevice.Dispose();
                    audioFile.Dispose();

                    label1.Text = "";
                    label2.Text = "";
                    label4.Text = "00:00:00";
                    label5.Text = "00:00:00";
                    trackBar1.Value = 0;
                    label6.Text = "◼";
                    button1.Enabled = true;
                }
            }
            if (listBox2.SelectedItem != null)
            {
                int selectedIndex = listBox2.SelectedIndex;
                listBox2.Items.RemoveAt(selectedIndex);

                // Если удалена текущая песня, остановить и очистить воспроизведение
                if (outputDevice != null && selectedIndex == listBox2.SelectedIndex)
                {
                    outputDevice.Stop();
                    outputDevice.Dispose();
                    audioFile.Dispose();

                    label1.Text = "";
                    label2.Text = "";
                    label4.Text = "00:00:00";
                    label5.Text = "00:00:00";
                    trackBar1.Value = 0;
                    label6.Text = "◼";
                    button1.Enabled = true;
                }
            }
        }

        private void DisplayAlbumArt(string filePath)
        {
            var file = TagLib.File.Create(filePath);

            if (file.Tag.Pictures != null && file.Tag.Pictures.Length > 0)
            {
                var coverArt = file.Tag.Pictures[0]; // Получение первой обложки

                using (var stream = new MemoryStream(coverArt.Data.Data))
                {
                    // Отображение обложки в PictureBox
                    pictureBox1.Image = Image.FromStream(stream);
                    pictureBox2.Image = pictureBox1.Image;
                    musicline();
                }
            }
            else
            {
                // Если обложка альбома отсутствует, вы можете отобразить заглушку или другое изображение по умолчанию.
                // Например:
                pictureBox1.Image = Properties.Resources.DisplayAlbumArt; // Заглушка по умолчанию
                pictureBox2.Image = pictureBox1.Image;
                musicline();
            }

            pictureBox1.Refresh(); // Перерисовка pictureBox1
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex >= 0 && listBox1.SelectedIndex < listBox2.Items.Count)
            {
                // Установка выбранного индекса в ListBox2
                listBox2.SelectedIndex = listBox1.SelectedIndex;
            }
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndex >= 0 && listBox2.SelectedIndex < listBox2.Items.Count)
            {
                // Получение выбранной песни из ListBox2
                string selectedFile = listBox1.Items[listBox2.SelectedIndex].ToString();

                // Установка выбранного индекса в ListBox1
                listBox1.SelectedIndex = listBox2.SelectedIndex;

                // Извлечение метаданных
                var file = TagLib.File.Create(selectedFile);
                string artist = file.Tag.FirstPerformer;
                string title = file.Tag.Title;

                // Обновление заголовка окна плеера
                string windowTitle;
                if (!string.IsNullOrEmpty(artist) && !string.IsNullOrEmpty(title))
                {
                    windowTitle = $"MayPlayer | {artist} - {title}";
                }
                else
                {
                    windowTitle = $"MayPlayer | {selectedFile}";
                }
                this.Text = windowTitle;

                // Обновление меток с распознанными метаданными
                if (!string.IsNullOrEmpty(artist) && !string.IsNullOrEmpty(title))
                {
                    string notificationText = $"{artist} - {title}";
                    notifyIcon1.ShowBalloonTip(1000, "Сейчас играет: ", notificationText, ToolTipIcon.Info);
                }
                else
                {
                    string notificationText = $"{selectedFile}";
                    notifyIcon1.ShowBalloonTip(1000, "Сейчас играет: ", notificationText, ToolTipIcon.Info);
                }
                label1.Text = !string.IsNullOrEmpty(artist) ? artist : "Неизвестный артист";
                label2.Text = !string.IsNullOrEmpty(title) ? title : "Неизвестная композиция";
                // Изменение обложки альбома
                DisplayAlbumArt(selectedFile);
                label6.Text = "Воспроизведение";

                // Воспроизведение выбранной композиции
                button1_Click(sender, e);
            }
        }

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 about = new Form2();
            about.Show();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (listBox1.Visible != true)
            {
                listBox1.Visible = true;
            }
            else
            {
                listBox1.Visible = false;
            }
        }

        private void открытьПапкуСПеснямиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Показать диалог выбора папки
            DialogResult result = folderBrowserDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                // Получить выбранную папку
                string folderPath = folderBrowserDialog1.SelectedPath;

                // Получить все аудиофайлы в выбранной папке
                string[] audioFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                    .Where(file => IsAudioFile(file))
                    .ToArray();

                // Очистить списки ListBox
                listBox1.Items.Clear();
                listBox2.Items.Clear();

                // Добавить аудиофайлы в ListBox1 и ListBox2
                foreach (string audioFile in audioFiles)
                {
                    // Извлечение метаданных
                    var file = TagLib.File.Create(audioFile);
                    string artist = file.Tag.FirstPerformer;
                    string title = file.Tag.Title;

                    // Определение отображаемого текста
                    string displayText = string.IsNullOrEmpty(artist) ? audioFile : $"{artist} - {title}";

                    // Добавление песни в ListBox1
                    listBox1.Items.Add(audioFile);

                    // Добавление названия композиции и имени исполнителя в ListBox2
                    listBox2.Items.Add(displayText);

                    // Обновление меток с распознанными метаданными
                    label1.Text = !string.IsNullOrEmpty(artist) ? artist : "Неизвестный артист";
                    label2.Text = !string.IsNullOrEmpty(title) ? title : "Неизвестная композиция";
                }

                // Если есть аудиофайлы, выбрать первый файл
                if (audioFiles.Length > 0)
                {
                    listBox1.SelectedIndex = 0;
                    listBox2.SelectedIndex = 0;
                }
            }
        }

        private bool IsAudioFile(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            return extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".flac", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".m4a", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".wma", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".aac", StringComparison.OrdinalIgnoreCase);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            // Получение выбранной песни
            string selectedFile = listBox1.SelectedItem.ToString();

            // Извлечение метаданных
            var file = TagLib.File.Create(selectedFile);
            string artist = file.Tag.FirstPerformer;
            string title = file.Tag.Title;

            // Обновление заголовка окна плеера
            string windowTitle;
            if (!string.IsNullOrEmpty(artist) && !string.IsNullOrEmpty(title))
            {
                windowTitle = $"MayPlayer | {artist} - {title}";
            }
            else
            {
                windowTitle = $"MayPlayer | {selectedFile}";
                // Обновление меток с распознанными метаданными
                label1.Text = !string.IsNullOrEmpty(artist) ? artist : "Неизвестный артист";
                label2.Text = !string.IsNullOrEmpty(title) ? title : "Неизвестная композиция";
            }
            this.Text = windowTitle;
        }

        private void button6_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            // При нажатии кнопки, переключаем режим повтора текущего трека
            isRepeatCurrentTrackEnabled = !isRepeatCurrentTrackEnabled;

            // Обновляем текст кнопки в соответствии с активностью режима повтора текущего трека
            button2.Text = isRepeatCurrentTrackEnabled ? "*↺¹" : "↺¹";
            label12.Text = isRepeatCurrentTrackEnabled ? "Повтор: Один трек" : "Повтор: Нет";
            button6.Enabled = isRepeatCurrentTrackEnabled ? false : true;
            button7.Enabled = isRepeatCurrentTrackEnabled ? false : true;
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            // При нажатии кнопки, переключаем между режимами повтора
            isShuffleEnabled = false;
            isRepeatCircularEnabled = !isRepeatCircularEnabled;
            button6.Text = isRepeatCircularEnabled ? "*↺" : "↺";
            label12.Text = isRepeatCircularEnabled ? "Повтор: По кругу" : "Повтор: Нет";
            button2.Enabled = isRepeatCircularEnabled ? false : true;
            button7.Enabled = isRepeatCircularEnabled ? false : true;
        }

        private void button7_Click_1(object sender, EventArgs e)
        {
            isRandomPlaybackEnabled = !isRandomPlaybackEnabled;
            button7.Text = isRandomPlaybackEnabled ? "*⇋" : "⇋";
            label12.Text = isRandomPlaybackEnabled ? "Повтор: В разброс" : "Повтор: Нет";
            button2.Enabled = isRandomPlaybackEnabled ? false : true;
            button6.Enabled = isRandomPlaybackEnabled ? false : true;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (outputDevice != null)
            {
                if (outputDevice.Volume > 0f) // Если звук включен (громкость больше 0)
                {
                    // Сохраняем текущее значение громкости перед выключением звука
                    previousVolume = outputDevice.Volume;

                    // Выключаем звук (устанавливаем громкость на минимум)
                    outputDevice.Volume = 0f;

                    // Обновляем trackBar1, чтобы он отображал, что звук выключен (на минимуме)
                    trackBar2.Value = 0;
                    trackBar2.Enabled = false;
                    groupBox1.Text = "Громкость: ---%";
                    button10.Text = "X";
                }
                else // Если звук уже выключен, то восстанавливаем предыдущее значение громкости
                {
                    // Восстанавливаем предыдущее значение громкости
                    outputDevice.Volume = previousVolume;

                    // Обновляем trackBar1, чтобы он отображал текущее значение громкости
                    trackBar2.Value = (int)(outputDevice.Volume * trackBar2.Maximum);
                    trackBar2.Enabled = true;
                    groupBox1.Text = $"Громкость: {trackBar2.Value}%";
                    button10.Text = "♫";
                }
            }
        }



        //settings.skins----------------------------------------------------------------------------//
        private void LoadThemeSettings(string themeFilePath)
        {
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(themeFilePath);

            // Применяем настройки к Form1
            ApplyThemeSettings(data);
        }
        private void ApplyThemeSettings(IniData data)
        {
            // Применение настроек для формы
            string formBackgroundColor = data["FormSettings"]["BackgroundColor"];
            string formForeColor = data["FormSettings"]["ForeColor"];
            this.BackColor = ColorTranslator.FromHtml(formBackgroundColor);
            this.ForeColor = ColorTranslator.FromHtml(formForeColor);
            tabPage1.BackColor = ColorTranslator.FromHtml(formBackgroundColor);
            tabPage1.ForeColor = ColorTranslator.FromHtml(formForeColor);
            trackBar1.BackColor = ColorTranslator.FromHtml(formBackgroundColor);
            trackBar2.BackColor = ColorTranslator.FromHtml(formBackgroundColor);
            listBox1.BackColor = ColorTranslator.FromHtml(formBackgroundColor);
            listBox2.BackColor = ColorTranslator.FromHtml(formBackgroundColor);
            listBox1.ForeColor = ColorTranslator.FromHtml(formForeColor);
            listBox2.ForeColor = ColorTranslator.FromHtml(formForeColor);
            toolStrip1.BackColor = ColorTranslator.FromHtml(formBackgroundColor);
            toolStrip1.ForeColor = ColorTranslator.FromHtml(formForeColor);

            // Применение настроек для лейблов
            string labelForeColor = data["LabelSettings"]["ForeColor"];
            label1.ForeColor = ColorTranslator.FromHtml(labelForeColor);
            label2.ForeColor = ColorTranslator.FromHtml(labelForeColor);
            label4.ForeColor = ColorTranslator.FromHtml(labelForeColor);
            label5.ForeColor = ColorTranslator.FromHtml(labelForeColor);
            label6.ForeColor = ColorTranslator.FromHtml(labelForeColor);
            label12.ForeColor = ColorTranslator.FromHtml(labelForeColor);
            groupBox1.ForeColor = ColorTranslator.FromHtml(labelForeColor);

            // Применение настроек для кнопок
            string buttonForeColor = data["ButtonSettings"]["ForeColor"];
            button1.ForeColor = ColorTranslator.FromHtml(buttonForeColor);
            button2.ForeColor = ColorTranslator.FromHtml(buttonForeColor);
            button3.ForeColor = ColorTranslator.FromHtml(buttonForeColor);
            button4.ForeColor = ColorTranslator.FromHtml(buttonForeColor);
            button5.ForeColor = ColorTranslator.FromHtml(buttonForeColor);
            button6.ForeColor = ColorTranslator.FromHtml(buttonForeColor);
            button7.ForeColor = ColorTranslator.FromHtml(buttonForeColor);
            button10.ForeColor = ColorTranslator.FromHtml(buttonForeColor);

            // Применение настроек для фонов лейблов и кнопок
            label1.BackColor = Color.Transparent;
            label2.BackColor = Color.Transparent;
            label4.BackColor = Color.Transparent;
            label5.BackColor = Color.Transparent;
            label6.BackColor = Color.Transparent;
            label12.BackColor = Color.Transparent;

            // Применение настроек для фон кнопок
            string buttonBackColor = data["ButtonSettings"]["BackColor"];
            button1.BackColor = ColorTranslator.FromHtml(buttonBackColor);
            button2.BackColor = ColorTranslator.FromHtml(buttonBackColor);
            button3.BackColor = ColorTranslator.FromHtml(buttonBackColor);
            button4.BackColor = ColorTranslator.FromHtml(buttonBackColor);
            button5.BackColor = ColorTranslator.FromHtml(buttonBackColor);
            button6.BackColor = ColorTranslator.FromHtml(buttonBackColor);
            button7.BackColor = ColorTranslator.FromHtml(buttonBackColor);
            button8.BackColor = ColorTranslator.FromHtml(buttonBackColor);
            button10.BackColor = ColorTranslator.FromHtml(buttonBackColor);

            // Применение настроек меню
            string toolstback = data["MenuSettings"]["ForeColor"];
            menuStrip1.ForeColor = ColorTranslator.FromHtml(toolstback);

            // Применяем настройки шрифтов для окна
            string windowFontSettings = data["WindowSettings"]["WindowFont"];
            if (!string.IsNullOrEmpty(windowFontSettings))
            {
                Font windowFont = ParseFontString(windowFontSettings);
                this.Font = windowFont;
            }

            // Применяем настройки шрифтов для текста
            string textFontSettings = data["TextSettings"]["WindowFont"];
            if (!string.IsNullOrEmpty(textFontSettings))
            {
                Font windowFont = ParseFontString(textFontSettings);
                label1.Font = windowFont;
            }

            // Применяем настройки шрифтов для текста
            string textFontSettings3 = data["OtherTextSettings"]["WindowFont"];
            if (!string.IsNullOrEmpty(textFontSettings3))
            {
                Font windowFont = ParseFontString(textFontSettings3);
                label4.Font = windowFont;
                label5.Font = windowFont;
                label12.Font = windowFont;
            }

            string textFontSettings4 = data["ItalicTextSettings"]["WindowFont"];
            if (!string.IsNullOrEmpty(textFontSettings4))
            {
                Font windowFont = ParseFontString(textFontSettings4);
                label2.Font = windowFont;
            }

        }





        private Font ParseFontString(string fontString)
        {
            string[] fontProperties = fontString.Split(',');
            if (fontProperties.Length >= 3)
            {
                string fontFamily = fontProperties[0].Trim();
                float fontSize = float.Parse(fontProperties[1].Trim());
                FontStyle fontStyle = (FontStyle)Enum.Parse(typeof(FontStyle), fontProperties[2].Trim(), true);

                // Создаем объект шрифта на основе полученных данных
                FontFamily fontFamilyObj = new FontFamily(fontFamily);
                Font font = new Font(fontFamilyObj, fontSize, fontStyle);

                return font;
            }

            // Если не удалось разобрать строку шрифта, используем шрифт по умолчанию
            return this.Font;
        }


        // В Form1 добавьте метод ApplySkin, который применит выбранный скин
        public void ApplySkin(string themeFilePath)
        {
            LoadThemeSettings(themeFilePath);
        }

        private void другиеНастройкиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form3 settings = new Form3(this);
            this.AddOwnedForm(settings);
            settings.ShowDialog();
        }

        private void LoadSelectedTheme()
        {
            // Загрузка настроек из файла settings.ini
            LoadSettings();

            // Применение настроек к форме
            ApplySettings();
        }
        public void LoadSettings()
        {
            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MayPlayer");
            string settingsFile = Path.Combine(appDataFolder, "settings.ini");

            // Создаем папку, если она не существует
            Directory.CreateDirectory(appDataFolder);

            if (System.IO.File.Exists(settingsFile))
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

                // Записываем настройки обратно в файл settings.ini
                iniParser.WriteFile(settingsFile, settingsData);
            }
        }

        public void ApplySettings()
        {
            if (settingsData != null)
            {
                // Применяем стили для формы и её элементов из settingsData
                string themeFilePath = settingsData["App"]["Skin"];
                if (!string.IsNullOrEmpty(themeFilePath))
                {
                    LoadThemeSettings(themeFilePath);
                }
                SaveSettings();
            }
        }

        public void SaveSettings()
        {
            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MayPlayer");
            string settingsFile = Path.Combine(appDataFolder, "settings.ini");

            // Записываем настройки обратно в файл settings.ini
            iniParser.WriteFile(settingsFile, settingsData);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Если плеер находится в режиме "Трей", скрываем окно, а не закрываем приложение
            if (currentMode == PlayerMode.Tray)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void скрытьВТрейToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Устанавливаем режим плеера в "Трей"
            currentMode = PlayerMode.Tray;

            // Скрываем окно формы
            this.Hide();
        }

        private void TrayIcon_Click(object sender, EventArgs e)
        {
            // Если кликнули по иконке в трее и плеер находится в режиме "Трей", показываем окно
            if (currentMode == PlayerMode.Tray)
            {
                this.Show();
                currentMode = PlayerMode.Player; // Восстанавливаем режим плеера
            }
        }

        private void НоваяПесняToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Здесь ваш код для загрузки и воспроизведения новой песни

            // Отправляем уведомление о текущей композиции
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Убеждаемся, что иконка в трее удаляется при закрытии приложения
            trayIcon.Dispose();
        }

        private void показатьОкноToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        private void выходИзПриложенияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void остановитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StopPlayback();
        }

        private void contextMenuStrip2_Opening(object sender, CancelEventArgs e)
        {

        }

        private void паузаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            outputDevice.Pause();
        }

        private void восToolStripMenuItem_Click(object sender, EventArgs e)
        {
            outputDevice.Play();
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void timer2_Tick_1(object sender, EventArgs e)
        {

        }

        private void musicline()
        {
            label7.Text = label1.Text;
            label8.Text = label2.Text;
            label9.Text = label12.Text;
            pictureBox2.Image = pictureBox1.Image;
            ApplyBackgroundBlur();
        }

        private void загрузитьПесниВПлейлистToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "M3U Playlist Files|*.m3u";
            openFileDialog1.Title = "Выберите M3U плейлист";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string m3uFilePath = openFileDialog1.FileName;
                string m3uContent = System.IO.File.ReadAllText(m3uFilePath);

                // Очищаем listBox1 и listBox2 перед загрузкой новых песен
                listBox1.Items.Clear();
                listBox2.Items.Clear();

                // Построчно обрабатываем содержимое m3u плейлиста
                string[] lines = m3uContent.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    // Проверяем, что строка не содержит недопустимые символы
                    if (IsValidFilePath(line))
                    {
                        // Добавляем путь к аудиофайлу в listBox1
                        listBox1.Items.Add(line);

                        // Загружаем метаданные и добавляем информацию о песне в listBox2
                        LoadMetadataAndAddToListBox(line);
                    }
                    else
                    {
                        // Игнорируем строку с недопустимыми символами
                        MessageBox.Show($"Пропущена строка с недопустимыми символами: {line}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                MessageBox.Show("Песни успешно загружены в плейлист.");
            }
        }

        private void LoadMetadataAndAddToListBox(string filePath)
        {
            // Извлечение метаданных
            var file = TagLib.File.Create(filePath);
            string artist = file.Tag.FirstPerformer;
            string title = file.Tag.Title;

            // Добавление информации о песне в listBox2
            string songInfo;
            if (!string.IsNullOrEmpty(title))
            {
                songInfo = title;
            }
            else
            {
                songInfo = Path.GetFileNameWithoutExtension(filePath);
            }
            listBox2.Items.Add($"{artist} - {songInfo}");
        }

        private bool IsValidFilePath(string filePath)
        {
            // Проверяем, что строка не содержит недопустимые символы для пути к файлу
            char[] invalidPathChars = Path.GetInvalidPathChars();
            return !filePath.Any(c => invalidPathChars.Contains(c));
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (panel2.Visible == true)
            {
                panel2.Visible = false;
                button8.Text = "︽";
            }
            else if (panel2.Visible == false)
            {
                panel2.Visible = true;
                button8.Text = "︾";
            }
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Form1_FormClosing_1(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        private void создатьПлейлистToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listBox1.Items.Count == 0)
            {
                MessageBox.Show("Плейлист пуст. Добавьте песни перед созданием плейлиста.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Открываем диалоговое окно для сохранения файла
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "M3U Playlist Files|*.m3u";
            saveFileDialog.Title = "Сохранить M3U плейлист";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string playlistFilePath = saveFileDialog.FileName;

                // Создаем или перезаписываем файл M3U
                using (StreamWriter writer = new StreamWriter(playlistFilePath))
                {
                    // Записываем пути к файлам из listBox1 в файл M3U
                    foreach (var item in listBox1.Items)
                    {
                        writer.WriteLine(item.ToString());
                    }
                }

                MessageBox.Show("Плейлист успешно сохранен.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (string path in files)
            {
                if (Directory.Exists(path))
                {
                    ProcessDirectory(path);
                }
                else
                {
                    ProcessFile(path);
                }
            }
            //MessageBox.Show("Файлы успешно загружены в плейлист, выберите трек и нажмите на ▶️ чтобы начать воспроизведение", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void ProcessFile(string filePath)
        {
            string fileExtension = Path.GetExtension(filePath).ToLower();
            if (IsMusicExtension(fileExtension))
            {
                listBox1.Items.Add(filePath);

                //
                var file = TagLib.File.Create(filePath);
                string artist = file.Tag.FirstPerformer;
                string title = file.Tag.Title;
                string displayText = string.IsNullOrEmpty(artist) ? filePath : $"{artist} - {title}";

                listBox2.Items.Add(displayText);
                listBox2.SelectedIndex = 0;
            }
        }

        private void ProcessDirectory(string directoryPath)
        {
            string[] files = Directory.GetFiles(directoryPath);
            foreach (string file in files)
            {
                ProcessFile(file);
            }

            string[] subdirectories = Directory.GetDirectories(directoryPath);
            foreach (string subdirectory in subdirectories)
            {
                ProcessDirectory(subdirectory);
            }
        }

        private bool IsMusicExtension(string fileExtension)
        {
            string[] musicExtensions = { ".mp3", ".wav", ".flac", ".ogg", ".m4a", ".wma", ".aac" }; // Добавьте другие расширения, если необходимо

            return musicExtensions.Contains(fileExtension);
        }

        public void select()
        {
            listBox1.SelectedIndex = 0;
            listBox2.SelectedIndex = 0;
            PlayM();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }
    }
}
