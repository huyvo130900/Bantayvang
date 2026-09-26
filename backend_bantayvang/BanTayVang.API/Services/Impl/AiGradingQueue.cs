using System.Threading.Channels;

namespace BanTayVang.API.Services.Impl
{
    public record AiGradingQueueItem(int SubmissionDetailId, bool AutoFinalize);

    /// <summary>
    /// Message queue don gian dung System.Threading.Channels (co san trong .NET 8).
    /// Singleton - 1 channel dung chung cho toan app.
    /// Worker doc viec tu day va goi AI theo tung item.
    /// </summary>
    public class AiGradingQueue
    {
        private readonly Channel<AiGradingQueueItem> _channel;

        public AiGradingQueue()
        {
            // Bounded queue: toi da 1000 item, neu day thi Wait (khong mat item)
            _channel = Channel.CreateBounded<AiGradingQueueItem>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
        }

        /// <summary>Day SubmissionDetailId vao queue de Worker xu ly</summary>
        public async ValueTask EnqueueAsync(int submissionDetailId, bool autoFinalize = true, CancellationToken ct = default)
            => await _channel.Writer.WriteAsync(new AiGradingQueueItem(submissionDetailId, autoFinalize), ct);

        /// <summary>Worker goi ReadAllAsync de lang nghe va xu ly tung item</summary>
        public IAsyncEnumerable<AiGradingQueueItem> ReadAllAsync(CancellationToken ct = default)
            => _channel.Reader.ReadAllAsync(ct);
    }
}
