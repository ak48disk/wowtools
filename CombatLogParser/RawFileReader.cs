using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatLogParser
{
    class LogLaxer
        : Pipe<RawCombatLogEntry, string>
    {
        public override RawCombatLogEntry Process(string input)
        {
            string dateTimeStr = null;
            string[] rawEntries = new string[30];
            int arrayLength = 30;
            int currentArrayIndex = 0;
            int startPos = 0;
            int endPos = 0;
            int strLength = input.Length;
            bool special = false;
            try
            {
                while (startPos < strLength)
                {
                    if (dateTimeStr == null)
                    {
                        if (input[endPos] == ' ')
                        {
                            if (!special)
                                special = true;
                            else
                            {
                                dateTimeStr = "2013/" + input.Substring(startPos, endPos - startPos);
                                startPos = endPos + 1;
                                endPos = startPos;
                                special = false;
                                continue;
                            }
                        }
                    }
                    else
                    {
                        if (endPos == strLength || (!special && input[endPos] == ','))
                        {
                            if (currentArrayIndex >= arrayLength)
                            {
                                string[] newEntries = new string[arrayLength * 2];
                                for (int i = 0; i < arrayLength; ++i)
                                    newEntries[i] = rawEntries[i];
                                rawEntries = newEntries;
                                arrayLength *= 2;
                            }
                            rawEntries[currentArrayIndex++] = input.Substring(startPos, endPos - startPos);
                            startPos = endPos + 1;
                            endPos = startPos;
                            continue;
                        }
                        else
                        {
                            if (input[endPos] == '"')
                                special = !special;
                        }
                    }
                    endPos++;
                }
                return new RawCombatLogEntry(DateTime.Parse(dateTimeStr),
                    rawEntries.Take(currentArrayIndex).ToArray());
            }
            catch (Exception)
            {
                Console.WriteLine(input);
                return null;
            }
        }
    }


    public class RawFileInlineReader
        : Pipe<IEnumerable<RawCombatLogEntry>, FileStream>
    {
        public override IEnumerable<RawCombatLogEntry> Process(FileStream input)
        {
            LogLaxer lex = new LogLaxer();
            using (StreamReader tx = new StreamReader(input))
            {
                while (!tx.EndOfStream)
                {
                    var logEntry = lex.Process(tx.ReadLine());
                    if (logEntry != null)
                        yield return logEntry;
                }
            }
        }
    }
}
