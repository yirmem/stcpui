using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Dapper;
using stcpui.Models;

namespace stcpui.Repository;

public interface ITcpClientRepository: IRepository<TcpClientModel>
{
    Task<TcpClientModel> GetByIpAsync(string address);
}

public class TcpClientRepository : BaseRepository<TcpClientModel>, ITcpClientRepository
{
    private readonly IDbConnection _connection;
    public TcpClientRepository(IDbConnection connection) : base(connection)
    {
        _connection = connection;
    }

    public async Task<TcpClientModel> GetByIpAsync(string ip)
    {
        try
        {
            var sql = $"SELECT * FROM TcpClient WHERE Ip='{ip}'";
            return await _connection.QueryFirstOrDefaultAsync<TcpClientModel>(sql);
        }catch (Exception ex)
        {
            string appDir = AppContext.BaseDirectory;
            File.AppendAllText(appDir+"debug.log", $"{ex.Message}\n");
        }

        return null;
    }
}