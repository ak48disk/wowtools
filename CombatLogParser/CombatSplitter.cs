using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatLogParser
{
    public class CombatSplitter
        : Pipe<IEnumerable<Combat>,IEnumerable<RawCombatLogEntry>>
    {
        public override IEnumerable<Combat> Process(IEnumerable<RawCombatLogEntry> input)
        {
            List<RawCombatLogEntry> currentCombatEvents = new List<RawCombatLogEntry>();
            DateTime? lastSpellDamageEventTime = null;
            TimeSpan threshold = new TimeSpan(0, 0, 6);
            bool flag = false;
            foreach (var entry in input)
            {
                if (lastSpellDamageEventTime.HasValue &&
                        entry.Time - lastSpellDamageEventTime.Value > threshold)
                {
                    if (!flag)
                    {
                        yield return (new Combat(currentCombatEvents));
                        currentCombatEvents = new List<RawCombatLogEntry>();
                        flag = true;
                    }
                }
                if (entry.eventType == "SPELL_DAMAGE")
                {
                    if (flag)
                    {
                        yield return (new Combat(currentCombatEvents));
                        currentCombatEvents = new List<RawCombatLogEntry>();
                        flag = false;
                    }
                    lastSpellDamageEventTime = entry.Time;

                }

                currentCombatEvents.Add(entry);
            }
            if (currentCombatEvents.Count > 0)
                yield return (new Combat(currentCombatEvents));
        }
    }
}
