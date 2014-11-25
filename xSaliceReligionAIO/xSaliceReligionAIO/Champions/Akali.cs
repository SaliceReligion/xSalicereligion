using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace xSaliceReligionAIO.Champions
{
    class Akali : Champion
    {
        public Akali()
        {
            
        }

        private void SetSpells()
        {
            Q = new Spell(SpellSlot.Q, 600);

            W = new Spell(SpellSlot.W, 700);

            E = new Spell(SpellSlot.E, 325);

            R = new Spell(SpellSlot.R, 800);
        }

        private void LoadMenu()
        {
            var spellMenu = new Menu("SpellMenu", "SpellMenu");
            {
                var wMenu = new Menu("WMenu", "WMenu");
                {
                    wMenu.AddItem(new MenuItem("useW_enemyCount", "Use W if x Enemys Arround")).SetValue(new Slider(1, 1, 5));
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
                    spellMenu.AddSubMenu(rMenu);
                }
                //add to menu
                menu.AddSubMenu(spellMenu);
            }

            var combo = new Menu("Combo", "Combo");
            {
                combo.AddItem(new MenuItem("Combo_mode", "Combo Mode").SetValue(new StringList(new[] { "Normal", "Q-R-AA-Q-E", "Q-Q-R-E-AA" }, 0)));
                combo.AddItem(new MenuItem("Combo_Switch", "Switch mode Key").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
                combo.AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
                combo.AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
                combo.AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
                combo.AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
                combo.AddItem(new MenuItem("Ignite", "Use Ignite").SetValue(true));
                combo.AddItem(new MenuItem("Botrk", "Use BOTRK/Bilge").SetValue(true));
                //add to menu
                menu.AddSubMenu(combo);
            }
            var harass = new Menu("Harass", "Harass");
            {
                harass.AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
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
            var rStacks = GetRStacks();
            var comboDamage = 0d;
            int mode = menu.Item("Combo_mode").GetValue<StringList>().SelectedIndex;

            if (mode == 0)
            {
                if (Q.IsReady())
                    comboDamage += (Player.GetSpellDamage(target, SpellSlot.Q) +
                                    Player.CalcDamage(target, Damage.DamageType.Magical,
                                        (45 + 35 * Q.Level + 0.5 * Player.FlatMagicDamageMod)));
            }
            else if (Q.IsReady())
            {
                comboDamage += (Player.GetSpellDamage(target, SpellSlot.Q) + Player.CalcDamage(target, Damage.DamageType.Magical, (45 + 35 * Q.Level + 0.5 * Player.FlatMagicDamageMod))) * 2;
            }

            if (E.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.E);

            if (HasBuff(target, "AkaliMota"))
                comboDamage += Player.CalcDamage(target, Damage.DamageType.Magical, (45 + 35 * Q.Level + 0.5 * Player.FlatMagicDamageMod));

            comboDamage += Player.CalcDamage(target, Damage.DamageType.Magical, CalcPassiveDmg());

            if (Items.CanUseItem(Bilge.Id))
                comboDamage += Player.GetItemDamage(target, Damage.DamageItems.Bilgewater);

            if (Items.CanUseItem(Hex.Id))
                comboDamage += Player.GetItemDamage(target, Damage.DamageItems.Hexgun);

            if (Ignite_Ready())
                comboDamage += Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);

            if (rStacks > 0)
                comboDamage += Player.GetSpellDamage(target, SpellSlot.R) * rStacks;

            return (float)(comboDamage + Player.GetAutoAttackDamage(target));
        }
        private double CalcPassiveDmg()
        {
            return (0.06 + 0.01 * (Player.FlatMagicDamageMod / 6)) * (Player.FlatPhysicalDamageMod + Player.BaseAttackDamage);
        }

        private int GetRStacks()
        {
            return (from buff in Player.Buffs where buff.Name == "AkaliShadowDance" select buff.Count).FirstOrDefault();
        }

        private void Combo()
        {
            UseSpells(menu.Item("UseQCombo").GetValue<bool>(), menu.Item("UseWCombo").GetValue<bool>(),
                menu.Item("UseECombo").GetValue<bool>(), menu.Item("UseRCombo").GetValue<bool>(), "Combo");
        }

        private void Harass()
        {
            UseSpells(menu.Item("UseQHarass").GetValue<bool>(), false,
                false, menu.Item("UseEHarass").GetValue<bool>(), "Harass");
        }

        private void UseSpells(bool useQ, bool useW, bool useE, bool useR, string source)
        {

        }

        private Obj_AI_Hero CheckMark(float range)
        {
            return ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(x => x.IsValidTarget(range) && HasBuff(x, "AkaliMota") && x.IsVisible);
        }

        private void Cast_Q(bool combo, int mode = 0)
        {
            if (!Q.IsReady())
                return;
            if (combo)
            {
                var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
                if (!target.IsValidTarget(Q.Range))
                    return;

                if (CheckMark(Q.Range) != null)
                    target = CheckMark(Q.Range);

                if (mode == 0)
                {
                    Q.Cast(target, packets());
                }
                else if (mode == 1)
                {
                    if (!HasBuff(target, "AkaliMota"))
                        Q.Cast(target);
                }
                else if (mode == 2)
                {
                    Q.Cast(target);
                    if (HasBuff(target, "AkaliMota"))
                        Q.LastCastAttemptT = Environment.TickCount + 400;
                }
            }
            else
            {
                foreach (var minion in MinionManager.GetMinions(Player.Position, Q.Range).Where(minion => HasBuff(minion, "AkaliMota") && xSLxOrbwalker.InAutoAttackRange(minion)))
                    xSLxOrbwalker.ForcedTarget = minion;

                foreach (var minion in MinionManager.GetMinions(Player.Position, Q.Range).Where(minion => HealthPrediction.GetHealthPrediction(minion,
                        (int)(E.Delay + (minion.Distance(Player) / E.Speed)) * 1000) <
                                                             Player.GetSpellDamage(minion, SpellSlot.Q) &&
                                                             HealthPrediction.GetHealthPrediction(minion,
                                                                 (int)(E.Delay + (minion.Distance(Player) / E.Speed)) * 1000) > 0 &&
                                                             xSLxOrbwalker.InAutoAttackRange(minion)))
                    Q.Cast(minion);

                foreach (var minion in MinionManager.GetMinions(Player.Position, Q.Range).Where(minion => HealthPrediction.GetHealthPrediction(minion,
                        (int)(Q.Delay + (minion.Distance(Player) / Q.Speed))) <
                                                             Player.GetSpellDamage(minion, SpellSlot.Q)))
                    Q.Cast(minion);

                foreach (var minion in MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).Where(minion => Player.Distance(minion) <= Q.Range))
                    Q.Cast(minion);
            }
        }

        private void Cast_W()
        {
            if (menu.Item("useW_enemyCount").GetValue<Slider>().Value > Utility.CountEnemysInRange(400) &&
                menu.Item("useW_Health").GetValue<Slider>().Value < (int)(Player.Health / Player.MaxHealth * 100))
                return;
            W.Cast(Player.Position, packets());
        }

        private void Cast_E(bool combo, int mode = 0)
        {
            if (!E.IsReady())
                return;
            if (combo)
            {
                var target = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
                if (target == null || !target.IsValidTarget(E.Range))
                    return;

                if (CheckMark(E.Range) != null)
                    target = CheckMark(Q.Range);

                if (mode == 0)
                {
                    if (HasBuff(target, "AkaliMota") && !Q.IsReady())
                        E.Cast();
                    else if (E.IsKillable(target) && menu.Item("E_On_Killable").GetValue<bool>())
                        E.Cast();
                    else if (!menu.Item("E_Wait_Q").GetValue<bool>())
                        E.Cast();
                }
                else if (mode == 1)
                {
                    if (HasBuff(target, "AkaliMota") && xSLxOrbwalker.InAutoAttackRange(target))
                        xSLxOrbwalker.ForcedTarget = target;
                    else if (HasBuff(target, "AkaliMota") && !Q.IsReady())
                        E.Cast();
                    else if (E.IsKillable(target) && menu.Item("E_On_Killable").GetValue<bool>())
                        E.Cast();
                    else if (!menu.Item("E_Wait_Q").GetValue<bool>())
                        E.Cast();
                }
                else if (mode == 2)
                {
                    if (HasBuff(target, "AkaliMota"))
                    {
                        E.Cast();
                        menu.Item("Combo_mode").SetValue(new StringList(new[] { "Normal", "Q-R-AA-Q-E", "Q-Q-R-E-AA" }, 0));
                    }
                    else if (E.IsKillable(target) && menu.Item("E_On_Killable").GetValue<bool>())
                        E.Cast();
                }
            }
            else
            {
                if (MinionManager.GetMinions(Player.Position, E.Range).Count >= menu.Item("LaneClear_useE_minHit").GetValue<Slider>().Value)
                    E.Cast();
                foreach (var minion in MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All,
                    MinionTeam.Neutral, MinionOrderTypes.MaxHealth).Where(minion => Player.Distance(minion) <= E.Range))
                    E.Cast();
            }
        }

        private double getSimpleDmg(Obj_AI_Hero target)
        {
            double dmg = 0;

            if (Q.IsReady())
                dmg += Player.GetSpellDamage(target, SpellSlot.Q) + Player.GetSpellDamage(target, SpellSlot.Q, 1);
            if (HasBuff(target, "AkaliMota"))
                dmg += Player.GetSpellDamage(target, SpellSlot.Q, 1);
            if (E.IsReady())
                dmg += Player.GetSpellDamage(target, SpellSlot.E);
            if (R.IsReady())
                dmg += Player.GetSpellDamage(target, SpellSlot.R) * GetRStacks();

            return dmg;
        }

        private void Cast_R(int mode)
        {
            var target = SimpleTs.GetTarget(R.Range + Player.BoundingRadius, SimpleTs.DamageType.Magical);
            if (target == null)
                return;

            if (CheckMark(Q.Range) != null)
                target = CheckMark(R.Range);

            if (target.IsValidTarget(R.Range) && R.IsReady())
            {
                if (R.IsKillable(target) && menu.Item("R_If_Killable").GetValue<bool>())
                    R.Cast(target, packets());
                else if (getSimpleDmg(target) > target.Health)
                    R.Cast(target, packets());

                if (mode == 0)
                {
                    if (menu.Item("R_Wait_For_Q").GetValue<bool>() && HasBuff(target, "AkaliMota"))
                        R.Cast(target, packets());
                    else
                        R.Cast(target, packets());
                }
                else if (mode == 1)
                {
                    if (HasBuff(target, "AkaliMota") && Q.IsReady())
                    {
                        R.Cast(target, packets());
                        menu.Item("Combo_mode").SetValue(new StringList(new[] { "Normal", "Q-R-AA-Q-E", "Q-Q-R-E-AA" }, 0));
                    }
                }
                else if (mode == 2)
                {
                    if (HasBuff(target, "AkaliMota") && Environment.TickCount - Q.LastCastAttemptT < Game.Ping)
                    {
                        R.Cast(target, packets());
                    }
                }
            }
        }
    }
}
