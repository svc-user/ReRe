using ReRe.Models;

namespace ReRe.Interfaces;

public interface IRequestClient<in TReq>
{
    Task<TRes> GetResponse<TRes>(TReq request, MessageHeader? header = default) where TRes : class;
}

public interface IRequestClient{

}