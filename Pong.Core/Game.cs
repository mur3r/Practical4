using Pong.Core.Commands;
using Pong.Core.Strategies;
using System.Collections.Generic;
using System;

namespace Pong.Core
{
    // спільні розміри та обмеження використовуються всіма об’єктами гри
    public static class GameRules
    {
        public const double Width = 800;
        public const double Height = 450;
        public const double MaxDeltaTime = 0.1;
        public const double PhysicsStep = 0.005;
        public const double ServeSpeed = 340;
        public const double MaxBallSpeed = 600;
        public const double HitSpeedIncrease = 18;
        public const double MaxBounceAngle = 60;
        public const int WinningScore = 3;
    }

    internal static class Guard
    {
        public static void CheckRange(double value, double min, double max, string name)
        {
            // окремо відхиляємо нескінченність і значення, які не є числами
            if (double.IsNaN(value) || double.IsInfinity(value) || value < min || value > max)
                throw new ArgumentOutOfRangeException(name, "Значення має бути скінченним і в допустимих межах.");
        }

        // перевіряємо тривалість оновлення перед зміною координат
        public static void CheckTime(double deltaTime)
        {
            CheckRange(deltaTime, 0, GameRules.MaxDeltaTime, nameof(deltaTime));
        }

        // напрямок -1 означає рух угору, 0 — зупинку, 1 — рух униз
        public static void CheckDirection(int direction)
        {
            if (direction < -1 || direction > 1)
                throw new ArgumentOutOfRangeException(nameof(direction), "Напрямок має бути -1, 0 або 1.");
        }
    }

    // ракетка зберігає власну позицію, розміри та швидкість руху
    public class Paddle
    {
        public const double Width = 12;
        public const double Height = 80;
        public const double Speed = 300;

        public double X { get; private set; }
        public double Y { get; private set; }
        public double MoveSpeed { get; private set; }
        public double CenterY { get { return Y + Height / 2; } }

        // координати задають положення верхнього лівого кута ракетки
        public Paddle(double x, double y, double speed = Speed)
        {
            Guard.CheckRange(x, 0, GameRules.Width - Width, nameof(x));
            Guard.CheckRange(y, 0, GameRules.Height - Height, nameof(y));
            Guard.CheckRange(speed, 1, Speed, nameof(speed));
            X = x;
            Y = y;
            MoveSpeed = speed;
        }

        public void Move(int direction, double deltaTime)
        {
            Guard.CheckDirection(direction);
            Guard.CheckTime(deltaTime);
            double nextY = Y + direction * MoveSpeed * deltaTime;

            // ракетка повністю залишається в межах поля
            Y = System.Math.Max(0, System.Math.Min(GameRules.Height - Height, nextY));
        }

        // перед новим розіграшем повертаємо ракетку на середину поля
        internal void Reset()
        {
            Y = (GameRules.Height - Height) / 2;
        }
    }

    // м’яч зберігає координати та дві складові швидкості
    public class Ball
    {
        public const double Size = 12;
        public double X { get; private set; }
        public double Y { get; private set; }
        public double SpeedX { get; private set; }
        public double SpeedY { get; private set; }
        public double CenterY { get { return Y + Size / 2; } }

        public Ball(double x, double y, double speedX, double speedY)
        {
            Guard.CheckRange(x, 0, GameRules.Width - Size, nameof(x));
            Guard.CheckRange(y, 0, GameRules.Height - Size, nameof(y));
            Guard.CheckRange(speedX, -GameRules.MaxBallSpeed, GameRules.MaxBallSpeed, nameof(speedX));
            Guard.CheckRange(speedY, -GameRules.MaxBallSpeed, GameRules.MaxBallSpeed, nameof(speedY));
            if (speedX == 0 && speedY == 0)
                throw new ArgumentException("М'яч повинен мати ненульову швидкість.");
            if (Math.Sqrt(speedX * speedX + speedY * speedY) > GameRules.MaxBallSpeed + 0.000000001)
                throw new ArgumentOutOfRangeException(nameof(speedX));

            X = x;
            Y = y;
            SpeedX = speedX;
            SpeedY = speedY;
        }

        // переміщення дорівнює швидкості, помноженій на час
        internal void Move(double deltaTime)
        {
            X += SpeedX * deltaTime;
            Y += SpeedY * deltaTime;
        }

