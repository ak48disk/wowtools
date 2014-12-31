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

        public static string ParseGUID(string source)
        {
            return source;
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

        public static Nullable<AuraType> ParseAuraType(string s)
        {
            if (s == "BUFF")
                return AuraType.BUFF;
            else if (s == "DEBUFF")
                return AuraType.DEBUFF;
            else
                return null;
        }
    }

    public static class UnitFlyweight
    {
        public static Unit TryGetUnit(string unitGUID)
        {
            Unit retVal;
            if (_unitDict.TryGetValue(unitGUID, out retVal))
                return retVal;
            else
                return null;
        }
        private static void AddUnit(Unit unit)
        {
            _unitDict.Add(unit.Guid,unit);
        }
        public static Unit ParseUnit(string[] param,ref int index)
        {
            string Guid = ParseHelper.ParseGUID(param[index + 0]);
            Unit retVal = TryGetUnit(Guid);
            if (retVal == null)
            {
                retVal = new Unit(param, ref index);
                AddUnit(retVal);
            }
            else
                index += Unit.NumberOfFields;
            return retVal;
        }
        private static Dictionary<string, Unit> _unitDict = new Dictionary<string, Unit>();
    }

    public class SpellInfo
    {
        public uint SpellId { get; private set; }
        public string SpellName { get; private set; }
        public uint SpellSchool { get; private set; }
        public SpellInfo(string[] param,ref int index)
        {
            SpellId = ParseHelper.ParseValue<uint>(param[index++]);
            SpellName = ParseHelper.ParseValue<string>(param[index++]);
            SpellSchool = ParseHelper.ParseHexValue(param[index++]);
        }
        public override bool Equals(object obj)
        {
            return (obj is SpellInfo) && ((SpellInfo)obj).SpellId == SpellId; 
        }
        public override int GetHashCode()
        {
            return SpellId.GetHashCode();
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
        public bool MultiStrike { get; private set; }
        public DamageInfo(string[] param,ref int index)
        {
            Amount = ParseHelper.ParseValue<uint>(param[index++]);
            Overkill = ParseHelper.ParseValue<int>(param[index++]);
            School = ParseHelper.ParseValue<uint>(param[index++]);
            Resisted = ParseHelper.ParseValue<int>(param[index++]);
            Blocked = ParseHelper.ParseValue<int>(param[index++]);
            Absorbed = ParseHelper.ParseValue<int>(param[index++]);
            Critical = ParseHelper.ParseBool(param[index++]);
            Glancing = ParseHelper.ParseBool(param[index++]);
            Crushing = ParseHelper.ParseBool(param[index++]);
            MultiStrike = ParseHelper.ParseBool(param[index++]);
        }
    }
    public class Unit
    {
        public string Guid { get; private set; }
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
        public Unit(string[] param,ref int index)
        {
            Guid = ParseHelper.ParseGUID(param[index + 0]);
            Name = ParseHelper.ParseValue<string>(param[index + 1]);
            Flags = ParseHelper.ParseHexValue(param[index + 2]);
            RaidFlags = ParseHelper.ParseHexValue(param[index + 3]);
            index += NumberOfFields;
        }
        public const int NumberOfFields = 4;
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
        public string AffectingUnitGUID { get; private set; }
        public Unit AffectingUnit
        {
            get
            {
                if (affectingUnit == null)
                    affectingUnit = UnitFlyweight.TryGetUnit(AffectingUnitGUID);
                return affectingUnit;
            }
        }
        private Unit affectingUnit;
        public uint HealthMax { get; private set; }
        public uint HealthAfterEvent { get; private set; }
        public uint AttackPower { get; private set; }
        public uint SpellPower { get; private set; }
        public uint Resolve { get; private set; }
        public int PowerType { get; private set; }
        public uint PowerAfterEvent { get; private set; }
        public uint PowerMax { get; private set; }
        public double MapX { get; private set; }
        public double MapY { get; private set; }
        public uint ItemLevel { get; private set; }
        public MiscInfo(string[] param,ref int index)
        {
            AffectingUnitGUID = ParseHelper.ParseGUID(param[index++]);
            HealthAfterEvent = ParseHelper.ParseValue<uint>(param[index++]);
            HealthMax = ParseHelper.ParseValue<uint>(param[index++]);
            AttackPower = ParseHelper.ParseValue<uint>(param[index++]);
            SpellPower = ParseHelper.ParseValue<uint>(param[index++]);
            Resolve = ParseHelper.ParseValue<uint>(param[index++]);
            PowerType = ParseHelper.ParseValue<int>(param[index++]);
            PowerAfterEvent = ParseHelper.ParseValue<uint>(param[index++]);
            PowerMax = ParseHelper.ParseValue<uint>(param[index++]);
            MapX = ParseHelper.ParseValue<double>(param[index++]);
            MapY = ParseHelper.ParseValue<double>(param[index++]);
            ItemLevel = ParseHelper.ParseValue<uint>(param[index++]);
        }
    }

    public class HealInfo
    {
        public uint Amount { get; private set; }
        public int Absorbed { get; private set; }
        public uint Effective { get { return Amount - OverHealing; } }
        public uint OverHealing { get; private set; }
        public bool Critical { get; private set; }
        public bool MultiStrike { get; private set; }
        public HealInfo(string[] param, ref int index)
        {
            Amount = ParseHelper.ParseValue<uint>(param[index++]);
            OverHealing = (uint)ParseHelper.ParseValue<uint>(param[index++]);
            Absorbed = ParseHelper.ParseValue<int>(param[index++]);
            Critical = ParseHelper.ParseBool(param[index++]);
            MultiStrike = ParseHelper.ParseBool(param[index++]);
        }
    }

    public class MissInfo
    {
        public string Reason { get; private set; }
        public bool OffHand { get; private set; }
        public bool MultiStrike { get; private set; }
        public uint Amount { get; private set; }
        public MissInfo(string[] param, ref int index)
        {
            Reason = param[index++];
            OffHand = ParseHelper.ParseBool(param[index++]);
            MultiStrike = ParseHelper.ParseBool(param[index++]);
            if (Reason == "ABSORB" || Reason == "BLOCK")
                Amount = ParseHelper.ParseValue<uint>(param[index++]);
        }
    }

    public enum AuraType
    {
        BUFF,
        DEBUFF,
    }

    public class RawCombatLogEntry
    {
        private void ParseEventType(string[] rawEntryValues, ref int index)
        {
            EventType = ParseHelper.ParseValue<string>(rawEntryValues[index++]);
        }

        private void ParseUnits(string[] rawEntryValues, ref int index)
        {
            Source = UnitFlyweight.ParseUnit(rawEntryValues, ref index);
            Dest = UnitFlyweight.ParseUnit(rawEntryValues, ref index);
        }

        private void ParseSpell(string[] rawEntryValues, ref int index)
        {
            if (EventType == "SPELL_ABSORBED")
            {
                uint spellId;
                if (!uint.TryParse(rawEntryValues[index], out spellId))
                    return;
            }
            if (EventType.StartsWith("SPELL_") || EventType.StartsWith("RANGE_") || EventType == "DAMAGE_SPLIT")
                Spell = new SpellInfo(rawEntryValues, ref index);
        }

        private void ParseMisc(string[] rawEntryValues, ref int index)
        {
            if (rawEntryValues.Length - index >= 12 && EventType != "SPELL_ABSORBED")
            {
                Misc = new MiscInfo(rawEntryValues, ref index);
            }
        }

        private void ParseDamage(string[] rawEntryValues, ref int index)
        {
            if (EventType.EndsWith("_DAMAGE") || EventType.EndsWith("_DAMAGE_LANDED") ||
                EventType.EndsWith("SHIELD") || EventType == "DAMAGE_SPLIT")
                Damage = new DamageInfo(rawEntryValues, ref index);
        }

        private void ParseHeal(string[] rawEntryValues, ref int index)
        {
            if (EventType.EndsWith("_HEAL"))
                Heal = new HealInfo(rawEntryValues, ref index);
        }
        
        private void ParseMiss(string[] rawEntryValues, ref int index)
        {
            if (EventType.EndsWith("MISSED"))
                Miss = new MissInfo(rawEntryValues, ref index);
        }

        private void ParseOthers(string[] rawEntryValues, ref int index)
        {
            switch(EventType)
            {
                case "SPELL_ABSORBED":
                    AbsorbCaster = UnitFlyweight.ParseUnit(rawEntryValues, ref index);
                    AlternativeSpell = new SpellInfo(rawEntryValues, ref index);
                    Amount = ParseHelper.ParseValue<uint>(rawEntryValues[index++]);
                    break;
                case "SPELL_AURA_APPLIED":
                case "SPELL_AURA_REMOVED":
                case "SPELL_AURA_REFRESH":
                case "SPELL_AURA_BROKEN":
                    SpellAuraType = ParseHelper.ParseAuraType(rawEntryValues[index++]);
                    if (rawEntryValues.Length > index)
                        Amount = ParseHelper.ParseValue<uint>(rawEntryValues[index++]);
                    break;
                case "SPELL_AURA_APPLIED_DOSE":
                case "SPELL_AURA_REMOVED_DOSE":
                    SpellAuraType = ParseHelper.ParseAuraType(rawEntryValues[index++]);
                    Amount = ParseHelper.ParseValue<uint>(rawEntryValues[index++]);
                    break;
                case "SPELL_CAST_FAILED":
                    FailReason = rawEntryValues[index++];
                    break;
                case "SPELL_ENERGIZE":
                    PowerType = ParseHelper.ParseValue<int>(rawEntryValues[index++]);
                    Amount = ParseHelper.ParseValue<uint>(rawEntryValues[index++]);
                    break;
                case "SPELL_INTERRUPT":
                    AlternativeSpell = new SpellInfo(rawEntryValues, ref index);
                    break;
                case "SPELL_AURA_BROKEN_SPELL":
                    AlternativeSpell = new SpellInfo(rawEntryValues, ref index);
                    SpellAuraType = ParseHelper.ParseAuraType(rawEntryValues[index++]);
                    break;
                case "UNIT_DIED":
                case "UNIT_DESTORYED":
                case "UNIT_DESTROYED":
                case "SPELL_INSTAKILL":
                case "PARTY_KILL":
                    if (rawEntryValues[index] == "0")
                        index++;
                    break;
            }
            if (rawEntryValues.Length > index && false)
            {
                Console.Write(EventType);
                Console.Write(" ");
                Console.WriteLine(string.Join(",", rawEntryValues.Skip(index)));
                Console.WriteLine(string.Join(",", rawEntryValues));
            }
        }


        public RawCombatLogEntry(DateTime time, string[] rawEntryValues)
        {
            this.Time = time;
            int index = 0;
            ParseEventType(rawEntryValues, ref index);
            if (EventType.StartsWith("ENCOUNTER"))
            {
                //Parse Encounter
            }
            else
            {
                ParseUnits(rawEntryValues, ref index);
                ParseSpell(rawEntryValues, ref index);
                ParseMisc(rawEntryValues, ref index);

                if (EventType.StartsWith("ENVIRONMENTAL_"))
                {
                    index++;
                }
                ParseDamage(rawEntryValues, ref index);
                ParseHeal(rawEntryValues, ref index);
                ParseMiss(rawEntryValues, ref index);
                ParseOthers(rawEntryValues, ref index);
            }

            this.rawEntryValues = rawEntryValues.Skip(index).ToArray();
        }

        public string RawEntryValue(int index)
        {
            return rawEntryValues[index];
        }

        public T RawEntryValue<T>(int index)
        {
            return (T)Convert.ChangeType(RawEntryValue(index), typeof(T));
        }

        public DateTime Time { get; private set; }
        public string EventType { get; private set; }
        public Unit Source { get; private set; }
        public Unit Dest { get; private set; }
        public SpellInfo Spell { get; private set; }
        public DamageInfo Damage { get; private set; }
        public MiscInfo Misc { get; private set; }
        public HealInfo Heal { get; private set; }
        public MissInfo Miss { get; private set; }

        public Unit AbsorbCaster { get; private set; }
        public Nullable<uint> Amount { get; private set; }
        public Nullable<int> PowerType { get; private set; }
        public Nullable<AuraType> SpellAuraType { get; private set; }
        public string FailReason { get; private set; }
        public SpellInfo AlternativeSpell { get; private set; }

        private string[] rawEntryValues;
    }
}
