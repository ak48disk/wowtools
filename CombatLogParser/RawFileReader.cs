using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatLogParser
{
    static class Transformer
    {
        public static RawCombatLogEntry Transform(string rawLine)
        {
            try
            {
                string[] splited = rawLine.Split(' ');
                RawCombatLogEntry entry;
                if (splited.Length < 3)
                    return null;
                entry = new RawCombatLogEntry(
                    DateTime.Parse("2013/" + string.Join(" ", splited[0], splited[1])),
                    string.Join(" ", splited.Skip(2)).Split(','));
                return entry;
            }
            catch (Exception)
            {
                Console.WriteLine(rawLine);
                return null;
            }
        }
    }

    class LogTransformer
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


    class RawFileReader
        :Pipe <IEnumerable<RawCombatLogEntry>,FileStream>
    {
        public override IEnumerable<RawCombatLogEntry> Process(FileStream input)
        {
            List<RawCombatLogEntry> retVal = new List<RawCombatLogEntry>();
            using (StreamReader tx = new StreamReader(input))
            {
                while (!tx.EndOfStream)
                {
                    string rawLine = tx.ReadLine();
                    try
                    {
                        RawCombatLogEntry entry = Transformer.Transform(rawLine);
                        if (entry == null)
                            continue;
                        retVal.Add(entry);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine(rawLine);
                    }
                }
            }
            return retVal;
        }
    }

    public class RawFileInlineReader
        : Pipe<IEnumerable<RawCombatLogEntry>, FileStream>
    {
      
        public class RawFileEnumerable
            : IEnumerable<RawCombatLogEntry>
        {
            public RawFileEnumerable(FileStream fs)
            {
                _fs = fs;
            }
            public class Enumerator
                : IEnumerator<RawCombatLogEntry>
            {
                public Enumerator(FileStream fs)
                {
                    _fs = fs;
                    Reset();
                }

                public RawCombatLogEntry Current
                {
                    get { return _current; }
                }

                public void Dispose()
                {

                }

                object System.Collections.IEnumerator.Current
                {
                    get { return _current; }
                }

                public bool MoveNext()
                {
                    while (!_tx.EndOfStream)
                    {
                        try
                        {
                            string str = _tx.ReadLine();
                            //_current = Transformer.Transform(str);
                            _current = _trans.Process(str);
                            return true;
                        }
                        catch (Exception)
                        {

                        }
                    }
                    return false;
                }

                public void Reset()
                {
                    _fs.Position = 0;
                    _tx = new StreamReader(_fs);
                }
                private FileStream _fs;
                private StreamReader _tx;
                private RawCombatLogEntry _current;
                private LogTransformer _trans = new LogTransformer();
            }
            public IEnumerator<RawCombatLogEntry> GetEnumerator()
            {
                return new RawFileEnumerable.Enumerator(_fs);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return new RawFileEnumerable.Enumerator(_fs);
            }
            private FileStream _fs;
            
        }
        public override IEnumerable<RawCombatLogEntry> Process(FileStream input)
        {
            LogTransformer t = new LogTransformer();
            using (StreamReader tx = new StreamReader(input))
            {
                while (!tx.EndOfStream)
                {
                    string s;
                    s = tx.ReadLine();
                    var r = t.Process(s);
                    if (r != null)
                        yield return r;
                }
            }
            //return new RawFileEnumerable(input);
        }
    }
}
