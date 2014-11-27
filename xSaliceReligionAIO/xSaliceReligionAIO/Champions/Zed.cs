using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Microsoft.Win32;
using SharpDX;
using Color = System.Drawing.Color;

namespace xSaliceReligionAIO.Champions
{
    class Zed : Champion
    {
        public Zed()
        {
            LoadSpells();
            LoadMenu();
        }

        private Vector3 CurrentWShadow = Vector3.Zero;
        private Vector3 CurrentRShadow = Vector3.Zero;

        private void LoadSpells()
        {
            Q = new Spell(SpellSlot.Q, 900f);
            W = new Spell(SpellSlot.W, 550f);
            E = new Spell(SpellSlot.E, 270f);
            R = new Spell(SpellSlot.R, 650f);

            Q.SetSkillshot(250f, 50f, 1700f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0f, 270f, float.MaxValue, false, SkillshotType.SkillshotCircle);
        }

        private void LoadMenu()
        {
            var key = new Menu("Key", "Key");
            {
                key.AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));
                key.AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
                key.AddItem(new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("N".ToCharArray()[0], KeyBindType.Toggle)));
                key.AddItem(new MenuItem("LaneClearActive", "Farm!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
                key.AddItem(new MenuItem("LastHitQ", "Last hit with Q!").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));
                //add to menu
                menu.AddSubMenu(key);
            }

            var spellMenu = new Menu("SpellMenu", "SpellMenu");
            {
                var wMenu = new Menu("WMenu", "WMenu");
                {
                    wMenu.AddItem(new MenuItem("W_Require_QE", "Require both Q/W to hit Harass")).SetValue(false);
                    wMenu.AddItem(new MenuItem("W_Follow_Combo", "Follow W in Combo")).SetValue(false);
                    wMenu.AddItem(new MenuItem("W_Follow_Harass", "Follow W in Harass")).SetValue(false);
                    wMenu.AddItem(new MenuItem("useW_Health", "Use W swap if health below").SetValue(new Slider(25)));
                    spellMenu.AddSubMenu(wMenu);
                }

                var rMenu = new Menu("RMenu", "RMenu");
                {
                    rMenu.AddItem(new MenuItem("R_Place_line", "R Range behind target in Line").SetValue(new Slider(400, 250, 550)));
                    rMenu.AddItem(new MenuItem("R_Back", "R Swap if Enemy Is dead").SetValue(true));
                    rMenu.AddItem(new MenuItem("useR_Health", "Use R swap if health below").SetValue(new Slider(10)));
                    //rMenu.AddItem(new MenuItem("Dont_R_If", "Do not R if > enemy")).SetValue(new Slider(3, 1, 5));
                    spellMenu.AddSubMenu(rMenu);
                }
                //add to menu
                menu.AddSubMenu(spellMenu);
            }

