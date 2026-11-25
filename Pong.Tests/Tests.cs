using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pong.Core;
using Pong.Core.Commands;
using Pong.Core.Strategies;

namespace Pong.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    // окремо перевіряємо сутності, фізику ударів і реалізації патернів
    public class UnitTests
    {
        [TestMethod]
        public void Paddle_MovesAtItsSpeedAndStopsAtBothBorders()
        {
            Paddle paddle = new Paddle(30, 185);
            paddle.Move(-1, 0.1);
            Assert.AreEqual(155.0, paddle.Y);
            for (int i = 0; i < 20; i++) paddle.Move(-1, 0.1);
            Assert.AreEqual(0.0, paddle.Y);
            for (int i = 0; i < 20; i++) paddle.Move(1, 0.1);
            Assert.AreEqual(370.0, paddle.Y);
            Paddle slow = new Paddle(758, 185, 120);
            slow.Move(1, 0.1);
            Assert.AreEqual(197.0, slow.Y);
        }

        [TestMethod]
        // помилкові параметри повинні відхилятися без зміни стану об’єкта
        public void InvalidValues_AreRejectedBeforeChangingState()
        {
            Paddle paddle = new Paddle(30, 185);
            foreach (double time in new[] { -0.1, 0.2, double.NaN, double.PositiveInfinity })
                Assert.ThrowsException<ArgumentOutOfRangeException>(() => paddle.Move(1, time));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => paddle.Move(2, 0.1));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Paddle(-1, 0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Paddle(0, 371));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Paddle(0, 0, 0));
            Assert.AreEqual(185.0, paddle.Y);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Ball(-1, 100, 260, 160));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Ball(100, 439, 260, 160));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Ball(100, 100, 601, 0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Ball(100, 100, 260, double.NaN));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Ball(100, 100, 500, 500));
            Assert.ThrowsException<ArgumentException>(() => new Ball(100, 100, 0, 0));
            Assert.AreEqual(444.0, new Ball(788, 438, -400, 300).CenterY);
            PongGame game = new PongGame(new TestStrategy());
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => game.SetPlayerDirection(2));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => game.Update(-0.1));
            Assert.ThrowsException<ArgumentNullException>(() => new PongGame(null));
            Assert.ThrowsException<ArgumentNullException>(() => game.SetStrategy(null));
        }

        [TestMethod]
        public void Strategies_UseTheirTargetsAndDefenceReturnsToCenter()
        {
            Paddle paddle = new Paddle(758, 185);
            IAiStrategy follow = new FollowBallStrategy(new AimRandom(0));
            IAiStrategy defence = new DefensiveStrategy(new AimRandom(0));
            Assert.AreEqual(-1, follow.GetDirection(paddle, new Ball(400, 20, -260, 0)));
            Assert.AreEqual(0, defence.GetDirection(paddle, new Ball(400, 20, -260, 0)));
            Assert.AreEqual(0, follow.GetDirection(paddle, new Ball(400, 219, 260, 0)));
            Assert.AreEqual(1, defence.GetDirection(paddle, new Ball(400, 330, 260, 0)));
            Assert.AreEqual(1, defence.GetDirection(new Paddle(758, 80), new Ball(400, 20, -260, 0)));
            Assert.AreNotEqual(follow.Name, defence.Name);
            foreach (IAiStrategy strategy in new[] { follow, defence })
            {
                Assert.ThrowsException<ArgumentNullException>(() => strategy.GetDirection(null, new Ball(100, 100, 100, 0)));
                Assert.ThrowsException<ArgumentNullException>(() => strategy.GetDirection(paddle, null));
            }
        }

        [TestMethod]
        public void Bot_SelectsContactPointOncePerIncomingFlight()
        {
            AimRandom random = new AimRandom(20);
            IAiStrategy strategy = new FollowBallStrategy(random);
            Paddle paddle = new Paddle(758, 185);
            Assert.AreEqual(-1, strategy.GetDirection(paddle, new Ball(400, 219, 260, 0)));
            strategy.GetDirection(paddle, new Ball(410, 219, 260, 0));
            Assert.AreEqual(1, random.Calls);
            strategy.GetDirection(paddle, new Ball(600, 219, -260, 0));
            strategy.GetDirection(paddle, new Ball(100, 219, 260, 0));
            Assert.AreEqual(2, random.Calls);
            strategy.GetDirection(paddle, new Ball(600, 219, 260, 0));
            strategy.GetDirection(paddle, new Ball(394, 219, 260, 0));
            Assert.AreEqual(3, random.Calls);
        }

        [TestMethod]
        public void Commands_SetMovementAndAllowAnExplicitStop()
        {
            PongGame game = new PongGame(new TestStrategy());
            new MovePaddleCommand(game, 1).Execute();
            Assert.AreEqual(185.0, game.Player.Y);
            game.Update(0.02);
            Assert.AreEqual(191.0, game.Player.Y);
            new MovePaddleCommand(game, -1).Execute();
            game.Update(0.01);
            Assert.AreEqual(188.0, game.Player.Y);
            new MovePaddleCommand(game, 0).Execute();
            game.Update(0.02);
            Assert.AreEqual(188.0, game.Player.Y);
            Assert.ThrowsException<ArgumentNullException>(() => new MovePaddleCommand(null, 1));
            Assert.ThrowsException<ArgumentNullException>(() => new PauseCommand(null));
            Assert.ThrowsException<ArgumentNullException>(() => new RestartCommand(null));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new MovePaddleCommand(game, 2));
        }

        [TestMethod]
        public void Walls_ReflectVerticalVelocityWithoutChangingHorizontalVelocity()
        {
            PongGame top = new PongGame(new TestStrategy(), new Ball(400, 1, 260, -160));
            PongGame bottom = new PongGame(new TestStrategy(), new Ball(400, 437, 260, 160));
            top.Update(0.01);
            bottom.Update(0.01);
            Assert.AreEqual(160.0, top.Ball.SpeedY);
            Assert.AreEqual(-160.0, bottom.Ball.SpeedY);
            Assert.AreEqual(0.0, top.Ball.Y);
            Assert.AreEqual(438.0, bottom.Ball.Y);
            Assert.AreEqual(260.0, top.Ball.SpeedX);
        }

        [TestMethod]
        public void PaddleContacts_ChangeAngleSymmetricallyForBothSides()
        {
            // однакова точка контакту повинна давати дзеркальні удари обох ракеток
            foreach (int offset in new[] { -40, -20, 0, 20, 40 })
            {
                PongGame left = new PongGame(new TestStrategy(), new Ball(43, 219 + offset, -400, 0));
                PongGame right = new PongGame(new TestStrategy(), new Ball(745, 219 + offset, 400, 0));
                left.Update(0.005);
                right.Update(0.005);
                Assert.IsTrue(left.Ball.SpeedX > 0 && right.Ball.SpeedX < 0);
                Assert.AreEqual(left.Ball.SpeedX, -right.Ball.SpeedX, 0.000001);
                Assert.AreEqual(left.Ball.SpeedY, right.Ball.SpeedY, 0.000001);
                Assert.AreEqual(Math.Sign(offset), Math.Sign(left.Ball.SpeedY));
                Assert.AreEqual(418.0, Speed(left.Ball), 0.000001);
                Assert.AreEqual(offset * 1.5, Math.Atan2(left.Ball.SpeedY, left.Ball.SpeedX) * 180 / Math.PI, 0.000001);
            }
        }

        [TestMethod]
        public void FastBall_DoesNotSkipPaddlesAndSpeedStaysLimited()
        {
            foreach (double speed in new[] { 580.0, 599.0, 600.0 })
            {
                PongGame left = new PongGame(new TestStrategy(), new Ball(45, 199, -speed, 0));
                PongGame right = new PongGame(new TestStrategy(), new Ball(743, 199, speed, 0));
                left.Update(0.1);
                right.Update(0.1);
                Assert.IsTrue(left.Ball.SpeedX > 0 && right.Ball.SpeedX < 0);
                Assert.AreEqual(Math.Min(600, speed + 18), Speed(left.Ball), 0.000001);
                Assert.AreEqual(Speed(left.Ball), Speed(right.Ball), 0.000001);
                new Ball(left.Ball.X, left.Ball.Y, left.Ball.SpeedX, left.Ball.SpeedY);
                Assert.AreEqual(0, left.ComputerScore + right.PlayerScore);
            }
        }

        [TestMethod]
        public void PaddleEdges_DistinguishTouchMissAndBackHit()
        {
            PongGame touch = new PongGame(new TestStrategy(), new Ball(44, 173, -400, 0));
            PongGame miss = new PongGame(new TestStrategy(), new Ball(44, 172.9, -400, 0));
            PongGame behind = new PongGame(new TestStrategy(), new Ball(32, 210, -400, 0));
            touch.Update(0.005);
            miss.Update(0.005);
            behind.Update(0.005);
            Assert.IsTrue(touch.Ball.SpeedX > 0);
            Assert.AreEqual(-400.0, miss.Ball.SpeedX);
            Assert.AreEqual(-400.0, behind.Ball.SpeedX);
            PongGame corner = new PongGame(new TestStrategy(), new Ball(43, 0, -400, -10));
            for (int i = 0; i < 20; i++) corner.Player.Move(-1, 0.1);
            corner.Update(0.005);
            Assert.AreEqual(0.0, corner.Ball.Y);
            Assert.IsTrue(corner.Ball.SpeedX > 0 && corner.Ball.SpeedY > 0);
        }

        [TestMethod]
        public void Serves_HaveDifferentAnglesAndReproduceWithTheSameSeed()
        {
            HashSet<int> directions = new HashSet<int>();
            HashSet<int> vertical = new HashSet<int>();
            HashSet<int> angles = new HashSet<int>();
            for (int seed = 0; seed < 32; seed++)
            {
                PongGame game = new PongGame(new TestStrategy(), random: new Random(seed));
                PongGame copy = new PongGame(new TestStrategy(), random: new Random(seed));
                Assert.AreEqual(game.Ball.SpeedX, copy.Ball.SpeedX);
                Assert.AreEqual(game.Ball.SpeedY, copy.Ball.SpeedY);
                Assert.AreEqual(GameRules.ServeSpeed, Speed(game.Ball), 0.000001);
                double angle = Math.Atan2(Math.Abs(game.Ball.SpeedY), Math.Abs(game.Ball.SpeedX)) * 180 / Math.PI;
                Assert.IsTrue(angle >= 20 && angle <= 40);
                directions.Add(Math.Sign(game.Ball.SpeedX));
                vertical.Add(Math.Sign(game.Ball.SpeedY));
                angles.Add((int)angle);
            }
            Assert.AreEqual(2, directions.Count);
            Assert.AreEqual(2, vertical.Count);
            Assert.IsTrue(angles.Count > 5);
        }

        internal static double Speed(Ball ball)
        {
            return Math.Sqrt(ball.SpeedX * ball.SpeedX + ball.SpeedY * ball.SpeedY);
        }
    }

    [TestClass]
    [TestCategory("ComponentIntegration")]
    // перевіряємо спільну роботу команд, моделі та стратегій
    public class IntegrationTests
    {
        [TestMethod]
        public void Controller_RebindsCommandsAndRejectsInvalidBindings()
        {
            PongGame game = new PongGame(new TestStrategy());
            GameController controller = new GameController(game);
            controller.Execute(GameAction.Up);
            controller.Tick(0.1);
            Assert.AreEqual(155.0, game.Player.Y);
            controller.Bind(GameAction.Up, new MovePaddleCommand(game, 1));
            Assert.IsTrue(controller.Execute(GameAction.Up));
            controller.Tick(0.1);
            Assert.AreEqual(185.0, game.Player.Y);
            Assert.IsFalse(controller.Execute((GameAction)99));
            Assert.ThrowsException<ArgumentNullException>(() => new GameController(null));
            Assert.ThrowsException<ArgumentNullException>(() => controller.Bind(GameAction.Down, null));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => controller.Bind((GameAction)99, new PauseCommand(game)));
        }

        [TestMethod]
        public void Pause_FreezesBothSidesAndClearsHeldMovement()
        {
            PongGame game = new PongGame(new TestStrategy { Direction = 1 });
            GameController controller = new GameController(game);
            controller.Execute(GameAction.Down);
            controller.Execute(GameAction.Pause);
            controller.Tick(0.1);
            Assert.AreEqual(394.0, game.Ball.X);
            Assert.AreEqual(185.0, game.Player.Y);
            Assert.AreEqual(185.0, game.Computer.Y);
            controller.Execute(GameAction.Pause);
            controller.Tick(0.01);
            Assert.AreEqual(185.0, game.Player.Y);
            Assert.AreEqual(188.0, game.Computer.Y);
            Assert.AreNotEqual(394.0, game.Ball.X);
        }

        [TestMethod]
        public void BothPaddles_MoveTogetherAtEqualSpeedWithoutArtificialDelay()
        {
            TestStrategy strategy = new TestStrategy { Direction = -1 };
            PongGame game = new PongGame(strategy, new Ball(400, 100, 300, 20));
            game.SetPlayerDirection(-1);
            game.Update(0);
            Assert.AreEqual(0, strategy.Calls);
            game.Update(0.01);
            Assert.AreEqual(1, strategy.Calls);
            Assert.AreSame(game.Ball, strategy.LastBall);
            Assert.AreEqual(182.0, game.Player.Y);
            Assert.AreEqual(game.Player.Y, game.Computer.Y);
            Assert.AreEqual(game.Player.MoveSpeed, game.Computer.MoveSpeed);
            double x = game.Ball.X;
            game.SetStrategy(new DefensiveStrategy());
            Assert.AreEqual(x, game.Ball.X);
        }

        [TestMethod]
        public void Goals_RequireFullExitAlternateServeAndResetBothPaddles()
        {
            PongGame left = new PongGame(new TestStrategy(), new Ball(0, 20, -400, 0));
            left.SetPlayerDirection(1);
            left.Update(0.01);
            Assert.AreEqual(0, left.ComputerScore);
            left.Update(0.1);
            Assert.AreEqual(1, left.ComputerScore);
            Assert.AreEqual(394.0, left.Ball.X);
            Assert.IsTrue(left.Ball.SpeedX > 0);
            Assert.AreEqual(185.0, left.Player.Y);
            Assert.AreEqual(185.0, left.Computer.Y);
            PongGame right = new PongGame(new TestStrategy(), new Ball(788, 20, 400, 0));
            right.Update(0.1);
            Assert.AreEqual(1, right.PlayerScore);
            Assert.IsTrue(right.Ball.SpeedX < 0);
        }

        [TestMethod]
        public void Restart_ResetsScoresPositionsAndKeepsChosenStrategy()
        {
            IAiStrategy strategy = new DefensiveStrategy();
            PongGame game = new PongGame(strategy, new Ball(0, 20, -400, 0));
            game.Update(0.1);
            Assert.AreEqual(1, game.ComputerScore);
            game.SetPlayerDirection(1);
            game.Update(0.1);
            game.TogglePause();
            new RestartCommand(game).Execute();
            Assert.AreEqual(0, game.PlayerScore + game.ComputerScore);
            Assert.AreEqual(185.0, game.Player.Y);
            Assert.AreEqual(185.0, game.Computer.Y);
            Assert.AreEqual(394.0, game.Ball.X);
            Assert.IsFalse(game.IsPaused || game.IsFinished);
            Assert.AreSame(strategy, game.AiStrategy);
        }

        [TestMethod]
        // дзеркальні початкові умови повинні давати дзеркальні результати
        public void MirroredGames_StaySymmetricWithMovingPaddles()
        {
            foreach (int direction in new[] { -1, 0, 1 })
            {
                PongGame left = new PongGame(new TestStrategy { Direction = direction },
                    new Ball(80, 210, -500, 60), new Random(24));
                PongGame right = new PongGame(new TestStrategy { Direction = direction },
                    new Ball(708, 210, 500, 60), new Random(24));
                for (int i = 0; i < 1000; i++)
                {
                    left.SetPlayerDirection(direction);
                    right.SetPlayerDirection(direction);
                    left.Update(0.01);
                    right.Update(0.01);
                    Assert.AreEqual(left.PlayerScore, right.ComputerScore);
                    Assert.AreEqual(left.ComputerScore, right.PlayerScore);
                    Assert.AreEqual(GameRules.Width - Ball.Size - left.Ball.X, right.Ball.X, 0.000001);
                    Assert.AreEqual(left.Ball.Y, right.Ball.Y, 0.000001);
                    Assert.AreEqual(left.Ball.SpeedY, right.Ball.SpeedY, 0.000001);
                    Assert.AreEqual(-left.Ball.SpeedX, right.Ball.SpeedX, 0.000001);
                }
            }
        }

        [TestMethod]
        // виконуємо серію матчів із незмінною тактикою руху гравця
        public void CampingAtTopBottomOrCenter_IsNotAWinningShortcut()
        {
            foreach (bool defensive in new[] { false, true })
            foreach (int direction in new[] { -1, 0, 1 })
            {
                int playerWins = 0;
                for (int seed = 0; seed < 24; seed++)
                {
                    IAiStrategy strategy = defensive
                        ? (IAiStrategy)new DefensiveStrategy(new Random(seed + 100))
                        : new FollowBallStrategy(new Random(seed + 100));
                    PongGame game = new PongGame(strategy, random: new Random(seed));
                    for (int i = 0; i < 30000 && !game.IsFinished; i++)
                    {
                        game.SetPlayerDirection(direction);
                        game.Update(0.01);
                    }
                    Assert.IsTrue(game.IsFinished);
                    if (game.PlayerWon) playerWins++;
                    Assert.IsTrue(UnitTests.Speed(game.Ball) <= GameRules.MaxBallSpeed + 0.000001);
                }
                Assert.IsTrue(playerWins < 12, "очікування біля межі не має бути виграшною тактикою");
            }
        }
    }

    internal class TestStrategy : IAiStrategy
    {
        public string Name { get { return "тестова стратегія"; } }
        public int Direction { get; set; }
        public int Calls { get; private set; }
        public Ball LastBall { get; private set; }
        public int GetDirection(Paddle paddle, Ball ball)
        {
            Calls++;
            LastBall = ball;
            return Direction;
        }
    }

    internal class AimRandom : Random
    {
        private readonly int offset;
        public int Calls { get; private set; }
        public AimRandom(int offset) { this.offset = offset; }
        public override int Next(int minValue, int maxValue)
        {
            Calls++;
            return offset;
        }
    }
}
