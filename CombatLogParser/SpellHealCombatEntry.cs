using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatLogParser
{
    public class SpellHealCombatEntry
    {
    }
    public class SpellHealGenerator
        : Pipe<SpellHealCombatEntry, RawCombatLogEntry>
    {
        public override SpellHealCombatEntry Process(RawCombatLogEntry input)
        {
            return null;
        }
    }
}
