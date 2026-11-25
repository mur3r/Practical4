using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Pong.Core;
using Pong.Core.Commands;
using Pong.Core.Strategies;

namespace Pong.WinForms
{
    public class GameCanvas : Panel
    {
        public GameCanvas()
        {
            // подвійна буферизація прибирає мерехтіння під час анімації
            DoubleBuffered = true;
            ResizeRedraw = true;
            TabStop = true;
        }
    }

    // відображення читає стан моделі та малює його без зміни координат
    public static class GameView
    {
        public static void Draw(Graphics graphics, Size size, PongGame game, bool hasStarted)
        {
            if (size.Width <= 0 || size.Height <= 0) return;
            // зберігаємо налаштування графіки для відновлення після малювання
            GraphicsState saved = graphics.Save();
            graphics.Clear(Color.FromArgb(242, 245, 249));
            // зберігаємо пропорції поля під час зміни розміру вікна
            float scale = Math.Min(size.Width / 840f, size.Height / 600f);
            graphics.TranslateTransform((size.Width - 840 * scale) / 2,
                (size.Height - 600 * scale) / 2);
            graphics.ScaleTransform(scale, scale);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // шрифти й пензлі звільняються після завершення малювання
            using (Font title = new Font("Segoe UI", 13, FontStyle.Bold))
            using (Font score = new Font("Segoe UI", 28, FontStyle.Bold))
            using (Font small = new Font("Segoe UI", 11))
            using (Brush dark = new SolidBrush(Color.FromArgb(24, 34, 54)))
            using (Brush blue = new SolidBrush(Color.FromArgb(37, 99, 235)))
            using (Brush orange = new SolidBrush(Color.FromArgb(234, 113, 40)))
            {
                CenterText(graphics, "PONG", title, dark, 420, 0);
                CenterText(graphics, "ГРАВЕЦЬ", small, blue, 240, 24);
                CenterText(graphics, "КОМП'ЮТЕР", small, orange, 600, 24);
                CenterText(graphics, game.PlayerScore.ToString(), score, blue, 340, 32);
                CenterText(graphics, ":", score, dark, 420, 32);
                CenterText(graphics, game.ComputerScore.ToString(), score, orange, 500, 32);

                // м’яч за бічною межею не повинен малюватися поверх інтерфейсу
                GraphicsState fieldState = graphics.Save();
                graphics.SetClip(new Rectangle(20, 100, 800, 450));
                graphics.FillRectangle(dark, 20, 100, 800, 450);
                using (Pen net = new Pen(Color.FromArgb(90, 108, 135), 2))
                {
                    net.DashStyle = DashStyle.Dash;
                    graphics.DrawLine(net, 420, 110, 420, 540);
                }
                DrawPaddle(graphics, game.Player, blue);
                DrawPaddle(graphics, game.Computer, orange);
                graphics.FillEllipse(Brushes.White, 20 + (float)game.Ball.X,
                    100 + (float)game.Ball.Y, (float)Ball.Size, (float)Ball.Size);
                graphics.Restore(fieldState);
                CenterText(graphics, "Матч до " + GameRules.WinningScore + " очок  •  " +
                    game.AiStrategy.Name, small, dark, 420, 566);
            }

            // повідомлення вибираємо за поточним станом матчу
            if (!hasStarted) DrawOverlay(graphics, "PONG", "Натисни «Грати» або пробіл");
            else if (game.IsFinished) DrawOverlay(graphics,
                game.PlayerWon ? "Ти переміг!" : "Переміг комп'ютер", "R або «Нова гра» — зіграти ще раз");
            else if (game.IsPaused) DrawOverlay(graphics, "Пауза", "Пробіл або «Продовжити» — повернутися до гри");
            graphics.Restore(saved);
        }

        private static void DrawPaddle(Graphics graphics, Paddle paddle, Brush brush)
        {
            graphics.FillRectangle(brush, 20 + (float)paddle.X, 100 + (float)paddle.Y,
                (float)Paddle.Width, (float)Paddle.Height);
        }

        private static void DrawOverlay(Graphics graphics, string title, string subtitle)
        {
            using (Brush shade = new SolidBrush(Color.FromArgb(210, 24, 34, 54)))
            using (Font large = new Font("Segoe UI", 27, FontStyle.Bold))
            using (Font small = new Font("Segoe UI", 12))
            {
                graphics.FillRectangle(shade, 20, 100, 800, 450);
                CenterText(graphics, title, large, Brushes.White, 420, 265);
                CenterText(graphics, subtitle, small, Brushes.White, 420, 325);
            }
        }

        private static void CenterText(Graphics graphics, string text, Font font,
            Brush brush, float centerX, float y)
        {
            // половина ширини тексту потрібна для вирівнювання по центру
            float width = graphics.MeasureString(text, font).Width;
            graphics.DrawString(text, font, brush, centerX - width / 2, y);
        }
    }

