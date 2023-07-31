using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace MayPlayer
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
            timer1.Start();
        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://maydilsiel.github.io");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://youtube.com/@Maydilsiel");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Maydilsiel");   
        }

        private void button6_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://t.me/maydilsiel");
        }

        private void button5_Click(object sender, EventArgs e)
        { 
            System.Diagnostics.Process.Start("https://mastodon.ml/@Maydilsiel");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://openvk.su/maydilsiel");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // Получаем текущее время
            DateTime currentTime = DateTime.Now;

            // Указываем время начала и окончания периода для пожелания
            DateTime startTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 6, 0, 0);
            DateTime endTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 22, 0, 0);

            // Если текущее время находится между 6:00 и 22:00, пожелание "Хорошо провести время"
            // В противном случае, пожелание "Доброй ночи"
            string wish;
            if (currentTime >= startTime && currentTime <= endTime)
            {
                wish = "Желаем вам хорошо провести время!";
            }
            else
            {
                wish = "Доброй ночи!";
            }

            // Выводим пожелание в Label11
            label11.Text = wish;
        }
    }
}
