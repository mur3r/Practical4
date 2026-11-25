using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pong.Core;
using Pong.Core.Strategies;
using Pong.WinForms;

namespace Pong.Tests
{
    [TestClass]
    [TestCategory("ComponentIntegration")]
    public class FormTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        // події форми мають правильно запускати гру та перемикати паузу
        public void Keyboard_StartsPausesAndDoesNotRepeatCommands()
        {
            OnSta(() =>
            {
                using (TestForm form = new TestForm())
                {
                    FormController controller = form.Controller;
                    Assert.IsFalse(controller.HasStarted);
                    Assert.IsTrue(controller.Game.IsPaused);
                    controller.Advance(0.1);
                    Assert.AreEqual(394.0, controller.Game.Ball.X);
                    form.Press(Keys.Space);
                    form.Press(Keys.Space);
                    Assert.IsFalse(controller.Game.IsPaused);
                    form.Release(Keys.Space);
                    form.Press(Keys.P);
                    Assert.IsTrue(controller.Game.IsPaused);
                    form.Release(Keys.P);
                    form.Press(Keys.P);
                    Assert.IsFalse(controller.Game.IsPaused);
                    form.Release(Keys.P);
                    form.Press(Keys.S);
                    form.LeaveWindow();
                    Assert.IsTrue(controller.Game.IsPaused);
                    controller.TogglePause();
                    controller.Advance(0.02);
                    Assert.AreEqual(185.0, controller.Game.Player.Y);
                    form.Press(Keys.Escape);
                    Assert.IsTrue(controller.Game.IsPaused);
                    form.Press(Keys.A);
                    form.Press(Keys.R);
                    Assert.IsFalse(controller.Game.IsPaused);
                    Assert.AreEqual(0, controller.Game.PlayerScore);
                }
            });
        }

        [TestMethod]
        // перевіряємо утримання клавіш, накопичення часу та протилежні напрямки
        public void HeldKeys_UseElapsedTimeAndCancelOppositeDirections()
        {
            OnSta(() =>
            {
                using (TestForm form = new TestForm())
                {
                    FormController controller = form.Controller;
                    controller.Restart();
                    form.Press(Keys.S);
                    controller.Advance(0.003);
                    Assert.AreEqual(185.0, controller.Game.Player.Y);
                    controller.Advance(0.007);
                    Assert.AreEqual(188.0, controller.Game.Player.Y);
                    form.Press(Keys.W);
                    controller.Advance(0.02);
                    Assert.AreEqual(188.0, controller.Game.Player.Y);
                    form.Release(Keys.S);
                    Assert.IsTrue(form.Arrow(Keys.Up));
                    form.Release(Keys.W);
                    controller.Advance(0.01);
                    Assert.AreEqual(185.0, controller.Game.Player.Y);
                    form.Release(Keys.Up);
                    Assert.IsTrue(form.Arrow(Keys.Down));
                    controller.Advance(10);
                    Assert.AreEqual(215.0, controller.Game.Player.Y);
                    form.Release(Keys.Down);
                    Assert.IsFalse(form.Arrow(Keys.A));
                    controller.Update();
                    Assert.ThrowsException<ArgumentOutOfRangeException>(() => controller.Advance(-1));
                    Assert.ThrowsException<ArgumentOutOfRangeException>(() => controller.Advance(double.NaN));
                    Assert.ThrowsException<ArgumentOutOfRangeException>(() => controller.Advance(double.PositiveInfinity));
                    Assert.ThrowsException<ArgumentNullException>(() => new FormController(null));
                }
            });
        }

        [TestMethod]
        // викликаємо кнопки справжньої форми та перевіряємо зміни моделі
        public void FormButtons_ChangeStrategyPauseRestartAndExit()
        {
            OnSta(() =>
            {
                using (TestForm form = new TestForm())
                {
                    form.Show();
                    Button pause = (Button)form.Controls.Find("pauseButton", true)[0];
                    Button restart = (Button)form.Controls.Find("restartButton", true)[0];
                    ComboBox strategy = (ComboBox)form.Controls.Find("strategyBox", true)[0];
                    pause.PerformClick();
                    Assert.IsTrue(form.Controller.HasStarted);
                    Assert.IsFalse(form.Controller.Game.IsPaused);
                    form.Controller.Advance(0.1);
                    double x = form.Controller.Game.Ball.X;
                    strategy.SelectedIndex = 1;
                    Assert.IsInstanceOfType(form.Controller.Game.AiStrategy, typeof(DefensiveStrategy));
                    Assert.AreEqual(x, form.Controller.Game.Ball.X);
                    strategy.SelectedIndex = 0;
                    Assert.IsInstanceOfType(form.Controller.Game.AiStrategy, typeof(FollowBallStrategy));
                    Assert.ThrowsException<ArgumentOutOfRangeException>(() => form.Controller.SelectStrategy(2));
                    pause.PerformClick();
                    Assert.IsTrue(form.Controller.Game.IsPaused);
                    restart.PerformClick();
                    Assert.IsFalse(form.Controller.Game.IsPaused);
                    Assert.AreEqual(394.0, form.Controller.Game.Ball.X);
                    ((Button)form.Controls.Find("exitButton", true)[0]).PerformClick();
                    Assert.IsTrue(form.IsDisposed);
                }
            });
        }

