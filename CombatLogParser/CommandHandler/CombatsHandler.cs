using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatLogParser.CommandHandler
{
    public class CombatsHandler
        : CommandHandler<List<Combat>>
    {
        public CombatsHandler(List<Combat> body)
            :base(body)
        {
        }
        public override void PreHandle()
        {
            int i;
            for (i = 0; i < body.Count; ++i)
                Console.WriteLine(body[i].MainTarget,"\t" , body[i].Duration);
        }
        public override CommandHandler Handle(string command)
        {
            if (command == "exit")
                return null;
            int index = int.Parse(command);
            return new CombatHandler(body[index]);
        }
    }
}
