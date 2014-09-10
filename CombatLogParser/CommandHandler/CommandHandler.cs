using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatLogParser.CommandHandler
{
    public abstract class CommandHandler
    {
        public CommandHandler(object body)
        {
            _body = body;
        }
        public abstract void PreHandle();
        public abstract CommandHandler Handle(string command);
        private object _body;
    }
    public abstract class CommandHandler<T>
        : CommandHandler
    {
        public CommandHandler(T body)
            : base(body)
        {
            this.body = body;
        }
        protected T body;
    }
    public class CommandController
    {
        private CommandController()
        {
        }

        public void Handle(CommandHandler startHandler)
        {
            Stack<CommandHandler> commandStack = new Stack<CommandHandler>();
            commandStack.Push(startHandler);

            while (commandStack.Count > 0)
            {
                try
                {
                    CommandHandler currentHandler = commandStack.Peek();
                    currentHandler.PreHandle();
                    CommandHandler newHandler = currentHandler.Handle(Console.ReadLine());
                    if (newHandler == null)
                        commandStack.Pop();
                    else if (newHandler != currentHandler)
                        commandStack.Push(newHandler);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public static CommandController Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new CommandController();
                return _instance;
            }
        }
        private static CommandController _instance = null;
       // private static object _lock = new object();
    }
}
