namespace Pong.WinForms
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel layout;
        private System.Windows.Forms.FlowLayoutPanel toolbar;
        private System.Windows.Forms.Button pauseButton;
        private System.Windows.Forms.Button restartButton;
        private System.Windows.Forms.Button exitButton;
        private System.Windows.Forms.Label strategyLabel;
        private System.Windows.Forms.ComboBox strategyBox;
        private System.Windows.Forms.Label helpLabel;
        private GameCanvas gameCanvas;
        private System.Windows.Forms.Timer gameTimer;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        // створюємо постійні елементи інтерфейсу та підключаємо їхні події
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            layout = new System.Windows.Forms.TableLayoutPanel();
            toolbar = new System.Windows.Forms.FlowLayoutPanel();
            pauseButton = new System.Windows.Forms.Button();
            restartButton = new System.Windows.Forms.Button();
            exitButton = new System.Windows.Forms.Button();
            strategyLabel = new System.Windows.Forms.Label();
            strategyBox = new System.Windows.Forms.ComboBox();
            helpLabel = new System.Windows.Forms.Label();
            gameCanvas = new GameCanvas();
            gameTimer = new System.Windows.Forms.Timer(components);
            SuspendLayout();
            layout.SuspendLayout();
            toolbar.SuspendLayout();

            layout.Dock = System.Windows.Forms.DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 3;
            // кнопки й підказка мають сталу висоту, поле займає решту місця
            layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60));
            layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100));
            layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42));
            layout.Controls.Add(toolbar, 0, 0);
            layout.Controls.Add(gameCanvas, 0, 1);
            layout.Controls.Add(helpLabel, 0, 2);

            toolbar.Dock = System.Windows.Forms.DockStyle.Fill;
            toolbar.Padding = new System.Windows.Forms.Padding(12, 10, 0, 0);
            toolbar.WrapContents = false;
            toolbar.Controls.Add(pauseButton);
            toolbar.Controls.Add(restartButton);
            toolbar.Controls.Add(strategyLabel);
            toolbar.Controls.Add(strategyBox);
            toolbar.Controls.Add(exitButton);

            pauseButton.Text = "Грати";
            pauseButton.Name = "pauseButton";
            pauseButton.Size = new System.Drawing.Size(115, 32);
            pauseButton.UseVisualStyleBackColor = true;
            pauseButton.Click += PauseButton_Click;
            restartButton.Text = "Нова гра";
            restartButton.Name = "restartButton";
            restartButton.Size = new System.Drawing.Size(115, 32);
            restartButton.UseVisualStyleBackColor = true;
            restartButton.Click += RestartButton_Click;

            strategyLabel.Text = "Стратегія:";
            strategyLabel.AutoSize = true;
            strategyLabel.Margin = new System.Windows.Forms.Padding(18, 8, 6, 0);
            strategyBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            strategyBox.Name = "strategyBox";
            strategyBox.Size = new System.Drawing.Size(200, 32);
            strategyBox.Margin = new System.Windows.Forms.Padding(3, 4, 12, 3);
            strategyBox.Items.AddRange(new object[] { "Переслідування м'яча", "Захисна гра" });
            strategyBox.SelectedIndex = 0;
            strategyBox.SelectedIndexChanged += StrategyBox_SelectedIndexChanged;

            exitButton.Text = "Вийти";
            exitButton.Name = "exitButton";
            exitButton.Size = new System.Drawing.Size(90, 32);
            exitButton.UseVisualStyleBackColor = true;
            exitButton.Click += ExitButton_Click;
            gameCanvas.Dock = System.Windows.Forms.DockStyle.Fill;
            gameCanvas.Margin = new System.Windows.Forms.Padding(0);
            gameCanvas.Paint += GameCanvas_Paint;

            helpLabel.Text = "W / S або ↑ / ↓ — рух     Пробіл / P — пауза     R — нова гра";
            helpLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            helpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            helpLabel.ForeColor = System.Drawing.Color.FromArgb(75, 85, 105);
            gameTimer.Interval = 16;
            gameTimer.Tick += GameTimer_Tick;

            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Font = new System.Drawing.Font("Segoe UI", 9F);
            BackColor = System.Drawing.Color.FromArgb(242, 245, 249);
            ClientSize = new System.Drawing.Size(920, 710);
            MinimumSize = new System.Drawing.Size(820, 650);
            // форма отримує клавіші раніше за вкладені елементи керування
            KeyPreview = true;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Pong — практична робота №4";
            Controls.Add(layout);
            KeyDown += Form1_KeyDown;
            KeyUp += Form1_KeyUp;
            Deactivate += Form1_Deactivate;
            toolbar.ResumeLayout(false);
            toolbar.PerformLayout();
            layout.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
