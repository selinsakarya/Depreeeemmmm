using Depreeeemmmm.Proxies.TelegramApiProxy.Models.Requests;
using Depreeeemmmm.Proxies.TelegramApiProxy.Models.Responses;

namespace Depreeeemmmm.Proxies.TelegramApiProxy;

public interface ITelegramApiProxy
{
    Task<ProxyResponse<SendMessageApiResponse>> SendMessage(SendMessageApiRequest request, CancellationToken cancellationToken = default);
}