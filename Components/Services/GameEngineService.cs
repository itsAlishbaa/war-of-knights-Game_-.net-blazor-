//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using final_pro_c.Models;

//namespace final_pro_c.Services
//{
//    public class LevelConfig
//    {
//        public string Name { get; set; } = "";
//        public string Badge { get; set; } = "";
//        public string Enemy { get; set; } = "";
//        public string EnemyLabel { get; set; } = "";
//        public int KnightHP { get; set; }
//        public int EnemyHP { get; set; }
//        public int[] EnemyAtk { get; set; } = new int[2];
//        public int[] KnightAtk { get; set; } = new int[2];
//        public int FireCooldown { get; set; }
//        public int[] FireAtk { get; set; } = new int[2];
//        public int EnemyAtkCooldown { get; set; }
//        public string Label { get; set; } = "";
//        public int Score { get; set; }
//    }

//    public class GameState
//    {
//        public int KnightHP { get; set; }
//        public int KnightMaxHP { get; set; }
//        public int EnemyHP { get; set; }
//        public int EnemyMaxHP { get; set; }
//        public int Score { get; set; }
//        public bool Paused { get; set; }
//        public bool Over { get; set; }
//        public bool Blocking { get; set; }
//        public int Level { get; set; }
//    }

//    public class GameEngineService : IDisposable
//    {
//        public static readonly Dictionary<int, LevelConfig> Levels = new()
//        {
//            { 1, new LevelConfig { Name="The Skeleton Crypt", Badge="LEVEL 1 — EASY", Enemy="skeleton", EnemyLabel="💀 SKELETON", KnightHP=150, EnemyHP=80, EnemyAtk=new[] {8,14}, KnightAtk=new[] {15,22}, FireCooldown=4000, FireAtk=new[] {10,16}, EnemyAtkCooldown=2000, Label="Skeleton", Score=100 } },
//            { 2, new LevelConfig { Name="The Undead Warrior", Badge="LEVEL 2 — MEDIUM", Enemy="warrior", EnemyLabel="💂 WARRIOR", KnightHP=120, EnemyHP=130, EnemyAtk=new[] {18,25}, KnightAtk=new[] {18,26}, FireCooldown=3000, FireAtk=new[] {15,22}, EnemyAtkCooldown=1600, Label="Warrior", Score=200 } },
//            { 3, new LevelConfig { Name="The Dragon's Lair", Badge="LEVEL 3 — HARD", Enemy="dragon", EnemyLabel="🐉 DRAGON", KnightHP=100, EnemyHP=200, EnemyAtk=new[] {25,40}, KnightAtk=new[] {20,30}, FireCooldown=2000, FireAtk=new[] {20,35}, EnemyAtkCooldown=1200, Label="Dragon", Score=400 } }
//        };

//        public GameState? State { get; private set; }
//        public List<string> CombatLog { get; } = new();
//        public int KnightMarginLeft { get; private set; } = 0;
//        public int ProjectilePosition { get; private set; } = 30;
//        public bool IsProjectileActive { get; private set; } = false;

//        public event Action? OnStateChanged;
//        public event Action<bool>? OnGameEnded; // true if won
//        public event Action? OnScreenFlash;
//        public event Action<string, string, bool>? OnHitTextCreated; // Text, Color, IsEnemy

//        private CancellationTokenSource? _gameLoopCts;
//        private readonly Random _rand = new();

//        public void StartGame(int level)
//        {
//            var cfg = Levels[level];
//            State = new GameState
//            {
//                KnightHP = cfg.KnightHP,
//                KnightMaxHP = cfg.KnightHP,
//                EnemyHP = cfg.EnemyHP,
//                EnemyMaxHP = cfg.EnemyHP,
//                Score = 0,
//                Paused = false,
//                Over = false,
//                Blocking = false,
//                Level = level
//            };

//            CombatLog.Clear();
//            KnightMarginLeft = 0;
//            IsProjectileActive = false;
//            LogCombat($"⚔ Battle begins! Knight vs {cfg.Label}!", "info");

