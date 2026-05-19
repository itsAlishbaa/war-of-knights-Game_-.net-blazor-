using System;
using System.Threading;
using System.Threading.Tasks;
using final_pro_c.Models;

namespace IronCrusadeBlazor.Services
{
    public class LevelConfig
    {
        public string Name { get; set; } = "";
        public string Badge { get; set; } = "";
        public string Enemy { get; set; } = "";
        public string EnemyLabel { get; set; } = "";
        public int KnightHP { get; set; }
        public int EnemyHP { get; set; }
        public int[] EnemyAtk { get; set; } = new int[2];
        public int[] KnightAtk { get; set; } = new int[2];
        public int FireCooldown { get; set; }
        public int[] FireAtk { get; set; } = new int[2];
        public int EnemyAtkCooldown { get; set; }
        public string Label { get; set; } = "";
        public int Score { get; set; }
    }

    public class GameState
    {
        public int KnightHP { get; set; }
        public int KnightMaxHP { get; set; }
        public int EnemyHP { get; set; }
        public int EnemyMaxHP { get; set; }
        public int Score { get; set; }
        public bool Paused { get; set; }
        public bool Over { get; set; }
        public bool Blocking { get; set; }
        public int Level { get; set; }
    }

    public class GameEngineService : IDisposable
    {
        public static readonly Dictionary<int, LevelConfig> Levels = new()
        {
            { 1, new LevelConfig { Name="The Skeleton Crypt", Badge="LEVEL 1 — EASY", Enemy="skeleton", EnemyLabel="💀 SKELETON", KnightHP=150, EnemyHP=80, EnemyAtk=new[] {8,14}, KnightAtk=new[] {15,22}, FireCooldown=4000, FireAtk=new[] {10,16}, EnemyAtkCooldown=2000, Label="Skeleton", Score=100 } },
            { 2, new LevelConfig { Name="The Undead Warrior", Badge="LEVEL 2 — MEDIUM", Enemy="warrior", EnemyLabel="💂 WARRIOR", KnightHP=120, EnemyHP=130, EnemyAtk=new[] {18,25}, KnightAtk=new[] {18,26}, FireCooldown=3000, FireAtk=new[] {15,22}, EnemyAtkCooldown=1600, Label="Warrior", Score=200 } },
            { 3, new LevelConfig { Name="The Dragon's Lair", Badge="LEVEL 3 — HARD", Enemy="dragon", EnemyLabel="🐉 DRAGON", KnightHP=100, EnemyHP=200, EnemyAtk=new[] {25,40}, KnightAtk=new[] {20,30}, FireCooldown=2000, FireAtk=new[] {20,35}, EnemyAtkCooldown=1200, Label="Dragon", Score=400 } }
        };

        public GameState? State { get; private set; }
        public List<string> CombatLog { get; } = new();
        public int KnightMarginLeft { get; private set; } = 0;
        public int ProjectilePosition { get; private set; } = 30;
        public bool IsProjectileActive { get; private set; } = false;

        public event Action? OnStateChanged;
        public event Action<bool>? OnGameEnded; // true if won
        public event Action? OnScreenFlash;
        public event Action<string, string, bool>? OnHitTextCreated; // Text, Color, IsEnemy

        private CancellationTokenSource? _gameLoopCts;
        private readonly Random _rand = new();

        public void StartGame(int level)
        {
            var cfg = Levels[level];
            State = new GameState
            {
                KnightHP = cfg.KnightHP,
                KnightMaxHP = cfg.KnightHP,
                EnemyHP = cfg.EnemyHP,
                EnemyMaxHP = cfg.EnemyHP,
                Score = 0,
                Paused = false,
                Over = false,
                Blocking = false,
                Level = level
            };

            CombatLog.Clear();
            KnightMarginLeft = 0;
            IsProjectileActive = false;
            LogCombat($"⚔ Battle begins! Knight vs {cfg.Label}!", "info");

            _gameLoopCts?.Cancel();
            _gameLoopCts = new CancellationTokenSource();

            StartEnemyActionLoops(_gameLoopCts.Token);
            NotifyState();
        }

        private void StartEnemyActionLoops(CancellationToken token)
        {
            if (State == null) return;
            var cfg = Levels[State.Level];

            // Enemy Attack Loop
            Task.Run(async () => {
                while (!token.IsCancellationRequested && State != null && !State.Over)
                {
                    await Task.Delay(cfg.EnemyAtkCooldown, token);
                    if (State == null || State.Paused || State.Over) continue;
                    ExecuteEnemyAttack();
                }
            }, token);

            // Fireball Loop
            Task.Run(async () => {
                while (!token.IsCancellationRequested && State != null && !State.Over)
                {
                    await Task.Delay(cfg.FireCooldown, token);
                    if (State == null || State.Paused || State.Over || IsProjectileActive) continue;
                    await ExecuteFireballSequence(token);
                }
            }, token);
        }

