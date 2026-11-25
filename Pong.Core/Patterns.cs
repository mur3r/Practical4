using System;

namespace Pong.Core.Strategies
{
    // спільний інтерфейс дозволяє міняти поведінку бота під час гри
    public interface IAiStrategy
    {
        string Name { get; }
        int GetDirection(Paddle paddle, Ball ball);
    }

    // ця стратегія супроводжує м’яч і підбирає точку для різних ударів
    public class FollowBallStrategy : IAiStrategy
    {
        private readonly AiAim aim;
        public string Name { get { return "Переслідування м'яча"; } }

        public FollowBallStrategy(Random random = null)
        {
            aim = new AiAim(random);
        }

        public int GetDirection(Paddle paddle, Ball ball)
        {
            return aim.GetDirection(paddle, ball, false);
        }
    }

    // захисна стратегія повертає ракетку до центру, коли м’яч віддаляється
    public class DefensiveStrategy : IAiStrategy
    {
        private readonly AiAim aim;
        public string Name { get { return "Захисна гра"; } }

        public DefensiveStrategy(Random random = null)
        {
            aim = new AiAim(random);
        }

        public int GetDirection(Paddle paddle, Ball ball)
        {
            return aim.GetDirection(paddle, ball, true);
        }
    }

    // спільний розрахунок цілі використовується обома стратегіями
    internal class AiAim
    {
        private readonly Random random;
        private bool wasApproaching;
        private double previousX = GameRules.Width;
        private int hitOffset;

        public AiAim(Random random)
        {
            this.random = random ?? new Random();
        }

        public int GetDirection(Paddle paddle, Ball ball, bool defensive)
        {
            if (paddle == null) throw new ArgumentNullException(nameof(paddle));
            if (ball == null) throw new ArgumentNullException(nameof(ball));

            bool approaching = ball.SpeedX > 0;
            // бот обирає місце удару один раз за підліт м'яча, а не кожен кадр
            if (approaching && (!wasApproaching || ball.X < previousX))
                hitOffset = random.Next(-26, 27);
            wasApproaching = approaching;
            previousX = ball.X;

            // у захисті повертаємося до центру, коли м'яч віддаляється
            double targetY = defensive && !approaching
                ? GameRules.Height / 2 : ball.CenterY - hitOffset;
            targetY = Math.Max(Paddle.Height / 2,
                Math.Min(GameRules.Height - Paddle.Height / 2, targetY));
            // невеликий допуск біля цілі запобігає смиканню ракетки
            double difference = targetY - paddle.CenterY;
            if (Math.Abs(difference) <= 3) return 0;
            return Math.Sign(difference);
        }
    }
}

namespace Pong.Core.Commands
{
    // обгортаємо дії гравця в окремі команди
    public interface ICommand
    {
        void Execute();
    }

    // одна команда підходить для руху вгору, вниз і зупинки
    public class MovePaddleCommand : ICommand
    {
        private readonly PongGame game;
        private readonly int direction;

        public MovePaddleCommand(PongGame game, int direction)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            Guard.CheckDirection(direction);
            this.game = game;
            this.direction = direction;
        }

        public void Execute()
        {
            game.SetPlayerDirection(direction);
        }
    }

    // команда паузи однакова для виклику кнопкою та клавіатурою
    public class PauseCommand : ICommand
    {
        private readonly PongGame game;

        public PauseCommand(PongGame game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            this.game = game;
        }

        public void Execute()
        {
            game.TogglePause();
        }
    }

    // команда нового матчу передає моделі запит на скидання гри
    public class RestartCommand : ICommand
    {
        private readonly PongGame game;

        public RestartCommand(PongGame game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            this.game = game;
        }

        public void Execute()
        {
            game.Restart();
        }
    }
}
