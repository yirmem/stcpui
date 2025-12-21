using System.Data;
using stcpui.Models;

namespace stcpui.Repository;

public interface IPm2WorkRepository: IRepository<Pm2WorkModel>
{
}

public class Pm2WorkRepository : BaseRepository<Pm2WorkModel>, IPm2WorkRepository
{
    public Pm2WorkRepository(IDbConnection connection) : base(connection)
    {
    }
}