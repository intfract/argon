using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace Argon;

public class Argon
{
    public class ArgonType
    {
        private dynamic data;
        public dynamic Data
        {
            get => data;
            set
            {
                data = value;
            }
        }

        public new string ToString()
        {
            return "";
        }
    }

    public class Processor
    {
        public bool isEmpty = false;
        public Func<byte, byte[], byte>? callback;
        public int noInterrupt = 0;

        public Processor(bool isEmpty)
        {
            this.isEmpty = isEmpty;
        }
        public Processor(Func<byte, byte[], byte> callback)
        {
            this.callback = callback;
        }
        public Processor(Func<byte, byte[], byte> callback, int noInterrupt)
        {
            this.callback = callback;
            this.noInterrupt = noInterrupt;
        }
    }

    public class Table
    {
        public string name = "";
        public List<string> fieldNames = new();
        public List<Type> fieldTypes = new();
        public List<List<dynamic>> records = new();

        public Table(string name)
        {
            this.name = name;
        }

        public List<dynamic> CreateRecord()
        {
            List<dynamic> dataList = new();
            records.Add(dataList);
            return dataList;
        }

        public void AddRecordValue(ArgonType data)
        {
            records.Last().Add(data);
        }
    }

    public static List<Table> Tables = new ();
    public static List<string> TableNames = new();

    public class Text : ArgonType
    {
        private string data = "";
        public new byte[] Data
        {
            get => StringToBytes(data);
            set
            {
                string s = BytesToString(value);
                data = s;
            }
        }

        public Text() { }

        public Text(string data)
        {
            this.data = data;
        }

        public string GetValue()
        {
            return data;
        }

        public new string ToString()
        {
            return data;
        }
    }

    public class Integer : ArgonType
    {
        private int data;
        public new byte[] Data
        {
            get => IntegerToBytes(data);
            set
            {
                int z = BytesToInteger(value);
                data = z;
            }
        }

        public Integer() { }

        public Integer(int data)
        {
            this.data = data;
        }

        public int GetValue()
        {
            return data;
        }

        public new string ToString()
        {
            return data.ToString();
        }
    }

    public static Processor End()
    {
        return new Processor(true);
    }

    public static Processor Split()
    {
        return new Processor(true);
    }

    public static Processor CreateTable()
    {
        return new Processor(byte (byte id, byte[] bytes) =>
        {
            string tableName = BytesToString(bytes);
            Tables.Add(new Table(tableName));
            TableNames.Add(tableName);
            return 0;
        });
    }

    public static Processor AddField()
    {
        return new Processor(byte (byte id, byte[] bytes) =>
        {
            // PrintBytes(bytes);
            if (id % 2 == 0)
            {
                string fieldName = BytesToString(bytes);
                if (!Tables.Last().fieldNames.Contains(fieldName))
                {
                    Tables.Last().fieldNames.Add(fieldName);
                    // Console.WriteLine(Tables.Last().fieldNames.Last());
                    return 0;
                } else
                {
                    throw new Exception("Duplicate Field Name");
                }
            } else
            {
                if (bytes.Length == 1)
                {
                    byte index = (byte)(bytes[0] - CommandMap.Count);
                    Tables.Last().fieldTypes.Add(TypeMap[index]);
                    // Console.WriteLine(Tables.Last().fieldTypes.Last());
                    return 0;
                } else
                {
                    throw new Exception("Invalid Type Byte\nCorrect Size: 1 Byte");
                }
            }
        });
    }

    public static Processor AddRecord()
    {
        return new Processor(byte (byte id, byte[] bytes) =>
        {
            Table table = Tables.Last();
            if (id > table.fieldNames.Count) throw new Exception("Byte Overflow");
            Type dataType = table.fieldTypes[id];
            dynamic instance = Activator.CreateInstance(dataType);
            instance.Data = bytes;
            // Console.WriteLine(instance.ToString());
            if (id == 0) table.CreateRecord();
            table.AddRecordValue(instance);
            if (id + 1 < table.fieldTypes.Count)
                if (table.fieldTypes[id + 1] == typeof(Integer)) return 4; // no interrupt for 4 bytes
            return 0;
        });
    }

