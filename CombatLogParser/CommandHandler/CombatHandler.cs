using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatLogParser.CommandHandler
{
    public class CombatHandler
        : CommandHandler<Combat>
    {
        public CombatHandler(Combat body)
            :base(body)
        {
            _subCommandList = new Dictionary<string, Func<string[], CommandHandler<Combat>>>
            {
                {"bypeople", args =>  {
                    if (args.Length < 2)
                    {
                        foreach (var name in body.Players.Select(r => r.Name))
                            Console.WriteLine(name);
                        return this;
                    }
                    else
                    {
                        string playerName = args[1];
                        var player = body.Players.Where(r => r.Name == playerName).FirstOrDefault();
                        if (player == null)
                            return this;
                        else
                            return new ByPeopleCommandHandler(body, player);
                    }
                }},
            };
        }
        public override void PreHandle()
        {
            foreach (var kvp in _subCommandList)
                Console.WriteLine(kvp.Key);
        }
        public override CommandHandler Handle(string command)
        {
            string[] commandSplit = command.Split(' ');
            Func<string[], CommandHandler<Combat>> newHandlerFunc;
            if (_subCommandList.TryGetValue(commandSplit[0], out newHandlerFunc))
                return newHandlerFunc(commandSplit);
            else
            {
                if (command == "exit")
                    return null;
                return this;
            }
        }
        private Dictionary<string, Func<string[],CommandHandler<Combat>>> _subCommandList;
    }

    public class ByPeopleCommandHandler
        : CommandHandler<Combat>
    {
        public sealed class DamageCommandHandler
            : CommandHandler<Combat>
        {
            public DamageCommandHandler(Combat body,Unit unit)
                :base (body)
            {
                this.unit = unit;
            }
            public override void PreHandle()
            {
                
            }
            public override CommandHandler Handle(string command)
            {
                var groupBySpell = body.Events.Where(e =>e.Source == unit && e.Damage != null && e.Damage.Amount > 0).GroupBy(
                    r => r.Spell == null ? "Attack" : r.Spell.SpellName).ToDictionary(
                    r => r.First().Spell == null ? "Attack" : r.First().Spell.SpellName,
                    r =>
                    {
                        UInt64 totalDamage = 0;
                        int damageCount = 0;
                        int critDamageCount = 0;
                        foreach (var entry in r)
                        {
                            damageCount++;
                            if (entry.Damage.Critical) critDamageCount++;
                            totalDamage += entry.Damage.Amount;
                        }
                        return new {totalDamage = totalDamage, damageCount = damageCount, critDamageCount = critDamageCount };
                    }).OrderBy(r => r.Value.totalDamage);
                Console.WriteLine("Spell\tDamage\tCount\tCritPrecentage");
                foreach (var entry in groupBySpell)
                {
                    Console.WriteLine("{0}\t{1}\t{2}\t{3}",
                        entry.Key, entry.Value.totalDamage, entry.Value.damageCount,
                        (float)entry.Value.critDamageCount / (float)entry.Value.damageCount);
                }
                Console.WriteLine(groupBySpell.Sum(r=> (Int64)r.Value.totalDamage));
                return null;
            }
            private Unit unit;
        }
        public sealed class HealingCommandHandler
            : CommandHandler<Combat>
        {
            public HealingCommandHandler(Combat body,Unit unit)
                : base(body)
            {
                this.unit = unit;
            }
            public override void PreHandle()
            {
                
            }
            public override CommandHandler Handle(string command)
            {
                var absorbs = (new AbsorbDetector()).Process(body).Single(r => r.Key == unit).Value;
                return null;
            }
            private Unit unit;
        }
        public ByPeopleCommandHandler(Combat body, Unit unit)
            : base(body)
        {
            player = unit;
        }
        public override void PreHandle()
        {
          
        }
        public override CommandHandler Handle(string command)
        {
            if (command == "damage")
                return new DamageCommandHandler(body, player);
            else if (command == "healing")
                return new HealingCommandHandler(body, player);
            else if (command.StartsWith( "buff"))
            {
                var split = command.Split(' ');
                if (split.Length > 1)
                {
                    var datetime = DateTime.Parse(command.Replace("buff ",""));
                    HashSet<string> curr = new HashSet<string>();
                    foreach (var events in body.Player(player).Events.Where(r => (r.eventType == "SPELL_AURA_APPLIED" || r.eventType == "SPELL_AURA_REMOVED") && r.Time <= datetime))
                    {
                        if (events.eventType == "SPELL_AURA_APPLIED" && !curr.Contains(events.Spell.SpellName))
                            curr.Add(events.Spell.SpellName);
                        if (events.eventType == "SPELL_AURA_REMOVED" && curr.Contains(events.Spell.SpellName))
                            curr.Remove(events.Spell.SpellName);
                    }
                    foreach (var buff1 in curr)
                    {
                        Console.WriteLine(buff1);
                    }
                    return this;
                }
                var buffs = body.Player(player).Events.Where(r => r.eventType == "SPELL_AURA_APPLIED").Select(r => String.Format("{0} {1}", r.Time, r.Spell.SpellName));
                int i = 0;
                foreach (var buff in buffs)
                {
                    ++i;
                    if (i > 20)
                    {
                        Console.ReadLine();
                        i = 0;
                    }
                    Console.WriteLine(buff);
                }
                return this;
            }
            else if (command == "taken")
            {
                var events = body.Player(player).Events.Where(r => r.eventType == "SPELL_DAMAGE" && r.Dest == player).Select(r => String.Format("{0} {1} {2} {3}", r.Time, r.Spell.SpellName, r.Damage.Amount, r.Damage.Absorbed));
                foreach (var buff in events)
                    Console.WriteLine(buff);
                return this;
            }
            else if (command == "exit")
                return null;
            return this;
        }
        private Unit player;
    }
}