            var combo = new Menu("Combo", "Combo");
            {
                combo.AddItem(new MenuItem("Combo_mode", "Combo Mode").SetValue(new StringList(new[] { "Normal", "Line Combo", "Coax" })));
                combo.AddItem(new MenuItem("Combo_Switch", "Switch mode Key").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
                combo.AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
                combo.AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
                combo.AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
                combo.AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
                combo.AddItem(new MenuItem("Ignite", "Use Ignite").SetValue(true));
                combo.AddItem(new MenuItem("Botrk", "Use Bilge/Botrk").SetValue(true));
                //add to menu
                menu.AddSubMenu(combo);
            }
            var harass = new Menu("Harass", "Harass");
            {
                harass.AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
                harass.AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
                harass.AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
                //add to menu
                menu.AddSubMenu(harass);
            }
            var farm = new Menu("LaneClear", "LaneClear");
            {
                farm.AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(true));
                farm.AddItem(new MenuItem("UseEFarm", "Use E").SetValue(true));
                farm.AddItem(new MenuItem("LaneClear_useE_minHit", "Use E if min. hit").SetValue(new Slider(2, 1, 6)));
                //add to menu
                menu.AddSubMenu(farm);
            }
            var misc = new Menu("Misc", "Misc");
            {
                misc.AddItem(new MenuItem("smartKS", "Use Smart KS System").SetValue(true));
                //add to menu
                menu.AddSubMenu(misc);
            }
            var drawMenu = new Menu("Drawing", "Drawing");
            {
                drawMenu.AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
                drawMenu.AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_W", "Draw W").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_R", "Draw R").SetValue(true));
                drawMenu.AddItem(new MenuItem("Current_Mode", "Draw current Mode").SetValue(true));

                var drawComboDamageMenu = new MenuItem("Draw_ComboDamage", "Draw Combo Damage").SetValue(true);
                drawMenu.AddItem(drawComboDamageMenu);
                Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
                Utility.HpBarDamageIndicator.Enabled = drawComboDamageMenu.GetValue<bool>();
                drawComboDamageMenu.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
                {
                    Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                };
                //add to menu
                menu.AddSubMenu(drawMenu);
            }
        }

        private float GetComboDamage(Obj_AI_Base target)
        {
            double comboDamage = 0;

            if (Q.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.Q);

            if (W.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.W);

            if (E.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.E);

            if (R.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.R);

            if (Items.CanUseItem(Bilge.Id))
                comboDamage += Player.GetItemDamage(target, Damage.DamageItems.Bilgewater);

            if (Items.CanUseItem(Botrk.Id))
                comboDamage += Player.GetItemDamage(target, Damage.DamageItems.Botrk);

            if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                comboDamage += Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);

            if ((target.Health / target.MaxHealth * 100) <= 50)
                comboDamage += CalcPassive(target);

            return (float)(comboDamage + Player.GetAutoAttackDamage(target) * 3);
        }

        public double CalcPassive(Obj_AI_Base target)
        {
            double dmg = 0;
            
            if (Player.Level > 16)
            {
                double hp = target.MaxHealth * .1;
                dmg += Player.CalcDamage(target, Damage.DamageType.Magical, hp);
            }
            else if (Player.Level > 6)
            {
                double hp = target.MaxHealth * .08;
                dmg += Player.CalcDamage(target, Damage.DamageType.Magical, hp);
            }
            else
            {
                double hp = target.MaxHealth * .06;
                dmg += Player.CalcDamage(target, Damage.DamageType.Magical, hp);
            }

            return dmg;
        }

        private void Combo()
        {
            Combo(menu.Item("UseQCombo").GetValue<bool>(), menu.Item("UseWCombo").GetValue<bool>(),
                menu.Item("UseECombo").GetValue<bool>(), menu.Item("UseRCombo").GetValue<bool>());
        }

        private void Harass()
        {
            Harass(menu.Item("UseQHarass").GetValue<bool>(), menu.Item("UseWHarass").GetValue<bool>(),
                 menu.Item("UseEHarass").GetValue<bool>());
        }

        private void Combo(bool useQ, bool useW, bool useE, bool useR)
        {
            int mode = menu.Item("Combo_mode").GetValue<StringList>().SelectedIndex;

            switch (mode)
            {
                case 0:

                    var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
                    if (qTarget != null)
                    {
                        if (GetComboDamage(qTarget) >= qTarget.Health && Ignite_Ready() && menu.Item("Ignite").GetValue<bool>())
                            Use_Ignite(qTarget);

                        if (menu.Item("Botrk").GetValue<bool>())
                        {
                            if (HasBuff(qTarget, "zedulttargetmark")) 
                                Use_Bilge(qTarget);

                            if (HasBuff(qTarget, "zedulttargetmark"))
                                Use_Botrk(qTarget);
                        }
                    }

                    if (useW)
                        Cast_W("Combo", useQ, useE);

                    if (!W.IsReady() || wSpell.ToggleState == 2)
                    {
                        if (useQ)
                        {
                            Cast_Q();
                        }

                        if (useE)
                            Cast_E();
                    }

                    if (WShadow == null)
                        return;

                    var target = SimpleTs.GetTarget(Q.Range + W.Range, SimpleTs.DamageType.Physical);
                    if(target == null)
                        return;

                    if (menu.Item("W_Follow_Combo").GetValue<bool>() && wSpell.ToggleState == 2 && Player.Distance(target) > WShadow.Distance(target))
                        W.Cast(packets());

                    break;
                case 1:
                    if(useR)
                        LineCombo(useQ, useE);
                    else
                        menu.Item("Combo_mode").SetValue(new StringList(new[] { "Normal", "Line Combo", "Coax" }));
                break;
                case 2:
                    CoaxCombo(useQ, useE);
                break;

            }
        }

        private int CoaxDelay;

        public void CoaxCombo(bool useQ, bool useE)
        {
            var target = SimpleTs.GetTarget(W.Range + Q.Range, SimpleTs.DamageType.Physical);

            if (target == null)
                return;

            if (getMarked() != null)
                target = getMarked();

            if (W.IsReady() && wSpell.ToggleState == 0)
            {
                Cast_W("Combo", useQ, useE);
                CoaxDelay = Environment.TickCount + 500;
                return;
            }

            if (WShadow == null)
                return;

            if (WShadow.Distance(target) > R.Range - 100)
            {
            }
            else
            {
                if (useQ && (QCooldown - Game.Time) > (qSpell.Cooldown / 3))
                    return;
                if (useE && !E.IsReady())
                    return;
            }

            if (WShadow != null && HasEnergy(Q.IsReady() && useQ, false, E.IsReady() && useE) && Environment.TickCount - CoaxDelay > 0)
            {
                if (wSpell.ToggleState == 2 && WShadow.Distance(target) < R.Range)
                {
                    W.Cast(packets());
                    Utility.DelayAction.Add(50, () => R.Cast(target, packets()));
                    Utility.DelayAction.Add(300, () => menu.Item("Combo_mode").SetValue(new StringList(new[] { "Normal", "Line Combo", "Coax" })));
                }
            }
        }

        public void LineCombo(bool useQ, bool useE)
        {
            var target = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Physical);
            if (target == null)
                return;

            if (getMarked() != null)
                target = getMarked();

            if (HasEnergy(Q.IsReady() && useQ, W.IsReady(), E.IsReady() && useE))
            {
                var pred = Prediction.GetPrediction(target, 250f);

                if (Environment.TickCount - R.LastCastAttemptT > Game.Ping && rSpell.ToggleState == 0 && W.IsReady())
                {
                    R.Cast(target, packets());
                    R.LastCastAttemptT = Environment.TickCount + 300;
                    return;
                }

                if (HasBuff(target, "zedulttargetmark"))
                {

                    if (wSpell.ToggleState == 0 && W.IsReady() && Environment.TickCount - R.LastCastAttemptT > 0 && Environment.TickCount - W.LastCastAttemptT > Game.Ping)
                    {
                        var dist = menu.Item("R_Place_line").GetValue<Slider>().Value;
                        var BehindVector = Player.ServerPosition - Vector3.Normalize(target.ServerPosition - Player.ServerPosition) * dist;
                        Game.PrintChat("dist: " + dist);

                        if ((useE && pred.Hitchance >= HitChance.Medium) ||
                            Q.GetPrediction(target).Hitchance >= HitChance.Medium)
                        {
                            W.Cast(BehindVector);
                            W.LastCastAttemptT = Environment.TickCount + 300;

                            if (useQ)
                                predWQ = Q.GetPrediction(target).CastPosition;
                            else
                                predWQ = Vector3.Zero;

                            if (useE)
                                willEHit = true;
                            else
                                willEHit = false;

                            Utility.DelayAction.Add(400, () => menu.Item("Combo_mode").SetValue(new StringList(new[] { "Normal", "Line Combo", "Coax" })));
                        }
                    }
                }
            }
        }

        public void CheckShouldSwap()
        {
            var wHP = menu.Item("useW_Health").GetValue<Slider>().Value;
            var rHP = menu.Item("useR_Health").GetValue<Slider>().Value;

            if (RShadow != null)
            {
                if (GetHealthPercent() < rHP && rSpell.ToggleState == 2 && countEnemiesNearPosition(RShadow.ServerPosition, 400) < 1)
                {
                    R.Cast(packets());
                    return;
                }
            }

            if (WShadow != null)
            {
                if (GetHealthPercent() < wHP && wSpell.ToggleState == 2 && countEnemiesNearPosition(WShadow.ServerPosition, 400) < 1)
                    W.Cast(packets());
            }
        }
        private void Harass(bool useQ, bool useW, bool useE)
        {
            //energy check
            if (HasEnergy(Q.IsReady() && useQ, W.IsReady() && useW, E.IsReady() && useE))
            {
                if (useW)
                    Cast_W("Harass", useQ, useE);

                if (useQ)
                {
                    if(useW && wSpell.ToggleState == 2)
                        Cast_Q();
                    else if(useW && !W.IsReady())
                        Cast_Q();
                    else if(!useW)
                        Cast_Q();
                }

                if (useE)
                    Cast_E();

                if (WShadow == null)
                    return;

                var target = SimpleTs.GetTarget(Q.Range + W.Range, SimpleTs.DamageType.Physical);
                if (target == null)
                    return;

                if (menu.Item("W_Follow_Harass").GetValue<bool>() && wSpell.ToggleState == 2 && Player.Distance(target) > WShadow.Distance(target))
                    W.Cast(packets());
            }
        }

        public Obj_AI_Hero getMarked()
        {
            return ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(x => x.IsValidTarget(W.Range + Q.Range) && HasBuff(x, "zedulttargetmark") && x.IsVisible);
        }

        public void SmartKs()
        {
            if (!menu.Item("smartKS").GetValue<bool>())
                return;

            foreach (Obj_AI_Hero target in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(W.Range + Q.Range) && !x.IsDead && !x.HasBuffOfType(BuffType.Invulnerability)))
            {
                //WQE
                if ((Player.GetSpellDamage(target, SpellSlot.Q) + Player.GetSpellDamage(target, SpellSlot.E)) > target.Health + 20 && W.IsReady() && Q.IsReady() && E.IsReady())
                {
                    Cast_W("Combo", true, true);
                }

                //WQ
                if (Q.IsKillable(target) && Player.Distance(target) > Q.Range && Q.IsReady() && W.IsReady()){
                    Cast_W("Combo", true, false);
                }
                //WE
                if (E.IsKillable(target) && Player.Distance(target) > E.Range && E.IsReady() && W.IsReady())
                {
                    Cast_W("Combo", false, true);
                }
                //Q
                if (Q.IsKillable(target) && Player.Distance(target) < Q.Range && Q.IsReady())
                {
                    Cast_Q(target);
                }
                //E
                if (E.IsKillable(target) && Player.Distance(target) < E.Range && E.IsReady())
                {
                    Cast_E(target);
                }
            }
        }

        public void Cast_Q(Obj_AI_Hero forceTarget = null)
        {
            var target = SimpleTs.GetTarget(Q.Range + W.Range, SimpleTs.DamageType.Physical);

            if (getMarked() != null)
                target = getMarked();

            if (forceTarget != null)
                target = forceTarget;

            if (target == null || !Q.IsReady())
                return;

            if (WShadow != null && RShadow != null)
            {
                var predW = GetP2(WShadow.ServerPosition, Q, target, true);
                var predR = GetP2(RShadow.ServerPosition, Q, target, true);
                var pred = Q.GetPrediction(target, true);

               if (pred.Hitchance >= HitChance.High)
                   Q.Cast(target, packets());

                if(predW.Hitchance >= HitChance.High)
                    Q.Cast(predW.CastPosition, packets());

                if(predR.Hitchance >= HitChance.High)
                    Q.Cast(predR.CastPosition, packets());
            }
            else if (WShadow != null)
            {
                var predW = GetP2(WShadow.ServerPosition, Q, target, true);
                var pred = Q.GetPrediction(target, true);

                if (predW.Hitchance >= HitChance.High)
                    Q.Cast(predW.CastPosition, packets());

                if (pred.Hitchance >= HitChance.High)
                    Q.Cast(target, packets());

            }
            else if (RShadow != null)
            {
                var predR = GetP2(RShadow.ServerPosition, Q, target, true);
                var pred = Q.GetPrediction(target, true);

                if (pred.Hitchance >= HitChance.High)
                    Q.Cast(target, packets());

                if (predR.Hitchance >= HitChance.High)
                    Q.Cast(predR.CastPosition, packets());
            }
            else
            {
                CastBasicSkillShot(Q, Q.Range,SimpleTs.DamageType.Physical, HitChance.High);
            }
        }

        public void Cast_E(Obj_AI_Hero forceTarget = null)
        {
            var target = SimpleTs.GetTarget(E.Range + W.Range, SimpleTs.DamageType.Physical);

            if (getMarked() != null)
                target = getMarked();

            if (forceTarget != null)
                target = forceTarget;

            if (target == null || !E.IsReady())
                return;

            if (WShadow != null && RShadow != null)
            {
                var predW = GetPCircle(WShadow.ServerPosition, E, target, true);
                var predR = GetPCircle(RShadow.ServerPosition, E, target, true);
                var pred = E.GetPrediction(target, true);

                if (pred.Hitchance >= HitChance.High && Player.Distance(target) < E.Range)
                    E.Cast(packets());

                if (predW.Hitchance >= HitChance.High && WShadow.Distance(target) < E.Range)
                    E.Cast(packets());

                if (predR.Hitchance >= HitChance.High && RShadow.Distance(target) < E.Range)
                    E.Cast(packets());
            }
            else if (WShadow != null)
            {
                var predW = GetPCircle(WShadow.ServerPosition, E, target, true);
                var pred = E.GetPrediction(target, true);

                if (predW.Hitchance >= HitChance.High && WShadow.Distance(target) < E.Range)
                    E.Cast(packets());

                if (pred.Hitchance >= HitChance.High && Player.Distance(target) < E.Range)
                    E.Cast(packets());

            }
            else if (RShadow != null)
            {
                var predR = GetPCircle(RShadow.ServerPosition, E, target, true);
                var pred = E.GetPrediction(target, true);

                if (pred.Hitchance >= HitChance.High && Player.Distance(target ) < E.Range)
                    E.Cast(packets());

                if (predR.Hitchance >= HitChance.High && RShadow.Distance(target) < E.Range)
                    E.Cast(packets());
            }
            else
            {
                if (E.GetPrediction(target).Hitchance >= HitChance.High && Player.Distance(target) < E.Range)
                    E.Cast(packets());
            }
        }

        private Vector3 predWQ;
        private bool willEHit;
        public void Cast_W(string source, bool useQ, bool useE)
        {
            var target = SimpleTs.GetTarget(Q.Range + W.Range, SimpleTs.DamageType.Physical);

            if (target == null)
                return;

            if (getMarked() != null)
                target = getMarked();

            if (wSpell.ToggleState == 0 && W.IsReady() && Environment.TickCount - W.LastCastAttemptT > Game.Ping)
            {
                if (Player.Distance(target) < W.Range + target.BoundingRadius)
                {
                    var pred = Prediction.GetPrediction(target, 250f);

                    if ((useQ ? Q.IsReady() : true) && (useE ? E.IsReady() : true))
                    {
                        if ((pred.Hitchance >= HitChance.Medium && Q.GetPrediction(target).Hitchance >= HitChance.Medium))
                        {
                            W.Cast(pred.UnitPosition);
                            W.LastCastAttemptT = Environment.TickCount + 300;

                            if (useQ)
                                predWQ = pred.CastPosition;
                            else
                                predWQ = Vector3.Zero;

                            if (useE && pred.UnitPosition.Distance(target.ServerPosition) < E.Range + target.BoundingRadius)
                                willEHit = true;
                            else
                                willEHit = false;
                        }
                    }
                }
                else
                {
                    var pred = Prediction.GetPrediction(target, 250f);
                    var predE = Prediction.GetPrediction(target, 250f);
                    var vec = Player.ServerPosition + Vector3.Normalize(pred.UnitPosition - Player.ServerPosition)*W.Range;

                    if (IsWall(vec.To2D()))
                        return;

                    if ((useQ ? Q.IsReady() : true) && (useE ? E.IsReady() : true))
                    {
                        if ((pred.Hitchance >= HitChance.Medium || Q.GetPrediction(target).Hitchance >= HitChance.Medium) || (predE.Hitchance >= HitChance.Medium))
                        {
                            if (useQ && useE)
                            {
                                if (menu.Item("W_Require_QE").GetValue<bool>())
                                {
                                    if (useQ && (useE && vec.Distance(target.ServerPosition) < E.Range))
                                    {
                                        W.Cast(vec);
                                        W.LastCastAttemptT = Environment.TickCount + 300;
                                    }
                                }
                            }
                            else if (useQ || (useE && vec.Distance(target.ServerPosition) < E.Range + target.BoundingRadius))
                            {
                                W.Cast(vec);
                                W.LastCastAttemptT = Environment.TickCount + 300;
                            }
                            else if(!useQ && !useE)
                            {
                                W.Cast(vec);
                                W.LastCastAttemptT = Environment.TickCount + 300;
                            }

                            if (useQ)
                                predWQ = pred.CastPosition;
                            else
                                predWQ = Vector3.Zero;

                            if (useE && vec.Distance(target.ServerPosition) < E.Range)
                                willEHit = true;
                            else
                                willEHit = false;
                        }
                    }
                }
            }
        }

        public bool HasEnergy(bool q, bool w, bool e)
        {
            float energy = Player.Mana;
            float totalEnergy = 0;

            if (q)
                totalEnergy += Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost;
            if (w)
                totalEnergy += Player.Spellbook.GetSpell(SpellSlot.W).ManaCost;
            if (e)
                totalEnergy += Player.Spellbook.GetSpell(SpellSlot.E).ManaCost;

            if (energy >= totalEnergy)
                return true;

            return false;
        }

        private int _lasttick;
        private void ModeSwitch()
        {
            int mode = menu.Item("Combo_mode").GetValue<StringList>().SelectedIndex;
            int lasttime = Environment.TickCount - _lasttick;

            if (menu.Item("Combo_Switch").GetValue<KeyBind>().Active && lasttime > Game.Ping)
            {
                if (mode == 0)
                {
                    menu.Item("Combo_mode").SetValue(new StringList(new[] { "Normal", "Line Combo", "Coax" }, 1));
                    _lasttick = Environment.TickCount + 300;
                }
                else if (mode == 1)
                {
                    menu.Item("Combo_mode").SetValue(new StringList(new[] { "Normal", "Line Combo", "Coax" }, 2));
                    _lasttick = Environment.TickCount + 300;
                }
                else
                {
                    menu.Item("Combo_mode").SetValue(new StringList(new[] { "Normal", "Line Combo", "Coax" }));
                    _lasttick = Environment.TickCount + 300;
                }
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;

            ModeSwitch();
            SmartKs();
            CheckShouldSwap();

            if (menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                //if (menu.Item("LastHitQ").GetValue<KeyBind>().Active)
                   // Cast_Q(false);

                //if (menu.Item("LaneClearActive").GetValue<KeyBind>().Active)
                    //Farm();

                if (menu.Item("HarassActiveT").GetValue<KeyBind>().Active)
                    Harass();

                if (menu.Item("HarassActive").GetValue<KeyBind>().Active)
                    Harass();
            }
        }

        private float QCooldown;
        public override void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            if (!unit.IsMe)
                return;

            if (args.SData.Name == "ZedShuriken")
                QCooldown = Game.Time + qSpell.Cooldown;

            if (args.SData.Name == "ZedShadowDash")
            {
                if (W.LastCastAttemptT - Environment.TickCount > 0)
                {
                    if (predWQ != Vector3.Zero)
                        Q.Cast(predWQ, packets());

                    if (willEHit)
                        E.Cast(packets());
                }
            }
            if (args.SData.Name == "zedw2")
            {
                CurrentWShadow = Player.ServerPosition;
            }

            if (args.SData.Name == "zedult")
            {
                CurrentRShadow = Player.ServerPosition;
            }

            if (args.SData.Name == "ZedR2")
            {
                CurrentRShadow = Player.ServerPosition;
            }
        }

        private Obj_AI_Minion WShadow
        {
            get
            {
                if (CurrentWShadow == Vector3.Zero)
                    return null;
                if(RShadow != null)
                    return ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(minion => minion.IsVisible && minion.IsAlly && minion.Name == "Shadow" && minion != RShadow && minion.ServerPosition != RShadow.ServerPosition);

                return ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(minion => minion.IsVisible && minion.IsAlly && minion.Name == "Shadow");
            }
        }

        private Obj_AI_Minion RShadow
        {
            get
            {
                if (CurrentRShadow == Vector3.Zero)
                    return null;
                if(CurrentRShadow == Vector3.Zero)
                    return ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(minion => minion.IsVisible && minion.IsAlly && minion.Name == "Shadow");

                return ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(minion => minion.IsVisible && minion.IsAlly && minion.Name == "Shadow" && minion.Distance(CurrentRShadow) < 200);
            }
        }

        public override void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (!(sender is Obj_GeneralParticleEmmiter))
                return;

            if (sender.Name == "Zed_Base_W_cloneswap_buf.troy")
            {
                //Game.PrintChat("W shadow created " + sender.Type);
                CurrentWShadow = sender.Position;
            }

            if (sender.Name == "ZedUltMissile")
            {
                //CurrentRShadow = Player.ServerPosition;
            }

            if (sender.Name == "Zed_Base_R_buf_tell.troy")
            {
                if (rSpell.ToggleState == 2 && RShadow != null && menu.Item("R_Back").GetValue<bool>())
                    R.Cast(packets());
            }

        }

        public override void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (!(sender is Obj_GeneralParticleEmmiter))
                return;
            
            if (sender.Name == "Zed_Clone_idle.troy" && CurrentWShadow != Vector3.Zero && WShadow.Distance(sender.Position) < 100)
            {
                CurrentWShadow = Vector3.Zero;
            }
            

            if (RShadow != null)
            {
                if (sender.Name == "Zed_Clone_idle.troy" && CurrentRShadow != Vector3.Zero && RShadow.Distance(sender.Position) < 100)
                {
                    CurrentRShadow = Vector3.Zero;
                    //Game.PrintChat("R Deleted");
                }
            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {

            if (menu.Item("Draw_Disabled").GetValue<bool>())
                return;

            if (menu.Item("Draw_Q").GetValue<bool>())
                if (Q.Level > 0)
                    Utility.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_W").GetValue<bool>())
                if (W.Level > 0)
                    Utility.DrawCircle(Player.Position, W.Range - 2, W.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_E").GetValue<bool>())
                if (E.Level > 0)
                    Utility.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_R").GetValue<bool>())
                if (R.Level > 0)
                    Utility.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);

            if (WShadow != null)
            {
                Utility.DrawCircle(WShadow.Position, E.Range, Color.Aqua);
            }

            if (RShadow != null)
            {
                Utility.DrawCircle(RShadow.Position, E.Range, Color.Yellow);
            }

            if (menu.Item("Current_Mode").GetValue<bool>())
            {
                Vector2 wts = Drawing.WorldToScreen(Player.Position);
                int mode = menu.Item("Combo_mode").GetValue<StringList>().SelectedIndex;
                if (mode == 0)
                    Drawing.DrawText(wts[0] - 20, wts[1], Color.White, "Normal ");
                else if (mode == 1)
                    Drawing.DrawText(wts[0] - 20, wts[1], Color.White, "Line Combo");
                else if (mode == 2)
                    Drawing.DrawText(wts[0] - 20, wts[1], Color.White, "Coax");
            }
        }
    }
}
