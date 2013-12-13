using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatLogParser
{
    public class Combat
    {
        public Combat(List<RawCombatLogEntry> events)
        {
            this.events = events;
            mainTarget = new Lazy<string>(
                () =>
                {
                    if (Parent != null)
                        return Parent.MainTarget;
                    var seq = events.Where(_ => _.eventType == "SPELL_DAMAGE").GroupBy(_ => _.Dest.Guid).OrderByDescending(
                r => r.Sum(_ => _.Damage.Amount)).FirstOrDefault();
                    if (seq != null)
                        return seq.First().Dest.Name;
                    else
                        return "Nothing";
                });
            duration = new Lazy<TimeSpan>(
                () => { if (events.Count == 0 ) return new TimeSpan(0);
                return events.Last().Time - events.First().Time;
                });
        }

        public Combat(List<RawCombatLogEntry> events, Combat parent)
            : this(events)
        {
            Parent = parent;
        }

        public Combat Player(Unit targetPlayer)
        {
            return new Combat(events.Where(_ => _.Source.Guid == targetPlayer.Guid ||
                _.Dest.Guid == targetPlayer.Guid).ToList(),this);
        }

        public Combat Between(DateTime start, DateTime end)
        {
            return new Combat(events.Where(_ => _.Time >= start && _.Time <= end).ToList(),this);
        }

        public Combat Between(RawCombatLogEntry start, RawCombatLogEntry end)
        {
            List<RawCombatLogEntry> entries = new List<RawCombatLogEntry>();
            bool startFound = false;
            foreach (var entry in events)
            {
                if (entry == start)
                    startFound = true;
                if (startFound)
                    entries.Add(entry);
                if (entry == end)
                    break;
            }
            return new Combat(entries, this);
        }

        private List<RawCombatLogEntry> events;
        public IReadOnlyList<RawCombatLogEntry> Events { get { return events.AsReadOnly(); } }
        public Lazy<string> mainTarget;
        public string MainTarget { get { return mainTarget.Value; } }
        private Lazy<TimeSpan> duration;
        public TimeSpan Duration { get { return duration.Value; } }
        public Combat Parent { get; private set; }
    }
}
