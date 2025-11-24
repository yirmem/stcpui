using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using stcpui.Models;

namespace stcpui.Repository;

public interface IModbusRepository: IRepository<ModbusModel>
{
}

public class ModbusRepository : BaseRepository<ModbusModel>, IModbusRepository
{
    public ModbusRepository(IDbConnection connection) : base(connection)
    {
    }
}