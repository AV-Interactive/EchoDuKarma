using Godot;

namespace EchoduKarma.Scripts.Helpers;

public class CSVLoader
{
    public static string[] GetCleanLine(string rawLine, char seperator)
    {
        string[] columns = rawLine.Split(seperator);

        for (int i = 0; i < columns.Length; i++)
        {
            columns[i] = columns[i].Trim(' ', '\"');
        }
        
        return columns;
    }
}
