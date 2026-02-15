using Depreeeemmmm.Proxies.TelegramApi.Models.Requests;
using Depreeeemmmm.Proxies.TelegramApi.Models.Responses;

namespace Depreeeemmmm.Proxies.TelegramApi;

public interface ITelegramApiProxy
{
    Task<ProxyResponse<SendMessageApiResponse>> SendMessage(SendMessageApiRequest request, CancellationToken cancellationToken = default);
}