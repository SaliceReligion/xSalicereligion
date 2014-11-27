﻿using System;
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
                var qMenu = new Menu("QMenu", "QMenu");
                {
                    qMenu.AddItem(new MenuItem("E_On_Killable", "E to KS").SetValue(true));
                    spellMenu.AddSubMenu(qMenu);
                }

                var wMenu = new Menu("WMenu", "WMenu");
                {
                    wMenu.AddItem(new MenuItem("useW_enemyCount", "Use W if x Enemys Arround")).SetValue(new Slider(3, 1, 5));
                    wMenu.AddItem(new MenuItem("useW_Health", "Use W if health below").SetValue(new Slider(25)));
                    spellMenu.AddSubMenu(wMenu);
                }

                var eMenu = new Menu("EMenu", "EMenu");
                {
                    eMenu.AddItem(new MenuItem("E_On_Killable", "E to KS").SetValue(true));
                    eMenu.AddItem(new MenuItem("E_Wait_Q", "Wait For Q").SetValue(true));
                    spellMenu.AddSubMenu(eMenu);
                }

                var rMenu = new Menu("RMenu", "RMenu");
                {
                    rMenu.AddItem(new MenuItem("R_Wait_For_Q", "Wait for Q Mark").SetValue(false));
                    rMenu.AddItem(new MenuItem("R_If_Killable", "R If Enemy Is killable").SetValue(true));
                    rMenu.AddItem(new MenuItem("Dont_R_If", "Do not R if > enemy")).SetValue(new Slider(3, 1, 5));
                    spellMenu.AddSubMenu(rMenu);
                }
                //add to menu
                menu.AddSubMenu(spellMenu);
            }

            var combo = new Menu("Combo", "Combo");
            {
                combo.AddItem(new MenuItem("Combo_mode", "Combo Mode").SetValue(new StringList(new[] { "Normal", "Q-R-AA-Q-E", "Q-Q-R-E-AA" })));
                combo.AddItem(new MenuItem("Combo_Switch", "Switch mode Key").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
                combo.AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
                combo.AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
                combo.AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
                combo.AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
                combo.AddItem(new MenuItem("Ignite", "Use Ignite").SetValue(true));
                combo.AddItem(new MenuItem("Bilge", "Use Bilge/Hextech").SetValue(true));
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

            return (float)(comboDamage + Player.GetAutoAttackDamage(target) * 1);
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
            if(useR)
                LineCombo(useQ, useE);

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
        }

        public void LineCombo(bool useQ, bool useE)
        {
            var target = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Physical);
            if (target == null)
                return;

            if (HasEnergy(Q.IsReady() && useQ, W.IsReady(), E.IsReady() && useE))
            {
                var pred = Prediction.GetPrediction(target, 250f);

                if (Environment.TickCount - R.LastCastAttemptT > Game.Ping && rSpell.ToggleState == 0)
                {
                    R.Cast(target, packets());
                    R.LastCastAttemptT = Environment.TickCount + 300;
                    return;
                }

                if (HasBuff(target, "zedulttargetmark"))
                {
                   
                    if (wSpell.ToggleState == 0 && W.IsReady() && Environment.TickCount - W.LastCastAttemptT > Game.Ping)
                    {
                        var BehindVector = Player.ServerPosition - Vector3.Normalize(target.ServerPosition - Player.ServerPosition) * W.Range;
                        if ((useE && pred.Hitchance >= HitChance.Medium) ||
                            Q.GetPrediction(target).Hitchance >= HitChance.Medium)
                        {
                            W.Cast(BehindVector);
                            W.LastCastAttemptT = Environment.TickCount + 100;
                            if (useQ)
                                Utility.DelayAction.Add(25, () => Q.Cast(pred.CastPosition, packets()));
                            if (useE)
                                Utility.DelayAction.Add(50, () => E.Cast(packets()));
                        }
                    }
                }
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
            }
        }

        public void Cast_Q()
        {
            var target = SimpleTs.GetTarget(Q.Range + W.Range, SimpleTs.DamageType.Physical);

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

        public void Cast_E()
        {
            var target = SimpleTs.GetTarget(E.Range + W.Range, SimpleTs.DamageType.Physical);

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

        public void Cast_W(string source, bool useQ, bool useE)
        {
            var target = SimpleTs.GetTarget(Q.Range + W.Range, SimpleTs.DamageType.Physical);

            if (target == null)
                return;

            if (wSpell.ToggleState == 0 && W.IsReady() && Environment.TickCount - W.LastCastAttemptT > Game.Ping)
            {
                if (Player.Distance(target) < W.Range)
                {
                    var pred = Prediction.GetPrediction(target, 250f);

                    if (Q.IsReady() && E.IsReady())
                    {
                        if ((pred.Hitchance >= HitChance.Medium || Q.GetPrediction(target).Hitchance >= HitChance.Medium))
                        {
                            W.Cast(pred.UnitPosition);
                            W.LastCastAttemptT = Environment.TickCount + 100;
                            Utility.DelayAction.Add(25, () => Q.Cast(pred.CastPosition, packets()));
                            Utility.DelayAction.Add(50, () => E.Cast(packets()));
                        }
                    }
                }
                else
                {
                    var pred = Prediction.GetPrediction(target, 500f);
                    var predE = Prediction.GetPrediction(target, 250f);
                    var vec = Player.ServerPosition + Vector3.Normalize(pred.UnitPosition - Player.ServerPosition)*W.Range;

                    if (IsWall(vec.To2D()))
                        return;

                    if (Q.IsReady() && E.IsReady())
                    {
                        if ((pred.Hitchance >= HitChance.Medium || Q.GetPrediction(target).Hitchance >= HitChance.Medium) &&
                            (predE.Hitchance >= HitChance.Medium && target.Distance(vec) < E.Range))
                        {
                            W.Cast(vec);
                            W.LastCastAttemptT = Environment.TickCount + 100;

                            Utility.DelayAction.Add(25, () => Q.Cast(pred.CastPosition, packets()));
                            Utility.DelayAction.Add(50, () => E.Cast(packets()));
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

        public override void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;


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

        public override void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            if (!unit.IsMe)
                return;

            //Game.PrintChat("Spell: " + args.SData.Name);

            if (args.SData.Name == "ZedShadowDash")
            {
                //E.Cast(packets());
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

                return ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(minion => minion.IsVisible && minion.IsAlly && minion.Name == "Shadow" && minion != RShadow);
            }
        }

        private Obj_AI_Minion RShadow
        {
            get
            {
                if (CurrentRShadow == Vector3.Zero)
                    return null;

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
                if (rSpell.ToggleState == 2 && RShadow != null)
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
            var target = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Physical);

            var pred = Prediction.GetPrediction(target, 250f);
            var behindVec = pred.UnitPosition + Vector3.Normalize(pred.UnitPosition - Player.ServerPosition) * (W.Range);

            Utility.DrawCircle(behindVec, 100, Color.Purple);

            if (WShadow != null)
            {
                Utility.DrawCircle(WShadow.Position, 200, Color.Aqua);
            }

            if (RShadow != null)
            {
                Utility.DrawCircle(RShadow.Position, 200, Color.Yellow);
            }

        }
    }
}