    public static Processor OpenFile()
    {
        return new Processor(byte (byte id, byte[] bytes) =>
        {
            string s = BytesToString(bytes);
            ProcessBytesFrom(s);
            return 0;
        });
    }

    public static Processor Ignore()
    {
        return new Processor(byte (byte id, byte[] bytes) =>
        {
            if (bytes.Length > 1) throw new Exception("Invalid Ignore Byte\nCorrect Size: 1 Byte");
            return (byte)(bytes[0] - CommandMap.Count); // more commands mean less ignorable bytes
        });
    }

    public static List<Func<Processor>> CommandMap = new()
    {
        End,
        Split,
        CreateTable,
        AddField,
        AddRecord,
        OpenFile,
        Ignore,
    };

    public static void PrintTable(Table table, byte? w)
    {
        int maxFieldWidth = table.name.Length;
        foreach (string fieldName in table.fieldNames)
        {
            if (fieldName.Length > maxFieldWidth) maxFieldWidth = fieldName.Length;
        }
        foreach (List<dynamic> record in table.records)
        {
            foreach (var field in record)
            {
                string fieldString = field.ToString();
                if (fieldString.Length > maxFieldWidth) maxFieldWidth = fieldString.Length;
            }
        }
        int fieldWith = w ?? maxFieldWidth + 1;
        int width = table.fieldNames.Count * (fieldWith + 2) + 2;
        string divider = $"+{new string('-', width - 2)}+";
        Console.WriteLine(divider);
        Console.WriteLine($"| {table.name}{new string(' ', width - table.name.Length - 4)} |");
        Console.WriteLine(divider);
        for (int i = 0; i < table.fieldNames.Count; i++)
        {
            string fieldName = table.fieldNames[i];
            Console.Write($"| {fieldName}{new string(' ', fieldWith - fieldName.Length)}");
        }
        Console.WriteLine(" |");
        Console.WriteLine(divider);
        foreach (List<dynamic> record in table.records)
        {
            foreach (var field in record)
            {
                Console.Write($"| {field.ToString()}{new string(' ', fieldWith - field.ToString().Length)}");
            }
            Console.WriteLine(" |");
        }
        Console.WriteLine(divider);
    }

    // Activator.CreateInstance
    public static List<Type> TypeMap = new()
    {
        typeof(Text),
        typeof(Integer),
    };

    public static List<char> CharacterMap = new();