    // поєднуємо введення з клавіатури, команди та оновлення відображення
    public class FormController
    {
        private const double Step = 0.01;
        private readonly Form1 form;
        private readonly GameController controller;
        // множина зберігає кожну затиснуту клавішу лише один раз
        private readonly HashSet<Keys> pressedKeys = new HashSet<Keys>();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private double accumulator;

        public PongGame Game { get; private set; }
        public bool HasStarted { get; private set; }

        public FormController(Form1 form, PongGame game = null)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));
            this.form = form;
            Game = game ?? new PongGame(new FollowBallStrategy());
            controller = new GameController(Game);
            if (!Game.IsPaused) controller.Execute(GameAction.Pause);
            RefreshView();
        }

        // таймер може спрацьовувати нерівномірно, тому вимірюємо реальний час
        public void Update()
        {
            double elapsed = clock.Elapsed.TotalSeconds;
            clock.Restart();
            Advance(elapsed);
        }

        public void Advance(double elapsed)
        {
            if (double.IsNaN(elapsed) || double.IsInfinity(elapsed) || elapsed < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsed));
            // під час очікування, паузи або завершення не накопичуємо час гри
            if (!HasStarted || Game.IsPaused || Game.IsFinished)
            {
                accumulator = 0;
                RefreshView();
                return;
            }

            // сталий крок робить швидкість руху незалежною від частоти таймера
            accumulator += Math.Min(elapsed, GameRules.MaxDeltaTime);
            while (accumulator + 0.000000001 >= Step)
            {
                bool up = pressedKeys.Contains(Keys.W) || pressedKeys.Contains(Keys.Up);
                bool down = pressedKeys.Contains(Keys.S) || pressedKeys.Contains(Keys.Down);
                // одночасні протилежні напрямки означають зупинку
                if (up && !down) controller.Execute(GameAction.Up);
                else if (down && !up) controller.Execute(GameAction.Down);
                else controller.Execute(GameAction.Stop);
                controller.Tick(Step);
                accumulator -= Step;
            }
            RefreshView();
        }

        public void KeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.W:
                case Keys.S:
                case Keys.Up:
                case Keys.Down:
                    pressedKeys.Add(e.KeyCode);
                    break;
                case Keys.Space:
                case Keys.P:
                    // утримання клавіші не повинно повторно перемикати паузу
                    if (pressedKeys.Add(e.KeyCode)) TogglePause();
                    break;
                case Keys.R:
                    if (pressedKeys.Add(e.KeyCode)) Restart();
                    break;
                case Keys.Escape:
                    LoseFocus();
                    break;
                default:
                    return;
            }
            e.SuppressKeyPress = true;
        }

        // після відпускання клавіша не впливає на наступні кроки руху
        public void KeyUp(KeyEventArgs e)
        {
            pressedKeys.Remove(e.KeyCode);
        }

        // перехоплюємо стрілки для гри замість переходу між кнопками
        public bool ProcessArrow(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key != Keys.Up && key != Keys.Down) return false;
            KeyDown(new KeyEventArgs(key));
            return true;
        }

        public void TogglePause()
        {
            if (Game.IsFinished) return;
            HasStarted = true;
            controller.Execute(GameAction.Pause);
            ResetClock();
            RefreshView();
            form.FocusGame();
        }

        public void Restart()
        {
            HasStarted = true;
            controller.Execute(GameAction.Restart);
            ResetClock();
            RefreshView();
            form.FocusGame();
        }

        // вибір у списку замінює поведінку бота через спільний інтерфейс
        public void SelectStrategy(int index)
        {
            if (index == 0) Game.SetStrategy(new FollowBallStrategy());
            else if (index == 1) Game.SetStrategy(new DefensiveStrategy());
            else throw new ArgumentOutOfRangeException(nameof(index));
            RefreshView();
            form.FocusGame();
        }

        public void LoseFocus()
        {
            // після перемикання вікна рух не залишається затиснутим
            pressedKeys.Clear();
            if (HasStarted && !Game.IsPaused && !Game.IsFinished)
                controller.Execute(GameAction.Pause);
            ResetClock();
            RefreshView();
        }

        public void Draw(Graphics graphics, Size size)
        {
            GameView.Draw(graphics, size, Game, HasStarted);
        }

        // очищуємо старе введення та відлік часу після паузи або перезапуску
        private void ResetClock()
        {
            pressedKeys.Remove(Keys.W);
            pressedKeys.Remove(Keys.S);
            pressedKeys.Remove(Keys.Up);
            pressedKeys.Remove(Keys.Down);
            accumulator = 0;
            clock.Restart();
        }

        // текст кнопки та доступність паузи визначаються станом матчу
        private void RefreshView()
        {
            string text = !HasStarted ? "Грати" : Game.IsPaused ? "Продовжити" : "Пауза";
            form.ShowState(text, !Game.IsFinished);
        }
    }
}
