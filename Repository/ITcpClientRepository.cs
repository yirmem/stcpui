using System.Data;
using stcpui.Models;

namespace stcpui.Repository;

public interface ITcpClientRepository: IRepository<TcpClientModel>
{
    
}

public class TcpClientRepository : BaseRepository<TcpClientModel>, ITcpClientRepository
{
    public TcpClientRepository(IDbConnection connection) : base(connection)
    {
    }
}