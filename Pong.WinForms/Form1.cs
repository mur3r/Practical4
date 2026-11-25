using System;
using System.Drawing;
using System.Windows.Forms;
using Pong.Core;

namespace Pong.WinForms
{
    public partial class Form1 : Form
    {
        private readonly FormController controller;

        public FormController Controller { get { return controller; } }

        public Form1() : this(null)
        {
        }

        // цей конструктор також дозволяє передати підготовлену модель у тестах
        public Form1(PongGame game)
        {
            InitializeComponent();
            controller = new FormController(this, game);
            gameTimer.Start();
        }

        // оновлюємо елементи інтерфейсу й запитуємо перемальовування поля
        public void ShowState(string pauseText, bool canPause)
        {
            pauseButton.Text = pauseText;
            pauseButton.Enabled = canPause;
            gameCanvas.Invalidate();
        }

        // повертаємо фокус полю, щоб клавіші одразу керували ракеткою
        public void FocusGame()
        {
            gameCanvas.Focus();
        }

        // обробники форми лише передають події контролеру
        private void GameTimer_Tick(object sender, EventArgs e)
        {
            controller.Update();
        }

        private void GameCanvas_Paint(object sender, PaintEventArgs e)
        {
            controller.Draw(e.Graphics, gameCanvas.ClientSize);
        }

        private void PauseButton_Click(object sender, EventArgs e)
        {
            controller.TogglePause();
        }

        private void RestartButton_Click(object sender, EventArgs e)
        {
            controller.Restart();
        }

        private void StrategyBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            controller.SelectStrategy(strategyBox.SelectedIndex);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            controller.KeyDown(e);
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            controller.KeyUp(e);
        }

        private void Form1_Deactivate(object sender, EventArgs e)
        {
            controller.LoseFocus();
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        // інші службові клавіші обробляє стандартний механізм форми
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            return controller.ProcessArrow(keyData) || base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
