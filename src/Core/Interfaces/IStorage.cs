namespace Core.Interfaces;

public interface IStorage
{
    public Task SaveFileToStorage(string filename, Stream fileContent);
}