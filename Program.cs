using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
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
        private byte size = 255;
        public byte Size
        {
            get => size;
            set
            {
                if (0 < size && size < 255)
                {
                    size = value;
                }
                else
                {
                    throw new Exception("Invalid Size\nCorrect Size: 0 < ArgonType.size < 255");
                }
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
        public Type? type;
        public Func<byte, byte[], byte>? callback;
        public Processor(bool isEmpty)
        {
            this.isEmpty = isEmpty;
        }
        public Processor(Type type, Func<byte, byte[], byte> callback)
        {
            this.type = type;
            this.callback = callback;
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

    public static bool IsValid(dynamic value, byte size)
    {
        if (value.size <= size)
        {
            return true;
        }
        return false;
    }

    public class Text : ArgonType
    {
        private string data = "";
        public new byte[] Data
        {
            get => StringToBytes(data);
            set
            {
                string s = BytesToString(value);
                if (0 < s.Length && s.Length < size)
                {
                    data = s;
                }
                else
                {
                    throw new Exception("Invalid Size\nCorrect Size: 0 < Text.size < 255");
                }
            }
        }
        private byte size = 255;
        public new byte Size
        {
            get => size;
            set
            {
                if (0 < size && size < 255)
                {
                    size = value;
                } else
                {
                    throw new Exception("Invalid Size\nCorrect Size: 0 < Text.size < 255");
                }
            }
        }

        public Text() { }

        public Text(string data, byte? size)
        {
            Size = size ?? 255;
            if (IsValid(data, this.size))
            {
                this.data = data;
            } else
            {
                throw new Exception("Invalid Entry");
            }
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

    public static Processor Next()
    {
        return new Processor(true);
    }

    public static Processor Split()
    {
        return new Processor(true);
    }

    public static Processor CreateTable()
    {
        return new Processor(typeof(Text), byte (byte id, byte[] bytes) =>
        {
            string tableName = BytesToString(bytes);
            Tables.Add(new Table(tableName));
            TableNames.Add(tableName);
            // Console.WriteLine($"Created Table: {tableName}");
            return 0;
        });
    }

    public static Processor AddField()
    {
        return new Processor(typeof(Text), byte (byte id, byte[] bytes) =>
        {
            // PrintBytes(bytes);
            if (id % 2 == 0)
            {
                string fieldName = BytesToString(bytes);
                if (!Tables.Last().fieldNames.Contains(fieldName))
                {
                    Tables.Last().fieldNames.Add(fieldName);
                    // Console.WriteLine(Tables.Last().fieldNames.Last());
                    return (byte)TypeMap.IndexOf(typeof(Text));
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
                    return index;
                } else
                {
                    throw new Exception("Invalid Type Byte\nCorrect Size: 1 Byte");
                }
            }
        });
    }

    public static Processor AddRecord()
    {
        return new Processor(typeof(Text), byte (byte id, byte[] bytes) =>
        {
            // PrintBytes(bytes);
            Table table = Tables.Last();
            if (id > table.fieldNames.Count) throw new Exception("Byte Overflow");
            Type dataType = table.fieldTypes[id];
            dynamic instance = Activator.CreateInstance(dataType);
            instance.Data = bytes;
            // Console.WriteLine(instance.ToString());
            if (id == 0) table.CreateRecord();
            table.AddRecordValue(instance);
            return 0;
        });
    }

    public static void PrintTable(Table table, byte? w)
    {
        byte fieldWith = w ?? 16; // fixed equal column widths
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

    public static List<Func<Processor>> CommandMap = new()
    {
        Next,
        Split,
        CreateTable,
        AddField,
        AddRecord,
    };

    // Activator.CreateInstance
    public static List<Type> TypeMap = new()
    {
        typeof(Text),
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
        Processor current = null;
        foreach (byte fileByte in fileBytes)
        {
            // Console.WriteLine(fileByte);
            if (fileByte < CommandMap.Count)
            {
                Processor x = CommandMap[fileByte]();
                if (x.isEmpty)
                {
                    current.callback(count, register.ToArray());
                    register.Clear();
                    switch (fileByte)
                    {
                        case 0:
                            // Next
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
                register.Add(fileByte);
            }
        }
        foreach (Table table in Tables)
        {
            PrintTable(table, 16);
        }
    }

    public static void Main(string[] args)
    {
        Console.WriteLine("ARGON DATABASE!");
        Console.WriteLine($"Characters: {InitCharacters()}");
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
