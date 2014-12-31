using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatLogParser
{
    public class HealingInfo
    {
        public SpellInfo Spell { get; private set; }
        public uint TotalAmount { get; set; }
        public uint EffectiveAmount { get { return TotalAmount - OverHealingAmount; } }
        public uint OverHealingAmount { get; set; }
        public HealingInfo(SpellInfo spell, uint totalAmount, uint overHealingAmount)
        {
            this.Spell = spell;
            this.TotalAmount = totalAmount;
            this.OverHealingAmount = overHealingAmount;
        }
    }

    public class HealingMeter
        : Pipe<Dictionary<Unit, List<HealingInfo>>, Combat>
    {
        public override Dictionary<Unit, List<HealingInfo>> Process(Combat input)
        {
            return input.Events.Where(r => r.Heal != null).GroupBy(r => r.Source).ToDictionary(
                r => r.First().Source, r =>
                    {
                        return r.GroupBy(e => e.Spell).Select(e =>
                            {
                                UInt64 healAmount = 0;
                                UInt64 overHeal = 0;
                                UInt64 critHeal = 0;
                                foreach (var entry in e)
                                {
                                    var singleHealAmount = entry.Heal.Amount + (uint)entry.Heal.Absorbed;
                                    overHeal += entry.Heal.OverHealing;
                                    if (entry.Heal.Critical)
                                        critHeal += singleHealAmount;
                                    healAmount += singleHealAmount;
                                }
                                return new HealingInfo(e.First().Spell, (uint)healAmount, (uint)overHeal);
                            }).OrderBy(t => t.EffectiveAmount).ToList();
                    });
        }
    }

    public class AbsorbDetector
        : Pipe<Dictionary<Unit,List<HealingInfo>>,Combat>
    {
        public override Dictionary<Unit, List<HealingInfo>> Process(Combat input)
        {
            return input.Events.GroupBy(_ => _.Source.Guid)
                .ToDictionary(_ => _.First().Source, r =>
            {
                Dictionary<uint, HealingInfo> spells = new Dictionary<uint, HealingInfo>();
                Dictionary<Unit, Dictionary<uint, uint>> currentAmounts = new Dictionary<Unit, Dictionary<uint, uint>>();
                var AuraEvents = r.Where(_ => _.EventType == "SPELL_AURA_APPLIED" ||
                    _.EventType == "SPELL_AURA_REFRESH" || _.EventType == "SPELL_AURA_REMOVED");
                foreach (var entry in AuraEvents)
                {
                    if (!spells.ContainsKey(entry.Spell.SpellId))
                        spells[entry.Spell.SpellId] = new HealingInfo(entry.Spell, 0, 0);
                    if (!currentAmounts.ContainsKey(entry.Dest))
                        currentAmounts[entry.Dest] = new Dictionary<uint, uint>();
                    var currentAmount = currentAmounts[entry.Dest];
                    if (entry.EventType == "SPELL_AURA_APPLIED")
                    {
                        if (currentAmount.ContainsKey(entry.Spell.SpellId) &&
                            currentAmount[entry.Spell.SpellId] > 0)
                        {
                            spells[entry.Spell.SpellId].OverHealingAmount += currentAmount[entry.Spell.SpellId];
                            currentAmount[entry.Spell.SpellId] = 0;
                        }
                        currentAmount[entry.Spell.SpellId] = entry.Amount.Value;
                        spells[entry.Spell.SpellId].TotalAmount += entry.Amount.Value;
                    }
                    else if (entry.EventType == "SPELL_AURA_REMOVED")
                    {
                        if (!currentAmount.ContainsKey(entry.Spell.SpellId))
                            continue;
                        spells[entry.Spell.SpellId].OverHealingAmount += entry.Amount.Value;
                        currentAmount[entry.Spell.SpellId] = 0;
                    }
                    else if (entry.EventType == "SPELL_AURA_REFRESH")
                    {
                        if (!currentAmount.ContainsKey(entry.Spell.SpellId))
                            currentAmount[entry.Spell.SpellId] = 0;
                        if (entry.Amount.Value > currentAmount[entry.Spell.SpellId])
                            spells[entry.Spell.SpellId].TotalAmount += (entry.Amount.Value - currentAmount[entry.Spell.SpellId]);
                        currentAmount[entry.Spell.SpellId] = entry.Amount.Value;
                    }
                }
                foreach (var currentAmount in currentAmounts)
                {
                    foreach (var entry in currentAmount.Value)
                    {
                        spells[entry.Key].OverHealingAmount += entry.Value;
                    }
                }
                return spells.Where(_ => _.Value.TotalAmount > 0).Select(_ => _.Value).ToList();
            });
        }
    }
}
