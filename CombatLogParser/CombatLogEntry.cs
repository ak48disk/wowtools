using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatLogParser
{
    public static class ParseHelper
    {
        public static T ParseValue<T>(string source)
        {
            source = source.Trim();
            if (source[0] == '"')
                return (T)Convert.ChangeType(source.Substring(1, source.Length - 2), typeof(T));
            return (T)Convert.ChangeType(source, typeof(T));
        }

        public static UInt64 ParseGUID(string source)
        {
            return Convert.ToUInt64(source, 16);
        }

        public static uint ParseHexValue(string source)
        {
            return Convert.ToUInt32(source, 16);
        }

        public static bool ParseBool(string s)
        {
            if (s == "nil") return false;
            if (int.Parse(s) != 0)
                return true;
            return false;
        }
    }

    public enum CombatEventType
    {

    }
    public class SpellInfo
    {
        public uint SpellId { get; private set; }
        public string SpellName { get; private set; }
        public uint SpellSchool { get; private set; }
        public SpellInfo(string[] param,int index)
        {
            SpellId = ParseHelper.ParseValue<uint>(param[0+index]);
            SpellName = ParseHelper.ParseValue<string>(param[1+index]);
            SpellSchool = ParseHelper.ParseHexValue(param[2+index]);
        }
    }
    public class DamageInfo
    {
        public uint Amount { get; private set; }
        public int Overkill { get; private set; }
        public uint School { get; private set; }
        public int Resisted { get; private set; }
        public int Blocked { get; private set; }
        public int Absorbed { get; private set; }
        public bool Critical { get; private set; }
        public bool Glancing { get; private set; }
        public bool Crushing { get; private set; }
        public DamageInfo(string[] param,int index)
        {
            Amount = ParseHelper.ParseValue<uint>(param[index + 0]);
            Overkill = ParseHelper.ParseValue<int>(param[index + 1]);
            School = ParseHelper.ParseValue<uint>(param[index + 2]);
            Resisted = ParseHelper.ParseValue<int>(param[index + 3]);
            Blocked = ParseHelper.ParseValue<int>(param[index + 4]);
            Absorbed = ParseHelper.ParseValue<int>(param[index + 5]);
            Critical = ParseHelper.ParseBool(param[index + 6]);
            Glancing = ParseHelper.ParseBool(param[index + 7]);
            Crushing = ParseHelper.ParseBool(param[index + 8]);
        }
    }
    public class Unit
    {
        public UInt64 Guid { get; private set; }
        public string Name { get; private set; }
        public uint Flags { get; private set; }
        public uint RaidFlags { get; private set; }
        public enum UnitType
        {
            Object,
            Guardian,
            Pet,
            NPC,
            Player,
            Unknown
        }
        public enum UnitController
        {
            NPC,
            Player,
        }
        public enum UnitReaction
        {
            Hostile,
            Neutral,
            Friendly,
            Unknown
        }
        public UnitType Type
        {
            get
            {
                if ((Flags & 0x4000) != 0) return UnitType.Object;
                if ((Flags & 0x2000) != 0) return UnitType.Guardian;
                if ((Flags & 0x1000) != 0) return UnitType.Pet;
                if ((Flags & 0x800) != 0) return UnitType.NPC;
                if ((Flags & 0x400) != 0) return UnitType.Player;
                return UnitType.Unknown;
            }
        }
        public UnitReaction Reaction
        {
            get
            {
                if ((Flags & 0x40) != 0) return UnitReaction.Hostile;
                if ((Flags & 0x20) != 0) return UnitReaction.Neutral;
                if ((Flags & 0x10) != 0) return UnitReaction.Friendly;
                return UnitReaction.Unknown;
            }
        }
        public Unit(string[] param,int index)
        {
            Guid = ParseHelper.ParseGUID(param[index + 0]);
            Name = ParseHelper.ParseValue<string>(param[index + 1]);
            Flags = ParseHelper.ParseHexValue(param[index + 2]);
            RaidFlags = ParseHelper.ParseHexValue(param[index + 3]);
        }
        public override bool Equals(object obj)
        {
            return (obj is Unit) && ((obj as Unit).Guid.Equals(Guid));
        }
        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }
    }

    public class MiscInfo
    {
        public UInt64 AffectingUnitGuid { get; private set; }
        public uint HealthAfterEvent { get; private set; }
        public uint ZoneID { get; private set; }
        public uint ClassId { get; private set; }
        public uint PowerId { get; private set; }
        public uint PowerAfterEvent { get; private set; }
        public MiscInfo(string[] param,int index)
        {
            AffectingUnitGuid = ParseHelper.ParseGUID(param[index + 0]);
            HealthAfterEvent = ParseHelper.ParseValue<uint>(param[index + 1]);
            ZoneID = ParseHelper.ParseValue<uint>(param[index + 2]);
            ClassId = ParseHelper.ParseValue<uint>(param[index + 3]);
            PowerId = ParseHelper.ParseValue<uint>(param[index + 4]);
            PowerAfterEvent = ParseHelper.ParseValue<uint>(param[index + 5]);
        }
    }

    public class HealInfo
    {
        public uint Amount { get; private set; }
        public int Absorbed { get; private set; }
        public uint Effective { get { return Amount - OverHealing; } }
        public uint OverHealing { get; private set; }
        public bool Critical { get; private set; }
        public HealInfo(string[] param, int index)
        {
            Amount = ParseHelper.ParseValue<uint>(param[index + 0]);
            OverHealing = ParseHelper.ParseValue<uint>(param[index + 1]);
            Absorbed = ParseHelper.ParseValue<int>(param[index + 2]);
            Critical = ParseHelper.ParseBool(param[index + 3]);
        }
    }

    public class OtherInfo
    {
        public uint Amount { get; private set; }
        public OtherInfo(string type, string[] param, ref int index)
        {
            if (type.EndsWith("AURA_APPLIED") ||
                type.EndsWith("AURA_REMOVED") ||
                type.EndsWith("AURA_REFRESH")
                )
            {
                index++;
                if (param.Length > index)
                    Amount = ParseHelper.ParseValue<uint>(param[index++]);
            }
        }
    }
    
    public class RawCombatLogEntry
    {
        public RawCombatLogEntry(DateTime time, string[] rawEntryValues)
        {
            this.Time = time;
            int index = 0;
            this.rawEntryValues = ParseCommonValues(rawEntryValues, ref index);
            if (eventType.StartsWith("SPELL_") || eventType.StartsWith("RANGE_") || eventType == "DAMAGE_SPLIT")
            {
                Spell = new SpellInfo(this.rawEntryValues,index);
                index += 3;
            }
            try
            {
                if (this.rawEntryValues.Length - index > 6 && eventType != "DAMAGE_SPLIT")
                {
                    Misc = new MiscInfo(this.rawEntryValues, index);
                    index += 6;
                }
            }
            catch (Exception)
            { }
            if (eventType.StartsWith("ENVIRONMENTAL_"))
            {
                index++;
            }
            if (eventType.EndsWith("_DAMAGE") || eventType.EndsWith("SHIELD"))
            {
                Damage = new DamageInfo(this.rawEntryValues,index);
                index += 9;
            }
            else if (eventType.EndsWith("_HEAL"))
            {
                Heal = new HealInfo(this.rawEntryValues, index);
                index += 4;
            }
            else
            {
                Other = new OtherInfo(eventType, this.rawEntryValues, ref index);
                //TODO ref index
            }
            this.rawEntryValues = this.rawEntryValues.Skip(index).ToArray();
        }

        public string RawEntryValue(int index)
        {
            return rawEntryValues[index];
        }

        public T RawEntryValue<T>(int index)
        {
            return (T)Convert.ChangeType(RawEntryValue(index), typeof(T));
        }

        private string[] ParseCommonValues(string[] rawEntryValues,ref int index)
        {
            eventType = ParseHelper.ParseValue<string>(rawEntryValues[index]);
            Source = new Unit(rawEntryValues,index + 1);
            Dest = new Unit(rawEntryValues,index + 5);
            /*affectingGUID = ParseGUID(rawEntryValues[9]);
            zoneID = ParseValue<uint>(rawEntryValues[10]);
            classID = ParseValue<uint>(rawEntryValues[11]);
            powerID = ParseValue<uint>(rawEntryValues[12]);
            powerAfterEvent = ParseValue<uint>(rawEntryValues[13]);*/
            index += 9;
            return rawEntryValues;
        }

        public DateTime Time { get; private set; }
        public string eventType { get; private set; }
        public Unit Source { get; private set; }
        public Unit Dest { get; private set; }
        /*public UInt64 sourceGUID { get; private set; }
        public string sourceName { get; private set; }
        public uint sourceFlags { get; private set; }
        public uint sourceRaidFlags { get; private set; }
        public UInt64 destGUID { get; private set; }
        public string destName { get; private set; }
        public uint destFlags { get; private set; }
        public uint destRaidFlags { get; private set; }*/
        public UInt64 affectingGUID { get; private set; }
        public uint affectingHealth { get; private set; }
        public uint zoneID { get; private set; }
        public uint classID { get; private set; }
        public uint powerID { get; private set; }
        public uint powerAfterEvent { get; private set; }
        public SpellInfo Spell { get; private set; }
        public DamageInfo Damage { get; private set; }
        public MiscInfo Misc { get; private set; }
        public HealInfo Heal { get; private set; }
        public OtherInfo Other { get; private set; }


        private string[] rawEntryValues;
    }
}
