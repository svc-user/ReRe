using System.ComponentModel;
using ReRe.Models;

namespace ReRe.Interfaces;

public interface IRequestHandler<TReq, TRes> : IEmptyHandler
{
    Task<TRes> Handle(MessageContext<TReq> context);
}

public interface IEmptyHandler
{
}