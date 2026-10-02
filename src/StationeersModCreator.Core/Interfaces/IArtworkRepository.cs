namespace StationeersModCreator.Core.Interfaces;
public interface IArtworkRepository
{
    IReadOnlyList<string> Names { get; }

    byte[]? ReadImage(string name);
}
