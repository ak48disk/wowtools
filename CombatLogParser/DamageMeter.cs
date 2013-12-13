using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatLogParser
{
    public class DamageMeter
        : Pipe<Dictionary<Unit,ulong>,Combat>
    {
        public override Dictionary<Unit, ulong> Process(Combat input)
        {
            return input.Events.Where(_ => (_.Source.Flags & 0x400) != 0).GroupBy(_ => _.Source.Guid)
                .ToDictionary(
                r => r.First().Source, r =>
                {
                    ulong result = 0;
                    foreach (var entry in r)
                    {
                        if (entry.eventType.EndsWith("_DAMAGE"))
                            if (entry.Damage != null)
                                result += entry.Damage.Amount;
                    }
                    return result;
                }
            );
        }
    }
}
