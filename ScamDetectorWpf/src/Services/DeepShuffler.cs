using System.Security.Cryptography;

namespace Phyrexia.ScamDetectorWpf.Services;

/// <summary>
/// различные способы шафлинга деки
/// </summary>
public class DeepShuffler
{
    private readonly Random random = new ();

    public Task<List<Guid>> StrongShuffleAsync(List<Guid> input) 
        => Task.Factory.StartNew(() => input.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToList());

    public Task<List<Guid>> StupidShuffleAsync(List<Guid> input) 
        => Task.Factory.StartNew(() => input.OrderBy(_ => random.Next()).ToList());
}