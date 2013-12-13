using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatLogParser
{
    class Program
    {
        static void Main(string[] args)
        {
            IEnumerable<RawCombatLogEntry> spellHeals;
            List<Combat> combats;
            using (FileStream fs = new FileStream(@"G:\World of Warcraft\Logs\WoWCombatLog3.txt", FileMode.Open))
            {
                var rw = new RawFileInlineReader().Then(new CombatSplitter());
                combats = rw.Process(fs).Where(_=>_.Duration > new TimeSpan(0,1,0) && _.MainTarget != "Nothing")
                    .ToList();
            }
            while (true)
            {
                for (int i = 0; i < combats.Count; ++i)
                {
                    Console.WriteLine("{0}\t{1}\t{2}", i, combats[i].MainTarget, combats[i].Duration);
                }
                string str = Console.ReadLine();
                
                int index = int.Parse(str);
                spellHeals = combats[index].Events.Where(_ => _.eventType.EndsWith ("_HEAL"));
                List<string> sourceNames = spellHeals.Select(_ => _.Source.Name).Distinct().ToList();
                for (int i = 0; i < sourceNames.Count; ++i)
                {
                    Console.WriteLine("{0}\t{1}", i, sourceNames[i]);
                }
                var absorbs = new AbsorbDetector().Process(combats[index]);
                var damages = new DamageMeter().Process(combats[index]).OrderByDescending(_ => _.Value).ToList();
                foreach (var dmgEntry in damages)
                {
                    Console.WriteLine("{0}\t{1}\t{2}", dmgEntry.Key.Name, dmgEntry.Value, (double)dmgEntry.Value / combats[index].Duration.TotalSeconds);
                }

                while (true)
                {
                    try
                    {
                        string s = Console.ReadLine();
                        if (s == "exit") break;

                        var combat = combats[index];
                        if (s.StartsWith("buff"))
                        {

                            IEnumerable<RawCombatLogEntry> buff = combat.Events.Where(r => r.eventType == "SPELL_AURA_APPLIED" &&
                                r.Source.Reaction != Unit.UnitReaction.Friendly &&
                                r.Dest.Reaction == Unit.UnitReaction.Friendly).OrderBy(r => r.Time);

                            var m = str.Split(' ');
                            if (m.Count() > 1)
                                buff = buff.Where(r => r.Spell.SpellId == int.Parse(m[1]));

                            foreach (var entry in buff)
                            {
                                Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}",
                                    entry.Time,
                                    entry.Spell.SpellId, entry.Spell.SpellName,
                                    entry.Source.Name, entry.Dest.Name);

                            }

                            continue;
                        }
                        if (s.StartsWith("cast"))
                        {

                            IEnumerable<RawCombatLogEntry> buff = combat.Events.Where(r => r.eventType == "SPELL_CAST_START" &&
                                
                                r.Source.Reaction != Unit.UnitReaction.Friendly).OrderBy(r => r.Time);

                            var m = s.Split(' ');
                            if (m.Count() > 1)
                                buff = buff.Where(r => r.Spell.SpellId == int.Parse(m[1]));

                            string p = "";
                            RawCombatLogEntry lastEntry = null;
                            foreach (var entry in buff)
                            {

                                p += string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}",
                                    (entry.Time - combat.Events.First().Time).ToString("g"),
                                    lastEntry == null ? 0.0 : (entry.Time - lastEntry.Time).TotalMilliseconds / 1000.0,
                                    entry.Spell.SpellId, entry.Spell.SpellName,
                                    entry.Source.Name, entry.Dest.Name);
                                lastEntry = entry;
                                p += Environment.NewLine;

                            }
                            Console.Write(p);
                            continue;
                        }



                        var heals = spellHeals.Where(_ => _.Source.Name == sourceNames[int.Parse(s)]);
                        var subHeals = heals.GroupBy(_ => _.Spell.SpellId).Select(_ =>
                            {
                                // 9 10 11 12
                                UInt64 healAmount = 0;
                                UInt64 overHeal = 0;
                                UInt64 critHeal = 0;
                                string spellName = _.First().Spell.SpellName;
                                foreach (var entry in _)
                                {
                                    var singleHealAmount = entry.Heal.Amount + (uint)entry.Heal.Absorbed;
                                    overHeal += entry.Heal.OverHealing;
                                    if (entry.Heal.Critical)
                                        critHeal += singleHealAmount;
                                    healAmount += singleHealAmount;
                                }
                                return new { spellName = spellName, healAmount = healAmount, overHeal = overHeal, critHeal = critHeal };
                            }).OrderByDescending(_ => _.healAmount);
                        var absorb = absorbs.Where(_ => _.Key.Name == sourceNames[int.Parse(s)]).First().Value.OrderBy(_ => _.EffectiveAmount);
                        foreach (var e in subHeals)
                        {
                            Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", e.spellName, e.healAmount - e.overHeal, e.overHeal, e.critHeal
                                , ((float)(e.healAmount - e.overHeal)) / e.healAmount * 100, (float)e.critHeal / (float)(e.healAmount - e.overHeal) * 100);
                        }
                        Console.WriteLine();
                        foreach (var e in absorb)
                        {
                            Console.WriteLine("{0}\t{1}\t{2}\t{3}", e.Spell.SpellName, e.EffectiveAmount, e.OverHealingAmount, (float)e.EffectiveAmount / e.TotalAmount);
                        }
                        var total = subHeals.Sum(_ => (int)_.healAmount - (int)_.overHeal) + absorb.Sum(_ => _.EffectiveAmount);
                        Console.WriteLine(total);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
                
            }
            Console.ReadKey();
        }
    }
}
