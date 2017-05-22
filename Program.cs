using System.Collections.Generic;
using System.Linq;
using System;

using Newtonsoft.Json;

namespace Robosight
{
    public sealed class Program
    {
        public static void Main(string[] args)
        {
            new Program().Execute();
        }

        private const double EPS = 0.00000001;
        private Random random = new Random();

        private void Execute()
        {
            string line;
            while ((line = Console.ReadLine()) != null)
            {
                var friendsAndEnemies = JsonConvert.DeserializeObject<List<List<Tank>>>(line);
                var actions = ThinkActions(friendsAndEnemies[0], friendsAndEnemies[1]);

                Console.WriteLine(JsonConvert.SerializeObject(actions));
            }
        }

        private List<Action> ThinkActions(List<Tank> friends, List<Tank> enemies)
        {
            return friends.Select(friend => ThinkAction(friend, friends, enemies)).ToList();
        }

        private Action ThinkAction(Tank friend, List<Tank> friends, List<Tank> enemies)
        {
            // HPがないと何もできないので、何もしません。
            if (friend.HP <= 0.0)
            {
                return null;
            }

            // 一番弱っている敵を探します。
            var targetEnemy = enemies.Where(enemy => enemy.HP > 0.0).Aggregate((acc, enemy) => enemy.HP < acc.HP ? enemy : acc);

            // 砲撃できる場合は、砲撃します。
            if (friend.CanShootAfter < 2)
            {
                // まずは向き変え。
                var targetEnemyAngle = Angle(Sub(targetEnemy.Center, friend.Center));
                if (Math.Abs(targetEnemyAngle - friend.Direction) > Math.PI * 5 / 180)
                {
                    return new Action() { Function = "turn-to", Parameter = targetEnemyAngle };
                }

                // 砲撃。
                return new Action() { Function = "shoot", Parameter = 10.0 };
            }

            // 現在の速度を計算します。
            var speed = Length(friend.Velocity);

            // Console.Errorには自由に出力できます。Console.Outは使っちゃ駄目。
            Console.Error.WriteLine(string.Format("{0}: speed = {1:0.00}", friend.Name, speed));

            // 速度が遅い場合は、乱数回避機動。。。(*^^*)
            if (speed < 5.0)
            {
                if (random.NextDouble() < 0.2)
                {
                    // 適当に回転。
                    return new Action() { Function = "turn-to", Parameter = random.NextDouble() * Math.PI * 2 };
                }

                // 適当に加速。
                return new Action() { Function = "forward", Parameter = random.NextDouble() * 0.5 + 0.5 };
            }

            // まずは、速度がゼロになる方向に向き変え。
            double antiVelocityAngle = NormalizeAngle(Angle(friend.Velocity) + Math.PI);
            if (Math.Abs(antiVelocityAngle - friend.Direction) > EPS)
            {
                return new Action() { Function = "turn-to", Parameter = antiVelocityAngle };
            }

            // 加速。
            return new Action() { Function = "forward", Parameter = 1.0 };  // 加速が大きすぎる場合は、システムが勝手に1.0まで下げます。
        }

        private double[] Sub(double[] vector1, double[] vector2)
        {
            return new double[] { vector1[0] - vector2[0], vector1[1] - vector2[1] };
        }

        private double Length(double[] vector)
        {
            return Math.Sqrt(Math.Pow(vector[0], 2) + Math.Pow(vector[1], 2));
        }

        private double Angle(double[] vector)
        {
            return Math.Atan2(vector[1], vector[0]);
        }

        private double NormalizeAngle(double angle)
        {
            return Angle(new double[] { Math.Cos(angle), Math.Sin(angle) });
        }
    }

    internal sealed class Tank
    {
        [JsonProperty("center")]
        public double[] Center { get; set; }

        [JsonProperty("direction")]
        public double Direction { get; set; }

        [JsonProperty("velocity")]
        public double[] Velocity { get; set; }

        [JsonProperty("hp")]
        public double HP { get; set; }

        [JsonProperty("can-shoot-after")]
        public int CanShootAfter { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    internal sealed class Action
    {
        [JsonProperty("function")]
        public string Function { get; set; }

        [JsonProperty("parameter")]
        public double Parameter { get; set; }
    }
}
