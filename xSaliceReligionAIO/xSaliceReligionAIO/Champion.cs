using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace xSaliceReligionAIO
{
    class Champion
    {
        public Champion()
        {
            //Events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPosibleToInterrupt;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            GameObject.OnCreate += GameObject_OnCreate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Game.OnGameSendPacket += Game_OnSendPacket;
            GameObject.OnDelete += GameObject_OnDelete;
        }

        public Champion(bool load)
        {
            if(load)
                GameOnLoad();;
        }

        //Orbwalker instance
        public Orbwalking.Orbwalker Orbwalker;

        //Player instance
        public Obj_AI_Hero Player = ObjectManager.Player;
        public Obj_AI_Hero SelectedTarget = null;

        //Spells
        public List<Spell> SpellList = new List<Spell>();

        public Spell Q;
        public Spell W;
        public Spell E;
        public Spell R;
        public Spell _r2;
        public SpellDataInst qSpell = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q);
        public SpellDataInst wSpell = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W);
        public SpellDataInst rSpell = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R);

        //summoners
        public SpellSlot IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");
        
        //items
        public Items.Item DFG = Utility.Map.GetMap()._MapType == Utility.Map.MapType.TwistedTreeline ? new Items.Item(3188, 750) : new Items.Item(3128, 750);
        public Items.Item Botrk = new Items.Item(3153, 450);
        public Items.Item Bilge = new Items.Item(3144, 450);
        public Items.Item Hex = new Items.Item(3146, 700);
        public int lastPlaced;
        public Vector3 lastWardPos;

        //Mana Manager
        public int[] qMana = { 0, 0, 0, 0, 0, 0 };
        public int[] wMana = { 0, 0, 0, 0, 0, 0 };
        public int[] eMana = { 0, 0, 0, 0, 0, 0 };
        public int[] rMana = { 0, 0, 0, 0, 0, 0 };

        //Menu
        public static Menu menu;
        public static Menu orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");

        public void GameOnLoad()
        {
            Game.PrintChat("<font color = \"#FFB6C1\">xSalice's Religion AIO</font> by <font color = \"#00FFFF\">xSalice</font>");
            Game.PrintChat("<font color = \"#87CEEB\">Feel free to donate via Paypal to:</font> <font color = \"#FFFF00\">xSalicez@gmail.com</font>");

            menu = new Menu(Player.ChampionName, Player.ChampionName, true);

            //Info
            menu.AddSubMenu(new Menu("Info", "Info"));
            menu.SubMenu("Info").AddItem(new MenuItem("Author", "By xSalice"));
            menu.SubMenu("Info").AddItem(new MenuItem("Paypal", "Donate: xSalicez@gmail.com"));

            //Target selector
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            menu.AddSubMenu(targetSelectorMenu);

            //Orbwalker submenu
            orbwalkerMenu.AddItem(new MenuItem("Orbwalker_Mode", "Change Orbwalker").SetValue(true));
            menu.AddSubMenu(orbwalkerMenu);
            chooseOrbwalker(menu.Item("Orbwalker_Mode").GetValue<bool>());

            //Packet Menu
            menu.AddSubMenu(new Menu("Packet Setting", "Packets"));
            menu.SubMenu("Packets").AddItem(new MenuItem("packet", "Use Packets").SetValue(false));

            menu.AddToMainMenu();

            try
            {
                if (Activator.CreateInstance(null, "xSaliceReligionAIO.Champions." + Player.ChampionName) != null)
                {
                    Game.PrintChat("<font color = \"#FFB6C1\">xSalice's " + Player.ChampionName + " Loaded!</font>");
                }
            }
            catch
            {
                Game.PrintChat("xSalice's Religion => {0} Not Support !", Player.ChampionName);
            }
        }

        public void chooseOrbwalker(bool mode)
        {
            if (mode)
            {
                Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
                Game.PrintChat("Regular Orbwalker Loaded");
            }
            else
            {
                xSLxOrbwalker.AddToMenu(orbwalkerMenu);
                Game.PrintChat("xSLx Orbwalker Loaded");
            }
        }
        public bool packets()
        {
            return menu.Item("packet").GetValue<bool>();
        }

        public void Use_DFG(Obj_AI_Hero target)
        {
            if (target != null && Player.Distance(target) < 750 && Items.CanUseItem(DFG.Id))
                Items.UseItem(DFG.Id, target);
        }

        public void Use_Hex(Obj_AI_Hero target)
        {
            if (target != null && Player.Distance(target) < 450 && Items.CanUseItem(Hex.Id))
                Items.UseItem(Hex.Id, target);
        }
        public void Use_Botrk(Obj_AI_Hero target)
        {
            if (target != null && Player.Distance(target) < 450 && Items.CanUseItem(Botrk.Id))
                Items.UseItem(Botrk.Id, target);
        }

        public void Use_Bilge(Obj_AI_Hero target)
        {
            if (target != null && Bilge.IsReady() && Player.Distance(target) < 450 && Items.CanUseItem(Bilge.Id))
                Items.UseItem(Bilge.Id, target);
        }
        public void Use_Ignite(Obj_AI_Hero target)
        {
            if (target != null && IgniteSlot != SpellSlot.Unknown &&
                    Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && Player.Distance(target) < 650)
                Player.SummonerSpellbook.CastSpell(IgniteSlot, target);
        }

        public bool Ignite_Ready()
        {
            if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                return true;
            return false;
        }
        public static bool IsInsideEnemyTower(Vector3 position)
        {
            return ObjectManager.Get<Obj_AI_Turret>()
                                    .Any(tower => tower.IsEnemy && tower.Health > 0 && tower.Position.Distance(position) < 775);
        }
        public float GetManaPercent(Obj_AI_Hero unit = null)
        {
            if (unit == null)
                unit = Player;
            return (unit.Mana / unit.MaxMana) * 100f;
        }
        public float GetHealthPercent(Obj_AI_Hero unit = null)
        {
            if (unit == null)
                unit = Player;
            return (unit.Health / unit.MaxHealth) * 100f;
        }
        public bool HasBuff(Obj_AI_Base target, string buffName)
        {
            return target.Buffs.Any(buff => buff.Name == buffName);
        }
        public bool IsWall(Vector2 pos)
        {
            return (NavMesh.GetCollisionFlags(pos.X, pos.Y) == CollisionFlags.Wall ||
                    NavMesh.GetCollisionFlags(pos.X, pos.Y) == CollisionFlags.Building);
        }
        public Vector2 V2E(Vector3 from, Vector3 direction, float distance)
        {
            return from.To2D() + distance * Vector3.Normalize(direction - from).To2D();
        }
        public bool IsPassWall(Vector3 start, Vector3 end)
        {
            double count = Vector3.Distance(start, end);
            for (uint i = 0; i <= count; i += 10)
            {
                Vector2 pos = V2E(start, end, i);
                if (IsWall(pos))
                    return true;
            }
            return false;
        }
        public int countEnemiesNearPosition(Vector3 pos, float range)
        {
            return
                ObjectManager.Get<Obj_AI_Hero>().Count(
                    hero => hero.IsEnemy && !hero.IsDead && hero.IsValid && hero.Distance(pos) <= range);
        }

        public int countAlliesNearPosition(Vector3 pos, float range)
        {
            return
                ObjectManager.Get<Obj_AI_Hero>().Count(
                    hero => hero.IsAlly && !hero.IsDead && hero.IsValid && hero.Distance(pos) <= range);
        }

        public bool manaCheck()
        {
            int totalMana = qMana[Q.Level] + wMana[W.Level] + eMana[E.Level] + rMana[R.Level];
            var checkMana = menu.Item("mana").GetValue<bool>();

            if (Player.Mana >= totalMana || !checkMana)
                return true;

            return false;
        }

        public PredictionOutput GetP(Vector3 pos, Spell spell, Obj_AI_Base target, float delay, bool aoe)
        {
            return Prediction.GetPrediction(new PredictionInput
            {
                Unit = target,
                Delay = spell.Delay + delay,
                Radius = spell.Width,
                Speed = spell.Speed,
                From = pos,
                Range = spell.Range,
                Collision = spell.Collision,
                Type = spell.Type,
                RangeCheckFrom = Player.ServerPosition,
                Aoe = aoe,
            });
        }

        public PredictionOutput GetP(Vector3 pos, Spell spell, Obj_AI_Base target, bool aoe)
        {
            return Prediction.GetPrediction(new PredictionInput
            {
                Unit = target,
                Delay = spell.Delay,
                Radius = spell.Width,
                Speed = spell.Speed,
                From = pos,
                Range = spell.Range,
                Collision = spell.Collision,
                Type = spell.Type,
                RangeCheckFrom = Player.ServerPosition,
                Aoe = aoe,
            });
        }

        public PredictionOutput GetP2(Vector3 pos, Spell spell, Obj_AI_Base target, bool aoe)
        {
            return Prediction.GetPrediction(new PredictionInput
            {
                Unit = target,
                Delay = spell.Delay,
                Radius = spell.Width,
                Speed = spell.Speed,
                From = pos,
                Range = spell.Range,
                Collision = spell.Collision,
                Type = spell.Type,
                RangeCheckFrom = pos,
                Aoe = aoe,
            });
        }

        public PredictionOutput GetPCircle(Vector3 pos, Spell spell, Obj_AI_Base target, bool aoe)
        {
            return Prediction.GetPrediction(new PredictionInput
            {
                Unit = target,
                Delay = spell.Delay,
                Radius = 1,
                Speed = float.MaxValue,
                From = pos,
                Range = float.MaxValue,
                Collision = spell.Collision,
                Type = spell.Type,
                RangeCheckFrom = pos,
                Aoe = aoe,
            });
        }

        public Object[] VectorPointProjectionOnLineSegment(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            float cx = v3.X;
            float cy = v3.Y;
            float ax = v1.X;
            float ay = v1.Y;
            float bx = v2.X;
            float by = v2.Y;
            float rL = ((cx - ax) * (bx - ax) + (cy - ay) * (by - ay)) /
                       ((float)Math.Pow(bx - ax, 2) + (float)Math.Pow(by - ay, 2));
            var pointLine = new Vector2(ax + rL * (bx - ax), ay + rL * (by - ay));
            float rS;
            if (rL < 0)
            {
                rS = 0;
            }
            else if (rL > 1)
            {
                rS = 1;
            }
            else
            {
                rS = rL;
            }
            bool isOnSegment;
            if (rS.CompareTo(rL) == 0)
            {
                isOnSegment = true;
            }
            else
            {
                isOnSegment = false;
            }
            var pointSegment = new Vector2();
            if (isOnSegment)
            {
                pointSegment = pointLine;
            }
            else
            {
                pointSegment = new Vector2(ax + rS * (bx - ax), ay + rS * (by - ay));
            }
            return new object[3] { pointSegment, pointLine, isOnSegment };
        }

        public void CastBasicSkillShot(Spell spell, float range, SimpleTs.DamageType type, HitChance hitChance)
        {
            var target = SimpleTs.GetTarget(range, type);

            if (target == null || !spell.IsReady())
                return;
            spell.UpdateSourcePosition();

            if (spell.GetPrediction(target).Hitchance >= hitChance)
                spell.Cast(target, packets());
        }

        public Obj_AI_Hero GetTargetFocus(float range)
        {
            var focusSelected = menu.Item("selected").GetValue<bool>();

            if (SimpleTs.GetSelectedTarget() != null)
                if (focusSelected && SimpleTs.GetSelectedTarget().Distance(Player.ServerPosition) < range && SimpleTs.GetSelectedTarget().Type == GameObjectType.obj_AI_Hero)
                {
                    //Game.PrintChat("Focusing: " + SimpleTs.GetSelectedTarget().Name);
                    return SimpleTs.GetSelectedTarget();
                }
            return null;
        }

        public HitChance GetHitchance(string Source)
        {
            var hitC = HitChance.High;
            int qHit = menu.Item("qHit").GetValue<Slider>().Value;
            int harassQHit = menu.Item("qHit2").GetValue<Slider>().Value;

            // HitChance.Low = 3, Medium , High .... etc..
            if (Source == "Combo")
            {
                switch (qHit)
                {
                    case 1:
                        hitC = HitChance.Low;
                        break;
                    case 2:
                        hitC = HitChance.Medium;
                        break;
                    case 3:
                        hitC = HitChance.High;
                        break;
                    case 4:
                        hitC = HitChance.VeryHigh;
                        break;
                }
            }
            else if (Source == "Harass")
            {
                switch (harassQHit)
                {
                    case 1:
                        hitC = HitChance.Low;
                        break;
                    case 2:
                        hitC = HitChance.Medium;
                        break;
                    case 3:
                        hitC = HitChance.High;
                        break;
                    case 4:
                        hitC = HitChance.VeryHigh;
                        break;
                }
            }

            return hitC;
        }
        public void AddManaManagertoMenu(Menu myMenu, String source, int standard)
        {
            myMenu.AddItem(new MenuItem(source + "_Manamanager", "Mana Manager").SetValue(new Slider(standard)));
        }

        public bool HasMana(string source)
        {
            if (GetManaPercent() > menu.Item(source + "_Manamanager").GetValue<Slider>().Value)
                return true;
            return false;
        }

        //to create by champ
        public virtual void Drawing_OnDraw(EventArgs args)
        {
            //for champs to use
        }

        public virtual void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            //for champs to use
        }

        public virtual void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            //for champs to use
        }

        public virtual void Game_OnGameUpdate(EventArgs args)
        {
            //for champs to use
        }

        public virtual void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            //for champs to use
        }

        public virtual void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            //for champs to use
        }

        public virtual void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            //for champ use
        }

        public virtual void Game_OnSendPacket(GamePacketEventArgs args)
        {
            //for champ use
        }
    }
}