    public static byte InitCharacters()
    {
        CharacterMap = CharacterMap
            .Concat("0123456789".ToCharArray()) // digits
            .Concat("ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray()) // capital alphabet
            .Concat("abcdefghijklmnopqrstuvwxyz".ToCharArray()) // lowercase alphabet
            .Concat(" \n") // whitespaces and breaks
            .Concat(",.;:!?".ToCharArray()) // dot punctuation
            .Concat("()[]{}".ToCharArray()) // parantheses, brackets, braces
            .Concat("'\"".ToCharArray()) // quotation marks
            .Concat("+-=<>".ToCharArray()) // arithmetic
            .Concat("@#$%^&*".ToCharArray()) // common symbols
            .Concat("_|/\\".ToCharArray()) // long lines
            .Concat("`~".ToCharArray()) // weird symbols
            .ToList();
        return (byte)CharacterMap.Count;
    }

    public static byte[] Chain(byte b)
    {
        return new byte[] { b };
    }

    public static byte[] TypeToBytes(Type t)
    {
        return Chain((byte)(TypeMap.IndexOf(t) + CommandMap.Count));
    }

    public static char ByteToCharacter(byte b)
    {
        return CharacterMap[b - CommandMap.Count];
    }

    public static byte CharacterToByte(char c)
    {
        return (byte)(CharacterMap.IndexOf(c) + CommandMap.Count);
    }

    public static byte[] StringToBytes(string s)
    {
        List<byte> bytes = new();
        foreach (char c in s)
        {
            bytes.Add(CharacterToByte(c));
        }
        return bytes.ToArray();
    }

    public static string BytesToString(byte[] bytes)
    {
        StringBuilder sb = new StringBuilder();
        foreach (byte b in bytes)
        {
            sb.Append(ByteToCharacter(b));
        }
        return sb.ToString();
    }

    public static byte[] IntegerToBytes(int z)
    {
        byte[] bytes = BitConverter.GetBytes(z);
        return bytes;
    }

    public static int BytesToInteger(byte[] bytes)
    {
        return BitConverter.ToInt32(bytes);
    }

    public static byte[] PrintBytes(byte[] bytes)
    {
        Console.Write("Bytes:");
        foreach (byte b in bytes)
        {
            Console.Write($" {b}");
        }
        Console.WriteLine();
        return bytes;
    }

    public static void ProcessBytesFrom(string fileName)
    {
        byte[] fileBytes = File.ReadAllBytes(fileName);
        List<byte> register = new();
        byte count = 0;
        byte ignoreCount = 0;
        Processor current = null;
        foreach (byte fileByte in fileBytes)
        {
            // Console.WriteLine(fileByte);
            if (ignoreCount < 0) throw new Exception("Negative Ignore Count");
            if (fileByte < CommandMap.Count && ignoreCount == 0)
            {
                Processor x = CommandMap[fileByte]();
                if (x.isEmpty)
                {
                    ignoreCount = current.callback(count, register.ToArray());
                    register.Clear();
                    switch (fileByte)
                    {
                        case 0:
                            // End
                            count = 0;
                            break;
                        case 1:
                            // Split
                            count++;
                            break;
                        default: break;
                    }
                }
                else
                {
                    current = x;
                }
            } else
            {
                if (ignoreCount > 0) ignoreCount--;
                register.Add(fileByte);
            }
        }
        foreach (Table table in Tables)
        {
            PrintTable(table, null);
        }
    }

    public const string Version = "Alpha";

    public static void Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("      ___           ___           ___           ___           ___     \n     /\\  \\         /\\  \\         /\\  \\         /\\  \\         /\\__\\    \n    /::\\  \\       /::\\  \\       /::\\  \\       /::\\  \\       /::|  |   \n   /:/\\:\\  \\     /:/\\:\\  \\     /:/\\:\\  \\     /:/\\:\\  \\     /:|:|  |   \n  /::\\~\\:\\  \\   /::\\~\\:\\  \\   /:/  \\:\\  \\   /:/  \\:\\  \\   /:/|:|  |__ \n /:/\\:\\ \\:\\__\\ /:/\\:\\ \\:\\__\\ /:/__/_\\:\\__\\ /:/__/ \\:\\__\\ /:/ |:| /\\__\\\n \\/__\\:\\/:/  / \\/_|::\\/:/  / \\:\\  /\\ \\/__/ \\:\\  \\ /:/  / \\/__|:|/:/  /\n      \\::/  /     |:|::/  /   \\:\\ \\:\\__\\    \\:\\  /:/  /      |:/:/  / \n      /:/  /      |:|\\/__/     \\:\\/:/  /     \\:\\/:/  /       |::/  /  \n     /:/  /       |:|  |        \\::/  /       \\::/  /        /:/  /   \n     \\/__/         \\|__|         \\/__/         \\/__/         \\/__/    \n");
        Console.ResetColor();
        Console.WriteLine($"Version: {Version}");
        Console.WriteLine($" + Commands: {CommandMap.Count}");
        Console.WriteLine($" + Characters: {InitCharacters()}");
        Console.WriteLine();
        if (args.Length == 1)
        {
            FileInfo file = new(args[0]);
            if (file.Exists)
            {
                ProcessBytesFrom(file.FullName);
            }
        } else
        {
            string fileName = Console.ReadLine() ?? "data.argon";
            try
            {
                ProcessBytesFrom(fileName);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }
        }
        Console.ReadKey();
    }
}