//            // --- FIXED: Safe cancellation loop block ---
//            try
//            {
//                if (_gameLoopCts != null)
//                {
//                    _gameLoopCts.Cancel();
//                    _gameLoopCts.Dispose();
//                }
//            }
//            catch (ObjectDisposedException)
//            {
//                // Agar object already disposed tha toh ignore karega, crash nahi hoga
//            }

//            // Fresh token initialization
//            _gameLoopCts = new CancellationTokenSource();
//            // --------------------------------------------

//            StartEnemyActionLoops(_gameLoopCts.Token);
//            NotifyState();
//        }

//        private void StartEnemyActionLoops(CancellationToken token)
//        {
//            if (State == null) return;
//            var cfg = Levels[State.Level];

//            // Enemy Attack Loop
//            Task.Run(async () => {
//                try
//                {
//                    while (!token.IsCancellationRequested && State != null && !State.Over)
//                    {
//                        await Task.Delay(cfg.EnemyAtkCooldown, token);
//                        if (State == null || State.Paused || State.Over) continue;
//                        ExecuteEnemyAttack();
//                    }
//                }
//                catch (OperationCanceledException) { /* Loop cleanly stopped */ }
//            }, token);

//            // Fireball Loop
//            Task.Run(async () => {
//                try
//                {
//                    while (!token.IsCancellationRequested && State != null && !State.Over)
//                    {
//                        await Task.Delay(cfg.FireCooldown, token);
//                        if (State == null || State.Paused || State.Over || IsProjectileActive) continue;
//                        await ExecuteFireballSequence(token);
//                    }
//                }
//                catch (OperationCanceledException) { /* Loop cleanly stopped */ }
//            }, token);
//        }

//        public void KnightAttack()
//        {
//            if (State == null || State.Over || State.Paused) return;
//            var cfg = Levels[State.Level];
//            int dmg = _rand.Next(cfg.KnightAtk[0], cfg.KnightAtk[1] + 1);

//            State.EnemyHP -= dmg;
//            State.Score += (int)(dmg * 1.5);

//            LogCombat($"⚔ Knight attacks for {dmg} damage!", "dmg");
//            OnHitTextCreated?.Invoke($"-{dmg} ⚔", "#ff6644", true);

//            if (State.EnemyHP <= 0)
//            {
//                State.EnemyHP = 0;
//                EndGame(true);
//            }
//            NotifyState();
//        }

//        private void ExecuteEnemyAttack()
//        {
//            if (State == null || State.Over || State.Paused) return;
//            var cfg = Levels[State.Level];
//            int dmg = _rand.Next(cfg.EnemyAtk[0], cfg.EnemyAtk[1] + 1);

//            if (State.Blocking)
//            {
//                int blocked = (int)(dmg * 0.75);
//                dmg -= blocked;
//                LogCombat($"🛡 Knight blocks! Only {dmg} damage taken!", "block");
//                OnHitTextCreated?.Invoke($"BLOCKED! -{dmg}", "#4aa8ff", false);
//            }
//            else
//            {
//                LogCombat($"💥 {cfg.Label} attacks for {dmg} damage!", "dmg");
//                OnScreenFlash?.Invoke();
//                OnHitTextCreated?.Invoke($"-{dmg} 💥", "#ff3333", false);
//            }

//            State.KnightHP -= dmg;
//            if (State.KnightHP <= 0)
//            {
//                State.KnightHP = 0;
//                EndGame(false);
//            }
//            NotifyState();
//        }

//        private async Task ExecuteFireballSequence(CancellationToken token)
//        {
//            if (State == null || State.Over || State.Paused) return;
//            var cfg = Levels[State.Level];

//            ProjectilePosition = 30;
//            IsProjectileActive = true;