        internal void BounceFromPaddle(Paddle paddle, int direction)
        {
            X = direction > 0 ? paddle.X + Paddle.Width : paddle.X - Size;
            // центр дає прямий удар, а краї ракетки змінюють кут до 60 градусів
            double offset = (CenterY - paddle.CenterY) / (Paddle.Height / 2);
            offset = Math.Max(-1, Math.Min(1, offset));
            double angle = offset * GameRules.MaxBounceAngle * Math.PI / 180;
            // прискорення однакове для обох сторін і має верхню межу
            double speed = Math.Min(GameRules.MaxBallSpeed,
                Math.Sqrt(SpeedX * SpeedX + SpeedY * SpeedY) + GameRules.HitSpeedIncrease);
            SpeedX = direction * speed * Math.Cos(angle);
            SpeedY = speed * Math.Sin(angle);
        }

        // при ударі об горизонтальну стіну змінюємо вертикальний напрямок
        internal void BounceY(double y)
        {
            Y = y;
            SpeedY = -SpeedY;
        }

        // ставимо м’яч у центр і розкладаємо швидкість подачі на дві складові
        internal void Reset(int direction, double angle)
        {
            X = (GameRules.Width - Size) / 2;
            Y = (GameRules.Height - Size) / 2;
            SpeedX = direction * GameRules.ServeSpeed * Math.Cos(angle);
            SpeedY = GameRules.ServeSpeed * Math.Sin(angle);
        }
    }

    // модель відповідає за правила, рух об’єктів і стан усього матчу
    public class PongGame
    {
        private readonly Random random;
        private int serveDirection;
        private int playerDirection;

        public Paddle Player { get; private set; }
        public Paddle Computer { get; private set; }
        public Ball Ball { get; private set; }
        public IAiStrategy AiStrategy { get; private set; }
        public int PlayerScore { get; private set; }
        public int ComputerScore { get; private set; }
        public bool IsPaused { get; private set; }
        public bool PlayerWon { get { return PlayerScore >= GameRules.WinningScore; } }
        public bool IsFinished { get { return PlayerWon || ComputerScore >= GameRules.WinningScore; } }

        public PongGame(IAiStrategy strategy, Ball ball = null, Random random = null)
        {
            this.random = random ?? new Random();
            serveDirection = ball != null && ball.SpeedX != 0
                ? Math.Sign(ball.SpeedX) : this.random.Next(2) == 0 ? -1 : 1;
            SetStrategy(strategy);
            Player = new Paddle(30, (GameRules.Height - Paddle.Height) / 2);
            Computer = new Paddle(GameRules.Width - 30 - Paddle.Width,
                (GameRules.Height - Paddle.Height) / 2);
            Ball = ball ?? new Ball((GameRules.Width - Ball.Size) / 2,
                (GameRules.Height - Ball.Size) / 2, GameRules.ServeSpeed, 0);
            if (ball == null) ResetRound();
        }