        [TestMethod]
        // перемальовування та масштабування не повинні змінювати стан матчу
        public void Graphics_DrawRealFormAtSeveralSizesAndPreserveModel()
        {
            OnSta(() =>
            {
                using (TestForm form = new TestForm())
                {
                    form.CreateControl();
                    SaveForm(form, "01_start");
                    form.Controller.Restart();
                    form.Controller.Advance(0.1);
                    SaveForm(form, "02_game");
                    double x = form.Controller.Game.Ball.X;
                    form.Controller.TogglePause();
                    SaveForm(form, "03_pause");
                    form.ClientSize = new Size(804, 611);
                    SaveForm(form, "06_small_window");
                    form.ClientSize = new Size(1280, 850);
                    SaveForm(form, "07_large_window");
                    form.Scale(new SizeF(1.25f, 1.25f));
                    SaveForm(form, "08_scaled_125");
                    Assert.AreEqual(x, form.Controller.Game.Ball.X);
                    using (Bitmap bitmap = new Bitmap(100, 100))
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        GameView.Draw(graphics, Size.Empty, form.Controller.Game, true);
                    }
                }
            });
        }

        [TestMethod]
        // відтворюємо завершені матчі та зберігаємо зображення обох результатів
        public void VisualController_CanFinishWinAndLossAndRestart()
        {
            OnSta(() =>
            {
                using (TestForm form = new TestForm())
                {
                    form.CreateControl();
                    FormController controller = form.Controller;
                    controller.Restart();
                    PlayMatch(controller, true);
                    Assert.IsTrue(controller.Game.PlayerWon);
                    Assert.AreEqual(GameRules.WinningScore, controller.Game.PlayerScore);
                    SaveForm(form, "04_victory");
                    double x = controller.Game.Ball.X;
                    controller.TogglePause();
                    controller.Advance(0.1);
                    Assert.AreEqual(x, controller.Game.Ball.X);
                    controller.Restart();
                    PlayMatch(controller, false);
                    Assert.IsTrue(controller.Game.IsFinished);
                    Assert.IsFalse(controller.Game.PlayerWon);
                    SaveForm(form, "05_defeat");
                    controller.Restart();
                    Assert.IsFalse(controller.Game.IsFinished);
                    Assert.AreEqual(0, controller.Game.PlayerScore + controller.Game.ComputerScore);
                }
            });
        }

        private static void PlayMatch(FormController controller, bool followBall)
        {
            IAiStrategy playerAi = new FollowBallStrategy(new Random(1001));
            for (int i = 0; i < 40000 && !controller.Game.IsFinished; i++)
            {
                controller.KeyUp(new KeyEventArgs(Keys.W));
                controller.KeyUp(new KeyEventArgs(Keys.S));
                int direction = -1;
                if (followBall)
                {
                    Paddle paddle = new Paddle(758, controller.Game.Player.Y);
                    Ball ball = controller.Game.Ball;
                    double x = Math.Max(0, Math.Min(788, 788 - ball.X));
                    Ball mirrored = new Ball(x, ball.Y, -ball.SpeedX, ball.SpeedY);
                    direction = playerAi.GetDirection(paddle, mirrored);
                }
                if (direction < 0) controller.KeyDown(new KeyEventArgs(Keys.W));
                else if (direction > 0) controller.KeyDown(new KeyEventArgs(Keys.S));
                controller.Advance(0.01);
            }
            Assert.IsTrue(controller.Game.IsFinished, "матч має завершитися за обмежений час");
        }

        private void SaveForm(Form1 form, string name)
        {
            if (!form.Visible) form.Show();
            form.PerformLayout();
            form.Refresh();
            string folder = Path.Combine(TestContext.TestRunDirectory, "Screenshots");
            Directory.CreateDirectory(folder);
            using (Bitmap bitmap = new Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                string path = Path.Combine(folder, name + ".png");
                bitmap.Save(path, ImageFormat.Png);
                TestContext.AddResultFile(path);
            }
        }

        private static void OnSta(Action action)
        {
            Exception error = null;
            Thread thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception exception) { error = exception; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.IsTrue(thread.Join(30000), "перевірка форми має завершитися за 30 секунд");
            if (error != null) throw error;
        }

        private class TestForm : Form1
        {
            public TestForm() : base(new PongGame(new FollowBallStrategy(new Random(101)),
                random: new Random(1))) { }

            public void Press(Keys key) { OnKeyDown(new KeyEventArgs(key)); }
            public void Release(Keys key) { OnKeyUp(new KeyEventArgs(key)); }
            public void LeaveWindow() { OnDeactivate(EventArgs.Empty); }
            public bool Arrow(Keys key)
            {
                Message message = new Message();
                return ProcessCmdKey(ref message, key);
            }
        }
    }
}