//            try
//            {
//                while (ProjectilePosition < 75 && State != null && !State.Over)
//                {
//                    await Task.Delay(40, token);
//                    if (State == null || State.Paused) continue;
//                    ProjectilePosition += 4;
//                    NotifyState();
//                }
//            }
//            catch (OperationCanceledException)
//            {
//                IsProjectileActive = false;
//                return;
//            }

//            IsProjectileActive = false;
//            if (State == null || State.Over) return;

//            if (State.Blocking)
//            {
//                LogCombat("🛡 Shield deflects the fireball!", "block");
//                OnHitTextCreated?.Invoke("SHIELD! 🛡", "#4aa8ff", false);
//                State.Score += 20;
//            }
//            else
//            {
//                int fireDmg = _rand.Next(cfg.FireAtk[0], cfg.FireAtk[1] + 1);
//                State.KnightHP -= fireDmg;
//                LogCombat($"🔥 Fireball hits for {fireDmg} damage!", "fire");
//                OnScreenFlash?.Invoke();
//                OnHitTextCreated?.Invoke($"-{fireDmg} 🔥", "#ff8800", false);

//                if (State.KnightHP <= 0)
//                {
//                    State.KnightHP = 0;
//                    EndGame(false);
//                }
//            }
//            NotifyState();
//        }

//        public void StartBlock()
//        {
//            if (State == null || State.Over || State.Paused) return;
//            State.Blocking = true;
//            NotifyState();
//        }

//        public void StopBlock()
//        {
//            if (State != null) State.Blocking = false;
//            NotifyState();
//        }

//        public void MoveKnight(int direction)
//        {
//            if (State == null || State.Over || State.Paused) return;
//            KnightMarginLeft += direction * 20;
//            State.Score += 2;
//            LogCombat($"Knight moves {(direction > 0 ? "forward" : "backward")}", "info");
//            NotifyState();
//        }

//        public void TogglePause()
//        {
//            if (State == null || State.Over) return;
//            State.Paused = !State.Paused;
//            NotifyState();
//        }

//        private void EndGame(bool won)
//        {
//            if (State == null || State.Over) return;
//            State.Over = true;

//            try
//            {
//                _gameLoopCts?.Cancel();
//            }
//            catch (ObjectDisposedException) { /* Already disposed, ignore */ }

//            OnGameEnded?.Invoke(won);
//        }

//        private void LogCombat(string message, string cssClass)
//        {
//            CombatLog.Insert(0, message);
//            if (CombatLog.Count > 12) CombatLog.RemoveAt(CombatLog.Count - 1);
//        }

//        private void NotifyState() => OnStateChanged?.Invoke();