        // міняємо спосіб керування ботом без початку нового матчу
        public void SetStrategy(IAiStrategy strategy)
        {
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));
            AiStrategy = strategy;
        }

        // команда задає напрямок, а координати змінюються під час оновлення
        public void SetPlayerDirection(int direction)
        {
            Guard.CheckDirection(direction);
            if (!IsPaused && !IsFinished) playerDirection = direction;
        }

        public void TogglePause()
        {
            if (!IsFinished)
            {
                // скидаємо старий напрямок, щоб після паузи не продовжився попередній рух
                IsPaused = !IsPaused;
                playerDirection = 0;
            }
        }

        // новий матч починається з нульового рахунку та нової першої подачі
        public void Restart()
        {
            PlayerScore = 0;
            ComputerScore = 0;
            IsPaused = false;
            serveDirection = random.Next(2) == 0 ? -1 : 1;
            ResetRound();
        }

        public void Update(double deltaTime)
        {
            Guard.CheckTime(deltaTime);
            if (IsPaused || IsFinished || deltaTime == 0) return;

            double remaining = deltaTime;
            // бот і гравець обирають напрямок один раз на оновлення
            int computerDirection = AiStrategy.GetDirection(Computer, Ball);
            // короткі підкроки не дають швидкому м'ячу перескочити ракетку
            while (remaining > 0.000000001)
            {
                double step = Math.Min(GameRules.PhysicsStep, remaining);
                // обидві ракетки рухаються з однаковою швидкістю в одному кроці фізики
                Player.Move(playerDirection, step);
                Computer.Move(computerDirection, step);
                double previousX = Ball.X;
                Ball.Move(step);
                CheckWalls();
                CheckPaddles(previousX);
                CheckWalls();

                if (CheckGoal()) break;
                remaining -= step;
            }
        }

        // відбиваємо м’яч лише тоді, коли він рухається назовні поля
        private void CheckWalls()
        {
            if (Ball.Y <= 0 && Ball.SpeedY < 0)
                Ball.BounceY(0);
            else if (Ball.Y + Ball.Size >= GameRules.Height && Ball.SpeedY > 0)
                Ball.BounceY(GameRules.Height - Ball.Size);
        }

        private void CheckPaddles(double previousX)
        {
            // враховуємо перетин передньої грані, щоб не відбивати м'яч із тилу
            if (Ball.SpeedX < 0 && previousX >= Player.X + Paddle.Width &&
                Ball.X <= Player.X + Paddle.Width && OverlapsVertically(Player))
            {
                Ball.BounceFromPaddle(Player, 1);
            }
            else if (Ball.SpeedX > 0 && previousX + Ball.Size <= Computer.X &&
                Ball.X + Ball.Size >= Computer.X && OverlapsVertically(Computer))
            {
                Ball.BounceFromPaddle(Computer, -1);
            }
        }

        // для удару вертикальні відрізки м’яча та ракетки мають перетинатися
        private bool OverlapsVertically(Paddle paddle)
        {
            return Ball.Y + Ball.Size >= paddle.Y && Ball.Y <= paddle.Y + Paddle.Height;
        }

        private bool CheckGoal()
        {
            // очко зараховується, коли весь м'яч залишив поле
            if (Ball.X + Ball.Size <= 0)
            {
                ComputerScore++;
                NextRound();
                return true;
            }
            if (Ball.X >= GameRules.Width)
            {
                PlayerScore++;
                NextRound();
                return true;
            }
            return false;
        }

        // після вирішального очка завершуємо матч, інакше готуємо нову подачу
        private void NextRound()
        {
            if (IsFinished)
            {
                playerDirection = 0;
                return;
            }
            serveDirection = -serveDirection;
            ResetRound();
        }

        private void ResetRound()
        {
            // подача має випадковий кут, а її сторона чергується після кожного очка
            double angle = (20 + random.NextDouble() * 20) * Math.PI / 180;
            if (random.Next(2) == 0) angle = -angle;
            Ball.Reset(serveDirection, angle);
            Player.Reset();
            Computer.Reset();
            playerDirection = 0;
        }
    }

    public enum GameAction
    {
        Up,
        Down,
        Stop,
        Pause,
        Restart
    }

    // цей контролер зберігає команди та передає їх моделі гри
    public class GameController
    {
        private readonly Dictionary<GameAction, ICommand> commands = new Dictionary<GameAction, ICommand>();
        public PongGame Game { get; private set; }

        public GameController(PongGame game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            Game = game;
            Bind(GameAction.Up, new MovePaddleCommand(game, -1));
            Bind(GameAction.Down, new MovePaddleCommand(game, 1));
            Bind(GameAction.Stop, new MovePaddleCommand(game, 0));
            Bind(GameAction.Pause, new PauseCommand(game));
            Bind(GameAction.Restart, new RestartCommand(game));
        }

        public void Bind(GameAction action, ICommand command)
        {
            // прив'язку можна змінити без зміни обробників клавіш
            if (!Enum.IsDefined(typeof(GameAction), action))
                throw new ArgumentOutOfRangeException(nameof(action));
            if (command == null) throw new ArgumentNullException(nameof(command));
            commands[action] = command;
        }

        // невідому дію ігноруємо, а відому виконуємо через інтерфейс команди
        public bool Execute(GameAction action)
        {
            ICommand command;
            if (!commands.TryGetValue(action, out command)) return false;
            command.Execute();
            return true;
        }

        public void Tick(double deltaTime)
        {
            Game.Update(deltaTime);
        }
    }
}
