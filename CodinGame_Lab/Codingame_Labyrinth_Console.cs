using System.IO;
using System.Text;
public class Codingame_Labyrinth_ConsoleWriter : TextWriter
{
    public override Encoding Encoding => Encoding.ASCII;
    public override void Write(string value)
    {

    }
}

public class Codingame_Labyrinth_Console : TextReader
{
    static string[] map = {
        "###############",
        "##########C####",
        "##########.####",
        "##########....#",
        "#############.#",
        "#############.#",
        "#######T......#",
        "##########.####",
        "##########.####",
        "##########.####",
        "##########.####",
        "###############",
        "###############",
        "###############",
        "###############",
        "###############",
        "###############",
        "###############",
        "###############",
        "###############",
        "###############",
        "###############",
        "###############",
        "###############",
        "###############",
        "###############",
        "###############",
        "###############",
        "###############",
        "###############"    };

    static int phase = -1;
    public override string ReadLine()
    {
        switch (++phase)
        {
            case 0:
                return "30 15 100";
            case 1:
                return "6 7";
            default:
                if (phase < 31)
                    return map[phase - 2];
                else
                    return "";

        }        
    }
}
    
