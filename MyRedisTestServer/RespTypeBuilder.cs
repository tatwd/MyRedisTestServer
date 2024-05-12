namespace MyRedisTestServer;

public static class RespTypeBuilder
{
    public static string String(string? str)
    {
        return $"${str?.Length ?? 0}\r\n{str}\r\n";
    }

    public static string Inline(string str)
    {
        return $"+{str}\r\n";
    }

    public static string Error(string err)
    {
        return $"-{err}\r\n";
    }

    public static string Int(int n)
    {
        return $":{n}\r\n";
    }

    public static string Float(double n)
    {
        return $",{n}\r\n";
    }

    public static string Nil() => "$-1\r\n";

    public static string NilResp3() => "_\r\n";
    
    public static string NilList() => "*-1\r\n";

    // Array(true, String("foo"), String("bar"))
    public static string Array(params string[] args)
    {
        return $"*{args.Length}\r\n" + string.Join("", args);
    }
    
    public static string Push(params string[] args)
    {
        return $">{args.Length}\r\n" + string.Join("", args);
    }

    public static string Strings(params string[] args)
    {
        var arr = new string[args.Length];

        for (var i = 0; i < args.Length; i++)
        {
            arr[i] = String(args[i]);
        }

        return Array(arr);
    }

    public static string Ints(params int[] args)
    {
        var arr = new string[args.Length];

        for (var i = 0; i < args.Length; i++)
        {
            arr[i] = Int(args[i]);
        }

        return Array(arr);
    }

    // Map(String("foo"), String("bar"))
    public static string Map(params string[] args)
    {
        return $"%{args.Length / 2}\r\n" + string.Join("", args);
    }

    public static string StringMap(params string[] args)
    {
        var arr = new string[args.Length];

        for (var i = 0; i < args.Length; i++)
        {
            arr[i] = String(args[i]);
        }

        return Map(arr);
    }
    
    // Set(String("foo"), String("bar"))
    public static string Set(params string[] args)
    {
        return $"~{args.Length}\r\n" + string.Join("", args);
    }

    public static string StringSet(params string[] args)
    {
        var arr = new string[args.Length];

        for (var i = 0; i < args.Length; i++)
        {
            arr[i] = String(args[i]);
        }

        return Set(arr);
    }

}