//        // --- FIXED: Safe memory cleanup when engine is destroyed ---
//        public void Dispose()
//        {
//            try
//            {
//                _gameLoopCts?.Cancel();
//            }
//            catch { }
//            finally
//            {
//                _gameLoopCts?.Dispose();
//                _gameLoopCts = null; // Reference cleared
//            }
//        }
//    }
//}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace final_pro_c.Services
{
    public class GameEngineService : IDisposable
    {
        // LEVEL CONFIGURATIONS (HP pools increased and damage scaled for longer gameplay)
        public static readonly Dictionary<int, LevelConfig> Levels = new()
        {
            { 1, new LevelConfig { Enemy = "skeleton", Label = "Skeleton", EnemyLabel = "💀 SKELETON CRYPT", Badge = "LVL 1", MaxHP = 350, BaseDamage = 7, MovementSpeed = 6 } },
            { 2, new LevelConfig { Enemy = "warrior", Label = "Warrior", EnemyLabel = "🛡️ UNDEAD WARRIOR", Badge = "LVL 2", MaxHP = 550, BaseDamage = 11, MovementSpeed = 7 } },
            { 3, new LevelConfig { Enemy = "dragon", Label = "Dragon", EnemyLabel = "🔥 DRAGON'S LAIR", Badge = "LVL 3", MaxHP = 950, BaseDamage = 16, MovementSpeed = 4 } }
        };

        public GameState? State { get; private set; }
        public int KnightMarginLeft { get; private set; } = 0;
        public bool IsProjectileActive { get; private set; } = false;
        public int ProjectilePosition { get; private set; } = 0;
        public List<string> CombatLog { get; private set; } = new();

        // AI MOVEMENT PROPERTY: Track how much the enemy has walked from the right side towards left
        public int EnemyMarginRight { get; private set; } = 0;

        // EVENTS FOR INTERACTIVE BINDING
        public event Action? OnStateChanged;
        public event Action<bool>? OnGameEnded;
        public event Action? OnScreenFlash;
        public event Action<string, string, bool>? OnHitTextCreated;

        private System.Timers.Timer? _gameTimer;
        private Random _random = new();

        public void StartGame(int level)
        {
            var config = Levels[level];

            // Knight Max HP set to 450 to make matches significantly longer and strategic
            State = new GameState
            {
                Level = level,
                KnightHP = 450,
                KnightMaxHP = 450,
                EnemyHP = config.MaxHP,
                EnemyMaxHP = config.MaxHP,
                Score = 0,
                Paused = false,
                Over = false,
                Blocking = false
            };

            KnightMarginLeft = 0;
            EnemyMarginRight = 0; // Reset enemy position to start line
            IsProjectileActive = false;
            ProjectilePosition = 0;
            CombatLog.Clear();
            CombatLog.Add("The gates open! Move FORWARD to engage the creature.");

            // Standardized game pulse loop (120ms for ultra-smooth AI calculations)
            _gameTimer?.Dispose();
            _gameTimer = new System.Timers.Timer(120);
            _gameTimer.Elapsed += GameTick;
            _gameTimer.Start();

            NotifyState();
        }

        private void GameTick(object? sender, ElapsedEventArgs e)
        {
            if (State == null || State.Paused || State.Over) return;

            var config = Levels[State.Level];

            // Arena Distance Engine (Dynamic spatial profiling)
            // Total width context: 600px boundary window
            int currentDistance = 600 - (KnightMarginLeft + EnemyMarginRight);

            // --- ADVANCED EMULATED AI MOVEMENT PIPELINE ---
            if (currentDistance > 115)
            {
                // CHASE STATE: Enemy approaches the player aggressively if too far
                EnemyMarginRight += config.MovementSpeed;

                if (_random.Next(0, 100) < 4)
                {
                    CombatLog.Insert(0, $"The {config.Label} steps forward, keeping its eyes locked on you.");
                }
            }
            else if (currentDistance < 85)
            {
                // RETREAT/ADJUST STATE: Tactical repositioning if Knight gets too close
                EnemyMarginRight -= (config.MovementSpeed - 2);
            }

            // --- REAL-TIME COMBAT LOGIC TRIGGER ---
            // If inside dynamic striking zone, unleash weapon swing patterns
            if (currentDistance <= 125)
            {
                if (_random.Next(0, 100) < 30) // 30% Attack deployment rate per pulse frame
                {
                    EnemyAttack(config);
                }
            }

            // --- LEVEL 3 SPECIFIC PROJECTILE MANAGEMENT ---
            if (config.Enemy == "dragon" && !IsProjectileActive && _random.Next(0, 100) < 12)
            {
                IsProjectileActive = true;
                ProjectilePosition = 0;
            }

            if (IsProjectileActive)
            {
                ProjectilePosition += 38; // Fluid cinematic translation velocity

                // Process structural payload intersection
                if (ProjectilePosition >= (600 - KnightMarginLeft - 50))
                {
                    IsProjectileActive = false;
                    if (!State.Blocking)
                    {
                        int dmg = config.BaseDamage + 6;
                        State.KnightHP -= dmg;
                        OnScreenFlash?.Invoke();
                        OnHitTextCreated?.Invoke($"💥 -{dmg}", "#ff4757", false);
                        CombatLog.Insert(0, $"The Dragon's breath burns you for {dmg} damage!");
                    }
                    else
                    {
                        OnHitTextCreated?.Invoke("🛡️ BLOCKED", "#00d2d3", false);
                        CombatLog.Insert(0, "Your steel shield deflects the dragon fire!");
                    }
                    CheckGameOver();
                }
                else if (ProjectilePosition > 750)
                {
                    IsProjectileActive = false;
                }
            }

            NotifyState();
        }

        public void MoveKnight(int direction)
        {
            if (State == null || State.Over || State.Paused) return;

            int nextPosition = KnightMarginLeft + (direction * 22);
            int currentDistance = 600 - (nextPosition + EnemyMarginRight);

            // Rigid bounding array constraint validation
            if (nextPosition >= 0 && currentDistance > 75)
            {
                KnightMarginLeft = nextPosition;
            }
            NotifyState();
        }

        public void KnightAttack()
        {
            if (State == null || State.Over || State.Paused) return;

            var config = Levels[State.Level];
            int currentDistance = 600 - (KnightMarginLeft + EnemyMarginRight);

            if (currentDistance <= 135)
            {
                // Damage balanced to ensure sustained combat scenarios
                int dmg = _random.Next(16, 26);
                State.EnemyHP -= dmg;
                State.Score += dmg * 3;

                OnHitTextCreated?.Invoke($"⚔ -{dmg}", "#ffee58", true);
                CombatLog.Insert(0, $"You sliced the {config.Label} dealing {dmg} damage.");

                CheckGameOver();
            }
            else
            {
                CombatLog.Insert(0, "Out of reach! Step closer to slash.");
            }
            NotifyState();
        }

        private void EnemyAttack(LevelConfig config)
        {
            if (State == null || State.Over) return;

            if (State.Blocking)
            {
                OnHitTextCreated?.Invoke("🛡️ BLOCKED", "#00d2d3", false);
                CombatLog.Insert(0, $"You raise your guard! Deflected {config.Label}'s attack.");
            }
            else
            {
                int dmg = _random.Next(config.BaseDamage - 2, config.BaseDamage + 4);
                State.KnightHP -= dmg;

                OnScreenFlash?.Invoke();
                OnHitTextCreated?.Invoke($"💥 -{dmg}", "#ff4757", false);
                CombatLog.Insert(0, $"The {config.Label} lunges forward and strikes you for {dmg} physical damage.");

                CheckGameOver();
            }
        }

        public void StartBlock() { if (State != null) State.Blocking = true; NotifyState(); }
        public void StopBlock() { if (State != null) State.Blocking = false; NotifyState(); }

        public void TogglePause()
        {
            if (State == null || State.Over) return;
            State.Paused = !State.Paused;
            NotifyState();
        }

        private void CheckGameOver()
        {
            if (State == null) return;

            if (State.EnemyHP <= 0)
            {
                State.EnemyHP = 0;
                State.Over = true;
                _gameTimer?.Stop();
                OnGameEnded?.Invoke(true);
            }
            else if (State.KnightHP <= 0)
            {
                State.KnightHP = 0;
                State.Over = true;
                _gameTimer?.Stop();
                OnGameEnded?.Invoke(false);
            }
        }

        private void NotifyState() => OnStateChanged?.Invoke();

        public void Dispose()
        {
            _gameTimer?.Dispose();
        }
    }

    // INTERNAL COMPONENT CONTRACT CONFIGURATIONS
    public class GameState
    {
        public int Level { get; set; }
        public int KnightHP { get; set; }
        public int KnightMaxHP { get; set; }
        public int EnemyHP { get; set; }
        public int EnemyMaxHP { get; set; }
        public int Score { get; set; }
        public bool Paused { get; set; }
        public bool Over { get; set; }
        public bool Blocking { get; set; }
    }

    public class LevelConfig
    {
        public string Enemy { get; set; } = "";
        public string Label { get; set; } = "";
        public string EnemyLabel { get; set; } = "";
        public string Badge { get; set; } = "";
        public int MaxHP { get; set; }
        public int BaseDamage { get; set; }
        public int MovementSpeed { get; set; }
    }
}