        public void KnightAttack()
        {
            if (State == null || State.Over || State.Paused) return;
            var cfg = Levels[State.Level];
            int dmg = _rand.Next(cfg.KnightAtk[0], cfg.KnightAtk[1] + 1);

            State.EnemyHP -= dmg;
            State.Score += (int)(dmg * 1.5);

            LogCombat($"⚔ Knight attacks for {dmg} damage!", "dmg");
            OnHitTextCreated?.Invoke($"-{dmg} ⚔", "#ff6644", true);

            if (State.EnemyHP <= 0)
            {
                State.EnemyHP = 0;
                EndGame(true);
            }
            NotifyState();
        }

        private void ExecuteEnemyAttack()
        {
            if (State == null || State.Over || State.Paused) return;
            var cfg = Levels[State.Level];
            int dmg = _rand.Next(cfg.EnemyAtk[0], cfg.EnemyAtk[1] + 1);

            if (State.Blocking)
            {
                int blocked = (int)(dmg * 0.75);
                dmg -= blocked;
                LogCombat($"🛡 Knight blocks! Only {dmg} damage taken!", "block");
                OnHitTextCreated?.Invoke($"BLOCKED! -{dmg}", "#4aa8ff", false);
            }
            else
            {
                LogCombat($"💥 {cfg.Label} attacks for {dmg} damage!", "dmg");
                OnScreenFlash?.Invoke();
                OnHitTextCreated?.Invoke($"-{dmg} 💥", "#ff3333", false);
            }

            State.KnightHP -= dmg;
            if (State.KnightHP <= 0)
            {
                State.KnightHP = 0;
                EndGame(false);
            }
            NotifyState();
        }

        private async Task ExecuteFireballSequence(CancellationToken token)
        {
            if (State == null || State.Over || State.Paused) return;
            var cfg = Levels[State.Level];

            ProjectilePosition = 30;
            IsProjectileActive = true;

            while (ProjectilePosition < 75 && State != null && !State.Over)
            {
                await Task.Delay(40, token);
                if (State == null || State.Paused) continue;
                ProjectilePosition += 4;
                NotifyState();
            }

            IsProjectileActive = false;
            if (State == null || State.Over) return;

            if (State.Blocking)
            {
                LogCombat("🛡 Shield deflects the fireball!", "block");
                OnHitTextCreated?.Invoke("SHIELD! 🛡", "#4aa8ff", false);
                State.Score += 20;
            }
            else
            {
                int fireDmg = _rand.Next(cfg.FireAtk[0], cfg.FireAtk[1] + 1);
                State.KnightHP -= fireDmg;
                LogCombat($"🔥 Fireball hits for {fireDmg} damage!", "fire");
                OnScreenFlash?.Invoke();
                OnHitTextCreated?.Invoke($"-{fireDmg} 🔥", "#ff8800", false);

                if (State.KnightHP <= 0)
                {
                    State.KnightHP = 0;
                    EndGame(false);
                }
            }
            NotifyState();
        }

        public void StartBlock()
        {
            if (State == null || State.Over || State.Paused) return;
            State.Blocking = true;
            NotifyState();
        }

        public void StopBlock()
        {
            if (State != null) State.Blocking = false;
            NotifyState();
        }

        public void MoveKnight(int direction)
        {
            if (State == null || State.Over || State.Paused) return;
            KnightMarginLeft += direction * 20;
            State.Score += 2;
            LogCombat($"Knight moves {(direction > 0 ? "forward" : "backward")}", "info");
            NotifyState();
        }

        public void TogglePause()
        {
            if (State == null || State.Over) return;
            State.Paused = !State.Paused;
            NotifyState();
        }

        private void EndGame(bool won)
        {
            if (State == null || State.Over) return;
            State.Over = true;
            _gameLoopCts?.Cancel();
            OnGameEnded?.Invoke(won);
        }

        private void LogCombat(string message, string cssClass)
        {
            CombatLog.Insert(0, message);
            if (CombatLog.Count > 12) CombatLog.RemoveAt(CombatLog.Count - 1);
        }

        private void NotifyState() => OnStateChanged?.Invoke();

        public void Dispose()
        {
            _gameLoopCts?.Cancel();
            _gameLoopCts?.Dispose();
        }
    }
}