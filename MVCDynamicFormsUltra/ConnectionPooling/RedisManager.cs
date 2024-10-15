using StackExchange.Redis;
using System;

public class RedisManager
{
    private static string?  _RedisConnection;
     private static Lazy<ConnectionMultiplexer> _connectionMultiplexer ;
    public RedisManager(string Redisconn)
    {
         _RedisConnection = Redisconn;
        _connectionMultiplexer = new Lazy<ConnectionMultiplexer>(CreateConnection);
    }
   

    private static ConnectionMultiplexer CreateConnection()
    {
        var configurationOptions = ConfigurationOptions.Parse(_RedisConnection);

        configurationOptions.AbortOnConnectFail = false; configurationOptions.ReconnectRetryPolicy = new LinearRetry(5000);

        // var configurationOptions = new ConfigurationOptions
        // {
        //     EndPoints = { _RedisConnection }, // Replace with your Redis server address
        //     AbortOnConnectFail = false,
        //     ConnectTimeout = 5000,
        //     SyncTimeout = 5000,
        //     KeepAlive = 180,
        //     ReconnectRetryPolicy = new LinearRetry(5000)
        // };

        return ConnectionMultiplexer.Connect(configurationOptions);
    }

    public static IDatabase GetDatabase()
    {
        return _connectionMultiplexer.Value.GetDatabase();
    }

    public static void DisposeConnection()
    {
        if (_connectionMultiplexer.IsValueCreated)
        {
            _connectionMultiplexer.Value.Dispose();
        }
    }
}
