public enum LaneType
{
    Left,
    Right
}

public class NoteData
{
    public double TimeInSeconds; // Momento exato que a nota precisa chegar na linha de hit ou ser tocada
    public LaneType Lane; // O quadrante que a nota percente
    public int NoteNumber; // O número da nota (ex: 60 para C4)

    public NoteData(double timeInSeconds, LaneType lane, int noteNumber)
    {
        TimeInSeconds = timeInSeconds;
        Lane = lane;
        NoteNumber = noteNumber;
    }
}
